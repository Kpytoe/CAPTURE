using System.Windows;
using System.Windows.Interop;

namespace CapturaLocal.Interop;

/// <summary>
/// Barra de titulo oscura. WPF pinta el contenido de la ventana, pero el marco lo dibuja el
/// gestor de ventanas: sin esto una ventana oscura queda coronada por una barra blanca.
/// </summary>
internal static class WindowTheming
{
    /// <summary>
    /// Aplica el modo oscuro al marco. Hay que llamarlo cuando la ventana ya tiene handle
    /// (SourceInitialized o despues); antes no hay nada al que aplicarselo.
    ///
    /// Es puramente cosmetico, asi que un fallo no interrumpe nada: en Windows 10 anterior a
    /// 1809 el atributo no existe, DwmSetWindowAttribute devuelve error y la ventana se queda
    /// con la barra clara de siempre.
    /// </summary>
    public static void UseDarkTitleBar(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;

        var enabled = 1;

        var result = NativeMethods.DwmSetWindowAttribute(
            handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int));

        if (result != 0)
        {
            NativeMethods.DwmSetWindowAttribute(
                handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE_ANTIGUO,
                ref enabled, sizeof(int));
        }
    }
}
