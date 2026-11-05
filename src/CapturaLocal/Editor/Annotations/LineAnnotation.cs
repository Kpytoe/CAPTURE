using System.Windows;
using System.Windows.Media;

namespace CapturaLocal.Editor.Annotations;

/// <summary>Linea recta entre dos puntos (RF-07).</summary>
public sealed class LineAnnotation : Annotation
{
    public Point Start { get; set; }

    public Point End { get; set; }

    public bool IsEmpty => (End - Start).Length < 2;

    public override void Render(DrawingContext dc)
    {
        if (IsEmpty) return;
        dc.DrawLine(CreatePen(), Start, End);
    }

    public override Rect Bounds
    {
        get
        {
            var bounds = new Rect(Start, End);
            bounds.Inflate(Thickness / 2, Thickness / 2);
            return bounds;
        }
    }

    /// <summary>Distancia del punto al segmento, no a la recta infinita que lo contiene.</summary>
    public override bool HitTest(Point point)
    {
        var segment = End - Start;
        var lengthSquared = segment.LengthSquared;

        // Proyeccion del punto sobre el segmento, recortada a sus extremos.
        var t = lengthSquared == 0
            ? 0
            : Math.Clamp(Vector.Multiply(point - Start, segment) / lengthSquared, 0, 1);

        var closest = Start + segment * t;
        return (point - closest).Length <= HitTolerance;
    }

    public override void Move(Vector delta)
    {
        Start += delta;
        End += delta;
    }
}
