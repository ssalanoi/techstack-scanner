using Api.Data.Entities;

namespace Api.Models.DTOs;

public class ScanHistoryItem
{
    public Guid ScanId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public ScanStatus Status { get; set; }
}
