using System;
using System.IO;
using UnityEngine;
using SceneFX.Core;

namespace SceneFX.UI
{
    /// <summary>
    /// v2 style panel (F10): style library, live adjusters and the LUT picker.
    /// </summary>
    internal sealed class StylePanel
    {
        private static readonly string[] Tabs = { "Styles", "Adjust", "LUT", "Suite" };

        private readonly Action _onChanged;

        private Rect _rect = new Rect(SceneRuntime.WindowX, SceneRuntime.WindowY, 480f, 450f);
        private int _tab;
        private Vector2 _scroll;
        private Vector2 _suiteScroll;
        private string _newStyleName = "My scene";
        private string _newSuiteName = "MySuite";
        private bool _includeWorldInStyle;
        private string[] _styleFiles = new string[0];
        private string[] _luts = new string[0];
        private string[] _suiteFiles = new string[0];

        internal StylePanel(Action onChanged)
        {
            _onChanged = onChanged;
            RefreshStyles();
            RefreshSuites();
        }

        internal void RefreshStyles()
        {
            _styleFiles = StyleStore.ListStyleFiles();
        }

        internal void RefreshSuites()
        {
            _suiteFiles = SuiteManager.ListSuiteFiles();
        }

        internal void Draw(int id)
        {
            if (_rect.x != SceneRuntime.WindowX || _rect.y != SceneRuntime.WindowY)
            {
                _rect.x = Mathf.Clamp(SceneRuntime.WindowX, 0f, Mathf.Max(0f, Screen.width - _rect.width));
                _rect.y = Mathf.Clamp(SceneRuntime.WindowY, 0f, Mathf.Max(0f, Screen.height - _rect.height));
            }

            Rect oldRect = _rect;
            _rect = GUI.Window(id, _rect, DrawWindow, "SceneFX");
            if (_rect.x != oldRect.x || _rect.y != oldRect.y)
            {
                _rect.x = Mathf.Clamp(_rect.x, 0f, Mathf.Max(0f, Screen.width - _rect.width));
                _rect.y = Mathf.Clamp(_rect.y, 0f, Mathf.Max(0f, Screen.height - _rect.height));
                SceneRuntime.WindowX = _rect.x;
                SceneRuntime.WindowY = _rect.y;
                SceneRuntime.SaveOptions();
            }
        }

        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0f, 0f, 440f, 22f));
            if (GUI.Button(new Rect(_rect.width - 26f, 4f, 22f, 18f), "x"))
            {
                PanelEngine.CloseLegacy();
            }

            _tab = GUI.Toolbar(new Rect(8f, 26f, _rect.width - 16f, 24f), _tab, Tabs);

            if (_tab == 0)
            {
                DrawStylesTab();
            }
            else if (_tab == 1)
            {
                DrawAdjustTab();
            }
            else if (_tab == 2)
            {
                DrawLutTab();
            }
            else
            {
                DrawSuiteTab();
            }
        }

        private void DrawStylesTab()
        {
            if (GUI.Button(new Rect(8f, 56f, 110f, 24f), "Refresh"))
            {
                RefreshStyles();
            }

            if (GUI.Button(new Rect(124f, 56f, 130f, 24f), "Restore game look"))
            {
                SceneRuntime.RestoreGame();
            }

            float listHeight = 200f;
            _scroll = GUI.BeginScrollView(new Rect(8f, 88f, _rect.width - 16f, listHeight), _scroll,
                new Rect(0f, 0f, _rect.width - 40f, Mathf.Max(1, _styleFiles.Length) * 28f));

            float y = 0f;
            foreach (string file in _styleFiles)
            {
                GUI.Label(new Rect(4f, y + 3f, 250f, 24f), Path.GetFileNameWithoutExtension(file));
                if (GUI.Button(new Rect(260f, y, 60f, 24f), "Apply"))
                {
                    StyleData loaded = StyleStore.LoadStyle(file);
                    if (loaded != null)
                    {
                        SceneRuntime.Current.Lut = loaded.Lut;
                        SceneRuntime.Current.Gamma = loaded.Gamma;
                        SceneRuntime.Current.Brightness = loaded.Brightness;
                        SceneRuntime.Current.Contrast = loaded.Contrast;
                        SceneRuntime.Current.SunIntensity = loaded.SunIntensity;
                        SceneRuntime.Current.Exposure = loaded.Exposure;
                        SceneRuntime.Current.Warmth = loaded.Warmth;
                        SceneRuntime.Current.FogDensity = loaded.FogDensity;
                        SceneRuntime.Current.FogStart = loaded.FogStart;
                        SceneRuntime.Current.SkyTonemap = loaded.SkyTonemap;
                        SceneRuntime.ApplyCurrent();
                        _onChanged();
                    }
                }

                if (GUI.Button(new Rect(326f, y, 60f, 24f), "Del"))
                {
                    StyleStore.DeleteStyle(file);
                    RefreshStyles();
                }

                y += 28f;
            }

            GUI.EndScrollView();

            float baseY = 88f + listHeight + 6f;
            _includeWorldInStyle = GUI.Toggle(new Rect(8f, baseY, 260f, 20f), _includeWorldInStyle, "Include world (time/weather)");
            baseY += 22f;

            _newStyleName = GUI.TextField(new Rect(8f, baseY, _rect.width - 16f, 22f), _newStyleName);
            baseY += 26f;

            if (GUI.Button(new Rect(8f, baseY, _rect.width - 16f, 26f), "Save current look as style"))
            {
                var copy = SceneRuntime.Current.Clone();
                copy.Name = StyleStore.SafeName(_newStyleName);
                copy.IncludeWorld = _includeWorldInStyle;
                if (copy.IncludeWorld)
                {
                    copy.TimeOfDay = WorldController.ReadTimeHours();
                    var dn = UnityEngine.Object.FindObjectOfType<DayNightProperties>();
                    copy.Latitude = dn != null ? dn.m_Latitude : 36f;
                    copy.Longitude = dn != null ? dn.m_Longitude : 0f;
                    var wm = WeatherManager.instance;
                    copy.Rain = wm != null ? wm.m_currentRain : 0f;
                    copy.Fog = wm != null ? wm.m_currentFog : 0f;
                    copy.Cloud = wm != null ? wm.m_currentCloud : 0f;
                }

                StyleStore.SaveStyle(copy);
                RefreshStyles();
            }
        }

        private void DrawAdjustTab()
        {
            var style = SceneRuntime.Current;
            float y = 58f;

            style.Gamma = Slider("Gamma", style.Gamma, 1.2f, 3f, 0.05f, y); y += 30f;
            style.Brightness = Slider("Brightness", style.Brightness, -1f, 1f, 0.05f, y); y += 30f;
            style.Contrast = Slider("Contrast", style.Contrast, -1f, 1f, 0.05f, y); y += 30f;
            style.SunIntensity = Slider("Sun gain", style.SunIntensity, 0f, 3f, 0.05f, y); y += 30f;
            style.Exposure = Slider("Exposure", style.Exposure, 0.5f, 1.5f, 0.02f, y); y += 30f;
            style.Warmth = Slider("Warmth", style.Warmth, -1f, 1f, 0.05f, y); y += 30f;
            style.FogDensity = Slider("Fog density", style.FogDensity, 0f, 0.005f, 0.00005f, y); y += 30f;
            style.FogStart = Slider("Fog start", style.FogStart, 0f, 10000f, 25f, y); y += 30f;
            style.SkyTonemap = GUI.Toggle(new Rect(10f, y, 300f, 24f), style.SkyTonemap, "Sky tonemapping");

            if (GUI.Button(new Rect(10f, y + 30f, 200f, 24f), "Apply and store"))
            {
                SceneRuntime.ApplyCurrent();
                _onChanged();
            }
        }

        private void MergeLutList()
        {
            var gameLuts = StyleEngine.ListLuts();
            var compat = LutCompat.Names();
            var native = NativeLut.Names();
            _luts = new string[gameLuts.Length + compat.Count + native.Count];
            gameLuts.CopyTo(_luts, 0);
            int i = gameLuts.Length;
            foreach (string name in native)
            {
                _luts[i++] = name + " (native)";
            }
            foreach (string name in compat)
            {
                _luts[i++] = name + " (compat)";
            }
        }

        private void DrawLutTab()
        {
            if (GUI.Button(new Rect(8f, 56f, 100f, 24f), "Scan LUTs"))
            {
                LutCompat.ScanFolders();
                MergeLutList();
            }

            if (GUI.Button(new Rect(112f, 56f, 170f, 24f), "Bake Look to LUT (32³)"))
            {
                string pngPath;
                string bakeName = "Baked_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                Texture3D baked = BakeToLut.Bake(SceneRuntime.Current, bakeName, out pngPath);
                if (baked != null)
                {
                    SceneRuntime.Current.Lut = bakeName;
                    SceneRuntime.ApplyCurrent();
                    _onChanged();
                    MergeLutList();
                }
            }

            if (_luts.Length == 0)
            {
                MergeLutList();
            }

            float listHeight = 260f;
            _scroll = GUI.BeginScrollView(new Rect(8f, 88f, _rect.width - 16f, listHeight), _scroll,
                new Rect(0f, 0f, _rect.width - 40f, Mathf.Max(1, _luts.Length) * 28f));

            float y = 0f;
            foreach (string lut in _luts)
            {
                GUI.Label(new Rect(4f, y + 3f, 270f, 24f), lut);
                if (GUI.Button(new Rect(280f, y, 90f, 24f), "Use"))
                {
                    string clean = lut.Replace(" (compat)", string.Empty).Replace(" (native)", string.Empty);
                    SceneRuntime.Current.Lut = clean;
                    SceneRuntime.ApplyCurrent();
                    _onChanged();
                }

                y += 28f;
            }

            GUI.EndScrollView();
        }

        private void DrawSuiteTab()
        {
            float y = 56f;
            GUI.Label(new Rect(8f, y, 200f, 24f), "<b><color=#4FC3F7>Suite Profiles (All 4 Mods)</color></b>");

            if (GUI.Button(new Rect(280f, y, 90f, 24f), "Refresh"))
            {
                RefreshSuites();
            }

            if (GUI.Button(new Rect(376f, y, 90f, 24f), "Optimized"))
            {
                SuiteManager.ApplySuiteProfile("Optimized");
                _onChanged();
            }

            y += 30f;
            float listHeight = 190f;
            _suiteScroll = GUI.BeginScrollView(new Rect(8f, y, _rect.width - 16f, listHeight), _suiteScroll,
                new Rect(0f, 0f, _rect.width - 40f, Mathf.Max(1, _suiteFiles.Length) * 28f));

            float sy = 0f;
            foreach (string file in _suiteFiles)
            {
                string pName = Path.GetFileNameWithoutExtension(file);
                GUI.Label(new Rect(4f, sy + 3f, 260f, 24f), pName);
                if (GUI.Button(new Rect(280f, sy, 80f, 24f), "Apply"))
                {
                    SuiteManager.ApplySuiteProfile(file);
                    _onChanged();
                }

                sy += 28f;
            }

            GUI.EndScrollView();

            float baseY = y + listHeight + 10f;
            _newSuiteName = GUI.TextField(new Rect(8f, baseY, _rect.width - 16f, 24f), _newSuiteName);
            baseY += 30f;

            if (GUI.Button(new Rect(8f, baseY, 220f, 26f), "Export current look as suite"))
            {
                SuiteManager.SaveSuiteProfile(_newSuiteName);
                RefreshSuites();
            }

            if (GUI.Button(new Rect(236f, baseY, _rect.width - 244f, 26f), "Open suite folder"))
            {
                if (!Directory.Exists(SuiteManager.SuiteFolder))
                {
                    Directory.CreateDirectory(SuiteManager.SuiteFolder);
                }
                Application.OpenURL("file://" + SuiteManager.SuiteFolder);
            }
        }

        /// <summary>
        /// Un deslizador que solo cambia el valor cuando el usuario lo mueve.
        /// </summary>
        /// <remarks>
        /// Redondear al paso mas cercano y devolverlo siempre reescribia los valores cargados
        /// de un preset, que casi nunca caen justo en un multiplo del paso. IMGUI ya avisa de
        /// si hubo interaccion con <c>GUI.changed</c>; sin ella, el deslizador solo dibuja.
        /// </remarks>
        private float Slider(string label, float value, float min, float max, float step, float y)
        {
            GUI.Label(new Rect(10f, y, 110f, 24f), label);

            bool changedBefore = GUI.changed;
            GUI.changed = false;
            float raw = GUI.HorizontalSlider(new Rect(125f, y + 3f, 240f, 22f), value, min, max);
            bool moved = GUI.changed;
            GUI.changed = changedBefore || moved;

            if (!moved)
            {
                GUI.Label(new Rect(375f, y, 90f, 24f), value.ToString("0.00#####"));
                return value;
            }

            float snapped = Mathf.Round(raw / step) * step;
            GUI.Label(new Rect(375f, y, 90f, 24f), snapped.ToString("0.00#####"));
            return snapped;
        }

        private static float Section(string title, float y)
        {
            GUI.Label(new Rect(8f, y, 300f, 24f), "<b><color=#4FC3F7>" + title + "</color></b>");
            return y + 26f;
        }
    }
}
