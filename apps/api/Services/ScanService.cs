using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Api.Data.Entities;
using Api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Services;

public class ScanService
{
    private static readonly HashSet<string> SupportedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "package.json",
        "global.json",
        "requirements.txt",
        "pyproject.toml",
        "Gemfile",
        "Gemfile.lock",
        "go.mod",
        "go.sum",
        "pom.xml",
        "build.gradle",
        "Dockerfile",
        "docker-compose.yml"
    };

    private static readonly Regex RequirementRegex = new(
        @"^(?<name>[A-Za-z0-9._-]+)\s*(?<op>==|>=|<=|~=|>|<)?\s*(?<version>[A-Za-z0-9.*+_-]+)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex GradleCoordinateRegex = new(
        @"(?<group>[A-Za-z0-9_.-]+):(?<artifact>[A-Za-z0-9_.-]+):(?<version>[A-Za-z0-9_.-]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex GemfileLineRegex = new(
        "^\\s*gem\\s+['\\\"]?(?<name>[^'\\\",\\s]+)['\\\"]?\\s*(,\\s*['\\\"]?(?<version>[^'\\\"]+)['\\\"]?)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private static readonly Regex GemfileLockRegex = new(
        @"^\s{2,}(?<name>[A-Za-z0-9._-]+)\s\((?<version>[^)]+)\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly Regex DockerImageRegex = new(
        @"^FROM\s+(?<image>[^\s]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly Regex ComposeImageRegex = new(
        @"image:\s*(?<image>[^\s#]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly ILogger<ScanService> _logger;
    private readonly ScanOptions _options;

    public ScanService(ILogger<ScanService> logger, IOptions<ScanOptions>? options = null)
    {
        _logger = logger;
        _options = options?.Value ?? new ScanOptions();
    }

    public Task<IReadOnlyList<TechnologyFinding>> ScanAsync(string path, Guid? scanId = null, CancellationToken cancellationToken = default)
    {
        var normalizedRoot = NormalizeAndValidatePath(path);
        var findings = new List<TechnologyFinding>();

        foreach (var file in EnumerateFiles(normalizedRoot, 0, cancellationToken))
        {
            var fileName = Path.GetFileName(file);
            if (!IsSupportedFile(fileName, file))
            {
                continue;
            }

            string content;
            try
            {
                content = File.ReadAllText(file);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to read file {File}", file);
                continue;
            }

            var relative = Path.GetRelativePath(normalizedRoot, file);
            var parsed = ParseFile(fileName, Path.GetExtension(file), content, relative, scanId ?? Guid.Empty);
            findings.AddRange(parsed);
        }

        return Task.FromResult<IReadOnlyList<TechnologyFinding>>(findings);
    }

    internal IReadOnlyList<TechnologyFinding> ParsePackageJson(string content, string sourceFile, Guid scanId)
    {
        var results = new List<TechnologyFinding>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return results;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var dependencySections = new[] { "dependencies", "devDependencies", "peerDependencies", "optionalDependencies" };

            foreach (var section in dependencySections)
            {
                if (!root.TryGetProperty(section, out var dependencies) || dependencies.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var property in dependencies.EnumerateObject())
                {
                    var version = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
                    results.Add(CreateFinding(property.Name, version, sourceFile, "npm", scanId));
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid package.json format in {SourceFile}", sourceFile);
        }

        return results;
    }

    internal IReadOnlyList<TechnologyFinding> ParseCsProj(string content, string sourceFile, Guid scanId)
    {
        var results = new List<TechnologyFinding>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return results;
        }

        try
        {
            var doc = XDocument.Parse(content);
            var packageReferences = doc.Descendants().Where(e => e.Name.LocalName == "PackageReference");

            foreach (var reference in packageReferences)
            {
                var name = reference.Attribute("Include")?.Value ?? reference.Attribute("Update")?.Value;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var version = reference.Attribute("Version")?.Value
                              ?? reference.Elements().FirstOrDefault(e => e.Name.LocalName == "Version")?.Value;
                results.Add(CreateFinding(name, version, sourceFile, "nuget", scanId));
            }
        }
        catch (Exception ex) when (ex is XmlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Invalid csproj format in {SourceFile}", sourceFile);
        }

        return results;
    }

    internal IReadOnlyList<TechnologyFinding> ParseGlobalJson(string content, string sourceFile, Guid scanId)
    {
        var results = new List<TechnologyFinding>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return results;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("sdk", out var sdk) && sdk.TryGetProperty("version", out var versionProp))
            {
                var version = versionProp.GetString();
                results.Add(CreateFinding(".NET SDK", version, sourceFile, "global.json", scanId));
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid global.json format in {SourceFile}", sourceFile);
        }

        return results;
    }

    internal IReadOnlyList<TechnologyFinding> ParseRequirementsTxt(string content, string sourceFile, Guid scanId)
    {
        var results = new List<TechnologyFinding>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return results;
        }

        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            var finding = ParsePythonRequirement(trimmed, sourceFile, "pip", scanId);
            if (finding != null)
            {
                results.Add(finding);
            }
        }

        return results;
    }

    internal IReadOnlyList<TechnologyFinding> ParsePyProjectToml(string content, string sourceFile, Guid scanId)
    {
        var results = new List<TechnologyFinding>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return results;
        }

        bool inPoetryDependencies = false;
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("["))
            {
                inPoetryDependencies = line.Equals("[tool.poetry.dependencies]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (line.StartsWith("dependencies", StringComparison.OrdinalIgnoreCase) && line.Contains('='))
            {
                var depsBlock = ExtractBracketBlock(content, "dependencies");
                foreach (var dependency in depsBlock)
                {
                    var finding = ParsePythonRequirement(dependency, sourceFile, "pyproject", scanId);
                    if (finding != null)
                    {
                        results.Add(finding);
                    }
                }
                continue;
            }

            if (inPoetryDependencies && line.Contains('='))
            {
                var parts = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var name = parts[0].Trim().Trim('"', '\'');
                    var version = parts[1].Trim().Trim('"', '\'');
                    var finding = CreateFinding(name, string.IsNullOrWhiteSpace(version) ? null : version, sourceFile, "pyproject", scanId);
                    results.Add(finding);
                }
            }
        }

        return results;
    }

    internal IReadOnlyList<TechnologyFinding> ParseGemfile(string content, string sourceFile, Guid scanId)
    {
        var results = new List<TechnologyFinding>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return results;
        }

        foreach (Match match in GemfileLineRegex.Matches(content))
        {
            var name = match.Groups["name"].Value;
            var version = match.Groups["version"].Success ? match.Groups["version"].Value : null;
            results.Add(CreateFinding(name, string.IsNullOrWhiteSpace(version) ? null : version, sourceFile, "gemfile", scanId));
        }

        return results;
    }

    internal IReadOnlyList<TechnologyFinding> ParseGemfileLock(string content, string sourceFile, Guid scanId)
    {
        var results = new List<TechnologyFinding>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return results;
        }

        foreach (Match match in GemfileLockRegex.Matches(content))
        {
            var name = match.Groups["name"].Value;
            var version = match.Groups["version"].Value;
            results.Add(CreateFinding(name, version, sourceFile, "gemfile.lock", scanId));
        }

        return results;
    }

    internal IReadOnlyList<TechnologyFinding> ParseGoMod(string content, string sourceFile, Guid scanId)
    {
        var results = new List<TechnologyFinding>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return results;
        }

        var lines = content.Split('\n').Select(l => l.Trim());
        var inRequireBlock = false;
        foreach (var line in lines)
        {
            if (line.StartsWith("require (", StringComparison.OrdinalIgnoreCase))
            {
                inRequireBlock = true;
                continue;
            }

            if (inRequireBlock && line.StartsWith(")"))
            {
                inRequireBlock = false;
                continue;
            }

            if (!inRequireBlock && !line.StartsWith("require ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var trimmed = line.Replace("require", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var name = parts[0];
                var version = parts[1];
                results.Add(CreateFinding(name, version, sourceFile, "go.mod", scanId));
            }
        }

        return results;
    }

    internal IReadOnlyList<TechnologyFinding> ParseGoSum(string content, string sourceFile, Guid scanId)
    {
        var results = new List<TechnologyFinding>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return results;
        }

        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var name = parts[0];
                var version = parts[1];
                results.Add(CreateFinding(name, version, sourceFile, "go.sum", scanId));
            }
        }

        return results;
    }

    internal IReadOnlyList<TechnologyFinding> ParsePomXml(string content, string sourceFile, Guid scanId)
    {
        var results = new List<TechnologyFinding>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return results;
        }

        try
        {
            var doc = XDocument.Parse(content);
            var dependencies = doc.Descendants().Where(e => e.Name.LocalName == "dependency");
            foreach (var dependency in dependencies)
            {
                var groupId = dependency.Elements().FirstOrDefault(e => e.Name.LocalName == "groupId")?.Value;
                var artifactId = dependency.Elements().FirstOrDefault(e => e.Name.LocalName == "artifactId")?.Value;
                var version = dependency.Elements().FirstOrDefault(e => e.Name.LocalName == "version")?.Value;

                if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(artifactId))
                {
                    continue;
                }

                var name = $"{groupId}:{artifactId}";
                results.Add(CreateFinding(name, version, sourceFile, "maven", scanId));
            }
        }
        catch (Exception ex) when (ex is XmlException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Invalid pom.xml format in {SourceFile}", sourceFile);
        }

        return results;
    }

    internal IReadOnlyList<TechnologyFinding> ParseBuildGradle(string content, string sourceFile, Guid scanId)
    {
        var results = new List<TechnologyFinding>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return results;
        }

        foreach (Match match in GradleCoordinateRegex.Matches(content))
        {
            var group = match.Groups["group"].Value;
            var artifact = match.Groups["artifact"].Value;
            var version = match.Groups["version"].Value;

            var name = $"{group}:{artifact}";
            results.Add(CreateFinding(name, version, sourceFile, "gradle", scanId));
        }

        var namedStyleRegex = new Regex(@"group:\s*'(?<group>[^']+)'\s*,\s*name:\s*'(?<artifact>[^']+)'\s*,\s*version:\s*'(?<version>[^']+)'",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        foreach (Match match in namedStyleRegex.Matches(content))
        {
            var group = match.Groups["group"].Value;
            var artifact = match.Groups["artifact"].Value;
            var version = match.Groups["version"].Value;
            var name = $"{group}:{artifact}";
            results.Add(CreateFinding(name, version, sourceFile, "gradle", scanId));
        }

        return results;
    }

    internal IReadOnlyList<TechnologyFinding> ParseDockerfile(string content, string sourceFile, Guid scanId)
    {
        var results = new List<TechnologyFinding>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return results;
        }

        foreach (Match match in DockerImageRegex.Matches(content))
        {
            var image = match.Groups["image"].Value;
            if (string.IsNullOrWhiteSpace(image))
            {
                continue;
            }

            var parts = image.Split(':', 2);
            var name = parts[0];
            var version = parts.Length > 1 ? parts[1].Split(' ').FirstOrDefault() : "latest";
            results.Add(CreateFinding(name, version, sourceFile, "dockerfile", scanId));
        }

        return results;
    }

    internal IReadOnlyList<TechnologyFinding> ParseDockerCompose(string content, string sourceFile, Guid scanId)
    {
        var results = new List<TechnologyFinding>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return results;
        }

        foreach (Match match in ComposeImageRegex.Matches(content))
        {
            var image = match.Groups["image"].Value.Trim();
            if (string.IsNullOrWhiteSpace(image))
            {
                continue;
            }

            var parts = image.Split(':', 2);
            var name = parts[0];
            var version = parts.Length > 1 ? parts[1] : "latest";
            results.Add(CreateFinding(name, version, sourceFile, "docker-compose", scanId));
        }

        return results;
    }

    private static TechnologyFinding? ParsePythonRequirement(string line, string sourceFile, string detector, Guid scanId)
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
        {
            return null;
        }

        var match = RequirementRegex.Match(line);
        if (!match.Success)
        {
            return null;
        }

        var name = match.Groups["name"].Value;
        var op = match.Groups["op"].Success ? match.Groups["op"].Value : string.Empty;
        var versionValue = match.Groups["version"].Success ? match.Groups["version"].Value : string.Empty;
        var version = string.IsNullOrWhiteSpace(op + versionValue) ? null : op + versionValue;

        return new TechnologyFinding
        {
            Id = Guid.NewGuid(),
            ScanId = scanId,
            Name = name,
            Version = version,
            SourceFile = sourceFile,
            Detector = detector,
            IsOutdated = false,
            LatestVersion = null
        };
    }

    private static List<string> ExtractBracketBlock(string content, string key)
    {
        var lines = content.Split('\n');
        var results = new List<string>();
        var collecting = false;
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (!collecting)
            {
                if (line.StartsWith($"{key}", StringComparison.OrdinalIgnoreCase) && line.Contains('['))
                {
                    collecting = true;
                    var afterBracket = line[(line.IndexOf('[') + 1)..];
                    if (afterBracket.Contains(']'))
                    {
                        var inner = afterBracket[..afterBracket.IndexOf(']')];
                        results.AddRange(inner.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(l => l.Trim('"', '\'')));
                        collecting = false;
                    }
                    else if (!string.IsNullOrWhiteSpace(afterBracket))
                    {
                        results.Add(afterBracket.Trim('"', '\'', ','));
                    }
                }
            }
            else
            {
                if (line.Contains(']'))
                {
                    var beforeBracket = line[..line.IndexOf(']')];
                    if (!string.IsNullOrWhiteSpace(beforeBracket))
                    {
                        results.Add(beforeBracket.Trim('"', '\'', ','));
                    }
                    collecting = false;
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    results.Add(line.Trim('"', '\'', ','));
                }
            }
        }

        return results;
    }

    private static TechnologyFinding CreateFinding(string name, string? version, string sourceFile, string detector, Guid scanId)
    {
        return new TechnologyFinding
        {
            Id = Guid.NewGuid(),
            ScanId = scanId,
            Name = name,
            Version = string.IsNullOrWhiteSpace(version) ? null : version,
            SourceFile = sourceFile,
            Detector = detector,
            IsOutdated = false,
            LatestVersion = null
        };
    }

    private static bool IsSupportedFile(string fileName, string fullPath)
    {
        return SupportedFileNames.Contains(fileName) || string.Equals(Path.GetExtension(fullPath), ".csproj", StringComparison.OrdinalIgnoreCase);
    }

    private string NormalizeAndValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must be provided.", nameof(path));
        }

        var fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {fullPath}");
        }

        if (!string.IsNullOrWhiteSpace(_options.AllowedRootPath))
        {
            var allowedRoot = Path.GetFullPath(_options.AllowedRootPath);
            if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Requested path is outside the allowed root.");
            }
        }

        return fullPath;
    }

    private IEnumerable<string> EnumerateFiles(string root, int depth, CancellationToken cancellationToken)
    {
        if (depth > _options.MaxDepth)
        {
            yield break;
        }

        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(root);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _logger.LogWarning(ex, "Skipping directory {Root}", root);
            yield break;
        }

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Directory.Exists(entry))
            {
                foreach (var child in EnumerateFiles(entry, depth + 1, cancellationToken))
                {
                    yield return child;
                }
            }
            else
            {
                yield return entry;
            }
        }
    }

    private IEnumerable<TechnologyFinding> ParseFile(string fileName, string extension, string content, string sourceFile, Guid scanId)
    {
        if (string.Equals(fileName, "package.json", StringComparison.OrdinalIgnoreCase))
        {
            return ParsePackageJson(content, sourceFile, scanId);
        }

        if (string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return ParseCsProj(content, sourceFile, scanId);
        }

        if (string.Equals(fileName, "global.json", StringComparison.OrdinalIgnoreCase))
        {
            return ParseGlobalJson(content, sourceFile, scanId);
        }

        if (string.Equals(fileName, "requirements.txt", StringComparison.OrdinalIgnoreCase))
        {
            return ParseRequirementsTxt(content, sourceFile, scanId);
        }

        if (string.Equals(fileName, "pyproject.toml", StringComparison.OrdinalIgnoreCase))
        {
            return ParsePyProjectToml(content, sourceFile, scanId);
        }

        if (string.Equals(fileName, "Gemfile", StringComparison.OrdinalIgnoreCase))
        {
            return ParseGemfile(content, sourceFile, scanId);
        }

        if (string.Equals(fileName, "Gemfile.lock", StringComparison.OrdinalIgnoreCase))
        {
            return ParseGemfileLock(content, sourceFile, scanId);
        }

        if (string.Equals(fileName, "go.mod", StringComparison.OrdinalIgnoreCase))
        {
            return ParseGoMod(content, sourceFile, scanId);
        }

        if (string.Equals(fileName, "go.sum", StringComparison.OrdinalIgnoreCase))
        {
            return ParseGoSum(content, sourceFile, scanId);
        }

        if (string.Equals(fileName, "pom.xml", StringComparison.OrdinalIgnoreCase))
        {
            return ParsePomXml(content, sourceFile, scanId);
        }

        if (string.Equals(fileName, "build.gradle", StringComparison.OrdinalIgnoreCase))
        {
            return ParseBuildGradle(content, sourceFile, scanId);
        }

        if (string.Equals(fileName, "Dockerfile", StringComparison.OrdinalIgnoreCase))
        {
            return ParseDockerfile(content, sourceFile, scanId);
        }

        if (string.Equals(fileName, "docker-compose.yml", StringComparison.OrdinalIgnoreCase))
        {
            return ParseDockerCompose(content, sourceFile, scanId);
        }

        return Array.Empty<TechnologyFinding>();
    }
}
