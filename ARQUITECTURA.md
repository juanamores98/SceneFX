# Arquitectura actual de SceneFX

Revisión 2026-09-08. El historial conserva la descripción anterior de ventanas y presets.

## Recorrido

1. Entrada del mod y anfitrión: configuración global y servicios del nivel.
2. Modelo: `Core/StyleData.cs` y `SceneRuntime.cs`. El XML se lee en un documento temporal validado antes de copiarlo al estado vivo.
3. Motor: `Core/StyleEngine.cs`, `CameraEffects.cs`, `WorldController.cs` y `TimeController.cs`. Captura antes de escribir; limpia referencias y caches al descargar.
4. `FxModule` expone estado y panel. `UI/PanelView.cs` contiene disposición vertical, controles nativos y refresco sin escribir.
5. `Infrastructure/FxStorage.cs` proporciona escritura segura, validación de finitos y reconocimiento de la receta; `FxInterop.cs` consulta reclamaciones sin dependencia obligatoria del compañero.

Se retiran NativeLut, LutCompat, BakeToLut, SkyMood y las variantes Optimized con LUTs procedurales. El selector usa ColorCorrectionManager y assets instalados. Se arreglan la persistencia de mundo/velocidad/hora bloqueada, restauración de LUT/cámara, estilos completos, rutas de selección y escrituras pendientes. Se añaden interruptores nativos de LUT, tono, bloom y motion blur de lluvia.

## Modos

VANILLA suspende el módulo y devuelve los campos escritos a su referencia previa cuando le corresponde. No equivale a aplicar constantes supuestamente neutras. Conserva el modo en el archivo global.

OPTIMIZED lee `BuiltIns/Optimized.xml`, incorporado en el ensamblado. La referencia seleccionada y diferencias están en `SceneFX/docs/Default.reference.xml` y `SceneFX/docs/VALIDACION.md`. No lee RenderIt Plus por frame ni lo integra.

`Mode` compara la receta contra el estado exportado: al editar muestra CUSTOM. Describe la configuración, no acredita disponibilidad del LUT ni ausencia de interferencias externas. `Status` comunica errores detectados.

## Contrato de incrustación

Llamadas de UI y motor en el hilo principal de Unity, con servicios del nivel disponibles:

```csharp
var panel = SceneFX.FxModule.CreatePanel(parent, 320f, 650f);
string xml = SceneFX.FxModule.ReadState();
bool accepted = SceneFX.FxModule.ApplyState(xml);
panel.Refresh(); // refrescar tras modificaciones externas
panel.SetSize(320f, 700f);
panel.Dispose(); // destruir UI no desactiva la configuración
SceneFX.FxModule.Flush();
```

- `parent` es un `ColossalFramework.UI.UIComponent` del futuro host. Este conserva la referencia, visibilidad y disposición.
- `Release()` es una acción separada: libera motor y persiste VANILLA.
- `ApplyState` recibe la sección XML exportada del mod. Rechaza raíz ajena, texto inválido y números no finitos. No aplica parcialmente una sección con validación fallida.
- La exportación no incluye la posición de ventana. Los campos de mundo tienen su alcance y modos explícitos.
- `SceneFXMod.ActiveClaims` informa de campos compartidos; no bloquea físicamente escrituras del motor.
- Tamaño preferido 360×680 y mínimo 280×260. Reapertura independiente recoloca el panel dentro de la resolución actual.

Sin SDK global, RPC, plugins ni referencia a Arrebol/RenderIt Plus.

## Cooperación y guardado

Lumen tiene prioridad para la luz que reclama; Atmosphere, para la niebla cuando está activo. Scene delega ediciones de look a Lumen activo y aplica localmente cuando está suspendido. Classic consulta reclamaciones antes de escribir/restaurar. La carga automática de Scene no sustituye las preferencias globales guardadas de Lumen.

La coordinación cubre los FX revisados. Otros mods, el orden real de carga y cambios tardíos de tema requieren prueba dentro del juego; no se garantiza restauración universal.

Estado: SceneFX.xml y SceneFXOptions.xml. Cambios agrupados durante aproximadamente un segundo, escritura temporal, reemplazo con `.bak`, pendiente hasta éxito y flush al cerrar anfitrión. Carga inválida no copia los primeros campos al estado vivo. La recuperación de `.bak` es manual; no se implementa una migración universal de formatos legados.

## Verificación

[Estado de sesión](docs/ESTADO-SESION.md) y [paridad](docs/PARIDAD.md) distinguen controles, comportamiento, formatos y aspecto.

Sin importador de presets ECX. El ciclo sigue siendo visual, separado del reloj de simulación; no reproduce todos los comportamientos de Play It. No incluye su reloj flotante configurable, reloj del sistema ni todos sus atajos/preferencias. La niebla meteorológica negativa del legado no está representada: -1 significa liberar. La suite puede aplicarse parcialmente si una sección falla; devuelve false y debe revisarse, no es una transacción entre cuatro mods.
