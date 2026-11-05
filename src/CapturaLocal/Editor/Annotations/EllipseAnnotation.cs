using System.Windows;
using System.Windows.Media;

namespace CapturaLocal.Editor.Annotations;

/// <summary>
/// Contorno eliptico inscrito en un rectangulo (RF-07). Con Mayus el lienzo fuerza un cuadrado
/// y la elipse sale circulo, como pide el SRS 6.1.
/// </summary>
public sealed class EllipseAnnotation : Annotation
{
    public Rect Rect { get; set; }

    public override void Render(DrawingContext dc)
    {
        if (Rect.Width <= 0 || Rect.Height <= 0) return;
        dc.DrawEllipse(null, CreatePen(), Center, Rect.Width / 2, Rect.Height / 2);
    }

    private Point Center => new(Rect.X + Rect.Width / 2, Rect.Y + Rect.Height / 2);

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
        // Solo la banda del contorno, igual que el rectangulo: lo que quede dentro del circulo
        // tiene que poder seleccionarse.
        var rx = Rect.Width / 2;
        var ry = Rect.Height / 2;

        return Inside(point, rx + HitTolerance, ry + HitTolerance)
            && !Inside(point, rx - HitTolerance, ry - HitTolerance);
    }

    private bool Inside(Point point, double rx, double ry)
    {
        if (rx <= 0 || ry <= 0) return false;

        var dx = (point.X - Center.X) / rx;
        var dy = (point.Y - Center.Y) / ry;
        return dx * dx + dy * dy <= 1;
    }

    public override void Move(Vector delta) =>
        Rect = new Rect(Rect.X + delta.X, Rect.Y + delta.Y, Rect.Width, Rect.Height);
}
