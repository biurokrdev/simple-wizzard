using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Workers;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using System.Text.Json;

namespace D2ApiCreator.Application.Features.Workers.Commands.UpdateWorker;

/// <summary>
/// Handler for updating a worker
/// </summary>
public class UpdateWorkerCommandHandler : IRequestHandler<UpdateWorkerCommand, WorkerDto>
{
    private readonly IWorkerRepository _workerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWorkerCommandHandler(
        IWorkerRepository workerRepository,
        IUnitOfWork unitOfWork)
    {
        _workerRepository = workerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkerDto> Handle(UpdateWorkerCommand request, CancellationToken cancellationToken)
    {
        var worker = await _workerRepository.GetByIdAsync(request.Id, cancellationToken);
        if (worker == null)
        {
            throw new KeyNotFoundException($"Worker with ID {request.Id} not found");
        }

        var configJson = JsonDocument.Parse(request.Dto.ConfigJson);
        
        worker.UpdateConfiguration(
            request.Dto.Name,
            request.Dto.WorkerType,
            configJson
        );

        await _workerRepository.UpdateAsync(worker, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new WorkerDto
        {
            Id = worker.Id,
            ProjectVersionId = worker.ProjectVersionId,
            Name = worker.Name,
            WorkerType = worker.WorkerType,
            ConfigJson = worker.ConfigJson.RootElement.GetRawText(),
            CreatedAt = worker.CreatedAt,
            UpdatedAt = worker.UpdatedAt
        };
    }
}
