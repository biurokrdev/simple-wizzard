using MediatR;
using Microsoft.AspNetCore.Http;

namespace D2ApiCreator.Application.Features.Files.Commands.UploadFile;

/// <summary>
/// Command to upload a file to a project version
/// </summary>
public class UploadFileCommand : IRequest<Guid>
{
    public Guid ProjectVersionId { get; set; }
    public IFormFile File { get; set; } = null!;
    public string? FileType { get; set; }
}
