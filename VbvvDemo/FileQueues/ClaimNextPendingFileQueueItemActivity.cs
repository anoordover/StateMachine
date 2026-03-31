using MassTransit;
using Microsoft.EntityFrameworkCore;
using VbvvDemo.Data;
using VbvvDemo.Entities;

namespace VbvvDemo.FileQueues;

public class ClaimNextPendingFileQueueItemActivity : IStateMachineActivity<DossierState, StartVolgendeBestandEvent>
{
    private readonly VbvvDbContext _dbContext;
    private readonly ILogger<ClaimNextPendingFileQueueItemActivity> _logger;

    public ClaimNextPendingFileQueueItemActivity(
        VbvvDbContext dbContext,
        ILogger<ClaimNextPendingFileQueueItemActivity> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("claim-next-pending-file-queue-item");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task Execute(
        BehaviorContext<DossierState, StartVolgendeBestandEvent> context,
        IBehavior<DossierState, StartVolgendeBestandEvent> next)
    {
        var nextItem = await _dbContext.FileQueueItems
            .Where(x => x.DossierId == context.Saga.DossierId && x.Status == FileQueueStatus.Pending)
            .OrderBy(x => x.ReceivedAtUtc)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (nextItem is not null)
        {
            nextItem.Status = FileQueueStatus.Processing;
            nextItem.ClaimedAtUtc = DateTime.UtcNow;

            context.Saga.ActiveFileId = nextItem.FileId;
            context.Saga.ActiveFileType = nextItem.FileType;
            context.Saga.IsProcessing = true;
            context.Saga.LastUpdatedUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Claimed next pending file for dossier {DossierId}, file {FileId}, type {FileType}",
                context.Saga.DossierId,
                nextItem.FileId,
                nextItem.FileType);
        }
        else
        {
            context.Saga.ActiveFileId = null;
            context.Saga.ActiveFileType = null;
            context.Saga.IsProcessing = false;
            context.Saga.LastUpdatedUtc = DateTime.UtcNow;

            _logger.LogInformation("No pending files found for dossier {DossierId}", context.Saga.DossierId);
        }

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<DossierState, StartVolgendeBestandEvent, TException> context,
        IBehavior<DossierState, StartVolgendeBestandEvent> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }
}