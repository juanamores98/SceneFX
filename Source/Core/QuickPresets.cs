using UnityEngine;

namespace SceneFX.Core
{
    /// <summary>
    /// Los dos ajustes de un clic: el del juego sin tocar, y la receta del usuario.
    /// </summary>
    /// <remarks>
    /// <b>Que son.</b> <c>Vanilla</c> deja este mod sin imponer nada: el juego tal cual.
    /// <c>Optimized</c> es la receta calibrada del usuario, la misma que llevaba el preset
    /// «Default» de Render It!+, derivada en su dia de como tenia configurados los mods
    /// clasicos que esta suite sustituye.
    ///
    /// <b>Por que pasan por ApplySuiteSection.</b> Es el mismo camino que recorre un perfil de
    /// suite guardado, con sus validaciones y sus efectos inmediatos. Un atajo que escribiera
    /// los campos por su cuenta se desincronizaria del resto en cuanto alguien anadiera un
    /// ajuste nuevo.
    /// </remarks>
    internal static class QuickPresets
    {
        private const string VanillaXml =
            "<scenefx>" +
            "<vanillaMode>true</vanillaMode>" +
            // Vanilla no es «poner ceros»: es soltar. Cada canal en -1 y cada interruptor de
            // tres estados en -1 devuelven el clima y el ritmo al juego.
            "<rain>-1</rain>" +
            "<fog>-1</fog>" +
            "<cloud>-1</cloud>" +
            "<northernLights>-1</northernLights>" +
            "<rainbow>-1</rainbow>" +
            "<groundWetness>-1</groundWetness>" +
            "<temperatureLock>false</temperatureLock>" +
            "<windLock>false</windLock>" +
            "<weatherEnabled>-1</weatherEnabled>" +
            "<rainIsSnow>-1</rainIsSnow>" +
            "<snowyRoads>-1</snowyRoads>" +
            "<gameSpeed>1</gameSpeed>" +
            "<cycleSpeedEnabled>false</cycleSpeedEnabled>" +
            "<cycleSpeed>1</cycleSpeed>" +
            "<nightCycleSpeed>1</nightCycleSpeed>" +
            "<separateDayNight>false</separateDayNight>" +
            "<cycleWhilePaused>false</cycleWhilePaused>" +
            "</scenefx>";

        private const string OptimizedXml =
            "<scenefx>" +
            "<vanillaMode>false</vanillaMode>" +
            "<lut>1539181199.Relight2Average</lut>" +
            "<nativeLut>ordinary</nativeLut>" +
            "<gamma>3.15</gamma>" +
            "<brightness>-0.4</brightness>" +
            "<contrast>-0.7</contrast>" +
            "<sunIntensity>1</sunIntensity>" +
            "<exposure>1.102</exposure>" +
            "<warmth>0.4</warmth>" +
            "<fogDensity>0.00006</fogDensity>" +
            "<fogStart>2852</fogStart>" +
            "<skyTonemap>true</skyTonemap>" +
            "<skyMood>0</skyMood>" +
            "<includeWorld>true</includeWorld>" +
            "<timeOfDay>12</timeOfDay>" +
            "<latitude>36</latitude>" +
            "<longitude>88</longitude>" +
            "<rain>0</rain>" +
            "<fog>0</fog>" +
            "<cloud>0</cloud>" +
            // El resto del mundo se deja al juego: la receta calibrada es de imagen, no de
            // clima ni de ritmo.
            "<northernLights>-1</northernLights>" +
            "<rainbow>-1</rainbow>" +
            "<groundWetness>-1</groundWetness>" +
            "<temperatureLock>false</temperatureLock>" +
            "<windLock>false</windLock>" +
            "<weatherEnabled>-1</weatherEnabled>" +
            "<rainIsSnow>-1</rainIsSnow>" +
            "<snowyRoads>-1</snowyRoads>" +
            "<gameSpeed>1</gameSpeed>" +
            "<cycleSpeedEnabled>false</cycleSpeedEnabled>" +
            "<cycleSpeed>1</cycleSpeed>" +
            "<nightCycleSpeed>1</nightCycleSpeed>" +
            "<separateDayNight>false</separateDayNight>" +
            "<cycleWhilePaused>false</cycleWhilePaused>" +
            "</scenefx>";

        internal static bool ApplyVanilla()
        {
            return Apply(VanillaXml, "Vanilla");
        }

        internal static bool ApplyOptimized()
        {
            return Apply(OptimizedXml, "Optimized");
        }

        private static bool Apply(string xml, string name)
        {
            bool ok = SceneFX.SceneFXMod.ApplySuiteSection(xml);
            Debug.Log("[SceneFX] preset " + name + (ok ? " aplicado" : " RECHAZADO"));
            return ok;
        }
    }
}
