# Trazabilidad SRS → código

Cada requisito del SRS con el archivo que lo implementa y cómo se comprueba.
Un requisito sin fila aquí **no está hecho**, por muy escrito que esté el código.

Estado: ✅ hecho y verificado · 🚧 parcial en el borrador 1 · ⬜ borrador 2

## Requisitos funcionales

| ID | Requisito | Estado | Implementado en | Verificación |
|---|---|---|---|---|
| RF-02 | Asignar combinación de teclas desde Configuración | ✅ | `Settings\SettingsWindow.xaml.cs`, `Settings\HotkeyDefinition.cs`, `Interop\HotKeyManager.cs` | Con la app corriendo, registrar `Ctrl+Shift+2` desde otro proceso devuelve 1409; sin ella se registra sin problema. 12 pruebas de ida y vuelta del formato guardado |
| RF-03 | Validar combinaciones reservadas o en uso, explicando el motivo | ✅ | `Interop\HotKeyManager.cs` (`Validate` + `DescribeFailure`) | Arrancar con `Win+Shift+S` en settings.json: el log da el motivo exacto, la app sigue viva y las preferencias no se tocan |
| RF-04 | Elegir modo predeterminado (región / ventana / pantalla) | 🚧 | `Capture\CaptureService.cs` | Solo modo región. Ventana activa y pantalla completa van al borrador 2 |
| RF-05 | Arrastrar, ajustar o cancelar la selección | ✅ | `Capture\OverlayWindow.xaml.cs`, `Capture\SelectionSession.cs` | `probar-captura.ps1`: arrastra una región conocida y compara el PNG con una referencia de pantalla → 26 800/26 800 píxeles idénticos. Esc y clic derecho cancelan |
| RF-06 | Abrir el editor de inmediato, a escala y con zoom | ✅ | `Editor\EditorWindow.xaml.cs` (`FitZoomToWindow`, `Scroller_PreviewMouseWheel`) | El editor abre solo al soltar; la imagen se ajusta a la ventana sin ampliarse por encima del 100 %; Ctrl+rueda hace zoom entre 10 % y 800 % |
| RF-07 | Herramientas: selección, lápiz, línea, rectángulo, elipse, texto | 🚧 | `Editor\Annotations\` (`LineAnnotation`, `EllipseAnnotation`), `Editor\AnnotationCanvas.cs`, `Editor\EditorWindow.xaml` (menú de figuras) | Las seis del SRS más **pixelar**. Línea, círculo y rectángulo comparten un botón con menú; `Mayús` fuerza línea a 45°, círculo y cuadrado (SRS 6.1). Texto: arrastrar define el cuadro inicial, que crece hacia abajo; un cuadro más estrecho que ~4 letras se toma como mínimo y crece también a lo ancho. Compila sin avisos con SDK 8.0.427; `dotnet test` 38/38 en verde, incluidas las 4 de `FigurasTests` (selección con margen, arrastre reversible). **Pendiente:** el recorrido manual de la receta `nueva-herramienta` (menú de figuras, Mayús, cuadro de texto estrecho) |
| RF-08 | Color y grosor por herramienta; texto con color y tamaño | ✅ | `Editor\EditorWindow.xaml.cs` (`BuildSwatches`), `Theme.xaml`, `Editor\Annotations\Annotation.cs` | 6 muestras de color y 6 de grosor en la barra, generadas desde los arrays `Colores` y `Grosores`. Comprobado dibujando un trazo con cada color y contando el resultado en el PNG guardado: los 6 valores exactos aparecen, 1 488 px muestreados cada uno. En la herramienta de texto el grosor gobierna el tamaño de letra y la etiqueta pasa a «Tamaño» |
| RF-09 | Seleccionar una anotación para moverla o eliminarla | ✅ | `Editor\AnnotationCanvas.cs` (`HitTest`, `DeleteSelected`) | Posible porque las anotaciones son objetos y no píxeles (ADR-005). Gana la marca dibujada más recientemente; `Supr` la borra |
| RF-10 | Deshacer y rehacer | ✅ | `Editor\UndoStack.cs` | 6 pruebas unitarias: ida y vuelta, rama abandonada tras deshacer, borrado que vuelve a su índice original, movimiento reversible exacto |
| RF-11 | Copiar la imagen al portapapeles | ✅ | `Services\ClipboardService.cs` | 8 reintentos con espera: el portapapeles es exclusivo y cualquier app puede tenerlo abierto justo en ese instante |
| RF-12 | Guardar en carpeta local y «Guardar como» | 🚧 | `Services\SaveService.cs` | Guardar en PNG funciona, con confirmación de sobrescritura y escritura a temporal + intercambio. **«Guardar como» va al borrador 2** |
| RF-13 | Configurar carpeta, formato, calidad JPG y patrón de nombre | 🚧 | `Settings\SettingsWindow.xaml`, `Settings\AppSettings.cs` | Carpeta y registro configurables. **Formato JPG, calidad y patrón de nombre van al borrador 2** |
| RF-14 | Configuración desde la bandeja, preferencias persistentes | ✅ | `App.xaml.cs`, `Settings\SettingsWindow.xaml` | Menú con las 4 entradas del SRS §9; `--configuracion` abre la ventana; las preferencias sobreviven al reinicio |
| RF-15 | Cancelar captura o descartar edición, con confirmación | ✅ | `Capture\SelectionSession.cs`, `Editor\EditorWindow.xaml.cs` (`OnClosing`) | Esc/clic derecho cancelan la captura; cerrar el editor con marcas sin guardar ofrece Guardar / Descartar / Cancelar |

## Requisitos no funcionales

| ID | Requisito | Estado | Implementado en | Verificación |
|---|---|---|---|---|
| RNF-01 | Overlay en ≤ 500 ms | ✅ | `Interop\ScreenCapture.cs`, `Capture\CaptureService.cs` | El log lo mide: **323 ms** en un monitor 2560×1440, incluyendo el arranque en frío del proceso |
| RNF-02 | Captura visible en el editor en ≤ 1,5 s a 4K | 🚧 | `Capture\CaptureService.cs` | No medido: no hay pantalla 4K disponible. A 2560×1440 el editor abre de forma inmediata |
| RNF-03 | Sin transmisión de capturas ni telemetría | ✅ | Todo `src\` | `git grep -nE 'HttpClient\|WebClient\|WebRequest\|Socket\|telemetr' -- src` devuelve vacío. Cero `PackageReference` en la app (ADR-001), así que no hay dependencia que pueda abrir una conexión |
| RNF-04 | Fallo al guardar conserva la edición y avisa | ✅ | `Services\SaveService.cs`, `Editor\EditorWindow.xaml.cs` | Ningún camino de error cierra la ventana; cada excepción se traduce a un mensaje que dice explícitamente que la captura no se ha perdido |
| RNF-05 | Guardar/Copiar/Deshacer/Cancelar visibles o con atajo | ✅ | `Editor\EditorWindow.xaml`, `Theme.xaml` | Los cuatro son botones visibles **y** tienen atajo: Ctrl+S, Ctrl+C, Ctrl+Z, Esc. La jerarquía visual los distingue: Guardar en acento, Copiar con borde, Cancelar plano; deshacer/rehacer se atenúan al 35 % cuando no hay nada que deshacer |
| RNF-06 | Navegable por teclado, con etiquetas accesibles | ✅ | `Editor\EditorWindow.xaml`, `Editor\EditorWindow.xaml.cs` (`BuildSwatches`), `Settings\SettingsWindow.xaml` | `AutomationProperties.Name` en todos los controles principales, incluidas las 12 muestras generadas en código («Color Rojo», «Grosor 8»…). Cada estilo del tema define su anillo de foco de teclado, porque al quitar el `FocusVisualStyle` por defecto el foco dejaría de verse. El grabador de atajo deja pasar el tabulador a propósito para no atrapar el foco |
| RNF-07 | Múltiples monitores y escala DPI | 🚧 | `app.manifest`, `Interop\MonitorInfo.cs`, `Capture\OverlayWindow.xaml.cs` | Implementado: un overlay por monitor, píxeles físicos, `SetWindowPos` exacto, `GetCursorPos` en vez de coordenadas WPF. **Sin verificar:** el equipo de desarrollo tiene un solo monitor |
| RNF-08 | Preferencias locales, versionadas y recuperables | ✅ | `Settings\SettingsStore.cs`, `Settings\AppSettings.cs` | `SchemaVersion` + escritura atómica; un JSON dañado se aparta como `settings.corrupto-*.json` y la app arranca con valores de fábrica |

## Criterios de aceptación (SRS §11)

| Escenario | Estado | Cómo se ejercita |
|---|---|---|
| Atajo personalizado | ✅ | `probar-atajo.ps1`: intenta registrar la combinación desde otro proceso y distingue LIBRE de OCUPADO (1409) |
| Atajo inválido | ✅ | Poner `"Hotkey": "Win+Shift+S"` en `settings.json` y arrancar: el log da el motivo y se conserva la configuración previa |
| Captura y anotación | 🚧 | `probar-editor.ps1`: captura 800×500, dibuja con lápiz, rectángulo y pixelado, guarda, y cuenta 5 441 píxeles rojos en el PNG. Línea y círculo ya existen, pero **falta repetir el recorrido con ellas** y comprobarlas en el PNG |
| Deshacer / rehacer | ✅ | 6 pruebas unitarias sobre `UndoStack` + el recorrido de `probar-editor.ps1` (deshacer ×2, rehacer ×2) |
| Guardado local | ✅ | `probar-editor.ps1`: el PNG aparece en la carpeta configurada con las marcas incrustadas, y el editor informa de la ruta |
| Privacidad | ✅ | El `git grep` de RNF-03 devuelve vacío. Pendiente de comprobación con analizador de tráfico en una sesión real |

## Entregables (SRS §13)

| Entregable | Estado | Implementado en | Verificación |
|---|---|---|---|
| Aplicación instalable para Windows, ejecutable local, con icono de bandeja | 🚧 | `src\CapturaLocal\Properties\PublishProfiles\Portable.pubxml`, `.github\workflows\release.yml` (ADR-007) | `dotnet publish ... -p:PublishProfile=Portable -p:Version=0.0.1` → un `CapturaLocal.exe` de 63 MB, versión 0.0.1.0. Copiado fuera del repo: arranca, registra `Ctrl+Shift+2` (otro proceso recibe 1409), no carga ningún módulo de `Program Files\dotnet`, y `--capturar` + Esc abre y cancela el overlay. Lógica de los pasos de privacidad y SHA-256 del flujo probada en local. **Pendiente:** primera ejecución real del flujo en GitHub (requiere subir una etiqueta) y probarlo en un equipo sin .NET. Es portable, no instalador: sin acceso en el menú Inicio ni entrada en «Desinstalar» |

## Resumen del borrador 1

**11 de 14 requisitos funcionales completos**, 3 parciales por alcance decidido (modos de
captura, línea/elipse, JPG y «Guardar como»). **6 de 8 no funcionales completos**; RNF-02 sin
medir por falta de pantalla 4K y RNF-07 sin verificar por falta de un segundo monitor.
