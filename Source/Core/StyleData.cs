using System;
using ColossalFramework.IO;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace SceneFX.Core
{
    /// <summary>
    /// A visual style: a named, complete look combining the color grading LUT,
    /// the filmic tone curve, scene fog and sun exposure. Schema owned by v2.
    /// </summary>
    [XmlRoot(ElementName = "sceneStyle", Namespace = "", IsNullable = false)]
    public class StyleData
    {
        [XmlAttribute("name")]
        public string Name = "Default";

        [XmlElement("lut")] public string Lut = "";              // empty = release the selected LUT
        [XmlElement("gamma")] public float Gamma = 2.2f;          // 1.5..3.5
        [XmlElement("brightness")] public float Brightness = 0f;  // -1..4
        [XmlElement("contrast")] public float Contrast = 0f;      // -1..1
        [XmlElement("sunIntensity")] public float SunIntensity = 1f;  // 0..3, multiplier over current
        [XmlElement("exposure")] public float Exposure = 1f;      // absolute sky exposure, 0 = release
        [XmlElement("warmth")] public float Warmth = 0f;          // -1..1
        [XmlElement("fogDensity")] public float FogDensity = 0f;  // 0..0.005, 0 = keep game value
        [XmlElement("fogStart")] public float FogStart = 0f;      // 0..10000, 0 = keep game value
        [XmlElement("skyTonemap")] public bool SkyTonemap = true;
        [XmlElement("includeWorld")] public bool IncludeWorld;
        [XmlElement("timeOfDay")] public float TimeOfDay = 12f;
        [XmlElement("latitude")] public float Latitude = 36f;
        [XmlElement("longitude")] public float Longitude = 0f;
        [XmlElement("rain")] public float Rain = -1f;
        [XmlElement("fog")] public float Fog = -1f;
        [XmlElement("cloud")] public float Cloud = -1f;

        // Canales de clima que faltaban. Mismo convenio: -1 = lo lleva el juego.
        [XmlElement("northernLights")] public float NorthernLights = -1f;
        [XmlElement("rainbow")] public float Rainbow = -1f;
        [XmlElement("groundWetness")] public float GroundWetness = -1f;

        // Temperatura y viento admiten valores negativos, asi que llevan interruptor aparte.
        [XmlElement("temperatureLock")] public bool TemperatureLock;
        [XmlElement("temperature")] public float Temperature = 15f;
        [XmlElement("windLock")] public bool WindLock;
        [XmlElement("windDirection")] public float WindDirection;

        // Tres estados: -1 sin tocar, 0 apagado, 1 encendido.
        [XmlElement("weatherEnabled")] public int WeatherEnabled = -1;
        [XmlElement("rainIsSnow")] public int RainIsSnow = -1;
        [XmlElement("snowyRoads")] public int SnowyRoads = -1;

        // Ritmo del juego y del ciclo dia/noche.
        [XmlElement("gameSpeed")] public float GameSpeed = 1f;
        [XmlElement("cycleSpeedEnabled")] public bool CycleSpeedEnabled;
        [XmlElement("cycleSpeed")] public float CycleSpeed = 1f;
        [XmlElement("nightCycleSpeed")] public float NightCycleSpeed = 1f;
        [XmlElement("separateDayNight")] public bool SeparateDayNight;
        [XmlElement("cycleWhilePaused")] public bool CycleWhilePaused;

        [XmlElement("timeLocked")] public bool TimeLocked;
        [XmlElement("timeSet")] public bool TimeSet;
        [XmlElement("positionSet")] public bool PositionSet;
        [XmlElement("worldConfigured")] public bool WorldConfigured;
        [XmlElement("lutEnabled")] public int LutEnabled = -1;
        [XmlElement("toneEnabled")] public int ToneEnabled = -1;
        [XmlElement("bloomEnabled")] public int BloomEnabled = -1;
        [XmlElement("rainMotionBlur")] public int RainMotionBlur = -1;

        internal void CopyWorldFrom(StyleData source)
        {
            TimeOfDay = source.TimeOfDay;
            TimeLocked = source.TimeLocked;
            TimeSet = source.TimeSet;
            PositionSet = source.PositionSet;
            Latitude = source.Latitude;
            Longitude = source.Longitude;
            Rain = source.Rain;
            Fog = source.Fog;
            Cloud = source.Cloud;
            NorthernLights = source.NorthernLights;
            Rainbow = source.Rainbow;
            GroundWetness = source.GroundWetness;
            TemperatureLock = source.TemperatureLock;
            Temperature = source.Temperature;
            WindLock = source.WindLock;
            WindDirection = source.WindDirection;
            WeatherEnabled = source.WeatherEnabled;
            RainIsSnow = source.RainIsSnow;
            SnowyRoads = source.SnowyRoads;
            GameSpeed = source.GameSpeed;
            CycleSpeedEnabled = source.CycleSpeedEnabled;
            CycleSpeed = source.CycleSpeed;
            NightCycleSpeed = source.NightCycleSpeed;
            SeparateDayNight = source.SeparateDayNight;
            CycleWhilePaused = source.CycleWhilePaused;
            WorldConfigured = source.WorldConfigured;
        }

        internal void Validate()
        {
            WeatherEnabled = Math.Max(-1, Math.Min(1, WeatherEnabled));
            RainIsSnow = Math.Max(-1, Math.Min(1, RainIsSnow));
            SnowyRoads = Math.Max(-1, Math.Min(1, SnowyRoads));
            LutEnabled = Math.Max(-1, Math.Min(1, LutEnabled));
            ToneEnabled = Math.Max(-1, Math.Min(1, ToneEnabled));
            BloomEnabled = Math.Max(-1, Math.Min(1, BloomEnabled));
            RainMotionBlur = Math.Max(-1, Math.Min(1, RainMotionBlur));
            Gamma = Infrastructure.FxStorage.Clamp(Gamma, 1.5f, 3.5f);
            Brightness = Infrastructure.FxStorage.Clamp(Brightness, -1f, 4f);
            Contrast = Infrastructure.FxStorage.Clamp(Contrast, -1f, 1f);
            SunIntensity = Infrastructure.FxStorage.Clamp(SunIntensity, 0f, 3f);
            Exposure = Infrastructure.FxStorage.Clamp(Exposure, 0f, 5f);
            Warmth = Infrastructure.FxStorage.Clamp(Warmth, -1f, 1f);
            FogDensity = Infrastructure.FxStorage.Clamp(FogDensity, 0f, 0.005f);
            FogStart = Infrastructure.FxStorage.Clamp(FogStart, 0f, 10000f);
            TimeOfDay = Infrastructure.FxStorage.Clamp(TimeOfDay, 0f, 24f);
            Latitude = Infrastructure.FxStorage.Clamp(Latitude, -90f, 90f);
            Longitude = Infrastructure.FxStorage.Clamp(Longitude, -180f, 180f);
            Rain = Infrastructure.FxStorage.Clamp(Rain, -1f, 2.5f);
            Fog = Infrastructure.FxStorage.Clamp(Fog, -1f, 1f);
            Cloud = Infrastructure.FxStorage.Clamp(Cloud, -1f, 1f);
            NorthernLights = Infrastructure.FxStorage.Clamp(NorthernLights, -1f, 1f);
            Rainbow = Infrastructure.FxStorage.Clamp(Rainbow, -1f, 1f);
            GroundWetness = Infrastructure.FxStorage.Clamp(GroundWetness, -1f, 1f);
            Temperature = Infrastructure.FxStorage.Clamp(Temperature, -100f, 100f);
            WindDirection = Infrastructure.FxStorage.Clamp(WindDirection, 0f, 360f);
            GameSpeed = Infrastructure.FxStorage.Clamp(GameSpeed, 0.01f, 5f);
            CycleSpeed = Infrastructure.FxStorage.Clamp(CycleSpeed, 0f, 128f);
            NightCycleSpeed = Infrastructure.FxStorage.Clamp(NightCycleSpeed, 0f, 128f);
        }

        internal StyleData Clone()
        {
            return (StyleData)MemberwiseClone();
        }
    }

    /// <summary>
    /// Own XML persistence for styles and for the last applied look.
    /// </summary>
    internal static class StyleStore
    {
        /// <remarks>
        /// Ruta completa, no relativa: un nombre suelto se resuelve contra el directorio de
        /// trabajo del proceso, que en Cities: Skylines es la carpeta de instalacion del juego.
        /// Ahi acababa este archivo, dentro de Archivos de Programa. Si queda uno en el sitio
        /// antiguo y todavia no hay ninguno en el nuevo, se lee el antiguo.
        /// </remarks>
        private const string StateFile = "SceneFX.xml";

        private static string StatePath
        {
            get { return Path.Combine(DataLocation.localApplicationData, StateFile); }
        }

        private static string StatePathToRead
        {
            get
            {
                return File.Exists(StatePath) || !File.Exists(StateFile) ? StatePath : StateFile;
            }
        }

        internal static string StylesFolder
        {
            get
            {
                return Path.Combine(DataLocation.localApplicationData, "ModConfig/SceneFXStyles");
            }
        }

        internal static string[] ListStyleFiles()
        {
            if (!Directory.Exists(StylesFolder))
            {
                return new string[0];
            }

            return Directory.GetFiles(StylesFolder, "*.scene.xml");
        }

        internal static StyleData LoadStyle(string path)
        {
            if (!File.Exists(path)) return null;
            var style = Infrastructure.FxStorage.ReadXml<StyleData>(path);
            style.Validate();
            return style;
        }

        internal static void SaveStyle(StyleData style)
        {
            if (!Directory.Exists(StylesFolder))
            {
                Directory.CreateDirectory(StylesFolder);
            }

            string path = Path.Combine(StylesFolder, SafeName(style.Name) + ".scene.xml");
            style.Validate();
            Infrastructure.FxStorage.WriteXml(path, style);
        }

        internal static void DeleteStyle(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        internal static string SafeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "style";
            }

            string clean = name.Trim().Replace(' ', '-');
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                clean = clean.Replace(c, '_');
            }

            return clean.Length == 0 ? "style" : clean;
        }

        internal static bool SaveState(StyleData current)
        {
            try
            {
                Infrastructure.FxStorage.WriteXml(StatePath, current);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        internal static StyleData LoadState()
        {
            try
            {
                if (!File.Exists(StatePathToRead))
                {
                    return null;
                }

                return LoadStyle(StatePathToRead);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }
    }
}
