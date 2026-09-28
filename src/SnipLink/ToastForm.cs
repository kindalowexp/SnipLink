namespace SnipLink;

sealed class ToastForm : Form
{
    public static void Show(string message)
    {
        var toast = new ToastForm(message);
        toast.Show();
    }

    public static async Task Countdown(int seconds)
    {
        if (seconds <= 0)
            return;

        var form = new CountdownForm(seconds);
        form.Show();
        try
        {
            for (var left = seconds; left > 0; left--)
            {
                form.SetSeconds(left);
                await Task.Delay(1000);
            }
        }
        finally
        {
            form.Close();
            form.Dispose();
        }
    }

    ToastForm(string message)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.FromArgb(32, 34, 37);
        Width = 420;
        Height = 88;

        var area = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
        Left = area.Right - Width - 16;
        Top = area.Bottom - Height - 16;

        Controls.Add(new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 8, 14, 8),
        });

        var timer = new System.Windows.Forms.Timer { Interval = 3200 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            Close();
            Dispose();
        };
        timer.Start();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WsExToolWindow = 0x80;
            const int WsExNoActivate = 0x08000000;
            var parameters = base.CreateParams;
            parameters.ExStyle |= WsExToolWindow | WsExNoActivate;
            return parameters;
        }
    }

    protected override bool ShowWithoutActivation => true;
}

sealed class CountdownForm : Form
{
    readonly Label _label;

    public CountdownForm(int seconds)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.FromArgb(32, 34, 37);
        Width = 88;
        Height = 88;

        var area = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Top + (area.Height - Height) / 2;

        _label = new Label
        {
            Text = seconds.ToString(),
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(FontFamily.GenericSansSerif, 28, FontStyle.Bold, GraphicsUnit.Pixel),
        };
        Controls.Add(_label);
    }

    public void SetSeconds(int seconds) => _label.Text = seconds.ToString();

    protected override CreateParams CreateParams
    {
        get
        {
            const int WsExToolWindow = 0x80;
            const int WsExNoActivate = 0x08000000;
            var parameters = base.CreateParams;
            parameters.ExStyle |= WsExToolWindow | WsExNoActivate;
            return parameters;
        }
    }

    protected override bool ShowWithoutActivation => true;
}
