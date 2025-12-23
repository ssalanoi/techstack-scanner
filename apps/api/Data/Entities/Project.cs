using System.ComponentModel.DataAnnotations;

namespace Api.Data.Entities;

public class Project
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Path { get; set; } = string.Empty;

    public DateTime LastScannedAt { get; set; }

    public string? AiInsights { get; set; }

    public ICollection<Scan> Scans { get; set; } = new List<Scan>();
}
