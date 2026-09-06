using System;
using System.Xml.Serialization;
using UnityEngine;

namespace SceneFX.Core
{
    /// <summary>
    /// Process-wide style state: the look being edited/applied and the
    /// apply-on-load behavior.
    /// </summary>
    public static class SceneRuntime
    {
        private static StyleData _current = new StyleData();
        private static float _lastStateSave = -10f;

        internal static StyleData Current
        {
            get { return _current; }
        }

        internal static bool ApplyOnLoad = true;

        internal static bool Borderless;

        internal static bool VanillaMode; // suspend everything, game untouched

        internal static void LoadPersisted()
        {
            var state = StyleStore.LoadState();
            if (state != null)
            {
                _current = state;
            }

            OptionsDocument options = OptionsStore.Load();
            if (options != null)
            {
                ApplyOnLoad = options.ApplyOnLoad;
                Borderless = options.Borderless;
                VanillaMode = options.VanillaMode;
            }
        }

        /// <summary>
        /// Applies the current style live; the state document is persisted at
        /// most once per second so slider drags stay cheap.
        /// </summary>
        internal static void ApplyCurrent()
        {
            if (VanillaMode)
            {
                // Suspended: apply nothing, keep the game untouched.
                return;
            }

            StyleEngine.Apply(_current);
            if (Time.realtimeSinceStartup - _lastStateSave > 1f)
            {
                _lastStateSave = Time.realtimeSinceStartup;
                StyleStore.SaveState(_current);
            }
        }

        internal static void RestoreGame()
        {
            StyleEngine.RestoreGame();
        }

        internal static void SaveOptions()
        {
            OptionsStore.Save(new OptionsDocument { ApplyOnLoad = ApplyOnLoad, Borderless = Borderless, VanillaMode = VanillaMode });
        }
    }

    [XmlRoot(ElementName = "sceneFxOptions", Namespace = "", IsNullable = false)]
    public class OptionsDocument
    {
        [XmlAttribute("schema")]
        public int Schema = 2;

        [XmlElement("applyOnLoad")]
        public bool ApplyOnLoad = true;

        [XmlElement("borderless")]
        public bool Borderless;

        [XmlElement("vanillaMode")]
        public bool VanillaMode;
    }

    internal static class OptionsStore
    {
        private const string FileName = "SceneFXOptions.xml";

        internal static OptionsDocument Load()
        {
            try
            {
                if (!System.IO.File.Exists(FileName))
                {
                    return null;
                }

                using (var reader = new System.IO.StreamReader(FileName))
                {
                    return new XmlSerializer(typeof(OptionsDocument)).Deserialize(reader) as OptionsDocument;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        internal static void Save(OptionsDocument document)
        {
            try
            {
                using (var writer = new System.IO.StreamWriter(FileName))
                {
                    new XmlSerializer(typeof(OptionsDocument)).Serialize(writer, document);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
