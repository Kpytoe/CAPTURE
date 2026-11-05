namespace CapturaLocal.Editor;

/// <summary>Herramientas del editor (RF-07).</summary>
public enum EditorTool
{
    /// <summary>Seleccionar una anotacion para moverla o borrarla (RF-09).</summary>
    Select,
    Pencil,

    // Las tres figuras comparten un solo boton en la barra, con un menu para elegir cual.
    Rectangle,
    Ellipse,
    Line,

    Text,
    Pixelate,
}
