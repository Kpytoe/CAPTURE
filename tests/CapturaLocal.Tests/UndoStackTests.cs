using System.Windows;
using System.Windows.Media;
using CapturaLocal.Editor;
using CapturaLocal.Editor.Annotations;

namespace CapturaLocal.Tests;

/// <summary>
/// Deshacer/rehacer es el criterio de aceptacion que exige que el estado vuelva
/// «exactamente a cada paso esperado». Estas pruebas ejercitan justo eso.
/// </summary>
public class UndoStackTests
{
    /// <summary>Anotacion minima: no hace falta WPF real para probar el historial.</summary>
    private sealed class Marca : Annotation
    {
        public Point Position { get; private set; }

        public override void Render(DrawingContext dc) { }
        public override Rect Bounds => new(Position, new Size(10, 10));
        public override bool HitTest(Point point) => Bounds.Contains(point);
        public override void Move(Vector delta) => Position += delta;
    }

    [Fact]
    public void DeshacerYRehacerDevuelvenLaListaAlMismoEstado()
    {
        var anotaciones = new List<Annotation>();
        var pila = new UndoStack(anotaciones);
        var primera = new Marca();
        var segunda = new Marca();

        pila.Do(new AddAnnotationAction(primera));
        pila.Do(new AddAnnotationAction(segunda));
        Assert.Equal(2, anotaciones.Count);

        pila.Undo();
        pila.Undo();
        Assert.Empty(anotaciones);

        pila.Redo();
        pila.Redo();
        Assert.Equal(new Annotation[] { primera, segunda }, anotaciones);
    }

    [Fact]
    public void HacerAlgoNuevoDespuesDeDeshacerAbandonaLaRamaDeshecha()
    {
        var anotaciones = new List<Annotation>();
        var pila = new UndoStack(anotaciones);

        pila.Do(new AddAnnotationAction(new Marca()));
        pila.Undo();
        Assert.True(pila.CanRedo);

        pila.Do(new AddAnnotationAction(new Marca()));

        // Lo deshecho ya no se puede recuperar: es lo que hace cualquier editor.
        Assert.False(pila.CanRedo);
        Assert.Single(anotaciones);
    }

    [Fact]
    public void DeshacerUnBorradoDevuelveLaAnotacionASuPosicionOriginal()
    {
        var primera = new Marca();
        var segunda = new Marca();
        var tercera = new Marca();
        var anotaciones = new List<Annotation> { primera, segunda, tercera };
        var pila = new UndoStack(anotaciones);

        pila.Do(new DeleteAnnotationAction(segunda, 1));
        Assert.Equal(new Annotation[] { primera, tercera }, anotaciones);

        pila.Undo();

        // Si volviese al final cambiaria el orden de dibujado y lo que estaba debajo
        // apareceria encima.
        Assert.Equal(new Annotation[] { primera, segunda, tercera }, anotaciones);
    }

    [Fact]
    public void DeshacerUnMovimientoLoRevierteExactamente()
    {
        var marca = new Marca();
        var anotaciones = new List<Annotation> { marca };
        var pila = new UndoStack(anotaciones);

        // Asi lo registra el lienzo: el arrastre ya se aplico en vivo.
        marca.Move(new Vector(30, -12));
        pila.DoWithoutApplying(new MoveAnnotationAction(marca, new Vector(30, -12)));

        pila.Undo();
        Assert.Equal(new Point(0, 0), marca.Position);

        pila.Redo();
        Assert.Equal(new Point(30, -12), marca.Position);
    }

    [Fact]
    public void UnArrastreYaAplicadoNoSeMueveElDoble()
    {
        var marca = new Marca();
        var pila = new UndoStack(new List<Annotation> { marca });

        marca.Move(new Vector(10, 10));
        pila.DoWithoutApplying(new MoveAnnotationAction(marca, new Vector(10, 10)));

        Assert.Equal(new Point(10, 10), marca.Position);
    }

    [Fact]
    public void GuardarLimpiaLaMarcaDeCambiosPendientesSinPerderElHistorial()
    {
        var pila = new UndoStack(new List<Annotation>());
        pila.Do(new AddAnnotationAction(new Marca()));
        Assert.True(pila.IsDirty);

        pila.MarkSaved();

        Assert.False(pila.IsDirty);
        Assert.True(pila.CanUndo);
    }
}
