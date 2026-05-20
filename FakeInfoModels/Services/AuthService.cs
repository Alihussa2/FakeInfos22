using System.Security.Cryptography;
using System.Text;

namespace FakeInfo.Core.Services;

public class AuthService
{
    // Simple JWT-like token (base64 encoded, NOT production-grade)
    private static readonly string SecretKey = "FakeInfoSuperSecretKey2026!ExamProject";

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "fakeinfo_salt"));
        return Convert.ToBase64String(bytes);
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        return HashPassword(password) == storedHash;
    }

    public string GenerateToken(string username)
    {
        var payload = $"{username}|{DateTime.UtcNow.AddHours(24):O}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
        var signature = hmac.ComputeHash(payloadBytes);

        return Convert.ToBase64String(payloadBytes) + "." + Convert.ToBase64String(signature);
    }

    public string? ValidateToken(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2) return null;

            var payloadBytes = Convert.FromBase64String(parts[0]);
            var providedSignature = Convert.FromBase64String(parts[1]);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
            var expectedSignature = hmac.ComputeHash(payloadBytes);

            if (!expectedSignature.SequenceEqual(providedSignature))
                return null;

            var payload = Encoding.UTF8.GetString(payloadBytes);
            var segments = payload.Split('|');
            if (segments.Length != 2) return null;

            var expiry = DateTime.Parse(segments[1]);
            if (expiry < DateTime.UtcNow) return null;

            return segments[0]; // username
        }
        catch
        {
            return null;
        }
    }

    public bool ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (password.Length < 8) return false;
        if (!password.Any(char.IsUpper)) return false;
        if (!password.Any(char.IsLower)) return false;
        if (!password.Any(char.IsDigit)) return false;
        return true;
    }

    public bool ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;
        if (username.Length < 3 || username.Length > 20) return false;
        return username.All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}
