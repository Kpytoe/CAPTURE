using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using CapturaLocal.Services;
using CapturaLocal.Settings;

using Forms = System.Windows.Forms;

namespace CapturaLocal;

public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\CapturaLocal.SingleInstance";

    private Mutex? _singleInstance;
    private Forms::NotifyIcon? _tray;
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
                "Captura Local ya se esta ejecutando. Busca su icono en la bandeja del sistema.",
                "Captura Local", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _store = new SettingsStore();
        _settings = _store.Load();
        FileLogger.Enabled = _settings.LoggingEnabled;
        FileLogger.Info("Aplicacion iniciada.");

        CreateTrayIcon();
    }

    private void CreateTrayIcon()
    {
        var menu = new Forms::ContextMenuStrip();
        menu.Items.Add("Nueva captura", null, (_, _) => OnNewCapture());
        menu.Items.Add("Configuracion...", null, (_, _) => OnOpenSettings());
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
        // Se conecta en el Paso 4 (captura de region).
        Notify("La captura de region llega en el siguiente paso del borrador 1.");
    }

    private void OnOpenSettings()
    {
        // Se conecta en el Paso 3 (atajo configurable).
        Notify("La ventana de Configuracion llega en el siguiente paso del borrador 1.");
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

        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
