using System.IO;
using System.Windows.Media.Imaging;
using CapturaLocal.Services;

namespace CapturaLocal.Capture;

/// <summary>
/// Andamio temporal del borrador 1: vuelca la captura a disco para poder comprobar que el
/// recorte coincide con lo que el usuario selecciono.
///
/// Desaparece en el Paso 6, cuando SaveService haga esto de verdad (con patron de nombre,
/// confirmacion de sobrescritura y manejo de errores recuperable).
/// </summary>
internal static class CapturePreview
{
    public static string SaveForVerification(BitmapSource image, string folder)
    {
        Directory.CreateDirectory(folder);

        var path = Path.Combine(folder, $"Captura_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));

        using (var stream = File.Create(path))
        {
            encoder.Save(stream);
        }

        FileLogger.Info($"Captura escrita en disco ({image.PixelWidth}x{image.PixelHeight} px).");
        return path;
    }
}
