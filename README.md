# SnipLink

Snip a region of the screen, upload the picture to a Discord channel, and copy the link.

## Download

[Releases](https://github.com/kindalowexp/SnipLink/releases)

Unzip and run `SnipLink.exe`. It stays in the tray.

## Usage

Create a webhook for the channel the pictures should go to:

1. Open the channel.
2. Click the gear next to the channel name.
3. Integrations → Webhooks → New Webhook.
4. Copy Webhook URL.

Right-click the tray icon, open Settings, paste the URL, and save.

- `Ctrl+Shift+X` starts a snip
- Drag to select the region
- `Esc` or right-click cancels

**Delay (seconds)** in Settings waits before the snip. **Start with Windows** is in Settings.

The link is copied to the clipboard. **Shorten link** in Settings uses [da.gd](https://da.gd). Leave it off to copy the Discord link.

The webhook is saved in `%AppData%\SnipLink\settings.json`.

## Building

[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```
dotnet run --project src/SnipLink/SnipLink.csproj
```

```
dotnet publish src/SnipLink/SnipLink.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist
```
