using System.Drawing;

namespace CapturaLocal.Interop;

/// <summary>
/// Un monitor, con sus limites en <b>pixeles fisicos</b> del escritorio virtual.
///
/// Todo el subsistema de captura trabaja en este espacio y solo en este. Es la regla que evita
/// el fallo del SRS 12 (monitores con escalas distintas): en cuanto una coordenada se convierte
/// a unidades de WPF deja de ser comparable con la de otro monitor, porque cada ventana escala
/// segun el DPI del monitor donde esta.
/// </summary>
public sealed record MonitorInfo(Rectangle Bounds, bool IsPrimary)
{
    /// <summary>
    /// Los monitores del sistema. Se apoya en WinForms Screen, que internamente enumera con
    /// EnumDisplayMonitors y devuelve coordenadas fisicas: la aplicacion se declara
    /// per-monitor DPI aware V2 en app.manifest, asi que Windows no las virtualiza.
    /// </summary>
    public static IReadOnlyList<MonitorInfo> All =>
        System.Windows.Forms.Screen.AllScreens
            .Select(s => new MonitorInfo(s.Bounds, s.Primary))
            .ToList();

    /// <summary>Rectangulo que envuelve todos los monitores. Puede empezar en negativo.</summary>
    public static Rectangle VirtualDesktop
    {
        get
        {
            var monitors = All;
            var left = monitors.Min(m => m.Bounds.Left);
            var top = monitors.Min(m => m.Bounds.Top);
            var right = monitors.Max(m => m.Bounds.Right);
            var bottom = monitors.Max(m => m.Bounds.Bottom);
            return Rectangle.FromLTRB(left, top, right, bottom);
        }
    }
}
