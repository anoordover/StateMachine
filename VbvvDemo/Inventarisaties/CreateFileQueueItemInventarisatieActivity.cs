using MassTransit;
using VbvvDemo.Data;
using VbvvDemo.Entities;
using VbvvDemo.FileQueues;

namespace VbvvDemo.Inventarisaties;

public class CreateFileQueueItemInventarisatieActivity2 : IStateMachineActivity<DossierState, InventarisatieOntvangenEvent>
{
    private readonly VbvvDbContext _dbContext;
    private readonly ILogger<CreateFileQueueItemInventarisatieActivity2> _logger;

    public CreateFileQueueItemInventarisatieActivity2(
        VbvvDbContext dbContext,
        ILogger<CreateFileQueueItemInventarisatieActivity2> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("create-file-queue-item");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task Execute(
        BehaviorContext<DossierState, InventarisatieOntvangenEvent> context,
        IBehavior<DossierState, InventarisatieOntvangenEvent> next)
    {
        if (context.Message.FileId is { } fileId)
        {
            _dbContext.FileQueueItems.Add(new FileQueueItem
            {
                DossierId = context.Message.DossierId,
                FileId = fileId,
                FileType = FileType.Inventarisatie,
                ReceivedAtUtc = DateTime.UtcNow,
                Status = FileQueueStatus.Processing
            });

            await _dbContext.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "FileQueueItem created for dossier {DossierId} and file {FileId}",
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