using System.Reflection;
using UnityEngine;

namespace SceneFX.Core
{
    /// <summary>
    /// Applies and reverts visual styles. All writes are direct values on the
    /// game's rendering components; the game look snapshot allows a full undo.
    /// </summary>
    internal static class StyleEngine
    {
        private static bool _snapshotTaken;
        private static float _vanillaSun = 1f;
        private static float _vanillaExposure = 1f;
        private static float _vanillaGamma = 2.2f;
        private static float _vanillaBoost = 1f;
        private static float _vanillaLuminance = 0.1f;

        internal static void Apply(StyleData style)
        {
            ApplyLut(style.Lut);
            ApplyTone(style);
            ApplySun(style);
            ApplyWarmth(style.Warmth);
            ApplyFog(style);
        }

        internal static void RestoreGame()
        {
            if (!_snapshotTaken)
            {
                return;
            }

            var dayNight = Object.FindObjectOfType<DayNightProperties>();
            if (dayNight != null)
            {
                dayNight.m_SunIntensity = _vanillaSun;
                dayNight.m_Exposure = _vanillaExposure;
            }

            var tone = FindToneMapping();
            if (tone != null)
            {
                tone.m_ToneMappingGamma = _vanillaGamma;
                tone.m_ToneMappingBoostFactor = _vanillaBoost;
                tone.m_Luminance = _vanillaLuminance;
            }

            ApplyWarmth(0f);
        }

        private static void TakeSnapshot()
        {
            if (_snapshotTaken)
            {
                return;
            }

            var dayNight = Object.FindObjectOfType<DayNightProperties>();
            if (dayNight != null)
            {
                _vanillaSun = dayNight.m_SunIntensity;
                _vanillaExposure = dayNight.m_Exposure;
            }

            var tone = FindToneMapping();
            if (tone != null)
            {
                _vanillaGamma = tone.m_ToneMappingGamma;
                _vanillaBoost = tone.m_ToneMappingBoostFactor;
                _vanillaLuminance = tone.m_Luminance;
            }

            _snapshotTaken = true;
        }

        internal static ColossalFramework.ToneMapping FindToneMapping()
        {
            var camera = GameObject.Find("Main Camera");
            return camera != null ? camera.GetComponent<ColossalFramework.ToneMapping>() : null;
        }

        /// <summary>
        /// Selects an installed color grading LUT by name using the game's own
        /// color correction manager.
        /// </summary>
        internal static string[] ListLuts()
        {
            var manager = ColorCorrectionManager.instance;
            if (manager == null || manager.items == null)
            {
                return new string[0];
            }

            var names = new string[manager.items.Length];
            for (int i = 0; i < manager.items.Length; i++)
            {
                names[i] = manager.items[i] ?? "(unnamed)";
            }

            return names;
        }

        internal static void ApplyLut(string name)
        {
            var manager = ColorCorrectionManager.instance;
            if (manager == null || manager.items == null || string.IsNullOrEmpty(name))
            {
                return;
            }

            for (int i = 0; i < manager.items.Length; i++)
            {
                if (manager.items[i] == name)
                {
                    manager.currentSelection = i;
                    return;
                }
            }
        }

        private static void ApplyTone(StyleData style)
        {
            TakeSnapshot();

            var tone = FindToneMapping();
            if (tone == null)
            {
                return;
            }

            float c = Mathf.Clamp(style.Contrast, -1f, 1f);

            tone.m_ToneMappingGamma = Mathf.Clamp(style.Gamma, 1.2f, 3f);
            tone.m_ToneMappingBoostFactor = 1f + 0.5f * Mathf.Clamp(style.Brightness, -1f, 1f);
            tone.m_Luminance = 0.11f + 0.02f * c;

            tone.m_ToneMappingParamsFilmic.A = 0.5f + 0.18f * c;
            tone.m_ToneMappingParamsFilmic.B = 0.26f - 0.14f * c;
            tone.m_ToneMappingParamsFilmic.C = 0.1f - 0.008f * c;
            tone.m_ToneMappingParamsFilmic.D = 0.72f + 0.18f * c;
            tone.m_ToneMappingParamsFilmic.E = 0.01f;
            tone.m_ToneMappingParamsFilmic.F = 0.24f - 0.11f * c;
            tone.m_ToneMappingParamsFilmic.W = 11f + 2.2f * c;
        }

        private static void ApplySun(StyleData style)
        {
            TakeSnapshot();

            var dayNight = Object.FindObjectOfType<DayNightProperties>();
            if (dayNight == null)
            {
                return;
            }

            dayNight.m_SunIntensity = _vanillaSun * Mathf.Clamp(style.SunIntensity, 0f, 3f);
            dayNight.m_Exposure = _vanillaExposure * Mathf.Clamp(style.Exposure, 0.5f, 1.5f);
            dayNight.m_Tonemapping = style.SkyTonemap;
        }

        /// <summary>
        /// Warmth is applied as a light regrade of the sun gradient at three
        /// key times; ambient light is left untouched.
        /// </summary>
        private static void ApplyWarmth(float warmth)
        {
            var dayNight = Object.FindObjectOfType<DayNightProperties>();
            if (dayNight == null || dayNight.m_LightColor == null)
            {
                return;
            }

            float[] times = { 0f, 0.5f, 1f };
            var keys = new GradientColorKey[times.Length];
            for (int i = 0; i < times.Length; i++)
            {
                Color c = dayNight.m_LightColor.Evaluate(times[i]);
                c.r = Mathf.Clamp01(c.r * (1f + 0.15f * warmth));
                c.b = Mathf.Clamp01(c.b * (1f - 0.15f * warmth));
                keys[i] = new GradientColorKey(c, times[i]);
            }

            dayNight.m_LightColor = new Gradient
            {
                colorKeys = keys,
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                },
            };
        }

        private static void ApplyFog(StyleData style)
        {
            var fog = Object.FindObjectOfType<FogProperties>();
            if (fog == null)
            {
                return;
            }

            if (style.FogDensity > 0f)
            {
                fog.m_FogDensity = Mathf.Clamp(style.FogDensity, 0f, 0.005f);
            }

            if (style.FogStart > 0f)
            {
                fog.m_FogStart = (int)Mathf.Clamp(style.FogStart, 0f, 10000f);
            }
        }
    }
}
