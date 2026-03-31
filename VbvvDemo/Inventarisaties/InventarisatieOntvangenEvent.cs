using VbvvDemo.Entities;
using VbvvDemo.FileQueues;

namespace VbvvDemo.Inventarisaties;

public record InventarisatieOntvangenEvent : IFileEvent
{
    public Guid DossierId { get; set; }
    public Guid FileId { get; set; }
    public FileType FileType { get; set; }
}