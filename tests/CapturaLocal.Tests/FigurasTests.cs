using System.Windows;
using CapturaLocal.Editor;
using CapturaLocal.Editor.Annotations;

namespace CapturaLocal.Tests;

/// <summary>
/// Linea y elipse (RF-07). Lo que se comprueba es lo que la receta de herramientas marca como
/// no negociable: acertar con margen, no acertar donde no hay trazo, y mover de forma reversible.
/// </summary>
public class FigurasTests
{
    [Fact]
    public void LaLineaSeSeleccionaCercaDelTrazoYNoMasAllaDeSusExtremos()
    {
        var linea = new LineAnnotation { Start = new Point(0, 0), End = new Point(100, 0), Thickness = 2 };

        Assert.True(linea.HitTest(new Point(50, 4)));    // dentro del margen
        Assert.False(linea.HitTest(new Point(50, 20)));  // lejos del trazo
        Assert.False(linea.HitTest(new Point(130, 0)));  // sobre la recta, pero pasado el extremo
    }

    [Fact]
    public void LaElipseSoloSeSeleccionaPorElContorno()
    {
        var elipse = new EllipseAnnotation { Rect = new Rect(0, 0, 200, 100), Thickness = 2 };

        Assert.True(elipse.HitTest(new Point(200, 50)));   // extremo derecho del contorno
        Assert.True(elipse.HitTest(new Point(100, 3)));    // cerca del borde superior
        Assert.False(elipse.HitTest(new Point(100, 50)));  // centro: deja seleccionar lo de dentro
        Assert.False(elipse.HitTest(new Point(2, 2)));     // esquina del rectangulo, fuera del ovalo
    }

    [Fact]
    public void DeshacerElArrastreDeUnaLineaLaDejaDondeEstaba()
    {
        var linea = new LineAnnotation { Start = new Point(10, 10), End = new Point(40, 25) };
        var pila = new UndoStack(new List<Annotation> { linea });

        linea.Move(new Vector(7.5, -3.25));
        pila.DoWithoutApplying(new MoveAnnotationAction(linea, new Vector(7.5, -3.25)));
        pila.Undo();

        Assert.Equal(new Point(10, 10), linea.Start);
        Assert.Equal(new Point(40, 25), linea.End);
    }

    [Fact]
    public void DeshacerElArrastreDeUnaElipseLaDejaDondeEstaba()
    {
        var elipse = new EllipseAnnotation { Rect = new Rect(10, 20, 60, 30) };
        var pila = new UndoStack(new List<Annotation> { elipse });

        elipse.Move(new Vector(-12.5, 4));
        pila.DoWithoutApplying(new MoveAnnotationAction(elipse, new Vector(-12.5, 4)));
        pila.Undo();

        Assert.Equal(new Rect(10, 20, 60, 30), elipse.Rect);
    }
}
