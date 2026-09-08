# Entrega de correcciones de los cuatro FX — 2026-09-08

Se corrigieron AtmosphereFX, LumenFX, ClassicLightFX y SceneFX. NeuralFX y SkyFX quedaron fuera. No se integró ni modificó RenderIt Plus/Arrebol. El usuario autorizó commit y push de esta entrega a los cuatro repositorios. La publicación del código no instala los mods ni sustituye las pruebas en juego pendientes.

**Resultado offline: cuatro builds net35 correctos y 60 comprobaciones de lógica aprobadas. No equivale a aprobación final en juego ni a paridad completa con todos los legados.**

## Qué cambia

| Mod | Cambios principales |
|---|---|
| AtmosphereFX | VANILLA restaura la captura; guardado y exportación precisos; exponente efectivo hasta 100000; profundidad estática y volumétrica separada; DEFAULT corregido |
| LumenFX | Persistencia completa de potencias/cielo; reconstrucción en segunda ciudad; restauración de exposición y sky tonemapping; presets completos; importación .light explícita |
| ClassicLightFX | VANILLA persistente; DEFAULT desactiva lo clásico; ApplyOnLoad respetado; tabla de niebla corregida; prioridad de compañeros; curva solar propia desde el mapa |
| SceneFX | LUTs normales del juego sin generador ni variantes Optimized; cámara LUT/tono/bloom/lluvia; ajustes de mundo persistentes; hora en unidades correctas; estilos y suites; ciclo visual desde pausa |

Los cuatro usan el mismo diseño nativo estrecho, con controles verticales, entrada decimal, refresco protegido y fábrica de panel con padre para incrustación. Tamaño preferido 360×680 y mínimo 280×260. No son cuatro interfaces ya integradas en otro producto.

Atajos comprobados en código: Atmosphere Ctrl+Alt+A; Lumen Ctrl+Alt+L; Classic F9; Scene F10/F11 (un solo panel).

## DEFAULT usado, sin recalibrarlo

Fuente exacta: `C:\Program Files (x86)\Steam\steamapps\common\Cities_Skylines\RenderIt-Plus.xml`, perfil `Default`, `Active=true`.

SHA256 del archivo completo leído: `71062f3ac84872ca305db47237e5fd7168bf5b5b629cfff9b24eae45ad6bbfba`.
[Perfil extraído](Default.reference.xml) y [registro de origen](Default.reference.txt).
La receta coincide en los valores examinados con la referencia histórica de 2026-09-04. No se escribió en el archivo fuente.

| Parte | Destino / tratamiento | Exactitud |
|---|---|---|
| Sol 5.5, luna 6, Rayleigh 1.116, Mie 1.858, exposición 1.102 | LumenFX | Valores directos |
| Temperaturas y tintes por fuente | Parámetros homólogos de Lumen | Mismos valores donde hay destino; respuesta matemática propia |
| Temperatura global 0.65; luz lunar 0.7 | Warmth 0.4; MoonStrength 0.745 | Conversiones FX existentes, aproximadas |
| Brillo 0.7, gamma 0.85, contraste -0.7 del perfil | Brillo -0.4, gamma 3.15, contraste -0.7 en FX | Conversión existente; no acredita la misma curva visual |
| DynamicFog=false; StaticFog=true; StaticFogDayOnly=true | AtmosphereFX | Flags directos |
| StaticVolumeFog=true y RenderProperties.UseVolumeFog=false | Dos controles distintos | No se mezclan ambos volúmenes |
| Densidad 0.00006, decay 0.21, ruido 0.51, altura 500, horizonte 1300, inicio 2852, viento 0 | AtmosphereFX | Valores directos |
| Inscattering legacy -9, intensidad 1.72 | Exponente efectivo 59049, intensidad 1.72 | Conversión de unidades de la referencia; 59049 es el valor del motor |
| Altura/inicio/distancia/borde estáticos 600/3710/4800/2800 | FogEffect | Valores directos |
| Altura/densidad/inicio/distancia/borde volumétricos 600/0.00141/0/4800/2800 | RenderProperties | Valores directos |
| Flags clásicos apagados | ClassicLightFX OPTIMIZED | Desactiva sus efectos por diseño |
| LUT 1539181199.Relight2Average | Asset instalado, seleccionado por nombre | Requiere recurso externo; no se empaqueta ni se sustituye |
| LUT, tono de cámara y bloom activos; rain motion blur apagado | SceneFX CameraEffects | Componentes nativos; build + lógica |
| Modos de mundo = juego | SceneFX libera hora, coordenadas y clima | No impone los números fijos almacenados pero inactivos del perfil |
| AA, AO y otros efectos de RenderIt Plus | Sin destino en estos cuatro FX | Fuera del alcance; no se afirma reproducir todo RenderIt Plus |

VANILLA devuelve los campos que cada FX escribió y mantiene ese modo globalmente. Con otros mods o temas activos, la referencia puede no ser vanilla puro. La etiqueta OPTIMIZED indica coincidencia con los valores FX, no equivalencia visual demostrada con RenderIt Plus.

## Persistencia y simplificación

Los cuatro archivos de preferencias están fuera de la partida. Se usa temporal/reemplazo con `.bak`, validación antes de copiar al estado, reintento tras fallos y flush al descargar. Scene separa la intención guardada de hora del instante visual: un preset solo captura el mundo cuando se pide. El archivo de estado habitual sí conserva controles y bloqueos del usuario.

Se eliminan de Scene los generadores y compatibilizadores de LUT propios y el segundo panel. En Lumen se retira la extracción de variantes built-in. No se borran ni sobreescriben presets de usuario ya instalados. Un preset viejo que solo nombraba una LUT propia retirada ahora debe escoger una LUT real.

## Pruebas ejecutadas

- Cuatro builds contra DLL reales del juego, net35/C# 7.3: cero errores.
- Cada build tiene cuatro avisos MSB3245 del entorno por referencias implícitas no resueltas: System.Data, System.Drawing, System.Runtime.Serialization y System.Xml.Linq.
- **60 PASS, 0 FAIL** en [resultados](../tests/Regression/results.latest.txt). Código real de configuración, aplicación y persistencia enlazado en un ejecutable .NET 8 con dobles.
- Cobertura: round-trip de datos, estados incompletos/NaN/texto inválido, fallos de escritura/reintento, modos, niebla por ciclo, segunda ciudad lógica, prioridades entre FX, LUT ausente/restauración, cuatro controles de cámara, hora/pausa/recaptura y error de suite parcial.
- UI/gradientes no se validan visualmente con los dobles. Harmony no se instala de verdad en esas pruebas.
- Registros de build en `tests/Regression/builds.latest.txt`.
- DLL instaladas comparadas con las huellas de la auditoría: ver `tests/Regression/installed-unchanged.json`.

Corrección metodológica respecto a la auditoría histórica: `DayNightProperties.m_TimeOfDay` usa **horas 0–24**; `normalizedTimeOfDay` divide por 24. Los dobles antiguos no acreditaban esa unidad. Los actuales y las pruebas S06/S07 usan la firma y comportamiento verificados en el juego. El informe histórico no fue reescrito retroactivamente.

## Lo que todavía no cubren por completo

- Relight: fórmulas y conversiones visuales propias; importación .light aproximada.
- Daylight Classic: LUTs/curva solar propias, sin igualdad histórica acreditada.
- Fog Controller: algunas entradas libres siguen acotadas; no hay importador completo de su XML.
- Eyecandy X: sin importación ECX; niebla meteorológica negativa no cubierta; sus preferencias/atajos no están replicados.
- Play It!: sin reloj flotante y de sistema, formatos y conjunto completo de teclas configurables. El ciclo FX es visual y no cambia el offset del reloj de simulación.
- Suite: no es transacción entre mods; una sección fallida devuelve false, pero puede haber otras aplicadas. Revisar y volver a aplicar el perfil deseado.
- Compatibilidad y orden de carga con mods externos, Theme Mixer y el proveedor de Harmony: pendientes de observación.
- No se midió rendimiento ni se acredita ausencia de tirones/cuelgues prolongados.

## Aceptación dentro del juego, pendiente

| Recorrido | Resultado exigido |
|---|---|
| Menú → primera ciudad → menú → segunda ciudad → reiniciar juego | Preferencias y modo conservados; sin duplicados ni excepciones |
| OPTIMIZED → editar → VANILLA, repetido por mod y juntos | Se aplican cambios, se liberan campos y no se restauran capturas sobre otro dueño activo |
| Cargar ciudad con ApplyOnLoad desactivado donde existe | No se impone la configuración hasta acción explícita |
| LUT instalada, inexistente y selección previa al mod | Selección correcta; aviso legible si falta; retorno a selección previa |
| Guardar estilo con y sin mundo, reaplicar y cambiar ciudad | Sin reset accidental de hora/clima; bloqueos y ajustes globales permanecen |
| Día/noche, inicio ya pausado, velocidad lenta y alta | Unidades correctas; sin realimentación del reloj ni salto incontrolado |
| Panel 280/320/360 px, resoluciones y escalas de UI | Texto legible; foco, rueda, decimal, pestañas y recolocación utilizables |
| Panel con padre de prueba nativo | Sin arrastre/ventana; Refresh y Dispose sin desactivar preferencias |
| A/B frente al DEFAULT, mismos mapa/tema/LUT/hora/cámara | Registrar diferencias de color, niebla y sombra; no aceptar por nombres de sliders |
| Sesión prolongada y arrastre de sliders | Registrar tiempos de frame, GC y excepciones con escenario reproducible |

No desplegado en esta sesión: el contrato autoriza compilación offline y exige autorización específica para instalación con respaldo. Se puede revisar el diff y los binarios locales antes de ese hito.

## Licencias y estado de integración

Código propio mantiene MIT-0; no se adopta GPL-3. Harmony conserva su aviso MIT y el texto completo de licencia. [Procedencia de SceneFX](../PROCEDENCIA.md), y documentos equivalentes en los otros repos, registran la exposición previa a fuentes legadas y el alcance de lo corregido. No hay certificación de cuarto limpio ni dictamen sobre todo el historial.

**Preparados en código para incrustación; integración y aprobación final todavía pendientes de las comprobaciones reales anteriores.**
