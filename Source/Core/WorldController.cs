using System;
using UnityEngine;

namespace SceneFX.Core
{
    /// <summary>
    /// World controls built on the game's own public fields:
    /// time of day, sun coordinates and weather intensities. A snapshot of
    /// the untouched values allows full restoration.
    /// </summary>
    /// <remarks>
    /// <b>El convenio de los canales del clima.</b> Un valor negativo quiere decir «esto lo
    /// lleva el juego»: no se escribe nada y el clima sigue su curso. Cualquier valor de 0 en
    /// adelante lo fija este mod fotograma a fotograma. Por eso el interruptor de la interfaz
    /// no es un campo aparte: soltar un canal es ponerlo en -1.
    ///
    /// <b>Sobre la nieve.</b> Cities: Skylines no tiene intensidad de nieve. En un mapa de
    /// invierno la nieve <i>es</i> la lluvia; lo que decide cual de las dos cae es
    /// <c>WeatherProperties.m_rainIsSnow</c>. Asi que el control de nieve es el de lluvia mas
    /// ese interruptor, y no un canal propio que no existe.
    /// </remarks>
    internal static class WorldController
    {
        private static bool _snapshotTaken;
        private static float _vanillaLatitude;
        private static float _vanillaTime;
        private static float _vanillaLongitude;
        private static bool _vanillaWeatherEnabled;
        private static bool _vanillaRainIsSnow;
        private static bool _vanillaSnowyRoads;
        private static bool _weatherWritten, _snowWritten, _roadsWritten;
        private static DayNightProperties _dayNight;
        private static int _frameCounter;

        internal static void ClearCache()
        {
            _dayNight = null;
            _snapshotTaken = false;
            TimeSet = PositionSet = false;
            _weatherWritten = _snowWritten = _roadsWritten = false;
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
        internal static bool TimeSet;
        internal static bool PositionSet;
        internal static float TimeOfDayHours = 12f;   // 0..24

        // Canales del clima: -1 = lo lleva el juego.
        internal static float RainIntensity = -1f;
        internal static float FogIntensity = -1f;
        internal static float CloudIntensity = -1f;
        internal static float NorthernLights = -1f;
        internal static float Rainbow = -1f;
        internal static float GroundWetness = -1f;

        // Temperatura y viento admiten valores negativos, asi que llevan su propio interruptor.
        internal static bool TemperatureLocked;
        internal static float Temperature = 15f;      // grados
        internal static bool WindLocked;
        internal static float WindDirection;          // 0..360

        // Interruptores de tres estados: -1 = sin tocar, 0 = apagado, 1 = encendido.
        internal static int WeatherEnabled = -1;
        internal static int RainIsSnow = -1;
        internal static int SnowyRoads = -1;

        internal static void Snapshot()
        {
            if (_snapshotTaken)
            {
                return;
            }

            var dayNight = DayNight;
            if (dayNight != null)
            {
                _vanillaTime = dayNight.m_TimeOfDay;
                _vanillaLatitude = dayNight.m_Latitude;
                _vanillaLongitude = dayNight.m_Longitude;
            }

            _snapshotTaken = dayNight != null;
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
                dayNight.m_TimeOfDay = Mathf.Repeat(TimeOfDayHours, 24f);
            }

            ApplyWeather();
        }

        internal static void ApplyTime(float hours)
        {
            Snapshot();
            TimeSet = true;
            TimeOfDayHours = Mathf.Repeat(Infrastructure.FxStorage.Clamp(hours, 0f, 24f), 24f);
            var dayNight = DayNight;
            if (dayNight != null)
            {
                dayNight.m_TimeOfDay = TimeOfDayHours;
            }
        }

        internal static float ReadTimeHours()
        {
            var dayNight = DayNight;
            if (dayNight != null)
            {
                return dayNight.m_TimeOfDay;
            }

            return TimeOfDayHours;
        }

        internal static void ApplyPosition(float latitude, float longitude)
        {
            Snapshot();
            PositionSet = true;
            var dayNight = DayNight;
            if (dayNight != null)
            {
                dayNight.m_Latitude = Mathf.Clamp(latitude, -90f, 90f);
                dayNight.m_Longitude = Mathf.Clamp(longitude, -180f, 180f);
            }
        }

        internal static void ApplyWeather(float rain, float fog, float cloud)
        {
            RainIntensity = rain < 0f ? -1f : Mathf.Clamp(rain, 0f, 2.5f);
            FogIntensity = fog < 0f ? -1f : Mathf.Clamp01(fog);
            CloudIntensity = cloud < 0f ? -1f : Mathf.Clamp01(cloud);
            ApplyWeather();
        }

        /// <summary>Fija o suelta un canal del clima por nombre. Un valor negativo lo suelta.</summary>
        internal static void SetChannel(string channel, float value)
        {
            float v = value < 0f ? -1f : Infrastructure.FxStorage.Clamp(value, 0f, channel == "rain" ? 2.5f : 1f);
            switch (channel)
            {
                case "rain": RainIntensity = v; break;
                case "fog": FogIntensity = v; break;
                case "cloud": CloudIntensity = v; break;
                case "northernLights": NorthernLights = v; break;
                case "rainbow": Rainbow = v; break;
                case "wetness": GroundWetness = v; break;
            }

            ApplyWeather();
        }

        internal static float ReadChannel(string channel)
        {
            var w = WeatherManager.instance;
            if (w == null)
            {
                return 0f;
            }

            switch (channel)
            {
                case "rain": return w.m_currentRain;
                case "fog": return w.m_currentFog;
                case "cloud": return w.m_currentCloud;
                case "northernLights": return w.m_currentNorthernLights;
                case "rainbow": return w.m_currentRainbow;
                case "wetness": return w.m_groundWetness;
                default: return 0f;
            }
        }

        internal static bool ChannelLocked(string channel)
        {
            switch (channel)
            {
                case "rain": return RainIntensity >= 0f;
                case "fog": return FogIntensity >= 0f;
                case "cloud": return CloudIntensity >= 0f;
                case "northernLights": return NorthernLights >= 0f;
                case "rainbow": return Rainbow >= 0f;
                case "wetness": return GroundWetness >= 0f;
                default: return false;
            }
        }

        private static void ApplyWeather()
        {
            var weather = WeatherManager.instance;
            if (weather == null)
            {
                return;
            }

            Drive(ref weather.m_currentRain, ref weather.m_targetRain, RainIntensity);
            Drive(ref weather.m_currentFog, ref weather.m_targetFog, FogIntensity);
            Drive(ref weather.m_currentCloud, ref weather.m_targetCloud, CloudIntensity);
            Drive(ref weather.m_currentNorthernLights, ref weather.m_targetNorthernLights, NorthernLights);
            Drive(ref weather.m_currentRainbow, ref weather.m_targetRainbow, Rainbow);

            if (GroundWetness >= 0f)
            {
                // La humedad del suelo no tiene objetivo al que tender: es un valor directo.
                weather.m_groundWetness = GroundWetness;
            }

            if (TemperatureLocked)
            {
                weather.m_targetTemperature = Temperature;
                weather.m_currentTemperature = Mathf.Lerp(weather.m_currentTemperature, Temperature, 0.2f);
            }

            if (WindLocked)
            {
                float dir = Mathf.Repeat(WindDirection, 360f);
                weather.m_targetDirection = dir;
                weather.m_windDirection = dir;
            }

            ApplyFlags(weather);
        }

        private static void Drive(ref float current, ref float target, float wanted)
        {
            if (wanted < 0f)
            {
                return;
            }

            target = wanted;
            if (!Mathf.Approximately(current, wanted))
            {
                current = Mathf.Lerp(current, wanted, 0.2f);
            }
        }

        private static void ApplyFlags(WeatherManager weather)
        {
            if (WeatherEnabled >= 0)
            {
                if (!_weatherWritten) _vanillaWeatherEnabled = weather.m_enableWeather;
                _weatherWritten = true;
                weather.m_enableWeather = WeatherEnabled == 1;
            }
            else if (_weatherWritten)
            {
                weather.m_enableWeather = _vanillaWeatherEnabled;
                _weatherWritten = false;
            }

            if (RainIsSnow >= 0 && weather.m_properties != null)
            {
                if (!_snowWritten) _vanillaRainIsSnow = weather.m_properties.m_rainIsSnow;
                _snowWritten = true;
                weather.m_properties.m_rainIsSnow = RainIsSnow == 1;
            }
            else if (_snowWritten && weather.m_properties != null)
            {
                weather.m_properties.m_rainIsSnow = _vanillaRainIsSnow;
                _snowWritten = false;
            }

            if (SnowyRoads >= 0)
            {
                // Solo la bandera con la que las calles deciden si el mojado se dibuja como
                // nieve. No toca ni la red ni sus datos: es como se pintan, no lo que son.
                var net = NetManager.instance;
                if (net != null)
                {
                    if (!_roadsWritten) _vanillaSnowyRoads = net.m_treatWetAsSnow;
                    _roadsWritten = true;
                    net.m_treatWetAsSnow = SnowyRoads == 1;
                }
            }
            else if (_roadsWritten && NetManager.instance != null)
            {
                NetManager.instance.m_treatWetAsSnow = _vanillaSnowyRoads;
                _roadsWritten = false;
            }
        }

        internal static void Restore()
        {
            TimeLocked = false;

            if (_snapshotTaken)
            {
                var dayNight = DayNight;
                if (dayNight != null && !ThemeOwnership.AtmosphereIsManaged)
                {
                    if (PositionSet) { dayNight.m_Latitude = _vanillaLatitude; dayNight.m_Longitude = _vanillaLongitude; }
                    if (TimeSet) dayNight.m_TimeOfDay = _vanillaTime;
                }
            }

            RainIntensity = -1f;
            FogIntensity = -1f;
            CloudIntensity = -1f;
            NorthernLights = -1f;
            Rainbow = -1f;
            GroundWetness = -1f;
            TemperatureLocked = false;
            WindLocked = false;

            RestoreFlags();
            TimeSet = PositionSet = false;
            _snapshotTaken = false;
        }

        private static void RestoreFlags()
        {
            if (!_weatherWritten && !_snowWritten && !_roadsWritten)
            {
                WeatherEnabled = -1;
                RainIsSnow = -1;
                SnowyRoads = -1;
                return;
            }

            try
            {
                var weather = WeatherManager.instance;
                if (weather != null)
                {
                    if (_weatherWritten) weather.m_enableWeather = _vanillaWeatherEnabled;
                    if (_snowWritten && weather.m_properties != null)
                    {
                        weather.m_properties.m_rainIsSnow = _vanillaRainIsSnow;
                    }
                }

                var net = NetManager.instance;
                if (_roadsWritten && net != null)
                {
                    net.m_treatWetAsSnow = _vanillaSnowyRoads;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            WeatherEnabled = -1;
            RainIsSnow = -1;
            SnowyRoads = -1;
            _weatherWritten = _snowWritten = _roadsWritten = false;
        }
    }
}
