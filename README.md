# SceneFX

Estilos visuales intercambiables para **Cities: Skylines**: cambia el aspecto
completo de tu escena (LUT, tono filmico, sol y niebla) en vivo y guarda tus
propios looks. Desarrollo original de **juanamores98**.

## Qué hace

- **Biblioteca de estilos**: guarda cualquier combinación como un estilo con
  nombre (`*.scene.xml`) y aplícalo con un clic desde el panel (F10).
- **Estilos incluidos**: `Vanilla` (neutro), `Optimized` (look cálido
  crepuscular sobre la tabla Relight2Average) y 11 variantes `Optimized <LUT>`
  — una por cada tabla de Relight (7) y Relight2 (5, incluida Average via
  Optimized). Los estilos **referencian** las tablas por nombre; si tienes
  suscritos los packs de LUTs se aplican, si no, el resto del estilo funciona
  igual. Se extraen a la carpeta de estilos en el primer arranque.
- **Selector de LUTs**: detecta todos los perfiles de corrección de color
  instalados (los del juego y los que añadan otros mods) y cambia entre ellos
  al instante.
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

- `F10`: abrir/cerrar el panel.
- Pestaña *Styles*: aplicar/borrar/guardar estilos y restaurar el look del juego.
- Pestaña *Adjust*: editar el estilo actual en vivo.
- Pestaña *LUT*: escanear y elegir el perfil de color.

## Compilación

```
dotnet build -c Release
```

## Licencia

[MIT-0](https://spdx.org/licenses/MIT-0.html) (MIT No Attribution) © 2026 juanamores98.
Uso, copia, modificación, venta, distribución y sublicencia sin atribución ni condiciones.
