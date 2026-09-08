using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SceneFX.Infrastructure
{
    internal static class FxStorage
    {
        internal static string LastError = string.Empty;
        private static XmlElement _optimized;

        internal static bool MatchesOptimized(string xml, Type module)
        {
            if (_optimized == null)
            {
                using (var stream = module.Assembly.GetManifestResourceStream(module.Namespace + ".BuiltIns.Optimized.xml"))
                {
                    if (stream == null) return false;
                    var preset = new XmlDocument(); preset.Load(stream); _optimized = preset.DocumentElement;
                }
            }
            var current = new XmlDocument(); current.LoadXml(xml);
            foreach (XmlNode wanted in _optimized.ChildNodes)
            {
                if (wanted.NodeType != XmlNodeType.Element) continue;
                string value = null;
                foreach (XmlNode field in current.DocumentElement.ChildNodes)
                    if (field.Name.Equals(wanted.Name, StringComparison.OrdinalIgnoreCase)) value = field.InnerText;
                if (value == null) return false;
                float a, b;
                var culture = System.Globalization.CultureInfo.InvariantCulture;
                if (float.TryParse(value, System.Globalization.NumberStyles.Float, culture, out a)
                    && float.TryParse(wanted.InnerText, System.Globalization.NumberStyles.Float, culture, out b))
                { if (a != b) return false; }
                else if (!value.Equals(wanted.InnerText, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }

        internal static void WriteXml(string path, object document)
        {
            using (var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture))
            {
                new XmlSerializer(document.GetType()).Serialize(writer, document);
                WriteText(path, writer.ToString());
            }
        }

        internal static void WriteText(string path, string text)
        {
            string fullPath = Path.GetFullPath(path);
            string temporary = fullPath + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                // StringWriter emits an UTF-16 declaration; use the matching encoding.
                File.WriteAllText(temporary, text, text.Contains("encoding=\"utf-16\"")
                    ? System.Text.Encoding.Unicode : new System.Text.UTF8Encoding(false));
                if (File.Exists(fullPath)) File.Replace(temporary, fullPath, fullPath + ".bak");
                else File.Move(temporary, fullPath);
                LastError = string.Empty;
            }
            catch (Exception e)
            {
                LastError = "Could not save settings: " + e.Message;
                throw;
            }
        }

        internal static T ReadXml<T>(string path)
        {
            var settings = new XmlReaderSettings { ProhibitDtd = true, XmlResolver = null };
            using (var reader = XmlReader.Create(path, settings))
                return (T)new XmlSerializer(typeof(T)).Deserialize(reader);
        }

        internal static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException("value", "A finite number is required.");
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
