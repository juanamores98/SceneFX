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
    /// Native in-game panel (ColossalFramework UI) with tabbed pages so every
    /// control stays inside the window: Style, LUT, Grade and World.
    /// Toggled with F10 or the Unified UI tray button.
    /// </summary>
    internal sealed class NativePanel
    {
        private static readonly string[] Tabs = { "Style", "LUT", "Grade", "World" };

        private UIPanel _root;
        private readonly UIPanel[] _pages = new UIPanel[Tabs.Length];
        private readonly UIButton[] _tabButtons = new UIButton[Tabs.Length];
        private int _activeTab = -1;

        private UIDropDown _styleDropDown;
        private UIDropDown _lutDropDown;
        private UITextField _nameField;
        private bool _building;
        private bool _suppressEvents;

        internal bool IsVisible
        {
            get { return _root != null && _root.isVisible; }
        }

        internal void Show()
        {
            if (_root == null)
            {
                Build();
            }

            if (_root != null)
            {
                _root.isVisible = true;
                RefreshDropdowns();
            }
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
                _root.size = new Vector2(400f, 640f);
                _root.relativePosition = new Vector3(760f, 100f);
                _root.opacity = 0.95f;

                var drag = _root.AddUIComponent<UIDragHandle>();
                drag.size = new Vector2(340f, 40f);
                drag.target = _root;

                var title = _root.AddUIComponent<UILabel>();
                title.text = "SceneFX";
                title.textScale = 1.2f;
                title.relativePosition = new Vector3(12f, 8f);

                var close = _root.AddUIComponent<UIButton>();
                close.text = "X";
                close.size = new Vector2(28f, 24f);
                close.relativePosition = new Vector3(362f, 8f);
                close.normalBgSprite = "ButtonMenu";
                close.hoveredBgSprite = "ButtonMenuHovered";
                close.eventClicked += (c, p) => Hide();

                // Tab buttons.
                float x = 8f;
                for (int i = 0; i < Tabs.Length; i++)
                {
                    int index = i;
                    var tab = _root.AddUIComponent<UIButton>();
                    tab.text = Tabs[i];
                    tab.size = new Vector2(93f, 26f);
                    tab.relativePosition = new Vector3(x, 38f);
                    tab.normalBgSprite = "ButtonMenu";
                    tab.hoveredBgSprite = "ButtonMenuHovered";
                    tab.focusedBgSprite = "ButtonMenuFocused";
                    tab.textColor = new Color32(255, 255, 255, 255);
                    tab.eventClicked += (c, p) => SelectTab(index);
                    _tabButtons[i] = tab;
                    x += 96f;
                }

                // One page per tab; only the active one is visible.
                _pages[0] = NewPage();
                BuildStylePage(_pages[0]);
                _pages[1] = NewPage();
                BuildLutPage(_pages[1]);
                _pages[2] = NewPage();
                BuildGradePage(_pages[2]);
                _pages[3] = NewPage();
                BuildWorldPage(_pages[3]);

                SelectTab(0);
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

        private UIPanel NewPage()
        {
            var page = _root.AddUIComponent<UIPanel>();
            page.backgroundSprite = null;
            page.size = new Vector2(392f, 556f);
            page.relativePosition = new Vector3(4f, 74f);
            page.isVisible = false;
            return page;
        }

        private void SelectTab(int index)
        {
            _activeTab = index;
            for (int i = 0; i < _pages.Length; i++)
            {
                if (_pages[i] != null)
                {
                    _pages[i].isVisible = i == index;
                }
            }

            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] != null)
                {
                    _tabButtons[i].normalBgSprite = i == index ? "ButtonMenuFocused" : "ButtonMenu";
                }
            }
        }

        private void BuildStylePage(UIPanel page)
        {
            var helper = new UIHelper(page);

            var group = helper.AddGroup("Style");
            string[] styleNames = ListStyleNames();
            _styleDropDown = (UIDropDown)group.AddDropdown("Style", styleNames, 0, sel =>
            {
                if (_suppressEvents)
                {
                    return;
                }

                ApplyStyleByName(styleNames[sel]);
            });

            _nameField = (UITextField)group.AddTextfield("Style name", "My scene", sel => { });
            group.AddButton("Save current look as style", () =>
            {
                var copy = SceneRuntime.Current.Clone();
                copy.Name = StyleStore.SafeName(_nameField.text);
                StyleStore.SaveStyle(copy);
                RefreshStyleDropdown();
            });
            group.AddButton("Restore game look", () =>
            {
                SceneRuntime.RestoreGame();
                WorldController.Restore();
            });
        }

        private void BuildLutPage(UIPanel page)
        {
            var helper = new UIHelper(page);

            var group = helper.AddGroup("Color grading LUT");
            string[] lutNames = ListLutNames();
            _lutDropDown = (UIDropDown)group.AddDropdown("LUT", lutNames, 0, sel =>
            {
                if (_suppressEvents)
                {
                    return;
                }

                SceneRuntime.Current.Lut = lutNames[sel].Replace("  (compat)", string.Empty);
                SceneRuntime.ApplyCurrent();
            });
            group.AddCheckbox("Sky tonemapping", SceneRuntime.Current.SkyTonemap, sel =>
            {
                SceneRuntime.Current.SkyTonemap = sel;
                SceneRuntime.ApplyCurrent();
            });
        }

        private void BuildGradePage(UIPanel page)
        {
            var helper = new UIHelper(page);

            var group = helper.AddGroup("Live grade");
            group.AddSlider("Gamma", 1.2f, 3f, 0.05f, SceneRuntime.Current.Gamma, v =>
            {
                SceneRuntime.Current.Gamma = v;
                SceneRuntime.ApplyCurrent();
            });
            group.AddSlider("Brightness", -1f, 1f, 0.05f, SceneRuntime.Current.Brightness, v =>
            {
                SceneRuntime.Current.Brightness = v;
                SceneRuntime.ApplyCurrent();
            });
            group.AddSlider("Contrast", -1f, 1f, 0.05f, SceneRuntime.Current.Contrast, v =>
            {
                SceneRuntime.Current.Contrast = v;
                SceneRuntime.ApplyCurrent();
            });
            group.AddSlider("Sun gain", 0f, 3f, 0.05f, SceneRuntime.Current.SunIntensity, v =>
            {
                SceneRuntime.Current.SunIntensity = v;
                SceneRuntime.ApplyCurrent();
            });
            group.AddSlider("Exposure", 0.5f, 1.5f, 0.02f, SceneRuntime.Current.Exposure, v =>
            {
                SceneRuntime.Current.Exposure = v;
                SceneRuntime.ApplyCurrent();
            });
            group.AddSlider("Warmth", -1f, 1f, 0.05f, SceneRuntime.Current.Warmth, v =>
            {
                SceneRuntime.Current.Warmth = v;
                SceneRuntime.ApplyCurrent();
            });
        }

        private void BuildWorldPage(UIPanel page)
        {
            var helper = new UIHelper(page);

            var group = helper.AddGroup("World");
            group.AddCheckbox("Lock time of day", WorldController.TimeLocked, sel =>
            {
                WorldController.TimeLocked = sel;
            });

            group.AddSlider("Time of day", 0f, 24f, 0.25f, WorldController.TimeOfDayHours, v =>
            {
                WorldController.ApplyTime(v);
            });

            group.AddSlider("Latitude", -90f, 90f, 0.5f, WorldLat(), v =>
            {
                WorldController.ApplyPosition(v, WorldLon());
            });
            group.AddSlider("Longitude", -180f, 180f, 0.5f, WorldLon(), v =>
            {
                WorldController.ApplyPosition(WorldLat(), v);
            });
            group.AddSlider("Rain", 0f, 1f, 0.02f, WorldRain(), v =>
            {
                WorldController.ApplyWeather(v, WorldFog(), WorldCloud());
            });
            group.AddSlider("Fog", 0f, 1f, 0.02f, WorldFog(), v =>
            {
                WorldController.ApplyWeather(WorldRain(), v, WorldCloud());
            });
            group.AddSlider("Cloud", 0f, 1f, 0.02f, WorldCloud(), v =>
            {
                WorldController.ApplyWeather(WorldRain(), WorldFog(), v);
            });
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
                    string[] names = ListLutNames();
                    _lutDropDown.items = names;

                    // Mirror the current style's selection when it is listed.
                    string current = SceneRuntime.Current.Lut;
                    if (!string.IsNullOrEmpty(current))
                    {
                        for (int i = 0; i < names.Length; i++)
                        {
                            if (names[i] == current || names[i].EndsWith("." + current, StringComparison.Ordinal))
                            {
                                _lutDropDown.selectedIndex = i;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    _suppressEvents = false;
                }
            }
        }
    }
}
