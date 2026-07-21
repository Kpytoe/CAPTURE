# Trazabilidad SRS → código

Cada requisito del SRS con el archivo que lo implementa y cómo se comprueba.
Un requisito sin fila aquí **no está hecho**, por muy escrito que esté el código.

Estado: ✅ hecho y verificado · 🚧 en curso · ⬜ borrador 2

## Requisitos funcionales

| ID | Requisito | Estado | Implementado en | Verificación |
|---|---|---|---|---|
| RF-02 | Asignar combinación de teclas desde Configuración | ⬜ | | |
| RF-03 | Validar combinaciones reservadas o en uso, explicando el motivo | ⬜ | | |
| RF-04 | Elegir modo predeterminado (región / ventana / pantalla) | ⬜ | | Borrador 1: solo región |
| RF-05 | Arrastrar, ajustar o cancelar la selección | ⬜ | | |
| RF-06 | Abrir el editor de inmediato, a escala y con zoom | ⬜ | | |
| RF-07 | Herramientas: selección, lápiz, línea, rectángulo, elipse, texto | ⬜ | | Borrador 1: sin línea ni elipse; con pixelado |
| RF-08 | Color y grosor por herramienta; texto con color y tamaño | ⬜ | | |
| RF-09 | Seleccionar una anotación para moverla o eliminarla | ⬜ | | |
| RF-10 | Deshacer y rehacer | ⬜ | | |
| RF-11 | Copiar la imagen al portapapeles | ⬜ | | |
| RF-12 | Guardar en carpeta local y «Guardar como» | ⬜ | | Borrador 1: solo Guardar, PNG |
| RF-13 | Configurar carpeta, formato, calidad JPG y patrón de nombre | ⬜ | | Borrador 1: carpeta y formato |
| RF-14 | Configuración desde la bandeja, preferencias persistentes | 🚧 | `App.xaml.cs` (menú de bandeja), `Settings\SettingsStore.cs` | Menú con las 4 entradas del SRS §9; las preferencias sobreviven al reinicio. Falta la ventana de Configuración (Paso 3) |
| RF-15 | Cancelar captura o descartar edición, con confirmación | ⬜ | | |

## Requisitos no funcionales

| ID | Requisito | Estado | Implementado en | Verificación |
|---|---|---|---|---|
| RNF-01 | Overlay en ≤ 500 ms | ⬜ | | |
| RNF-02 | Captura visible en el editor en ≤ 1,5 s a 4K | ⬜ | | |
| RNF-03 | Sin transmisión de capturas ni telemetría | ⬜ | | `git grep -nE 'HttpClient\|WebClient\|WebRequest\|Socket\|telemetr' -- src` vacío |
| RNF-04 | Fallo al guardar conserva la edición y avisa | ⬜ | | |
| RNF-05 | Guardar/Copiar/Deshacer/Cancelar visibles o con atajo | ⬜ | | |
| RNF-06 | Navegable por teclado, con etiquetas accesibles | ⬜ | | |
| RNF-07 | Múltiples monitores y escala DPI | ⬜ | | |
| RNF-08 | Preferencias locales, versionadas y recuperables | ✅ | `Settings\SettingsStore.cs`, `Settings\AppSettings.cs` | `%LOCALAPPDATA%\CapturaLocal\settings.json` con `SchemaVersion`; escritura atómica (temp + `File.Replace`); un JSON dañado se aparta como `settings.corrupto-*.json` y se arranca con valores de fábrica |

## Criterios de aceptación (SRS §11)

| Escenario | Estado | Cómo se ejercita |
|---|---|---|
| Atajo personalizado | ⬜ | |
| Atajo inválido | ⬜ | |
| Captura y anotación | ⬜ | |
| Deshacer / rehacer | ⬜ | |
| Guardado local | ⬜ | |
| Privacidad | ⬜ | |
