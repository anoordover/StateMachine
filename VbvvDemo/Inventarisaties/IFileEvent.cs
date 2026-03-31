using VbvvDemo.Entities;
using VbvvDemo.FileQueues;

namespace VbvvDemo.Inventarisaties;

public interface IFileEvent
{
    Guid DossierId { get; set; }
    Guid FileId { get; set; }
    FileType FileType { get; set; }
}