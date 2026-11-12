using D2ApiCreator.Application.DTOs;
using MediatR;
namespace D2ApiCreator.Application.Features.Documents.Queries.ExtractPlaceholders;
/// <summary>
/// Query to extract placeholders from a DOCX document
/// </summary>
public class ExtractPlaceholdersQuery : IRequest<DocumentPlaceholderDto>
{
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
}
