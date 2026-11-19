## Qué cambia

<!-- Una o dos frases. Qué ve distinto quien usa la app, o qué cambia en el código. -->

## Por qué

<!-- Requisito del SRS (RF-xx / RNF-xx), ADR o bug que motiva el cambio. -->

## Cómo se probó

<!-- Evidencia, no "debería funcionar": comando + salida, o la receta manual exacta. -->

```
dotnet test tests\CapturaLocal.Tests

```

**Recorrido manual:**

1.

**Capturas** (si el cambio se ve en pantalla):

## Riesgos y pendientes

<!-- Qué no se probó (p. ej. multi-monitor), qué queda para otro PR. "Ninguno" vale. -->

## Checklist

- [ ] `dotnet test` en verde (el CI lo confirma en este PR)
- [ ] Fila nueva o actualizada en `docs\TRAZABILIDAD.md` si toca un requisito
- [ ] ADR en `docs\DECISIONS.md` si es una decisión de diseño
- [ ] Sin `PackageReference` ni código de red en `src\` (el CI lo confirma)
- [ ] Sin `DllImport` fuera de `Interop\NativeMethods.cs`
- [ ] Colores, iconos y estilos nuevos en `Theme.xaml`, no en la vista
- [ ] Los logs no escriben contenido de imagen ni rutas completas de capturas
- [ ] `README.md` sigue siendo solo la guía de descarga
- [ ] `CLAUDE.md` actualizado si cambia una convención o una nota viva
