namespace SnipLink;

sealed class HotkeyWindow : NativeWindow, IDisposable
{
    const int WmHotkey = 0x0312;
    const int HotkeyId = 1;
    const uint ModControl = 0x0002;
    const uint ModShift = 0x0004;
    const uint ModNoRepeat = 0x4000;
    const uint VkX = 0x58;

    bool _registered;

    public event EventHandler? Pressed;

    public void Create()
    {
        CreateHandle(new CreateParams { Parent = new IntPtr(-3) });
    }

    public bool RegisterCtrlShiftX()
    {
        _registered = NativeMethods.RegisterHotKey(Handle, HotkeyId, ModControl | ModShift | ModNoRepeat, VkX);
        return _registered;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey)
            Pressed?.Invoke(this, EventArgs.Empty);

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_registered && Handle != IntPtr.Zero)
            NativeMethods.UnregisterHotKey(Handle, HotkeyId);

        if (Handle != IntPtr.Zero)
            DestroyHandle();
    }
}

sealed class UiMarshal : Control
{
    public UiMarshal() => CreateHandle();

    public void Post(Action action) => BeginInvoke(action);
}
