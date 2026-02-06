namespace SampleCode;

/// <summary>
/// A well-structured user service following C# best practices.
/// </summary>
public sealed class UserService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public UserService(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
    }

    /// <summary>
    /// Gets a user by ID asynchronously.
    /// </summary>
    public async Task<string?> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId), "User ID must be positive.");

        var response = await _httpClient.GetAsync($"{_baseUrl}/users/{userId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Lists users with proper pagination.
    /// </summary>
    public async Task<IReadOnlyList<string>> ListUsersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"{_baseUrl}/users?page={page}&size={pageSize}", cancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    public void Dispose()
    {
        // HttpClient is injected, so we don't dispose it here
    }
}
