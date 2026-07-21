using System.Windows;
using System.Windows.Media;

namespace CapturaLocal.Editor.Annotations;

/// <summary>
/// Una marca dibujada sobre la captura. Las anotaciones son objetos, no pixeles: el mapa de
/// bits original nunca se toca y solo se fusiona todo al exportar (ADR-005).
///
/// Esa decision es la que hace posible seleccionar una marca ya hecha para moverla o borrarla
/// (RF-09) y deshacer sin guardar una copia de la imagen por cada trazo (RF-10).
///
/// Las coordenadas son pixeles de la imagen capturada, nunca de la pantalla ni de la ventana:
/// asi el zoom del editor no afecta a lo que acaba en el archivo.
/// </summary>
public abstract class Annotation
{
    public Color Color { get; set; } = Colors.Red;

    public double Thickness { get; set; } = 3;

    /// <summary>Se pinta a si misma. Es el mismo codigo en pantalla y al exportar.</summary>
    public abstract void Render(DrawingContext dc);

    /// <summary>Rectangulo que la contiene, para dibujar la seleccion.</summary>
    public abstract Rect Bounds { get; }

    /// <summary>Indica si un punto de la imagen cae sobre esta anotacion.</summary>
    public abstract bool HitTest(Point point);

    /// <summary>Desplaza la anotacion. Se usa tanto al arrastrar como al deshacer un arrastre.</summary>
    public abstract void Move(Vector delta);

    protected Pen CreatePen()
    {
        var pen = new Pen(new SolidColorBrush(Color), Thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };

        pen.Freeze();
        return pen;
    }

    /// <summary>
    /// Margen extra al comprobar si el usuario acerto a una anotacion. Sin esto acertar a una
    /// linea de 1 px seria imposible con el raton.
    /// </summary>
    protected double HitTolerance => Math.Max(6, Thickness);
}
