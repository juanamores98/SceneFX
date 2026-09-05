using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace SceneFX.Core
{
    /// <summary>
    /// A visual style: a named, complete look combining the color grading LUT,
    /// the filmic tone curve, scene fog and sun exposure. Schema owned by v2.
    /// </summary>
    public class StyleData
    {
        [XmlAttribute("name")]
        public string Name = "Default";

        [XmlElement("lut")] public string Lut = "";              // empty = leave current
        [XmlElement("gamma")] public float Gamma = 2.2f;          // 1.2..3.0
        [XmlElement("brightness")] public float Brightness = 0f;  // -1..1
        [XmlElement("contrast")] public float Contrast = 0f;      // -1..1
        [XmlElement("sunIntensity")] public float SunIntensity = 1f;  // 0..3, multiplier over current
        [XmlElement("exposure")] public float Exposure = 1f;      // 0.5..1.5
        [XmlElement("warmth")] public float Warmth = 0f;          // -1..1
        [XmlElement("fogDensity")] public float FogDensity = 0f;  // 0..0.005, 0 = keep game value
        [XmlElement("fogStart")] public float FogStart = 0f;      // 0..10000, 0 = keep game value
        [XmlElement("skyTonemap")] public bool SkyTonemap = true;

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
        private const string StateFile = "SceneFX.xml";

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

            string clean = name.Trim();
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
                using (var writer = new StreamWriter(StateFile))
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
                if (!File.Exists(StateFile))
                {
                    return null;
                }

                using (var reader = new StreamReader(StateFile))
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
