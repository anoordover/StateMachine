using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VbvvDemo.FileQueues;

namespace VbvvDemo.Entities;

public class FileQueueItem
{
    public long Id { get; set; }
    public Guid DossierId { get; set; }
    public Guid FileId { get; set; }
    public FileType FileType { get; set; }
    public DateTime ReceivedAtUtc { get; set; }

    public FileQueueStatus Status { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }

    [Timestamp]
    [Column("xmin")]
    public uint RowVersion { get; set; }
}