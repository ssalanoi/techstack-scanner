namespace Api.Models.DTOs;

public class ProjectListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime LastScannedAt { get; set; }
    public int ScanCount { get; set; }
    public int TechnologyCount { get; set; }
    public string? LastStatus { get; set; }
}
