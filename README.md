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
  latitud/longitud del sol, e intensidad de lluvia/niebla/nubes, sobre los
  campos públicos del juego (`DayNightProperties`, `WeatherManager`), con
  restauración al descargar el mapa.
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
- Puede solaparse con otros mods de iluminación (LumenFX, AtmosphereFX, etc.):
  gana el último en escribir.

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
- Panel nativo: grupos *Style* (cargar/guardar estilos, restaurar juego),
  *Color grading LUT* (selector nativo + nativos + compat), *Live grade* y
  *World* (hora, posición solar, clima).

## Compilación

```
dotnet build -c Release
```

## Licencia

[MIT-0](https://spdx.org/licenses/MIT-0.html) (MIT No Attribution) © 2026 juanamores98.
Uso, copia, modificación, venta, distribución y sublicencia sin atribución ni condiciones.
