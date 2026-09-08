# Validación de la consolidación FX 2.1

Fecha: 2026-09-08.

**Resultado ejecutado: 90 PASS / 0 FAIL.**

[GitHub Actions: ejecución 34292241614](https://github.com/juanamores98/SceneFX/actions/runs/34292241614)

Código SceneFX verificado: `7f585a542602b9a69544ca6060a12fdb77a91e78`.
Participantes fijados por commit en el workflow:

| Módulo | Commit verificado |
|---|---|
| LumenFX | `e15552fbf8df17940f308cd8d3da00819d2dd30a` |
| AtmosphereFX | `a5daebe8541067488ce334944ab61fce8b2286e8` |
| ClassicLightFX | `9dfe6fcef8b5941082a2b245bca69bd4c19f5ba5` |

Se compiló y ejecutó el harness con .NET 8 y **C# 7.3**. Enlaza fuentes de estado, aplicación, persistencia, coordinación y los cuatro paneles reales; usa dobles de los componentes y eventos del juego. Incluye los escenarios de regresión originales y los de la consolidación. Resultado detallado: [results.latest.txt](../tests/Regression/results.latest.txt).

Pruebas relevantes: aislamiento Scene/Lumen; Game en ambos sentidos y por propiedad; transferencia de referencia y respeto de cambios externos; validación de LUT y tercera sección; rollback de memoria/archivos por fallo de escritura; solicitudes clásicas de luz/niebla/coordenadas; migración de ganancia solar; cambio de un preset migrado a Default; persistencia de cámara y modelo; construcción de paneles; coma decimal; foco y notificaciones; deshacer; etiquetas en español.

No se ejecutaron Cities: Skylines, Unity, el parche Harmony ni comparaciones visuales. **No hay compilaciones nuevas de las DLL net35 contra los ensamblados reales del juego en esta entrega.** Los archivos históricos builds.latest.txt e installed-unchanged.json pertenecen a la entrega anterior; no son evidencia de esta consolidación.

Las LUT clásicas se bloquean expresamente mientras Scene controla la selección. El registro de variantes clásicas como recursos independientes seleccionables en Scene queda pendiente; no se reemplaza silenciosamente su textura activa. También permanecen pendientes la calibración A/B, la matriz completa de convivencia real, el rendimiento y los cambios de tema que muten gradientes internamente sin cambiar su referencia.

La documentación de este resultado puede tener un commit posterior al código probado, sin modificaciones adicionales de código.

[Compilación y prueba en Windows](CONSOLIDACION-v3.md).
