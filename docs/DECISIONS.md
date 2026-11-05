# Decisiones de arquitectura (ADR)

Formato: contexto → decisión → alternativas descartadas → reversibilidad.
Una decisión que da para discutir alternativas vive aquí. Si no da para eso, es un buen mensaje
de commit.

---

## ADR-001 — C# / WPF / .NET 8 en vez del stack web por defecto

**Fecha:** 2026-07-21

**Contexto.** El stack por defecto de este usuario (probado en FINANCE) es HTML + CSS + JS vanilla
desplegado en Vercel. Este producto es una app de escritorio que necesita atajos de teclado
globales, lectura del framebuffer de todos los monitores, icono en la bandeja del sistema y
acceso al portapapeles de Windows. Nada de eso existe dentro de un navegador.

**Decisión.** C# sobre WPF / .NET 8 (LTS), **cero paquetes NuGet en tiempo de ejecución**. Todo lo
que hace falta viene en caja o vía P/Invoke: `RegisterHotKey`, `BitBlt`, `NotifyIcon` de WinForms,
DPI per-monitor V2, `System.Text.Json`, `System.Windows.Clipboard`.

Cero dependencias tiene un segundo beneficio que pesa más que la comodidad: **RNF-03 se vuelve
auditable**. La promesa "ninguna captura sale del equipo" deja de ser una afirmación y pasa a ser
comprobable con un `grep` — si no existe `HttpClient` en el árbol, no hay tráfico posible.

El runtime .NET 8 Desktop 8.0.23 ya estaba instalado en el equipo, así que la app corre sin
empaquetar nada extra.

**Alternativas descartadas.**
- *Electron + TypeScript:* ~150 MB de Chromium dentro de una app cuyo argumento de venta es la
  privacidad; un proceso de navegador completo contradice esa promesa a ojos de una auditoría de
  soporte. Además Node no está instalado.
- *Python + PySide6:* el `python` del equipo es el stub de Microsoft Store, no un intérprete real.
  Atajos globales, DPI mixto y empaquetado a `.exe` (PyInstaller) son notablemente más frágiles en
  Windows.

**Reversibilidad.** Alta. La pieza grande es el editor, y es lógica pura sobre una lista de
anotaciones y un `Canvas`; portarla a otro toolkit no requiere rediseñar el modelo.

---

## ADR-002 — El repositorio vive fuera de OneDrive

**Fecha:** 2026-07-21

**Contexto.** El proyecto nació en `C:\Users\bvelasco\OneDrive - BSMART\Escritorio\CODE\Cut`.

**Decisión.** Se movió a `C:\CODE\Cut` antes de escribir la primera línea de código.

**Porqué.** OneDrive **no respeta `.gitignore`**. `bin/` y `obj/` se regeneran en cada compilación
con cientos de archivos, y OneDrive los sincronizaría todos: builds lentos, bloqueos de archivo en
mitad de un `dotnet build` y tormentas de sincronización. `.gitignore` protege el repo, no el
disco. Mover el proyecto cuando solo existía un `.md` costó dos segundos; con código dentro habría
costado una tarde.

**Reversibilidad.** Total — es una carpeta.

---

## ADR-003 — La carpeta de guardado por defecto NO es «Imágenes»

**Fecha:** 2026-07-21

**Contexto.** En este equipo, la carpeta *Mis Imágenes* está redirigida por política a
`C:\Users\bvelasco\OneDrive - BSMART\Imágenes`, es decir, a OneDrive **corporativo**.

**Decisión.** La carpeta de guardado por defecto es `%USERPROFILE%\Capturas`. Si el usuario elige a
mano una carpeta que esté por debajo de `$env:OneDrive`, Configuración muestra una advertencia
visible; no la bloquea — es su decisión, pero informada.

**Porqué.** Guardar por defecto en *Imágenes* subiría cada captura a la nube de BSMART. Eso rompe
RNF-03 («no debe transmitir capturas… fuera del equipo sin una acción expresa del usuario») y el
criterio de aceptación de privacidad del SRS §11. El valor por defecto más «natural» de Windows es
justamente el que traiciona el producto.

**Reversibilidad.** Total — es un valor en `settings.json`.

---

## ADR-004 — Un overlay por monitor, con coordenadas en píxeles físicos

**Fecha:** 2026-07-21

**Contexto.** El SRS exige soportar múltiples monitores y respetar la escala DPI (RNF-07), y
señala como caso límite (§12) los monitores con escalas distintas.

**Decisión.**
1. La app se declara **per-monitor DPI aware V2** en `app.manifest`.
2. Al dispararse el atajo se captura de una vez todo el escritorio virtual con `BitBlt`, en
   píxeles físicos, y esa imagen se **congela**.
3. Se muestra **una ventana borderless por monitor**, cada una con su recorte de la imagen
   congelada. La selección vive siempre en el espacio de píxeles físicos del escritorio virtual.
4. El recorte final se toma del bitmap físico.

**Porqué.** Bajo per-monitor V2, una sola ventana que cruce dos monitores con escalas distintas
recibe un único DPI, y las coordenadas del ratón dejan de corresponder con los píxeles reales: la
región queda desplazada. Una ventana por monitor elimina la ambigüedad. Congelar la imagen antes
de mostrar el overlay tiene un beneficio adicional: la pantalla deja de moverse mientras el
usuario selecciona, y el overlay puede aparecer en menos de 500 ms (RNF-01) porque no hay nada que
recomponer.

**Reversibilidad.** Media — la aritmética de coordenadas está aislada en `Interop\MonitorInfo.cs`
y cubierta por pruebas unitarias.

---

## ADR-005 — Anotaciones vectoriales, rasterizadas solo al exportar

**Fecha:** 2026-07-21

**Contexto.** RF-09 pide seleccionar una anotación para moverla o borrarla; RF-10 pide
deshacer/rehacer; la regla de negocio §8 dice que «las anotaciones se rasterizan durante la
exportación».

**Decisión.** Las anotaciones son objetos en una lista (`List<Annotation>`), no píxeles pintados
sobre el bitmap. El deshacer/rehacer es una pila de comandos sobre esa lista. Al copiar o guardar,
`RenderTargetBitmap` compone imagen + anotaciones en un solo mapa de bits.

**Porqué.** Si se pintara directamente sobre los píxeles, mover una anotación ya dibujada sería
imposible sin trucos (guardar snapshots del bitmap completo por cada trazo), y deshacer costaría
memoria proporcional al número de acciones. Con objetos, ambas cosas son triviales y el bitmap
original nunca se toca — que es exactamente lo que dice la tabla de herramientas del SRS §6.1 para
Selección/mover: «no modifica píxeles originales en esta versión».

**Excepción consciente:** la herramienta de pixelado sí lee píxeles del bitmap origen para
renderizarse, pero tampoco lo modifica: se dibuja como un recorte submuestreado por encima.

**Reversibilidad.** Baja en la práctica — es el modelo central del editor. Es la decisión más
costosa de revertir de las cinco, y por eso está escrita.

---

## ADR-006 — Tema propio en XAML, con iconos vectoriales escritos a mano

**Fecha:** 2026-07-22

**Contexto.** El borrador 1 funcionaba pero no se veía terminado: la barra del editor era
oscura (`#2D2D30`) y encima flotaban `ToggleButton` y `ComboBox` con el chrome **claro por
defecto de WPF**. Las herramientas eran texto plano en botones de 86 px, el color se elegía en
un desplegable que decía «Rojo» en vez de enseñar rojo, y ninguna ventana declaraba `Icon`, así
que en la barra de tareas salía el icono genérico de WPF y no `Assets\app.ico`.

**Decisión.**
1. Un único `src\CapturaLocal\Theme.xaml` con los colores, los iconos y los estilos, fusionado
   en `App.xaml`. Un archivo, no una carpeta: el árbol del README no se toca por esto.
2. Los **iconos son geometrías `Path` escritas en ese archivo**, no glifos de una fuente.
3. Color y grosor pasan de dos `ComboBox` a **muestras en línea**: seis círculos y seis puntos.
4. La barra de título se pinta en oscuro con `DwmSetWindowAttribute`
   (`DWMWA_USE_IMMERSIVE_DARK_MODE`), vía el `DllImport` que manda la convención, en
   `Interop\NativeMethods.cs`.

**Porqué los iconos no son una fuente.** `Segoe MDL2 Assets` viene con Windows 10 y 11 y habría
sido menos código. Pero es una dependencia externa igual: si la fuente falta o el sistema la
sustituye, la barra se llena de cuadros vacíos, y eso pasa en el equipo del usuario, no en el
build. Una geometría no puede fallar, es nítida a cualquier DPI y — al pintarse con el
`Foreground` del botón — cambia de color con el estado sin duplicar recursos. La misma lógica
de ADR-001 aplicada a la capa visual: menos cosas de las que depender, menos formas de romperse.

**Porqué muestras en vez de desplegables.** Un `ComboBox` de colores exige dos clics y muestra
el nombre del color, no el color. Las muestras son un clic, enseñan lo que hacen y ocupan menos
ancho. De paso evitan replantillar `ComboBox` + `ComboBoxItem` + `Popup`, que es lo más verboso
de todo el tema. Cada muestra lleva `AutomationProperties.Name`, así que RNF-06 se mantiene.

**Alternativas descartadas.**
- *Barra de título propia con `WindowChrome`:* control total del aspecto a cambio de
  reimplementar arrastrar, maximizar, ajustar a los bordes y el menú del sistema. Mucho riesgo
  para una ventana que se abre y se cierra en veinte segundos.
- *Un paquete de tema (MahApps, ModernWpf):* rompe ADR-001 de frente. Todo el argumento de
  privacidad auditable descansa en que en `src\` no hay ni una dependencia.

**Reversibilidad.** Alta. El tema es un `ResourceDictionary`: quitarlo de `App.xaml` devuelve la
aplicación al aspecto por defecto de WPF sin tocar una línea de lógica.
