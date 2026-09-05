using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ColossalFramework.UI;
using ICities;
using SceneFX.Core;

namespace SceneFX.UI
{
    /// <summary>
    /// Native in-game panel (ColossalFramework UI) built with the game's own
    /// UIHelper widgets: style switching, LUT picking, live grading and the
    /// world controls. Toggled with F10.
    /// </summary>
    internal sealed class NativePanel
    {
        private UIPanel _root;
        private UIHelperBase _styleGroup;
        private UIDropDown _styleDropDown;
        private UIDropDown _lutDropDown;
        private UITextField _nameField;
        private UISlider _timeSlider;
        private UICheckBox _lockBox;
        private bool _building;
        private bool _suppressEvents;

        internal bool IsVisible
        {
            get { return _root != null && _root.isVisible; }
        }

        internal void Toggle()
        {
            if (_root == null)
            {
                Build();
            }

            if (_root != null)
            {
                _root.isVisible = !_root.isVisible;
                if (_root.isVisible)
                {
                    RefreshDropdowns();
                }
            }
        }

        internal void Hide()
        {
            if (_root != null)
            {
                _root.isVisible = false;
            }
        }

        private void Build()
        {
            if (_building)
            {
                return;
            }

            _building = true;
            try
            {
                var view = UIView.GetAView();
                _root = view.AddUIComponent(typeof(UIPanel)) as UIPanel;
                _root.backgroundSprite = "MenuPanel";
                _root.size = new Vector2(400f, 620f);
                _root.relativePosition = new Vector3(120f, 100f);
                _root.opacity = 0.95f;

                var drag = _root.AddUIComponent<UIDragHandle>();
                drag.size = new Vector2(340f, 40f);
                drag.target = _root;

                var title = _root.AddUIComponent<UILabel>();
                title.text = "SceneFX";
                title.textScale = 1.2f;
                title.relativePosition = new Vector3(12f, 10f);

                var close = _root.AddUIComponent<UIButton>();
                close.text = "X";
                close.size = new Vector2(28f, 24f);
                close.relativePosition = new Vector3(362f, 8f);
                close.normalBgSprite = "ButtonMenu";
                close.hoveredBgSprite = "ButtonMenuHovered";
                close.eventClicked += (c, p) => Hide();

                var content = _root.AddUIComponent<UIPanel>();
                content.backgroundSprite = null;
                content.size = new Vector2(392f, 570f);
                content.relativePosition = new Vector3(4f, 44f);
                content.autoLayout = true;
                content.autoLayoutDirection = LayoutDirection.Vertical;
                content.autoLayoutPadding = new RectOffset(4, 4, 4, 4);

                var helper = new UIHelper(content);

                // ---- Style group ----
                _styleGroup = helper.AddGroup("Style");
                string[] styleNames = ListStyleNames();
                _styleDropDown = (UIDropDown)_styleGroup.AddDropdown("Style", styleNames, 0, sel =>
                {
                    if (_suppressEvents)
                    {
                        return;
                    }

                    ApplyStyleByName(styleNames[sel]);
                });

                _nameField = (UITextField)_styleGroup.AddTextfield("Style name", "My scene", sel => { });
                _styleGroup.AddButton("Save current look as style", () =>
                {
                    var copy = SceneRuntime.Current.Clone();
                    copy.Name = StyleStore.SafeName(_nameField.text);
                    StyleStore.SaveStyle(copy);
                    RefreshStyleDropdown();
                });
                _styleGroup.AddButton("Restore game look", () =>
                {
                    SceneRuntime.RestoreGame();
                    WorldController.Restore();
                });

                // ---- LUT group ----
                var lutGroup = helper.AddGroup("Color grading LUT");
                string[] lutNames = ListLutNames();
                _lutDropDown = (UIDropDown)lutGroup.AddDropdown("LUT", lutNames, 0, sel =>
                {
                    if (_suppressEvents)
                    {
                        return;
                    }

                    SceneRuntime.Current.Lut = lutNames[sel];
                    SceneRuntime.ApplyCurrent();
                });
                lutGroup.AddCheckbox("Sky tonemapping", SceneRuntime.Current.SkyTonemap, sel =>
                {
                    SceneRuntime.Current.SkyTonemap = sel;
                    SceneRuntime.ApplyCurrent();
                });

                // ---- Grade group ----
                var grade = helper.AddGroup("Live grade");
                grade.AddSlider("Gamma", 1.2f, 3f, 0.05f, SceneRuntime.Current.Gamma, v =>
                {
                    SceneRuntime.Current.Gamma = v;
                    SceneRuntime.ApplyCurrent();
                });
                grade.AddSlider("Brightness", -1f, 1f, 0.05f, SceneRuntime.Current.Brightness, v =>
                {
                    SceneRuntime.Current.Brightness = v;
                    SceneRuntime.ApplyCurrent();
                });
                grade.AddSlider("Contrast", -1f, 1f, 0.05f, SceneRuntime.Current.Contrast, v =>
                {
                    SceneRuntime.Current.Contrast = v;
                    SceneRuntime.ApplyCurrent();
                });
                grade.AddSlider("Sun gain", 0f, 3f, 0.05f, SceneRuntime.Current.SunIntensity, v =>
                {
                    SceneRuntime.Current.SunIntensity = v;
                    SceneRuntime.ApplyCurrent();
                });
                grade.AddSlider("Exposure", 0.5f, 1.5f, 0.02f, SceneRuntime.Current.Exposure, v =>
                {
                    SceneRuntime.Current.Exposure = v;
                    SceneRuntime.ApplyCurrent();
                });
                grade.AddSlider("Warmth", -1f, 1f, 0.05f, SceneRuntime.Current.Warmth, v =>
                {
                    SceneRuntime.Current.Warmth = v;
                    SceneRuntime.ApplyCurrent();
                });

                // ---- World group ----
                var world = helper.AddGroup("World");
                _lockBox = (UICheckBox)world.AddCheckbox("Lock time of day", WorldController.TimeLocked, sel =>
                {
                    WorldController.TimeLocked = sel;
                });

                _timeSlider = (UISlider)world.AddSlider("Time of day", 0f, 24f, 0.25f, WorldController.TimeOfDayHours, v =>
                {
                    WorldController.ApplyTime(v);
                });

                world.AddSlider("Latitude", -90f, 90f, 0.5f, WorldLat(), v =>
                {
                    WorldController.ApplyPosition(v, WorldLon());
                });
                world.AddSlider("Longitude", -180f, 180f, 0.5f, WorldLon(), v =>
                {
                    WorldController.ApplyPosition(WorldLat(), v);
                });
                world.AddSlider("Rain", 0f, 1f, 0.02f, WorldRain(), v =>
                {
                    WorldController.ApplyWeather(v, WorldFog(), WorldCloud());
                });
                world.AddSlider("Fog", 0f, 1f, 0.02f, WorldFog(), v =>
                {
                    WorldController.ApplyWeather(WorldRain(), v, WorldCloud());
                });
                world.AddSlider("Cloud", 0f, 1f, 0.02f, WorldCloud(), v =>
                {
                    WorldController.ApplyWeather(WorldRain(), WorldFog(), v);
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (_root != null)
                {
                    UnityEngine.Object.Destroy(_root);
                    _root = null;
                }
            }
            finally
            {
                _building = false;
            }
        }

        private static float WorldLat()
        {
            var dn = UnityEngine.Object.FindObjectOfType<DayNightProperties>();
            return dn != null ? dn.m_Latitude : 36f;
        }

        private static float WorldLon()
        {
            var dn = UnityEngine.Object.FindObjectOfType<DayNightProperties>();
            return dn != null ? dn.m_Longitude : 0f;
        }

        private static float WorldRain()
        {
            var w = WeatherManager.instance;
            return w != null ? w.m_currentRain : 0f;
        }

        private static float WorldFog()
        {
            var w = WeatherManager.instance;
            return w != null ? w.m_currentFog : 0f;
        }

        private static float WorldCloud()
        {
            var w = WeatherManager.instance;
            return w != null ? w.m_currentCloud : 0f;
        }

        private static string[] ListStyleNames()
        {
            var names = new List<string>();
            foreach (string file in StyleStore.ListStyleFiles())
            {
                names.Add(Path.GetFileNameWithoutExtension(file).Replace("-", " "));
            }

            if (names.Count == 0)
            {
                names.Add("Vanilla");
            }

            return names.ToArray();
        }

        private static void ApplyStyleByName(string displayName)
        {
            string fileName = displayName.Replace(" ", "-");
            string path = Path.Combine(StyleStore.StylesFolder, StyleStore.SafeName(fileName) + ".scene.xml");
            StyleData loaded = StyleStore.LoadStyle(path);
            if (loaded == null)
            {
                // Tolerate naming differences: try the display name verbatim.
                loaded = StyleStore.LoadStyle(Path.Combine(StyleStore.StylesFolder, displayName + ".scene.xml"));
            }

            if (loaded == null)
            {
                return;
            }

            var current = SceneRuntime.Current;
            current.Lut = loaded.Lut;
            current.NativeLut = loaded.NativeLut;
            current.Gamma = loaded.Gamma;
            current.Brightness = loaded.Brightness;
            current.Contrast = loaded.Contrast;
            current.SunIntensity = loaded.SunIntensity;
            current.Exposure = loaded.Exposure;
            current.Warmth = loaded.Warmth;
            current.FogDensity = loaded.FogDensity;
            current.FogStart = loaded.FogStart;
            current.SkyTonemap = loaded.SkyTonemap;
            SceneRuntime.ApplyCurrent();
        }

        private static string[] ListLutNames()
        {
            var names = new List<string> { "(keep current)" };
            foreach (string name in StyleEngine.ListLuts())
            {
                names.Add(name);
            }

            foreach (string name in NativeLut.Names())
            {
                if (!names.Contains(name))
                {
                    names.Add(name);
                }
            }

            foreach (string name in LutCompat.Names())
            {
                string entry = name + "  (compat)";
                if (!names.Contains(entry))
                {
                    names.Add(entry);
                }
            }

            return names.ToArray();
        }

        private void RefreshStyleDropdown()
        {
            if (_styleDropDown == null)
            {
                return;
            }

            _suppressEvents = true;
            try
            {
                string[] names = ListStyleNames();
                _styleDropDown.items = names;
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void RefreshDropdowns()
        {
            RefreshStyleDropdown();
            if (_lutDropDown != null)
            {
                _suppressEvents = true;
                try
                {
                    _lutDropDown.items = ListLutNames();
                }
                finally
                {
                    _suppressEvents = false;
                }
            }
        }
    }
}
