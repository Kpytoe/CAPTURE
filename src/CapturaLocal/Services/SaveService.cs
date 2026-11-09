using System.IO;
using System.Windows.Media.Imaging;

namespace CapturaLocal.Services;

/// <summary>Resultado de intentar guardar, para que quien llama sepa que decirle al usuario.</summary>
public enum SaveOutcome
{
    Saved,
    /// <summary>El archivo ya existia y el usuario no confirmo sobrescribir.</summary>
    CancelledByUser,
    Failed,
}

public sealed record SaveResult(SaveOutcome Outcome, string? Path, string? Error);

/// <summary>
/// Escritura de la captura en disco.
///
/// La regla que gobierna esta clase: <b>un fallo al guardar nunca puede perder la imagen</b>
/// (RNF-04). Por eso ningun metodo cierra ni descarta nada; solo informa de que paso y deja
/// que el editor siga en pantalla con la edicion intacta.
/// </summary>
public sealed class SaveService
{
    /// <summary>Patron del SRS 8. En el borrador 2 sera configurable.</summary>
    public static string BuildFileName(DateTime moment, string extension) =>
        $"Captura_{moment:yyyy-MM-dd_HH-mm-ss}{extension}";

    public static string SuggestPath(string folder, DateTime moment) =>
        Path.Combine(folder, BuildFileName(moment, ".png"));

    /// <summary>
    /// Guarda la imagen. <paramref name="confirmOverwrite"/> se consulta solo si el archivo ya
    /// existe: sobrescribir exige confirmacion explicita (regla de negocio del SRS 8).
    /// </summary>
    public SaveResult Save(BitmapSource image, string path, Func<string, bool> confirmOverwrite)
    {
        try
        {
            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            if (File.Exists(path) && !confirmOverwrite(path))
            {
                return new SaveResult(SaveOutcome.CancelledByUser, path, null);
            }

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            // Se escribe a un temporal y se intercambia: si falla a mitad, el archivo que ya
            // hubiese en esa ruta sigue entero en vez de quedar truncado.
            var temp = path + ".tmp";
            using (var stream = File.Create(temp))
            {
                encoder.Save(stream);
            }

            if (File.Exists(path))
            {
                File.Replace(temp, path, destinationBackupFileName: null);
            }
            else
            {
                File.Move(temp, path);
            }

            FileLogger.Info($"Captura guardada ({image.PixelWidth}x{image.PixelHeight} px).");
            return new SaveResult(SaveOutcome.Saved, path, null);
        }
        catch (UnauthorizedAccessException ex)
        {
            FileLogger.Error($"Sin permisos para guardar en la carpeta indicada. {Describe(ex)}");
            return new SaveResult(SaveOutcome.Failed, path,
                "No tienes permisos para escribir en esa carpeta. Elige otra ubicación; " +
                "la captura sigue abierta y no se ha perdido nada.");
        }
        catch (DirectoryNotFoundException ex)
        {
            FileLogger.Error($"La carpeta de guardado no existe. {Describe(ex)}");
            return new SaveResult(SaveOutcome.Failed, path,
                "La carpeta de guardado ya no existe. Elige otra ubicación; " +
                "la captura sigue abierta y no se ha perdido nada.");
        }
        catch (IOException ex)
        {
            FileLogger.Error($"Error de entrada/salida al guardar. {Describe(ex)}");
            return new SaveResult(SaveOutcome.Failed, path,
                $"No se pudo escribir el archivo: {ex.Message}\n\n" +
                "La captura sigue abierta y no se ha perdido nada.");
        }
        catch (Exception ex)
        {
            FileLogger.Error($"Fallo inesperado al guardar. {Describe(ex)}");
            return new SaveResult(SaveOutcome.Failed, path,
                $"No se pudo guardar: {ex.Message}\n\n" +
                "La captura sigue abierta y no se ha perdido nada.");
        }
    }

    /// <summary>
    /// Lo que se anota de un fallo al guardar: tipo y codigo, nunca <c>ex.Message</c>. Los
    /// mensajes de E/S de .NET llevan la ruta completa («Access to the path 'C:\...\Captura_....png'
    /// is denied»), y el log no puede contener el nombre de archivo de una captura.
    /// </summary>
    private static string Describe(Exception ex) => $"{ex.GetType().Name}, HResult 0x{ex.HResult:X8}";
}
