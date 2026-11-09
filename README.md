# Captura Local

Aplicación de escritorio para Windows que captura una región de la pantalla mediante un atajo
global, la abre de inmediato en un editor ligero y la guarda **solo en tu equipo**.

**Versión actual: [v0.0.1](https://github.com/Kpytoe/CAPTURE/releases/tag/v0.0.1)** ·
[⬇ Descargar `CapturaLocal-0.0.1-win-x64.exe`](https://github.com/Kpytoe/CAPTURE/releases/download/v0.0.1/CapturaLocal-0.0.1-win-x64.exe)
· un solo archivo, sin instalar nada · Windows 10/11 de 64 bits

## Las tres preguntas

1. **¿Qué problema resuelve?** Capturar una región de pantalla y anotarla en segundos, sin abrir
   Paint y sin que la imagen pase por ningún servidor.
2. **¿Quién lo usa?** Una sola persona, en su equipo Windows. Sin cuentas, sin backend, sin nube.
3. **¿Qué demuestra que funciona?** Pulsar `Ctrl+Shift+2`, arrastrar una región, dibujar un trazo
   rojo y ver el PNG en la carpeta de guardado.

## Estado

**Borrador 1 (vertical slice) — funcionando.** La cadena completa atajo → región → editor →
copiar/guardar, con cinco herramientas de anotación (seleccionar, lápiz, rectángulo, texto y
pixelar) y deshacer/rehacer.

**Borrador 2, paso 0 — hecho:** pasada de acabado visual. Tema oscuro propio en `Theme.xaml`,
herramientas con icono vectorial y etiqueta, color y grosor como muestras en línea, barra de
título oscura e icono propio de la aplicación. Sin cambios de funcionalidad y sin dependencias
nuevas (ADR-006).

**Borrador 2, paso 1 — compilado, falta el recorrido manual:** línea, círculo y rectángulo en
un solo botón con menú, y texto con cuadro definido por arrastre.

**v0.0.1 — primera versión descargable:** un `.exe` portable autocontenido publicado en GitHub
Releases por GitHub Actions (ADR-007).

Qué está hecho y cómo se comprobó: [`docs/TRAZABILIDAD.md`](docs/TRAZABILIDAD.md).
Por qué está hecho así: [`docs/DECISIONS.md`](docs/DECISIONS.md).
Cómo se usa: [`docs/MANUAL.md`](docs/MANUAL.md).

Pendiente para el borrador 2: modos ventana y pantalla completa, JPG con calidad,
«Guardar como», patrón de nombre configurable, iniciar con Windows, captura temporizada e instalador (hoy se distribuye como `.exe` portable).

## Descargar y usar

1. Entra en **[Releases](https://github.com/Kpytoe/CAPTURE/releases/latest)** y descarga
   `CapturaLocal-X.Y.Z-win-x64.exe` (el `.sha256` de al lado sirve para comprobar que el
   archivo llegó íntegro: `Get-FileHash .\CapturaLocal-*.exe`).
2. Ponlo donde quieras (por ejemplo `%LOCALAPPDATA%\Programs\CapturaLocal\`) y ábrelo.
   No hace falta instalar .NET ni nada más.
3. Como el `.exe` no está firmado, Windows avisará la primera vez: **Más información →
   Ejecutar de todas formas**.

La app aparece en la bandeja del sistema. Pulsa `Ctrl+Shift+2` para capturar.
Detalles en [`docs/MANUAL.md`](docs/MANUAL.md).

## Requisitos

- Windows 10 / 11 de 64 bits
- .NET SDK 8 para compilar (`winget install Microsoft.DotNet.SDK.8`)
- Para usar el `.exe` de Releases no hace falta nada: lleva el runtime dentro

## Publicar una versión

```powershell
dotnet publish src\CapturaLocal\CapturaLocal.csproj -p:PublishProfile=Portable   # -> artifacts\portable\CapturaLocal.exe
git tag v0.0.2
git push origin v0.0.2   # GitHub Actions prueba, publica y crea la Release con el .exe
```

Por qué un `.exe` portable y no un instalador: ADR-007 en [`docs/DECISIONS.md`](docs/DECISIONS.md).

## Cómo correr en local

```powershell
cd C:\CODE\Cut
dotnet build src\CapturaLocal\CapturaLocal.csproj
dotnet test  tests\CapturaLocal.Tests
dotnet run   --project src\CapturaLocal\CapturaLocal.csproj
```

La app no abre ventana al arrancar: vive en el **icono de la bandeja del sistema**.

## Estructura

```
Cut\
├── .claude\        Convenciones del proyecto para Claude Code (CLAUDE.md, settings, skills)
├── .github\        Flujo de GitHub Actions que publica el .exe en Releases
├── docs\           SRS, decisiones de arquitectura (ADR), trazabilidad y manual
├── src\            Código de la aplicación (CapturaLocal, WPF / .NET 8)
└── tests\          Pruebas unitarias (xunit) de la lógica pura
```

Este árbol lista **solo carpetas que existen**. Si se crea una, se documenta en el mismo commit.

## Privacidad

La aplicación no contiene código de red. Verificable:

```powershell
git grep -nE 'HttpClient|WebClient|WebRequest|Socket|telemetr' -- src
```

Debe devolver vacío.
