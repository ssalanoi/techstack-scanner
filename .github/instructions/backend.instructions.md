# Backend Development Instructions – TechStack Scanner

## Tech Stack
- **Framework:** ASP.NET Core 10.0
- **Language:** C# 13
- **ORM:** Entity Framework Core 9.0
- **Database:** SQLite 3
- **Auth:** JWT Bearer tokens (9.0)
- **Logging:** Serilog 8.0
- **Testing:** xUnit, Moq, FluentAssertions

## Project Structure

```
apps/api/
├── Controllers/          # API endpoints
│   ├── AuthController.cs
│   ├── ProjectsController.cs
│   └── ScanController.cs
├── Data/
│   ├── AppDbContext.cs   # EF Core context
│   └── Entities/         # Database models
├── Services/             # Business logic
│   ├── ScanService.cs
│   ├── QueueService.cs
│   ├── ScanWorkerService.cs
│   ├── LlmService.cs
│   ├── OutdatedDependencyService.cs
│   └── JwtService.cs
├── Models/               # DTOs and request models
├── Migrations/           # EF Core migrations
├── Options/              # Configuration models
├── App_Data/             # SQLite database
└── Logs/                 # Serilog logs
```

## Core Patterns

### 1. Program.cs Structure

The application bootstraps in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// 2. Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* options */);

// 4. Register services
builder.Services.AddSingleton<ScanService>();
builder.Services.AddHostedService<ScanWorkerService>();

var app = builder.Build();

// 5. Apply EF migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
```

**Requirements:**
- Serilog configured first
- DbContext registered before migrations
- JWT validation parameters match settings
- Migrations run automatically on startup
- CORS configured for `http://localhost:5173`

### 2. Database Context (Entity Framework Core)

Define DbContext with entities and UTC normalization:

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Scan> Scans => Set<Scan>();
    public DbSet<TechnologyFinding> TechnologyFindings => Set<TechnologyFinding>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure cascade deletes
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Scans)
            .WithOne(s => s.Project)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Scan>()
            .HasMany(s => s.Findings)
            .WithOne(f => f.Scan)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public override int SaveChanges()
    {
        NormalizeDates();
        return base.SaveChanges();
    }

    private void NormalizeDates()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Project project)
            {
                project.CreatedAt = DateTime.SpecifyKind(project.CreatedAt, DateTimeKind.Utc);
                project.UpdatedAt = DateTime.SpecifyKind(project.UpdatedAt, DateTimeKind.Utc);
            }
            // Similar for Scan and TechnologyFinding
        }
    }
}
```

**Requirements:**
- Configure cascade deletes in `OnModelCreating`
- Normalize dates to UTC in `SaveChanges` override
- Use `DbSet<T>` properties with `Set<T>()` accessors
- Connection string from `appsettings.json`: `DefaultConnection`
- SQLite database file location: `App_Data/scan.db`

### 3. Controllers

Use standard REST API patterns with JWT authorization:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Require JWT for all actions
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(AppDbContext db, ILogger<ProjectsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAll()
    {
        var projects = await _db.Projects
            .Include(p => p.Scans)
            .ToListAsync();
        
        return Ok(projects.Select(MapToDto));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var project = new Project { Name = request.Name, /* ... */ };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created project {ProjectId}", project.Id);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, MapToDto(project));
    }
}
```

**Requirements:**
- Use `[Authorize]` attribute for protected endpoints
- Inject `AppDbContext` and `ILogger<T>` via constructor
- Use async/await: `Task<ActionResult<T>>`
- Return proper HTTP status codes: `Ok()`, `CreatedAtAction()`, `NotFound()`, `BadRequest()`
- Validate `ModelState` for POST/PUT requests
- Log important actions with structured logging

### 4. Authentication (JWT)

JWT generation in `JwtService`:

```csharp
public class JwtService
{
    private readonly JwtSettings _settings;

    public JwtService(JwtSettings settings)
    {
        _settings = settings;
    }

    public string GenerateToken(string email)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

**Requirements:**
- JWT secret must be ≥32 characters (validated in `Program.cs`)
- Token includes email claim and JTI (unique identifier)
- Expiry from configuration (`Jwt:ExpiryMinutes`, default 60)
- Use `DateTime.UtcNow` for timestamps
- Validate credentials against env vars: `ADMIN_EMAIL`, `ADMIN_PASSWORD`

### 5. Scanning Pipeline

The scanning flow uses three services:

**ScanService** – Parses files and detects technologies:
```csharp
public class ScanService
{
    public List<TechnologyFinding> ScanDirectory(string rootPath, int maxDepth = 5)
    {
        var findings = new List<TechnologyFinding>();
        
        // Scan for package.json
        var packageJsonFiles = Directory.GetFiles(rootPath, "package.json", SearchOption.AllDirectories);
        foreach (var file in packageJsonFiles)
        {
            findings.AddRange(ParsePackageJson(file));
        }

        // Similar for other manifests
        return findings;
    }

    private List<TechnologyFinding> ParsePackageJson(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var json = JsonDocument.Parse(content);
        
        // Extract dependencies
        var findings = new List<TechnologyFinding>();
        if (json.RootElement.TryGetProperty("dependencies", out var deps))
        {
            foreach (var dep in deps.EnumerateObject())
            {
                findings.Add(CreateFinding(dep.Name, dep.Value.GetString(), "npm"));
            }
        }
        
        return findings;
    }
}
```

**QueueService** – In-memory queue:
```csharp
public class QueueService
{
    private readonly ConcurrentQueue<ScanQueueItem> _queue = new();

    public void Enqueue(int scanId)
    {
        _queue.Enqueue(new ScanQueueItem { ScanId = scanId });
    }

    public bool TryDequeue(out ScanQueueItem item)
    {
        return _queue.TryDequeue(out item);
    }
}
```

**ScanWorkerService** – Background processor:
```csharp
public class ScanWorkerService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var item))
            {
                await ProcessScan(item.ScanId);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessScan(int scanId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var scan = await db.Scans.FindAsync(scanId);
        
        // Update status, scan files, check outdated, generate LLM summary
    }
}
```

**Requirements:**
- `ScanService` is stateless and thread-safe
- Use `ConcurrentQueue` for queue implementation
- Worker service runs as `IHostedService`
- Create scoped `AppDbContext` per scan processing
- Handle exceptions and set scan status to `Failed` on error

### 6. LLM Integration (Ollama)

`LlmService` generates AI summaries:

```csharp
public class LlmService
{
    private readonly HttpClient _httpClient;
    private readonly LlmOptions _options;

    public async Task<string> GenerateSummaryAsync(List<TechnologyFinding> findings)
    {
        var prompt = BuildPrompt(findings);
        var payload = new { model = _options.Model, prompt = prompt, stream = false };

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/generate", payload);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                return result?.Response ?? "No summary available.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM attempt {Attempt} failed", attempt);
                if (attempt == 3) return "LLM summary unavailable.";
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }
    }
}
```

**Requirements:**
- Endpoint: `{OLLAMA_HOST}/api/generate` (default `http://localhost:11434`)
- Model from config: `Ollama:Model` (default `llama3.2`)
- 3 retry attempts with exponential backoff
- Return fallback text on failure (don't throw)
- Timeout from config: `Ollama:TimeoutSeconds` (default 120)

### 7. Outdated Dependency Service

Check latest versions from package registries:

```csharp
public class OutdatedDependencyService
{
    public async Task<string?> GetLatestNpmVersionAsync(string packageName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://registry.npmjs.org/{packageName}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("dist-tags").GetProperty("latest").GetString();
        }
        catch
        {
            return null;
        }
    }

    public bool IsOutdated(string currentVersion, string latestVersion)
    {
        // Compare semantic versions
        if (!Version.TryParse(CleanVersion(currentVersion), out var current)) return false;
        if (!Version.TryParse(CleanVersion(latestVersion), out var latest)) return false;
        return current < latest;
    }
}
```

**Requirements:**
- Support npm, NuGet, PyPI, RubyGems registries
- Handle network failures gracefully (return null)
- Clean version strings (remove `^`, `~`, etc.)
- Compare using semantic versioning
- Rate limit requests to avoid throttling

### 8. Configuration (Options Pattern)

Use strongly-typed configuration:

```csharp
// Options/LlmOptions.cs
public class LlmOptions
{
    public const string SectionName = "Ollama";
    
    public string Host { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxTokens { get; set; } = 1000;
}

// Program.cs
builder.Services.Configure<LlmOptions>(
    builder.Configuration.GetSection(LlmOptions.SectionName));
```

**Requirements:**
- Define options classes in `Options/` folder
- Use `const string SectionName` for section name
- Provide sensible defaults
- Bind in `Program.cs` using `Configure<T>()`
- Inject as `IOptions<T>` in services

### 9. Logging (Serilog)

Use structured logging:

```csharp
_logger.LogInformation("Processing scan {ScanId} for project {ProjectId}", 
    scan.Id, scan.ProjectId);

_logger.LogWarning("Failed to fetch version for {PackageName} from {Registry}", 
    packageName, "npm");

_logger.LogError(ex, "Scan {ScanId} failed with error", scanId);
```

**Requirements:**
- Use structured logging with named parameters
- Log levels: `Debug`, `Information`, `Warning`, `Error`
- Serilog config in `appsettings.json`: console + file sinks
- Log files: `Logs/log-.txt` with daily rolling
- Include exception objects in error logs

### 10. Testing (xUnit)

Write unit tests with xUnit, Moq, FluentAssertions:

```csharp
public class ScanServiceTests
{
    [Fact]
    public void ParsePackageJson_ExtractsDependencies()
    {
        // Arrange
        var service = new ScanService();
        var testFile = "test-package.json";
        File.WriteAllText(testFile, @"{""dependencies"":{""react"":""18.0.0""}}");

        // Act
        var findings = service.ParsePackageJson(testFile);

        // Assert
        findings.Should().ContainSingle();
        findings.First().Name.Should().Be("react");
        findings.First().Version.Should().Be("18.0.0");
        findings.First().Category.Should().Be("npm");
    }

    [Theory]
    [InlineData("1.0.0", "2.0.0", true)]
    [InlineData("2.0.0", "1.0.0", false)]
    public void IsOutdated_ComparesVersions(string current, string latest, bool expected)
    {
        var service = new OutdatedDependencyService(Mock.Of<HttpClient>());
        service.IsOutdated(current, latest).Should().Be(expected);
    }
}
```

**Requirements:**
- Test file per service: `{ServiceName}Tests.cs`
- Use `[Fact]` for single test cases
- Use `[Theory]` + `[InlineData]` for parameterized tests
- Use FluentAssertions: `.Should().Be()`, `.Should().ContainSingle()`
- Mock dependencies with Moq: `Mock.Of<IService>()`
- Clean up test files in teardown

## Code Style

### Naming Conventions
- Classes/Interfaces: PascalCase (`ScanService`, `IProjectRepository`)
- Methods: PascalCase (`GetProjectById`, `SaveChangesAsync`)
- Properties: PascalCase (`CreatedAt`, `IsOutdated`)
- Parameters/Variables: camelCase (`scanId`, `projectName`)
- Private fields: `_camelCase` (`_dbContext`, `_logger`)
- Constants: PascalCase (`SectionName`, `DefaultTimeout`)

### Async/Await
```csharp
// ✅ Do
public async Task<Project> GetProjectAsync(int id)
{
    return await _db.Projects.FindAsync(id);
}

// ❌ Don't
public async Task<Project> GetProject(int id)
{
    return _db.Projects.Find(id);
}
```

**Requirements:**
- Suffix async methods with `Async`
- Use `Task<T>` return type
- Avoid `async void` (except event handlers)
- Use `ConfigureAwait(false)` for library code (not needed in ASP.NET Core)

### Dependency Injection
```csharp
// ✅ Do - Constructor injection
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(AppDbContext db, ILogger<ProjectsController> logger)
    {
        _db = db;
        _logger = logger;
    }
}

// ❌ Don't - Service locator anti-pattern
public class BadService
{
    public void DoWork(IServiceProvider services)
    {
        var db = services.GetService<AppDbContext>();
    }
}
```

### Exception Handling
```csharp
// ✅ Do - Log and return error response
try
{
    await ProcessScan(scanId);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to process scan {ScanId}", scanId);
    return StatusCode(500, "Scan processing failed");
}

// ❌ Don't - Swallow exceptions
try
{
    await ProcessScan(scanId);
}
catch { }
```

## Environment Variables

**Required:**
- `JWT_SECRET` – Min 32 chars (dev default: auto-generated)
- `ADMIN_EMAIL` – Admin login email (dev default: `admin@techstack.local`)
- `ADMIN_PASSWORD` – Admin password (dev default: `ChangeMe123!`)

**Optional:**
- `OLLAMA_HOST` – Default: `http://localhost:11434`
- `OLLAMA_MODEL` – Default: `llama3.2`
- `Jwt:Issuer` – Default: `TechStackScanner`
- `Jwt:Audience` – Default: `TechStackScanner`
- `Jwt:ExpiryMinutes` – Default: `60`

## Build Commands

```bash
# Build
dotnet build apps/api/api.csproj

# Run (from repo root)
dotnet run --project apps/api/api.csproj --urls http://localhost:5000

# Run tests
dotnet test apps/api.Tests/api.Tests.csproj

# Add migration
dotnet ef migrations add MigrationName --project apps/api

# Apply migrations (automatic on startup)
dotnet ef database update --project apps/api
```

## Performance Best Practices

1. **EF Core:**
   - Use `.AsNoTracking()` for read-only queries
   - Use `Include()` to prevent N+1 queries
   - Batch SaveChanges calls when possible

2. **Async I/O:**
   - Use async methods for all I/O operations
   - Avoid blocking calls like `.Result` or `.Wait()`

3. **Caching:**
   - Consider adding `IMemoryCache` for frequently accessed data
   - Cache LLM responses for identical prompts

4. **Background Processing:**
   - Offload long-running tasks to `ScanWorkerService`
   - Use `CancellationToken` for graceful shutdown

## Common Pitfalls

❌ **Don't:**
- Use `DateTime.Now` (use `DateTime.UtcNow`)
- Expose entities directly as API responses
- Ignore `CancellationToken` in async methods
- Hard-code connection strings or secrets
- Use `Find()` when you need `.Include()`

✅ **Do:**
- Use UTC timestamps consistently
- Map entities to DTOs for API responses
- Pass `CancellationToken` to async methods
- Use configuration and env vars for settings
- Use eager loading with `.Include()` or projections

## Debug Checklist

When things don't work:
1. Check logs in `Logs/` folder
2. Verify environment variables are set
3. Check database exists: `App_Data/scan.db`
4. Verify migrations applied: `dotnet ef database update`
5. Test JWT endpoint: `POST /api/auth/login`
6. Check CORS configuration matches frontend URL
7. Verify Ollama is running (if using LLM features)

## Additional Resources

- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [Serilog](https://serilog.net/)
- [xUnit](https://xunit.net/)
- [Moq](https://github.com/moq/moq4)
