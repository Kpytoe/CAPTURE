using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using CapturaLocal.Services;

namespace CapturaLocal.Settings;

/// <summary>
/// Lee y escribe las preferencias en %LOCALAPPDATA%\CapturaLocal\settings.json.
/// Dos garantias de RNF-08: la escritura es atomica (nadie ve un archivo a medias aunque se
/// corte la luz) y un archivo ilegible nunca hace fallar el arranque: se aparta y se sigue
/// con los valores por defecto.
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        // El codificador por defecto escapa el '+' de los atajos a su secuencia unicode, y el
        // archivo queda con un "Ctrl\u00XXShift..." que nadie puede leer ni editar a mano. Ese
        // escapado agresivo existe para incrustar JSON dentro de HTML; esto es un archivo de
        // texto local que el usuario puede abrir, asi que se relaja.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Directory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CapturaLocal");

    public static string FilePath => Path.Combine(Directory, "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                var fresh = new AppSettings();
                Save(fresh);
                FileLogger.Info("No habia preferencias previas; se crearon las de fabrica.");
                return fresh;
            }

            var json = File.ReadAllText(FilePath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

            if (loaded is null)
            {
                return Quarantine("el archivo de preferencias estaba vacio");
            }

            return Migrate(loaded);
        }
        catch (JsonException ex)
        {
            return Quarantine($"el archivo de preferencias no es JSON valido: {ex.Message}");
        }
        catch (Exception ex)
        {
            FileLogger.Error("No se pudieron leer las preferencias; se usan las de fabrica.", ex);
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        System.IO.Directory.CreateDirectory(Directory);

        // Escritura atomica: se vuelca a un temporal y se intercambia. Si el proceso muere a
        // media escritura, el settings.json anterior sigue intacto.
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, JsonOptions));

        if (File.Exists(FilePath))
        {
            File.Replace(temp, FilePath, destinationBackupFileName: null);
        }
        else
        {
            File.Move(temp, FilePath);
        }
    }

    /// <summary>
    /// Lleva un archivo de una version anterior al esquema actual. Hoy no hay migraciones
    /// porque solo existe la version 1; el gancho existe para que la primera no llegue tarde.
    /// </summary>
    private AppSettings Migrate(AppSettings settings)
    {
        if (settings.SchemaVersion > AppSettings.CurrentSchemaVersion)
        {
            // Lo escribio una version mas nueva de la app. No se destruye: se respeta lo que
            // se entiende y se deja el archivo tal cual esta en disco.
            FileLogger.Warn($"Las preferencias son de un esquema mas nuevo " +
                            $"(v{settings.SchemaVersion} > v{AppSettings.CurrentSchemaVersion}).");
            return settings;
        }

        if (settings.SchemaVersion < AppSettings.CurrentSchemaVersion)
        {
            // Aqui iran las migraciones v1 -> v2 -> ... cuando existan.
            settings.SchemaVersion = AppSettings.CurrentSchemaVersion;
            Save(settings);
            FileLogger.Info($"Preferencias migradas al esquema v{AppSettings.CurrentSchemaVersion}.");
        }

        return settings;
    }

    private AppSettings Quarantine(string reason)
    {
        try
        {
            var backup = Path.Combine(Directory,
                $"settings.corrupto-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Move(FilePath, backup, overwrite: true);
            FileLogger.Warn($"Preferencias apartadas ({reason}). Copia en: {backup}");
        }
        catch (Exception ex)
        {
            FileLogger.Error("No se pudo apartar el archivo de preferencias danado.", ex);
        }

        var defaults = new AppSettings();
        Save(defaults);
        return defaults;
    }
}
