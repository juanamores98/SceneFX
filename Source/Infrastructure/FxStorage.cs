using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SceneFX.Infrastructure
{
    internal static class FxStorage
    {
        internal static string LastError = string.Empty;
        private static readonly System.Collections.Generic.Dictionary<Type, XmlElement> Optimized
            = new System.Collections.Generic.Dictionary<Type, XmlElement>();

        internal static string LastNote = string.Empty;

        /// <summary>
        /// Que campos del preset no quedaron como pedia, o null si quedaron todos.
        /// </summary>
        /// <remarks>
        /// <b>Para que.</b> Un preset puede aplicarse sin error y aun asi no verse: otro mod
        /// prioritario administra ese campo, el juego lo reescribe cada fotograma, o el valor
        /// se recorta al llegar. Sin esto, lo unico que ve quien pulsa es que no pasa nada, y
        /// lo natural es volver a pulsar. Con esto, el panel dice cual es el campo.
        ///
        /// Se compara contra el estado exportado, que es lo que el mod cree tener, no contra
        /// el juego: si el mod cree tenerlo y no se ve, el problema esta fuera de aqui y el
        /// dueño real de ese campo es otro.
        /// </remarks>
        internal static string OptimizedGap(string xml, Type module)
        {
            try
            {
                if (!LoadOptimized(module)) return null;
                var current = new XmlDocument(); current.LoadXml(xml);
                var differ = new System.Collections.Generic.List<string>();
                foreach (XmlNode wanted in Optimized[module].ChildNodes)
                {
                    if (wanted.NodeType != XmlNodeType.Element) continue;
                    string value = null;
                    foreach (XmlNode field in current.DocumentElement.ChildNodes)
                        if (field.Name.Equals(wanted.Name, StringComparison.OrdinalIgnoreCase)) value = field.InnerText;
                    if (value == null) { differ.Add(wanted.Name + " (not published)"); continue; }
                    if (!SameValue(value, wanted.InnerText)) differ.Add(wanted.Name + " = " + value + ", asked " + wanted.InnerText);
                }

                if (differ.Count == 0) return null;
                if (differ.Count > 3) return string.Join("; ", differ.GetRange(0, 3).ToArray()) + " and " + (differ.Count - 3) + " more";
                return string.Join("; ", differ.ToArray());
            }
            catch (Exception failure)
            {
                return "could not be checked: " + failure.Message;
            }
        }

        private static bool LoadOptimized(Type module)
        {
            XmlElement cached;
            if (Optimized.TryGetValue(module, out cached)) return cached != null;
            using (var stream = module.Assembly.GetManifestResourceStream(module.Namespace + ".BuiltIns.Optimized.xml"))
            {
                var preset = stream == null ? null : new XmlDocument();
                if (preset != null) preset.Load(stream);
                Optimized[module] = cached = preset == null ? null : preset.DocumentElement;
                return cached != null;
            }
        }

        private static bool SameValue(string a, string b)
        {
            float x, y;
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            if (float.TryParse(a, System.Globalization.NumberStyles.Float, culture, out x)
                && float.TryParse(b, System.Globalization.NumberStyles.Float, culture, out y)) return x == y;
            return a.Equals(b, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool MatchesOptimized(string xml, Type module)
        {
            if (!LoadOptimized(module)) return false;
            var current = new XmlDocument(); current.LoadXml(xml);
            foreach (XmlNode wanted in Optimized[module].ChildNodes)
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
            if (FxTransaction.Stage(path, text)) { LastError = string.Empty; return; }
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
