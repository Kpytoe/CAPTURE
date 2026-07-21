using System.IO;
using CapturaLocal.Services;
using CapturaLocal.Settings;

namespace CapturaLocal.Tests;

public class NombreDeArchivoTests
{
    [Fact]
    public void ElNombreSigueElPatronDelSrs()
    {
        var momento = new DateTime(2026, 7, 21, 9, 5, 3);

        // El SRS 8 fija exactamente Captura_YYYY-MM-DD_HH-mm-ss.png, con ceros a la izquierda.
        Assert.Equal("Captura_2026-07-21_09-05-03.png",
            SaveService.BuildFileName(momento, ".png"));
    }

    [Fact]
    public void ElNombreUsaHorarioDe24HorasParaQueOrdeneBien()
    {
        var tarde = new DateTime(2026, 7, 21, 21, 30, 0);

        Assert.Contains("_21-30-00", SaveService.BuildFileName(tarde, ".png"));
    }

    [Fact]
    public void DosCapturasEnElMismoSegundoProponenElMismoNombre()
    {
        // Documenta el comportamiento actual: el choque lo resuelve la confirmacion de
        // sobrescritura, no el nombre. Si algun dia se anade un sufijo, esta prueba avisa.
        var momento = new DateTime(2026, 7, 21, 9, 5, 3);

        Assert.Equal(SaveService.SuggestPath(@"C:\Capturas", momento),
                     SaveService.SuggestPath(@"C:\Capturas", momento));
    }
}

/// <summary>
/// La promesa del producto es que nada sale del equipo. La carpeta de guardado por defecto es
/// donde esa promesa se rompe mas facil, porque el valor «natural» de Windows (Imagenes) suele
/// estar redirigido a OneDrive. Ver ADR-003.
/// </summary>
public class CarpetaDeGuardadoTests
{
    [Fact]
    public void LaCarpetaPorDefectoNoEsImagenes()
    {
        var imagenes = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        Assert.NotEqual(imagenes, AppSettings.DefaultSaveFolder);
    }

    [Fact]
    public void LaCarpetaPorDefectoNoEstaDentroDeOneDrive()
    {
        Assert.False(AppSettings.IsInsideOneDrive(AppSettings.DefaultSaveFolder));
    }

    [Fact]
    public void SeDetectaUnaRutaDentroDeOneDrive()
    {
        var raiz = Environment.GetEnvironmentVariable("OneDrive");
        if (string.IsNullOrWhiteSpace(raiz))
        {
            return; // En un equipo sin OneDrive no hay nada que detectar.
        }

        Assert.True(AppSettings.IsInsideOneDrive(Path.Combine(raiz, "Imagenes", "Capturas")));
        Assert.True(AppSettings.IsInsideOneDrive(raiz));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UnaRutaVaciaNoSeConsideraOneDrive(string? ruta)
    {
        Assert.False(AppSettings.IsInsideOneDrive(ruta!));
    }

    [Fact]
    public void UnaCarpetaQueSoloEmpiezaIgualNoCuentaComoDentro()
    {
        var raiz = Environment.GetEnvironmentVariable("OneDrive");
        if (string.IsNullOrWhiteSpace(raiz)) return;

        // "OneDrive - BSMART-copias" no esta dentro de "OneDrive - BSMART", aunque el texto
        // empiece igual. Comparar cadenas sin el separador daria un falso positivo.
        Assert.False(AppSettings.IsInsideOneDrive(raiz + "-copias"));
    }
}
