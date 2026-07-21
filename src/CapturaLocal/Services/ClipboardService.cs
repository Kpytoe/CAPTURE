using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CapturaLocal.Services;

/// <summary>
/// Copia al portapapeles con reintentos.
///
/// El portapapeles de Windows es un recurso exclusivo: cualquier aplicacion puede tenerlo
/// abierto en el instante justo en que nosotros lo pedimos, y entonces la llamada falla. No es
/// un error del usuario ni algo que se arregle repitiendo la accion a mano, asi que se
/// reintenta unas cuantas veces antes de rendirse (SRS 12).
/// </summary>
public static class ClipboardService
{
    private const int Attempts = 8;
    private const int DelayMs = 60;

    /// <summary>
    /// Devuelve true si se copio. Si no, <paramref name="error"/> explica que paso para poder
    /// ofrecer reintentar o guardar en su lugar.
    /// </summary>
    public static bool TryCopy(BitmapSource image, out string? error)
    {
        Exception? last = null;

        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            try
            {
                Clipboard.SetImage(image);
                FileLogger.Info($"Imagen copiada al portapapeles (intento {attempt}).");
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                last = ex;
                Thread.Sleep(DelayMs);
            }
        }

        FileLogger.Error($"No se pudo copiar al portapapeles tras {Attempts} intentos.", last);
        error = "Otra aplicación está usando el portapapeles y no lo ha soltado. " +
                "Puedes reintentar o guardar la captura en disco.";
        return false;
    }
}
