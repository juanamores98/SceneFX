using System;
using System.IO;
using UnityEngine;
using SceneFX.Core;
using SceneFX.UI;

namespace SceneFX
{
    /// <summary>
    /// Scene host: native panel (F10), legacy IMGUI window (F11) and the
    /// world tick.
    /// </summary>
    public class PanelEngine : MonoBehaviour
    {
        private static bool _legacyOpen;
        private static NativePanel _native;

        private StylePanel _panel;
        private int _windowId;

        internal static void CloseLegacy()
        {
            _legacyOpen = false;
        }

        /// <summary>
        /// Opens the native panel from the Unified UI tray button (or any
        /// external caller).
        /// </summary>
        internal static void OpenFromTray()
        {
            try
            {
                if (_native == null)
                {
                    _native = new NativePanel();
                }

                _native.Show();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _legacyOpen = true;
            }
        }

        private void Start()
        {
            _windowId = GetInstanceID();
            EnsureBuiltInStyles();
            SuiteManager.EnsureBuiltInSuites();
            SceneRuntime.LoadPersisted();
            _panel = new StylePanel(OnSceneChanged);

            if (SceneRuntime.ApplyOnLoad && !SceneRuntime.VanillaMode)
            {
                SceneRuntime.ApplyCurrent();
            }
        }

        private void OnDestroy()
        {
            SceneRuntime.SaveOptions();
            StyleStore.SaveState(SceneRuntime.Current);
            StyleEngine.ClearCache();
            WorldController.ClearCache();
            TimeController.Restore();
            TimeController.ClearCache();
        }

        private static void OnSceneChanged()
        {
            SceneRuntime.SaveOptions();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                ToggleNative();
            }

            if (Input.GetKeyDown(KeyCode.F11))
            {
                _legacyOpen = !_legacyOpen;
            }

            WorldController.Tick();
            TimeController.Tick();
        }

        private void ToggleNative()
        {
            try
            {
                if (_native == null)
                {
                    _native = new NativePanel();
                }

                _native.Toggle();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _legacyOpen = true; // fall back to the IMGUI window
            }
        }

        private void OnGUI()
        {
            if (_legacyOpen)
            {
                _panel.Draw(_windowId);
            }
        }

        /// <summary>
        /// Writes the styles shipped with the mod into the styles folder the
        /// first time (or if the user deleted them).
        /// </summary>
        private static void EnsureBuiltInStyles()
        {
            string[] builtins =
            {
                "Vanilla",
                "Optimized",
                "Optimized Nocturne",
                "Optimized Sepia",
                "Optimized Cine",
                "Optimized Frost",
                "Optimized Ember",
                "Optimized Relight2Alpine",
                "Optimized Relight2Lush",
                "Optimized Relight2Natural",
                "Optimized Relight2North",
                "Optimized RelightAverage",
                "Optimized RelightBleak",
                "Optimized RelightCool",
                "Optimized RelightFilm",
                "Optimized RelightNeutral",
                "Optimized RelightVintage",
                "Optimized RelightWarm",
            };

            foreach (string name in builtins)
            {
                try
                {
                    string path = Path.Combine(StyleStore.StylesFolder, StyleStore.SafeName(name) + ".scene.xml");
                    if (File.Exists(path))
                    {
                        continue;
                    }

                    Directory.CreateDirectory(StyleStore.StylesFolder);
                    using (var stream = System.Reflection.Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("SceneFX.BuiltIns." + StyleStore.SafeName(name) + ".scene.xml"))
                    {
                        if (stream == null)
                        {
                            continue;
                        }

                        using (var reader = new StreamReader(stream))
                        {
                            File.WriteAllText(path, reader.ReadToEnd());
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
