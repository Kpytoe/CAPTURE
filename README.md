# Captura Local

Aplicación de escritorio para Windows que captura una región de la pantalla mediante un atajo
global, la abre de inmediato en un editor ligero y la guarda **solo en tu equipo**.

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

Qué está hecho y cómo se comprobó: [`docs/TRAZABILIDAD.md`](docs/TRAZABILIDAD.md).
Por qué está hecho así: [`docs/DECISIONS.md`](docs/DECISIONS.md).
Cómo se usa: [`docs/MANUAL.md`](docs/MANUAL.md).

Pendiente para el borrador 2: modos ventana y pantalla completa, herramientas de línea y
elipse, JPG con calidad, «Guardar como», patrón de nombre configurable, iniciar con Windows,
captura temporizada e instalador.

## Requisitos

- Windows 10 / 11 de 64 bits
- .NET SDK 8 para compilar (`winget install Microsoft.DotNet.SDK.8`)
- El runtime .NET 8 Desktop basta para ejecutar el `.exe` publicado

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
