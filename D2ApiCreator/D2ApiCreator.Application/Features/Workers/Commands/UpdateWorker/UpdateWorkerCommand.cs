using D2ApiCreator.Application.DTOs.Workers;
using MediatR;

namespace D2ApiCreator.Application.Features.Workers.Commands.UpdateWorker;

/// <summary>
/// Command to update a worker
/// </summary>
public record UpdateWorkerCommand(Guid Id, UpdateWorkerDto Dto) : IRequest<WorkerDto>;
