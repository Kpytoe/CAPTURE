using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace CapturaLocal.Editor.Annotations;

/// <summary>Texto sobre la captura, con color y tamano configurables (RF-08).</summary>
public sealed class TextAnnotation : Annotation
{
    private static readonly Typeface Face = new("Segoe UI");

    private FormattedText? _cached;
    private string _text = string.Empty;
    private double _fontSize = 20;

    /// <summary>Esquina superior izquierda, en pixeles de la imagen.</summary>
    public Point Origin { get; set; }

    public string Text
    {
        get => _text;
        set { _text = value; _cached = null; }
    }

    public double FontSize
    {
        get => _fontSize;
        set { _fontSize = value; _cached = null; }
    }

    public bool IsEmpty => string.IsNullOrWhiteSpace(_text);

    private FormattedText Formatted
    {
        get
        {
            if (_cached is not null) return _cached;

            var brush = new SolidColorBrush(Color);
            brush.Freeze();

            // pixelsPerDip = 1: las coordenadas son pixeles de la imagen, no de la pantalla,
            // asi que el texto debe medirse en ese mismo espacio o cambiaria de tamano segun
            // el monitor donde se edite.
            return _cached = new FormattedText(
                _text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                Face, _fontSize, brush, pixelsPerDip: 1.0);
        }
    }

    public override void Render(DrawingContext dc)
    {
        if (IsEmpty) return;
        dc.DrawText(Formatted, Origin);
    }

    public override Rect Bounds => IsEmpty
        ? Rect.Empty
        : new Rect(Origin, new Size(Formatted.Width, Formatted.Height));

    public override bool HitTest(Point point)
    {
        var bounds = Bounds;
        bounds.Inflate(HitTolerance / 2, HitTolerance / 2);
        return bounds.Contains(point);
    }

    public override void Move(Vector delta) => Origin += delta;
}
