namespace Api.Models.Auth;

public sealed class LoginResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public string TokenType { get; set; } = "Bearer";
}
