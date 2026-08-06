using System.IO.Compression;
using KickstartCreator.Web.Models;
using KickstartCreator.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KickstartCreator.Web.Endpoints;

/// <summary>
/// File downloads for a generated artifact set. The first request for ANY file
/// belonging to a given id clears that id's in-memory plaintext secrets (see
/// GeneratedArtifactSet.ClearPlaintextSecrets) - "show once on the result page,
/// purge after download starts".
/// </summary>
public static class ArtifactEndpoints
{
    public static IEndpointRouteBuilder MapArtifactEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/artifacts/{id:guid}");

        group.MapGet("/ks.cfg", (Guid id, IArtifactStore store)
            => ServeFile(store, id, a => a.KickstartFileName, "text/plain"));

        group.MapGet("/oemdrv.iso", (Guid id, IArtifactStore store)
            => ServeFile(store, id, a => a.IsoFileName, "application/octet-stream"));

        group.MapGet("/oemdrv.img", (Guid id, IArtifactStore store)
            => ServeFile(store, id, a => a.ImgFileName, "application/octet-stream"));

        group.MapGet("/all.zip", (Guid id, IArtifactStore store) => ServeZip(store, id));

        return endpoints;
    }

    private static IResult ServeFile(IArtifactStore store, Guid id, Func<GeneratedArtifactSet, string> fileNameSelector, string contentType)
    {
        if (!store.TryGet(id, out var artifactSet))
        {
            return Results.NotFound();
        }

        artifactSet.ClearPlaintextSecrets();

        var fileName = fileNameSelector(artifactSet);
        var fullPath = Path.Combine(artifactSet.DirectoryPath, fileName);

        return File.Exists(fullPath)
            ? Results.File(fullPath, contentType, fileName)
            : Results.NotFound();
    }

    private static IResult ServeZip(IArtifactStore store, Guid id)
    {
        if (!store.TryGet(id, out var artifactSet))
        {
            return Results.NotFound();
        }

        artifactSet.ClearPlaintextSecrets();

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddFileIfExists(archive, artifactSet.DirectoryPath, artifactSet.KickstartFileName);
            AddFileIfExists(archive, artifactSet.DirectoryPath, artifactSet.IsoFileName);
            AddFileIfExists(archive, artifactSet.DirectoryPath, artifactSet.ImgFileName);
        }

        return Results.File(memoryStream.ToArray(), "application/zip", $"kickstart-{artifactSet.Hostname}.zip");
    }

    private static void AddFileIfExists(ZipArchive archive, string directoryPath, string fileName)
    {
        var fullPath = Path.Combine(directoryPath, fileName);
        if (File.Exists(fullPath))
        {
            archive.CreateEntryFromFile(fullPath, fileName);
        }
    }
}
