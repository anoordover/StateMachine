using MassTransit;

namespace VbvvDemo.Inventarisaties;

public class VerstuurOntvangstbevestigingInventarisatieConsumer : IConsumer<VerstuurOntvangstbevestigingInventarisatie>
{
    public async Task Consume(ConsumeContext<VerstuurOntvangstbevestigingInventarisatie> context)
    {
        await Task.Delay(5000);
        await context.Publish(new OntvangstbevestigingInventarisatieVerstuurdEvent
            {
                DossierId = context.Message.DossierId,
                FileId = context.Message.FileId
            },
            context.CancellationToken);
    }
}