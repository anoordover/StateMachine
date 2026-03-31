using MassTransit;
using Microsoft.EntityFrameworkCore;
using VbvvDemo.Data;
using VbvvDemo.Entities;
using VbvvDemo.Inventarisaties;

namespace VbvvDemo.FileQueues;

public class CreateFileQueueItemInventarisatieActivity : IStateMachineActivity<DossierState, InventarisatieOntvangenEvent>
{
    private readonly VbvvDbContext _dbContext;
    private readonly ILogger<CreateFileQueueItemInventarisatieActivity> _logger;

    public CreateFileQueueItemInventarisatieActivity(
        VbvvDbContext dbContext,
        ILogger<CreateFileQueueItemInventarisatieActivity> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("create-pending-inventarisatie-file-queue-item");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task Execute(
        BehaviorContext<DossierState, InventarisatieOntvangenEvent> context,
        IBehavior<DossierState, InventarisatieOntvangenEvent> next)
    {
        var fileId = context.Message.FileId;

        var exists = await _dbContext.FileQueueItems.AnyAsync(x => x.FileId == fileId, context.CancellationToken);
        if (!exists)
        {
            _dbContext.FileQueueItems.Add(new FileQueueItem
            {
                DossierId = context.Message.DossierId,
                FileId = fileId,
                FileType = FileType.Inventarisatie,
                ReceivedAtUtc = DateTime.UtcNow,
                Status = FileQueueStatus.Pending
            });

            await _dbContext.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Pending inventarisatie FileQueueItem created for dossier {DossierId} and file {FileId}",
                context.Message.DossierId,
                fileId);
        }

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<DossierState, InventarisatieOntvangenEvent, TException> context,
        IBehavior<DossierState, InventarisatieOntvangenEvent> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }
}