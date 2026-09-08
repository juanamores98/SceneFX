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
        private static bool _dirty;
        internal static bool Active;
        internal static StyleData Current { get { return _current; } }
        internal static bool ApplyOnLoad = true;
        internal static bool Borderless;
        internal static bool VanillaMode = true;
        internal static float WindowX = 200f;
        internal static float WindowY = 60f;

        internal static void LoadPersisted()
        {
            var state = StyleStore.LoadState();
            if (state != null) _current = state;
            var options = OptionsStore.Load();
            if (options == null) return;
            ApplyOnLoad = options.ApplyOnLoad;
            Borderless = options.Borderless;
            VanillaMode = options.VanillaMode;
            WindowX = options.WindowX;
            WindowY = options.WindowY;
        }

        internal static void ApplyCurrent()
        {
            if (VanillaMode) return;
            _current.Validate();
            Active = true;
            StyleEngine.Apply(_current);
            QueueSave();
        }

        internal static void ApplyOnLevel()
        {
            if (!ApplyOnLoad || VanillaMode) return;
            _current.Validate();
            Active = true;
            // Companion settings loaded from their own files remain authoritative.
            StyleEngine.Apply(_current, false);
            if (_current.WorldConfigured || _current.IncludeWorld) StyleEngine.ApplyWorld(_current);
        }

        internal static void LoadStyle(StyleData style)
        {
            var next = style.Clone();
            next.Validate();
            if (!next.IncludeWorld) next.CopyWorldFrom(_current);
            _current = next;
            VanillaMode = false;
            ApplyCurrent();
            if (next.IncludeWorld)
            {
                next.WorldConfigured = true;
                StyleEngine.ApplyWorld(next);
            }
            SaveOptions();
        }

        internal static void ReplaceState(StyleData state, bool vanilla, bool applyWorld)
        {
            state.Validate();
            _current = state;
            VanillaMode = vanilla;
            if (vanilla) RestoreGame();
            else
            {
                ApplyCurrent();
                if (applyWorld)
                {
                    _current.WorldConfigured = true;
                    StyleEngine.ApplyWorld(_current);
                }
            }
            QueueSave();
            SaveOptions();
        }

        internal static void WorldChanged()
        {
            VanillaMode = false;
            Active = true;
            _current.WorldConfigured = true;
            StyleEngine.CaptureWorld(_current);
            QueueSave();
            SaveOptions();
        }

        internal static void QueueSave()
        {
            _dirty = true;
            CheckPendingSave();
        }

        internal static void CheckPendingSave()
        {
            if (_dirty && Time.realtimeSinceStartup - _lastStateSave >= 1f) Flush();
            OptionsStore.CheckPendingSave();
        }

        internal static void Flush()
        {
            _lastStateSave = Time.realtimeSinceStartup;
            _dirty = !StyleStore.SaveState(_current);
            OptionsStore.Flush();
        }

        internal static void RestoreGame()
        {
            StyleEngine.RestoreGame();
            WorldController.Restore();
            TimeController.Restore();
            Active = false;
        }

        internal static void SaveOptions()
        {
            OptionsStore.Save(new OptionsDocument { ApplyOnLoad = ApplyOnLoad,
                Borderless = Borderless, VanillaMode = VanillaMode, WindowX = WindowX, WindowY = WindowY });
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
        private static OptionsDocument _pending;
        private static OptionsDocument _written;
        private static float _lastSave = -10f;
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

                return Infrastructure.FxStorage.ReadXml<OptionsDocument>(OptionsPathToRead);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        internal static void Save(OptionsDocument document)
        {
            _pending = document;
            CheckPendingSave();
        }

        internal static void CheckPendingSave()
        {
            if (_pending != null && Time.realtimeSinceStartup - _lastSave >= 1f) Flush();
        }

        internal static void Flush()
        {
            if (_pending == null) return;
            if (_written != null && _pending.ApplyOnLoad == _written.ApplyOnLoad
                && _pending.Borderless == _written.Borderless && _pending.VanillaMode == _written.VanillaMode
                && _pending.WindowX == _written.WindowX && _pending.WindowY == _written.WindowY)
            { _pending = null; return; }
            _lastSave = Time.realtimeSinceStartup;
            try
            {
                Infrastructure.FxStorage.WriteXml(OptionsPath, _pending);
                _written = _pending;
                _pending = null;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
