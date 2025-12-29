using System.Security.Cryptography;
using System.Text;
using Api.Models.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(JwtService jwtService, ILogger<AuthController> logger, IConfiguration configuration)
    {
        _jwtService = jwtService;
        _logger = logger;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // Try environment variables first, then fall back to configuration (appsettings.Development.json)
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") 
                         ?? _configuration["Auth:AdminEmail"] 
                         ?? string.Empty;
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") 
                            ?? _configuration["Auth:AdminPassword"] 
                            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            _logger.LogError("Admin credentials are not configured in environment variables or configuration.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Admin credentials are not configured.");
        }

        var emailMatches = string.Equals(adminEmail.Trim(), request.Email.Trim(), StringComparison.OrdinalIgnoreCase);
        var passwordMatches = SecureEquals(adminPassword, request.Password);

        if (!emailMatches || !passwordMatches)
        {
            return Unauthorized();
        }

        var (token, expiresAtUtc) = _jwtService.GenerateToken(adminEmail.Trim());
        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc
        });
    }

    private static bool SecureEquals(string expected, string provided)
    {
        var expectedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(expected ?? string.Empty));
        var providedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(provided ?? string.Empty));
        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
