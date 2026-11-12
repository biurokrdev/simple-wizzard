using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using D2ApiCreator.Application.DTOs.Packages;
using D2ApiCreator.Domain.Entities;

namespace D2ApiCreator.Application.Features.Packages;

/// <summary>
/// Składa paczkę wdrożeniową wersji pisma (BR-022): ZIP z <c>manifest.json</c>, <c>workers.json</c>
/// (wszystkie workery wersji, konfiguracje jako obiekty JSON) i plikiem szablonu wgranym w tej wersji.
/// Czysta funkcja — bez bazy i storage; bajty szablonu dostarcza wywołujący.
/// </summary>
public static class ProjectPackageBuilder
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static ProjectPackage Build(
        Project project,
        ProjectVersion version,
        IReadOnlyCollection<Worker> workers,
        FileEntity? template,
        byte[]? templateBytes,
        string? tenantName,
        string? generatedBy,
        DateTime? now = null)
    {
        if (template != null && templateBytes == null)
        {
            throw new InvalidOperationException($"Template file '{template.Filename}' has no content available.");
        }

        var manifest = new PackageManifestDto
        {
            GeneratedAt = now ?? DateTime.UtcNow,
            GeneratedBy = generatedBy,
            Project = new PackageProjectInfo
            {
                Id = project.Id,
                Name = project.Name,
                Source = project.Source,
                TenantId = project.TenantId,
                TenantName = tenantName,
                ApplicationName = project.Metadata.ApplicationName,
                RetentionDays = project.RetentionDays
            },
            Version = new PackageVersionInfo
            {
                Id = version.Id,
                Tag = version.VersionTag,
                Major = version.Major,
                Minor = version.Minor,
                Status = version.Status.ToString(),
                IsActive = version.IsActive,
                CreatedAt = version.CreatedAt
            },
            Workers = new PackageWorkersInfo
            {
                Count = workers.Count,
                ByType = workers.GroupBy(w => w.WorkerType).ToDictionary(g => g.Key, g => g.Count())
            }
        };

        var workersFile = new PackageWorkersFile
        {
            Source = project.Source,
            VersionTag = version.VersionTag,
            Workers = workers
                .OrderBy(w => w.WorkerType, StringComparer.Ordinal).ThenBy(w => w.Name, StringComparer.Ordinal)
                .Select(w => new PackageWorkerEntry { Id = w.Id, Name = w.Name, WorkerType = w.WorkerType, Config = w.ConfigJson.RootElement.Clone() })
                .ToList()
        };

        if (template != null && templateBytes != null)
        {
            var safeName = SafeFileName(template.Filename);
            manifest.Template = new PackageTemplateInfo
            {
                Entry = $"template/{safeName}",
                FileName = template.Filename,
                ContentType = template.ContentType,
                Size = templateBytes.LongLength,
                Sha256 = Sha256Hex(templateBytes),
                Kind = Path.GetExtension(safeName).TrimStart('.').ToLowerInvariant()
            };
        }

        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddText(zip, "manifest.json", JsonSerializer.Serialize(manifest, JsonOptions));
            AddText(zip, "workers.json", JsonSerializer.Serialize(workersFile, JsonOptions));
            if (manifest.Template != null && templateBytes != null)
            {
                var entry = zip.CreateEntry(manifest.Template.Entry, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                entryStream.Write(templateBytes, 0, templateBytes.Length);
            }
        }

        var bytes = stream.ToArray();
        return new ProjectPackage
        {
            FileName = PackageFileName(project.Source, version.VersionTag),
            Content = bytes,
            Sha256 = Sha256Hex(bytes),
            Manifest = manifest
        };
    }

    /// <summary><c>{source}_v{tag}.zip</c> z nazwą bezpieczną dla systemu plików (np. <c>UmowaKredytowa_v1.2.zip</c>).</summary>
    public static string PackageFileName(string source, string versionTag)
        => $"{SafeFileName(source)}_v{SafeFileName(versionTag)}.zip";

    public static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            sb.Append(invalid.Contains(ch) || ch == '/' || ch == '\\' ? '_' : ch);
        }
        var result = sb.ToString().Trim();
        return result.Length == 0 ? "plik" : result;
    }

    public static string Sha256Hex(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    private static void AddText(ZipArchive zip, string entryName, string text)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(text);
    }
}
