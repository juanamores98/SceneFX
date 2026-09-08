# SceneFX

Selector de LUTs normales de Cities: Skylines 1, estilos y controles de clima, tiempo y cámara, con coordinación opcional de los otros tres FX.

## Uso

- Abrir con **F10 (F11 abre el mismo panel)**, o UUI opcional.
- Panel nativo preferido: **360 × 680**, mínimo 280 × 260. Secciones: Look, Weather, Time, Presets.
- **VANILLA** libera las modificaciones del módulo y guarda ese modo. Restaura la referencia capturada respetando compañeros detectados; un tema u otro mod puede hacer que difiera del vanilla puro.
- **OPTIMIZED** aplica la parte de este módulo del **Default personal de RenderIt Plus**. Es una receta de aspecto, no de rendimiento.
- Editar controles guarda un estado personalizado. El pie distingue VANILLA, OPTIMIZED y CUSTOM según los valores configurados.
- Los sliders incluyen entrada decimal y refresco sin escrituras por repaint.

LUT instalada 1539181199.Relight2Average; LUT, tono de cámara y bloom activados; motion blur de lluvia apagado. Tono/calidez FX según la conversión documentada. Hora, coordenadas y clima quedan bajo control del juego, como indican los modos del DEFAULT.

## Persistencia

Archivos globales: **SceneFX.xml y SceneFXOptions.xml**, bajo `%LOCALAPPDATA%\Colossal Order\Cities_Skylines`. Independientes de la partida. Temporal y reemplazo con copia `.bak`, pendientes que se reintentan y guardado al cerrar el anfitrión. Se conserva lectura desde la ubicación histórica cuando procede. Un cierre forzado durante el intervalo de guardado puede perder el último cambio pendiente.

Los built-ins no sobrescriben presets ya extraídos del usuario. Los botones de modo leen la receta incorporada; un antiguo archivo llamado Optimized puede contener valores distintos.

## Incrustación futura

`FxModule.CreatePanel(parent, width, height)` crea el panel dentro de un `UIComponent`. Con padre no tiene arrastre ni botón de ventana. Ofrece `ReadState`, `ApplyState`, `Release`, `ApplyOptimized`, `Flush`, `Mode` y `Status`. Ver [arquitectura](ARQUITECTURA.md).

No se ha integrado con RenderIt Plus ni Arrebol; tampoco hay dependencia de esos productos.

## Compilar y verificar

```powershell
dotnet build SceneFX.csproj -c Release
```

Target **net35 / C# 7.3**, referencias de CS1 instalado. El build normal genera `bin/Release/net35` y **no instala**. El target de despliegue requiere `DeployMod=true`; solo debe utilizarse con autorización, juego cerrado y respaldo.

Regresiones conjuntas: `SceneFX/tests/Regression/Regression.csproj`, que enlaza el código actual de los cuatro repos hermanos. Prueba lógica en .NET 8 con dobles del motor; no valida render ni interacción visual.

## Límites

Sin importador de presets ECX. El ciclo sigue siendo visual, separado del reloj de simulación; no reproduce todos los comportamientos de Play It. No incluye su reloj flotante configurable, reloj del sistema ni todos sus atajos/preferencias. La niebla meteorológica negativa del legado no está representada: -1 significa liberar. La suite puede aplicarse parcialmente si una sección falla; devuelve false y debe revisarse, no es una transacción entre cuatro mods.

[Paridad](docs/PARIDAD.md) · [Estado](docs/ESTADO-SESION.md) · [Procedencia](PROCEDENCIA.md). `DESIGN.md` se conserva como referencia histórica.

Código propio bajo **MIT-0**, [LICENSE](LICENSE).
