# Especificación de Requisitos de Software (SRS)

## Aplicación local de capturas de pantalla y anotación rápida

**Documento:** SRS v1.0 (borrador base)  
**Producto:** Captura Local  
**Estado:** Propuesta para validación  
**Fecha:** 21 de julio de 2026

## 1. Propósito

Definir los requisitos de una aplicación de escritorio para Windows que permita capturar pantalla mediante atajos configurables, editar la captura de inmediato con herramientas esenciales y guardar el resultado exclusivamente en el equipo del usuario.

## 2. Alcance

La solución reemplaza el flujo “capturar → abrir Paint → editar → guardar” por una experiencia única y local. Incluye captura de región, ventana o pantalla; anotación básica; copia al portapapeles; guardado local y configuración de atajos.

Queda fuera del alcance de esta versión:

- Sincronización en la nube.
- Colaboración o cuentas de usuario.
- OCR, grabación de video o edición avanzada por capas.
- Filtros y biblioteca multimedia compleja.

## 3. Partes interesadas y perfiles

| Perfil | Necesidad principal |
|---|---|
| Usuario final | Capturar y marcar una imagen en segundos sin depender de Paint. |
| Usuario avanzado | Definir atajos que no interfieran con su flujo de trabajo. |
| Soporte / administración | Instalación simple, sin servicios en la nube ni datos fuera del equipo. |

## 4. Supuestos y restricciones

- Sistema objetivo inicial: Windows 10 y Windows 11 de 64 bits.
- La aplicación funciona localmente: imágenes, configuraciones y registros permanecen en el dispositivo, salvo que el usuario copie o comparta un archivo manualmente.
- La aplicación debe informar de forma clara si un atajo está reservado por Windows o ya es usado por otra aplicación.
- Los formatos iniciales de exportación son PNG y JPG; PNG es el predeterminado.

## 5. Descripción general del producto

### 5.1 Flujo principal

1. El usuario pulsa el atajo global configurado, debera ser definida por el usuario.
2. La pantalla entra en modo de selección y el usuario elige región, ventana o pantalla completa.
3. La captura se abre inmediatamente en un editor ligero.
4. El usuario agrega anotaciones o texto.
5. El usuario copia, guarda, guarda como o descarta el resultado.

### 5.2 Principios de experiencia

- **Rapidez:** el primer trazo debe estar disponible sin abrir ventanas adicionales.
- **Simplicidad:** pocas herramientas, visibles y previsibles.
- **Privacidad:** no se sube ni procesa la captura en servidores externos.
- **Reversibilidad:** deshacer y rehacer antes de guardar.

## 6. Requisitos funcionales

| ID | Requisito | Prioridad |
|---|---|---|
| RF-02 | El usuario debe poder asignar una combinación de teclas como `Shift + 5` u otra combinación válida desde Configuración. | Alta |
| RF-03 | El sistema debe validar combinaciones reservadas, ya usadas por la aplicación o no registrables. Debe explicar el motivo y no guardar una configuración inválida. | Alta |
| RF-04 | El usuario debe poder elegir el modo predeterminado: región rectangular, ventana activa o pantalla completa. | Alta |
| RF-05 | En modo región, el usuario debe poder arrastrar, ajustar o cancelar la selección antes de capturar. | Alta |
| RF-06 | Tras capturar, la aplicación debe abrir el editor de forma inmediata, mostrar la imagen a escala y permitir zoom. | Alta |
| RF-07 | El editor debe incluir selección/mover, lápiz, línea, rectángulo, elipse/círculo y texto. | Alta |
| RF-08 | Cada herramienta de anotación debe permitir al menos color y grosor; texto debe permitir contenido, color y tamaño básico. | Alta |
| RF-09 | El usuario debe poder seleccionar una anotación creada para moverla o eliminarla. | Media |
| RF-10 | El editor debe ofrecer deshacer y rehacer para las acciones de anotación de la sesión actual. | Alta |
| RF-11 | El usuario debe poder copiar la imagen resultante al portapapeles. | Alta |
| RF-12 | El usuario debe poder guardar en una carpeta local elegida y usar “Guardar como” para cambiar nombre, ubicación o formato. | Alta |
| RF-13 | El sistema debe permitir configurar carpeta predeterminada, formato, calidad JPG y patrón de nombre. | Media |
| RF-14 | El usuario debe poder abrir Configuración desde el icono de bandeja y volver a invocar la aplicación sin perder sus preferencias. | Media |
| RF-15 | El usuario debe poder cancelar una captura o descartar una edición sin guardar. Si hay cambios, debe solicitar confirmación. | Alta |

### 6.1 Herramientas esenciales del editor

| Herramienta | Comportamiento mínimo |
|---|---|
| Selección / mover | Selecciona una anotación para moverla o eliminarla; no modifica píxeles originales en esta versión. |
| Lápiz | Dibujo libre con color y grosor configurables. |
| Línea | Línea recta entre dos puntos; color y grosor configurables. |
| Rectángulo | Contorno rectangular; color, grosor y opción de relleno semitransparente. |
| Elipse / círculo | Contorno elíptico; con tecla modificadora puede forzarse una proporción circular. |
| Texto | Inserta texto editable antes de confirmar; color y tamaño básico configurables. |

## 7. Requisitos no funcionales

| ID | Categoría | Requisito |
|---|---|---|
| RNF-01 | Rendimiento | En un equipo compatible, el modo de selección debe aparecer en ≤ 500 ms para el 90 % de activaciones sin carga extrema. |
| RNF-02 | Rendimiento | La captura debe mostrarse en el editor en ≤ 1,5 s para una pantalla 4K en condiciones normales. |
| RNF-03 | Privacidad | La aplicación no debe transmitir capturas, miniaturas, configuración ni telemetría fuera del equipo sin una acción expresa del usuario. |
| RNF-04 | Confiabilidad | Ante fallo al guardar, la aplicación debe conservar la edición en memoria y mostrar un mensaje recuperable. |
| RNF-05 | Usabilidad | Guardar, Copiar, Deshacer y Cancelar deben ser visibles o accesibles mediante atajos documentados. |
| RNF-06 | Accesibilidad | La interfaz debe ser navegable con teclado y mostrar etiquetas o descripciones accesibles para controles principales. |
| RNF-07 | Compatibilidad | Debe soportar múltiples monitores y respetar la escala DPI de Windows. |
| RNF-08 | Mantenibilidad | Las preferencias deben almacenarse localmente, versionadas y recuperables ante cambios de versión. |

## 8. Reglas de negocio

- La configuración de atajo solo se aplica después de una validación satisfactoria.
- El nombre por defecto será `Captura_YYYY-MM-DD_HH-mm-ss.png`; el usuario puede cambiar el patrón mediante campos de fecha y hora documentados.
- Guardar sobrescribe un archivo existente únicamente después de confirmación explícita.
- Copiar al portapapeles no obliga a guardar en disco.
- Las anotaciones se rasterizan durante la exportación. La edición de una captura cerrada no se garantiza para la primera versión.

## 9. Interfaz y navegación

| Componente | Contenido esperado |
|---|---|
| Icono de bandeja | Nueva captura, Configuración, Abrir carpeta de guardado y Salir. |
| Overlay de captura | Instrucción breve, cursor de selección y control para cancelar; muestra las dimensiones de la región. |
| Editor | Lienzo, barra de herramientas, color/grosor, deshacer/rehacer, copiar, guardar, guardar como y cancelar. |
| Configuración | Atajo, modo de captura, carpeta, formato, calidad JPG, patrón de nombre y opción de iniciar con Windows. |
| Mensajes | Errores de atajo, guardado o permisos; confirmación de copia/guardado; advertencia por cambios sin guardar. |

## 10. Datos locales

| Dato | Ubicación / retención |
|---|---|
| Capturas exportadas | Carpeta local configurada por el usuario; no hay subida automática. |
| Preferencias | Directorio de datos de la aplicación del usuario, por ejemplo `AppData` local. |
| Historial de edición | Solo en memoria durante la sesión de la captura; se elimina al cerrar o descartar. |
| Registros técnicos | Locales, rotativos y sin contenido de imagen. Deben poder desactivarse o borrarse desde soporte. |

## 11. Criterios de aceptación

| Escenario | Dado / cuando / entonces |
|---|---|
| Atajo personalizado | Dado que el usuario abre Configuración, cuando registra `Shift + 5` y no existe conflicto, entonces la próxima pulsación inicia una captura. |
| Atajo inválido | Dado un atajo reservado o en conflicto, cuando el usuario intenta guardarlo, entonces se muestra un motivo y se conserva el atajo anterior. |
| Captura y anotación | Dada una región seleccionada, cuando el usuario dibuja una línea, rectángulo, círculo, texto y trazo libre, entonces todas las marcas se ven en el lienzo y se incluyen al guardar. |
| Deshacer / rehacer | Dado que se agregaron dos anotaciones, cuando el usuario deshace y rehace, entonces el estado visual vuelve exactamente a cada paso esperado. |
| Guardado local | Dada una captura anotada, cuando el usuario pulsa Guardar, entonces se crea un archivo PNG local en la carpeta configurada y se informa la ruta. |
| Privacidad | Dado que se toma y guarda una captura, cuando se revisa el tráfico de la aplicación, entonces no se observan transferencias de imagen ni telemetría saliente. |

## 12. Casos límite y manejo de errores

- Si la carpeta configurada no existe o no tiene permisos, pedir una nueva ubicación y no perder la captura actual.
- Si Windows bloquea el atajo, mantener el atajo anterior y sugerir una combinación alternativa.
- Si hay varios monitores con escalas distintas, la región seleccionada y las anotaciones deben mantener la posición correcta en el resultado final.
- Si el portapapeles está ocupado, informar el error y permitir reintentar o guardar.
- Si el usuario cierra el editor con cambios, solicitar **Guardar**, **Descartar** o **Cancelar**.

## 13. Entregables sugeridos para la primera versión

- Aplicación instalable para Windows, ejecutable local y con icono de bandeja.
- Manual breve: cambiar atajo, elegir carpeta, capturar, anotar, copiar y guardar.
- Pruebas funcionales de los criterios de aceptación y pruebas con uno o varios monitores.

## 14. Preguntas abiertas para la siguiente validación

- ¿La aplicación debe iniciar automáticamente con Windows por defecto o solo ofrecer la opción?
- ¿Se requiere difuminado o pixelado para ocultar información sensible, aunque no forme parte del alcance esencial?
- ¿La primera versión necesita capturar menús desplegables o ventanas que desaparecen al perder foco, mediante una captura temporizada?
- ¿Qué combinaciones de atajo deben recomendarse cuando `Shift + 5` o `Win + Shift + S` no se puedan registrar?
