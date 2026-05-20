namespace FakeInfoModels;

public class LoginResponse
{
    public bool Success { get; set; }
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
    public string Error { get; set; } = "";
}