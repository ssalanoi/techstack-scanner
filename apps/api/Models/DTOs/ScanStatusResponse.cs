using Api.Data.Entities;

namespace Api.Models.DTOs;

public class ScanStatusResponse
{
    public Guid ScanId { get; set; }
    public ScanStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public double? Progress { get; set; }
    public string? Error { get; set; }
}
