namespace SnipLink;

public static class SnipPipeline
{
    public readonly record struct Outcome(string ClipboardText, string Toast);

    public static async Task<Outcome> UploadAndShortenAsync(byte[] png, AppSettings settings, CancellationToken cancellationToken = default)
    {
        var direct = await DiscordUploader.UploadPngAsync(settings.WebhookUrl, png, cancellationToken);
        if (!settings.Shorten)
            return new Outcome(direct, direct);

        try
        {
            var shortUrl = await UrlShortener.ShortenAsync(direct, cancellationToken);
            return new Outcome(shortUrl, shortUrl);
        }
        catch (Exception ex) when (ex is ShortenException or HttpRequestException or TaskCanceledException)
        {
            return new Outcome(direct, direct);
        }
    }
}
