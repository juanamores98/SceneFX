# Relevo de Render It! Plus — cobertura de lo que tiene licencia restrictiva

Revisión: 2026-09-09. Auditoría de la suite FX contra el inventario real de
Render It! Plus.

## El problema que esto resuelve

Render It! Plus **no puede publicarse**. Render It! es MIT (© 2022 keallu), pero
el fork absorbió **Daylight Classic, que es GPL-3.0**, y al hacerlo el conjunto
pasó a ser obra derivada de GPL. Es privado y local, y así debe seguir.

La suite FX es MIT-0. Si cubre lo que aportan las piezas restrictivas, existe un
camino publicable: Render It! Plus deja de necesitar ese código.

## Cómo se midió

No por impresión. `ModdingResearch/Datos/INVENTARIO-RANGOS.txt` lista **84
deslizadores** extraídos del código de los cinco mods fusionados, con rango,
paso, archivo y línea. Se repartieron por licencia de origen:

| Origen | Deslizadores | Licencia | ¿Hay que relevarlo? |
|---|---:|---|---|
| Render It! | 50 | MIT, keallu | No |
| Relight | 13 | sin licencia publicada | **Sí** |
| Fog Controller | 12 | sin licencia publicada | **Sí** |
| Eyecandy X | 9 | sin licencia publicada | **Sí** |
| Daylight Classic | 7 interruptores + presets | **GPL-3.0** | **Sí, es la que contamina** |

El objetivo son esos **34 deslizadores más los 7 interruptores** de Daylight
Classic. Los 50 de Render It! son MIT y pueden quedarse donde están.

## Cobertura, medida contra la fuente original

### Relight — 13/13, completo

`lightingValues[0..12]`, todos −1..1, mapeados uno a uno sobre LumenFX:

| Relight | LumenFX | Relight | LumenFX |
|---|---|---|---|
| `[10]` Brightness | Brightness | `[4]` Sky Temperature | SkyTemp |
| `[11]` Gamma | Gamma | `[5]` Sky Tint | SkyTint |
| `[12]` Contrast | Contrast | `[6]` Moon Temperature | MoonTemp |
| `[0]` Temperature | Warmth | `[7]` Moon Tint | MoonTint |
| `[1]` Tint | GlobalTint | `[8]` Moon Light | MoonStrength / MoonPower |
| `[2]` Sun Temperature | SunTemp | `[9]` Twilight Tint | TwilightTint |
| `[3]` Sun Tint | SunTint | *Sky Tonemapping* | SkyTonemapping |

**No se promete igualdad visual**: las fórmulas de temperatura, tinte y curva
filmic son propias. Lo que hay es el mismo conjunto de mandos con el mismo rango.

### Fog Controller — 12/12, todos con rango igual o más ancho

| Fog Controller | AtmosphereFX |
|---|---|
| Color Decay 0,05–1 | Colour decay 0–1 |
| Fog Density 0–0,00223 | Density 0–0,005 |
| Noise Contribution 0,1–1,4 | Noise 0–2 |
| Fog Visibility 0–8000 paso 1 | Fog start 0–10000 paso 1 |
| Wind Speed 0–0,01 | Wind drift 0–0,05 |
| Inscattering Size −10 … −1 | Scatter exponent 0,5–100000 (log) |
| Color de niebla R/G/B | Volume R/G/B |
| Color de inscattering R/G/B | Scatter R/G/B |

**Sobre «Inscattering Size».** El deslizador original va de −10 a −1, pero lo que
escribe es `m_inscatteringExponent = -(valor^5)`, o sea **+1 … +100000**. El
nuestro escribe el campo directo con rango 0,5–100000 en escala logarítmica: lo
cubre entero y llega más abajo. Mirar sólo el rango del deslizador habría hecho
creer que faltaba la mitad negativa, que no existe.

### Eyecandy X — 9/9 (dos se arreglaron en esta revisión)

| Eyecandy X | Suite FX | Nota |
|---|---|---|
| Time of Day 0–24 paso 1/60 | SceneFX Hour 0–24 paso 0,01 | más fino |
| Day/night cycle speed | SceneFX Cycle speed 0–128 | más ancho |
| Sun height (latitud) **−120…120** | SceneFX Latitude **−120…120** | **corregido: estaba en ±90** |
| Sun rotation (longitud) −180…180 | SceneFX Longitude −180…180 | igual |
| Global light intensity 0–10 | LumenFX Sun power 0–20 | más ancho |
| Ambient 0–2 | LumenFX Ambient gain 0–2 | igual |
| Game Speed 0–2 | SceneFX Game speed 0,01–5 | más ancho |
| Precipitation 0–2,5 | SceneFX Rain/snow 0–2,5 | igual |
| Fog **−0,485…1** | SceneFX Weather fog **−0,485…1** | **corregido: estaba en 0…1** |

Extras que Eyecandy X tenía y la suite conserva: casilla de clima dinámico,
desenfoque de lluvia, y humedad del suelo —que en Eyecandy X estaba comentada y
por tanto inactiva—.

**No hay intensidad de nieve porque el juego no la tiene.** Se volcó por
reflexión todo `Assembly-CSharp.dll`: no existe ningún `m_targetSnow`. En un mapa
de invierno la nieve *es* la lluvia, y quien decide cuál cae es
`WeatherProperties.m_rainIsSnow`. Ese es el interruptor que se ofrece.

### Daylight Classic — 7/7 interruptores

`swapLuts`, `sunColor`, `sunStrength`, `sunCoords`, `classicFogMode`,
`classicFogTint`, `classicFogWithCycle`, más los presets (tinte clásico
`104,166,211`, longitudes de onda `680/680/680`, intensidad `3,318695`,
exposición `1,0`, degradado solar de 8 claves).

**Las tablas de color se sintetizan, no se copian.** `ClassicLutSynth` genera las
tablas con curvas propias; no hay ni un byte del mod original en el repositorio.
Es lo que permite que esto sea MIT-0 y lo que rompe la cadena GPL.

## Las cuatro formas de gobernar el cielo

El expediente de Render It! Plus señala que `m_SkyTint` se controla de cuatro
maneras que no se pueden sumar, y que hay que conservar las cuatro:

| Forma | Origen | Dónde vive ahora |
|---|---|---|
| Sesgo relativo −1…1 | Relight | LumenFX `SkyTemp` / `SkyTint` |
| Color absoluto de un tema | Theme Mixer | Theme Mixer+ (sigue siendo suyo) |
| Longitudes de onda R/G/B 0–1000 | Render It! | LumenFX — **añadido en esta revisión** |
| Degradado sobre la hora | Daylight Classic | ClassicLightFX |

La tercera faltaba. Ahora son tres campos con el convenio del resto del panel:
**0 = el valor del mapa**, y se puede tocar sólo el rojo dejando los otros dos
como estaban. Paso 1 en vez de los 10 del original.

Con el tinte clásico pedido, LumenFX no toca el campo: ese modo es un degradado
sobre la hora que no admite otra forma, y quien lo pide manda. Para que eso
funcione, ClassicLightFX ahora **anuncia `fogTint`** entre sus peticiones; antes
escribía `m_SkyTint` y `m_WaveLengths` sin que ningún otro FX pudiera saberlo.

## Veredicto

**Las cuatro fuentes restrictivas están cubiertas: 34/34 deslizadores y 7/7
interruptores.** Con las correcciones de esta revisión no queda ningún rango por
debajo del original, y en la mayoría es más ancho o más fino.

Lo que **no** se declara:

- **Igualdad visual.** Las fórmulas son propias. Que exista el mando con el mismo
  rango no significa que el mismo número dé la misma imagen. Eso se compara en
  partida, no aquí.
- **Los 50 deslizadores de Render It!** siguen siendo suyos, y no hacen falta:
  son MIT. La suite no los duplica salvo donde ya coincidían.
- **El post-procesado** (SSAO, TAA, bloom, color grading de la pila de Unity) es
  competencia de Render It!, no de la suite.

## Qué falta comprobar en partida

Estas 92 comprobaciones de regresión pasan contra el código real de los cuatro
mods, con dobles del juego; no sustituyen a Unity:

1. Latitud por encima de 90° y por debajo de −90°: el sol debe pasar sobre el
   polo sin artefactos.
2. Niebla de clima en negativo: debe limpiar la calima base, no dejar de dibujar.
3. Longitudes de onda: tocar sólo el rojo y comprobar que verde y azul se quedan
   en el valor del mapa.
4. Encender el tinte clásico con longitudes de onda puestas: los tres
   deslizadores deben quedar deshabilitados y el cielo pasar al degradado clásico.
