using System.Text.Json;

namespace ReviewService.Services;

public class MovieInfo
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Genre { get; set; } = string.Empty;
}

public interface ICatalogueClient
{
    Task<MovieInfo?> GetMovieAsync(Guid movieId);
}

public class CatalogueClient : ICatalogueClient
{
    private readonly HttpClient _http;
    private readonly ILogger<CatalogueClient> _logger;

    public CatalogueClient(HttpClient http, ILogger<CatalogueClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<MovieInfo?> GetMovieAsync(Guid movieId)
    {
        _logger.LogDebug("Fetching movie {MovieId} from CatalogueService", movieId);
        var response = await _http.GetAsync($"movies/{movieId}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MovieInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}