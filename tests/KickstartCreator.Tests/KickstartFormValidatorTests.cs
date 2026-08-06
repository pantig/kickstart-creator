using KickstartCreator.Web.Models;
using KickstartCreator.Web.Services;
using KickstartCreator.Web.Validation;
using Xunit;

namespace KickstartCreator.Tests;

public class KickstartFormValidatorTests
{
    private sealed class FakeTimeZoneCatalog : ITimeZoneCatalog
    {
        public bool IsKnown(string timeZoneId) => timeZoneId is "Europe/Warsaw" or "Etc/UTC";
    }

    private static KickstartFormValidator CreateValidator() => new(new FakeTimeZoneCatalog());

    private static KickstartFormModel ValidModel() => new()
    {
        RhelVersion = RhelVersionOption.Rhel98,
        Hostname = "ct-ma-rh9.domain.local",
        DiskSelectionMode = DiskSelectionMode.Manual,
        DiskById = "/dev/disk/by-id/md-uuid-9f46537d:dccdf959:f48b39fa:056d38e3",
        NetworkMode = NetworkMode.Dhcp,
        SshAllowedCidrsRaw = "10.41.202.11/32\n10.41.205.11/32",
        SshPort = 9022,
        AdminUsername = "focas",
        AdminUid = 9001,
        AdminExtraGroupsRaw = "",
        AdminPasswordMode = PasswordSourceMode.Generate,
        RootPasswordMode = PasswordSourceMode.Generate,
        GrubPasswordMode = PasswordSourceMode.Generate,
        Timezone = "Europe/Warsaw",
        NtpServersRaw = "tempus1.gum.gov.pl\ntempus2.gum.gov.pl",
    };

    [Fact]
    public void Validate_AcceptsAWellFormedModel()
    {
        var result = CreateValidator().Validate(ValidModel());

        Assert.True(result.IsValid);
        Assert.NotNull(result.Parsed);
        Assert.Equal(2, result.Parsed!.SshAllowedCidrs.Count);
    }

    [Theory]
    [InlineData("host`whoami`")]
    [InlineData("host;rm -rf /")]
    [InlineData("host$(whoami)")]
    [InlineData("host name")]
    public void Validate_RejectsInjectionAttemptsInHostname(string hostname)
    {
        var model = ValidModel();
        model.Hostname = hostname;

        var result = CreateValidator().Validate(model);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("/dev/disk/by-id/foo; rm -rf /")]
    [InlineData("../../etc/passwd")]
    [InlineData("/dev/disk/by-id/`id`")]
    public void Validate_RejectsInjectionAttemptsInDiskById(string diskById)
    {
        var model = ValidModel();
        model.DiskById = diskById;

        var result = CreateValidator().Validate(model);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RequiresDiskByIdWhenModeIsManual()
    {
        var model = ValidModel();
        model.DiskSelectionMode = DiskSelectionMode.Manual;
        model.DiskById = null;

        var result = CreateValidator().Validate(model);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_AllowsMissingDiskByIdWhenModeIsInteractive()
    {
        var model = ValidModel();
        model.DiskSelectionMode = DiskSelectionMode.Interactive;
        model.DiskById = null;

        var result = CreateValidator().Validate(model);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("root")]
    [InlineData("bin")]
    [InlineData("wheel")]
    public void Validate_RejectsReservedUsernames(string username)
    {
        var model = ValidModel();
        model.AdminUsername = username;

        var result = CreateValidator().Validate(model);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("user`id`")]
    [InlineData("user;ls")]
    [InlineData("Admin")]
    public void Validate_RejectsMalformedUsernames(string username)
    {
        var model = ValidModel();
        model.AdminUsername = username;

        var result = CreateValidator().Validate(model);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("10.0.0.1/33")]
    [InlineData("not-an-ip/32")]
    [InlineData("10.0.0.1")]
    public void Validate_RejectsMalformedCidrs(string cidr)
    {
        var model = ValidModel();
        model.SshAllowedCidrsRaw = cidr;

        var result = CreateValidator().Validate(model);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RequiresStaticNetworkFieldsWhenModeIsStatic()
    {
        var model = ValidModel();
        model.NetworkMode = NetworkMode.Static;
        // StaticIp/Netmask/Gateway/Dns intentionally left null.

        var result = CreateValidator().Validate(model);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_AcceptsValidStaticNetworkConfiguration()
    {
        var model = ValidModel();
        model.NetworkMode = NetworkMode.Static;
        model.StaticIp = "10.0.2.15";
        model.StaticNetmask = "255.255.255.0";
        model.StaticGateway = "10.0.2.254";
        model.StaticDns = "10.0.2.1";

        var result = CreateValidator().Validate(model);

        Assert.True(result.IsValid);
        Assert.Equal("10.0.2.15", result.Parsed!.NormalizedStaticIp);
    }

    [Fact]
    public void Validate_RejectsUnknownTimezone()
    {
        var model = ValidModel();
        model.Timezone = "Mars/OlympusMons";

        var result = CreateValidator().Validate(model);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ForceIncludesMandatoryGroupsWithoutErroringOnReSpecification()
    {
        var model = ValidModel();
        model.AdminExtraGroupsRaw = "wheel,docker";

        var result = CreateValidator().Validate(model);

        Assert.True(result.IsValid);
        Assert.DoesNotContain("wheel", result.Parsed!.AdminExtraGroups);
        Assert.Contains("docker", result.Parsed!.AdminExtraGroups);
    }

    [Fact]
    public void Validate_RequiresManualPasswordWhenModeIsManual()
    {
        var model = ValidModel();
        model.AdminPasswordMode = PasswordSourceMode.Manual;
        model.AdminPassword = null;

        var result = CreateValidator().Validate(model);

        Assert.False(result.IsValid);
    }
}
