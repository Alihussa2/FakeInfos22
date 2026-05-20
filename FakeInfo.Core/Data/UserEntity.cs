namespace FakeInfo.Core.Data;

public class UserEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "user"; // "user" eller "admin"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}