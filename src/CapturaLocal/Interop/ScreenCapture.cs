using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace CapturaLocal.Interop;

/// <summary>
/// Lectura del framebuffer. Captura el escritorio virtual completo de una sola vez y lo congela;
/// a partir de ahi todo el mundo trabaja sobre esa imagen inmovil.
///
/// Congelar no es un detalle de implementacion, es lo que hace que el overlay pueda aparecer en
/// menos de 500 ms (RNF-01) y que la pantalla no se mueva mientras el usuario elige la region.
/// </summary>
public static class ScreenCapture
{
    /// <summary>
    /// Copia todos los monitores a un mapa de bits en pixeles fisicos. El pixel (0,0) del
    /// resultado corresponde a la esquina superior izquierda del escritorio virtual, que en
    /// montajes con un monitor a la izquierda del principal tiene coordenadas negativas.
    /// </summary>
    public static Bitmap CaptureVirtualDesktop()
    {
        var desktop = MonitorInfo.VirtualDesktop;
        var bitmap = new Bitmap(desktop.Width, desktop.Height, PixelFormat.Format32bppArgb);

        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(desktop.Left, desktop.Top, 0, 0, desktop.Size,
                                CopyPixelOperation.SourceCopy);

        return bitmap;
    }

    /// <summary>
    /// Recorta una region expresada en coordenadas fisicas del escritorio virtual.
    /// </summary>
    /// <param name="source">Imagen devuelta por <see cref="CaptureVirtualDesktop"/>.</param>
    /// <param name="region">Region en coordenadas del escritorio virtual.</param>
    public static Bitmap Crop(Bitmap source, Rectangle region)
    {
        var desktop = MonitorInfo.VirtualDesktop;

        // De coordenadas del escritorio virtual a indices dentro del mapa de bits.
        var local = new Rectangle(
            region.X - desktop.X,
            region.Y - desktop.Y,
            region.Width,
            region.Height);

        local.Intersect(new Rectangle(0, 0, source.Width, source.Height));

        if (local.Width <= 0 || local.Height <= 0)
        {
            throw new ArgumentException("La region seleccionada quedo fuera del escritorio.", nameof(region));
        }

        return source.Clone(local, source.PixelFormat);
    }

    /// <summary>
    /// Convierte a un BitmapSource de WPF, desacoplado del HBITMAP de origen para que se pueda
    /// liberar el mapa de bits nativo sin invalidar la imagen.
    /// </summary>
    public static BitmapSource ToBitmapSource(Bitmap bitmap)
    {
        var handle = bitmap.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                handle, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            source.Freeze();
            return source;
        }
        finally
        {
            NativeMethods.DeleteObject(handle);
        }
    }
}
