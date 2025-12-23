using System.Net;
using Api.Data.Entities;
using Api.Options;
using Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;
using Xunit;

namespace Api.Tests;

public class LlmServiceTests
{
    [Fact]
    public async Task AnalyzeTechnologyStackAsync_ReturnsContentOnSuccess()
    {
        var (service, handler) = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"response\":\"Insight text\"}")
        });

        var project = new Project { Id = Guid.NewGuid(), Name = "Test", Path = "c:/projects/test" };
        var findings = new[]
        {
            new TechnologyFinding { Name = "react", Version = "18.2.0", Detector = "npm", SourceFile = "package.json" }
        };

        var result = await service.AnalyzeTechnologyStackAsync(project, findings);

        Assert.Equal("Insight text", result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task AnalyzeTechnologyStackAsync_RetriesAndReturnsWhenSuccessful()
    {
        var (service, handler) = CreateService(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"response\":\"Recovered\"}")
            });

        var project = new Project { Id = Guid.NewGuid(), Name = "Retry", Path = "c:/projects/retry" };
        var findings = new[]
        {
            new TechnologyFinding { Name = "aspnet", Version = "9.0.0", Detector = "nuget", SourceFile = "api.csproj" }
        };

        var result = await service.AnalyzeTechnologyStackAsync(project, findings);

        Assert.Equal("Recovered", result);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task AnalyzeTechnologyStackAsync_ReturnsFallbackAfterFailures()
    {
        var (service, handler) = CreateService(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var project = new Project { Id = Guid.NewGuid(), Name = "Failing", Path = "c:/projects/failing" };
        var findings = new[]
        {
            new TechnologyFinding { Name = "python", Version = "3.11", Detector = "runtime", SourceFile = "runtime" }
        };

        var result = await service.AnalyzeTechnologyStackAsync(project, findings);

        Assert.Equal("LLM analysis is currently unavailable. Please try again later.", result);
        Assert.Equal(3, handler.CallCount);
    }

    private static (LlmService service, StubHttpMessageHandler handler) CreateService(params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
    {
        var handler = new StubHttpMessageHandler(responses);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434"),
            Timeout = TimeSpan.FromSeconds(5)
        };

        var options = MsOptions.Create(new LlmOptions
        {
            Model = "llama3.2",
            Host = "http://localhost:11434",
            TimeoutSeconds = 5,
            MaxTokens = 256
        });

        var service = new LlmService(client, NullLogger<LlmService>.Instance, options);
        return (service, handler);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses;

        public int CallCount { get; private set; }

        public StubHttpMessageHandler(IEnumerable<Func<HttpRequestMessage, HttpResponseMessage>> responses)
        {
            _responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (_responses.Count > 0)
            {
                return Task.FromResult(_responses.Dequeue().Invoke(request));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }
}
