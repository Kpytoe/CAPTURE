using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CapturaLocal.Editor.Annotations;

namespace CapturaLocal.Editor;

/// <summary>
/// El lienzo: dibuja la captura con sus anotaciones y traduce el raton en acciones.
///
/// Es un FrameworkElement con OnRender propio en vez de un Canvas con formas de WPF. La razon
/// es que asi <b>el mismo codigo</b> pinta la pantalla y el archivo exportado: no hay forma de
/// que lo que se ve y lo que se guarda diverjan (ADR-005).
/// </summary>
public sealed class AnnotationCanvas : FrameworkElement
{
    private readonly BitmapSource _image;
    private readonly List<Annotation> _annotations = new();
    private readonly UndoStack _undo;

    private Annotation? _inProgress;
    private Annotation? _selected;

    private bool _isMoving;
    private Point _moveStart;
    private Vector _moveAccumulated;

    /// <summary>
    /// Punto donde empezo el arrastre. Se guarda aparte porque el rectangulo en curso se
    /// normaliza en cada movimiento: leer su esquina como ancla haria que el origen se fuese
    /// desplazando en cuanto el usuario arrastrase hacia arriba o hacia la izquierda.
    /// </summary>
    private Point _dragAnchor;

    public AnnotationCanvas(BitmapSource image)
    {
        _image = image;
        _undo = new UndoStack(_annotations);
        _undo.Changed += () => { InvalidateVisual(); Changed?.Invoke(); };

        Width = image.PixelWidth;
        Height = image.PixelHeight;
        Focusable = true;
        ClipToBounds = true;
    }

    public UndoStack Undo => _undo;

    public BitmapSource Image => _image;

    public EditorTool Tool { get; set; } = EditorTool.Pencil;

    public Color Color { get; set; } = Colors.Red;

    public double Thickness { get; set; } = 3;

    public double FontSize { get; set; } = 20;

    /// <summary>Avisa de cualquier cambio para refrescar botones y titulo de la ventana.</summary>
    public event Action? Changed;

    /// <summary>Pide a la ventana que abra el editor de texto en un punto.</summary>
    public event Action<Point>? TextRequested;

    public Annotation? Selected
    {
        get => _selected;
        private set { _selected = value; InvalidateVisual(); Changed?.Invoke(); }
    }

    // --- Dibujado ---

    protected override void OnRender(DrawingContext dc)
    {
        RenderContent(dc);

        // El marcador de seleccion es ayuda visual del editor: no forma parte de la imagen y
        // por eso no esta en RenderContent, que es lo unico que se exporta.
        if (_selected is not null)
        {
            var bounds = _selected.Bounds;
            bounds.Inflate(3, 3);

            var pen = new Pen(new SolidColorBrush(Color.FromRgb(0x2F, 0x7F, 0xF9)), 1.5)
            {
                DashStyle = new DashStyle(new double[] { 4, 3 }, 0),
            };

            dc.DrawRectangle(null, pen, bounds);
        }
    }

    /// <summary>Imagen + anotaciones. Es exactamente lo que se copia y lo que se guarda.</summary>
    public void RenderContent(DrawingContext dc)
    {
        dc.DrawImage(_image, new Rect(0, 0, _image.PixelWidth, _image.PixelHeight));

        foreach (var annotation in _annotations)
        {
            annotation.Render(dc);
        }

        _inProgress?.Render(dc);
    }

    // --- Raton ---

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var point = e.GetPosition(this);

        if (Tool == EditorTool.Select)
        {
            // De arriba abajo: gana la anotacion dibujada mas recientemente, que es la que el
            // usuario ve encima.
            Selected = _annotations.LastOrDefault(a => a.HitTest(point));

            if (Selected is not null)
            {
                _isMoving = true;
                _moveStart = point;
                _moveAccumulated = default;
                CaptureMouse();
            }

            return;
        }

        Selected = null;
        _dragAnchor = point;

        switch (Tool)
        {
            case EditorTool.Pencil:
                var pencil = new PencilAnnotation { Color = Color, Thickness = Thickness };
                pencil.AddPoint(point);
                _inProgress = pencil;
                break;

            case EditorTool.Rectangle:
                _inProgress = new RectangleAnnotation
                {
                    Color = Color,
                    Thickness = Thickness,
                    Rect = new Rect(point, point),
                };
                break;

            case EditorTool.Pixelate:
                _inProgress = new PixelateAnnotation(_image) { Rect = new Rect(point, point) };
                break;

            case EditorTool.Text:
                TextRequested?.Invoke(point);
                return;
        }

        if (_inProgress is not null) CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var point = e.GetPosition(this);

        if (_isMoving && _selected is not null)
        {
            var delta = point - _moveStart;
            _selected.Move(delta);
            _moveAccumulated += delta;
            _moveStart = point;
            InvalidateVisual();
            return;
        }

        switch (_inProgress)
        {
            case PencilAnnotation pencil:
                pencil.AddPoint(point);
                break;

            case RectangleAnnotation rectangle:
                rectangle.Rect = Normalize(_dragAnchor, point);
                break;

            case PixelateAnnotation pixelate:
                pixelate.Rect = Normalize(_dragAnchor, point);
                break;

            default:
                return;
        }

        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        ReleaseMouseCapture();

        if (_isMoving)
        {
            _isMoving = false;

            // El arrastre entero es UNA accion de deshacer, no una por cada pixel movido.
            // Ya esta aplicado sobre la anotacion, asi que se registra sin volver a aplicarlo.
            if (_moveAccumulated.Length > 0.5 && _selected is not null)
            {
                _undo.DoWithoutApplying(new MoveAnnotationAction(_selected, _moveAccumulated));
            }

            return;
        }

        if (_inProgress is null) return;

        var finished = _inProgress;
        _inProgress = null;

        // Trazos vacios y rectangulos de tamano cero son clics accidentales: no ensucian el
        // historial de deshacer.
        var isUseless = finished switch
        {
            PencilAnnotation pencil => pencil.IsEmpty,
            RectangleAnnotation rectangle => rectangle.Rect.Width < 2 || rectangle.Rect.Height < 2,
            PixelateAnnotation pixelate => pixelate.Rect.Width < 2 || pixelate.Rect.Height < 2,
            _ => false,
        };

        if (isUseless)
        {
            InvalidateVisual();
            return;
        }

        _undo.Do(new AddAnnotationAction(finished));
    }

    private static Rect Normalize(Point anchor, Point current) => new(
        Math.Min(anchor.X, current.X),
        Math.Min(anchor.Y, current.Y),
        Math.Abs(current.X - anchor.X),
        Math.Abs(current.Y - anchor.Y));

    // --- Operaciones del editor ---

    public void AddText(TextAnnotation text)
    {
        if (text.IsEmpty) return;
        _undo.Do(new AddAnnotationAction(text));
    }

    public void DeleteSelected()
    {
        if (_selected is null) return;

        var index = _annotations.IndexOf(_selected);
        if (index < 0) return;

        _undo.Do(new DeleteAnnotationAction(_selected, index));
        Selected = null;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key is Key.Delete or Key.Back && _selected is not null)
        {
            DeleteSelected();
            e.Handled = true;
        }
    }
}
