using System;
using UnityEngine;

namespace SceneFX.Core
{
    /// <summary>
    /// Clean-room world controls built on the game's own public fields:
    /// time of day, sun coordinates and weather intensities. A snapshot of
    /// the untouched values allows full restoration.
    /// </summary>
    internal static class WorldController
    {
        private static bool _snapshotTaken;
        private static float _vanillaLatitude;
        private static float _vanillaLongitude;
        private static DayNightProperties _dayNight;
        private static int _frameCounter;

        internal static void ClearCache()
        {
            _dayNight = null;
            _snapshotTaken = false;
            _frameCounter = 0;
        }

        private static DayNightProperties DayNight
        {
            get
            {
                if (_dayNight == null)
                {
                    if (_frameCounter <= 0)
                    {
                        _frameCounter = 60;
                        _dayNight = UnityEngine.Object.FindObjectOfType<DayNightProperties>();
                    }
                    else
                    {
                        _frameCounter--;
                    }
                }

                return _dayNight;
            }
        }

        internal static bool TimeLocked;
        internal static float TimeOfDayHours = 12f;   // 0..24

        internal static float RainIntensity = -1f;     // -1 = leave to the game
        internal static float FogIntensity = -1f;
        internal static float CloudIntensity = -1f;

        internal static void Snapshot()
        {
            if (_snapshotTaken)
            {
                return;
            }

            var dayNight = DayNight;
            if (dayNight != null)
            {
                _vanillaLatitude = dayNight.m_Latitude;
                _vanillaLongitude = dayNight.m_Longitude;
            }

            _snapshotTaken = true;
        }

        internal static void Tick()
        {
            var dayNight = DayNight;
            if (dayNight == null)
            {
                return;
            }

            if (TimeLocked)
            {
                dayNight.m_TimeOfDay = Mathf.Repeat(TimeOfDayHours / 24f, 1f);
            }

            ApplyWeather();
        }

        internal static void ApplyTime(float hours)
        {
            TimeOfDayHours = Mathf.Repeat(hours, 24f);
            var dayNight = DayNight;
            if (dayNight != null)
            {
                dayNight.m_TimeOfDay = TimeOfDayHours / 24f;
            }
        }

        internal static float ReadTimeHours()
        {
            var dayNight = DayNight;
            if (dayNight != null)
            {
                return dayNight.m_TimeOfDay * 24f;
            }

            return TimeOfDayHours;
        }

        internal static void ApplyPosition(float latitude, float longitude)
        {
            Snapshot();
            var dayNight = DayNight;
            if (dayNight != null)
            {
                dayNight.m_Latitude = Mathf.Clamp(latitude, -90f, 90f);
                dayNight.m_Longitude = Mathf.Clamp(longitude, -180f, 180f);
            }
        }

        internal static void ApplyWeather(float rain, float fog, float cloud)
        {
            RainIntensity = Mathf.Clamp01(rain);
            FogIntensity = Mathf.Clamp01(fog);
            CloudIntensity = Mathf.Clamp01(cloud);
            ApplyWeather();
        }

        private static void ApplyWeather()
        {
            var weather = WeatherManager.instance;
            if (weather == null)
            {
                return;
            }

            if (RainIntensity >= 0f)
            {
                weather.m_targetRain = RainIntensity;
                if (!Mathf.Approximately(weather.m_currentRain, RainIntensity))
                {
                    weather.m_currentRain = Mathf.Lerp(weather.m_currentRain, RainIntensity, 0.2f);
                }
            }

            if (FogIntensity >= 0f)
            {
                weather.m_targetFog = FogIntensity;
                if (!Mathf.Approximately(weather.m_currentFog, FogIntensity))
                {
                    weather.m_currentFog = Mathf.Lerp(weather.m_currentFog, FogIntensity, 0.2f);
                }
            }

            if (CloudIntensity >= 0f)
            {
                weather.m_targetCloud = CloudIntensity;
                if (!Mathf.Approximately(weather.m_currentCloud, CloudIntensity))
                {
                    weather.m_currentCloud = Mathf.Lerp(weather.m_currentCloud, CloudIntensity, 0.2f);
                }
            }
        }

        internal static void Restore()
        {
            if (!_snapshotTaken)
            {
                return;
            }

            TimeLocked = false;

            var dayNight = DayNight;
            if (dayNight != null)
            {
                if (!ThemeOwnership.AtmosphereIsManaged)
                {
                    dayNight.m_Latitude = _vanillaLatitude;
                    dayNight.m_Longitude = _vanillaLongitude;
                }
            }

            var weather = WeatherManager.instance;
            if (weather != null)
            {
                RainIntensity = -1f;
                FogIntensity = -1f;
                CloudIntensity = -1f;
            }
        }
    }
}
