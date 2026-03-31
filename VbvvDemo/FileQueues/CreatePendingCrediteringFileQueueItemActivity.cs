using MassTransit;
using VbvvDemo.Data;
using VbvvDemo.Entities;
using VbvvDemo.Inventarisaties;

namespace VbvvDemo.FileQueues;

public class CreatePendingCrediteringFileQueueItemActivity<T>
    : IStateMachineActivity<DossierState, T> where T : class, IFileEvent
{
    private readonly VbvvDbContext _dbContext;
    private readonly ILogger<CreatePendingCrediteringFileQueueItemActivity<T>> _logger;

    public CreatePendingCrediteringFileQueueItemActivity(
        VbvvDbContext dbContext,
        ILogger<CreatePendingCrediteringFileQueueItemActivity<T>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("create-pending-creditering-file-queue-item");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task Execute(
        BehaviorContext<DossierState, T> context,
        IBehavior<DossierState, T> next)
    {
        var fileId = context.Message.FileId;

        var exists = _dbContext.FileQueueItems.Any(x => x.FileId == fileId);
        if (!exists)
        {
            _dbContext.FileQueueItems.Add(new FileQueueItem
            {
                DossierId = context.Message.DossierId,
                FileId = fileId,
                FileType = context.Message.FileType,
                ReceivedAtUtc = DateTime.UtcNow,
                Status = FileQueueStatus.Pending
            });

            await _dbContext.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Pending FileQueueItem created for dossier {DossierId} and file {FileId}",
                context.Message.DossierId,
                fileId);
        }

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<DossierState, T, TException> context,
        IBehavior<DossierState, T> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }
}