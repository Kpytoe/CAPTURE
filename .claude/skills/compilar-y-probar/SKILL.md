---
name: compilar-y-probar
description: Compilar, ejecutar y verificar Captura Local. Usar al arrancar la app, comprobar que un cambio funciona de verdad, depurar el atajo global o el overlay de captura, o antes de dar por terminada cualquier tarea en este proyecto.
---

# Compilar y probar Captura Local

Esta app no tiene una salida de consola que mirar: vive en la bandeja del sistema y su
funcionamiento se ve en la pantalla. Por eso «compila sin errores» no es evidencia de nada aquí.
Lo que sigue son las recetas que sí lo son, todas comprobadas.

## Comandos

```powershell
cd C:\CODE\Cut
dotnet build src\CapturaLocal\CapturaLocal.csproj      # ~2 s en caliente
dotnet test  tests\CapturaLocal.Tests                  # 34 pruebas, ~30 s con restore
dotnet run   --project src\CapturaLocal\CapturaLocal.csproj
```

El ejecutable queda en `src\CapturaLocal\bin\Debug\net8.0-windows\CapturaLocal.exe`.

**Antes de recompilar, cierra la app** o el build falla con el archivo bloqueado:

```powershell
Get-Process CapturaLocal -ErrorAction SilentlyContinue | Stop-Process -Force
```

## Arrancar sin ratón

La app admite dos argumentos, pensados para accesos directos del usuario y utilísimos para
probar sin depender del menú de bandeja:

```powershell
CapturaLocal.exe --configuracion   # abre Configuración directamente
CapturaLocal.exe --capturar        # lanza el overlay de selección
```

## Dónde mirar cuando algo falla

| Qué | Dónde |
|---|---|
| Log de la app | `%LOCALAPPDATA%\CapturaLocal\logs\captura.log` |
| Preferencias | `%LOCALAPPDATA%\CapturaLocal\settings.json` |
| Capturas guardadas | `%USERPROFILE%\Capturas` |

El log anota el atajo registrado, el tiempo que tardó el overlay y el motivo exacto de
cualquier fallo. Empieza siempre por ahí.

## Verificar el atajo global sin tocar el teclado

El truco: intentar registrar la misma combinación desde otro proceso. Si la app la tiene,
Windows responde 1409.

```powershell
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class HK {
  [DllImport("user32.dll", SetLastError=true)] public static extern bool RegisterHotKey(IntPtr h, int id, uint m, uint k);
  [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
  public static string Try(uint m, uint k) {
    if (RegisterHotKey(IntPtr.Zero, 9911, m, k)) { UnregisterHotKey(IntPtr.Zero, 9911); return "LIBRE"; }
    return "OCUPADO (" + Marshal.GetLastWin32Error() + ")";
  }
}
'@
[HK]::Try(6, 0x32)   # Ctrl+Shift+2 -> debe decir OCUPADO con la app corriendo
```

Para probar el camino de fallo (RF-03), pon `"Hotkey": "Win+Shift+S"` en `settings.json` y
arranca: Windows se reserva esa combinación, así que el log debe dar el motivo y la app debe
seguir viva.

## Verificar que el recorte es exacto

Un fallo de coordenadas por DPI **no se nota en el tamaño** —puede salir correcto— sino en el
contenido: la imagen sale desplazada. Hay que comparar píxeles, no medidas.

La receta: capturar una región conocida con `CopyFromScreen` como referencia, arrastrar esa
misma región en el overlay, y comparar. Debe dar ~100 % de píxeles idénticos.

**Importante:** el proceso de PowerShell que haga esto tiene que declararse per-monitor DPI
aware *antes* de dibujar nada, o sus coordenadas no coincidirán con las físicas:

```powershell
[U]::SetProcessDpiAwarenessContext([IntPtr]::new(-4))
```

Sin eso se acaba capturando una región distinta y persiguiendo un bug que no existe.

## Fotografiar una ventana de la app

`CopyFromScreen` sobre coordenadas de pantalla se equivoca de ventana en cuanto hay escalado.
Usa `PrintWindow`, que copia la ventana directamente aunque esté tapada y no lee nada más de
la pantalla:

```powershell
PrintWindow($hwnd, $hdc, 2)   # 2 = PW_RENDERFULLCONTENT
```

Combínalo con `DwmGetWindowAttribute(hwnd, 9, ...)` (DWMWA_EXTENDED_FRAME_BOUNDS) para obtener
el rectángulo real, sin el margen invisible de redimensión que devuelve `GetWindowRect`.

## No automatices el ratón a ciegas

Un clic sintético dirigido «a donde debería estar» acaba en la ventana de otra persona. Si
tienes que inyectar entrada, hazlo solo cuando el overlay de captura esté en primer plano
cubriendo la pantalla, o sobre una ventana que hayas traído al frente y verificado por título.

## Verificación de privacidad (RNF-03)

```powershell
cd C:\CODE\Cut
git grep -nE 'HttpClient|WebClient|WebRequest|Socket|telemetr' -- src
```

Debe devolver **vacío**. Es la prueba de que la promesa del producto se sostiene.
