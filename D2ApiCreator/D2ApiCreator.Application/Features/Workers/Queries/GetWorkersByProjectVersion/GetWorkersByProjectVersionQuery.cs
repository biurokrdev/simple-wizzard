using D2ApiCreator.Application.DTOs.Workers;
using MediatR;

namespace D2ApiCreator.Application.Features.Workers.Queries.GetWorkersByProjectVersion;

/// <summary>
/// Query to get all workers for a project version
/// </summary>
public record GetWorkersByProjectVersionQuery(Guid ProjectVersionId) : IRequest<List<WorkerDto>>;
