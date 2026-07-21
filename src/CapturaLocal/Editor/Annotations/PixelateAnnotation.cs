using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CapturaLocal.Editor.Annotations;

/// <summary>
/// Difuminado por bloques para tapar informacion sensible.
///
/// Es la unica anotacion que lee pixeles de la imagen de origen, pero tampoco la modifica:
/// calcula una version en bloques de esa region y la dibuja encima. Si se mueve, se recalcula
/// desde la zona nueva — como una lupa opaca, no como un parche recortado.
/// </summary>
public sealed class PixelateAnnotation : Annotation
{
    private readonly BitmapSource _source;
    private Rect _rect;
    private BitmapSource? _cached;

    public PixelateAnnotation(BitmapSource source)
    {
        _source = source;
    }

    public Rect Rect
    {
        get => _rect;
        set { _rect = value; _cached = null; }
    }

    /// <summary>Lado del bloque en pixeles. Cuanto mayor, menos recuperable es el contenido.</summary>
    public int BlockSize { get; set; } = 12;

    public override void Render(DrawingContext dc)
    {
        if (_rect.Width < 1 || _rect.Height < 1) return;

        var pixelated = _cached ??= BuildPixelated();
        if (pixelated is null) return;

        dc.DrawImage(pixelated, _rect);
    }

    private BitmapSource? BuildPixelated()
    {
        // Recorte alineado a pixeles enteros y contenido dentro de la imagen: pedir a
        // CroppedBitmap una region que se sale lanza excepcion.
        var x = (int)Math.Round(_rect.X);
        var y = (int)Math.Round(_rect.Y);
        var width = (int)Math.Round(_rect.Width);
        var height = (int)Math.Round(_rect.Height);

        x = Math.Clamp(x, 0, _source.PixelWidth - 1);
        y = Math.Clamp(y, 0, _source.PixelHeight - 1);
        width = Math.Clamp(width, 1, _source.PixelWidth - x);
        height = Math.Clamp(height, 1, _source.PixelHeight - y);

        var crop = new CroppedBitmap(_source, new Int32Rect(x, y, width, height));

        // Reducir y volver a ampliar: ahi nacen los bloques.
        var smallWidth = Math.Max(1, width / BlockSize);
        var smallHeight = Math.Max(1, height / BlockSize);

        var small = new TransformedBitmap(crop,
            new ScaleTransform((double)smallWidth / width, (double)smallHeight / height));

        // La ampliacion tiene que ser por vecino mas proximo; con la interpolacion normal
        // saldria un borron suave, no bloques, y un borron suave a veces se puede revertir.
        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.NearestNeighbor);
        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(small, new Rect(0, 0, width, height));
        }

        var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        target.Render(visual);
        target.Freeze();
        return target;
    }

    public override Rect Bounds => _rect;

    public override bool HitTest(Point point) => _rect.Contains(point);

    public override void Move(Vector delta)
    {
        _rect = new Rect(_rect.X + delta.X, _rect.Y + delta.Y, _rect.Width, _rect.Height);
        _cached = null;
    }
}
