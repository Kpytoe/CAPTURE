using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using CapturaLocal.Services;
using CapturaLocal.Settings;

namespace CapturaLocal.Interop;

/// <summary>
/// Registra el atajo global contra una ventana solo-mensajes (no visible, sin barra de tareas)
/// y avisa cuando se pulsa.
///
/// La parte importante de esta clase no es registrar: es <b>explicar por que no se pudo</b>.
/// RF-03 exige un motivo, no un "no se pudo guardar".
/// </summary>
public sealed class HotKeyManager : IDisposable
{
    private const int HotKeyId = 1;

    private readonly HwndSource _source;
    private bool _registered;

    public HotKeyManager()
    {
        var parameters = new HwndSourceParameters("CapturaLocal.HotKeyWindow")
        {
            ParentWindow = NativeMethods.HWND_MESSAGE,
            WindowStyle = 0,
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    /// <summary>Se dispara cuando el usuario pulsa el atajo registrado.</summary>
    public event Action? Pressed;

    public HotkeyDefinition? Current { get; private set; }

    /// <summary>
    /// Intenta dejar registrado <paramref name="hotkey"/>. Si falla, el atajo anterior sigue
    /// vigente y <paramref name="error"/> explica el motivo en castellano.
    /// </summary>
    public bool TryRegister(HotkeyDefinition hotkey, out string? error)
    {
        error = Validate(hotkey);
        if (error is not null) return false;

        // Se libera el anterior solo despues de validar, pero antes de registrar: Windows no
        // deja tener dos atajos con el mismo id.
        var previous = Current;
        Unregister();

        var modifiers = ToNative(hotkey.Modifiers) | NativeMethods.HotKeyModifiers.NoRepeat;
        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(hotkey.Key);

        if (NativeMethods.RegisterHotKey(_source.Handle, HotKeyId, modifiers, virtualKey))
        {
            _registered = true;
            Current = hotkey;
            FileLogger.Info($"Atajo global registrado: {hotkey}");
            return true;
        }

        var code = Marshal.GetLastWin32Error();
        error = DescribeFailure(hotkey, code);
        FileLogger.Warn($"No se pudo registrar el atajo {hotkey}: codigo {code}. {error}");

        // Recuperar el anterior para no dejar al usuario sin atajo (SRS 12).
        if (previous is not null &&
            NativeMethods.RegisterHotKey(_source.Handle, HotKeyId, ToNative(previous.Modifiers) |
                NativeMethods.HotKeyModifiers.NoRepeat,
                (uint)KeyInterop.VirtualKeyFromKey(previous.Key)))
        {
            _registered = true;
            Current = previous;
            FileLogger.Info($"Se restauro el atajo anterior: {previous}");
        }

        return false;
    }

    public void Unregister()
    {
        if (!_registered) return;

        NativeMethods.UnregisterHotKey(_source.Handle, HotKeyId);
        _registered = false;
        Current = null;
    }

    /// <summary>
    /// Rechazos que se pueden razonar sin molestar a Windows. Se comprueban antes de llamar a
    /// RegisterHotKey porque la API los aceptaria y el resultado seria peor que un error.
    /// </summary>
    private static string? Validate(HotkeyDefinition hotkey)
    {
        if (HotkeyDefinition.IsModifierKey(hotkey.Key))
        {
            return "Una tecla modificadora por sí sola no forma un atajo. " +
                   "Añade una tecla normal, por ejemplo Ctrl + Shift + 2.";
        }

        if (!hotkey.HasAnyModifier)
        {
            return "El atajo necesita al menos un modificador. Tal cual, " +
                   $"«{hotkey.ToDisplayString()}» capturaría esa tecla en todo el sistema y " +
                   "dejaría de poder escribirse.";
        }

        return null;
    }

    private static string DescribeFailure(HotkeyDefinition hotkey, int win32Code) => win32Code switch
    {
        NativeMethods.ERROR_HOTKEY_ALREADY_REGISTERED =>
            $"«{hotkey.ToDisplayString()}» ya lo está usando otra aplicación o el propio Windows. " +
            "Cierra la aplicación que lo ocupa o elige otra combinación.",

        NativeMethods.ERROR_HOTKEY_NOT_REGISTERED =>
            $"Windows no permite registrar «{hotkey.ToDisplayString()}»: se reserva esa combinación.",

        _ =>
            $"Windows rechazó «{hotkey.ToDisplayString()}» (código {win32Code}). " +
            "Prueba con otra combinación.",
    };

    private static NativeMethods.HotKeyModifiers ToNative(ModifierKeys modifiers)
    {
        var result = NativeMethods.HotKeyModifiers.None;
        if (modifiers.HasFlag(ModifierKeys.Control)) result |= NativeMethods.HotKeyModifiers.Control;
        if (modifiers.HasFlag(ModifierKeys.Alt)) result |= NativeMethods.HotKeyModifiers.Alt;
        if (modifiers.HasFlag(ModifierKeys.Shift)) result |= NativeMethods.HotKeyModifiers.Shift;
        if (modifiers.HasFlag(ModifierKeys.Windows)) result |= NativeMethods.HotKeyModifiers.Win;
        return result;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotKeyId)
        {
            handled = true;
            Pressed?.Invoke();
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
