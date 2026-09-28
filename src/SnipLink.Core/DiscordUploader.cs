using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SnipLink;

public static class DiscordUploader
{
    static readonly HttpClient Http = CreateClient();

    static readonly Regex WebhookPattern = new(
        @"^https://(discord\.com|discordapp\.com)/api/webhooks/\d+/[A-Za-z0-9_.\-]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("SnipLink/1.0");
        return http;
    }

    public static string NormalizeWebhook(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "";

        var trimmed = url.Trim().Trim('<', '>', '"').Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return "";
        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return "";
        if (!uri.Host.Equals("discord.com", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.Equals("discordapp.com", StringComparison.OrdinalIgnoreCase))
            return "";

        return "https://" + uri.Host.ToLowerInvariant() + uri.AbsolutePath.TrimEnd('/');
    }

    public static bool IsValidWebhook(string? url) => WebhookPattern.IsMatch(NormalizeWebhook(url));

    public static async Task<string> UploadPngAsync(string webhookUrl, byte[] png, CancellationToken cancellationToken = default)
    {
        var webhook = NormalizeWebhook(webhookUrl);
        if (!WebhookPattern.IsMatch(webhook))
            throw new InvalidOperationException("The Discord webhook URL is missing or not a discord.com webhook.");
        if (png.Length == 0)
            throw new InvalidOperationException("The screenshot was empty.");

        var fileName = "snip-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png";
        using var body = new MultipartFormDataContent();
        var file = new ByteArrayContent(png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        body.Add(file, "files[0]", fileName);

        using var response = await Http.PostAsync(webhook + "?wait=true", body, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Discord rejected the upload ({(int)response.StatusCode}). {Trim(text)}");

        return ParseAttachmentUrl(text);
    }

    public static string ParseAttachmentUrl(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("attachments", out var attachments)
                || attachments.ValueKind != JsonValueKind.Array
                || attachments.GetArrayLength() == 0
                || !attachments[0].TryGetProperty("url", out var urlProp))
            {
                throw new InvalidOperationException("Discord did not return an attachment URL.");
            }

            var url = urlProp.GetString();
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("Discord returned an empty attachment URL.");

            return url;
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Discord returned something that was not a message. " + Trim(json));
        }
    }

    static string Trim(string text)
    {
        text = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return text.Length <= 400 ? text : text[..400];
    }
}
