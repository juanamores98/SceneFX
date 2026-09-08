using System;
using System.IO;
using UnityEngine;
using SceneFX.Core;
using LumenFX.Runtime;
using AC = AtmosphereFX.Config.ModConfig;
using CO = ClassicLightFX.Options.ModOptions;
partial class Program
{
    static void ConsolidationChecks()
    {
        FreshWorld();
        var state = TunerRuntime.CurrentState;
        state.Gamma = 2.95f; state.SunPower = 6f; TunerRuntime.ApplyAll();
        string before = LumenFX.LumenFXMod.ExportSuiteSection();
        SceneRuntime.Current.Gamma = 1.8f; SceneRuntime.Current.BloomEnabled = 0;
        SceneRuntime.ApplyCurrent(); SceneRuntime.Current.Lut = "Other"; SceneRuntime.ApplyCurrent();
        Check("FX01 bloom/LUT edits preserve all Lumen preferences", before == LumenFX.LumenFXMod.ExportSuiteSection(), "no cross-module gamma, power, warmth or exposure write");
        Check("FX01 effective tone unchanged", Equal(ObjectTone().m_ToneMappingGamma, 2.95f), "real scalar on fake component");
        FreshWorld();
        var staticFog = UnityEngine.Object.FindObjectOfType<FogEffect>();
        foreach (bool baseline in new[] { true, false })
        {
            staticFog.m_UseVolumeFog = baseline;
            AC.StaticVolumeFog = baseline ? 0 : 1;
            AtmosphereFX.Runtime.SettingsApplier.ApplyCubemapFog();
            AC.StaticVolumeFog = -1; AtmosphereFX.Runtime.SettingsApplier.ApplyCubemapFog();
            Check("FX03 static volume Game restores " + baseline, staticFog.m_UseVolumeFog == baseline, "both acquisition directions");
        }
        var props = UnityEngine.Object.FindObjectOfType<RenderProperties>();
        props.m_volumeFogDensity = 0.003f; AC.VolumeDensity = 0.001f;
        AtmosphereFX.Runtime.SettingsApplier.ApplyRenderProperties();
        AC.VolumeDensity = -1f; AtmosphereFX.Runtime.SettingsApplier.ApplyRenderProperties();
        Check("FX03 volume density Game releases", Equal(props.m_volumeFogDensity, 0.003f), "sentinel is outside numeric UI range");
        FreshWorld();
        var dn = DayNightProperties.instance;
        dn.m_SunIntensity = 1.4f;
        ClassicLightFX.Infrastructure.PropertyLedger.Write(dn, "m_SunIntensity", 3.3f);
        LumenFX.Infrastructure.PropertyLedger.Write(dn, "m_SunIntensity", 7f);
        ClassicLightFX.Infrastructure.PropertyLedger.ReleaseAll();
        Check("FX04 release old owner preserves new owner", Equal(dn.m_SunIntensity, 7f), "handoff shares baseline");
        LumenFX.Infrastructure.PropertyLedger.ReleaseAll();
        Check("FX04 final owner restores original acquisition", Equal(dn.m_SunIntensity, 1.4f), "not the old owner's transformed value");
        LumenFX.Infrastructure.PropertyLedger.Write(dn, "m_SunIntensity", 7f);
        dn.m_SunIntensity = 9f;
        LumenFX.Infrastructure.PropertyLedger.ReleaseAll();
        Check("FX04 newer external value is preserved", Equal(dn.m_SunIntensity, 9f), "safe release does not overwrite external changes");
        FreshWorld();
        before = SceneFX.SceneFXMod.ExportSuiteSection();
        bool ok = SuiteManager.ApplySuiteProfile("<suiteProfile><scenefx><bloomEnabled>0</bloomEnabled></scenefx><lumenfx><gamma>2.8</gamma></lumenfx><atmospherefx><density>invalid</density></atmospherefx></suiteProfile>");
        Check("FX05 invalid third section changes nothing", !ok && before == SceneFX.SceneFXMod.ExportSuiteSection(), SuiteManager.LastResult);
        before = SceneFX.SceneFXMod.ExportSuiteSection();
        ok = SuiteManager.ApplySuiteProfile("<suiteProfile><lumenfx><gamma>2.9</gamma></lumenfx><scenefx><lut>missing LUT</lut></scenefx></suiteProfile>");
        Check("FX05 LUT preflight changes nothing", !ok && before == SceneFX.SceneFXMod.ExportSuiteSection(), SuiteManager.LastResult);
        var tone = ObjectTone(); float gamma = tone.m_ToneMappingGamma;
        ok = SceneFX.SceneFXMod.ApplySuiteSection("<scenefx><lut>missing LUT</lut><bloomEnabled>0</bloomEnabled></scenefx>");
        Check("FX05 missing LUT rejects local preset before mutation", !ok && before == SceneFX.SceneFXMod.ExportSuiteSection() && Equal(tone.m_ToneMappingGamma,gamma), "no partially applied camera preset");
        FreshWorld();
        before = LumenFX.LumenFXMod.ExportSuiteSection();
        LumenFX.IO.StateStore.SaveImmediate();
        string path = Path.Combine(ConfigDir, "LumenFX2.xml"); string bytes = File.ReadAllText(path);
        string blocker = path + ".tmp"; if (File.Exists(blocker)) File.Delete(blocker); Directory.CreateDirectory(blocker);
        ok = SuiteManager.ApplySuiteProfile("<suiteProfile><lumenfx><gamma>2.9</gamma></lumenfx><atmospherefx><density>0.001</density></atmospherefx></suiteProfile>");
        Directory.Delete(blocker);
        Check("FX05 disk failure restores preferences", !ok && before == LumenFX.LumenFXMod.ExportSuiteSection(), SuiteManager.LastResult);
        Check("FX05 disk failure restores saved bytes", File.ReadAllText(path) == bytes, "memory and disk rollback");
        ok = SuiteManager.ApplySuiteProfile("<suiteProfile><lumenfx><gamma>2.9</gamma></lumenfx><atmospherefx><density>0.001</density></atmospherefx></suiteProfile>");
        Check("FX05 valid suite commits after recovered disk failure", ok && Equal(state.Gamma,2.9f) && Equal(AC.Density,0.001f), SuiteManager.LastResult);
        FreshWorld();
        CO.Instance.SunColor = true; CO.Instance.SunStrength = true; CO.Instance.SunCoords = false;
        CO.Instance.ClassicFogMode = true; CO.Instance.ClassicFogWithCycle = true;
        CO.Instance.SwapLuts = false; CO.Instance.ClassicFogTint = false;
        ClassicLightFX.Core.ClassicLook.Attach();
        state.SunPower = 6f; TunerRuntime.ApplyAll(); AtmosphereFX.Runtime.SettingsApplier.ApplyAll();
        before = LumenFX.LumenFXMod.ExportSuiteSection();
        ClassicLightFX.Core.ClassicLook.ApplyFromOptions();
        Check("FX08 classic power applied through Lumen", Equal(DayNightProperties.instance.m_SunIntensity,3.318695f) && before == LumenFX.LumenFXMod.ExportSuiteSection(), "derived behavior preserves user recipe");
        Check("FX08 classic fog applied through Atmosphere", UnityEngine.Object.FindObjectOfType<FogEffect>().enabled && !UnityEngine.Object.FindObjectOfType<DayNightFogEffect>().enabled, "one writer for fog enable flags");
        ClassicLightFX.Core.ClassicLook.Release();
        Check("FX08 releasing Classic reveals saved Lumen", Equal(DayNightProperties.instance.m_SunIntensity,6f), "no silent preference overwrite");
        FreshWorld();
        var legacy = new StyleData { SunIntensity = 2f, Warmth = 0.5f, Gamma = 2.8f, Lut = "Other" };
        string input = Path.Combine(ConfigDir, "legacy.scene.xml"); File.WriteAllText(input, Xml(legacy));
        string source = File.ReadAllText(input);
        string migrated = LegacyMigration.SaveSuite(input);
        ok = SuiteManager.ApplySuiteProfile(migrated);
        Check("FX02 migrated multiplier preserves intensity semantics", ok && Equal(DayNightProperties.instance.m_SunIntensity, 2f) && Equal(state.SunStrength,1f), "multiplier is never mapped to colour gain");
        Check("FX02 legacy source and exact backup preserved", File.ReadAllText(input) == source && File.ReadAllText(migrated + ".source.xml") == source, "migration produces a new suite");
        File.Delete(migrated); File.Delete(migrated + ".source.xml");
    }
    static ColossalFramework.ToneMapping ObjectTone() { return UnityEngine.Object.FindObjectOfType<ColossalFramework.ToneMapping>(); }
}
