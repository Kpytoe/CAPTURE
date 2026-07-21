using System.IO;
using System.Text;

namespace CapturaLocal.Services;

/// <summary>
/// Registro tecnico local. Nunca escribe contenido de imagen ni el nombre de archivo de una
/// captura: solo eventos y errores. Es local y borrable desde Configuracion (SRS 10).
/// </summary>
public static class FileLogger
{
    private static readonly object Gate = new();

    public static bool Enabled { get; set; } = true;

    public static string Directory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CapturaLocal", "logs");

    public static string FilePath => Path.Combine(Directory, "captura.log");

    public static void Info(string message) => Write("INFO ", message);

    public static void Warn(string message) => Write("WARN ", message);

    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : $"{message} :: {ex.GetType().Name}: {ex.Message}");

    /// <summary>Borra el registro. Expuesto para soporte (SRS 10).</summary>
    public static void Clear()
    {
        lock (Gate)
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch
            {
                // Borrar el log jamas debe tumbar la aplicacion.
            }
        }
    }

    private static void Write(string level, string message)
    {
        if (!Enabled) return;

        lock (Gate)
        {
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {level} {message}{Environment.NewLine}";
                File.AppendAllText(FilePath, line, Encoding.UTF8);
            }
            catch
            {
                // Un fallo al registrar no puede propagarse: el log es diagnostico, no funcion.
            }
        }
    }
}
