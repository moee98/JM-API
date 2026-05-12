namespace JMAPI.Models;

public class IntegrationToken
{
    public int Id { get; set; }
    public required string Provider { get; set; }      // "square" | "ebay"
    public required string AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Scope { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
