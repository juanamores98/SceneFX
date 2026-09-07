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

        [XmlElement("lut")] public string Lut = "";              // empty = leave current
        [XmlElement("nativeLut")] public string NativeLut = "";  // fallback: procedurally generated table owned by the mod
        [XmlElement("gamma")] public float Gamma = 2.2f;          // 1.2..3.0
        [XmlElement("brightness")] public float Brightness = 0f;  // -1..1
        [XmlElement("contrast")] public float Contrast = 0f;      // -1..1
        [XmlElement("sunIntensity")] public float SunIntensity = 1f;  // 0..3, multiplier over current
        [XmlElement("exposure")] public float Exposure = 1f;      // 0.5..1.5
        [XmlElement("warmth")] public float Warmth = 0f;          // -1..1
        [XmlElement("fogDensity")] public float FogDensity = 0f;  // 0..0.005, 0 = keep game value
        [XmlElement("fogStart")] public float FogStart = 0f;      // 0..10000, 0 = keep game value
        [XmlElement("skyTonemap")] public bool SkyTonemap = true;
        [XmlElement("skyMood")] public int SkyMood = 0; // 0 = keep game sky, 1..N = procedural palette
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
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Path.Combine("Colossal Order", Path.Combine("Cities_Skylines", "ModConfig\\SceneFXStyles")));
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
            var serializer = new XmlSerializer(typeof(StyleData));
            using (var reader = new StreamReader(path))
            {
                return serializer.Deserialize(reader) as StyleData;
            }
        }

        internal static void SaveStyle(StyleData style)
        {
            if (!Directory.Exists(StylesFolder))
            {
                Directory.CreateDirectory(StylesFolder);
            }

            string path = Path.Combine(StylesFolder, SafeName(style.Name) + ".scene.xml");
            using (var writer = new StreamWriter(path))
            {
                new XmlSerializer(typeof(StyleData)).Serialize(writer, style);
            }
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

        internal static void SaveState(StyleData current)
        {
            try
            {
                using (var writer = new StreamWriter(StatePath))
                {
                    new XmlSerializer(typeof(StyleData)).Serialize(writer, current);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
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

                using (var reader = new StreamReader(StatePathToRead))
                {
                    return new XmlSerializer(typeof(StyleData)).Deserialize(reader) as StyleData;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }
    }
}
