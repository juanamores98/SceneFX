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
        private static DayNightProperties _cachedDayNight;
        private static bool _lutWritten;
        private static int _lutSelection, _lastLutSelection;
        internal static string LastLutError = string.Empty;
        internal static string ActiveClaims { get { return _lutWritten ? "lut," : string.Empty; } }
        internal static void ClearCache()
        {
            PropertyLedger.Forget(); _cachedDayNight = null;
            _lutWritten = false; LastLutError = string.Empty; CameraEffects.Clear();
        }
        private static DayNightProperties GetDayNight()
        {
            if (_cachedDayNight == null) _cachedDayNight = UnityEngine.Object.FindObjectOfType<DayNightProperties>();
            return _cachedDayNight;
        }
        // Compatibility hook: Scene now acquires only its own properties at first write.
        internal static void CaptureBaseline() { }

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
            else WorldController.ReleaseTime();
            if (style.PositionSet || style.IncludeWorld) WorldController.ApplyPosition(style.Latitude, style.Longitude);
            else WorldController.ReleasePosition();

            WorldController.RainIntensity = style.Rain < 0f ? -1f : Mathf.Clamp(style.Rain, 0f, 2.5f);
            WorldController.FogIntensity = Clamped(style.Fog, WorldController.FogFloor);
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
                style.Latitude = WorldController.PositionSet ? WorldController.RequestedLatitude : dayNight.m_Latitude;
                style.Longitude = WorldController.PositionSet ? WorldController.RequestedLongitude : dayNight.m_Longitude;
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
            return Clamped(value, 0f);
        }

        /// <summary>
        /// Recorta un canal del clima respetando su suelo, que en la niebla es negativo.
        /// </summary>
        /// <remarks>
        /// Hay dos clases de valor negativo y no se pueden confundir: -1 significa «esto lo
        /// lleva el juego», y cualquier cosa entre el suelo del canal y 1 es un ajuste. Para
        /// la niebla el suelo es -0,485, asi que un recorte que mande a -1 todo lo negativo
        /// convierte un ajuste valido en un canal suelto.
        /// </remarks>
        private static float Clamped(float value, float floor)
        {
            return value <= -0.9f ? -1f : Mathf.Clamp(value, floor, 1f);
        }

        private static int TriState(int value)
        {
            return value < 0 ? -1 : (value == 0 ? 0 : 1);
        }

        internal static void RestoreGame()
        {
            CameraEffects.Restore(); RestoreLut(); PropertyLedger.ReleaseAll();
        }

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
