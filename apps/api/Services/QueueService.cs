using System.Threading.Channels;
using Api.Models;
using Api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Services;

public class QueueService
{
    private readonly Channel<ScanRequest> _channel;
    private readonly QueueOptions _options;
    private readonly ILogger<QueueService> _logger;

    public QueueService(ILogger<QueueService> logger, IOptions<QueueOptions>? options = null)
    {
        _logger = logger;
        _options = options?.Value ?? new QueueOptions();

        var channelOptions = new BoundedChannelOptions(_options.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<ScanRequest>(channelOptions);
    }

    public ValueTask EnqueueAsync(ScanRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enqueuing scan {ScanId} for project {ProjectId} (attempt {Attempt})", request.ScanId, request.ProjectId, request.Attempt);
        return _channel.Writer.WriteAsync(request, cancellationToken);
    }

    internal IAsyncEnumerable<ScanRequest> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public void Complete(Exception? exception = null)
    {
        if (exception != null)
        {
            _channel.Writer.TryComplete(exception);
        }
        else
        {
            _channel.Writer.TryComplete();
        }
    }
}
