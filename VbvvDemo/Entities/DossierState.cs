using System.ComponentModel.DataAnnotations;
using MassTransit;
using VbvvDemo.FileQueues;

namespace VbvvDemo.Entities;

public class DossierState : SagaStateMachineInstance
{
    [Key]
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;

    public Guid DossierId { get; set; }

    public bool IsProcessing { get; set; }
    public Guid? ActiveFileId { get; set; }
    public FileType? ActiveFileType { get; set; }

    public DateTime? LastUpdatedUtc { get; set; }
    public int NumberOfFiles { get; set; }
}