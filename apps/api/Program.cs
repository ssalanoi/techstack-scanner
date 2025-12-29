using System.Text;
using Api.Data;
using Api.Models.DTOs;
using Api.Options;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Use development defaults if environment variables are not set
var isDevelopment = builder.Environment.IsDevelopment();
var jwtSecret = builder.Configuration["JWT_SECRET"];

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    if (isDevelopment)
    {
        jwtSecret = "development-secret-key-32-chars-min-for-jwt-token-signing";
        builder.Logging.AddConsole().AddFilter("Microsoft", LogLevel.Warning);
        Console.WriteLine("⚠️  Using default JWT_SECRET for development. Set JWT_SECRET env var for production.");
    }
    else
    {
        throw new InvalidOperationException("JWT_SECRET must be provided in production environments.");
    }
}

if (jwtSecret.Length < 32)
{
    throw new InvalidOperationException("JWT_SECRET must be at least 32 characters long.");
}

var jwtSettings = new JwtSettings
{
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "TechStackScanner",
    Audience = builder.Configuration["Jwt:Audience"] ?? "TechStackScanner",
    Secret = jwtSecret,
    ExpiryMinutes = int.TryParse(builder.Configuration["Jwt:ExpiryMinutes"], out var expiry) ? expiry : 60
};

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<JwtService>();
builder.Services.Configure<ScanOptions>(builder.Configuration.GetSection("ScanOptions"));
builder.Services.Configure<QueueOptions>(builder.Configuration.GetSection("QueueOptions"));
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.SectionName));
builder.Services.AddSingleton<ScanService>();
builder.Services.AddSingleton<QueueService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<OutdatedDependencyService>();
builder.Services.AddHttpClient<LlmService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
    client.BaseAddress = new Uri(options.Host);
    client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds));
});
builder.Services.AddHostedService<ScanWorkerService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVite", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TechStack Scanner API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Display default credentials info
    var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") 
                     ?? app.Configuration["Auth:AdminEmail"];
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") 
                        ?? app.Configuration["Auth:AdminPassword"];
    
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine("🔐 DEVELOPMENT MODE - Default Credentials Loaded");
        Console.WriteLine($"   Email: {adminEmail}");
        Console.WriteLine($"   Password: {adminPassword}");
        Console.WriteLine("   ⚠️  Change these in production!");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    }
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors("AllowVite");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Ok("TechStack Scanner API"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
