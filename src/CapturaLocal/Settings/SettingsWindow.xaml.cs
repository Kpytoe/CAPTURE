using System.IO;
using System.Windows;
using System.Windows.Input;
using CapturaLocal.Interop;

namespace CapturaLocal.Settings;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsStore _store;
    private readonly HotKeyManager _hotKeys;

    private HotkeyDefinition _pendingHotkey;

    public SettingsWindow(AppSettings settings, SettingsStore store, HotKeyManager hotKeys)
    {
        InitializeComponent();

        _settings = settings;
        _store = store;
        _hotKeys = hotKeys;

        _pendingHotkey = HotkeyDefinition.TryParse(settings.Hotkey, out var parsed) && parsed is not null
            ? parsed
            : HotkeyDefinition.Default;

        HotkeyBox.Text = _pendingHotkey.ToDisplayString();
        FolderBox.Text = settings.SaveFolder;
        LoggingCheck.IsChecked = settings.LoggingEnabled;

        UpdateHotkeyWarning();
        UpdateFolderWarning();

        SourceInitialized += (_, _) => WindowTheming.UseDarkTitleBar(this);
    }

    // --- Grabador de atajo ---

    private void HotkeyBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        HotkeyHelp.Text = "Pulsa ahora la combinación. Escape cancela la grabación.";
        HotkeyBox.SelectAll();
    }

    private void HotkeyBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        HotkeyHelp.Text = "Pon el foco en el cuadro y pulsa la combinación que quieras.";
    }

    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Tabulador fuera: si no, no habria forma de salir del cuadro con el teclado (RNF-06).
        if (e.Key is Key.Tab) return;

        e.Handled = true;

        if (e.Key == Key.Escape)
        {
            HotkeyBox.Text = _pendingHotkey.ToDisplayString();
            Keyboard.ClearFocus();
            return;
        }

        // Con Alt pulsado, WPF entrega la tecla real en SystemKey.
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (HotkeyDefinition.IsModifierKey(key))
        {
            // Aun esta componiendo la combinacion: se muestra el progreso sin aceptarla.
            HotkeyBox.Text = DescribeModifiers(Keyboard.Modifiers) + "...";
            return;
        }

        _pendingHotkey = new HotkeyDefinition(Keyboard.Modifiers, key);
        HotkeyBox.Text = _pendingHotkey.ToDisplayString();
        UpdateHotkeyWarning();
    }

    /// <summary>Muestra la combinacion a medio componer mientras el usuario mantiene teclas.</summary>
    private static string DescribeModifiers(ModifierKeys modifiers)
    {
        if (modifiers == ModifierKeys.None) return string.Empty;

        // Se reutiliza el mismo orden y nombres que el atajo definitivo para que el texto no
        // salte al soltar la ultima tecla.
        var partial = new HotkeyDefinition(modifiers, Key.None).ToDisplayString();
        return partial[..partial.LastIndexOf(" + ", StringComparison.Ordinal)] + " + ";
    }

    private void ResetHotkey_Click(object sender, RoutedEventArgs e)
    {
        _pendingHotkey = HotkeyDefinition.Default;
        HotkeyBox.Text = _pendingHotkey.ToDisplayString();
        UpdateHotkeyWarning();
    }

    private void UpdateHotkeyWarning()
    {
        if (!_pendingHotkey.HasStrongModifier && _pendingHotkey.HasAnyModifier)
        {
            HotkeyWarning.Text =
                $"Aviso: «{_pendingHotkey.ToDisplayString()}» no lleva Ctrl, Alt ni Win. " +
                "Un atajo así se queda esa tecla en todo el sistema y dejarás de poder escribirla " +
                "en cualquier programa.";
            HotkeyWarning.Visibility = Visibility.Visible;
        }
        else
        {
            HotkeyWarning.Visibility = Visibility.Collapsed;
        }
    }

    // --- Carpeta ---

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Elige la carpeta donde guardar las capturas",
            InitialDirectory = Directory.Exists(FolderBox.Text)
                ? FolderBox.Text
                : AppSettings.DefaultSaveFolder,
        };

        if (dialog.ShowDialog(this) == true)
        {
            FolderBox.Text = dialog.FolderName;
        }
    }

    private void FolderBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => UpdateFolderWarning();

    private void UpdateFolderWarning()
    {
        if (FolderWarning is null) return;

        if (AppSettings.IsInsideOneDrive(FolderBox.Text))
        {
            FolderWarning.Text =
                "Aviso: esta carpeta está dentro de OneDrive. Cada captura que guardes ahí se " +
                "subirá a la nube, que es justo lo que esta aplicación evita. Puedes continuar, " +
                "pero deja de ser una app 100 % local.";
            FolderWarning.Visibility = Visibility.Visible;
        }
        else
        {
            FolderWarning.Visibility = Visibility.Collapsed;
        }
    }

    // --- Guardar / cancelar ---

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var folder = FolderBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(folder))
        {
            Warn("Indica una carpeta de guardado.");
            return;
        }

        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception ex)
        {
            Warn($"No se puede usar esa carpeta:\n{folder}\n\n{ex.Message}");
            return;
        }

        // El atajo solo se aplica tras validarse contra Windows (regla de negocio SRS 8).
        // Si falla, TryRegister deja vigente el anterior y aqui no se guarda nada.
        if (_pendingHotkey.ToString() != _settings.Hotkey)
        {
            if (!_hotKeys.TryRegister(_pendingHotkey, out var error))
            {
                MessageBox.Show(this,
                    $"{error}\n\nSe conserva el atajo anterior: " +
                    $"{(_hotKeys.Current?.ToDisplayString() ?? "ninguno")}.",
                    "No se pudo registrar el atajo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _settings.Hotkey = _pendingHotkey.ToString();
        }

        _settings.SaveFolder = folder;
        _settings.LoggingEnabled = LoggingCheck.IsChecked == true;
        Services.FileLogger.Enabled = _settings.LoggingEnabled;

        _store.Save(_settings);

        // Sin DialogResult: la ventana se abre con Show(), no con ShowDialog(), y asignarlo en
        // una ventana no modal lanza InvalidOperationException (la app moria al pulsar Guardar).
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void Warn(string message) =>
        MessageBox.Show(this, message, "Captura Local", MessageBoxButton.OK, MessageBoxImage.Warning);
}
