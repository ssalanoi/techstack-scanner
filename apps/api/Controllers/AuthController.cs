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

    public AuthController(JwtService jwtService, ILogger<AuthController> logger)
    {
        _jwtService = jwtService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? string.Empty;
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            _logger.LogError("Admin credentials are not configured in environment variables.");
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
