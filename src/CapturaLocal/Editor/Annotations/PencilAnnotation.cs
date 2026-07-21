using System.Windows;
using System.Windows.Media;

namespace CapturaLocal.Editor.Annotations;

/// <summary>Trazo libre: la sucesion de puntos por los que paso el raton.</summary>
public sealed class PencilAnnotation : Annotation
{
    private readonly List<Point> _points = new();
    private Geometry? _cached;

    public IReadOnlyList<Point> Points => _points;

    public void AddPoint(Point point)
    {
        // Puntos casi identicos no anaden forma y si coste: el raton reporta decenas por
        // segundo aunque apenas se mueva.
        if (_points.Count > 0)
        {
            var last = _points[^1];
            if (Math.Abs(last.X - point.X) < 0.7 && Math.Abs(last.Y - point.Y) < 0.7) return;
        }

        _points.Add(point);
        _cached = null;
    }

    public bool IsEmpty => _points.Count < 2;

    private Geometry Geometry
    {
        get
        {
            if (_cached is not null) return _cached;

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(_points[0], isFilled: false, isClosed: false);
                ctx.PolyLineTo(_points.Skip(1).ToList(), isStroked: true, isSmoothJoin: true);
            }

            geometry.Freeze();
            return _cached = geometry;
        }
    }

    public override void Render(DrawingContext dc)
    {
        if (IsEmpty) return;
        dc.DrawGeometry(null, CreatePen(), Geometry);
    }

    public override Rect Bounds => IsEmpty ? Rect.Empty : Geometry.GetRenderBounds(CreatePen());

    public override bool HitTest(Point point)
    {
        if (IsEmpty) return false;

        // StrokeContains con un lapiz mas grueso da el margen de tolerancia sin recalcular
        // la geometria a mano.
        var fat = new Pen(Brushes.Black, Thickness + HitTolerance);
        return Geometry.StrokeContains(fat, point);
    }

    public override void Move(Vector delta)
    {
        for (var i = 0; i < _points.Count; i++)
        {
            _points[i] += delta;
        }

        _cached = null;
    }
}
