using System.Windows;
using System.Windows.Media;

namespace CapturaLocal.Editor.Annotations;

/// <summary>Contorno rectangular, con relleno semitransparente opcional (SRS 6.1).</summary>
public sealed class RectangleAnnotation : Annotation
{
    public Rect Rect { get; set; }

    public bool Filled { get; set; }

    public override void Render(DrawingContext dc)
    {
        if (Rect.Width <= 0 || Rect.Height <= 0) return;

        Brush? fill = null;
        if (Filled)
        {
            fill = new SolidColorBrush(Color) { Opacity = 0.25 };
            fill.Freeze();
        }

        dc.DrawRectangle(fill, CreatePen(), Rect);
    }

    public override Rect Bounds
    {
        get
        {
            var bounds = Rect;
            bounds.Inflate(Thickness / 2, Thickness / 2);
            return bounds;
        }
    }

    public override bool HitTest(Point point)
    {
        // Relleno: cuenta todo el interior. Solo contorno: unicamente la banda del borde, para
        // que se pueda seleccionar algo que este dentro del rectangulo.
        if (Filled) return Bounds.Contains(point);

        var outer = Rect;
        outer.Inflate(HitTolerance, HitTolerance);

        var inner = Rect;
        inner.Inflate(-HitTolerance, -HitTolerance);

        return outer.Contains(point) && !inner.Contains(point);
    }

    // Rect.Offset() muta en el sitio y devuelve void, asi que no sirve sobre una propiedad:
    // hay que construir el rectangulo desplazado.
    public override void Move(Vector delta) =>
        Rect = new Rect(Rect.X + delta.X, Rect.Y + delta.Y, Rect.Width, Rect.Height);
}
