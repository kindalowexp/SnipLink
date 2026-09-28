namespace SnipLink;

static class Program
{
    [STAThread]
    static void Main()
    {
        NativeMethods.TryEnablePerMonitorV2();
        ApplicationConfiguration.Initialize();
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

        using var mutex = new Mutex(true, @"Local\SnipLink.SingleInstance", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("SnipLink is already running.", "SnipLink", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.Run(new TrayApplicationContext());
    }
}
