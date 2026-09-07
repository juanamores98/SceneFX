using ColossalFramework.IO;
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

        internal static float WindowX = 200f;
        internal static float WindowY = 500f;

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
                if (options.WindowX > 0f) WindowX = options.WindowX;
                if (options.WindowY > 0f) WindowY = options.WindowY;
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
            OptionsStore.Save(new OptionsDocument
            {
                ApplyOnLoad = ApplyOnLoad,
                Borderless = Borderless,
                VanillaMode = VanillaMode,
                WindowX = WindowX,
                WindowY = WindowY
            });
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

        [XmlElement("windowX")]
        public float WindowX = 200f;

        [XmlElement("windowY")]
        public float WindowY = 500f;
    }

    internal static class OptionsStore
    {
                /// <remarks>
        /// <b>Ruta completa, no relativa.</b> Un nombre suelto lo resuelve .NET contra el
        /// directorio de trabajo del proceso, que en Cities: Skylines es la carpeta de
        /// instalacion del juego. Ahi acababan estos XML: dentro de Archivos de Programa, donde
        /// escribir suele requerir permisos y donde una verificacion de Steam puede borrarlos.
        /// Se midio en partida —los cuatro archivos aparecieron en la carpeta del juego— y solo
        /// LumenFX lo hacia bien.
        ///
        /// <b>La migracion.</b> Si queda un archivo en el sitio antiguo y todavia no hay uno en
        /// el nuevo, se lee el antiguo: nadie pierde su configuracion por arreglar esto.
        /// </remarks>
        private const string FileName = "SceneFXOptions.xml";

        private static string OptionsPath
        {
            get { return System.IO.Path.Combine(DataLocation.localApplicationData, "SceneFXOptions.xml"); }
        }

        /// <summary>El sitio antiguo: la carpeta de trabajo del proceso.</summary>
        private static string OptionsPathLegacy
        {
            get { return "SceneFXOptions.xml"; }
        }

        /// <summary>De donde leer: el sitio nuevo si existe, y si no el antiguo.</summary>
        private static string OptionsPathToRead
        {
            get
            {
                return System.IO.File.Exists(OptionsPath) || !System.IO.File.Exists(OptionsPathLegacy)
                    ? OptionsPath
                    : OptionsPathLegacy;
            }
        }

        internal static OptionsDocument Load()
        {
            try
            {
                if (!System.IO.File.Exists(OptionsPathToRead))
                {
                    return null;
                }

                using (var reader = new System.IO.StreamReader(OptionsPathToRead))
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
                using (var writer = new System.IO.StreamWriter(OptionsPath))
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
