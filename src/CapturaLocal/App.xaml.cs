using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CapturaLocal.Interop;
using CapturaLocal.Services;
using CapturaLocal.Settings;

using Forms = System.Windows.Forms;

namespace CapturaLocal;

public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\CapturaLocal.SingleInstance";

    private Mutex? _singleInstance;
    private Forms::NotifyIcon? _tray;
    private HotKeyManager? _hotKeys;
    private SettingsWindow? _settingsWindow;
    private SettingsStore _store = null!;
    private AppSettings _settings = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Render por software (ADR-008): el dispositivo Direct3D de WPF cuesta ~40 MB al arrancar
        // y ~50 MB mas tras la primera ventana a pantalla completa, que no se devuelven nunca.
        // Una app que vive en la bandeja no los amortiza.
        System.Windows.Media.RenderOptions.ProcessRenderMode =
            System.Windows.Interop.RenderMode.SoftwareOnly;

        // Dos instancias competirian por el mismo atajo global y la segunda fallaria con un
        // mensaje incomprensible ("atajo ya en uso" ... por ella misma).
        _singleInstance = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var isFirst);
        if (!isFirst)
        {
            MessageBox.Show(
                "Captura Local ya se está ejecutando. Busca su icono en la bandeja del sistema.",
                "Captura Local", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _store = new SettingsStore();
        _settings = _store.Load();
        FileLogger.Enabled = _settings.LoggingEnabled;
        FileLogger.Info("Aplicacion iniciada.");

        CreateTrayIcon();
        RegisterConfiguredHotkey();

        // Permiten arrancar una accion desde un acceso directo propio del usuario, y de paso
        // hacen la aplicacion verificable sin depender del menu de bandeja.
        if (e.Args.Contains("--configuracion", StringComparer.OrdinalIgnoreCase))
        {
            OnOpenSettings();
        }
        else if (e.Args.Contains("--capturar", StringComparer.OrdinalIgnoreCase))
        {
            OnNewCapture();
        }
    }

    /// <summary>
    /// Registra el atajo guardado. Si Windows lo rechaza (otra aplicacion se le adelanto desde
    /// la ultima sesion), no se cambia el archivo de preferencias: se avisa y se deja al usuario
    /// entrar en Configuracion a elegir otro (SRS 12).
    /// </summary>
    private void RegisterConfiguredHotkey()
    {
        _hotKeys = new HotKeyManager();
        _hotKeys.Pressed += OnNewCapture;

        var hotkey = HotkeyDefinition.TryParse(_settings.Hotkey, out var parsed) && parsed is not null
            ? parsed
            : HotkeyDefinition.Default;

        if (_hotKeys.TryRegister(hotkey, out var error))
        {
            if (_tray is not null)
            {
                _tray.Text = $"Captura Local - {hotkey.ToDisplayString()}";
            }
            return;
        }

        FileLogger.Warn($"Arranque sin atajo activo: {error}");
        Notify($"No se pudo activar {hotkey.ToDisplayString()}. {error} " +
               "Abre Configuración para elegir otra combinación.");
    }

    private void CreateTrayIcon()
    {
        var menu = new Forms::ContextMenuStrip();
        menu.Items.Add("Nueva captura", null, (_, _) => OnNewCapture());
        menu.Items.Add("Configuración…", null, (_, _) => OnOpenSettings());
        menu.Items.Add("Abrir carpeta de guardado", null, (_, _) => OnOpenSaveFolder());
        menu.Items.Add(new Forms::ToolStripSeparator());
        menu.Items.Add("Salir", null, (_, _) => OnExit());

        _tray = new Forms::NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = "Captura Local",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _tray.DoubleClick += (_, _) => OnNewCapture();
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        var uri = new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute);
        using var stream = GetResourceStream(uri)!.Stream;
        return new System.Drawing.Icon(stream);
    }

    private async void OnNewCapture()
    {
        FileLogger.Info("Captura solicitada.");

        try
        {
            var image = await Capture.CaptureService.CaptureRegionAsync();
            if (image is null)
            {
                ReleaseMemoryWhenIdle();
                return;
            }

            var editor = new Editor.EditorWindow(image, _settings);
            editor.Closed += (_, _) => ReleaseMemoryWhenIdle();
            editor.Show();
            editor.Activate();
        }
        catch (Exception ex)
        {
            FileLogger.Error("No se pudo completar la captura.", ex);
            MessageBox.Show($"No se pudo completar la captura.\n\n{ex.Message}",
                "Captura Local", MessageBoxButton.OK, MessageBoxImage.Error);
            ReleaseMemoryWhenIdle();
        }
    }

    /// <summary>
    /// Devuelve al sistema la memoria de la captura cuando ya no queda ninguna ventana abierta.
    ///
    /// Tras cerrar el overlay o el editor no queda ninguna referencia viva a las imagenes, pero
    /// la app en la bandeja no asigna nada, asi que el recolector no vuelve a pasar por si solo:
    /// las copias del escritorio (unos 4 bytes por pixel cada una, casi todo memoria nativa que
    /// solo se suelta al finalizar los objetos) se quedaban en el proceso hasta la siguiente
    /// captura. Aqui se fuerza esa pasada, en reposo y con el hilo de interfaz libre.
    /// </summary>
    private void ReleaseMemoryWhenIdle()
    {
        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
        {
            // Con un editor todavia abierto no merece la pena la pausa: se hara al cerrarlo.
            if (Windows.Count > 0) return;

            // El monton administrado es pequeno (2-5 MB medidos); lo que pesa son los mapas de
            // bits nativos que cuelgan de objetos ya inalcanzables, y esos solo se liberan
            // cuando corren sus finalizadores.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        });
    }

    private void OnOpenSettings()
    {
        // Una sola ventana de Configuracion: si ya esta abierta, se trae al frente.
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings, _store, _hotKeys!);
        _settingsWindow.Closed += (_, _) =>
        {
            _settingsWindow = null;
            if (_tray is not null && _hotKeys?.Current is not null)
            {
                _tray.Text = $"Captura Local - {_hotKeys.Current.ToDisplayString()}";
            }

            ReleaseMemoryWhenIdle();
        };
        _settingsWindow.Show();
    }

    private void OnOpenSaveFolder()
    {
        try
        {
            Directory.CreateDirectory(_settings.SaveFolder);
            Process.Start(new ProcessStartInfo(_settings.SaveFolder) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            FileLogger.Error("No se pudo abrir la carpeta de guardado.", ex);
            MessageBox.Show(
                $"No se pudo abrir la carpeta de guardado:\n{_settings.SaveFolder}\n\n{ex.Message}",
                "Captura Local", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnExit()
    {
        FileLogger.Info("Aplicacion cerrada por el usuario.");
        Shutdown();
    }

    private void Notify(string message)
    {
        _tray?.ShowBalloonTip(3000, "Captura Local", message, Forms::ToolTipIcon.Info);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Sin esto el icono se queda como fantasma en la bandeja hasta que el usuario pasa
        // el raton por encima.
        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }

        _hotKeys?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
