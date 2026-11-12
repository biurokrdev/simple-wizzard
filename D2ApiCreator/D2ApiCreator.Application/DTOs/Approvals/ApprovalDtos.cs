namespace D2ApiCreator.Application.DTOs.Approvals;

/// <summary>Wersja pisma czekająca na decyzję administratora (BR-017).</summary>
public class PendingVersionDto
{
    public Guid VersionId { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = null!;
    public string Source { get; set; } = null!;
    public Guid? TenantId { get; set; }
    public string VersionTag { get; set; } = null!;
    public DateTime? SubmittedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    /// <summary>Nazwa pliku szablonu wersji (jeśli jest).</summary>
    public string? TemplateName { get; set; }
}

/// <summary>Decyzja administratora.</summary>
public class ReviewDecisionDto
{
    /// <summary>Identyfikator recenzenta (klucz korporacyjny / login); do czasu R-03 podawany przez GUI.</summary>
    public string? Reviewer { get; set; }
    /// <summary>Komentarz — przy odrzuceniu wymagany (co poprawić).</summary>
    public string? Comment { get; set; }
}

/// <summary>Uwagi recenzenta do kroków kreatora (zapisywane w trakcie przeglądu, przed decyzją).</summary>
public class ReviewNotesDto
{
    /// <summary>Klucz kroku (documentDescription | templateAndSchema | configuration | summary) → treść uwagi.</summary>
    public Dictionary<string, string> Notes { get; set; } = new();
}

/// <summary>Zgłoszenie wersji do publikacji przez projektanta.</summary>
public class SubmitForReviewDto
{
    public string? SubmittedBy { get; set; }
}
