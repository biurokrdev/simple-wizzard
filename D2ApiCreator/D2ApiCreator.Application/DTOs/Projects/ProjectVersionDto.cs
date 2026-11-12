using D2ApiCreator.Domain.Enums;
using System.Text.Json;

namespace D2ApiCreator.Application.DTOs.Projects;

/// <summary>
/// DTO representing a project version
/// </summary>
public class ProjectVersionDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public int Major { get; set; }
    public int Minor { get; set; }
    public string VersionTag { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public ProjectStatus Status { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public JsonDocument? StepData { get; set; }
    public string? Notes { get; set; }

    // Przegląd/akceptacja (BR-017)
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewComment { get; set; }
    /// <summary>Uwagi recenzenta do kroków kreatora (klucz kroku → treść).</summary>
    public Dictionary<string, string>? ReviewNotes { get; set; }

    // Paczka wdrożeniowa (BR-022): ostatnia wysyłka do aplikacji docelowej
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveredTo { get; set; }
    public string? DeliveredPackageSha { get; set; }
}
