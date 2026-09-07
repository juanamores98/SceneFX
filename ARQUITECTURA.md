# SceneFX — arquitectura y cambios

Documento ejecutivo. Estado a día de hoy, no historia. `DESIGN.md` es la
especificación funcional original del formato de estilos; este documento
describe cómo está construido el mod hoy y qué cambió en el último ciclo.

## Qué manda este mod

Dentro de la suite FX cada propiedad del juego tiene **un solo dueño**. El de
SceneFX es el mundo:

| Materia | Dueño | Campos del juego |
|---|---|---|
| Estilos completos, LUTs, gradación | **SceneFX** | `ColorCorrectionManager`, `ToneMapping` |
| Hora, latitud, longitud, cielo | **SceneFX** | `DayNightProperties.m_TimeOfDay`, `m_Latitude`, `m_Longitude` |
| Clima entero | **SceneFX** | `WeatherManager`, `WeatherProperties.m_rainIsSnow`, `NetManager.m_treatWetAsSnow` |
| Ritmo del juego y del cielo | **SceneFX** | `Time.timeScale`, `DayNightProperties.m_TimeOfDay` |
| Tono, calidez y curva solar | LumenFX | se le **piden** por `ApplySuiteSection` |
| Niebla | AtmosphereFX | se le **pide** por `ApplySuiteSection` |

Las dos últimas filas son la regla de dueño único. `StyleEngine.ApplyTone` y
`ApplyFog` comprueban con `SuiteManager.ModPresent` si el dueño está cargado: si
lo está, le mandan un XML por su API pública y no escriben nada; si no lo está,
escriben ellas. **Ninguna función desaparece porque falte un mod**, y aplicar el
mismo preset en un orden u otro da la misma imagen.

## Piezas

```
Source/
  SceneFXMod.cs          IUserMod + ciclo de vida + API de suite (36 etiquetas)
  PanelEngine.cs         MonoBehaviour anfitrión: teclas y el Tick de cada fotograma
  Core/
    StyleEngine.cs       aplica un estilo; delega tono y niebla a sus dueños
    StyleData.cs         el esquema *.scene.xml y su persistencia
    WorldController.cs   clima, hora y posición solar
    TimeController.cs    velocidad del juego y del ciclo día/noche
    SuiteManager.cs      descubre los otros mods y les pide lo que es suyo
    SkyMood.cs           paletas de cielo procedurales
    NativeLut.cs         tablas 3D 32³ generadas por el propio mod
    BakeToLut.cs         hornea el look actual a una tabla + PNG
    ThemeOwnership.cs    detecta Theme Mixer y le cede sus 6 campos
    QuickPresets.cs      Vanilla y Optimized de un clic
  UI/
    NativePanel.cs       panel nativo del juego, 4 pestañas (F10)
    UICard.cs            tarjeta plegable reutilizable
    StylePanel.cs        ventana IMGUI de respaldo (F11)
  Locale/                en, es, de, fr, ru
```

## Interfaz

Cuatro pestañas, cada una un `UIScrollablePanel` recortado a la ventana, con
tarjetas plegables dentro. Nada puede salirse por abajo: lo que no cabe se
desplaza.

1. **Style & Color** — perfiles, LUTs, hornear a LUT, gradación en vivo, perfil de suite.
2. **Sun & Light** — latitud, longitud, cielo, ganancia solar, ir a LumenFX.
3. **Weather & Sky** — los seis canales del clima, temperatura, viento, nieve, ir a AtmosphereFX.
4. **Time & Rhythm** — velocidad del juego, pausa, hora, ciclo día/noche.

Sobre las pestañas hay una cabecera fija con Vanilla, `Suite: Optimized` y un
reloj arrastrable, visibles desde cualquier pestaña.

**Los canales del clima.** Un valor negativo quiere decir «esto lo lleva el
juego»; de 0 en adelante lo fija el mod fotograma a fotograma. La casilla de
cada canal es esa distinción, y mover el deslizador la marca sola: quien mueve
un control espera verlo aplicado, no tener que armarlo antes.

## API de suite

`ApplySuiteSection(string|XmlElement)` y `ExportSuiteSection()`, ambas públicas
y estáticas. 36 etiquetas, todas las que se aplican se exportan también —el
laboratorio comprueba que un ajuste es ejecutable mirando la exportación, así
que una etiqueta que no se publique se saltaría en silencio.

```
lut nativeLut gamma brightness contrast sunIntensity exposure warmth
fogDensity fogStart skyTonemap skyMood includeWorld vanillaMode
timeOfDay latitude longitude
rain fog cloud northernLights rainbow groundWetness
temperatureLock temperature windLock windDirection
weatherEnabled rainIsSnow snowyRoads
gameSpeed cycleSpeedEnabled cycleSpeed nightCycleSpeed separateDayNight cycleWhilePaused
```

## Dónde guarda las cosas

Todo bajo `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\`:

- `SceneFX.xml` — el último look aplicado.
- `SceneFXOptions.xml` — las opciones del mod.
- `ModConfig\SceneFXStyles\*.scene.xml` — los estilos guardados.

Rutas completas, no relativas: un nombre suelto se resuelve contra el directorio
de trabajo del proceso, que en Cities: Skylines es la carpeta de instalación del
juego. Ahí acababan estos archivos, dentro de Archivos de Programa. Si queda uno
en el sitio antiguo y todavía no hay ninguno en el nuevo, se lee el antiguo.

## Qué cambió en este ciclo

**Clima y ritmo.** Los seis canales que el juego expone —lluvia, niebla, nubes,
auroras boreales, arcoíris y suelo mojado—, más temperatura, dirección del
viento, clima activo, *la lluvia cae como nieve* y *calles con aspecto nevado al
mojarse*. Y una pestaña de ritmo con la velocidad del juego (0,1× a 5×) y la del
ciclo día/noche, con ritmos separados para el día y la noche.

**No hay control de intensidad de nieve porque el juego no lo tiene.** Se volcó
por reflexión todo `Assembly-CSharp.dll`: no existe ningún `m_targetSnow`. En un
mapa de invierno la nieve *es* la lluvia, y lo que decide cuál cae es
`WeatherProperties.m_rainIsSnow`. Ese es el interruptor que se ofrece.

**El ciclo acelerado es visual.** Acelerarlo de verdad significa mover
`SimulationManager.m_dayTimeOffsetFrames`, que es estado de simulación y viaja
dentro de la partida guardada. Este mod no deja rastro en una partida por el
mero hecho de estar activo, así que reescribe `DayNightProperties.m_TimeOfDay`,
que solo alimenta la imagen. El reloj de la ciudad y las políticas nocturnas
siguen su curso. El ritmo no se calcula: se observa cuánto movió el juego el
reloj en el último fotograma y se reescala, así velocidad 1 es exactamente el
juego sin tocar, y el ritmo aprendido es lo que permite seguir girando en pausa.

**Interfaz.** De seis pestañas planas a cuatro con tarjetas plegables y scroll
propio, más la cabecera fija. Exportar dejó de pisar la hora guardada: trabaja
sobre una copia. Un idioma sin traducir cae al inglés en vez de mostrar el
identificador crudo.

## Correcciones de la revisión

- **Sin emoji en la interfaz.** Se comprobó sobre 12.960 ficheros `.cs` de los
  mods que ya funcionan: `▼` y `►` aparecen en 79 y 54 —esos sí se dibujan— y
  ninguno de los 27 emoji propuestos aparece en ninguno. La fuente Arial de
  Unity 5.6 no lleva pictogramas, y los del plano astral llegan además como
  pares sustitutos que IMGUI de esa versión no compone.
- **Los botones de ir al otro panel no hacían nada.** Buscaban un
  `ToggleWindow` que no existía, en `LumenFX.Runtime.TunerEngine`, que tampoco
  existe —el tipo real es `LumenFX.Core.TunerEngine`—. Se añadió el método a los
  dos motores y se corrigió el nombre. El atajo de AtmosphereFX es Ctrl+Alt+A,
  no «Alt+F» como decía el botón.
- **El botón de pausa no pausaba.** `ApplyGameSpeed(0)` se recorta a 0,1× y el
  juego se arrastraba en vez de detenerse. Ahora conmuta la pausa del propio
  juego, la misma de la barra espaciadora.
- **La ganancia solar estaba duplicada** en dos pestañas, escribiendo el mismo
  campo sin enterarse la una de la otra. Se queda en la pestaña del sol.
- **Los textos de pantalla pasan por el traductor**, no por literales sueltos.

## Atajos

- `F10` — panel nativo.
- `F11` — ventana IMGUI de respaldo.

## Licencia

MIT-0 © 2026 juanamores98. Sin atribución ni condiciones.

Reconocimiento: las ideas de la pestaña de ritmo vienen de
[Play It!](https://github.com/keallu/CSL-PlayIt) de keallu (MIT, © 2022 keallu).
Lo adoptado es el conjunto de funciones; la implementación es propia.
