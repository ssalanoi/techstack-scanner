using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Api.Data.Entities;
using Microsoft.Extensions.Logging;

namespace Api.Services;

public class OutdatedDependencyService
{
    private readonly ILogger<OutdatedDependencyService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    // Regex to extract semantic version numbers
    private static readonly Regex VersionRegex = new(
        @"(\d+)\.(\d+)(?:\.(\d+))?",
        RegexOptions.Compiled);

    public OutdatedDependencyService(
        ILogger<OutdatedDependencyService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> CheckAndUpdateOutdatedAsync(
        IEnumerable<TechnologyFinding> findings,
        CancellationToken cancellationToken = default)
    {
        var tasks = findings.Select(f => CheckOutdatedAsync(f, cancellationToken));
        await Task.WhenAll(tasks);
        return true;
    }

    private async Task CheckOutdatedAsync(TechnologyFinding finding, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(finding.Version))
        {
            return;
        }

        try
        {
            var latestVersion = finding.Detector?.ToLowerInvariant() switch
            {
                "npm" => await GetLatestNpmVersionAsync(finding.Name, cancellationToken),
                "nuget" => await GetLatestNuGetVersionAsync(finding.Name, cancellationToken),
                "pip" => await GetLatestPyPiVersionAsync(finding.Name, cancellationToken),
                "gem" => await GetLatestRubyGemsVersionAsync(finding.Name, cancellationToken),
                _ => null
            };

            if (latestVersion != null)
            {
                finding.LatestVersion = latestVersion;
                finding.IsOutdated = IsVersionOutdated(finding.Version, latestVersion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check outdated status for {Name} ({Detector})", 
                finding.Name, finding.Detector);
        }
    }

    private async Task<string?> GetLatestNpmVersionAsync(string packageName, CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = Timeout;
            
            var response = await client.GetAsync(
                $"https://registry.npmjs.org/{Uri.EscapeDataString(packageName)}/latest",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (data.TryGetProperty("version", out var versionProp))
            {
                return versionProp.GetString();
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(ex, "Failed to fetch npm package {PackageName}", packageName);
        }

        return null;
    }

    private async Task<string?> GetLatestNuGetVersionAsync(string packageName, CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = Timeout;

            var response = await client.GetAsync(
                $"https://api.nuget.org/v3-flatcontainer/{packageName.ToLowerInvariant()}/index.json",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (data.TryGetProperty("versions", out var versions) && versions.ValueKind == JsonValueKind.Array)
            {
                var versionList = versions.EnumerateArray()
                    .Select(v => v.GetString())
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToList();

                return versionList.LastOrDefault();
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(ex, "Failed to fetch NuGet package {PackageName}", packageName);
        }

        return null;
    }

    private async Task<string?> GetLatestPyPiVersionAsync(string packageName, CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = Timeout;

            var response = await client.GetAsync(
                $"https://pypi.org/pypi/{Uri.EscapeDataString(packageName)}/json",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (data.TryGetProperty("info", out var info) && 
                info.TryGetProperty("version", out var versionProp))
            {
                return versionProp.GetString();
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(ex, "Failed to fetch PyPI package {PackageName}", packageName);
        }

        return null;
    }

    private async Task<string?> GetLatestRubyGemsVersionAsync(string gemName, CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = Timeout;

            var response = await client.GetAsync(
                $"https://rubygems.org/api/v1/versions/{Uri.EscapeDataString(gemName)}/latest.json",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (data.TryGetProperty("version", out var versionProp))
            {
                return versionProp.GetString();
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(ex, "Failed to fetch RubyGems gem {GemName}", gemName);
        }

        return null;
    }

    internal bool IsVersionOutdated(string currentVersion, string latestVersion)
    {
        // Clean up version strings (remove operators, etc.)
        var currentClean = CleanVersion(currentVersion);
        var latestClean = CleanVersion(latestVersion);

        var currentMatch = VersionRegex.Match(currentClean);
        var latestMatch = VersionRegex.Match(latestClean);

        if (!currentMatch.Success || !latestMatch.Success)
        {
            // Fallback to string comparison
            return !string.Equals(currentClean, latestClean, StringComparison.OrdinalIgnoreCase);
        }

        var currentMajor = int.Parse(currentMatch.Groups[1].Value);
        var currentMinor = int.Parse(currentMatch.Groups[2].Value);
        var currentPatch = currentMatch.Groups[3].Success ? int.Parse(currentMatch.Groups[3].Value) : 0;

        var latestMajor = int.Parse(latestMatch.Groups[1].Value);
        var latestMinor = int.Parse(latestMatch.Groups[2].Value);
        var latestPatch = latestMatch.Groups[3].Success ? int.Parse(latestMatch.Groups[3].Value) : 0;

        if (latestMajor > currentMajor) return true;
        if (latestMajor < currentMajor) return false;
        
        if (latestMinor > currentMinor) return true;
        if (latestMinor < currentMinor) return false;
        
        return latestPatch > currentPatch;
    }

    private static string CleanVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return string.Empty;
        }

        // Remove common version operators and prefixes
        var cleaned = version
            .Replace("^", "")
            .Replace("~", "")
            .Replace(">=", "")
            .Replace("<=", "")
            .Replace("==", "")
            .Replace(">", "")
            .Replace("<", "")
            .Replace("=", "")
            .Replace("v", "")
            .Trim();

        return cleaned;
    }
}
