namespace Api.Models.DTOs;

public class TechnologyAggregateDto
{
    public string Name { get; set; } = string.Empty;
    public int Total { get; set; }
    public int OutdatedCount { get; set; }
    public IReadOnlyList<VersionCountDto> Versions { get; set; } = Array.Empty<VersionCountDto>();
}

public class VersionCountDto
{
    public string Version { get; set; } = string.Empty;
    public int Count { get; set; }
}
