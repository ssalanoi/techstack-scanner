namespace Api.Models.DTOs;

public class TechnologyFindingDto
{
    public string Name { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? SourceFile { get; set; }
    public string? Detector { get; set; }
    public bool IsOutdated { get; set; }
    public string? LatestVersion { get; set; }
}
