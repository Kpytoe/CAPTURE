# Captura Local — convenciones del proyecto

App de escritorio Windows: atajo global → overlay de selección de región → editor ligero →
copiar/guardar en local. Sin nube, sin cuentas, sin backend. El SRS completo está en
`docs\SRS_Aplicacion_Local_de_Capturas.md`; las decisiones y sus porqués en `docs\DECISIONS.md`.

## Stack

- **C# / WPF / .NET 8** (`net8.0-windows`), SDK 8.0.423.
- **Cero `PackageReference` en `src\`.** Es una regla, no una casualidad: sin dependencias, la
  promesa de privacidad (RNF-03) se verifica con un `grep`. Antes de añadir un paquete, escribe un
  ADR explicando por qué no alcanza lo que trae .NET en caja.
- Todo lo nativo va por P/Invoke en `src\CapturaLocal\Interop\NativeMethods.cs`. No se declara un
  `DllImport` fuera de ese archivo.
- `tests\` sí puede usar paquetes (xunit) — es código que no se distribuye.

## Rutas reales

| Qué | Dónde |
|---|---|
| Proyecto de la app | `src\CapturaLocal\CapturaLocal.csproj` |
| Punto de entrada / bandeja | `src\CapturaLocal\App.xaml.cs` |
| P/Invoke y monitores | `src\CapturaLocal\Interop\` |
| Overlay de captura | `src\CapturaLocal\Capture\` |
| Editor y anotaciones | `src\CapturaLocal\Editor\` |
| Preferencias | `src\CapturaLocal\Settings\` |
| Portapapeles, guardado, log | `src\CapturaLocal\Services\` |
| Pruebas | `tests\CapturaLocal.Tests\` |
| Preferencias del usuario en runtime | `%LOCALAPPDATA%\CapturaLocal\settings.json` |

## Comandos

```powershell
dotnet build src\CapturaLocal\CapturaLocal.csproj
dotnet run   --project src\CapturaLocal\CapturaLocal.csproj
dotnet test  tests\CapturaLocal.Tests
git log --oneline
```

La app **no abre ventana al arrancar**: aparece en la bandeja del sistema. Si `dotnet run` parece
no hacer nada, mira el área de notificaciones.

## Restricciones

- **Nunca `git push` sin que el usuario lo pida.** Tampoco `commit` de trabajo a medio verificar.
- **Nunca añadir código de red.** Ni telemetría, ni actualizador automático, ni "reportar error".
  `HttpClient`, `WebClient`, `Socket` están prohibidos en `src\`.
- **Nunca guardar capturas por defecto en una ruta bajo `$env:OneDrive`** — ver ADR-003.
- Nada de `.env`, `credentials.*`, `*.pfx` ni `*.key` en el repo.
- Los logs (`Services\FileLogger.cs`) **jamás** escriben contenido de imagen ni rutas de capturas
  con nombre de archivo completo.

## Cómo se trabaja aquí

- Cada requisito implementado se anota en `docs\TRAZABILIDAD.md` con el archivo que lo cumple y
  cómo se verifica. Un RF sin fila en esa tabla no está hecho.
- Un cambio no está terminado sin evidencia: el comando que se corrió y su salida, o la receta
  manual exacta. "Debería funcionar" no cuenta.
- Cero sobre-ingeniería: nada de capas ni abstracciones para problemas que aún no existen.
- Si se crea una carpeta nueva, se actualiza el árbol del `README.md` en el mismo commit.

## Deuda conocida

- **La ruta multi-monitor no está verificada.** El equipo de desarrollo tiene un solo monitor
  (`\\.\DISPLAY1`, 2560×1440). Todo el diseño de captura asume escalas mixtas y trabaja en
  píxeles físicos (ADR-004), pero nadie lo ha ejercitado con dos pantallas. Antes de dar por
  bueno RNF-07 hay que probar: seleccionar cruzando el borde entre un monitor al 150 % y otro
  al 100 %, y comprobar que el recorte no sale desplazado.
- **`Capture\CapturePreview.cs` es un andamio temporal** del Paso 4: vuelca la captura a disco
  para poder verificar el recorte. Se borra cuando `SaveService` haga el guardado de verdad.

## Notas vivas

- **Borrador 1 terminado y verificado.** Alcance: región (no ventana ni pantalla completa),
  herramientas selección/lápiz/rectángulo/texto/pixelado (no línea ni elipse), guardado PNG
  (no JPG ni "Guardar como"), atajo configurable, bandeja, preferencias versionadas.
  34 pruebas unitarias en verde. Estado por requisito en `docs\TRAZABILIDAD.md`.
- **Fuera del borrador 1, va al 2:** modos ventana y pantalla completa · línea y elipse · JPG con
  calidad · Guardar como · patrón de nombre configurable · iniciar con Windows · rotación de logs ·
  instalador · captura temporizada.
- **Hueco en el SRS:** la tabla §6 empieza en RF-02; no existe RF-01. Falta el requisito de
  "invocar la captura mediante atajo global", o hay un error de numeración. Pendiente de aclarar
  con el autor del documento.
