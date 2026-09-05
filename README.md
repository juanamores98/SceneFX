# SceneFX

Estilos visuales intercambiables para **Cities: Skylines**: cambia el aspecto
completo de tu escena (LUT, tono filmico, sol y niebla) en vivo y guarda tus
propios looks. Desarrollo original de **juanamores98**.

## QuÃ© hace

- **LUTs nativos procedurales**: el mod genera sus propias tablas 3D (32Â³) con
  curvas de gradaciÃ³n propias â€” `ordinary`, `nocturne`, `sepia`, `cine`,
  `frost`, `ember`. Nada extraÃ­do de terceros: se calculan con el cÃ³digo del
  mod y van integradas en Ã©l.
- **Estilos incluidos**: `Vanilla` (neutro), `Optimized` (look cÃ¡lido
  crepuscular; usa la tabla Relight2Average si estÃ¡ instalada, con la nativa
  `ordinary` como reserva) y variantes â€” 5 con LUT nativo (`Optimized
  Nocturne/Sepia/Cine/Frost/Ember`) y 11 que referencian las tablas de Relight
  y Relight2 si el usuario las tiene. Se extraen a la carpeta de estilos en el
  primer arranque.
- **Panel nativo del juego (F10)**: interfaz ColossalFramework construida con
  los widgets del propio juego (como los mods clÃ¡sicos), con grupos Style /
  Color grading LUT / Live grade / World. La ventana IMGUI clÃ¡sica sigue
  disponible en F11.
- **World (cuarto limpio del bloque Eyecandy)**: hora del dÃ­a con bloqueo,
  latitud/longitud del sol, e intensidad de lluvia/niebla/nubes, sobre los
  campos pÃºblicos del juego (`DayNightProperties`, `WeatherManager`), con
  restauraciÃ³n al descargar el mapa.
- **Selector de LUTs**: detecta todos los perfiles de correcciÃ³n de color
  instalados (los del juego y los que aÃ±adan otros mods) y cambia entre ellos
  al instante.
- **Modo compatible**: lee tablas LUT en formato `.crp` que el usuario ya tenga
  en su mÃ¡quina (subscripciones del Workshop o sueltas en una carpeta `Luts`
  junto al DLL, o en las carpetas `Luts` de otros mods instalados). El mod
  **no incluye, copia ni redistribuye ninguna tabla** â€” solo las carga en
  memoria si existen; sin ellas, el resto del estilo se aplica igualmente.
- **Ajuste en vivo por estilo**: gamma, brillo, contraste, ganancia solar,
  exposiciÃ³n, calidez, densidad/distancia de niebla y sky tonemapping.
- **Restaurar el look del juego**: deshace todo con un botÃ³n (snapshot de los
  valores vanilla tomado antes de tocar nada).
- **Auto-aplicar** el Ãºltimo estilo al cargar un mapa (configurable).
- Estado en `SceneFX.xml`, opciones en `SceneFXOptions.xml`, estilos en
  `ModConfig\SceneFXStyles\`.

## Compatibilidad

- Cities: Skylines en Windows / Linux / macOS. Sin DLCs requeridos.
- No parchea mÃ©todos del juego: escribe valores y selecciÃ³n de LUT por las vÃ­as
  pÃºblicas del juego.
- Puede solaparse con otros mods de iluminaciÃ³n (LumenFX, AtmosphereFX, etc.):
  gana el Ãºltimo en escribir.

## Requisitos

- **No requiere Harmony** ni ninguna otra librerÃ­a externa.
- Compila contra .NET Framework 3.5 (el runtime Mono del juego lo provee).

## InstalaciÃ³n

Copiar `SceneFX.dll` a:

```
%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\SceneFX\
```

## Uso

- `F10`: abrir/cerrar el panel nativo del juego.
- `F11`: ventana IMGUI clÃ¡sica de respaldo.
- Panel nativo: grupos *Style* (cargar/guardar estilos, restaurar juego),
  *Color grading LUT* (selector nativo + nativos + compat), *Live grade* y
  *World* (hora, posiciÃ³n solar, clima).

## CompilaciÃ³n

```
dotnet build -c Release
```

## Licencia

[MIT-0](https://spdx.org/licenses/MIT-0.html) (MIT No Attribution) Â© 2026 juanamores98.
Uso, copia, modificaciÃ³n, venta, distribuciÃ³n y sublicencia sin atribuciÃ³n ni condiciones.
