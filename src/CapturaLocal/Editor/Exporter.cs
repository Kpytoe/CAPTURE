using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CapturaLocal.Editor;

/// <summary>
/// Fusiona imagen y anotaciones en un unico mapa de bits. Es el momento en que las anotaciones
/// dejan de ser objetos y pasan a ser pixeles (regla de negocio del SRS 8).
/// </summary>
public static class Exporter
{
    /// <summary>
    /// Rasteriza el contenido del lienzo a resolucion nativa de la captura. El zoom del editor
    /// no interviene: se pide al lienzo que se pinte otra vez sobre un destino del tamano real.
    /// </summary>
    public static BitmapSource Rasterize(AnnotationCanvas canvas)
    {
        var width = canvas.Image.PixelWidth;
        var height = canvas.Image.PixelHeight;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            canvas.RenderContent(dc);
        }

        var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        target.Render(visual);
        target.Freeze();
        return target;
    }
}
