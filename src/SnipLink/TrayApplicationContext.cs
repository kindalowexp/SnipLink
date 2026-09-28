using System.Runtime.InteropServices;

namespace SnipLink;

sealed class TrayApplicationContext : ApplicationContext
{
    readonly NotifyIcon _tray;
    readonly UiMarshal _ui = new();
    readonly HotkeyWindow _hotkeys = new();
    readonly IntPtr _iconHandle;
    bool _busy;

    public TrayApplicationContext()
    {
        _iconHandle = NativeMethods.CreateTrayIcon();
        var menu = new ContextMenuStrip();
        menu.Items.Add("Snip", null, (_, _) => PostSnip());
        menu.Items.Add("Settings", null, (_, _) => PostSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        _tray = new NotifyIcon
        {
            Icon = Icon.FromHandle(_iconHandle),
            Visible = true,
            Text = "SnipLink  (Ctrl+Shift+X)",
            ContextMenuStrip = menu,
        };
        _tray.DoubleClick += (_, _) => PostSnip();

        _hotkeys.Create();
        if (!_hotkeys.RegisterCtrlShiftX())
            _tray.ShowBalloonTip(3000, "SnipLink", "Ctrl+Shift+X is already in use. Snip from the tray menu.", ToolTipIcon.Warning);

        _hotkeys.Pressed += (_, _) => PostSnip();

        try
        {
            var settings = SettingsStore.Load();
            if (settings.Autostart)
                WindowsStartup.Apply(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
        }
    }

    void PostSnip() => _ui.Post(() => _ = SnipAsync());

    void PostSettings() => _ui.Post(ShowSettings);

    void ShowSettings()
    {
        if (_busy)
            return;

        _busy = true;
        try
        {
            using var form = new SettingsForm(SettingsStore.Load());
            if (form.ShowDialog() != DialogResult.OK || form.Result is null)
                return;

            try
            {
                SettingsStore.Save(form.Result);
                WindowsStartup.Apply(form.Result.Autostart);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                MessageBox.Show("Could not save settings. " + ex.Message, "SnipLink", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    async Task SnipAsync()
    {
        if (_busy)
            return;

        _busy = true;
        try
        {
            var settings = SettingsStore.Load();
            if (!DiscordUploader.IsValidWebhook(settings.WebhookUrl))
            {
                MessageBox.Show(
                    "Set a Discord webhook in Settings first.",
                    "SnipLink",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            await ToastForm.Countdown(Math.Clamp(settings.DelaySeconds, 0, 60));

            using var bitmap = SnipOverlayForm.Snip();
            if (bitmap is null)
                return;

            byte[] png;
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                png = stream.ToArray();
            }

            var outcome = await SnipPipeline.UploadAndShortenAsync(png, settings);
            SetClipboard(outcome.ClipboardText);
            ToastForm.Show(outcome.Toast);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "SnipLink", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    static void SetClipboard(string text)
    {
        ExternalException? last = null;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                return;
            }
            catch (ExternalException ex)
            {
                last = ex;
                Thread.Sleep(40);
            }
        }

        throw new InvalidOperationException("Could not copy the link to the clipboard.", last);
    }

    protected override void ExitThreadCore()
    {
        _hotkeys.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        NativeMethods.DestroyIcon(_iconHandle);
        _ui.Dispose();
        base.ExitThreadCore();
    }
}
