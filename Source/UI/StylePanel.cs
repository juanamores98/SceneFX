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
        private static readonly string[] Tabs = { "Styles", "Adjust", "LUT" };

        private readonly Action _onChanged;

        private Rect _rect = new Rect(200f, 500f, 480f, 430f);
        private int _tab;
        private Vector2 _scroll;
        private string _newStyleName = "My scene";
        private string[] _styleFiles = new string[0];
        private string[] _luts = new string[0];

        internal StylePanel(Action onChanged)
        {
            _onChanged = onChanged;
            RefreshStyles();
        }

        internal void RefreshStyles()
        {
            _styleFiles = StyleStore.ListStyleFiles();
        }

        internal void Draw(int id)
        {
            _rect = GUI.Window(id, _rect, DrawWindow, "SceneFX");
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
            else
            {
                DrawLutTab();
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

            float baseY = 88f + listHeight + 10f;
            _newStyleName = GUI.TextField(new Rect(8f, baseY, _rect.width - 16f, 24f), _newStyleName);
            baseY += 30f;

            if (GUI.Button(new Rect(8f, baseY, _rect.width - 16f, 26f), "Save current look as style"))
            {
                var copy = SceneRuntime.Current.Clone();
                copy.Name = StyleStore.SafeName(_newStyleName);
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
            _luts = new string[gameLuts.Length + compat.Count];
            gameLuts.CopyTo(_luts, 0);
            int i = gameLuts.Length;
            foreach (string name in compat)
            {
                _luts[i++] = name + "  (compat)";
            }
        }

        private void DrawLutTab()
        {
            if (GUI.Button(new Rect(8f, 56f, 110f, 24f), "Scan LUTs"))
            {
                LutCompat.ScanFolders();
                MergeLutList();
            }

            if (GUI.Button(new Rect(124f, 56f, 130f, 24f), "Scan folders"))
            {
                LutCompat.ScanFolders();
                MergeLutList();
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
                    SceneRuntime.Current.Lut = lut.Replace("  (compat)", string.Empty);
                    SceneRuntime.ApplyCurrent();
                    _onChanged();
                }

                y += 28f;
            }

            GUI.EndScrollView();
        }

        private float Slider(string label, float value, float min, float max, float step, float y)
        {
            GUI.Label(new Rect(10f, y, 110f, 24f), label);
            float raw = GUI.HorizontalSlider(new Rect(125f, y + 3f, 240f, 22f), value, min, max);
            float snapped = Mathf.Round(raw / step) * step;
            GUI.Label(new Rect(375f, y, 90f, 24f), snapped.ToString("0.00#####"));
            return snapped;
        }
    }
}
