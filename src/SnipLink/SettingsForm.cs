namespace SnipLink;

sealed class SettingsForm : Form
{
    readonly TextBox _webhook = new();
    readonly CheckBox _shorten = new();
    readonly NumericUpDown _delay = new();
    readonly CheckBox _autostart = new();

    public AppSettings? Result { get; private set; }

    public SettingsForm(AppSettings current)
    {
        Text = "SnipLink settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(520, 248);
        Font = SystemFonts.MessageBoxFont;

        var webhookLabel = new Label { Text = "Discord webhook URL", AutoSize = true, Left = 16, Top = 16 };
        _webhook.SetBounds(16, 38, 488, 23);
        _webhook.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _webhook.Text = current.WebhookUrl;

        _shorten.Text = "Shorten link";
        _shorten.AutoSize = true;
        _shorten.Left = 16;
        _shorten.Top = 78;
        _shorten.Checked = current.Shorten;

        var delayLabel = new Label { Text = "Delay (seconds)", AutoSize = true, Left = 16, Top = 114 };
        _delay.SetBounds(140, 110, 64, 23);
        _delay.Minimum = 0;
        _delay.Maximum = 60;
        _delay.Value = Math.Clamp(current.DelaySeconds, 0, 60);

        _autostart.Text = "Start with Windows";
        _autostart.AutoSize = true;
        _autostart.Left = 16;
        _autostart.Top = 150;
        _autostart.Checked = current.Autostart;

        var save = new Button { Text = "Save", Left = 328, Top = 202, Width = 80 };
        var cancel = new Button { Text = "Cancel", Left = 416, Top = 202, Width = 88, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => SaveAndClose();
        AcceptButton = save;
        CancelButton = cancel;

        Controls.AddRange(new Control[] { webhookLabel, _webhook, _shorten, delayLabel, _delay, _autostart, save, cancel });
    }

    void SaveAndClose()
    {
        var raw = _webhook.Text.Trim();
        var settings = new AppSettings
        {
            Shorten = _shorten.Checked,
            DelaySeconds = (int)_delay.Value,
            Autostart = _autostart.Checked,
        };

        if (raw.Length == 0)
        {
            Result = settings;
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        var webhook = DiscordUploader.NormalizeWebhook(raw);
        if (!DiscordUploader.IsValidWebhook(webhook))
        {
            MessageBox.Show(
                this,
                "That is not a Discord webhook URL.",
                "SnipLink",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        settings.WebhookUrl = webhook;
        Result = settings;
        DialogResult = DialogResult.OK;
        Close();
    }
}
