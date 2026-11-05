---
name: nueva-herramienta
description: Añadir una herramienta de anotación al editor de Captura Local (línea, elipse, flecha, resaltador...). Usar cuando haya que implementar cualquier RF-07 pendiente o ampliar la barra del editor.
---

# Añadir una herramienta de anotación

Receta escrita después de implementar las cinco primeras (selección, lápiz, rectángulo, texto,
pixelado). Son **cinco sitios**; saltarse uno deja la herramienta a medias de una forma que
compila sin quejarse.

## 1. La anotación

Nueva clase en `src\CapturaLocal\Editor\Annotations\`, heredando de `Annotation`:

```csharp
public sealed class LineAnnotation : Annotation
{
    public Point Start { get; set; }
    public Point End { get; set; }

    public override void Render(DrawingContext dc) => dc.DrawLine(CreatePen(), Start, End);
    public override Rect Bounds => new Rect(Start, End);
    public override bool HitTest(Point p) { /* con margen HitTolerance */ }
    public override void Move(Vector d) { Start += d; End += d; }
}
```

Reglas que no son negociables:

- **Las coordenadas son píxeles de la imagen capturada**, nunca de la ventana ni de la
  pantalla. Si usas coordenadas de la ventana, la marca se descoloca al hacer zoom y sale en
  otro sitio en el archivo guardado.
- **`Move` tiene que ser exactamente reversible.** `UndoStack` deshace un arrastre llamando
  `Move(-delta)`. Si `Move` redondea o normaliza, deshacer no vuelve al punto de partida y se
  rompe el criterio de aceptación de deshacer/rehacer.
- **`HitTest` necesita margen.** Usa `HitTolerance`; acertar a una línea de 1 px con el ratón
  es imposible sin él.
- Si cacheas geometría (como `PencilAnnotation`), **invalida la caché en `Move` y en cada
  setter**. Es el olvido clásico: la marca se mueve pero se dibuja donde estaba.

## 2. El enum

`src\CapturaLocal\Editor\EditorTool.cs`: añade el valor.

## 3. El lienzo

`src\CapturaLocal\Editor\AnnotationCanvas.cs`, tres puntos:

- `OnMouseLeftButtonDown`: crea la anotación en curso con `Color` y `Thickness` actuales. El
  origen del arrastre está en `_dragAnchor` — **úsalo, no leas la esquina de la propia figura**,
  que se normaliza en cada movimiento y haría que el ancla se desplazase al arrastrar hacia
  arriba o hacia la izquierda.
- `OnMouseMove`: actualiza la figura y llama `InvalidateVisual()`.
- `OnMouseLeftButtonUp`: añade tu caso a la comprobación `isUseless`, para que un clic
  accidental no meta una figura de tamaño cero en el historial de deshacer.

## 4. La barra del editor

`EditorWindow.xaml`: un `ToggleButton` más con `Tag` igual al nombre del enum, `ToolTip` y
`AutomationProperties.Name` (RNF-06 exige etiquetas accesibles). El contenido es icono arriba y
etiqueta debajo, con los estilos del tema — **no fijes colores aquí**: el icono se pinta con el
`Foreground` del botón, que es lo que cambia en hover y al estar activo (ADR-006).

```xml
<ToggleButton x:Name="ToolLine" Style="{StaticResource Herramienta}" Tag="Line"
              ToolTip="Línea recta (N)" AutomationProperties.Name="Herramienta línea">
    <StackPanel>
        <Path Style="{StaticResource IconoHerramienta}" Data="{StaticResource IconoLinea}" />
        <TextBlock Style="{StaticResource EtiquetaHerramienta}" Text="Línea" />
    </StackPanel>
</ToggleButton>
```

La geometría `IconoLinea` va en `src\CapturaLocal\Theme.xaml`, junto a las demás, dibujada en un
lienzo de 24×24 para que encaje con las que ya están.

**Si es una figura** (como línea, círculo o rectángulo), no lleva botón propio: va como un
`ToggleButton` más dentro de `ShapePopup`, con `Click="ShapeOption_Click"`, y se añade su caso
en `ShowShape` (etiqueta, icono y marca) y en `ShapePopup_Opened`. El botón `ToolShape` siempre
muestra la última figura elegida.

`EditorWindow.xaml.cs`, en `SelectTool`: añade la línea que marca o desmarca tu botón —
funcionan como grupo de opciones excluyentes y se sincronizan a mano.

En `OnPreviewKeyDown`: la tecla rápida. Ojo, va **después** del `if (typing) return;` o esa
letra dejará de poder escribirse dentro de un cuadro de texto.

## 5. Nada que tocar al exportar

`Exporter.Rasterize` llama a `AnnotationCanvas.RenderContent`, que recorre la lista y pide a
cada anotación que se pinte. Es el mismo código que dibuja la pantalla, así que **si se ve, se
guarda**. Esa es la razón de que el lienzo tenga `OnRender` propio en vez de formas de WPF
sobre un `Canvas` (ADR-005): no existe la posibilidad de que lo visible y lo exportado
diverjan.

## Verificar

Cubre estas cuatro y la herramienta está de verdad terminada:

1. Dibujar la figura y comprobar que aparece.
2. Cambiar a «Seleccionar», hacer clic encima, arrastrarla y borrarla con Supr.
3. Deshacer y rehacer el dibujo **y** el arrastre.
4. Guardar y abrir el PNG: la figura tiene que estar, en la misma posición.

El punto 3 es el que más falla, y casi siempre por una caché sin invalidar.

Luego actualiza la fila del requisito en `docs\TRAZABILIDAD.md`: un RF sin fila ahí no está
hecho.
