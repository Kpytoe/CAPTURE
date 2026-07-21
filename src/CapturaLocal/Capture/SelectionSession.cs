using System.Drawing;

namespace CapturaLocal.Capture;

/// <summary>
/// Estado compartido de una seleccion en curso. Existe uno solo por captura, y todas las
/// ventanas de overlay lo leen: por eso arrastrar de un monitor a otro produce un unico
/// rectangulo coherente en lugar de dos selecciones independientes.
///
/// Todas las coordenadas son pixeles fisicos del escritorio virtual.
/// </summary>
public sealed class SelectionSession
{
    private Point _anchor;

    /// <summary>Se dispara en cada cambio para que los overlays se repinten.</summary>
    public event Action? Changed;

    /// <summary>Se dispara al soltar (con region valida) o al cancelar (con null).</summary>
    public event Action<Rectangle?>? Finished;

    public bool IsDragging { get; private set; }

    public Rectangle Selection { get; private set; }

    public void Begin(Point origin)
    {
        _anchor = origin;
        IsDragging = true;
        Selection = Rectangle.Empty;
        Changed?.Invoke();
    }

    public void DragTo(Point current)
    {
        if (!IsDragging) return;

        // Normalizado: da igual hacia donde arrastre el usuario, el rectangulo siempre tiene
        // ancho y alto positivos.
        Selection = Rectangle.FromLTRB(
            Math.Min(_anchor.X, current.X),
            Math.Min(_anchor.Y, current.Y),
            Math.Max(_anchor.X, current.X),
            Math.Max(_anchor.Y, current.Y));

        Changed?.Invoke();
    }

    public void Complete()
    {
        IsDragging = false;

        // Un clic suelto sin arrastrar no es una region: se trata como cancelacion para no
        // abrir el editor con una imagen de 1 pixel.
        var result = Selection.Width >= MinimumSize && Selection.Height >= MinimumSize
            ? Selection
            : (Rectangle?)null;

        Finished?.Invoke(result);
    }

    public void Cancel()
    {
        IsDragging = false;
        Selection = Rectangle.Empty;
        Changed?.Invoke();
        Finished?.Invoke(null);
    }

    /// <summary>Por debajo de esto casi siempre es un clic accidental, no una region querida.</summary>
    public const int MinimumSize = 4;
}
