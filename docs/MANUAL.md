# Captura Local — manual breve

## Poner en marcha

Ejecuta `CapturaLocal.exe`. No se abre ninguna ventana: la aplicación queda esperando en el
**icono de la bandeja del sistema**.

> En Windows 11, los iconos nuevos aparecen escondidos detrás de la flecha `^` de la barra de
> tareas. Para tenerlo siempre a la vista, arrástralo desde ahí hasta la barra.

Haz clic derecho en el icono para: **Nueva captura**, **Configuración**, **Abrir carpeta de
guardado** y **Salir**. Doble clic lanza una captura directamente.

## Capturar

1. Pulsa **Ctrl + Shift + 2** (o el atajo que hayas configurado).
2. La pantalla se oscurece. **Arrastra** para elegir la región; verás sus medidas en píxeles
   mientras la ajustas.
3. Suelta y se abre el editor.

Para cancelar sin capturar: **Esc** o **clic derecho**.

## Anotar

| Herramienta | Tecla | Qué hace |
|---|---|---|
| Seleccionar | `S` | Elige una marca ya hecha para moverla o borrarla con `Supr` |
| Lápiz | `L` | Dibujo libre |
| Rectángulo | `R` | Recuadro |
| Texto | `T` | Clic donde quieras escribir; `Intro` confirma, `Esc` descarta |
| Pixelar | `P` | Tapa información sensible con bloques |

El **color** y el **grosor** de la barra superior se aplican a la siguiente marca que dibujes.
En la herramienta de texto, el grosor controla el tamaño de la letra.

**Ctrl + rueda del ratón** hace zoom. La rueda sola desplaza la imagen. El zoom es solo para
ver: no afecta a lo que se guarda.

## Deshacer

**Ctrl + Z** deshace, **Ctrl + Y** rehace. Funciona con todo: dibujos, textos, movimientos y
borrados. Ojo: si deshaces y luego dibujas algo nuevo, lo deshecho ya no se puede recuperar.

## Guardar y copiar

- **Ctrl + S** o **Guardar**: escribe un PNG en tu carpeta configurada, con el nombre
  `Captura_2026-07-21_13-36-57.png`. Si ya existiera un archivo con ese nombre, te pregunta
  antes de sobrescribirlo.
- **Ctrl + C** o **Copiar**: al portapapeles, listo para pegar en un correo o un chat. No hace
  falta guardar antes.
- **Esc** o **Cancelar**: descarta la captura. Si tenías marcas sin guardar, primero pregunta.

Si guardar falla (carpeta borrada, sin permisos, disco lleno), **la captura no se pierde**: el
editor sigue abierto con todo tu trabajo y puedes elegir otra ubicación.

## Cambiar el atajo

Configuración → pon el foco en el cuadro **Atajo para capturar** → pulsa la combinación que
quieras. `Esc` cancela la grabación.

El atajo solo se guarda si Windows lo acepta. Si otra aplicación ya lo está usando —o si
Windows se lo reserva, como pasa con `Win + Shift + S`— verás el motivo y **se conserva el
atajo que tenías**.

Un consejo: usa siempre `Ctrl`, `Alt` o `Win`. Un atajo global como `Shift + 5` se queda esa
tecla en todo el sistema y dejarías de poder escribir el símbolo `%` en cualquier programa. La
aplicación te avisa si eliges uno así, pero no te lo impide.

## Dónde se guarda todo

| Qué | Dónde |
|---|---|
| Capturas | `C:\Users\<tu usuario>\Capturas` (configurable) |
| Preferencias | `%LOCALAPPDATA%\CapturaLocal\settings.json` |
| Registro técnico | `%LOCALAPPDATA%\CapturaLocal\logs\captura.log` |

**La carpeta por defecto no es «Imágenes» a propósito.** En equipos de empresa esa carpeta
suele estar redirigida a OneDrive, y guardar ahí subiría cada captura a la nube — justo lo
contrario de lo que hace esta aplicación. Si eliges a mano una carpeta dentro de OneDrive,
Configuración te lo advierte.

## Privacidad

Nada sale de tu equipo. La aplicación no tiene código de red: ni telemetría, ni actualizador,
ni envío de errores. El registro técnico anota eventos y fallos, nunca imágenes, y puedes
desactivarlo desde Configuración.

## Todavía no está (llega en el borrador 2)

Capturar una ventana concreta o la pantalla completa · herramientas de línea y elipse ·
guardar en JPG y «Guardar como» · personalizar el patrón del nombre · iniciar con Windows ·
captura temporizada para menús desplegables · instalador.
