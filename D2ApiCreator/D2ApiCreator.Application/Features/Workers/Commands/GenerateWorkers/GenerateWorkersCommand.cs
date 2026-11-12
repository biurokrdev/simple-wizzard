using D2ApiCreator.Application.DTOs.Workers;
using MediatR;

namespace D2ApiCreator.Application.Features.Workers.Commands.GenerateWorkers;

/// <summary>
/// Command to generate workers from document placeholders and content controls
/// </summary>
public record GenerateWorkersCommand(GenerateWorkersDto Dto) : IRequest<List<WorkerDto>>;
