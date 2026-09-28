using System.Drawing.Imaging;

namespace SnipLink;

sealed class SnipOverlayForm : Form
{
    readonly Bitmap _source;
    readonly Bitmap _dimmed;
    readonly Rectangle _virtual;
    readonly Pen _white = new(Color.White, 1);
    readonly Pen _black = new(Color.Black, 3);
    readonly Font _sizeFont = new(FontFamily.GenericSansSerif, 12, FontStyle.Regular, GraphicsUnit.Pixel);
    Rectangle _selection;
    Point _start;
    bool _dragging;

    SnipOverlayForm(Bitmap source, Rectangle virtualScreen)
    {
        _source = source;
        _virtual = virtualScreen;
        _dimmed = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(_dimmed))
        {
            graphics.DrawImageUnscaled(source, 0, 0);
            using var shade = new SolidBrush(Color.FromArgb(110, 0, 0, 0));
            graphics.FillRectangle(shade, 0, 0, _dimmed.Width, _dimmed.Height);
            using var hintFont = new Font(FontFamily.GenericSansSerif, 16, FontStyle.Bold, GraphicsUnit.Pixel);
            using var shadow = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
            const string hint = "Drag to select    Esc cancels";
            graphics.DrawString(hint, hintFont, shadow, 25, 25);
            graphics.DrawString(hint, hintFont, Brushes.White, 24, 24);
        }

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        Cursor = Cursors.Cross;
        BackColor = Color.Black;
        Bounds = virtualScreen;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        UpdateStyles();
    }

    public static Bitmap? Snip()
    {
        var virtualScreen = NativeMethods.VirtualScreen();
        if (virtualScreen.Width < 2 || virtualScreen.Height < 2)
            throw new InvalidOperationException("Could not read the virtual screen size.");

        var source = new Bitmap(virtualScreen.Width, virtualScreen.Height, PixelFormat.Format24bppRgb);
        try
        {
            using var graphics = Graphics.FromImage(source);
            graphics.CopyFromScreen(virtualScreen.Location, Point.Empty, virtualScreen.Size, CopyPixelOperation.SourceCopy);
        }
        catch
        {
            source.Dispose();
            throw;
        }

        using var form = new SnipOverlayForm(source, virtualScreen);
        return form.ShowDialog() == DialogResult.OK ? form.Cut() : null;
    }

    Bitmap Cut()
    {
        var selection = _selection;
        var crop = new Bitmap(selection.Width, selection.Height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(crop);
        graphics.DrawImage(_source, new Rectangle(0, 0, selection.Width, selection.Height), selection, GraphicsUnit.Pixel);
        return crop;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WsExToolWindow = 0x80;
            var parameters = base.CreateParams;
            parameters.ExStyle |= WsExToolWindow;
            return parameters;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Bounds = _virtual;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
        Focus();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        var clip = e.ClipRectangle;
        clip.Intersect(new Rectangle(Point.Empty, _dimmed.Size));
        if (clip.Width < 1 || clip.Height < 1)
            return;

        graphics.DrawImage(_dimmed, clip, clip, GraphicsUnit.Pixel);
        if (_selection.Width < 1 || _selection.Height < 1)
            return;

        var selection = _selection;
        graphics.DrawImage(_source, selection, selection, GraphicsUnit.Pixel);
        var border = new Rectangle(selection.X, selection.Y, Math.Max(1, selection.Width - 1), Math.Max(1, selection.Height - 1));
        graphics.DrawRectangle(_black, border);
        graphics.DrawRectangle(_white, border);

        var label = selection.Width + " x " + selection.Height;
        var y = selection.Y > 22 ? selection.Y - 18 : selection.Bottom + 4;
        graphics.DrawString(label, _sizeFont, Brushes.White, selection.X + 2, y);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Right)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        if (e.Button != MouseButtons.Left)
            return;

        _dragging = true;
        Capture = true;
        _start = ToBitmapPoint(e.Location);
        ReplaceSelection(Rectangle.Empty);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging)
            return;

        ReplaceSelection(Normalize(_start, ToBitmapPoint(e.Location)));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_dragging || e.Button != MouseButtons.Left)
            return;

        _dragging = false;
        Capture = false;
        var selection = Normalize(_start, ToBitmapPoint(e.Location));
        if (selection.Width < 2 || selection.Height < 2)
        {
            ReplaceSelection(Rectangle.Empty);
            return;
        }

        _selection = selection;
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    void ReplaceSelection(Rectangle next)
    {
        var previous = _selection;
        _selection = next;
        Invalidate(Grow(previous));
        Invalidate(Grow(next));
    }

    Rectangle Grow(Rectangle rectangle)
    {
        if (rectangle.Width < 1 || rectangle.Height < 1)
            return Rectangle.Empty;

        rectangle.Inflate(8, 28);
        rectangle.Intersect(ClientRectangle);
        return rectangle;
    }

    Point ToBitmapPoint(Point client)
    {
        var screen = PointToScreen(client);
        return new Point(
            Clamp(screen.X - _virtual.X, 0, _source.Width),
            Clamp(screen.Y - _virtual.Y, 0, _source.Height));
    }

    static int Clamp(int value, int min, int maxInclusive)
    {
        if (value < min)
            return min;
        if (value > maxInclusive)
            return maxInclusive;
        return value;
    }

    static Rectangle Normalize(Point a, Point b)
    {
        var x = Math.Min(a.X, b.X);
        var y = Math.Min(a.Y, b.Y);
        return new Rectangle(x, y, Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _white.Dispose();
            _black.Dispose();
            _sizeFont.Dispose();
            _dimmed.Dispose();
            _source.Dispose();
        }

        base.Dispose(disposing);
    }
}
