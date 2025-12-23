namespace Api.Options;

public class ScanOptions
{
    public int MaxDepth { get; set; } = 5;

    public string? AllowedRootPath { get; set; }
}
