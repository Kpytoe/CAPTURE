using System.Windows;
using System.Windows.Media;

namespace CapturaLocal.Editor.Annotations;

/// <summary>
/// Bloque de color solido para tapar algo, la alternativa a pixelar cuando se quiere que la
/// zona desaparezca del todo (un fondo blanco sobre un dato, por ejemplo). Con el cuentagotas
/// del editor el color puede ser exactamente el del fondo de la captura.
/// </summary>
public sealed class PaintAnnotation : Annotation
{
    /// <summary>Lo que el usuario arrastro, sin redondear: Move tiene que ser reversible.</summary>
    public Rect Rect { get; set; }

    public override void Render(DrawingContext dc)
    {
        if (Rect.Width <= 0 || Rect.Height <= 0) return;

        // Siempre opaco: es una herramienta para tapar, y un color con transparencia dejaria
        // ver lo que hay debajo.
        var brush = new SolidColorBrush(Color.FromRgb(Color.R, Color.G, Color.B));
        brush.Freeze();

        dc.DrawRectangle(brush, null, Bounds);
    }

    /// <summary>
    /// El rectangulo ampliado a pixeles enteros. Con coordenadas fraccionarias (cualquier zoom
    /// distinto del 100 %) el borde se suavizaria y dejaria una franja semitransparente por
    /// donde asoma el contenido tapado.
    /// </summary>
    public override Rect Bounds
    {
        get
        {
            var left = Math.Floor(Rect.Left);
            var top = Math.Floor(Rect.Top);
            return new Rect(left, top,
                Math.Ceiling(Rect.Right) - left,
                Math.Ceiling(Rect.Bottom) - top);
        }
    }

    public override bool HitTest(Point point) => Bounds.Contains(point);

    public override void Move(Vector delta) =>
        Rect = new Rect(Rect.X + delta.X, Rect.Y + delta.Y, Rect.Width, Rect.Height);
}
