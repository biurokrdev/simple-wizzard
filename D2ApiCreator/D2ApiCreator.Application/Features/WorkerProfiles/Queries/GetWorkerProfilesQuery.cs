using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.DTOs.WorkerProfiles;
using D2ApiCreator.Domain.Repositories;
using MediatR;

namespace D2ApiCreator.Application.Features.WorkerProfiles.Queries;

/// <summary>Lista profili workerów. <c>OnlyVisible</c> = tylko typy udostępnione użytkownikom kreatora.</summary>
public record GetWorkerProfilesQuery(bool OnlyVisible) : IRequest<Result<List<WorkerProfileDto>>>;

public class GetWorkerProfilesQueryHandler : IRequestHandler<GetWorkerProfilesQuery, Result<List<WorkerProfileDto>>>
{
    private readonly IWorkerProfileRepository _repository;

    public GetWorkerProfilesQueryHandler(IWorkerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<WorkerProfileDto>>> Handle(GetWorkerProfilesQuery request, CancellationToken cancellationToken)
    {
        var profiles = request.OnlyVisible
            ? await _repository.GetVisibleAsync(cancellationToken)
            : await _repository.GetAllOrderedAsync(cancellationToken);

        return Result<List<WorkerProfileDto>>.Success(profiles.Select(WorkerProfileMapper.ToDto).ToList());
    }
}
