using MassTransit;
using Microsoft.EntityFrameworkCore;
using VbvvDemo.Data;
using VbvvDemo.Entities;
using VbvvDemo.Inventarisaties;

namespace VbvvDemo.FileQueues;

public class UpdateToProcessedFileQueueItemActivity<T> : IStateMachineActivity<DossierState, T> where T : class, IFileEvent
{
    private readonly VbvvDbContext _dbContext;
    private readonly ILogger<UpdateToProcessedFileQueueItemActivity<T>> _logger;

    public UpdateToProcessedFileQueueItemActivity(
        VbvvDbContext dbContext,
        ILogger<UpdateToProcessedFileQueueItemActivity<T>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("set-processed-file-queue-item");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task Execute(BehaviorContext<DossierState, T> context, IBehavior<DossierState, T> next)
    {
        if (context.Message.FileId is { } fileId)
        {
            var fileQueueItem = await _dbContext.FileQueueItems.SingleAsync(x => x.FileId == fileId);
            fileQueueItem.Status = FileQueueStatus.Processed;

            await _dbContext.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "FileQueueItem created for dossier {DossierId} and file {FileId}",
                context.Message.DossierId,
                fileId);
        }

        await next.Execute(context).ConfigureAwait(false);
    }
    

    public Task Faulted<TException>(BehaviorExceptionContext<DossierState, T, TException> context, IBehavior<DossierState, T> next) where TException : Exception
    {
        throw new NotImplementedException();
    }
}