# Diseño de SceneFX

Especificación funcional de la primera versión.

## Objetivo

Gestionar "estilos" visuales completos e intercambiables en vivo: cada estilo
describe un look (perfil de color, curva de tono, sol y niebla) y puede
guardarse, aplicarse y compartirse como archivo.

## Modelo de estilo (schema v2, `*.scene.xml`)

| Campo | Rango | Semántica |
|---|---|---|
| `lut` | nombre del perfil | selección directa en el gestor de color del juego; vacío = no tocar |
| `nativeLut` | nombre de tabla nativa | fallback procedural generado por el propio mod |
| `gamma` | 1.2–3.0 | curva de tono directa |
| `brightness` | −1..1 | boost factor `1 + 0.5·b` |
| `contrast` | −1..1 | filmic propio: `A=0.5+0.18c, B=0.26−0.14c, C=0.10−0.008c, D=0.72+0.18c, E=0.01, F=0.24−0.11c, W=11+2.2c`, luminancia `0.11+0.02c` |
| `sunIntensity` | 0–3 | multiplicador sobre la intensidad vanilla capturada |
| `exposure` | 0.5–1.5 | multiplicador sobre la exposición vanilla capturada |
| `warmth` | −1..1 | regrade propio de la gradiente solar en 3 claves (R/B `±15 %`) |
| `fogDensity` | 0–0.005 | 0 = mantener el valor del juego |
| `fogStart` | 0–10000 | 0 = mantener el valor del juego |
| `skyTonemap` | bool | `DayNightProperties.m_Tonemapping` |

## Ciclo de vida

1. Al habilitar el mod se crea el host del panel; al cargar un mapa se aplica
   el último estilo si `applyOnLoad` está activo.
2. Antes de la primera escritura se toma un **snapshot vanilla** (intensidad
   solar, exposición y parámetros de tono); "Restore game look" revierte a él.
3. Al descargar el mapa se restaura el look del juego y el mundo.

## Estilos incluidos (built-ins)

18 documentos en `BuiltIns\`, extraídos a la carpeta de estilos en el primer
arranque:

- `Vanilla` — todo neutro, sin LUT, niebla 0 = valores del juego.
- `Optimized` — look cálido crepuscular de contraste alto; referencia la tabla
  `1539181199.Relight2Average` con la nativa `ordinary` como reserva,
  exposición 1.1, niebla 0.00006 / 2852.
- 5 variantes con LUT nativo: `Optimized Nocturne/Sepia/Cine/Frost/Ember`
  (totalmente autocontenidas).
- 11 variantes que referencian las tablas de Relight (`1209581656.*`, 7) y
  Relight2 (`1539181199.*`, 4 restantes) — funcionan si el usuario tiene los
  packs.

Las tablas `.crp` **no se empaquetan**. Sin ellas, cada estilo aplica
igualmente el resto de parámetros.

## LUTs nativos procedurales (`Core/NativeLut`)

El mod genera sus propias tablas 3D de 32³ evaluando una curva de gradación
propia sobre el cubo identidad: exposición → calidez (ganancia opuesta R/B) →
saturación alrededor de luminancia → contraste en S con hombro suave → lift de
negros. Paleta propia: `ordinary`, `nocturne`, `sepia`, `cine`, `frost`,
`ember`. Las texturas se calculan una vez y se cachean; se aplican con
`ColorCorrectionManager.SetLUT`.

Cadena de resolución de LUT de un estilo:

1. Selector nativo del juego (`items` / `currentSelection`) — incluye las
   tablas que el propio juego lista en sus opciones gráficas.
2. Modo compatible (tablas del usuario leídas en runtime).
3. Tabla nativa procedural del mod.
4. Fallback `nativeLut` declarado en el estilo.

## World (`Core/WorldController`) — cuarto limpio del bloque Eyecandy

Construido sobre campos públicos del juego, descubiertos por reflexión:

| Control | API del juego |
|---|---|
| Hora del día (0–24) + bloqueo | `DayNightProperties.m_TimeOfDay` (0..1), aplicado por tick al bloquear |
| Latitud / longitud del sol | `DayNightProperties.m_Latitude` / `m_Longitude` |
| Lluvia / niebla / nubes | `WeatherManager.m_targetRain/m_targetFog/m_targetCloud` (+ convergencia suave del valor actual) |

Se toma un snapshot de lat/lon antes del primer cambio y `Restore()` revierte
todo al descargar el mapa. El multiplicador de velocidad del ciclo día/noche
queda fuera de v2: el juego no expone un control aislado y alterar la
velocidad de simulación no es aceptable.

## Modo compatible (lectura en runtime)

`Core/LutCompat` escanea, una sola vez bajo demanda, las carpetas de tablas que
el usuario ya tenga en su máquina:

1. `Luts\` junto al DLL del mod (archivos que el usuario ponga ahí).
2. `Addons\Mods\*\Luts\` de otros mods instalados localmente.

Cada `.crp` se abre con el `Package` del juego y su textura 3D se instancia en
memoria; `ColorCorrectionManager.SetLUT` la aplica por la vía pública del
juego. Claves registradas: nombre de archivo y nombre interno del asset, con
coincidencia por sufijo (`1539181199.Relight2Average` ↔ `Relight2Average`).

**Límites de licencia**: el mod no empaqueta, copia ni modifica ninguna tabla
de terceros; solo las lee si el usuario ya las posee (interoperabilidad). El
código del mod sigue siendo MIT-0.

## Arquitectura

- `Core/StyleData` — modelo de estilo + almacenamiento XML (estilos y estado).
- `Core/StyleEngine` — aplicación/reversión (LUT, tono, sol, warmth, niebla).
- `Core/NativeLut` — tablas 3D procedurales propias.
- `Core/LutCompat` — modo compatible (lectura runtime de tablas del usuario).
- `Core/WorldController` — hora, posición solar y clima (cuarto limpio).
- `Core/SceneRuntime` — estado global y opciones (`applyOnLoad`).
- `UI/NativePanel` — panel nativo del juego (F10, widgets `UIHelper`).
- `UI/StylePanel` + `PanelEngine` — ventana IMGUI clásica de respaldo (F11).
- `SceneFXMod` — entry point IUserMod + ciclo de carga del mapa.
