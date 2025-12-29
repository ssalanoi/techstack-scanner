using Api.Data;
using Api.Data.Entities;
using Api.Models;
using Api.Models.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ScanController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly QueueService _queue;
    private readonly OutdatedDependencyService _outdatedService;

    public ScanController(AppDbContext db, QueueService queue, OutdatedDependencyService outdatedService)
    {
        _db = db;
        _queue = queue;
        _outdatedService = outdatedService;
    }

    [HttpPost]
    public async Task<IActionResult> StartScan([FromBody] StartScanRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!Directory.Exists(request.Path))
        {
            return BadRequest("Path does not exist.");
        }

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Path == request.Path, cancellationToken);
        if (project == null)
        {
            project = new Project
            {
                Id = Guid.NewGuid(),
                Name = request.ProjectName.Trim(),
                Path = request.Path.Trim(),
                LastScannedAt = DateTime.UtcNow
            };
            await _db.Projects.AddAsync(project, cancellationToken);
        }
        else
        {
            project.Name = request.ProjectName.Trim();
            project.LastScannedAt = DateTime.UtcNow;
            _db.Projects.Update(project);
        }

        var scan = new Scan
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Status = ScanStatus.Pending,
            StartedAt = DateTime.UtcNow
        };

        await _db.Scans.AddAsync(scan, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var queueRequest = new ScanRequest
        {
            ProjectId = project.Id,
            ScanId = scan.Id,
            Path = request.Path,
            Attempt = 0
        };

        await _queue.EnqueueAsync(queueRequest, cancellationToken);

        var response = new ScanStatusResponse
        {
            ScanId = scan.Id,
            Status = scan.Status,
            StartedAt = scan.StartedAt,
            FinishedAt = scan.FinishedAt,
            Progress = 0
        };

        return Accepted(response);
    }

    [HttpGet("{scanId:guid}/status")]
    public async Task<ActionResult<ScanStatusResponse>> GetStatus([FromRoute] Guid scanId, CancellationToken cancellationToken)
    {
        var scan = await _db.Scans.FirstOrDefaultAsync(s => s.Id == scanId, cancellationToken);
        if (scan == null)
        {
            return NotFound();
        }

        var response = new ScanStatusResponse
        {
            ScanId = scan.Id,
            Status = scan.Status,
            StartedAt = scan.StartedAt,
            FinishedAt = scan.FinishedAt,
            Progress = scan.Status == ScanStatus.Completed ? 1 : null,
            Error = scan.Status == ScanStatus.Failed ? "Scan failed" : null
        };

        return Ok(response);
    }

    [HttpPost("{scanId:guid}/check-outdated")]
    public async Task<IActionResult> CheckOutdated([FromRoute] Guid scanId, CancellationToken cancellationToken)
    {
        var findings = await _db.TechnologyFindings
            .Where(tf => tf.ScanId == scanId)
            .ToListAsync(cancellationToken);

        if (findings.Count == 0)
        {
            return NotFound("No findings for this scan.");
        }

        await _outdatedService.CheckAndUpdateOutdatedAsync(findings, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var outdatedCount = findings.Count(f => f.IsOutdated);
        return Ok(new { message = $"Checked {findings.Count} dependencies, {outdatedCount} are outdated." });
    }
}
