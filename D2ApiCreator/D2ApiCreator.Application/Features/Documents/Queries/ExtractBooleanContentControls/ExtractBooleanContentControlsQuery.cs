using D2ApiCreator.Application.Common;
using MediatR;

namespace D2ApiCreator.Application.Features.Documents.Queries.ExtractBooleanContentControls;

/// <summary>
/// Query to extract all content controls from a DOCX document with their types
/// </summary>
public class ExtractBooleanContentControlsQuery : IRequest<Result<Dictionary<string, string>>>
{
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
}

