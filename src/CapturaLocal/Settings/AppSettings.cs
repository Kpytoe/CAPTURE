using System.IO;

namespace CapturaLocal.Settings;

/// <summary>
/// Preferencias del usuario. Se serializan tal cual a settings.json, asi que anadir una
/// propiedad es compatible hacia atras: al leer un archivo viejo simplemente toma su valor
/// por defecto. Quitar o cambiar el significado de una propiedad exige subir
/// <see cref="CurrentSchemaVersion"/> y escribir su migracion en SettingsStore.
/// </summary>
public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Atajo global, en el formato de <c>HotkeyDefinition</c> (por ejemplo "Ctrl+Shift+D2").</summary>
    public string Hotkey { get; set; } = "Ctrl+Shift+D2";

    /// <summary>Carpeta donde se guardan las capturas.</summary>
    public string SaveFolder { get; set; } = DefaultSaveFolder;

    /// <summary>Formato de exportacion. El borrador 1 solo entiende "Png".</summary>
    public string Format { get; set; } = "Png";

    public bool LoggingEnabled { get; set; } = true;

    /// <summary>
    /// %USERPROFILE%\Capturas, deliberadamente NO la carpeta Imagenes: en equipos con
    /// carpetas conocidas redirigidas, Imagenes apunta a OneDrive y cada captura se subiria
    /// a la nube corporativa, rompiendo RNF-03. Ver ADR-003 en docs\DECISIONS.md.
    /// </summary>
    public static string DefaultSaveFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Capturas");

    /// <summary>
    /// Indica si una ruta cae dentro del arbol sincronizado de OneDrive. No se bloquea al
    /// usuario: se le advierte, porque guardar ahi contradice la promesa de privacidad.
    /// </summary>
    public static bool IsInsideOneDrive(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        foreach (var variable in new[] { "OneDrive", "OneDriveCommercial", "OneDriveConsumer" })
        {
            var root = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(root)) continue;

            var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            var normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

            if (normalizedPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar,
                                          StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public AppSettings Clone() => (AppSettings)MemberwiseClone();
}
