using System.Runtime.InteropServices;

namespace CapturaLocal.Interop;

/// <summary>
/// Unico lugar del proyecto donde se declara un DllImport. Si necesitas una funcion nativa
/// nueva, va aqui — no dispersa por la clase que la usa.
/// </summary>
internal static class NativeMethods
{
    // --- Atajos globales ---

    public const int WM_HOTKEY = 0x0312;

    /// <summary>Ventana solo-mensajes: no se dibuja, no aparece en la barra de tareas.</summary>
    public static readonly IntPtr HWND_MESSAGE = new(-3);

    [Flags]
    public enum HotKeyModifiers : uint
    {
        None = 0,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008,
        /// <summary>Evita que mantener pulsado el atajo dispare capturas en rafaga.</summary>
        NoRepeat = 0x4000,
    }

    /// <summary>El atajo ya lo tiene registrado otro proceso.</summary>
    public const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

    /// <summary>Windows se reserva la combinacion y no la cede a nadie.</summary>
    public const int ERROR_HOTKEY_NOT_REGISTERED = 1419;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, HotKeyModifiers fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
