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

    // --- Captura y overlay ---

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    /// <summary>
    /// Posicion del cursor en pixeles fisicos del escritorio virtual. Es la unica medida que
    /// vale igual en todos los monitores: las coordenadas de WPF dependen del DPI de la ventana
    /// que las reporta, asi que con escalas mixtas no se pueden comparar entre monitores.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT point);

    public static readonly IntPtr HWND_TOPMOST = new(-1);

    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;

    /// <summary>
    /// Coloca una ventana en pixeles fisicos exactos. Necesario porque Left/Top/Width/Height de
    /// WPF estan en unidades independientes del dispositivo, y al abarcar monitores con escalas
    /// distintas no hay un unico factor que convierta bien.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint flags);

    /// <summary>
    /// Libera el HBITMAP que devuelve Bitmap.GetHbitmap(). Sin esto se fuga un objeto GDI por
    /// cada captura, y el limite por proceso son 10.000.
    /// </summary>
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject(IntPtr hObject);

    // --- Apariencia de la ventana ---

    /// <summary>
    /// Pinta la barra de titulo en oscuro. Es atributo del gestor de ventanas, no de WPF: sin
    /// el, una ventana negra se queda con la barra de titulo blanca del sistema.
    /// </summary>
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    /// <summary>Numero del atributo antes de Windows 10 1903; en 1809 valia 19.</summary>
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE_ANTIGUO = 19;

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hWnd, int attribute,
        ref int value, int size);
}
