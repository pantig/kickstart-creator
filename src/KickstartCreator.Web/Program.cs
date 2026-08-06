using KickstartCreator.Web.Auth;
using KickstartCreator.Web.Endpoints;
using KickstartCreator.Web.Services;
using KickstartCreator.Web.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.Configure<ProcessRunnerOptions>(builder.Configuration.GetSection("ProcessRunner"));
builder.Services.Configure<GrubPbkdf2Options>(builder.Configuration.GetSection("GrubPbkdf2"));
builder.Services.Configure<PasswordPolicyOptions>(builder.Configuration.GetSection("PasswordPolicy"));
builder.Services.Configure<ArtifactStorageOptions>(builder.Configuration.GetSection("ArtifactStorage"));
builder.Services.Configure<ArtifactRetentionOptions>(builder.Configuration.GetSection("ArtifactRetention"));

// Process execution + hashing/media-building services (they shell out to
// openssl / xorriso / mkfs.vfat / mtools installed in the container image).
builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
builder.Services.AddSingleton<IPasswordHashingService, OpenSslShaCryptHashingService>();
builder.Services.AddSingleton<ISaltProvider, RandomSaltProvider>();
builder.Services.AddSingleton<IGrubPasswordHashingService, GrubPbkdf2HashingService>();
builder.Services.AddSingleton<IPasswordGeneratorService, StrongPasswordGeneratorService>();

// Validation (allow-list rules + IANA timezone cross-check).
builder.Services.AddSingleton<ITimeZoneCatalog, SystemTimeZoneCatalog>();
builder.Services.AddSingleton<KickstartFormValidator>();

// Templating + media generation.
builder.Services.AddSingleton<IKickstartTemplateProvider, KickstartTemplateProvider>();
builder.Services.AddSingleton<IKickstartTemplateRenderer, ScribanKickstartTemplateRenderer>();
builder.Services.AddSingleton<IRemovableMediaBuilder, CliRemovableMediaBuilder>();

// Artifact storage/retention + top-level orchestrator.
builder.Services.AddSingleton<IArtifactStore, FileSystemArtifactStore>();
builder.Services.AddSingleton<IKickstartGenerationService, KickstartGenerationService>();
builder.Services.AddHostedService<ArtifactRetentionCleanupService>();

var basicAuthEnabled = builder.Configuration.GetValue<bool>("BasicAuth:Enabled");
if (basicAuthEnabled)
{
    builder.Services
        .AddAuthentication(BasicAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
            BasicAuthenticationHandler.SchemeName, _ => { });

    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder(BasicAuthenticationHandler.SchemeName)
            .RequireAuthenticatedUser()
            .Build();
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

if (basicAuthEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapRazorPages();
app.MapArtifactEndpoints();
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.Run();
