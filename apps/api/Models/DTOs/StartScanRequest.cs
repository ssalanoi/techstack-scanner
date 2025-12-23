using System.ComponentModel.DataAnnotations;

namespace Api.Models.DTOs;

public class StartScanRequest
{
    [Required]
    [MaxLength(200)]
    public string ProjectName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Path { get; set; } = string.Empty;
}
