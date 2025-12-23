namespace Api.Models.DTOs;

public class ProjectDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime LastScannedAt { get; set; }
    public string? AiInsights { get; set; }
    public IReadOnlyList<TechnologyFindingDto> TechnologyFindings { get; set; } = Array.Empty<TechnologyFindingDto>();
    public IReadOnlyList<ScanHistoryItem> ScanHistory { get; set; } = Array.Empty<ScanHistoryItem>();
}
