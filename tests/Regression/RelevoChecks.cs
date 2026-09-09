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

        LutBank.Unregister();
        Check("R17 unregistering leaves the list exactly as it was",
            manager.m_BuiltinLUTs.Length == builtInsBefore
            && string.Join("|", manager.items) == string.Join("|", before),
            "entries=" + manager.m_BuiltinLUTs.Length + " expected=" + builtInsBefore);
    }
}
