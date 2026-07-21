using System.Text;
using System.Windows.Input;

namespace CapturaLocal.Settings;

/// <summary>
/// Una combinacion de teclas, con su ida y vuelta a texto para poder guardarla en
/// settings.json y volver a leerla. El formato persistido usa los nombres de
/// <see cref="Key"/> ("Ctrl+Shift+D2"); el que se ensena al usuario es legible ("Ctrl + Shift + 2").
/// </summary>
public sealed record HotkeyDefinition(ModifierKeys Modifiers, Key Key)
{
    public static readonly HotkeyDefinition Default = new(ModifierKeys.Control | ModifierKeys.Shift, Key.D2);

    /// <summary>
    /// Un atajo global sin Ctrl, Alt o Win secuestra la tecla en TODO el sistema: registrar
    /// "Shift + 5" significa que el usuario ya no puede escribir '%' en ningun programa.
    /// No se prohibe, pero se advierte.
    /// </summary>
    public bool HasStrongModifier =>
        Modifiers.HasFlag(ModifierKeys.Control) ||
        Modifiers.HasFlag(ModifierKeys.Alt) ||
        Modifiers.HasFlag(ModifierKeys.Windows);

    public bool HasAnyModifier => Modifiers != ModifierKeys.None;

    /// <summary>Las teclas modificadoras por si solas no forman un atajo.</summary>
    public static bool IsModifierKey(Key key) => key is
        Key.LeftCtrl or Key.RightCtrl or
        Key.LeftAlt or Key.RightAlt or
        Key.LeftShift or Key.RightShift or
        Key.LWin or Key.RWin or
        Key.System or Key.None;

    /// <summary>
    /// Orden Win, Ctrl, Alt, Shift: es como Windows escribe sus propios atajos
    /// ("Win+Shift+S", "Ctrl+Alt+Supr"). Un orden distinto hace que el usuario no reconozca
    /// el atajo que el mismo acaba de teclear.
    /// </summary>
    private static IEnumerable<string> ModifierNames(ModifierKeys modifiers)
    {
        if (modifiers.HasFlag(ModifierKeys.Windows)) yield return "Win";
        if (modifiers.HasFlag(ModifierKeys.Control)) yield return "Ctrl";
        if (modifiers.HasFlag(ModifierKeys.Alt)) yield return "Alt";
        if (modifiers.HasFlag(ModifierKeys.Shift)) yield return "Shift";
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var name in ModifierNames(Modifiers)) sb.Append(name).Append('+');
        sb.Append(Key);
        return sb.ToString();
    }

    /// <summary>Version para mostrar en pantalla: "Ctrl + Shift + 2".</summary>
    public string ToDisplayString() =>
        string.Join(" + ", ModifierNames(Modifiers).Append(FriendlyKeyName(Key)));

    public static string FriendlyKeyName(Key key) => key switch
    {
        >= Key.D0 and <= Key.D9 => ((int)key - (int)Key.D0).ToString(),
        >= Key.NumPad0 and <= Key.NumPad9 => "Num " + ((int)key - (int)Key.NumPad0),
        Key.OemPlus => "+",
        Key.OemMinus => "-",
        Key.OemComma => ",",
        Key.OemPeriod => ".",
        Key.Oem1 => ";",
        Key.Oem2 => "/",
        Key.Oem3 => "`",
        Key.Space => "Espacio",
        Key.Escape => "Esc",
        Key.Snapshot => "Impr Pant",
        Key.Return => "Intro",
        Key.Back => "Retroceso",
        Key.Delete => "Supr",
        Key.Insert => "Insert",
        Key.PageUp => "Re Pág",
        Key.Next => "Av Pág",
        _ => key.ToString(),
    };

    public static bool TryParse(string? text, out HotkeyDefinition? hotkey)
    {
        hotkey = null;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var modifiers = ModifierKeys.None;
        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;

        for (var i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToLowerInvariant())
            {
                case "ctrl" or "control": modifiers |= ModifierKeys.Control; break;
                case "alt": modifiers |= ModifierKeys.Alt; break;
                case "shift": modifiers |= ModifierKeys.Shift; break;
                case "win" or "windows": modifiers |= ModifierKeys.Windows; break;
                default: return false;
            }
        }

        if (!Enum.TryParse<Key>(parts[^1], ignoreCase: true, out var key)) return false;
        if (IsModifierKey(key)) return false;

        hotkey = new HotkeyDefinition(modifiers, key);
        return true;
    }
}
