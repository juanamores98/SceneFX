using System;
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
        private static bool _sunCaptured;
        private static bool _toneCaptured;
        private static float _vanillaSun = 1f;
        private static float _vanillaExposure = 1f;
        private static float _vanillaGamma = 2.2f;
        private static float _vanillaBoost = 1f;
        private static float _vanillaLuminance = 0.1f;
        private static float _filmicA;
        private static float _filmicB;
        private static float _filmicC;
        private static float _filmicD;
        private static float _filmicE = 0.01f;
        private static float _filmicF = 0.24f;
        private static float _filmicW = 11f;

        private static Gradient _originalLight;
        private static Gradient _originalSky;
        private static Gradient _originalEquator;
        private static Gradient _originalGround;
        private static bool _gradientsCaptured;

        private static ColossalFramework.ToneMapping _cachedToneMapping;
        private static DayNightProperties _cachedDayNight;
        private static FogProperties _cachedFogProperties;

        internal static void ClearCache()
        {
            _cachedToneMapping = null;
            _cachedDayNight = null;
            _cachedFogProperties = null;
            _sunCaptured = false;
            _toneCaptured = false;
            _gradientsCaptured = false;
            _originalLight = null;
            _originalSky = null;
            _originalEquator = null;
            _originalGround = null;
        }

        private static DayNightProperties GetDayNight()
        {
            if (_cachedDayNight == null)
            {
                _cachedDayNight = UnityEngine.Object.FindObjectOfType<DayNightProperties>();
            }
            return _cachedDayNight;
        }

        private static FogProperties GetFogProperties()
        {
            if (_cachedFogProperties == null)
            {
                _cachedFogProperties = UnityEngine.Object.FindObjectOfType<FogProperties>();
            }
            return _cachedFogProperties;
        }

        internal static void Apply(StyleData style)
        {
            ApplyLut(style.Lut, style.NativeLut);
            ApplyTone(style);
            ApplySun(style);
            ApplyWarmth(style.Warmth);
            ApplyFog(style);
            SkyMood.Apply(style.SkyMood);

            if (style.IncludeWorld)
            {
                WorldController.ApplyTime(style.TimeOfDay);
                WorldController.ApplyPosition(style.Latitude, style.Longitude);
                if (style.Rain >= 0f || style.Fog >= 0f || style.Cloud >= 0f)
                {
                    WorldController.ApplyWeather(
                        style.Rain >= 0f ? style.Rain : 0f,
                        style.Fog >= 0f ? style.Fog : 0f,
                        style.Cloud >= 0f ? style.Cloud : 0f);
                }
            }
        }

        internal static void RestoreGame()
        {
            if (!_sunCaptured && !_toneCaptured)
            {
                return;
            }

            var dayNight = GetDayNight();
            if (dayNight != null && _sunCaptured)
            {
                dayNight.m_SunIntensity = _vanillaSun;
                dayNight.m_Exposure = _vanillaExposure;

                if (_gradientsCaptured)
                {
                    // Exact game gradients, not a warmth-zero approximation.
                    dayNight.m_LightColor = _originalLight;
                    var ambientType = typeof(DayNightProperties.AmbientColor);
                    var ambient = dayNight.m_AmbientColor;
                    if (_originalSky != null)
                    {
                        ambientType.GetField("m_SkyColor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(ambient, _originalSky);
                    }

                    if (_originalEquator != null)
                    {
                        ambientType.GetField("m_EquatorColor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(ambient, _originalEquator);
                    }

                    if (_originalGround != null)
                    {
                        ambientType.GetField("m_GroundColor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(ambient, _originalGround);
                    }
                }
            }

            var tone = FindToneMapping();
            if (tone != null && _toneCaptured)
            {
                tone.m_ToneMappingGamma = _vanillaGamma;
                tone.m_ToneMappingBoostFactor = _vanillaBoost;
                tone.m_Luminance = _vanillaLuminance;
                tone.m_ToneMappingParamsFilmic.A = _filmicA;
                tone.m_ToneMappingParamsFilmic.B = _filmicB;
                tone.m_ToneMappingParamsFilmic.C = _filmicC;
                tone.m_ToneMappingParamsFilmic.D = _filmicD;
                tone.m_ToneMappingParamsFilmic.E = _filmicE;
                tone.m_ToneMappingParamsFilmic.F = _filmicF;
                tone.m_ToneMappingParamsFilmic.W = _filmicW;
            }
        }

        private static void CaptureGradients(DayNightProperties dn)
        {
            if (_gradientsCaptured)
            {
                return;
            }

            if (dn == null || dn.m_LightColor == null)
            {
                return;
            }

            _originalLight = dn.m_LightColor;
            var ambientType = typeof(DayNightProperties.AmbientColor);
            var ambient = dn.m_AmbientColor;
            _originalSky = (Gradient)ambientType.GetField("m_SkyColor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(ambient);
            _originalEquator = (Gradient)ambientType.GetField("m_EquatorColor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(ambient);
            _originalGround = (Gradient)ambientType.GetField("m_GroundColor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(ambient);
            _gradientsCaptured = true;
        }

        /// <summary>
        /// Toma la referencia del juego lo antes posible en la escena.
        /// </summary>
        /// <remarks>
        /// <b>Por que existe aparte.</b> El snapshot se tomaba de forma perezosa, la primera
        /// vez que el usuario aplicaba un estilo. Para entonces otro mod de la suite ya podia
        /// haber escrito, y lo capturado no era el valor del juego sino el suyo. Medido en
        /// partida: con ClassicLightFX aplicando su potencia solar al cargar, SceneFX capturo
        /// 3.318695 como "vanilla" y la multiplico por el factor del estilo, dando 7.798934.
        ///
        /// Llamarlo al cargar el nivel no elimina la carrera entre mods —el orden de carga no
        /// se puede fijar desde aqui— pero la reduce a esa ventana, en vez de depender de
        /// cuando al usuario le da por abrir el panel.
        /// </remarks>
        internal static void CaptureBaseline()
        {
            TakeSnapshot();

            var dayNight = GetDayNight();
            if (dayNight != null)
            {
                CaptureGradients(dayNight);
            }
        }

        private static void TakeSnapshot()
        {
            var dayNight = GetDayNight();
            if (dayNight != null && !_sunCaptured)
            {
                _vanillaSun = dayNight.m_SunIntensity;
                _vanillaExposure = dayNight.m_Exposure;
                _sunCaptured = true;
            }

            var tone = FindToneMapping();
            if (tone != null && !_toneCaptured)
            {
                _vanillaGamma = tone.m_ToneMappingGamma;
                _vanillaBoost = tone.m_ToneMappingBoostFactor;
                _vanillaLuminance = tone.m_Luminance;
                _filmicA = tone.m_ToneMappingParamsFilmic.A;
                _filmicB = tone.m_ToneMappingParamsFilmic.B;
                _filmicC = tone.m_ToneMappingParamsFilmic.C;
                _filmicD = tone.m_ToneMappingParamsFilmic.D;
                _filmicE = tone.m_ToneMappingParamsFilmic.E;
                _filmicF = tone.m_ToneMappingParamsFilmic.F;
                _filmicW = tone.m_ToneMappingParamsFilmic.W;
                _toneCaptured = true;
            }
        }

        internal static ColossalFramework.ToneMapping FindToneMapping()
        {
            if (_cachedToneMapping == null)
            {
                var camera = GameObject.Find("Main Camera");
                if (camera != null)
                {
                    _cachedToneMapping = camera.GetComponent<ColossalFramework.ToneMapping>();
                }
            }

            return _cachedToneMapping;
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

        internal static void ApplyLut(string name, string nativeFallback)
        {
            var manager = ColorCorrectionManager.instance;
            if (manager == null)
            {
                return;
            }

            // 1. Native game selector: builtin + user-installed LUT assets.
            if (!string.IsNullOrEmpty(name) && manager.items != null)
            {
                // Exact name, then suffix (workshop-id.name), then loose contains.
                int found = -1;
                for (int i = 0; i < manager.items.Length && found < 0; i++)
                {
                    if (manager.items[i] == name)
                    {
                        found = i;
                    }
                }

                for (int i = 0; i < manager.items.Length && found < 0; i++)
                {
                    string item = manager.items[i];
                    if (item != null && (item.EndsWith("." + name, StringComparison.Ordinal) || item.Contains(name)))
                    {
                        found = i;
                    }
                }

                if (found >= 0)
                {
                    manager.currentSelection = found;
                    return;
                }
            }

            // 2. Compatibility mode: tables installed by the user on this
            // machine, read at runtime. Nothing is shipped with the mod.
            Texture3D custom;
            if (LutCompat.TryGet(name, out custom))
            {
                manager.SetLUT(custom);
                return;
            }

            // 3. Native procedural table generated by this mod.
            if (NativeLut.TryGet(name, out custom))
            {
                manager.SetLUT(custom);
                return;
            }

            // 4. Style-defined fallback table.
            if (NativeLut.TryGet(nativeFallback, out custom))
            {
                manager.SetLUT(custom);
            }
        }

        private static void ApplyTone(StyleData style)
        {
            TakeSnapshot();

            if (SuiteManager.IsLumenFXToneWriter())
            {
                return;
            }

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

            var dayNight = GetDayNight();
            if (dayNight == null)
            {
                return;
            }

            // Cada eje se cede por separado: LumenFX puede estar escribiendo la intensidad
            // solar y no la exposicion, o al reves. Escribir igual no solo perderia la pelea
            // -el parche de LumenFX corre en cada refresco de luz- sino que dejaria el valor
            // parpadeando entre los dos mods.
            if (!SuiteManager.IsLumenFXWriting("sunIntensity"))
            {
                dayNight.m_SunIntensity = _vanillaSun * Mathf.Clamp(style.SunIntensity, 0f, 3f);
            }

            if (!SuiteManager.IsLumenFXWriting("exposure"))
            {
                dayNight.m_Exposure = _vanillaExposure * Mathf.Clamp(style.Exposure, 0.5f, 1.5f);
            }

            if (!SuiteManager.IsLumenFXWriting("skyTonemapping"))
            {
                dayNight.m_Tonemapping = style.SkyTonemap;
            }
        }

        /// <summary>
        /// Warmth is applied as a light regrade of the captured sun gradient
        /// at its own key times, so the curve shape is preserved and the
        /// operation stays idempotent. Ambient light is left untouched.
        /// </summary>
        private static void ApplyWarmth(float warmth)
        {
            var dayNight = GetDayNight();
            if (dayNight == null || dayNight.m_LightColor == null)
            {
                return;
            }

            CaptureGradients(dayNight);

            if (!_gradientsCaptured || _originalLight == null)
            {
                return;
            }

            // La curva solar la remuestrea LumenFX entera cuando esta activo. Si los dos
            // escriben, el ultimo en hacerlo trabaja sobre la salida del otro.
            if (SuiteManager.IsLumenFXWriting("lightColor"))
            {
                return;
            }

            if (Mathf.Approximately(warmth, 0f))
            {
                dayNight.m_LightColor = _originalLight;
                return;
            }

            var sourceKeys = _originalLight.colorKeys;
            var keys = new GradientColorKey[sourceKeys.Length];
            for (int i = 0; i < sourceKeys.Length; i++)
            {
                Color c = sourceKeys[i].color;
                c.r = Mathf.Clamp01(c.r * (1f + 0.15f * warmth));
                c.b = Mathf.Clamp01(c.b * (1f - 0.15f * warmth));
                keys[i] = new GradientColorKey(c, sourceKeys[i].time);
            }

            dayNight.m_LightColor = new Gradient
            {
                colorKeys = keys,
                alphaKeys = _originalLight.alphaKeys,
            };
        }

        private static void ApplyFog(StyleData style)
        {
            var fog = GetFogProperties();
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
