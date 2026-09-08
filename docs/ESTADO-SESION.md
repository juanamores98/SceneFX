# Estado de sesión — SceneFX

- Fecha: 2026-09-08.
- Repo: `juanamores98/SceneFX`, rama `main`.
- HEAD de partida: `24921aa96bb5146f299b1d1d8fc84196d4680c8b`.
- Cambios preparados para commit y push a main por autorización del usuario. Árbol de partida limpio; esta entrega conserva las pruebas offline y no instala el mod.
- Encargo: correcciones, VANILLA/OPTIMIZED y panel estrecho incrustable; MIT/MIT-0; sin integración.
- NeuralFX y SkyFX excluidos.

## Entregado

Se retiran NativeLut, LutCompat, BakeToLut, SkyMood y las variantes Optimized con LUTs procedurales. El selector usa ColorCorrectionManager y assets instalados. Se arreglan la persistencia de mundo/velocidad/hora bloqueada, restauración de LUT/cámara, estilos completos, rutas de selección y escrituras pendientes. Se añaden interruptores nativos de LUT, tono, bloom y motion blur de lluvia.

Receta del perfil activo Default. SHA256 del archivo fuente: `71062f3ac84872ca305db47237e5fd7168bf5b5b629cfff9b24eae45ad6bbfba`. Referencia y limitaciones en `SceneFX/docs/VALIDACION.md`.

## Comprobado

- Build net35/C# 7.3: cero errores.
- Cuatro advertencias MSB3245: referencias implícitas System.Data, System.Drawing, System.Runtime.Serialization y System.Xml.Linq no resueltas en este entorno.
- Suite conjunta: **60 PASS, 0 FAIL**, con .NET 8 y dobles. Código y resultados en `SceneFX/tests/Regression`.
- Build sin instalación; DLL instaladas fuera de este cambio.

## Pendiente y próximo paso

Sin importador de presets ECX. El ciclo sigue siendo visual, separado del reloj de simulación; no reproduce todos los comportamientos de Play It. No incluye su reloj flotante configurable, reloj del sistema ni todos sus atajos/preferencias. La niebla meteorológica negativa del legado no está representada: -1 significa liberar. La suite puede aplicarse parcialmente si una sección falla; devuelve false y debe revisarse, no es una transacción entre cuatro mods.

Prueba agrupada en el juego con los cuatro binarios de esta revisión, después de autorizar despliegue con respaldo y juego cerrado. Matriz en `SceneFX/docs/VALIDACION.md`: primera/segunda ciudad, reinicio, modos repetidos, LUT presente/ausente, UI estrecha, convivencia y comparación con DEFAULT. Registrar observaciones y medidas antes de aprobar integración o afirmar «100 %».
