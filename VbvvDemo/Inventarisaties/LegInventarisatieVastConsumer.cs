using MassTransit;

namespace VbvvDemo.Inventarisaties;

public class LegInventarisatieVastConsumer : IConsumer<LegInventarisatieVast>
{
    public async Task Consume(ConsumeContext<LegInventarisatieVast> context)
    {
        await Task.Delay(5000);
        await context.Publish(new InventarisatieVastgelegdEvent
        {
            DossierId = context.Message.DossierId,
            FileId = context.Message.FileId
        }, context.CancellationToken);
    }
}