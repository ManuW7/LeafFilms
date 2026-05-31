using System.Text.Json;

namespace FeedService.Services;

public interface ISocialClient
{
    Task<IEnumerable<Guid>> GetFollowerIdsAsync(Guid userId);
}

public class SocialClient : ISocialClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SocialClient> _logger;

    public SocialClient(HttpClient http, ILogger<SocialClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IEnumerable<Guid>> GetFollowerIdsAsync(Guid userId)
    {
        try
        {
            var response = await _http.GetAsync($"users/{userId}/followers");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var followers = JsonSerializer.Deserialize<IEnumerable<FollowerDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return followers?.Select(f => f.UserId) ?? Enumerable.Empty<Guid>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get followers for user {UserId}", userId);
            return Enumerable.Empty<Guid>();
        }
    }

    private class FollowerDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}