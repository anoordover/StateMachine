using MassTransit;

namespace VbvvDemo.Crediteringen;

public class VerwerkCrediteringConsumer : IConsumer<VerwerkCreditering>
{
    public async Task Consume(ConsumeContext<VerwerkCreditering> context)
    {
        await Task.Delay(5000, context.CancellationToken);

        await context.Publish(new CrediteringVerwerktEvent
        {
            DossierId = context.Message.DossierId,
            FileId = context.Message.FileId
        }, context.CancellationToken);
    }
}
