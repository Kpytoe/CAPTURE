using System.Windows.Input;
using CapturaLocal.Settings;

namespace CapturaLocal.Tests;

/// <summary>
/// El atajo hace un viaje de ida y vuelta a texto cada vez que se guarda y se relee
/// settings.json. Si ese viaje pierde informacion, el usuario reinicia y su atajo cambia solo.
/// </summary>
public class HotkeyDefinitionTests
{
    [Theory]
    [InlineData(ModifierKeys.Control | ModifierKeys.Shift, Key.D2)]
    [InlineData(ModifierKeys.Alt, Key.F9)]
    [InlineData(ModifierKeys.Windows | ModifierKeys.Shift, Key.S)]
    [InlineData(ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, Key.Insert)]
    public void ElTextoGuardadoSeVuelveALeerIgual(ModifierKeys modifiers, Key key)
    {
        var original = new HotkeyDefinition(modifiers, key);

        Assert.True(HotkeyDefinition.TryParse(original.ToString(), out var recovered));
        Assert.Equal(original, recovered);
    }

    [Fact]
    public void LosModificadoresSeEscribenEnElOrdenDeWindows()
    {
        var hotkey = new HotkeyDefinition(
            ModifierKeys.Shift | ModifierKeys.Windows, Key.S);

        // Windows escribe "Win+Shift+S". Si saliese "Shift + Win + S" el usuario no
        // reconoceria el atajo que acaba de teclear.
        Assert.Equal("Win + Shift + S", hotkey.ToDisplayString());
    }

    [Fact]
    public void LasTeclasNumericasSeMuestranComoDigitos()
    {
        var hotkey = new HotkeyDefinition(ModifierKeys.Control | ModifierKeys.Shift, Key.D2);

        Assert.Equal("Ctrl + Shift + 2", hotkey.ToDisplayString());
        Assert.Equal("Ctrl+Shift+D2", hotkey.ToString());
    }

    [Theory]
    [InlineData(ModifierKeys.Control, true)]
    [InlineData(ModifierKeys.Alt, true)]
    [InlineData(ModifierKeys.Windows, true)]
    [InlineData(ModifierKeys.Shift, false)]
    [InlineData(ModifierKeys.None, false)]
    public void SoloCtrlAltYWinCuentanComoModificadorFuerte(ModifierKeys modifiers, bool esperado)
    {
        // Shift solo no basta: "Shift+5" secuestraria el simbolo % en todo el sistema.
        var hotkey = new HotkeyDefinition(modifiers, Key.D5);

        Assert.Equal(esperado, hotkey.HasStrongModifier);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Mayus+A")]
    [InlineData("Ctrl+NoExisteEstaTecla")]
    [InlineData("Ctrl+LeftShift")]
    public void UnTextoInvalidoNoSeInterpreta(string? texto)
    {
        Assert.False(HotkeyDefinition.TryParse(texto, out var resultado));
        Assert.Null(resultado);
    }

    [Fact]
    public void SeAceptaCualquierOrdenAlLeerElArchivo()
    {
        // Un usuario que edite settings.json a mano no tiene por que respetar el orden.
        Assert.True(HotkeyDefinition.TryParse("Shift+Ctrl+D2", out var leido));
        Assert.Equal(new HotkeyDefinition(ModifierKeys.Control | ModifierKeys.Shift, Key.D2), leido);
    }
}
