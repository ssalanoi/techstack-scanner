using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Entities;

public enum ScanStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

public class Scan
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [ForeignKey(nameof(Project))]
    public Guid ProjectId { get; set; }

    public Project? Project { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    [Required]
    public ScanStatus Status { get; set; }

    public ICollection<TechnologyFinding> TechnologyFindings { get; set; } = new List<TechnologyFinding>();
}
