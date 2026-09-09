# Paridad de SceneFX

Revisión: 2026-09-08. Requisitos del encargo y de la matriz de comportamiento de `ModdingResearch/EncargosFX/Auditoria-20260908/INFORME.md`. No implica adopción de implementaciones GPL.

| Capacidad | Fuente de requisito | Entrada / unidad / rango | Comportamiento | API comprobada | Destino | Prueba | Estado / diferencia |
|---|---|---|---|---|---|---|---|
| LUTs instaladas | Relight / Eyecandy X / encargo | Nombre de asset; sin textura propia | Coincidencia exacta o sufijo único; recurso ausente visible y sin sustituto | ColorCorrectionManager.items/currentSelection/lastSelection | StyleEngine.ApplyLut | S05, S09 | Sin NativeLut/LutCompat/BakeToLut ni familia Optimized de LUTs |
| Cámara | Eyecandy X / DEFAULT | Game / Off / On | Aplicar y restituir cada componente realmente escrito | ColorCorrectionLut.enabled, ToneMapping.enabled, Bloom.enabled, ForceRainMotionBlur | CameraEffects | S10, S11 | Nuevos controles; imagen/foco en juego pendientes |
| Look y presets | Relight / Eyecandy X / H04–H07 | Gamma 1.5–3.5; brillo -1–4; contraste/calidez -1–1 | Tono propio, fallback y delegación a Lumen activo; estilo completo | ToneMapping / DayNightProperties / XML | StyleEngine / StyleStore | X02, X03, S17 | Formato propio, sin importación ECX; conversión visual aproximada |
| Clima | Eyecandy X / Play It | Lluvia 0–2.5; otros 0–1; -1 libera; temperatura ±100 °C | Persistir bloqueos y valores; Game restituye banderas individualmente | WeatherManager, NetManager | WorldController / SceneRuntime | S01, S03, S12, S13 | Niebla negativa del legado no cubierta |
| Hora y coordenadas | Eyecandy X / Play It | Hora 0–24; latitud ±90; longitud ±180 | Horas reales de API; bloqueo persistente; captura explícita del momento | DayNightProperties.m_TimeOfDay/m_Latitude/m_Longitude | WorldController | S02, S06, S07, S16 | Reloj visual, no offset del reloj de simulación |
| Velocidad y pausa | Play It / Eyecandy X | Juego 0.01–5; ciclo 0–128, decimal libre | Ritmos visuales por fase; arranque ya pausado con duración pública de frame | Time.timeScale/fixedDeltaTime; SimulationManager.DAYTIME_FRAMES | TimeController | S01, S14, S15 | Modelo diferente; falta validar transición en Unity |
| Preferencias de Play It | Auditoría de capacidades | Reloj, formato, teclas, visibilidad | No incluidas en este cambio | — | — | Inventario | Paridad completa todavía no alcanzada |
| Persistencia / suites / modos | Encargo / H03–H09 | XML global + .scene.xml / .suite.xml | Dirty, reintento, flush; mundo explícito en preset estético | XML / API de compañeros | SceneRuntime / SuiteManager | S04, S08, S18, S19, IO03, X04, X05 | Una suite fallida puede dejar secciones aplicadas; no se anuncia éxito |

Las pruebas citadas están en `SceneFX/tests/Regression/Program.cs`. Firmas compiladas contra DLL reales; aserciones ejecutadas con dobles, no Unity. UI, imagen, tiempo de respuesta y rendimiento pendientes de observación.

**No se declara paridad total.** Sin importador de presets ECX. El ciclo sigue siendo visual, separado del reloj de simulación; no reproduce todos los comportamientos de Play It. No incluye su reloj flotante configurable, reloj del sistema ni todos sus atajos/preferencias. La niebla meteorológica negativa del legado no está representada: -1 significa liberar. La suite puede aplicarse parcialmente si una sección falla; devuelve false y debe revisarse, no es una transacción entre cuatro mods.

Para la cobertura de lo que en Render It! Plus tiene licencia restrictiva -Relight, Fog Controller, Eyecandy X y Daylight Classic, que es GPL-3.0- el documento es `SceneFX/docs/RELEVO-RENDERIT-PLUS.md`.
