using Xunit;

namespace SnipLink.Tests;

public class ParserTests
{
    [Theory]
    [InlineData("https://discord.com/api/webhooks/123/abc_DEF-09")]
    [InlineData("https://discordapp.com/api/webhooks/123/abc/")]
    [InlineData("  <https://discord.com/api/webhooks/123/tok.en>  ")]
    [InlineData("https://Discord.com/api/webhooks/99/token?wait=true")]
    public void AcceptsDiscordWebhooks(string url)
    {
        Assert.True(DiscordUploader.IsValidWebhook(url));
        Assert.StartsWith("https://discord", DiscordUploader.NormalizeWebhook(url));
        Assert.DoesNotContain("?", DiscordUploader.NormalizeWebhook(url));
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://example.com/api/webhooks/123/abc")]
    [InlineData("http://discord.com/api/webhooks/123/abc")]
    [InlineData("https://discord.com/api/webhooks/123/abc/slack")]
    public void RejectsNonWebhooks(string url)
    {
        Assert.False(DiscordUploader.IsValidWebhook(url));
    }

    [Fact]
    public void ReadsDiscordAttachmentUrl()
    {
        const string json = """
            {
              "attachments": [
                { "url": "https://cdn.discordapp.com/attachments/1/2/snip.png?ex=abc&is=def&hm=ghi" }
              ]
            }
            """;

        Assert.Equal(
            "https://cdn.discordapp.com/attachments/1/2/snip.png?ex=abc&is=def&hm=ghi",
            DiscordUploader.ParseAttachmentUrl(json));
    }

    [Fact]
    public void RejectsDiscordMessageWithoutAttachment()
    {
        Assert.Throws<InvalidOperationException>(() => DiscordUploader.ParseAttachmentUrl("{\"attachments\":[]}"));
    }

    [Fact]
    public void SettingsRoundTrip()
    {
        var path = Path.Combine(Path.GetTempPath(), "sniplink-tests", Guid.NewGuid().ToString("n"), "settings.json");
        var settings = new AppSettings
        {
            WebhookUrl = "https://discord.com/api/webhooks/123/abc",
            Shorten = true,
            DelaySeconds = 5,
            Autostart = true,
        };

        SettingsStore.Save(settings, path);
        var loaded = SettingsStore.Load(path);

        Assert.Equal(settings.WebhookUrl, loaded.WebhookUrl);
        Assert.True(loaded.Shorten);
        Assert.Equal(5, loaded.DelaySeconds);
        Assert.True(loaded.Autostart);
    }
}
