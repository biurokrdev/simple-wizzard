using System.Text.Json;

namespace D2ApiCreator.Domain.Entities;

using Common.BaseTypes;

public class ParseResult : AggregateRoot<Guid>
{
    protected ParseResult() { }

    public ParseResult(Guid id, Guid fileId)
        : base(id)
    {
        FileId = fileId;
        ParsedAt = DateTime.UtcNow;
    }

    public Guid FileId { get; private set; }
    public JsonDocument? ParseJson { get; private set; }
    public JsonDocument? SampleRequest { get; private set; }
    public JsonDocument? ValidationErrors { get; private set; }
    public DateTime ParsedAt { get; private set; }

    // Navigation property
    public FileEntity File { get; private set; } = null!;

    public void SetParseResults(JsonDocument? parseJson, JsonDocument? sampleRequest, JsonDocument? validationErrors)
    {
        ParseJson = parseJson;
        SampleRequest = sampleRequest;
        ValidationErrors = validationErrors;
    }
}

