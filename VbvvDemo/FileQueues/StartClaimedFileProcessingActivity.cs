using MassTransit;
using VbvvDemo.Crediteringen;
using VbvvDemo.Entities;
using VbvvDemo.Inventarisaties;

namespace VbvvDemo.FileQueues;

public class StartClaimedFileProcessingActivity : IStateMachineActivity<DossierState, StartVolgendeBestandEvent>
{
    public void Probe(ProbeContext context)
    {
        context.CreateScope("start-claimed-file-processing");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task Execute(
        BehaviorContext<DossierState, StartVolgendeBestandEvent> context,
        IBehavior<DossierState, StartVolgendeBestandEvent> next)
    {
        if (context.Saga.ActiveFileId is null || context.Saga.ActiveFileType is null)
        {
            await next.Execute(context).ConfigureAwait(false);
            return;
        }

        var fileId = context.Saga.ActiveFileId.Value;

        switch (context.Saga.ActiveFileType)
        {
            case FileType.Inventarisatie:
                await context.Publish(new LegInventarisatieVast
                {
                    DossierId = context.Saga.DossierId,
                    FileId = fileId
                }, context.CancellationToken);
                break;

            case FileType.Creditering:
                await context.Publish(new VerwerkCreditering
                {
                    DossierId = context.Saga.DossierId,
                    FileId = fileId
                }, context.CancellationToken);
                break;

            default:
                throw new InvalidOperationException(
                    $"Onbekend FileType '{context.Saga.ActiveFileType}' voor file {fileId}");
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