using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CapturaLocal.Editor;
using CapturaLocal.Editor.Annotations;

namespace CapturaLocal.Tests;

/// <summary>
/// Herramienta «Pintar» y cuentagotas. Pintar existe para tapar, asi que lo que se comprueba
/// es que tape de verdad: opaco y sin borde suavizado aunque el arrastre caiga entre pixeles.
/// </summary>
public class PintarTests
{
    [Fact]
    public void ElBloqueSeSeleccionaPorDentroYNoFuera()
    {
        var bloque = new PaintAnnotation { Rect = new Rect(10, 10, 50, 20) };

        Assert.True(bloque.HitTest(new Point(35, 20)));   // centro: es un relleno, cuenta todo
        Assert.False(bloque.HitTest(new Point(80, 20)));
    }

    [Fact]
    public void DeshacerElArrastreDeUnBloqueLoDejaDondeEstaba()
    {
        var bloque = new PaintAnnotation { Rect = new Rect(10.3, 20.7, 60.2, 30.1) };
        var pila = new UndoStack(new List<Annotation> { bloque });

        bloque.Move(new Vector(-12.5, 4.25));
        pila.DoWithoutApplying(new MoveAnnotationAction(bloque, new Vector(-12.5, 4.25)));
        pila.Undo();

        // El redondeo a pixeles es solo al dibujar: el rectangulo guardado no se toca.
        Assert.Equal(new Rect(10.3, 20.7, 60.2, 30.1), bloque.Rect);
    }

    [Fact]
    public void ConCoordenadasFraccionariasTapaPixelesEnterosSinBordeSuave()
    {
        var bloque = new PaintAnnotation { Rect = new Rect(2.4, 3.6, 4.2, 2.1), Color = Colors.Black };

        // Floor del origen y Ceiling del extremo: (2,3) a (7,6).
        Assert.Equal(new Rect(2, 3, 5, 3), bloque.Bounds);

        var pixeles = EnSta(() =>
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, 10, 10));
                bloque.Render(dc);
            }

            var bmp = new RenderTargetBitmap(10, 10, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(visual);

            var datos = new byte[10 * 10 * 4];
            bmp.CopyPixels(datos, 10 * 4, 0);
            return datos;
        });

        byte Azul(int x, int y) => pixeles[(y * 10 + x) * 4];

        // Las cuatro esquinas del bloque, negras del todo: nada asoma por el borde.
        Assert.Equal(0, Azul(2, 3));
        Assert.Equal(0, Azul(6, 3));
        Assert.Equal(0, Azul(2, 5));
        Assert.Equal(0, Azul(6, 5));

        // Justo fuera, blanco intacto.
        Assert.Equal(255, Azul(1, 3));
        Assert.Equal(255, Azul(7, 5));
        Assert.Equal(255, Azul(4, 6));
    }

    [Fact]
    public void ElCuentagotasDevuelveElColorExactoDelPixel()
    {
        // 2x1 en Bgr24, un formato distinto del Bgra32 que lee el cuentagotas, para que la
        // conversion tambien quede probada. Izquierda (12,34,56), derecha (200,150,100).
        var imagen = BitmapSource.Create(2, 1, 96, 96, PixelFormats.Bgr24, null,
            new byte[] { 56, 34, 12, 100, 150, 200, 0, 0 }, 8);

        Assert.Equal(Color.FromRgb(12, 34, 56), AnnotationCanvas.SampleColor(imagen, new Point(0.9, 0.5)));
        Assert.Equal(Color.FromRgb(200, 150, 100), AnnotationCanvas.SampleColor(imagen, new Point(1.2, 0.5)));

        // Fuera de la imagen (arrastre al borde) se queda con el pixel mas cercano, no lanza.
        Assert.Equal(Color.FromRgb(200, 150, 100), AnnotationCanvas.SampleColor(imagen, new Point(5, -3)));
    }

    /// <summary>Los visuales de WPF exigen un hilo STA y xunit corre en MTA.</summary>
    private static T EnSta<T>(Func<T> accion)
    {
        T resultado = default!;
        Exception? error = null;

        var hilo = new Thread(() =>
        {
            try { resultado = accion(); }
            catch (Exception ex) { error = ex; }
        });

        hilo.SetApartmentState(ApartmentState.STA);
        hilo.Start();
        hilo.Join();

        if (error is not null) throw error;
        return resultado;
    }
}
