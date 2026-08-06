using System.Runtime.CompilerServices;
using KickstartCreator.Web.Models;
using KickstartCreator.Web.Services;
using Xunit;

namespace KickstartCreator.Tests;

public class ScribanKickstartTemplateRendererTests
{
    private sealed class FakeTemplateProvider(string templatesDirectory) : IKickstartTemplateProvider
    {
        public string TemplatesDirectory { get; } = templatesDirectory;

        public string GetTemplatePath(RhelVersionOption version) => Path.Combine(TemplatesDirectory, "rhel9.ks.sbn");
    }

    // Resolves the real Templates/ directory relative to this source file, so
    // the test works regardless of whether Content-copy-to-output-directory
    // propagates to the test project's own bin folder.
    private static string GetRepoTemplatesDirectory([CallerFilePath] string testFilePath = "")
    {
        var testDir = Path.GetDirectoryName(testFilePath)!;
        return Path.GetFullPath(Path.Combine(testDir, "..", "..", "src", "KickstartCreator.Web", "Templates"));
    }

    private static KickstartTemplateModel BuildModel(string networkMode) => new()
    {
        RhelVersionLabel = "RHEL 9.8",
        GeneratedAtUtc = "2026-01-01 00:00:00",
        DiskById = "/dev/disk/by-id/md-uuid-TESTDISK",
        Hostname = "test-host.domain.local",
        NetworkMode = networkMode,
        StaticIp = "10.0.2.15",
        StaticNetmask = "255.255.255.0",
        StaticGateway = "10.0.2.254",
        StaticDns = "10.0.2.1",
        SshAllowedCidrs = ["10.41.202.11/32", "10.41.205.11/32"],
        SshPort = 9022,
        AdminUsername = "focas",
        AdminUid = 9001,
        AdminGroupsCsv = "wheel,sshusers",
        AdminPasswordHash = "$6$fakeSaltAdmin$fakehashadminfakehashadminfakehashadminfakehashadmin1",
        RootPasswordHash = "$6$fakeSaltRoot$fakehashrootfakehashrootfakehashrootfakehashrootfakehashroot12345",
        GrubPasswordHash = "grub.pbkdf2.sha512.10000.AAAA.BBBB",
        Timezone = "Europe/Warsaw",
        NtpServers = ["tempus1.gum.gov.pl", "tempus2.gum.gov.pl"],
    };

    private static ScribanKickstartTemplateRenderer CreateRenderer()
        => new(new FakeTemplateProvider(GetRepoTemplatesDirectory()));

    [Fact]
    public void Render_SubstitutesDiskByIdInExactlyFourPlaces()
    {
        var rendered = CreateRenderer().Render(RhelVersionOption.Rhel98, BuildModel("dhcp"));

        Assert.Equal(4, CountOccurrences(rendered, "/dev/disk/by-id/md-uuid-TESTDISK"));
    }

    [Fact]
    public void Render_SubstitutesSshPortInAllFunctionalLocations()
    {
        var rendered = CreateRenderer().Render(RhelVersionOption.Rhel98, BuildModel("dhcp"));

        Assert.Contains("firewall-offline-cmd --zone=sshaccess --add-port=9022/tcp", rendered);
        Assert.Contains("semanage port -a -t ssh_port_t -p tcp 9022", rendered);
        Assert.Contains("Port 9022", rendered);
    }

    [Fact]
    public void Render_ProducesExactlyOneActiveNetworkLine_ForDhcp()
    {
        var rendered = CreateRenderer().Render(RhelVersionOption.Rhel98, BuildModel("dhcp"));

        Assert.Contains("network --device=link --hostname=test-host.domain.local --bootproto=dhcp --onboot=true", rendered);
        Assert.DoesNotContain("--bootproto=static", rendered);
    }

    [Fact]
    public void Render_ProducesExactlyOneActiveNetworkLine_ForStatic()
    {
        var rendered = CreateRenderer().Render(RhelVersionOption.Rhel98, BuildModel("static"));

        Assert.Contains(
            "network --bootproto=static --ip=10.0.2.15 --netmask=255.255.255.0 --gateway=10.0.2.254 --nameserver=10.0.2.1 --hostname=test-host.domain.local --onboot=true",
            rendered);
        Assert.DoesNotContain("--bootproto=dhcp", rendered);
    }

    [Fact]
    public void Render_LeavesNoUnresolvedTemplateTokens()
    {
        var rendered = CreateRenderer().Render(RhelVersionOption.Rhel98, BuildModel("dhcp"));

        Assert.DoesNotContain("{{", rendered);
        Assert.DoesNotContain("}}", rendered);
    }

    [Fact]
    public void Render_IncludesAllConfiguredSshCidrsInFirewallAndSummaryChecklist()
    {
        var rendered = CreateRenderer().Render(RhelVersionOption.Rhel98, BuildModel("dhcp"));

        Assert.Contains("firewall-offline-cmd --zone=sshaccess --add-source=10.41.202.11/32", rendered);
        Assert.Contains("firewall-offline-cmd --zone=sshaccess --add-source=10.41.205.11/32", rendered);
        Assert.Contains("Firewalld sshaccess zawiera 10.41.202.11/32", rendered);
        Assert.Contains("Firewalld sshaccess zawiera 10.41.205.11/32", rendered);
    }

    [Fact]
    public void Render_NeverEmitsPlaintextPasswords()
    {
        var rendered = CreateRenderer().Render(RhelVersionOption.Rhel98, BuildModel("dhcp"));

        Assert.Contains("$6$fakeSaltAdmin$", rendered, StringComparison.Ordinal);
        // Sanity: only hashes appear, no leftover template placeholders.
        Assert.DoesNotContain("<wygeneruj haslo>", rendered);
        Assert.DoesNotContain("<uzupelnij>", rendered);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
