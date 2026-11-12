namespace D2ApiCreator.Domain.Entities;

using Common.BaseTypes;

public class FileEntity : AggregateRoot<Guid>
{
    private readonly List<ParseResult> _parseResults = new();

    protected FileEntity() { }

    /// <summary>
    /// Plik przypięty do wersji projektu. Od 2026-10-05 nowe pliki mają <paramref name="content"/> = null
    /// i <paramref name="storageRef"/> = klucz obiektu w GCS (ADR-0005). Starsze wiersze mogą nadal mieć
    /// treść w <see cref="Content"/> (bytea) — oba warianty są poprawne, patrz <see cref="IsInExternalStorage"/>.
    /// </summary>
    public FileEntity(
        Guid id,
        Guid projectVersionId,
        string filename,
        byte[]? content,
        string? contentType = null,
        long? size = null,
        string? sha256 = null,
        string? storageRef = null)
        : base(id)
    {
        if (content is null && string.IsNullOrWhiteSpace(storageRef))
        {
            throw new ArgumentException("File must have either content or a storage reference.", nameof(storageRef));
        }

        ProjectVersionId = projectVersionId;
        Filename = filename;
        Content = content;
        StorageRef = storageRef;
        ContentType = contentType;
        Size = size ?? content?.Length;
        Sha256 = sha256;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid ProjectVersionId { get; private set; }
    public string Filename { get; private set; } = null!;
    public byte[]? Content { get; private set; }
    public string? StorageRef { get; private set; }
    public string? ContentType { get; private set; }
    public long? Size { get; private set; }
    public string? Sha256 { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Treść jest w zewnętrznym storage (GCS), a nie w kolumnie <c>files.content</c>.
    /// </summary>
    public bool IsInExternalStorage => !string.IsNullOrWhiteSpace(StorageRef);

    // Navigation properties
    public ProjectVersion ProjectVersion { get; private set; } = null!;
    public IReadOnlyCollection<ParseResult> ParseResults => _parseResults.AsReadOnly();

    public void MigrateToExternalStorage(string storageRef)
    {
        if (string.IsNullOrWhiteSpace(storageRef))
        {
            throw new ArgumentException("Storage reference is required.", nameof(storageRef));
        }

        StorageRef = storageRef;
        Content = null; // Clear DB storage after migration
    }
}
