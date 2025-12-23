using Api.Data;
using Api.Data.Entities;
using Api.Models;
using Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Services;

public class ScanWorkerService : BackgroundService
{
    private readonly QueueService _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly QueueOptions _options;
    private readonly ILogger<ScanWorkerService> _logger;
    private readonly SemaphoreSlim _throttler;

    public ScanWorkerService(QueueService queue, IServiceProvider serviceProvider, IOptions<QueueOptions>? options, ILogger<ScanWorkerService> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _options = options?.Value ?? new QueueOptions();
        _logger = logger;
        _throttler = new SemaphoreSlim(Math.Max(1, _options.MaxConcurrency));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in _queue.ReadAllAsync(stoppingToken))
        {
            await _throttler.WaitAsync(stoppingToken);
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessRequestAsync(request, stoppingToken);
                }
                finally
                {
                    _throttler.Release();
                }
            }, stoppingToken);
        }
    }

    private async Task ProcessRequestAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var scanService = scope.ServiceProvider.GetRequiredService<ScanService>();
        var llmService = scope.ServiceProvider.GetRequiredService<LlmService>();

        var scan = await db.Scans.Include(s => s.Project).Include(s => s.TechnologyFindings)
            .FirstOrDefaultAsync(s => s.Id == request.ScanId, cancellationToken);

        if (scan == null)
        {
            _logger.LogWarning("Scan {ScanId} not found; dropping request", request.ScanId);
            return;
        }

        try
        {
            scan.Status = ScanStatus.Running;
            scan.StartedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            // Clear previous findings for this scan run
            if (scan.TechnologyFindings.Any())
            {
                db.TechnologyFindings.RemoveRange(scan.TechnologyFindings);
                await db.SaveChangesAsync(cancellationToken);
            }

            var findings = await scanService.ScanAsync(request.Path, scan.Id, cancellationToken);
            await db.TechnologyFindings.AddRangeAsync(findings, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            if (scan.Project != null)
            {
                try
                {
                    var insights = await llmService.AnalyzeTechnologyStackAsync(scan.Project, findings, cancellationToken);
                    scan.Project.AiInsights = insights;
                }
                catch (Exception llmEx)
                {
                    _logger.LogWarning(llmEx, "LLM analysis failed for scan {ScanId}, continuing without insights", scan.Id);
                    scan.Project.AiInsights = "⚠️ AI insights generation failed. The scan completed successfully, but LLM analysis is currently unavailable.\n\n" +
                                             "This may be due to:\n" +
                                             "- Ollama not running or not accessible\n" +
                                             "- Model not downloaded (run: `ollama pull llama3.2`)\n" +
                                             "- Network connectivity issues\n\n" +
                                             "You can re-run the scan to try again once Ollama is ready.";
                }
            }

            scan.Status = ScanStatus.Completed;
            scan.FinishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Completed scan {ScanId} for project {ProjectId}", scan.Id, scan.ProjectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process scan {ScanId} on attempt {Attempt}", request.ScanId, request.Attempt);
            await HandleFailureAsync(request, scan, ex, cancellationToken);
        }
    }

    private async Task HandleFailureAsync(ScanRequest request, Scan scan, Exception ex, CancellationToken cancellationToken)
    {
        if (request.Attempt + 1 < _options.MaxRetries)
        {
            var retry = new ScanRequest
            {
                ProjectId = request.ProjectId,
                ScanId = request.ScanId,
                Path = request.Path,
                Attempt = request.Attempt + 1
            };

            _logger.LogWarning(ex, "Retrying scan {ScanId} (attempt {Attempt})", request.ScanId, retry.Attempt);
            await _queue.EnqueueAsync(retry, cancellationToken);
            return;
        }

        scan.Status = ScanStatus.Failed;
        scan.FinishedAt = DateTime.UtcNow;
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Scans.Attach(scan);
        db.Entry(scan).Property(s => s.Status).IsModified = true;
        db.Entry(scan).Property(s => s.FinishedAt).IsModified = true;
        await db.SaveChangesAsync(cancellationToken);
    }
}
