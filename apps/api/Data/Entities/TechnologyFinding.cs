using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Entities;

public class TechnologyFinding
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [ForeignKey(nameof(Scan))]
    public Guid ScanId { get; set; }

    public Scan? Scan { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Version { get; set; }

    [MaxLength(500)]
    public string? SourceFile { get; set; }

    [MaxLength(200)]
    public string? Detector { get; set; }

    public bool IsOutdated { get; set; }

    [MaxLength(100)]
    public string? LatestVersion { get; set; }
}
