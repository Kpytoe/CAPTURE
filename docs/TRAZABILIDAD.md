# Trazabilidad SRS → código

Cada requisito del SRS con el archivo que lo implementa y cómo se comprueba.
Un requisito sin fila aquí **no está hecho**, por muy escrito que esté el código.

Estado: ✅ hecho y verificado · 🚧 en curso · ⬜ borrador 2

## Requisitos funcionales

| ID | Requisito | Estado | Implementado en | Verificación |
|---|---|---|---|---|
| RF-02 | Asignar combinación de teclas desde Configuración | ✅ | `Settings\SettingsWindow.xaml.cs` (grabador), `Settings\HotkeyDefinition.cs`, `Interop\HotKeyManager.cs` | Con la app corriendo, `RegisterHotKey` de `Ctrl+Shift+2` desde otro proceso devuelve 1409; sin ella, se registra sin problema |
| RF-03 | Validar combinaciones reservadas o en uso, explicando el motivo | ✅ | `Interop\HotKeyManager.cs` (`Validate` + `DescribeFailure`) | Arrancar con `Win+Shift+S` en settings.json: se registra el motivo exacto, la app sigue viva y las preferencias no se tocan |
| RF-04 | Elegir modo predeterminado (región / ventana / pantalla) | 🚧 | `Capture\CaptureService.cs` | Solo modo región en el borrador 1; ventana y pantalla completa van al borrador 2 |
| RF-05 | Arrastrar, ajustar o cancelar la selección | ✅ | `Capture\OverlayWindow.xaml.cs`, `Capture\SelectionSession.cs` | `scratchpad\probar-captura.ps1`: arrastra una región conocida y compara el PNG con una referencia de la pantalla → 100 % de píxeles idénticos. Esc y clic derecho cancelan |
| RF-06 | Abrir el editor de inmediato, a escala y con zoom | ⬜ | | |
| RF-07 | Herramientas: selección, lápiz, línea, rectángulo, elipse, texto | ⬜ | | Borrador 1: sin línea ni elipse; con pixelado |
| RF-08 | Color y grosor por herramienta; texto con color y tamaño | ⬜ | | |
| RF-09 | Seleccionar una anotación para moverla o eliminarla | ⬜ | | |
| RF-10 | Deshacer y rehacer | ⬜ | | |
| RF-11 | Copiar la imagen al portapapeles | ⬜ | | |
| RF-12 | Guardar en carpeta local y «Guardar como» | ⬜ | | Borrador 1: solo Guardar, PNG |
| RF-13 | Configurar carpeta, formato, calidad JPG y patrón de nombre | ⬜ | | Borrador 1: carpeta y formato |
| RF-14 | Configuración desde la bandeja, preferencias persistentes | ✅ | `App.xaml.cs` (menú de bandeja), `Settings\SettingsWindow.xaml` | Menú con las 4 entradas del SRS §9; `CapturaLocal.exe --configuracion` abre la ventana; las preferencias sobreviven al reinicio |
| RF-15 | Cancelar captura o descartar edición, con confirmación | ⬜ | | |

## Requisitos no funcionales

| ID | Requisito | Estado | Implementado en | Verificación |
|---|---|---|---|---|
| RNF-01 | Overlay en ≤ 500 ms | ✅ | `Interop\ScreenCapture.cs`, `Capture\CaptureService.cs` | El propio log lo mide: **323 ms** en un monitor 2560×1440, incluyendo el arranque en frío del proceso |
| RNF-02 | Captura visible en el editor en ≤ 1,5 s a 4K | ⬜ | | |
| RNF-03 | Sin transmisión de capturas ni telemetría | ⬜ | | `git grep -nE 'HttpClient\|WebClient\|WebRequest\|Socket\|telemetr' -- src` vacío |
| RNF-04 | Fallo al guardar conserva la edición y avisa | ⬜ | | |
| RNF-05 | Guardar/Copiar/Deshacer/Cancelar visibles o con atajo | ⬜ | | |
| RNF-06 | Navegable por teclado, con etiquetas accesibles | ⬜ | | |
| RNF-07 | Múltiples monitores y escala DPI | 🚧 | `app.manifest` (PerMonitorV2), `Interop\MonitorInfo.cs`, `Capture\OverlayWindow.xaml.cs` | Implementado (un overlay por monitor, coordenadas en píxeles físicos, `SetWindowPos` exacto, `GetCursorPos` en vez de coordenadas WPF). **Sin verificar:** el equipo de desarrollo tiene un solo monitor, así que la ruta de escalas mixtas no se ha ejercitado. Ver «Deuda conocida» en `.claude\CLAUDE.md` |
| RNF-08 | Preferencias locales, versionadas y recuperables | ✅ | `Settings\SettingsStore.cs`, `Settings\AppSettings.cs` | `%LOCALAPPDATA%\CapturaLocal\settings.json` con `SchemaVersion`; escritura atómica (temp + `File.Replace`); un JSON dañado se aparta como `settings.corrupto-*.json` y se arranca con valores de fábrica |

## Criterios de aceptación (SRS §11)

| Escenario | Estado | Cómo se ejercita |
|---|---|---|
| Atajo personalizado | ✅ | `scratchpad\probar-atajo.ps1`: intenta registrar la combinación desde otro proceso y distingue LIBRE de OCUPADO (1409) |
| Atajo inválido | ✅ | Poner `"Hotkey": "Win+Shift+S"` en `settings.json` y arrancar: el log da el motivo y se conserva la configuración previa |
| Captura y anotación | ⬜ | |
| Deshacer / rehacer | ⬜ | |
| Guardado local | ⬜ | |
| Privacidad | ⬜ | |
