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
        // Los dueños de lo que este estilo no escribe por su cuenta.
        private const string LumenFXMod = "LumenFX.LumenFXMod";
        private const string AtmosphereFXMod = "AtmosphereFX.AtmosphereFXMod";

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

        private static bool _toneWritten, _sunWritten, _exposureWritten, _skyWritten, _gradientWritten;
        private static bool _fogWritten, _lutWritten;
        private static float _fogDensity;
        private static float _fogStart;
        private static bool _skyTonemap;
        private static int _lutSelection;
        private static int _lastLutSelection;
        private static bool _delegateCompanions;
        internal static string LastLutError = string.Empty;
        internal static string ActiveClaims
        {
            get
            {
                return (_lutWritten ? "lut," : "")
                    + (_toneWritten && !SuiteManager.IsLumenFXWriting("tone") ? "tone," : "")
                    + (_sunWritten && !SuiteManager.IsLumenFXWriting("sunIntensity") ? "sunIntensity," : "")
                    + (_exposureWritten && !SuiteManager.IsLumenFXWriting("exposure") ? "exposure," : "")
                    + (_skyWritten && !SuiteManager.IsLumenFXWriting("skyTonemapping") ? "skyTonemapping," : "")
                    + (_gradientWritten && !SuiteManager.IsLumenFXWriting("lightColor") ? "lightColor," : "")
                    + (_fogWritten && !Infrastructure.FxInterop.Claims(AtmosphereFXMod, "fog") ? "fog," : "");
            }
        }

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
            _toneWritten = _sunWritten = _exposureWritten = _skyWritten = _gradientWritten = false;
            _fogWritten = _lutWritten = false;
            LastLutError = string.Empty;
            CameraEffects.Clear();
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

        internal static void Apply(StyleData style, bool delegateCompanions = true)
        {
            _delegateCompanions = delegateCompanions;
            ApplyLut(style.Lut, string.Empty);
            CameraEffects.Apply(style);
            ApplyTone(style);
            ApplySun(style);
            ApplyWarmth(style.Warmth);
            ApplyFog(style);

        }

        /// <summary>Lleva al mundo lo que dice el estilo: hora, sol, clima y ritmo.</summary>
        internal static void ApplyWorld(StyleData style)
        {
            WorldController.TimeLocked = style.TimeLocked;
            if (style.TimeSet || style.IncludeWorld) WorldController.ApplyTime(style.TimeOfDay);
            if (style.PositionSet || style.IncludeWorld) WorldController.ApplyPosition(style.Latitude, style.Longitude);

            WorldController.RainIntensity = style.Rain < 0f ? -1f : Mathf.Clamp(style.Rain, 0f, 2.5f);
            WorldController.FogIntensity = Clamped(style.Fog);
            WorldController.CloudIntensity = Clamped(style.Cloud);
            WorldController.NorthernLights = Clamped(style.NorthernLights);
            WorldController.Rainbow = Clamped(style.Rainbow);
            WorldController.GroundWetness = Clamped(style.GroundWetness);

            WorldController.TemperatureLocked = style.TemperatureLock;
            WorldController.Temperature = style.Temperature;
            WorldController.WindLocked = style.WindLock;
            WorldController.WindDirection = style.WindDirection;

            WorldController.WeatherEnabled = TriState(style.WeatherEnabled);
            WorldController.RainIsSnow = TriState(style.RainIsSnow);
            WorldController.SnowyRoads = TriState(style.SnowyRoads);
            WorldController.Tick();

            TimeController.CycleSpeedEnabled = style.CycleSpeedEnabled;
            TimeController.CycleSpeed = style.CycleSpeed;
            TimeController.NightCycleSpeed = style.NightCycleSpeed;
            TimeController.SeparateDayNight = style.SeparateDayNight;
            TimeController.CycleWhilePaused = style.CycleWhilePaused;
            TimeController.ApplyGameSpeed(style.GameSpeed);
        }

        /// <summary>Copia al estilo lo que hay ahora mismo en el mundo.</summary>
        /// <remarks>
        /// Lo que se exporta o se guarda tiene que ser lo que se ve. Los controles escriben
        /// directamente en <see cref="WorldController"/>, asi que el estilo se refresca desde
        /// ahi antes de guardarlo o de publicarlo a la suite.
        /// </remarks>
        internal static void CaptureWorld(StyleData style, bool snapshotMoment = false)
        {
            style.TimeOfDay = snapshotMoment ? WorldController.ReadTimeHours() : WorldController.TimeOfDayHours;
            style.TimeLocked = WorldController.TimeLocked;
            style.TimeSet = WorldController.TimeSet;
            style.PositionSet = WorldController.PositionSet;

            var dayNight = GetDayNight();
            if (dayNight != null)
            {
                style.Latitude = dayNight.m_Latitude;
                style.Longitude = dayNight.m_Longitude;
            }

            style.Rain = WorldController.RainIntensity;
            style.Fog = WorldController.FogIntensity;
            style.Cloud = WorldController.CloudIntensity;
            style.NorthernLights = WorldController.NorthernLights;
            style.Rainbow = WorldController.Rainbow;
            style.GroundWetness = WorldController.GroundWetness;

            style.TemperatureLock = WorldController.TemperatureLocked;
            style.Temperature = WorldController.Temperature;
            style.WindLock = WorldController.WindLocked;
            style.WindDirection = WorldController.WindDirection;

            style.WeatherEnabled = WorldController.WeatherEnabled;
            style.RainIsSnow = WorldController.RainIsSnow;
            style.SnowyRoads = WorldController.SnowyRoads;

            style.GameSpeed = TimeController.GameSpeed;
            style.CycleSpeedEnabled = TimeController.CycleSpeedEnabled;
            style.CycleSpeed = TimeController.CycleSpeed;
            style.NightCycleSpeed = TimeController.NightCycleSpeed;
            style.SeparateDayNight = TimeController.SeparateDayNight;
            style.CycleWhilePaused = TimeController.CycleWhilePaused;
        }

        private static float Clamped(float value)
        {
            return value < 0f ? -1f : Mathf.Clamp01(value);
        }

        private static int TriState(int value)
        {
            return value < 0 ? -1 : (value == 0 ? 0 : 1);
        }

        internal static void RestoreGame()
        {
            CameraEffects.Restore();
            RestoreLut();
            if (_fogWritten && !Infrastructure.FxInterop.Claims(AtmosphereFXMod, "fog"))
            {
                var fog = GetFogProperties();
                if (fog != null) { fog.m_FogDensity = _fogDensity; fog.m_FogStart = _fogStart; }
            }
            _fogWritten = false;

            var dayNight = GetDayNight();
            if (dayNight != null && _sunCaptured)
            {
                if (_sunWritten && !SuiteManager.IsLumenFXWriting("sunIntensity")) dayNight.m_SunIntensity = _vanillaSun;
                if (_skyWritten && !SuiteManager.IsLumenFXWriting("skyTonemapping")) dayNight.m_Tonemapping = _skyTonemap;

                if (_exposureWritten && !ThemeOwnership.AtmosphereIsManaged && !SuiteManager.IsLumenFXWriting("exposure"))
                {
                    dayNight.m_Exposure = _vanillaExposure;
                }

                if (_gradientWritten && _gradientsCaptured && !SuiteManager.IsLumenFXWriting("lightColor"))
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
            if (tone != null && _toneCaptured && _toneWritten && !SuiteManager.IsLumenFXWriting("tone"))
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
            _toneWritten = _sunWritten = _exposureWritten = _skyWritten = _gradientWritten = false;
            _sunCaptured = _toneCaptured = _gradientsCaptured = false;
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
                _skyTonemap = dayNight.m_Tonemapping;
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

        internal static void ApplyLut(string name, string ignoredLegacyFallback)
        {
            var manager = ColorCorrectionManager.instance;
            if (manager == null || manager.items == null) return;
            if (string.IsNullOrEmpty(name)) { RestoreLut(); LastLutError = string.Empty; return; }
            int found = Array.IndexOf(manager.items, name);
            if (found < 0)
            {
                // Accept a short asset name only if its Workshop suffix is unambiguous.
                for (int i = 0; i < manager.items.Length; i++)
                    if (manager.items[i] != null && manager.items[i].EndsWith("." + name, StringComparison.Ordinal))
                    {
                        if (found >= 0) { found = -1; break; }
                        found = i;
                    }
            }
            if (found < 0)
            {
                LastLutError = "LUT not installed: " + name;
                return;
            }
            if (!_lutWritten) _lutSelection = manager.lastSelection;
            manager.currentSelection = found;
            _lastLutSelection = found;
            _lutWritten = true;
            LastLutError = string.Empty;
        }

        private static void RestoreLut()
        {
            var manager = ColorCorrectionManager.instance;
            if (_lutWritten && manager != null && manager.lastSelection == _lastLutSelection)
                manager.currentSelection = _lutSelection;
            _lutWritten = false;
        }

        /// <remarks>
        /// El tono tiene un solo dueño: LumenFX. Si está cargado, este estilo no escribe la
        /// curva —se la pide—, y así aplicar el mismo preset en un orden u otro da lo mismo.
        /// Sin LumenFX, SceneFX la escribe como siempre.
        /// </remarks>
        private static void ApplyTone(StyleData style)
        {
            TakeSnapshot();

            if (SuiteManager.IsLumenFXWriting("tone"))
            {
                if (!_delegateCompanions) return;
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                SuiteManager.Delegate(LumenFXMod,
                    "<lumenfx>"
                    + "<gamma>" + Mathf.Clamp(style.Gamma, 1.5f, 3.5f).ToString("0.###", ci) + "</gamma>"
                    + "<brightness>" + Mathf.Clamp(style.Brightness, -1f, 4f).ToString("0.###", ci) + "</brightness>"
                    + "<contrast>" + Mathf.Clamp(style.Contrast, -1f, 1f).ToString("0.###", ci) + "</contrast>"
                    + "<skyTonemapping>" + (style.SkyTonemap ? "true" : "false") + "</skyTonemapping>"
                    + "<skyExposure>" + style.Exposure.ToString("R", ci) + "</skyExposure>"
                    + "<warmth>" + style.Warmth.ToString("R", ci) + "</warmth>"
                    + "<sunStrength>" + style.SunIntensity.ToString("R", ci) + "</sunStrength>"
                    + "</lumenfx>");
                return;
            }

            var tone = FindToneMapping();
            if (tone == null)
            {
                return;
            }

            _toneWritten = true;
            float c = Mathf.Clamp(style.Contrast, -1f, 1f);

            tone.m_ToneMappingGamma = Mathf.Clamp(style.Gamma, 1.5f, 3.5f);
            tone.m_ToneMappingBoostFactor = (style.Brightness <= 1f ? 1f + 0.6f * style.Brightness : 1.6f + 0.84f * (style.Brightness - 1f));
            tone.m_Luminance = 0.10f + 0.02f * c;

            tone.m_ToneMappingParamsFilmic.A = 0.50f + 0.20f * c;
            tone.m_ToneMappingParamsFilmic.B = 0.25f - 0.15f * c;
            tone.m_ToneMappingParamsFilmic.C = 0.10f - 0.01f * c;
            tone.m_ToneMappingParamsFilmic.D = 0.70f + 0.20f * c;
            tone.m_ToneMappingParamsFilmic.E = 0.01f;
            tone.m_ToneMappingParamsFilmic.F = 0.25f - 0.12f * c;
            tone.m_ToneMappingParamsFilmic.W = 11.2f + 2.5f * c;
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
                _sunWritten = true;
                dayNight.m_SunIntensity = _vanillaSun * Mathf.Clamp(style.SunIntensity, 0f, 3f);
            }

            // Un valor pedido por el estilo se aplica; lo que no se repone es la línea base
            // capturada —ver RestoreGame—, que puede ser anterior al tema.
            if (!SuiteManager.IsLumenFXWriting("exposure"))
            {
                if (style.Exposure > 0f)
                {
                    _exposureWritten = true;
                    dayNight.m_Exposure = Mathf.Clamp(style.Exposure, 0f, 5f);
                }
                else if (_exposureWritten)
                {
                    if (!ThemeOwnership.AtmosphereIsManaged) dayNight.m_Exposure = _vanillaExposure;
                    _exposureWritten = false;
                }
            }

            if (!SuiteManager.IsLumenFXWriting("skyTonemapping"))
            {
                _skyWritten = true;
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

            // La curva solar es de LumenFX. Se le pide la calidez y no se toca la gradiente:
            // si los dos escriben, el ultimo trabaja sobre la salida del otro.
            if (SuiteManager.IsLumenFXWriting("lightColor")) return;
            _gradientWritten = true;

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

        /// <remarks>
        /// La niebla tiene un solo dueño: AtmosphereFX. Si está cargado, el estilo se la pide.
        /// </remarks>
        private static void ApplyFog(StyleData style)
        {
            if (style.FogDensity <= 0f && style.FogStart <= 0f) return;
            if (Infrastructure.FxInterop.Claims(AtmosphereFXMod, "fog"))
            {
                if (!_delegateCompanions) return;
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                var xml = new System.Text.StringBuilder("<atmospherefx>");
                if (style.FogDensity > 0f)
                {
                    xml.Append("<density>")
                       .Append(Mathf.Clamp(style.FogDensity, 0f, 0.005f).ToString("0.######", ci))
                       .Append("</density>");
                }

                if (style.FogStart > 0f)
                {
                    xml.Append("<startDistance>")
                       .Append(Mathf.Clamp(style.FogStart, 0f, 10000f).ToString("0.#", ci))
                       .Append("</startDistance>");
                }

                xml.Append("</atmospherefx>");
                SuiteManager.Delegate(AtmosphereFXMod, xml.ToString());
                return;
            }

            var fog = GetFogProperties();
            if (fog == null)
            {
                return;
            }

            if (!_fogWritten) { _fogDensity = fog.m_FogDensity; _fogStart = fog.m_FogStart; _fogWritten = true; }
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
