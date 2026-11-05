using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CapturaLocal.Editor.Annotations;
using CapturaLocal.Interop;
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

    private readonly List<ToggleButton> _colorSwatches = new();
    private readonly List<ToggleButton> _thicknessSwatches = new();

    private TextBox? _textEditor;

    /// <summary>Figura que activa el boton de figuras: la ultima elegida en su menu.</summary>
    private EditorTool _shape = EditorTool.Rectangle;
    private bool _forceClose;
    private string? _lastSavedPath;

    public EditorWindow(BitmapSource capture, AppSettings settings)
    {
        InitializeComponent();

        _settings = settings;
        _canvas = new AnnotationCanvas(capture);
        _canvas.Changed += UpdateChrome;
        _canvas.TextRequested += BeginTextEntry;
        CanvasHost.Children.Add(_canvas);

        BuildSwatches();
        SelectColor(0);
        SelectThickness(2);

        Title = $"Captura Local — {capture.PixelWidth} × {capture.PixelHeight} px";
        UpdateChrome();

        SourceInitialized += (_, _) => WindowTheming.UseDarkTitleBar(this);

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
        ToolText.IsChecked = tool == EditorTool.Text;
        ToolPixelate.IsChecked = tool == EditorTool.Pixelate;

        var isShape = tool is EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line;
        if (isShape) ShowShape(tool);
        ToolShape.IsChecked = isShape;

        // En la herramienta de texto el grosor gobierna el tamano de letra, asi que la
        // etiqueta dice lo que de verdad hace.
        ThicknessLabel.Text = tool == EditorTool.Text ? "Tamaño" : "Grosor";

        _canvas.Cursor = tool == EditorTool.Select ? Cursors.Arrow : Cursors.Cross;
    }

    // --- Figuras ---

    /// <summary>
    /// El boton de figuras muestra la figura vigente: un clic la activa sin abrir el menu, que
    /// es lo que se repite al marcar varias cosas seguidas con la misma forma.
    /// </summary>
    private void ShowShape(EditorTool shape)
    {
        _shape = shape;

        var (etiqueta, icono) = shape switch
        {
            EditorTool.Ellipse => ("Círculo", "IconoElipse"),
            EditorTool.Line => ("Línea", "IconoLinea"),
            _ => ("Rectángulo", "IconoRectangulo"),
        };

        ToolShape.Tag = shape.ToString();
        ShapeIcon.Data = (Geometry)FindResource(icono);
        ShapeLabel.Text = etiqueta;
        AutomationProperties.SetName(ToolShape, $"Herramienta {etiqueta.ToLowerInvariant()}");

        ShapeRectangle.IsChecked = shape == EditorTool.Rectangle;
        ShapeEllipse.IsChecked = shape == EditorTool.Ellipse;
        ShapeLine.IsChecked = shape == EditorTool.Line;
    }

    private void ShapeMenu_Click(object sender, RoutedEventArgs e) => ShapePopup.IsOpen = true;

    private void ShapeOption_Click(object sender, RoutedEventArgs e)
    {
        ShapePopup.IsOpen = false;
        SelectTool(Enum.Parse<EditorTool>((string)((ToggleButton)sender).Tag));
    }

    private void ShapePopup_Opened(object sender, EventArgs e)
    {
        // Mientras el menu esta abierto, el boton que lo abre no recibe clics: el clic fuera
        // del menu ya lo cierra (StaysOpen=False), y si el boton lo recibiera lo volveria a abrir.
        ShapeMenuButton.IsChecked = true;
        ShapeMenuButton.IsHitTestVisible = false;

        var current = _shape switch
        {
            EditorTool.Ellipse => ShapeEllipse,
            EditorTool.Line => ShapeLine,
            _ => ShapeRectangle,
        };

        // El foco entra en el menu para poder elegir con Tab y Espacio (RNF-06).
        Dispatcher.BeginInvoke(() => current.Focus());
    }

    private void ShapePopup_Closed(object sender, EventArgs e)
    {
        ShapeMenuButton.IsChecked = false;
        ShapeMenuButton.IsHitTestVisible = true;
    }

    // --- Color y grosor ---

    /// <summary>
    /// Genera las muestras a partir de <see cref="Colores"/> y <see cref="Grosores"/>, que
    /// siguen siendo la unica fuente de verdad: anadir un color es anadir una fila al array.
    /// </summary>
    private void BuildSwatches()
    {
        for (var i = 0; i < Colores.Length; i++)
        {
            var (nombre, valor) = Colores[i];

            var relleno = new SolidColorBrush(valor);
            relleno.Freeze();

            var muestra = new ToggleButton
            {
                Style = (Style)FindResource("MuestraColor"),
                Background = relleno,
                Tag = i,
                ToolTip = nombre,
            };

            AutomationProperties.SetName(muestra, $"Color {nombre}");
            muestra.Click += Color_Click;

            ColorSwatches.Children.Add(muestra);
            _colorSwatches.Add(muestra);
        }

        for (var i = 0; i < Grosores.Length; i++)
        {
            var grosor = Grosores[i];

            // Un punto del tamano que representa. El diametro no es el grosor literal: 1 px
            // seria invisible y 12 px no cabria, asi que se comprime manteniendo el orden.
            var punto = new Ellipse
            {
                Width = Math.Min(18, 4 + grosor),
                Height = Math.Min(18, 4 + grosor),
            };

            // El punto se pinta con el Foreground de su boton, asi que sigue los estados
            // (reposo, hover, activo) sin repetir los colores aqui.
            punto.SetBinding(Shape.FillProperty, new Binding("Foreground")
            {
                RelativeSource = new RelativeSource(
                    RelativeSourceMode.FindAncestor, typeof(ToggleButton), 1),
            });

            var muestra = new ToggleButton
            {
                Style = (Style)FindResource("MuestraGrosor"),
                Content = punto,
                Tag = i,
                ToolTip = $"Grosor {grosor}",
            };

            AutomationProperties.SetName(muestra, $"Grosor {grosor}");
            muestra.Click += Thickness_Click;

            ThicknessSwatches.Children.Add(muestra);
            _thicknessSwatches.Add(muestra);
        }
    }

    private void Color_Click(object sender, RoutedEventArgs e) =>
        SelectColor((int)((ToggleButton)sender).Tag);

    private void Thickness_Click(object sender, RoutedEventArgs e) =>
        SelectThickness((int)((ToggleButton)sender).Tag);

    private void SelectColor(int index)
    {
        _canvas.Color = Colores[index].Valor;

        // Se asigna siempre: sin esto, volver a pulsar la muestra ya activa la desmarcaria y
        // el color quedaria seleccionado sin que se vea cual es.
        for (var i = 0; i < _colorSwatches.Count; i++) _colorSwatches[i].IsChecked = i == index;
    }

    private void SelectThickness(int index)
    {
        _canvas.Thickness = Grosores[index];
        _canvas.FontSize = Math.Max(12, Grosores[index] * 6);

        for (var i = 0; i < _thicknessSwatches.Count; i++)
        {
            _thicknessSwatches[i].IsChecked = i == index;
        }
    }

    // --- Texto ---

    /// <summary>
    /// Abre el cuadro donde se escribe. Si el usuario arrastro, el cuadro tiene ese ancho y ese
    /// alto inicial: el texto salta de linea al llegar al borde y el cuadro crece hacia abajo.
    /// Si solo hizo clic, el cuadro empieza estrecho y se ensancha con el texto.
    /// </summary>
    private void BeginTextEntry(Rect area)
    {
        CommitTextEntry();

        // Lo que queda de imagen a la derecha. El cuadro no pasa de ahi: si lo hiciera,
        // ensancharia la rejilla que contiene el lienzo y la captura se desplazaria.
        var room = Math.Max(20, _canvas.Width - area.X);

        _textEditor = new TextBox
        {
            FontFamily = TextAnnotation.Family,
            FontSize = _canvas.FontSize,
            Foreground = new SolidColorBrush(_canvas.Color),
            Background = new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0)),
            BorderBrush = new SolidColorBrush(_canvas.Color),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            VerticalContentAlignment = VerticalAlignment.Top,
            Margin = new Thickness(area.X, area.Y, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = room,
            AcceptsReturn = false,
        };

        // Ideal es el modo con el que FormattedText mide al dibujar. Con el modo Display de la
        // ventana las letras miden distinto, y una palabra podria saltar de linea al confirmar.
        TextOptions.SetTextFormattingMode(_textEditor, TextFormattingMode.Ideal);

        // Un cuadro donde no caben ni unas pocas letras no se respeta como ancho fijo: el texto
        // se partiria letra a letra. Se toma como tamano minimo y el cuadro crece con lo escrito
        // hasta el borde de la imagen, donde empieza a saltar de linea.
        var tooNarrow = area.Width < _canvas.FontSize * 4;

        if (area.Width > 0 && !tooNarrow)
        {
            _textEditor.Width = Math.Min(area.Width, room);
            _textEditor.MinHeight = area.Height;
        }
        else
        {
            // Clic sin arrastre: un ancho de partida comodo. Arrastre corto: el que se dibujo.
            _textEditor.MinWidth = Math.Min(area.Width > 0 ? area.Width : 140, room);
            _textEditor.MinHeight = area.Height;
        }

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

        var editor = _textEditor;
        if (string.IsNullOrWhiteSpace(editor.Text))
        {
            RemoveTextEditor();
            return;
        }

        // Se copia lo que el usuario ve, no lo que el codigo supone que vera: las lineas tal
        // como las partio el cuadro, y la posicion real de la primera letra (borde y relleno
        // incluidos). Asi el texto no se mueve ni cambia de forma al confirmarlo.
        editor.UpdateLayout();

        var content = editor.LineCount > 0
            ? string.Join("\n", Enumerable.Range(0, editor.LineCount)
                .Select(i => editor.GetLineText(i).TrimEnd()))
            : editor.Text;

        var firstLetter = editor.GetRectFromCharacterIndex(0);
        var origin = firstLetter.IsEmpty
            ? new Point(editor.Margin.Left, editor.Margin.Top)
            : editor.TranslatePoint(firstLetter.TopLeft, _canvas);

        RemoveTextEditor();

        _canvas.AddText(new TextAnnotation
        {
            Origin = origin,
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

        if (_canvas.Undo.IsDirty)
        {
            StatusText.Text = "Cambios sin guardar";

            // Lo guardado ya no es lo que se ve: el enlace llevaria a un archivo desactualizado.
            OpenFolderButton.Visibility = Visibility.Collapsed;
            return;
        }

        StatusText.Text = _lastSavedPath is null
            ? "Sin cambios pendientes"
            : $"Guardada en {_lastSavedPath}";
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
            // Copiar cierra el editor: el usuario ya tiene la imagen en el portapapeles y la
            // edicion era el paso previo. El lienzo sigue "sucio", asi que forzamos el cierre
            // para no disparar el aviso de cambios sin guardar (RF-15) — el trabajo esta a salvo.
            _forceClose = true;
            Close();
            return;
        }

        MessageBox.Show(this, error, "No se pudo copiar",
            MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Guardar con exito tambien cierra: MarkSaved dejo el lienzo limpio, asi que Close no
        // vuelve a preguntar. Si el guardado falla o se cancela, seguimos en el editor.
        if (SaveCapture()) Close();
    }

    private bool SaveCapture()
    {
        CommitTextEntry();

        var image = Exporter.Rasterize(_canvas);
        var path = SaveService.SuggestPath(_settings.SaveFolder, DateTime.Now);

        var result = _saveService.Save(image, path, ConfirmOverwrite);

        switch (result.Outcome)
        {
            case SaveOutcome.Saved:
                // El orden importa: MarkSaved dispara Changed y UpdateChrome lee esta ruta.
                _lastSavedPath = result.Path;
                _canvas.Undo.MarkSaved();
                OpenFolderButton.Visibility = Visibility.Visible;
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

    /// <summary>
    /// Guardar no interrumpe: antes abria un dialogo modal preguntando si abrir la carpeta.
    /// Ahora la ruta se anuncia en la barra inferior y abrirla es una accion opcional, que es
    /// lo que se espera de un guardado que ha ido bien.
    /// </summary>
    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_lastSavedPath is null) return;

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_lastSavedPath}\"")
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

        // Esc con el menu de figuras abierto cierra el menu, no el editor.
        if (ShapePopup.IsOpen && e.Key == Key.Escape)
        {
            ShapePopup.IsOpen = false;
            ShapeMenuButton.Focus();
            e.Handled = true;
            return;
        }

        // Mientras se escribe texto, las letras son texto y no atajos de herramienta.
        var typing = _textEditor is not null;

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.Z: _canvas.Undo.Undo(); e.Handled = true; return;
                case Key.Y: _canvas.Undo.Redo(); e.Handled = true; return;
                case Key.C: Copy_Click(this, new RoutedEventArgs()); e.Handled = true; return;
                case Key.S: Save_Click(this, new RoutedEventArgs()); e.Handled = true; return;
            }
        }

        if (typing) return;

        switch (e.Key)
        {
            case Key.S: SelectTool(EditorTool.Select); e.Handled = true; break;
            case Key.L: SelectTool(EditorTool.Pencil); e.Handled = true; break;
            case Key.R: SelectTool(EditorTool.Rectangle); e.Handled = true; break;
            case Key.C: SelectTool(EditorTool.Ellipse); e.Handled = true; break;
            case Key.N: SelectTool(EditorTool.Line); e.Handled = true; break;
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
