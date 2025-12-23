using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Api.Data.Entities;
using Api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Services;

public class LlmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LlmService> _logger;
    private readonly LlmOptions _options;
    private const int MaxAttempts = 3;

    public LlmService(HttpClient httpClient, ILogger<LlmService> logger, IOptions<LlmOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value ?? new LlmOptions();
    }

    public async Task<string> AnalyzeTechnologyStackAsync(Project project, IEnumerable<TechnologyFinding> findings, CancellationToken cancellationToken = default)
    {
        var findingsList = findings.ToList();
        if (findingsList.Count == 0)
        {
            return "No findings were detected for this project.";
        }

        var host = Environment.GetEnvironmentVariable("OLLAMA_HOST");
        if (!string.IsNullOrWhiteSpace(host) && Uri.TryCreate(host, UriKind.Absolute, out var parsedHost))
        {
            if (_httpClient.BaseAddress == null || !_httpClient.BaseAddress.Equals(parsedHost))
            {
                _httpClient.BaseAddress = parsedHost;
            }
        }

        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? _options.Model;
        var prompt = BuildPrompt(project, findingsList);
        var request = new OllamaRequest
        {
            Model = model,
            Prompt = prompt,
            Stream = false,
            Options = new OllamaRequestOptions
            {
                NumPredict = Math.Max(128, _options.MaxTokens)
            }
        };

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync("/api/generate", request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var payload = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: cancellationToken);
                    var content = payload?.Response?.Trim();
                    return string.IsNullOrWhiteSpace(content)
                        ? "LLM returned an empty response."
                        : content!;
                }

                _logger.LogWarning("Ollama request failed with status {StatusCode} on attempt {Attempt}", response.StatusCode, attempt);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ollama request failed on attempt {Attempt}", attempt);
            }

            if (attempt < MaxAttempts)
            {
                var backoffSeconds = Math.Pow(2, attempt - 1);
                await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), cancellationToken);
            }
        }

        return "LLM analysis is currently unavailable. Please try again later.";
    }

    private static string BuildPrompt(Project project, IReadOnlyCollection<TechnologyFinding> findings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an experienced software architect analyzing a project's technology stack.");
        sb.AppendLine("Provide markdown with the following sections:");
        sb.AppendLine("1) Outdated dependencies (name, version, recommendation)");
        sb.AppendLine("2) Security concerns (brief reason)");
        sb.AppendLine("3) Compatibility issues between technologies");
        sb.AppendLine("4) Recommended upgrade paths (prioritized)");
        sb.AppendLine("5) Overall health score from 1-10 with rationale");
        sb.AppendLine();
        sb.AppendLine($"Project: {project.Name}");
        sb.AppendLine($"Path: {project.Path}");
        sb.AppendLine("Detected technologies (JSON lines):");

        foreach (var finding in findings)
        {
            sb.AppendLine($"- {{ \"name\": \"{finding.Name}\", \"version\": \"{finding.Version ?? "unknown"}\", \"detector\": \"{finding.Detector ?? "unknown"}\", \"source\": \"{finding.SourceFile ?? "unknown"}\" }}");
        }

        sb.AppendLine();
        sb.AppendLine("Emphasize concise, actionable insights.");
        return sb.ToString();
    }

    private sealed class OllamaRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("options")]
        public OllamaRequestOptions Options { get; set; } = new();
    }

    private sealed class OllamaRequestOptions
    {
        [JsonPropertyName("num_predict")]
        public int NumPredict { get; set; }
    }

    private sealed class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}
