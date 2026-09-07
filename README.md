# SceneFX

Estilos visuales intercambiables para **Cities: Skylines**: cambia el aspecto
completo de tu escena (LUT, tono filmico, sol y niebla) en vivo y guarda tus
propios looks. Desarrollo original de **juanamores98**.

## Qué hace

- **LUTs nativos procedurales**: el mod genera sus propias tablas 3D (32³) con
  curvas de gradación propias — `ordinary`, `nocturne`, `sepia`, `cine`,
  `frost`, `ember`. Nada extraído de terceros: se calculan con el código del
  mod y van integradas en él.
- **Estilos incluidos**: `Vanilla` (neutro), `Optimized` (look cálido
  crepuscular; usa la tabla Relight2Average si está instalada, con la nativa
  `ordinary` como reserva) y variantes — 5 con LUT nativo (`Optimized
  Nocturne/Sepia/Cine/Frost/Ember`) y 11 que referencian las tablas de Relight
  y Relight2 si el usuario las tiene. Se extraen a la carpeta de estilos en el
  primer arranque.
- **Panel nativo del juego (F10)**: interfaz ColossalFramework construida con
  los widgets del propio juego (como los mods clásicos), con grupos Style /
  Color grading LUT / Live grade / World. La ventana IMGUI clásica sigue
  disponible en F11.
- **World (cuarto limpio del bloque Eyecandy)**: hora del día con bloqueo,
  latitud/longitud del sol y cielo procedural, sobre los campos públicos del
  juego (`DayNightProperties`), con restauración al descargar el mapa.
- **Weather**: los seis canales que el juego expone — lluvia, niebla, nubes,
  auroras boreales, arcoíris y suelo mojado — cada uno con su interruptor de
  «lo lleva el juego» o «fijado aquí»; temperatura y dirección del viento; y
  tres interruptores de comportamiento: clima activo, *la lluvia cae como
  nieve* y *calles con aspecto nevado al mojarse*.

  No hay control de «intensidad de nieve» porque el juego no lo tiene: en un
  mapa de invierno la nieve **es** la lluvia, y lo que decide cuál de las dos
  cae es `WeatherProperties.m_rainIsSnow`. Ese es el interruptor que se ofrece,
  y sirve también en mapas templados.
- **Time**: velocidad del juego (`Time.timeScale`, 0,1× a 5×) y velocidad del
  ciclo día/noche, con ritmos separados para el día y para la noche y la opción
  de que el cielo siga girando con la simulación en pausa.

  El ciclo que se acelera es el **visual**. Cambiarlo de verdad significa mover
  `SimulationManager.m_dayTimeOffsetFrames`, que es estado de simulación y viaja
  dentro de la partida guardada; este mod no deja rastro en una partida por el
  mero hecho de estar activo, así que reescribe `DayNightProperties.m_TimeOfDay`,
  que solo alimenta la imagen. El reloj de la ciudad y las políticas nocturnas
  siguen su curso normal.
- **Selector de LUTs**: detecta todos los perfiles de corrección de color
  instalados (los del juego y los que añadan otros mods) y cambia entre ellos
  al instante.
- **Modo compatible**: lee tablas LUT en formato `.crp` que el usuario ya tenga
  en su máquina (subscripciones del Workshop o sueltas en una carpeta `Luts`
  junto al DLL, o en las carpetas `Luts` de otros mods instalados). El mod
  **no incluye, copia ni redistribuye ninguna tabla** — solo las carga en
  memoria si existen; sin ellas, el resto del estilo se aplica igualmente.
- **Ajuste en vivo por estilo**: gamma, brillo, contraste, ganancia solar,
  exposición, calidez, densidad/distancia de niebla y sky tonemapping.
- **Restaurar el look del juego**: deshace todo con un botón (snapshot de los
  valores vanilla tomado antes de tocar nada).
- **Auto-aplicar** el último estilo al cargar un mapa (configurable).
- Estado en `SceneFX.xml`, opciones en `SceneFXOptions.xml`, estilos en
  `ModConfig\SceneFXStyles\`.

## Compatibilidad

- Cities: Skylines en Windows / Linux / macOS. Sin DLCs requeridos.
- No parchea métodos del juego: escribe valores y selección de LUT por las vías
  públicas del juego.
- Dentro de la suite cada propiedad tiene un solo dueño: SceneFX cede el tono y
  la curva solar a LumenFX y la niebla a AtmosphereFX, y se las pide por su API
  pública en vez de escribirlas. Si el dueño no está cargado, SceneFX las
  escribe por su cuenta: ninguna función desaparece porque falte un mod. Con
  mods de iluminación de fuera de la suite sigue ganando el último en escribir.

## Requisitos

- **No requiere Harmony** ni ninguna otra librería externa.
- Compila contra .NET Framework 3.5 (el runtime Mono del juego lo provee).

## Instalación

Copiar `SceneFX.dll` a:

```
%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\SceneFX\
```

## Uso

- `F10`: abrir/cerrar el panel nativo del juego.
- `F11`: ventana IMGUI clásica de respaldo.
- Panel nativo: pestañas *Style* (cargar/guardar estilos, restaurar juego),
  *LUT* (selector nativo + nativos + compat), *Grade*, *World* (hora, posición
  solar, cielo), *Weather* (los canales del clima) y *Time* (velocidad del juego
  y del ciclo día/noche).

## Compilación

```
dotnet build -c Release
```

## Reconocimientos

- **[Play It!](https://github.com/keallu/CSL-PlayIt)** de **keallu** — MIT,
  © 2022 keallu. De ahí vienen las ideas de la pestaña *Time*: separar la
  velocidad del juego de la del reloj, poder darle ritmos distintos al día y a
  la noche, y dejar el cielo girando con la simulación en pausa. Lo adoptado es
  el conjunto de funciones; la implementación de este mod es propia y se apoya
  en los campos públicos del juego. Gracias a keallu por publicarlo con una
  licencia que permite construir encima.

Los controles de clima se escriben contra los campos públicos de
`WeatherManager`, `WeatherProperties` y `NetManager` del propio juego. Cubren
lo mismo que ofrecía Eyecandy X, pero **sin partir de su código**: ese mod no
publica licencia, así que solo se tomó la lista de lo que un usuario espera
poder ajustar, que no es de nadie.

## Licencia

[MIT-0](https://spdx.org/licenses/MIT-0.html) (MIT No Attribution) © 2026 juanamores98.
Uso, copia, modificación, venta, distribución y sublicencia sin atribución ni condiciones.
