using System.Text.Json;
using D2ApiCreator.Application.Common;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Application.DTOs.WorkerProfiles;
using D2ApiCreator.Domain.Entities;
using D2ApiCreator.Domain.Enums;
using D2ApiCreator.Domain.Repositories;
using MediatR;

namespace D2ApiCreator.Application.Features.WorkerProfiles.Commands;

/// <summary>Ręczne dodanie profilu (administrator). Klucz musi być unikalny.</summary>
public record CreateWorkerProfileCommand(CreateWorkerProfileDto Dto) : IRequest<Result<WorkerProfileDto>>;

/// <summary>Edycja profilu (klucz niezmienny).</summary>
public record UpdateWorkerProfileCommand(Guid Id, UpdateWorkerProfileDto Dto) : IRequest<Result<WorkerProfileDto>>;

/// <summary>Usunięcie profilu — tylko dodanego ręcznie; zsynchronizowane można jedynie ukryć.</summary>
public record DeleteWorkerProfileCommand(Guid Id) : IRequest<Result<bool>>;

public class CreateWorkerProfileCommandHandler : IRequestHandler<CreateWorkerProfileCommand, Result<WorkerProfileDto>>
{
    private readonly IWorkerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWorkerProfileCommandHandler(IWorkerProfileRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WorkerProfileDto>> Handle(CreateWorkerProfileCommand request, CancellationToken cancellationToken)
    {
        var key = request.Dto.Key.Trim();
        if (await _repository.GetByKeyAsync(key, cancellationToken) != null)
        {
            return Result<WorkerProfileDto>.Failure($"Worker profile with key '{key}' already exists");
        }

        var config = WorkerProfileJson.Parse(request.Dto.DefaultConfigJson, out var error);
        if (config == null)
        {
            return Result<WorkerProfileDto>.Failure(error!);
        }

        var profile = new WorkerProfile(
            Guid.NewGuid(), key, request.Dto.DisplayName.Trim(), request.Dto.Description?.Trim(),
            config, request.Dto.IsVisible, request.Dto.SortOrder, WorkerProfileSource.Manual);

        await _repository.AddAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<WorkerProfileDto>.Success(WorkerProfileMapper.ToDto(profile));
    }
}

public class UpdateWorkerProfileCommandHandler : IRequestHandler<UpdateWorkerProfileCommand, Result<WorkerProfileDto>>
{
    private readonly IWorkerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWorkerProfileCommandHandler(IWorkerProfileRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WorkerProfileDto>> Handle(UpdateWorkerProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (profile == null)
        {
            return Result<WorkerProfileDto>.Failure($"Worker profile {request.Id} not found");
        }

        var config = WorkerProfileJson.Parse(request.Dto.DefaultConfigJson, out var error);
        if (config == null)
        {
            return Result<WorkerProfileDto>.Failure(error!);
        }

        profile.Update(request.Dto.DisplayName.Trim(), request.Dto.Description?.Trim(), config, request.Dto.IsVisible, request.Dto.SortOrder);
        await _repository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<WorkerProfileDto>.Success(WorkerProfileMapper.ToDto(profile));
    }
}

public class DeleteWorkerProfileCommandHandler : IRequestHandler<DeleteWorkerProfileCommand, Result<bool>>
{
    private readonly IWorkerProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWorkerProfileCommandHandler(IWorkerProfileRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteWorkerProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (profile == null)
        {
            return Result<bool>.Failure($"Worker profile {request.Id} not found");
        }
        if (profile.Source == WorkerProfileSource.Synced)
        {
            return Result<bool>.Failure("Synced worker profiles cannot be deleted — hide them instead");
        }

        await _repository.DeleteAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}

/// <summary>Parsowanie domyślnej konfiguracji: musi być obiektem JSON.</summary>
internal static class WorkerProfileJson
{
    public static JsonDocument? Parse(string? json, out string? error)
    {
        error = null;
        var text = string.IsNullOrWhiteSpace(json) ? "{}" : json;
        try
        {
            var document = JsonDocument.Parse(text);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                document.Dispose();
                error = "DefaultConfigJson must be a JSON object";
                return null;
            }
            return document;
        }
        catch (JsonException ex)
        {
            error = $"DefaultConfigJson is not valid JSON: {ex.Message}";
            return null;
        }
    }
}
