# Regresiones offline de los cuatro FX

Desde la raíz común de los cuatro repos:

```powershell
dotnet run --project SceneFX/tests/Regression/Regression.csproj -c Release
```

El ejecutable devuelve código 1 si falla una aserción y 0 si pasan todas. Enlaza fuentes actuales de los cuatro repos; no carga las DLL instaladas.

Requiere .NET 8 para el ejecutable de prueba. Eso no cambia el target del mod: los cuatro siguen en net35/C# 7.3.

Cada ejecución crea una carpeta de ajustes aislada bajo la salida del test y cambia a ella su directorio de trabajo. No utiliza los archivos de preferencias reales del usuario. Las operaciones de suites probadas reciben XML en memoria. No llamar desde este harness a las rutas de instalación o extracción de suites.

Stubs.cs reemplaza servicios del juego para comprobar mutaciones, persistencia y orden. No emula render, UI, ciclo de vida Unity ni aplicación de Harmony. Gradient.Evaluate es intencionalmente simplificado: no hay aserciones de paridad visual.

Resultados de la revisión: results.latest.txt. Builds reales: builds.latest.txt. Verificación de DLL instaladas: installed-unchanged.json. La matriz de aceptación real está en ../../docs/VALIDACION.md.

## Consolidación 2.1

El harness también enlaza `FxModule`, `PanelView` y las etiquetas de los cuatro módulos. `UiStubs.cs` permite comprobar eventos, foco, entrada decimal y deshacer; no reproduce el layout ni los gráficos nativos. La migración usa un directorio de datos aislado por ejecución. Resultado ejecutado y límites: [VALIDACION-2.1.md](../../docs/VALIDACION-2.1.md).
