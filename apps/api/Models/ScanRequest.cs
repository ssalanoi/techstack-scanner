namespace Api.Models;

public class ScanRequest
{
    public Guid ProjectId { get; set; }

    public Guid ScanId { get; set; }

    public string Path { get; set; } = string.Empty;

    public int Attempt { get; set; } = 0;
}
