using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.Workers;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Repositories;
using MediatR;
using System.Text.Json;

namespace D2ApiCreator.Application.Features.Workers.Commands.CreateWorker;

/// <summary>
/// Handler for creating a new worker
/// </summary>
public class CreateWorkerCommandHandler : IRequestHandler<CreateWorkerCommand, WorkerDto>
{
    private readonly IWorkerRepository _workerRepository;
    private readonly IProjectVersionRepository _projectVersionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWorkerCommandHandler(
        IWorkerRepository workerRepository,
        IProjectVersionRepository projectVersionRepository,
        IUnitOfWork unitOfWork)
    {
        _workerRepository = workerRepository;
        _projectVersionRepository = projectVersionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkerDto> Handle(CreateWorkerCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        // Verify project version exists
        var projectVersion = await _projectVersionRepository.GetByIdAsync(dto.ProjectVersionId, cancellationToken);
        if (projectVersion == null)
        {
            throw new KeyNotFoundException($"Project version with ID {dto.ProjectVersionId} not found");
        }

        var configJson = JsonDocument.Parse(dto.ConfigJson);
        
        var worker = new Worker(
            Guid.NewGuid(),
            dto.ProjectVersionId,
            dto.Name,
            dto.WorkerType,
            configJson
        );

        await _workerRepository.AddAsync(worker, cancellationToken);
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
