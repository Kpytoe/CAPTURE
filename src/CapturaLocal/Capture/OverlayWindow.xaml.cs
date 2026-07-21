using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CapturaLocal.Interop;

using Drawing = System.Drawing;

namespace CapturaLocal.Capture;

/// <summary>
/// Overlay de seleccion de UN monitor. Hay una instancia por pantalla y todas comparten el
/// mismo estado de seleccion, expresado en pixeles fisicos del escritorio virtual.
///
/// La posicion del raton se lee con GetCursorPos y no con los eventos de WPF: asi la seleccion
/// sigue siendo correcta cuando el arrastre cruza a un monitor con otra escala (SRS 12).
/// </summary>
public partial class OverlayWindow : Window
{
    private readonly MonitorInfo _monitor;
    private readonly SelectionSession _session;

    public OverlayWindow(MonitorInfo monitor, BitmapSource frozenCrop, SelectionSession session,
                         bool showHint)
    {
        InitializeComponent();

        _monitor = monitor;
        _session = session;

        Frozen.Source = frozenCrop;
        Hint.Visibility = showHint ? Visibility.Visible : Visibility.Collapsed;

        _session.Changed += OnSessionChanged;

        SourceInitialized += OnSourceInitialized;
        Loaded += (_, _) => UpdateGeometry();
        SizeChanged += (_, _) => UpdateGeometry();
    }

    /// <summary>Escala de pixeles fisicos del monitor a unidades logicas de esta ventana.</summary>
    private double PixelsToUnits => Root.ActualWidth <= 0
        ? 1.0
        : Root.ActualWidth / _monitor.Bounds.Width;

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        // Colocar la ventana en pixeles fisicos exactos. Asignar Left/Top/Width/Height de WPF
        // no sirve: esas propiedades estan en unidades logicas y el factor cambia por monitor.
        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.SetWindowPos(handle, NativeMethods.HWND_TOPMOST,
            _monitor.Bounds.X, _monitor.Bounds.Y,
            _monitor.Bounds.Width, _monitor.Bounds.Height,
            NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_NOACTIVATE);
    }

    // --- Entrada ---

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Hint.Visibility = Visibility.Collapsed;
        _session.Begin(CursorPosition());
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_session.IsDragging) _session.DragTo(CursorPosition());
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!_session.IsDragging) return;

        ReleaseMouseCapture();
        _session.DragTo(CursorPosition());
        _session.Complete();
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);
        _session.Cancel();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape) _session.Cancel();
    }

    private static Drawing::Point CursorPosition()
    {
        NativeMethods.GetCursorPos(out var point);
        return new Drawing::Point(point.X, point.Y);
    }

    // --- Pintado ---

    private void OnSessionChanged() => UpdateGeometry();

    private void UpdateGeometry()
    {
        if (Root.ActualWidth <= 0) return;

        FullGeometry.Rect = new Rect(0, 0, Root.ActualWidth, Root.ActualHeight);

        var selection = _session.Selection;
        if (selection.Width <= 0 || selection.Height <= 0)
        {
            HoleGeometry.Rect = Rect.Empty;
            SelectionBorder.Visibility = Visibility.Collapsed;
            SizeBadge.Visibility = Visibility.Collapsed;
            return;
        }

        // De coordenadas del escritorio virtual a las de esta ventana.
        var scale = PixelsToUnits;
        var x = (selection.X - _monitor.Bounds.X) * scale;
        var y = (selection.Y - _monitor.Bounds.Y) * scale;
        var width = selection.Width * scale;
        var height = selection.Height * scale;

        HoleGeometry.Rect = new Rect(x, y, width, height);

        SelectionBorder.Visibility = Visibility.Visible;
        SelectionBorder.Margin = new Thickness(x, y, 0, 0);
        SelectionBorder.Width = width;
        SelectionBorder.Height = height;

        // Las medidas se anuncian en pixeles reales, que es lo que acabara midiendo el archivo.
        SizeText.Text = $"{selection.Width} × {selection.Height}";
        SizeBadge.Visibility = Visibility.Visible;
        SizeBadge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        // Debajo de la seleccion, o dentro si no cabe (por ejemplo al llegar al borde inferior).
        var badgeY = y + height + 6;
        if (badgeY + SizeBadge.DesiredSize.Height > Root.ActualHeight)
        {
            badgeY = Math.Max(0, y - SizeBadge.DesiredSize.Height - 6);
        }

        SizeBadge.Margin = new Thickness(x, badgeY, 0, 0);
    }

    protected override void OnClosed(EventArgs e)
    {
        _session.Changed -= OnSessionChanged;
        base.OnClosed(e);
    }
}
