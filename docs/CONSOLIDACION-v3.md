# Consolidación FX 2.1 / contrato v3

Esta entrega implementa la consolidación del plan del 8 de septiembre de 2026. Los cuatro repositorios deben actualizarse juntos para usar suites y comportamiento clásico delegado. Cada mod conserva su ensamblado autónomo, target net35 y C# 7.3; no hay quinto mod obligatorio ni código trasladado de mods legados.

## Comportamiento

- Scene conserva cámara/LUT, tiempo y meteorología. Una edición de bloom, LUT o clima no modifica las preferencias de Lumen ni de Atmosphere. Los antiguos campos de iluminación siguen legibles para migración explícita; ya no son controles activos duplicados.
- Lumen controla luz, tono de cámara (incluida su habilitación), exposición y sombras. Ganancia de color y potencia solar se identifican por separado.
- Atmosphere libera cada campo opcional al elegir Game. Los valores negativos se conservan como formato compatible en XML, pero están fuera del recorrido numérico de los sliders: la interfaz ofrece Juego/Manual. Se mantiene el último valor manual mientras el panel está abierto.
- El registro de propiedades compartido usa identidad del componente, propiedad, referencia, último valor escrito y dueño. Un traspaso entre FX conserva la referencia anterior a sus transformaciones. Al liberar se conserva una escritura externa posterior; una nueva escritura externa detectada pasa a ser la referencia. Los registros se eliminan al terminar el ciclo del componente/mod. No se promete detectar cambios externos que escriban exactamente el mismo valor, mutaciones internas de una Gradient ni atomicidad frente a terceros.
- Classic aporta solicitudes de curva solar, intensidad y selección de niebla a Lumen/Atmosphere. No altera sus documentos de preferencias. Al desactivarlo reaparece la receta guardada. Las coordenadas con dueño Scene y el reemplazo de LUT con selección activa en Scene se muestran bloqueados de forma explícita: no se presenta una opción omitida como aplicada. Se conserva el adaptador autónomo y se identifican las aproximaciones visuales como tales.

## Presets, guardado y migración

Game significa liberar este módulo a su referencia adquirida; no garantiza el aspecto de una instalación sin otros mods. Default v3 es la referencia personal RenderIt Plus / Default, sin garantía de rendimiento ni equivalencia visual calibrada. Scene necesita `1539181199.Relight2Average`; si falta o es ambigua se rechaza antes de cambiar estado. La receta integrada está embebida en cada DLL y la nueva suite usa `Default-v3.suite.xml`, dejando intactas recetas anteriores.

Las suites validan todos los participantes y secciones antes de aplicar. Durante el commit se difieren las escrituras de los cuatro módulos. Un error revierte los documentos en memoria y los archivos; los errores de rollback se muestran como PARTIAL. La validación requiere una ciudad cargada y versiones con el nuevo protocolo. No se afirma haber verificado el render por haber guardado un XML.

Para un `.scene.xml` antiguo: Scene → Presets → Migrar preset antiguo a suite. Se genera un nuevo `*-migrated-v3.suite.xml` y una copia exacta `.source.xml`. No se modifica el origen. La ganancia solar se aplica mediante el modelo de compatibilidad de Lumen como multiplicador de la referencia solar, nunca como `SunStrength`. La calidez conserva la fórmula anterior de Scene en la curva directa. Los valores del resto de la receta de Lumen permanecen como ajustes de ese propietario; comparar visualmente el resultado antes de aceptarlo.

Los paneles incluyen deshacer de una acción, restauración por campo (valor al abrir el panel salvo un default explícito), entrada con punto/coma decimal, escalas logarítmicas para rangos grandes y campos dependientes deshabilitados. Se suscriben a cambios externos de los otros FX y respetan campos de texto que tienen el foco. Las etiquetas principales incluyen español según el idioma del juego y fallback inglés; reabrir el panel tras cambiar idioma.

## Validación y límites

La suite de regresión de Scene enlaza fuentes actuales de los cuatro repos y usa dobles del juego. Cubre estado/persistencia, aislamiento de ediciones, Game por campo, handoff, LUT ausente, sección inválida, fallo de disco y solicitudes clásicas. El workflow `FX source regressions` permite ejecutarla con .NET 8. No sustituye una compilación net35 con DLL reales ni la ejecución de Unity/Harmony.

No hay acceso a tus DLL instaladas, ciudad, tema, capturas ni rendimiento en este entorno. El criterio visual y las comprobaciones de interacción real permanecen pendientes de tu prueba. La calibración histórica exacta y las capacidades de Lumina fuera del alcance no se declaran terminadas.

## Compilar y probar en Windows

Clonar los cuatro repositorios uno junto a otro. Desde SceneFX:

```powershell
.\tools\Build-Fx.ps1 -ManagedPath 'C:\Program Files (x86)\Steam\steamapps\common\Cities_Skylines\Cities_Data\Managed'
```

Con el juego cerrado, añadir `-Deploy` para compilar y copiar las DLL a Addons/Mods. El script produce hashes locales y detiene el proceso si una compilación falla. Respaldar antes tus preferencias, presets y DLL instaladas. No mezclar DLL antiguas y nuevas en la prueba de suites.

1. Ajustar gamma en Lumen y luego LUT/bloom en Scene: gamma y demás preferencias de Lumen deben conservarse.
2. Niebla volumétrica clásica: On → Off → Game y Off → On → Game; comprobar el efecto real y no solo el selector.
3. Aplicar Default v3 con/sin su LUT. Sin LUT debe fallar sin cambiar los demás campos.
4. Activar/desactivar Classic junto a Lumen/Atmosphere; los parámetros guardados de los dueños deben conservarse.
5. Probar por separado y juntos; ciudad A → menú → ciudad B → reinicio; tema antes/después; liberar todos los FX.
6. Comparar mañana, mediodía, tarde y noche con cámara, mapa, tema, LUT y clima idénticos. Registrar FPS/tiempo de frame, captura y hashes de DLL. Validar las filas, desplazamiento, decimales y foco a tu resolución/escala.
