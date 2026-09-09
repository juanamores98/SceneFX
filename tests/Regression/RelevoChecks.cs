using System;
using System.Linq;
using UnityEngine;
using SceneFX.Core;
using LumenFX.Runtime;
using CO = ClassicLightFX.Options.ModOptions;

/// <summary>
/// Comprobaciones del relevo de lo que en Render It! Plus tiene licencia restrictiva:
/// los dos rangos que estaban por debajo del original, las longitudes de onda que
/// faltaban, y el banco de tablas de color propio.
/// </summary>
partial class Program
{
    static void RelevoChecks()
    {
        // ---- Rangos que Eyecandy X ofrecia y aqui se habian quedado cortos --------
        FreshWorld();
        WorldController.ApplyPosition(-118f, 0f);
        Check("R01 latitude reaches Eyecandy X's range",
            Equal(WorldController.RequestedLatitude, -118f),
            "asked -118, got " + WorldController.RequestedLatitude + " (before: clamped to -90)");

        WorldController.ApplyPosition(200f, 0f);
        Check("R02 latitude still stops at the ceiling",
            Equal(WorldController.RequestedLatitude, 120f),
            "asked 200, got " + WorldController.RequestedLatitude);

        WorldController.SetChannel("fog", -0.4f);
        Check("R03 weather fog keeps a negative value",
            Equal(WorldController.FogIntensity, -0.4f) && WorldController.ChannelLocked("fog"),
            "value=" + WorldController.FogIntensity + " locked=" + WorldController.ChannelLocked("fog"));

        WorldController.SetChannel("fog", -0.6f);
        Check("R04 weather fog floor stops at Eyecandy X's",
            Equal(WorldController.FogIntensity, -0.485f),
            "asked -0.6, between the floor and the release mark; got " + WorldController.FogIntensity);

        WorldController.SetChannel("fog", -1f);
        Check("R05 minus one still means the game owns it",
            !WorldController.ChannelLocked("fog"),
            "released=" + !WorldController.ChannelLocked("fog") + " value=" + WorldController.FogIntensity);

        var style = new StyleData { Fog = -0.4f, Latitude = -118f };
        style.Validate();
        Check("R06 the saved document keeps both",
            Equal(style.Fog, -0.4f) && Equal(style.Latitude, -118f),
            "fog=" + style.Fog + " latitude=" + style.Latitude);

        style = new StyleData { Fog = -0.7f };
        style.Validate();
        Check("R07 an in-between fog value is not ambiguous",
            Equal(style.Fog, -0.485f),
            "-0.7 sits between the floor and the release mark; kept as " + style.Fog);

        // ---- Las longitudes de onda del cielo, la cuarta forma que faltaba --------
        FreshWorld();
        var dayNight = DayNightProperties.instance;
        dayNight.m_WaveLengths = new Vector3(680f, 550f, 440f);
        var state = TunerRuntime.CurrentState;
        state.SkyWaveR = 700f; state.SkyWaveG = 0f; state.SkyWaveB = 0f;
        LumenFX.Core.LightingMixer.Apply(state);
        Check("R08 a single wavelength channel moves alone",
            Equal(dayNight.m_WaveLengths.x, 700f)
            && Equal(dayNight.m_WaveLengths.y, 550f) && Equal(dayNight.m_WaveLengths.z, 440f),
            "asked red only; got " + dayNight.m_WaveLengths.x + "/" + dayNight.m_WaveLengths.y + "/" + dayNight.m_WaveLengths.z);

        Check("R09 LumenFX announces the wavelengths it writes",
            LumenFX.LumenFXMod.ActiveClaims.Contains("waveLengths"),
            "claims=" + LumenFX.LumenFXMod.ActiveClaims);

        state.SkyWaveR = 0f;
        LumenFX.Core.LightingMixer.Apply(state);
        Check("R10 releasing them gives the map its value back",
            Equal(dayNight.m_WaveLengths.x, 680f),
            "back to " + dayNight.m_WaveLengths.x + " and no longer claimed="
            + !LumenFX.LumenFXMod.ActiveClaims.Contains("waveLengths"));

        // ---- Y con el tinte clasico pedido, LumenFX no toca el campo -------------
        FreshWorld();
        dayNight = DayNightProperties.instance;
        dayNight.m_WaveLengths = new Vector3(680f, 550f, 440f);
        CO.Instance.ClassicFogTint = true;
        AppDomain.CurrentDomain.SetData("FX.ClassicRequests.v1", "fogTint,");
        state = TunerRuntime.CurrentState;
        state.SkyWaveR = 900f; state.SkyWaveG = 900f; state.SkyWaveB = 900f;
        LumenFX.Core.LightingMixer.Apply(state);
        Check("R11 the classic tint owner is not overwritten",
            Equal(dayNight.m_WaveLengths.x, 680f),
            "asked 900 with fogTint requested; field stayed at " + dayNight.m_WaveLengths.x);
        Check("R12 and LumenFX does not claim what it did not write",
            !LumenFX.LumenFXMod.ActiveClaims.Contains("waveLengths"),
            "claims=" + LumenFX.LumenFXMod.ActiveClaims);
        AppDomain.CurrentDomain.SetData("FX.ClassicRequests.v1", string.Empty);
        CO.Instance.ClassicFogTint = false;
        state.SkyWaveR = state.SkyWaveG = state.SkyWaveB = 0f;

        // ---- El banco de tablas de color propias --------------------------------
        FreshWorld();
        var manager = ColorCorrectionManager.instance;
        string[] before = (string[])manager.items.Clone();
        int builtInsBefore = manager.m_BuiltinLUTs.Length;

        LutBank.RegisterBuiltIns();
        var names = StyleEngine.ListLuts();
        int found = new[] { "Ordinary", "Nocturne", "Sepia", "Cine", "Frost", "Ember" }
            .Count(n => Array.IndexOf(names, "SceneFX " + n) >= 0);
        Check("R13 the six own tables reach the game's list",
            found == 6, found + "/6 visible as SceneFX <name>");

        LutBank.RegisterBuiltIns();
        Check("R14 registering twice does not duplicate",
            manager.m_BuiltinLUTs.Length == builtInsBefore + 6,
            "entries=" + manager.m_BuiltinLUTs.Length + " expected=" + (builtInsBefore + 6));

        var withOwnTable = new StyleData { Lut = "SceneFX Cine" };
        bool accepted = true;
        try { StyleEngine.ValidateResources(withOwnTable); }
        catch (Exception) { accepted = false; }
        Check("R15 a style may name an own table",
            accepted, "validation of 'SceneFX Cine' " + (accepted ? "passed" : "rejected: " + StyleEngine.LastLutError));

        string png;
        string baked = LutBank.BakeCurrentLook("Test", out png);
        Check("R16 baking produces a selectable table",
            Array.IndexOf(StyleEngine.ListLuts(), baked) >= 0,
            "baked as '" + baked + "'");

        // El juego traduce el nombre de cada tabla incluida. Sin la clave, escribia una
        // linea de error por tabla cada vez que se dibujaba el desplegable: dieciocho por
        // apertura en la primera medida en partida.
        Check("R17b every own table gets its localized name",
            LocaleEntries() >= 6,
            "entries registered under BUILTIN_COLORCORRECTION = " + LocaleEntries());

        LutBank.Unregister();
        Check("R17 unregistering leaves the list exactly as it was",
            manager.m_BuiltinLUTs.Length == builtInsBefore
            && string.Join("|", manager.items) == string.Join("|", before),
            "entries=" + manager.m_BuiltinLUTs.Length + " expected=" + builtInsBefore);

        // ---- Un clic en Optimized tiene que bastar --------------------------------
        // El usuario reporta que a veces hay que pulsarlo varias veces. Si el primer
        // clic dejara algo sin aplicar, el segundo daria un estado distinto: aplicar
        // dos veces seguidas es la forma de verlo sin mirar la pantalla.
        FreshWorld();
        TunerRuntime.CurrentState.Gamma = 1.9f;
        TunerRuntime.CurrentState.SunPower = 1f;
        TunerRuntime.CurrentState.VanillaMode = false;
        TunerRuntime.ApplyAll();

        LumenFX.FxModule.ApplyOptimized();
        string lumenOnce = LumenFX.LumenFXMod.ExportSuiteSection();
        float toneOnce = ObjectTone().m_ToneMappingGamma;
        float sunOnce = DayNightProperties.instance.m_SunIntensity;
        string modeOnce = LumenFX.FxModule.Mode;

        LumenFX.FxModule.ApplyOptimized();
        Check("R18 one click on Optimized is enough for LumenFX",
            lumenOnce == LumenFX.LumenFXMod.ExportSuiteSection(),
            "settings after the second click " + (lumenOnce == LumenFX.LumenFXMod.ExportSuiteSection() ? "identical" : "DIFFERENT"));
        Check("R19 and the engine already had the values after the first",
            Equal(toneOnce, ObjectTone().m_ToneMappingGamma)
            && Equal(sunOnce, DayNightProperties.instance.m_SunIntensity),
            "gamma " + toneOnce + " -> " + ObjectTone().m_ToneMappingGamma
            + ", sun " + sunOnce + " -> " + DayNightProperties.instance.m_SunIntensity);
        Check("R19b nothing is reported as left over when it all took",
            LumenFX.Infrastructure.FxStorage.OptimizedGap(
                LumenFX.LumenFXMod.ExportSuiteSection(), typeof(LumenFX.LumenFXMod)) == null,
            "gap after a clean apply = "
            + (LumenFX.Infrastructure.FxStorage.OptimizedGap(
                LumenFX.LumenFXMod.ExportSuiteSection(), typeof(LumenFX.LumenFXMod)) ?? "none"));

        Check("R20 the button reports OPTIMIZED after one click",
            modeOnce == "OPTIMIZED",
            "mode after the first click = " + modeOnce);

        FreshWorld();
        AtmosphereFX.Config.ModConfig.Density = 0.0009f;
        AtmosphereFX.Config.ModConfig.VanillaMode = false;
        AtmosphereFX.Runtime.SettingsApplier.ApplyAll();
        AtmosphereFX.FxModule.ApplyOptimized();
        string atmoOnce = AtmosphereFX.AtmosphereFXMod.ExportSuiteSection();
        string atmoModeOnce = AtmosphereFX.FxModule.Mode;
        AtmosphereFX.FxModule.ApplyOptimized();
        Check("R21 one click on Optimized is enough for AtmosphereFX",
            atmoOnce == AtmosphereFX.AtmosphereFXMod.ExportSuiteSection()
            && atmoModeOnce == "OPTIMIZED",
            "mode after the first click = " + atmoModeOnce);

        FreshWorld();
        ClassicLightFX.FxModule.ApplyOptimized();
        string classicOnce = ClassicLightFX.ClassicLightFXMod.ExportSuiteSection();
        string classicModeOnce = ClassicLightFX.FxModule.Mode;
        ClassicLightFX.FxModule.ApplyOptimized();
        Check("R22 one click on Optimized is enough for ClassicLightFX",
            classicOnce == ClassicLightFX.ClassicLightFXMod.ExportSuiteSection()
            && classicModeOnce == "OPTIMIZED",
            "mode after the first click = " + classicModeOnce);

        FreshWorld();
        SceneFX.FxModule.ApplyOptimized();
        string sceneOnce = SceneFX.SceneFXMod.ExportSuiteSection();
        string sceneModeOnce = SceneFX.FxModule.Mode;
        SceneFX.FxModule.ApplyOptimized();
        Check("R23 one click on Optimized is enough for SceneFX",
            sceneOnce == SceneFX.SceneFXMod.ExportSuiteSection()
            && sceneModeOnce == "OPTIMIZED",
            "mode after the first click = " + sceneModeOnce);

        // ---- Y el camino de los presets respeta el suelo de la niebla -------------
        // Medido en partida: por aqui, -0,485 se convertia en -1 y el canal quedaba
        // suelto. La interfaz pasaba por SetChannel y si lo respetaba.
        FreshWorld();
        StyleEngine.ApplyWorld(new StyleData { Fog = -0.485f, IncludeWorld = true });
        Check("R24 a preset can ask for negative fog",
            Equal(WorldController.FogIntensity, -0.485f),
            "asked -0.485 through ApplyWorld, got " + WorldController.FogIntensity);

        StyleEngine.ApplyWorld(new StyleData { Fog = -1f, IncludeWorld = true });
        Check("R25 and minus one through the same path still releases",
            !WorldController.ChannelLocked("fog"),
            "released=" + !WorldController.ChannelLocked("fog"));
    }

    static int LocaleEntries()
    {
        var field = typeof(ColossalFramework.Globalization.LocaleManager).GetField(
            "m_Locale", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var locale = field == null ? null
            : field.GetValue(ColossalFramework.Globalization.LocaleManager.instance)
                as ColossalFramework.Globalization.Locale;
        return locale == null ? 0 : locale.Count;
    }
}
