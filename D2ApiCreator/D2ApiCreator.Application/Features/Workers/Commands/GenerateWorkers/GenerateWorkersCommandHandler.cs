using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs;
using D2ApiCreator.Application.DTOs.Workers;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace D2ApiCreator.Application.Features.Workers.Commands.GenerateWorkers;

/// <summary>
/// Handler for generating workers from document analysis
/// Creates "Podstaw wartość" workers for placeholders and "Stan" workers for content controls
/// </summary>
public class GenerateWorkersCommandHandler : IRequestHandler<GenerateWorkersCommand, List<WorkerDto>>
{
    private readonly IWorkerRepository _workerRepository;
    private readonly IProjectVersionRepository _projectVersionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GenerateWorkersCommandHandler> _logger;

    public GenerateWorkersCommandHandler(
        IWorkerRepository workerRepository,
        IProjectVersionRepository projectVersionRepository,
        IUnitOfWork unitOfWork,
        ILogger<GenerateWorkersCommandHandler> logger)
    {
        _workerRepository = workerRepository;
        _projectVersionRepository = projectVersionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<WorkerDto>> Handle(GenerateWorkersCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        // Verify project version exists
        var projectVersion = await _projectVersionRepository.GetByIdAsync(dto.ProjectVersionId, cancellationToken);
        if (projectVersion == null)
        {
            throw new KeyNotFoundException($"Project version with ID {dto.ProjectVersionId} not found");
        }

        _logger.LogInformation(
            "Generating workers for project version {ProjectVersionId}: {PlaceholderCount} placeholders, {ControlCount} content controls",
            dto.ProjectVersionId, dto.Placeholders.Count, dto.ContentControls.Count);

        // Delete existing workers for this project version
        await _workerRepository.DeleteByProjectVersionIdAsync(dto.ProjectVersionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var workers = new List<Worker>();

        var placeholderDetails = (dto.PlaceholderDetails ?? new List<PlaceholderDetailDto>())
            .GroupBy(d => d.Placeholder, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        // Generate "Podstaw wartość" workers for placeholders
        foreach (var placeholder in dto.Placeholders)
        {
            var cleanName = placeholder.Replace("<%", "").Replace("%>", "").Trim();
            placeholderDetails.TryGetValue(placeholder, out var placeholderDetail);
            
            var configJson = JsonDocument.Parse(JsonSerializer.Serialize(BuildSubstitutionConfig(cleanName, placeholder, placeholderDetail), ConfigJsonOptions));

            var worker = new Worker(
                Guid.NewGuid(),
                dto.ProjectVersionId,
                cleanName,
                "Podstaw wartość",
                configJson
            );

            workers.Add(worker);
            await _workerRepository.AddAsync(worker, cancellationToken);
        }

        // Content controls: wariant → "Stan" (jak dotąd); fragment powtarzany / lista / tabela → "Powtórz blok"
        var detailsByName = (dto.ContentControlDetails ?? new List<ContentControlDetailDto>())
            .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var control in dto.ContentControls)
        {
            detailsByName.TryGetValue(control.Key, out var detail);
            var worker = BuildContentControlWorker(dto.ProjectVersionId, control.Key, control.Value, detail);

            workers.Add(worker);
            await _workerRepository.AddAsync(worker, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully generated {WorkerCount} workers for project version {ProjectVersionId}",
            workers.Count, dto.ProjectVersionId);

        // Map to DTOs
        return workers.Select(w => new WorkerDto
        {
            Id = w.Id,
            ProjectVersionId = w.ProjectVersionId,
            Name = w.Name,
            WorkerType = w.WorkerType,
            ConfigJson = w.ConfigJson.RootElement.GetRawText(),
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt
        }).ToList();
    }

    /// <summary>Konfiguracje zapisujemy bez escapowania „&lt;” / „&gt;” (znaczniki &lt;%…%&gt; zostają czytelne) i bez pustych pól.</summary>
    public static readonly JsonSerializerOptions ConfigJsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Typ workera dla formantu: z rodzaju (gdy podany), inaczej z typu w schemacie ("array" → powtarzany).</summary>
    public static string ResolveContentControlWorkerType(string schemaType, ContentControlDetailDto? detail)
    {
        var kind = detail?.Kind ?? (string.Equals(schemaType, "array", StringComparison.OrdinalIgnoreCase) ? ContentControlKinds.Repeat : ContentControlKinds.Variant);
        return kind == ContentControlKinds.Variant ? WorkerTypeNames.Visibility : WorkerTypeNames.Repeat;
    }

    /// <summary>Zakres powielania dla „Powtórz blok”: tabela → wiersz, lista → akapit, inaczej cały blok.</summary>
    public static string ResolveRepeatScope(ContentControlDetailDto? detail) => detail?.Kind switch
    {
        ContentControlKinds.Table => "row",
        ContentControlKinds.List => "paragraph",
        _ => "block"
    };

    /// <summary>„Podstaw wartość”: jak dotąd; dla XLSX dodatkowo arkusz, komórka i etykieta z komórki obok.</summary>
    public static Dictionary<string, object?> BuildSubstitutionConfig(string cleanName, string placeholder, PlaceholderDetailDto? detail)
    {
        var config = new Dictionary<string, object?>
        {
            ["replacement_property_expression"] = $"formModel.{cleanName}",
            ["search_for"] = placeholder
        };
        if (detail != null)
        {
            if (!string.IsNullOrWhiteSpace(detail.Sheet)) config["sheet"] = detail.Sheet;
            if (!string.IsNullOrWhiteSpace(detail.Cell)) config["cell"] = detail.Cell;
            if (!string.IsNullOrWhiteSpace(detail.Label)) config["label"] = detail.Label;
        }
        return config;
    }

    public static Worker BuildContentControlWorker(Guid projectVersionId, string name, string schemaType, ContentControlDetailDto? detail)
    {
        var workerType = ResolveContentControlWorkerType(schemaType, detail);
        object config;
        if (workerType == WorkerTypeNames.Visibility)
        {
            config = new { tag_name = name, conditional_expression = $"formModel.{name}" };
        }
        else if (detail?.Columns is { Count: > 0 } columns)
        {
            // Wiersz arkusza: każda komórka → właściwość elementu listy („item.kwota”), a przy jednej kolumnie → sam element („item”).
            // Lista z JSON-a może mieć więcej pól niż tabela — mapujemy tylko te kolumny.
            config = new
            {
                tag_name = name,
                collection_expression = $"formModel.{name}",
                repeat_scope = "row",
                sheet = detail.Sheet,
                range = detail.Range,
                columns = columns.Select(c => new
                {
                    cell = c.Cell,
                    search_for = c.Placeholder,
                    item_property_expression = string.IsNullOrEmpty(c.Property) ? "item" : $"item.{c.Property}",
                    header = c.Header
                }).ToList()
            };
        }
        else
        {
            config = new { tag_name = name, collection_expression = $"formModel.{name}", repeat_scope = ResolveRepeatScope(detail) };
        }

        return new Worker(Guid.NewGuid(), projectVersionId, name, workerType, JsonDocument.Parse(JsonSerializer.Serialize(config, ConfigJsonOptions)));
    }
}

/// <summary>Nazwy typów workerów zapisywane do <c>workers.worker_type</c> (polskie literały — zgodne z <c>worker_profiles.key</c>).</summary>
public static class WorkerTypeNames
{
    public const string Substitution = "Podstaw wartość";
    public const string Visibility = "Stan";
    public const string Repeat = "Powtórz blok";
}
