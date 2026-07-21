using System.Diagnostics;
using System.Windows.Media.Imaging;
using CapturaLocal.Interop;
using CapturaLocal.Services;

using Drawing = System.Drawing;

namespace CapturaLocal.Capture;

/// <summary>
/// Orquesta una captura de region: congelar el escritorio, mostrar un overlay por monitor,
/// esperar a que el usuario elija y devolver el recorte.
/// </summary>
public static class CaptureService
{
    private static bool _inProgress;

    /// <summary>
    /// Devuelve la region elegida, o null si el usuario cancelo. Si ya hay una captura en
    /// curso devuelve null sin hacer nada: pulsar el atajo dos veces no debe apilar overlays.
    /// </summary>
    public static async Task<BitmapSource?> CaptureRegionAsync()
    {
        if (_inProgress) return null;
        _inProgress = true;

        var stopwatch = Stopwatch.StartNew();
        Drawing::Bitmap? desktop = null;

        try
        {
            desktop = ScreenCapture.CaptureVirtualDesktop();

            var session = new SelectionSession();
            var completion = new TaskCompletionSource<Drawing::Rectangle?>();
            session.Finished += region => completion.TrySetResult(region);

            var cursor = CursorMonitor();
            var overlays = new List<OverlayWindow>();
            OverlayWindow? underCursor = null;

            foreach (var monitor in MonitorInfo.All)
            {
                var isCursorMonitor = monitor.Bounds == cursor?.Bounds;

                var crop = ScreenCapture.Crop(desktop, monitor.Bounds);
                try
                {
                    var window = new OverlayWindow(
                        monitor,
                        ScreenCapture.ToBitmapSource(crop),
                        session,
                        showHint: isCursorMonitor);

                    overlays.Add(window);
                    if (isCursorMonitor) underCursor = window;
                }
                finally
                {
                    crop.Dispose();
                }
            }

            foreach (var overlay in overlays) overlay.Show();

            // El foco va al overlay del monitor donde esta el raton, para que Escape funcione
            // sin obligar al usuario a hacer clic antes.
            var focused = underCursor ?? overlays.FirstOrDefault();
            focused?.Activate();
            focused?.Focus();

            FileLogger.Info($"Overlay de seleccion mostrado en {stopwatch.ElapsedMilliseconds} ms " +
                            $"sobre {overlays.Count} monitor(es).");

            var selection = await completion.Task;

            foreach (var overlay in overlays) overlay.Close();

            if (selection is null)
            {
                FileLogger.Info("Captura cancelada por el usuario.");
                return null;
            }

            using var cropped = ScreenCapture.Crop(desktop, selection.Value);
            var result = ScreenCapture.ToBitmapSource(cropped);

            FileLogger.Info($"Region capturada: {selection.Value.Width}x{selection.Value.Height} px " +
                            $"en {stopwatch.ElapsedMilliseconds} ms.");
            return result;
        }
        catch (Exception ex)
        {
            FileLogger.Error("Fallo la captura de pantalla.", ex);
            throw;
        }
        finally
        {
            desktop?.Dispose();
            _inProgress = false;
        }
    }

    private static MonitorInfo? CursorMonitor()
    {
        if (!NativeMethods.GetCursorPos(out var point)) return null;

        return MonitorInfo.All.FirstOrDefault(m => m.Bounds.Contains(point.X, point.Y));
    }
}
