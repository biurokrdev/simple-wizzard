using D2ApiCreator.Application.DTOs.Workers;
using MediatR;

namespace D2ApiCreator.Application.Features.Workers.Commands.CreateWorker;

/// <summary>
/// Command to create a new worker
/// </summary>
public record CreateWorkerCommand(CreateWorkerDto Dto) : IRequest<WorkerDto>;
