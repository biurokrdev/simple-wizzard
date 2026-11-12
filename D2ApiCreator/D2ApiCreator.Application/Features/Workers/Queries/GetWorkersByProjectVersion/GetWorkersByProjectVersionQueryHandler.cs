using D2ApiCreator.Application.DTOs.Workers;
using D2ApiCreator.Domain.Repositories;
using MediatR;

namespace D2ApiCreator.Application.Features.Workers.Queries.GetWorkersByProjectVersion;

/// <summary>
/// Handler for getting workers by project version
/// </summary>
public class GetWorkersByProjectVersionQueryHandler : IRequestHandler<GetWorkersByProjectVersionQuery, List<WorkerDto>>
{
    private readonly IWorkerRepository _workerRepository;

    public GetWorkersByProjectVersionQueryHandler(IWorkerRepository workerRepository)
    {
        _workerRepository = workerRepository;
    }

    public async Task<List<WorkerDto>> Handle(GetWorkersByProjectVersionQuery request, CancellationToken cancellationToken)
    {
        var workers = await _workerRepository.GetByProjectVersionIdAsync(request.ProjectVersionId, cancellationToken);

        return workers.Select(w => new WorkerDto
        {
            Id = w.Id,
            ProjectVersionId = w.ProjectVersionId,
            Name = w.Name,
            WorkerType = w.WorkerType,
            ConfigJson = w.ConfigJson.RootElement.GetRawText(),
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt
        }).ToList();
    }
}
