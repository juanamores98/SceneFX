using System;
using SceneFX.Infrastructure;
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
            PropertyLedger.Forget();
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
            ValidateResources(style);
            ApplyLut(style.Lut, string.Empty);
            CameraEffects.Apply(style);
            // Old documents keep their payload for explicit migration. Normal Scene edits
            // never apply light/tone/fog and never mutate a companion's preferences.
        }

        internal static void ValidateResources(StyleData style)
        {
            if (string.IsNullOrEmpty(style.Lut)) return;
            var names = ListLuts();
            int matches = Array.IndexOf(names, style.Lut) >= 0 ? 1 : 0;
            if (matches == 0)
                foreach (var name in names) if (name.EndsWith("." + style.Lut, StringComparison.Ordinal)) matches++;
            if (matches != 1)
            {
                LastLutError = "LUT missing or ambiguous: " + style.Lut;
                throw new InvalidOperationException(LastLutError);
            }
            LastLutError = string.Empty;
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
            PropertyLedger.ReleaseAll();
            _toneWritten = _sunWritten = _exposureWritten = _skyWritten = _gradientWritten = false;
            _fogWritten = false;
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

    }
}
