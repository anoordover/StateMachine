using VbvvDemo.Entities;
using VbvvDemo.FileQueues;
using VbvvDemo.Inventarisaties;

namespace VbvvDemo.Crediteringen;

public class CrediteringVerwerktEvent : IFileEvent
{
    public Guid DossierId { get; set; }
    public Guid FileId { get; set; }
    public FileType FileType { get; set; }

}
