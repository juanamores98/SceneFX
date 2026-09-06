# SceneFX ReShade Pack

Shaders de ReShade **originales** de la suite v2 (juanamores98, MIT-0).
Complementan los mods internos: implementan a pantalla completa la misma
familia de curvas que usan `NativeLut` (SceneFX) y `TonemapProfile` (LumenFX),
escrita en ReShade FX desde cero — sin derivar de FilmicPass, AmbientLight,
qUINT ni ningún shader de terceros.

> Este pack es la alternativa publicable a los shaders de la carpeta
> NoWorkshopMods: FilmicPass (sin concesión de licencia en su cabecera),
> AmbientLight (licencia restrictiva de Ganossa), qUINT y Depth3D
> (propietarios) se usan localmente pero **no se pueden redistribuir**;
> este sí.

## Contenido

| Archivo | Qué hace |
|---|---|
| `Shaders/SceneFX/SceneFXFilmic.fx` | Gradado full-screen: calidez (ganancia opuesta R/B), saturación alrededor de luma, contraste en S con hombro suave por encima de 0.8 y recorte de brillo. Parámetros expuestos al menú de ReShade. |
| `Shaders/SceneFX/SceneFXGlow.fx` | Glow ambiente en una pasada: 12 muestras con peso por brillo añadidas sobre la imagen. Alternativa propia a AmbientLight. |
| `Shaders/SceneFX/SceneFXDepthAO.fx` | Oclusión ambiental por profundidad (halo AO sobre `GetLinearizedDepth`, 8 muestras). Alternativa propia a MXAO/qUINT. |
| `Presets/03-SceneFX-Optimizado.ini` | Preset propio con los valores recomendados de la suite. |

## Instalación (independiente: solo requiere ReShade)

1. Copiar `Shaders\SceneFX\` dentro de
   `...\Cities_Skylines\reshade-shaders\Shaders\`.
2. Copiar `Presets\03-SceneFX-Optimizado.ini` dentro de
   `...\Cities_Skylines\Presets\` (o elegirlo desde Home → Presets en ReShade).
3. En el menú de ReShade (Home), activar la técnica **SceneFXFilmic**.

## Notas de convivencia

- Con la técnica activa, **baja los sliders equivalentes** de LumenFX
  (contraste/gamma) para no duplicar el mismo ajuste dos veces.
- Convive bien con `FilmicPass` solo si se reparten tareas: SceneFXFilmic para
  calidez/saturación, FilmicPass para la curva base — o desactiva uno.
- Licencia: MIT-0 (`LICENSE`). El shader es obra original; compatible con la
  colección Standard de ReShade (su `ReShade.fxh` es CC0-1.0).
