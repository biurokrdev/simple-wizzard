using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Domain.Repositories;
using MediatR;

namespace D2ApiCreator.Application.Features.Workers.Commands.DeleteWorker;

/// <summary>
/// Handler for deleting a worker
/// </summary>
public class DeleteWorkerCommandHandler : IRequestHandler<DeleteWorkerCommand, bool>
{
    private readonly IWorkerRepository _workerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWorkerCommandHandler(
        IWorkerRepository workerRepository,
        IUnitOfWork unitOfWork)
    {
        _workerRepository = workerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteWorkerCommand request, CancellationToken cancellationToken)
    {
        var worker = await _workerRepository.GetByIdAsync(request.Id, cancellationToken);
        if (worker == null)
        {
            return false;
        }

        await _workerRepository.DeleteAsync(worker, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
