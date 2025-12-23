using Api.Data;
using Api.Data.Entities;
using Api.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProjectsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectListItem>>> GetProjects([FromQuery] int skip = 0, [FromQuery] int take = 50, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);

        var query = _db.Projects.Include(p => p.Scans).ThenInclude(s => s.TechnologyFindings).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Path.Contains(term));
        }

        var items = await query
            .OrderByDescending(p => p.LastScannedAt)
            .Skip(skip)
            .Take(take)
            .Select(p => new ProjectListItem
            {
                Id = p.Id,
                Name = p.Name,
                Path = p.Path,
                LastScannedAt = p.LastScannedAt,
                ScanCount = p.Scans.Count,
                TechnologyCount = p.Scans.SelectMany(s => s.TechnologyFindings).Count(),
                LastStatus = p.Scans.OrderByDescending(s => s.StartedAt).Select(s => s.Status.ToString()).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{projectId:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> GetProject(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _db.Projects
            .Include(p => p.Scans)
                .ThenInclude(s => s.TechnologyFindings)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
        {
            return NotFound();
        }

        var latestScan = project.Scans.OrderByDescending(s => s.StartedAt).FirstOrDefault();
        var findings = latestScan?.TechnologyFindings
            .Select(tf => new TechnologyFindingDto
            {
                Name = tf.Name,
                Version = tf.Version,
                SourceFile = tf.SourceFile,
                Detector = tf.Detector,
                IsOutdated = tf.IsOutdated,
                LatestVersion = tf.LatestVersion
            }).ToList() ?? new List<TechnologyFindingDto>();

        var history = project.Scans
            .OrderByDescending(s => s.StartedAt)
            .Select(s => new ScanHistoryItem
            {
                ScanId = s.Id,
                StartedAt = s.StartedAt,
                FinishedAt = s.FinishedAt,
                Status = s.Status
            }).ToList();

        var dto = new ProjectDetailDto
        {
            Id = project.Id,
            Name = project.Name,
            Path = project.Path,
            LastScannedAt = project.LastScannedAt,
            AiInsights = project.AiInsights,
            TechnologyFindings = findings,
            ScanHistory = history
        };

        return Ok(dto);
    }

    [HttpGet("{projectId:guid}/insights")]
    public async Task<ActionResult<string>> GetInsights(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(project.AiInsights))
        {
            return NotFound();
        }

        return Ok(project.AiInsights);
    }

    [HttpGet("/api/technologies")]
    public async Task<ActionResult<IEnumerable<TechnologyAggregateDto>>> GetTechnologies(CancellationToken cancellationToken)
    {
        // Load all findings into memory first to avoid complex SQL translation
        var allFindings = await _db.TechnologyFindings
            .Select(tf => new { tf.Name, tf.Version, tf.IsOutdated })
            .ToListAsync(cancellationToken);

        var aggregates = allFindings
            .GroupBy(tf => tf.Name)
            .Select(g => new TechnologyAggregateDto
            {
                Name = g.Key,
                Total = g.Count(),
                OutdatedCount = g.Count(tf => tf.IsOutdated),
                Versions = g.Where(tf => tf.Version != null)
                    .GroupBy(tf => tf.Version!)
                    .Select(v => new VersionCountDto
                    {
                        Version = v.Key,
                        Count = v.Count()
                    }).ToList()
            }).ToList();

        return Ok(aggregates);
    }

    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> DeleteProject(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _db.Projects
            .Include(p => p.Scans)
                .ThenInclude(s => s.TechnologyFindings)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
        {
            return NotFound();
        }

        var findings = project.Scans.SelectMany(s => s.TechnologyFindings).ToList();
        if (findings.Count > 0)
        {
            _db.TechnologyFindings.RemoveRange(findings);
        }

        if (project.Scans.Count > 0)
        {
            _db.Scans.RemoveRange(project.Scans);
        }

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
