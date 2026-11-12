using MediatR;

namespace D2ApiCreator.Application.Features.Workers.Commands.DeleteWorker;

/// <summary>
/// Command to delete a worker
/// </summary>
public record DeleteWorkerCommand(Guid Id) : IRequest<bool>;
