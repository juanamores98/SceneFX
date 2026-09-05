using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using UnityEngine;
using ColossalFramework.Globalization;

namespace SceneFX.Locale
{
    [XmlRoot(ElementName = "Language", Namespace = "", IsNullable = false)]
    public class LanguageFile
    {
        [XmlAttribute("UniqueName")]
        public string UniqueName = "";

        [XmlAttribute("ReadableName")]
        public string ReadableName = "";

        [XmlArray("Translations", IsNullable = false)]
        [XmlArrayItem("Translation", IsNullable = false)]
        public TranslationEntry[] Entries = new TranslationEntry[0];
    }

    public class TranslationEntry
    {
        [XmlAttribute("ID")]
        public string Id = "";

        [XmlAttribute("String")]
        public string Text = "";
    }

    /// <summary>
    /// Reads the Locale folder shipped next to the assembly and serves
    /// strings for the language selected in the game (English fallback).
    /// </summary>
    internal static class Translator
    {
        private const string FallbackLanguage = "en";

        private static readonly Dictionary<string, string> Strings = new Dictionary<string, string>();
        private static bool _loaded;
        private static bool _hooked;

        internal static string Get(string id)
        {
            EnsureLoaded();

            string text;
            return Strings.TryGetValue(id, out text) ? text : id;
        }

        private static void EnsureLoaded()
        {
            if (!_hooked)
            {
                _hooked = true;
                LocaleManager.eventLocaleChanged += LoadAll;
            }

            if (!_loaded)
            {
                _loaded = true;
                LoadAll();
            }
        }

        private static void LoadAll()
        {
            Strings.Clear();

            var languages = new List<LanguageFile>();
            string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string localePath = Path.Combine(basePath ?? "", "Locale");

            if (!Directory.Exists(localePath))
            {
                return;
            }

            var serializer = new XmlSerializer(typeof(LanguageFile));
            foreach (string file in Directory.GetFiles(localePath, "*.xml"))
            {
                try
                {
                    using (var reader = new StreamReader(file))
                    {
                        if (serializer.Deserialize(reader) is LanguageFile language)
                        {
                            languages.Add(language);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[SceneFX] could not read locale file " + file + ": " + e.Message);
                }
            }

            string selected = null;
            if (LocaleManager.exists)
            {
                selected = LocaleManager.instance.language;
            }

            var current = languages.Find(l => l.UniqueName == selected)
                ?? languages.Find(l => l.UniqueName == FallbackLanguage);
            if (current == null)
            {
                return;
            }

            foreach (TranslationEntry entry in current.Entries)
            {
                Strings[entry.Id] = entry.Text;
            }
        }
    }
}
