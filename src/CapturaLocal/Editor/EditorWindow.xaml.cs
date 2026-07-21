using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CapturaLocal.Editor.Annotations;
using CapturaLocal.Services;
using CapturaLocal.Settings;

namespace CapturaLocal.Editor;

public partial class EditorWindow : Window
{
    private static readonly (string Nombre, Color Valor)[] Colores =
    {
        ("Rojo", Colors.Red),
        ("Amarillo", Colors.Gold),
        ("Verde", Colors.LimeGreen),
        ("Azul", Color.FromRgb(0x2F, 0x7F, 0xF9)),
        ("Negro", Colors.Black),
        ("Blanco", Colors.White),
    };

    private static readonly double[] Grosores = { 1, 2, 3, 5, 8, 12 };

    private readonly AnnotationCanvas _canvas;
    private readonly AppSettings _settings;
    private readonly SaveService _saveService = new();

    private TextBox? _textEditor;
    private Point _textOrigin;
    private bool _forceClose;

    public EditorWindow(BitmapSource capture, AppSettings settings)
    {
        InitializeComponent();

        _settings = settings;
        _canvas = new AnnotationCanvas(capture);
        _canvas.Changed += UpdateChrome;
        _canvas.TextRequested += BeginTextEntry;
        CanvasHost.Children.Add(_canvas);

        foreach (var color in Colores) ColorPicker.Items.Add(color.Nombre);
        ColorPicker.SelectedIndex = 0;

        foreach (var grosor in Grosores) ThicknessPicker.Items.Add(grosor.ToString());
        ThicknessPicker.SelectedIndex = 2;

        Title = $"Captura Local — {capture.PixelWidth} × {capture.PixelHeight} px";
        UpdateChrome();

        Loaded += (_, _) =>
        {
            FitZoomToWindow(capture);
            _canvas.Focus();
        };
    }

    // --- Herramientas ---

    private void Tool_Click(object sender, RoutedEventArgs e)
    {
        var clicked = (System.Windows.Controls.Primitives.ToggleButton)sender;
        SelectTool(Enum.Parse<EditorTool>((string)clicked.Tag));
    }

    private void SelectTool(EditorTool tool)
    {
        CommitTextEntry();

        _canvas.Tool = tool;

        // Los ToggleButton actuan como grupo de opciones excluyentes.
        ToolSelect.IsChecked = tool == EditorTool.Select;
        ToolPencil.IsChecked = tool == EditorTool.Pencil;
        ToolRectangle.IsChecked = tool == EditorTool.Rectangle;
        ToolText.IsChecked = tool == EditorTool.Text;
        ToolPixelate.IsChecked = tool == EditorTool.Pixelate;

        _canvas.Cursor = tool == EditorTool.Select ? Cursors.Arrow : Cursors.Cross;
    }

    private void Color_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ColorPicker.SelectedIndex >= 0)
        {
            _canvas.Color = Colores[ColorPicker.SelectedIndex].Valor;
        }
    }

    private void Thickness_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ThicknessPicker.SelectedIndex >= 0)
        {
            _canvas.Thickness = Grosores[ThicknessPicker.SelectedIndex];
            _canvas.FontSize = Math.Max(12, Grosores[ThicknessPicker.SelectedIndex] * 6);
        }
    }

    // --- Texto ---

    private void BeginTextEntry(Point origin)
    {
        CommitTextEntry();
        _textOrigin = origin;

        _textEditor = new TextBox
        {
            MinWidth = 140,
            FontSize = _canvas.FontSize,
            Foreground = new SolidColorBrush(_canvas.Color),
            Background = new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0)),
            BorderBrush = new SolidColorBrush(_canvas.Color),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(origin.X, origin.Y, 0, 0),
            AcceptsReturn = false,
        };

        _textEditor.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) { CommitTextEntry(); e.Handled = true; }
            else if (e.Key == Key.Escape) { CancelTextEntry(); e.Handled = true; }
        };

        CanvasHost.Children.Add(_textEditor);
        _textEditor.Focus();
    }

    private void CommitTextEntry()
    {
        if (_textEditor is null) return;

        var content = _textEditor.Text;
        RemoveTextEditor();

        if (string.IsNullOrWhiteSpace(content)) return;

        _canvas.AddText(new TextAnnotation
        {
            Origin = _textOrigin,
            Text = content,
            Color = _canvas.Color,
            FontSize = _canvas.FontSize,
        });
    }

    private void CancelTextEntry() => RemoveTextEditor();

    private void RemoveTextEditor()
    {
        if (_textEditor is null) return;

        CanvasHost.Children.Remove(_textEditor);
        _textEditor = null;
        _canvas.Focus();
    }

    // --- Deshacer / rehacer ---

    private void Undo_Click(object sender, RoutedEventArgs e) { CommitTextEntry(); _canvas.Undo.Undo(); }

    private void Redo_Click(object sender, RoutedEventArgs e) { CommitTextEntry(); _canvas.Undo.Redo(); }

    private void UpdateChrome()
    {
        UndoButton.IsEnabled = _canvas.Undo.CanUndo;
        RedoButton.IsEnabled = _canvas.Undo.CanRedo;

        StatusText.Text = _canvas.Undo.IsDirty
            ? "Cambios sin guardar"
            : "Sin cambios pendientes";
    }

    // --- Zoom (RF-06) ---

    private void FitZoomToWindow(BitmapSource capture)
    {
        // Solo se reduce: una captura pequena se muestra a tamano real, no ampliada y borrosa.
        var available = new Size(Scroller.ActualWidth - 60, Scroller.ActualHeight - 60);
        if (available.Width <= 0 || available.Height <= 0) return;

        var scale = Math.Min(available.Width / capture.PixelWidth,
                             available.Height / capture.PixelHeight);

        ApplyZoom(Math.Min(1.0, scale));
    }

    private void ApplyZoom(double scale)
    {
        scale = Math.Clamp(scale, 0.1, 8.0);
        Zoom.ScaleX = Zoom.ScaleY = scale;
    }

    private void Scroller_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Ctrl + rueda hace zoom; la rueda sola sigue desplazando, como en cualquier visor.
        if (Keyboard.Modifiers != ModifierKeys.Control) return;

        ApplyZoom(Zoom.ScaleX * (e.Delta > 0 ? 1.15 : 1 / 1.15));
        e.Handled = true;
    }

    // --- Salida ---

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        CommitTextEntry();

        if (ClipboardService.TryCopy(Exporter.Rasterize(_canvas), out var error))
        {
            StatusText.Text = "Copiado al portapapeles";
            return;
        }

        MessageBox.Show(this, error, "No se pudo copiar",
            MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Save_Click(object sender, RoutedEventArgs e) => SaveCapture();

    private bool SaveCapture()
    {
        CommitTextEntry();

        var image = Exporter.Rasterize(_canvas);
        var path = SaveService.SuggestPath(_settings.SaveFolder, DateTime.Now);

        var result = _saveService.Save(image, path, ConfirmOverwrite);

        switch (result.Outcome)
        {
            case SaveOutcome.Saved:
                _canvas.Undo.MarkSaved();
                StatusText.Text = $"Guardada en {result.Path}";
                ShowSavedConfirmation(result.Path!);
                return true;

            case SaveOutcome.CancelledByUser:
                return false;

            default:
                // La ventana NO se cierra: la edicion sigue viva en memoria (RNF-04).
                MessageBox.Show(this, result.Error, "No se pudo guardar",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
        }
    }

    private bool ConfirmOverwrite(string path) =>
        MessageBox.Show(this,
            $"Ya existe un archivo con este nombre:\n{path}\n\n¿Sobrescribirlo?",
            "El archivo ya existe",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private void ShowSavedConfirmation(string path)
    {
        var answer = MessageBox.Show(this,
            $"Captura guardada en:\n{path}\n\n¿Abrir la carpeta?",
            "Guardada", MessageBoxButton.YesNo, MessageBoxImage.Information);

        if (answer != MessageBoxResult.Yes) return;

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"")
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            FileLogger.Error("No se pudo abrir el explorador.", ex);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    // --- Atajos y cierre ---

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        // Mientras se escribe texto, las letras son texto y no atajos de herramienta.
        var typing = _textEditor is not null;

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.Z: _canvas.Undo.Undo(); e.Handled = true; return;
                case Key.Y: _canvas.Undo.Redo(); e.Handled = true; return;
                case Key.C: Copy_Click(this, new RoutedEventArgs()); e.Handled = true; return;
                case Key.S: SaveCapture(); e.Handled = true; return;
            }
        }

        if (typing) return;

        switch (e.Key)
        {
            case Key.S: SelectTool(EditorTool.Select); e.Handled = true; break;
            case Key.L: SelectTool(EditorTool.Pencil); e.Handled = true; break;
            case Key.R: SelectTool(EditorTool.Rectangle); e.Handled = true; break;
            case Key.T: SelectTool(EditorTool.Text); e.Handled = true; break;
            case Key.P: SelectTool(EditorTool.Pixelate); e.Handled = true; break;
            case Key.Escape: Close(); e.Handled = true; break;
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        base.OnClosing(e);

        if (_forceClose || !_canvas.Undo.IsDirty) return;

        // Cerrar con cambios pregunta antes de tirar el trabajo (RF-15).
        var answer = MessageBox.Show(this,
            "La captura tiene marcas sin guardar.\n\n" +
            "«Sí» la guarda, «No» la descarta y «Cancelar» vuelve al editor.",
            "Cambios sin guardar",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

        switch (answer)
        {
            case MessageBoxResult.Yes:
                if (!SaveCapture()) e.Cancel = true;
                else _forceClose = true;
                break;

            case MessageBoxResult.No:
                _forceClose = true;
                break;

            default:
                e.Cancel = true;
                break;
        }
    }
}
