using VbvvDemo.Entities;
using VbvvDemo.FileQueues;

namespace VbvvDemo.Inventarisaties;

public class OntvangstbevestigingInventarisatieVerstuurdEvent : IFileEvent
{
    public Guid DossierId { get; set; }
    public Guid FileId { get; set; }
    public FileType FileType { get; set; }
}