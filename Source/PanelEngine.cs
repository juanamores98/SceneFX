using System;
using System.IO;
using UnityEngine;
using SceneFX.Core;
using SceneFX.UI;

namespace SceneFX
{
    /// <summary>
    /// Scene host: hotkey (F10) and the style panel lifecycle.
    /// </summary>
    public class PanelEngine : MonoBehaviour
    {
        private static bool _open;

        private StylePanel _panel;
        private int _windowId;

        internal static void Close()
        {
            _open = false;
        }

        private void Start()
        {
            _windowId = GetInstanceID();
            EnsureBuiltInStyles();
            SceneRuntime.LoadPersisted();
            _panel = new StylePanel(OnSceneChanged);

            if (SceneRuntime.ApplyOnLoad)
            {
                SceneRuntime.ApplyCurrent();
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

        private static void OnSceneChanged()
        {
            SceneRuntime.SaveOptions();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                _open = !_open;
            }
        }

        private void OnGUI()
        {
            if (_open)
            {
                _panel.Draw(_windowId);
            }
        }
    }
}
