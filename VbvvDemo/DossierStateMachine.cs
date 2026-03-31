using MassTransit;
using VbvvDemo.Crediteringen;
using VbvvDemo.Entities;
using VbvvDemo.FileQueues;
using VbvvDemo.Inventarisaties;

namespace VbvvDemo;

public class DossierStateMachine : MassTransitStateMachine<DossierState>
{
    private readonly ILogger<DossierStateMachine> _logger;

    public State InventarisatieOntvangen { get; set; }
    public State InventarisatieVastgelegd { get; set; }
    public State WachtOpBestand { get; set; }
    public State CrediteringInVerwerking { get; set; }

    public DossierStateMachine(ILogger<DossierStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        Event(() => InventarisatieOntvangenEvent,
            configurator => configurator.CorrelateById(context => context.Message.DossierId));

        Event(() => InventarisatieVastgelegdEvent,
            configurator => configurator.CorrelateById(context => context.Message.DossierId));

        Event(() => OntvangstbevestigingInventarisatieVerstuurdEvent,
            configurator => configurator.CorrelateById(context => context.Message.DossierId));

        Event(() => CrediteringOntvangenEvent,
            configurator =>
            {
                configurator.CorrelateById(context => context.Message.DossierId);
                configurator.OnMissingInstance(m => m.Discard());
            });

        Event(() => CrediteringVerwerktEvent,
            configurator => configurator.CorrelateById(context => context.Message.DossierId));

        Event(() => StartVolgendeBestandEvent,
            configurator => configurator.CorrelateById(context => context.Message.DossierId));

        Initially(
            When(InventarisatieOntvangenEvent)
                .InitializeDossier()
                .Activity(x => x.OfType<CreateFileQueueItemInventarisatieActivity>())
                .Publish(context => new StartVolgendeBestandEvent
                {
                    DossierId = context.Message.DossierId
                })
                .TransitionTo(WachtOpBestand));

        During(InventarisatieOntvangen,
            When(InventarisatieVastgelegdEvent)
                .Then(context => context.Saga.LastUpdatedUtc = DateTime.UtcNow)
                .TransitionTo(InventarisatieVastgelegd),

            When(CrediteringOntvangenEvent)
                .RegisterPendingCreditering());

        During(InventarisatieVastgelegd,
            When(OntvangstbevestigingInventarisatieVerstuurdEvent)
                .MarkCurrentFileProcessed()
                .Activity(x => x.OfType<UpdateToProcessedFileQueueItemActivity<OntvangstbevestigingInventarisatieVerstuurdEvent>>())
                .Publish(context => new StartVolgendeBestandEvent
                {
                    DossierId = context.Saga.DossierId
                })
                .TransitionTo(WachtOpBestand),

            When(CrediteringOntvangenEvent)
                .RegisterPendingCreditering());

        During(WachtOpBestand,
            When(StartVolgendeBestandEvent)
                .Activity(x => x.OfType<ClaimNextPendingFileQueueItemActivity>())
                .Activity(x => x.OfType<StartClaimedFileProcessingActivity>()),

            When(CrediteringOntvangenEvent)
                .RegisterPendingCreditering()
                .Publish(context => new StartVolgendeBestandEvent
                {
                    DossierId = context.Message.DossierId
                }),

            When(OntvangstbevestigingInventarisatieVerstuurdEvent)
                .MarkCurrentFileProcessed()
                .Activity(x => x.OfType<UpdateToProcessedFileQueueItemActivity<OntvangstbevestigingInventarisatieVerstuurdEvent>>())
                .Publish(context => new StartVolgendeBestandEvent
                {
                    DossierId = context.Saga.DossierId
                }),

            When(CrediteringVerwerktEvent)
                .MarkCurrentFileProcessed()
                .Activity(x => x.OfType<UpdateToProcessedFileQueueItemActivity<CrediteringVerwerktEvent>>())
                .Publish(context => new StartVolgendeBestandEvent
                {
                    DossierId = context.Saga.DossierId
                }));

        During(CrediteringInVerwerking,
            When(CrediteringVerwerktEvent)
                .MarkCurrentFileProcessed()
                .Activity(x => x.OfType<UpdateToProcessedFileQueueItemActivity<CrediteringVerwerktEvent>>())
                .Publish(context => new StartVolgendeBestandEvent
                {
                    DossierId = context.Saga.DossierId
                })
                .TransitionTo(WachtOpBestand));

        _logger.LogInformation("DossierStateMachine initialized");
    }

    public Event<InventarisatieOntvangenEvent> InventarisatieOntvangenEvent { get; private set; }
    public Event<InventarisatieVastgelegdEvent> InventarisatieVastgelegdEvent { get; private set; }
    public Event<OntvangstbevestigingInventarisatieVerstuurdEvent> OntvangstbevestigingInventarisatieVerstuurdEvent { get; private set; }
    public Event<CrediteringOntvangenEvent> CrediteringOntvangenEvent { get; private set; }
    public Event<CrediteringVerwerktEvent> CrediteringVerwerktEvent { get; private set; }
    public Event<StartVolgendeBestandEvent> StartVolgendeBestandEvent { get; private set; }
}

internal static class BinderHelpers
{
    public static EventActivityBinder<DossierState, InventarisatieOntvangenEvent> InitializeDossier(
        this EventActivityBinder<DossierState, InventarisatieOntvangenEvent> binder)
    {
        return binder.Then(context =>
        {
            context.Saga.CorrelationId = context.Message.DossierId;
            context.Saga.DossierId = context.Message.DossierId;
            context.Saga.LastUpdatedUtc = DateTime.UtcNow;
            context.Saga.NumberOfFiles = 1;
            context.Saga.IsProcessing = false;
            context.Saga.ActiveFileId = null;
            context.Saga.ActiveFileType = null;
        });
    }

    public static EventActivityBinder<DossierState, CrediteringOntvangenEvent> RegisterPendingCreditering(
        this EventActivityBinder<DossierState, CrediteringOntvangenEvent> binder)
    {
        return binder
            .Activity(x => x.OfType<CreatePendingCrediteringFileQueueItemActivity<CrediteringOntvangenEvent>>())
            .Then(context =>
            {
                context.Saga.NumberOfFiles++;
                context.Saga.LastUpdatedUtc = DateTime.UtcNow;
            });
    }

    public static EventActivityBinder<DossierState, OntvangstbevestigingInventarisatieVerstuurdEvent> MarkCurrentFileProcessed(
        this EventActivityBinder<DossierState, OntvangstbevestigingInventarisatieVerstuurdEvent> binder)
    {
        return binder.Then(ResetCurrentFileState);
    }

    public static EventActivityBinder<DossierState, CrediteringVerwerktEvent> MarkCurrentFileProcessed(
        this EventActivityBinder<DossierState, CrediteringVerwerktEvent> binder)
    {
        return binder.Then(ResetCurrentFileState);
    }

    private static void ResetCurrentFileState(BehaviorContext<DossierState> context)
    {
        context.Saga.ActiveFileId = null;
        context.Saga.ActiveFileType = null;
        context.Saga.IsProcessing = false;
        context.Saga.LastUpdatedUtc = DateTime.UtcNow;
    }
}