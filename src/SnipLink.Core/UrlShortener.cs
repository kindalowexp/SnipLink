namespace SnipLink;

public sealed class ShortenException : Exception
{
    public ShortenException(string longUrl, string message) : base(message) => LongUrl = longUrl;

    public string LongUrl { get; }
}

public static class UrlShortener
{
    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("SnipLink/1.0");
        return http;
    }

    public static async Task<string> ShortenAsync(string longUrl, CancellationToken cancellationToken = default)
    {
        var request = "https://da.gd/s?url=" + Uri.EscapeDataString(longUrl);
        using var response = await Http.GetAsync(request, cancellationToken);
        var text = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
        if (!response.IsSuccessStatusCode
            || !(text.StartsWith("https://da.gd/", StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("http://da.gd/", StringComparison.OrdinalIgnoreCase)))
            throw new ShortenException(longUrl, "da.gd failed.");

        return text;
    }
}
