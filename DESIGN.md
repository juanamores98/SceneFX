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
3. Al descargar el mapa se restaura el look del juego.

## Estilos incluidos (built-ins)

13 documentos en `BuiltIns\`, extraídos a la carpeta de estilos en el primer
arranque:

- `Vanilla` — todo neutro, sin LUT, niebla 0 = valores del juego.
- `Optimized` — look cálido crepuscular de contraste alto sobre la tabla
  `1539181199.Relight2Average`, exposición 1.1, niebla 0.00006 / 2852.
- 11 variantes `Optimized <LUT>` — idénticas al Optimized pero apuntando a
  cada tabla de Relight (`1209581656.*`, 7 tablas) y Relight2
  (`1539181199.*`, 4 tablas restantes).

Las tablas `.crp` **no se empaquetan**: los estilos las referencian por nombre
(el selector acepta coincidencia exacta, sufijo `id.nombre` o contiene). Sin
los packs suscritos, el estilo aplica igualmente el resto de parámetros.

## Selección de LUT

Se usa el gestor de corrección de color del juego: se enumeran los perfiles
instalados (`items`) y la selección se realiza por índice (`currentSelection`),
por lo que se ven tanto los LUTs del juego como los que registren otros mods.

## Arquitectura

- `Core/StyleData` — modelo de estilo + almacenamiento XML (estilos y estado).
- `Core/StyleEngine` — aplicación/reversión (LUT, tono, sol, warmth, niebla).
- `Core/SceneRuntime` — estado global y opciones (`applyOnLoad`).
- `UI/StylePanel` + `PanelEngine` — panel F10 de tres pestañas.
- `SceneFXMod` — entry point IUserMod + ciclo de carga del mapa.
