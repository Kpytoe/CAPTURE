using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
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

        // Permite abrir Configuracion desde un acceso directo, y hace la ventana verificable
        // sin depender de que alguien abra el menu de bandeja con el raton.
        if (e.Args.Contains("--configuracion", StringComparer.OrdinalIgnoreCase))
        {
            OnOpenSettings();
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

    private void OnNewCapture()
    {
        FileLogger.Info("Captura solicitada.");

        // Se conecta en el Paso 4 (captura de region).
        Notify("La captura de region llega en el siguiente paso del borrador 1.");
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
