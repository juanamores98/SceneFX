using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ColossalFramework.UI;
using ICities;
using SceneFX.Core;
using SceneFX.Locale;

namespace SceneFX.UI
{
    /// <summary>
    /// Native in-game panel (ColossalFramework UI) with tabbed pages so every
    /// control stays inside the window: Style, LUT, Grade, World, Weather and Time.
    /// Toggled with F10 or the Unified UI tray button. Localized via the
    /// Locale folder (English fallback).
    /// </summary>
    internal sealed class NativePanel
    {
        private static readonly string[] Tabs =
        {
            Translator.Get("SCX_TAB_STYLE"),
            Translator.Get("SCX_TAB_LUT"),
            Translator.Get("SCX_TAB_GRADE"),
            Translator.Get("SCX_TAB_WORLD"),
            Translator.Get("SCX_TAB_WEATHER"),
            Translator.Get("SCX_TAB_TIME"),
        };

        private UIPanel _root;
        private readonly UIPanel[] _pages = new UIPanel[Tabs.Length];
        private readonly UIButton[] _tabButtons = new UIButton[Tabs.Length];
        private int _activeTab = -1;

        private UIDropDown _styleDropDown;
        private UIDropDown _lutDropDown;
        private UIDropDown _suiteDropDown;
        private UITextField _nameField;
        private UITextField _suiteNameField;
        private string _activeStyleName = "Default";
        private bool _includeWorldInSavedStyle;
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
                _root.size = new Vector2(460f, 760f);
                _root.relativePosition = new Vector3(700f, 80f);
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
                close.relativePosition = new Vector3(422f, 8f);
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
                    tab.size = new Vector2(71f, 26f);
                    tab.relativePosition = new Vector3(x, 38f);
                    tab.textScale = 0.8f;
                    tab.normalBgSprite = "ButtonMenu";
                    tab.hoveredBgSprite = "ButtonMenuHovered";
                    tab.focusedBgSprite = "ButtonMenuFocused";
                    tab.textColor = new Color32(255, 255, 255, 255);
                    tab.eventClicked += (c, p) => SelectTab(index);
                    _tabButtons[i] = tab;
                    x += 74f;
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
                _pages[4] = NewPage();
                BuildWeatherPage(_pages[4]);
                _pages[5] = NewPage();
                BuildTimePage(_pages[5]);

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
            page.size = new Vector2(452f, 676f);
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

            var group = helper.AddGroup(Translator.Get("SCX_GROUP_STYLE"));
            string[] styleNames = ListStyleNames();
            _styleDropDown = (UIDropDown)group.AddDropdown(Translator.Get("SCX_STYLE"), styleNames, 0, sel =>
            {
                if (_suppressEvents)
                {
                    return;
                }

                _activeStyleName = styleNames[sel];
                ApplyStyleByName(_activeStyleName);
            });

            _nameField = (UITextField)group.AddTextfield(Translator.Get("SCX_STYLE_NAME"), "My scene", sel => { });

            group.AddCheckbox("Include world (time, sun, weather)", _includeWorldInSavedStyle, sel =>
            {
                _includeWorldInSavedStyle = sel;
            });

            group.AddButton(Translator.Get("SCX_SAVE_STYLE"), () =>
            {
                var copy = SceneRuntime.Current.Clone();
                copy.Name = StyleStore.SafeName(_nameField.text);
                copy.IncludeWorld = _includeWorldInSavedStyle;
                if (copy.IncludeWorld)
                {
                    StyleEngine.CaptureWorld(copy);
                }

                StyleStore.SaveStyle(copy);
                RefreshStyleDropdown();
            });

            group.AddButton(Translator.Get("SCX_RESTORE_GAME"), () =>
            {
                SceneRuntime.RestoreGame();
                WorldController.Restore();
                TimeController.Restore();
            });

            group.AddCheckbox(Translator.Get("SCX_VANILLA"), SceneRuntime.VanillaMode, sel =>
            {
                SceneRuntime.VanillaMode = sel;
                SceneRuntime.SaveOptions();
                if (sel)
                {
                    SceneRuntime.RestoreGame();
                    WorldController.Restore();
                    TimeController.Restore();
                    Core.SkyMood.Restore();
                }
                else
                {
                    SceneRuntime.ApplyCurrent();
                }
            });

            var suiteGroup = helper.AddGroup("Suite Profile (SceneFX + Suite)");
            string[] suiteNames = SuiteManager.ListSuiteNames();
            _suiteDropDown = (UIDropDown)suiteGroup.AddDropdown("Profile", suiteNames, 0, sel => { });
            suiteGroup.AddButton("Apply Suite Profile", () =>
            {
                if (_suiteDropDown != null && _suiteDropDown.selectedIndex >= 0 && _suiteDropDown.selectedIndex < _suiteDropDown.items.Length)
                {
                    string selProfile = _suiteDropDown.items[_suiteDropDown.selectedIndex];
                    SuiteManager.ApplySuiteProfile(selProfile);
                    RefreshDropdowns();
                }
            });

            _suiteNameField = (UITextField)suiteGroup.AddTextfield("Suite name", "MySuite", sel => { });
            suiteGroup.AddButton("Export Suite Profile", () =>
            {
                string sName = _suiteNameField != null ? _suiteNameField.text : "MySuite";
                SuiteManager.SaveSuiteProfile(sName);
                RefreshSuiteDropdown();
            });
        }

        private void BuildLutPage(UIPanel page)
        {
            var helper = new UIHelper(page);

            var group = helper.AddGroup(Translator.Get("SCX_GROUP_LUT"));
            string[] lutNames = ListLutNames();
            _lutDropDown = (UIDropDown)group.AddDropdown(Translator.Get("SCX_LUT"), lutNames, 0, sel =>
            {
                if (_suppressEvents)
                {
                    return;
                }

                SceneRuntime.Current.Lut = lutNames[sel].Replace("  (compat)", string.Empty);
                SceneRuntime.ApplyCurrent();
            });

            group.AddButton("Bake Look to LUT (32³ + PNG)", () =>
            {
                string pngPath;
                string bakeName = "Baked_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                Texture3D baked = BakeToLut.Bake(SceneRuntime.Current, bakeName, out pngPath);
                if (baked != null)
                {
                    SceneRuntime.Current.Lut = bakeName;
                    RefreshDropdowns();
                    SceneRuntime.ApplyCurrent();
                }
            });

            group.AddCheckbox(Translator.Get("SCX_SKY_TONEMAP"), SceneRuntime.Current.SkyTonemap, sel =>
            {
                SceneRuntime.Current.SkyTonemap = sel;
                SceneRuntime.ApplyCurrent();
            });
        }

        private void BuildGradePage(UIPanel page)
        {
            var helper = new UIHelper(page);

            var group = helper.AddGroup(Translator.Get("SCX_GROUP_GRADE"));
            group.AddSlider(Translator.Get("SCX_GAMMA"), 1.2f, 3f, 0.05f, SceneRuntime.Current.Gamma, v =>
            {
                SceneRuntime.Current.Gamma = v;
                SceneRuntime.ApplyCurrent();
            });
            group.AddSlider(Translator.Get("SCX_BRIGHTNESS"), -1f, 1f, 0.05f, SceneRuntime.Current.Brightness, v =>
            {
                SceneRuntime.Current.Brightness = v;
                SceneRuntime.ApplyCurrent();
            });
            group.AddSlider(Translator.Get("SCX_CONTRAST"), -1f, 1f, 0.05f, SceneRuntime.Current.Contrast, v =>
            {
                SceneRuntime.Current.Contrast = v;
                SceneRuntime.ApplyCurrent();
            });
            group.AddSlider(Translator.Get("SCX_SUN_GAIN"), 0f, 3f, 0.05f, SceneRuntime.Current.SunIntensity, v =>
            {
                SceneRuntime.Current.SunIntensity = v;
                SceneRuntime.ApplyCurrent();
            });
            group.AddSlider(Translator.Get("SCX_EXPOSURE"), 0.5f, 1.5f, 0.02f, SceneRuntime.Current.Exposure, v =>
            {
                SceneRuntime.Current.Exposure = v;
                SceneRuntime.ApplyCurrent();
            });
            group.AddSlider(Translator.Get("SCX_WARMTH"), -1f, 1f, 0.05f, SceneRuntime.Current.Warmth, v =>
            {
                SceneRuntime.Current.Warmth = v;
                SceneRuntime.ApplyCurrent();
            });
        }

        private void BuildWorldPage(UIPanel page)
        {
            var helper = new UIHelper(page);

            var group = helper.AddGroup(Translator.Get("SCX_GROUP_WORLD"));
            group.AddCheckbox(Translator.Get("SCX_LOCK_TIME"), WorldController.TimeLocked, sel =>
            {
                WorldController.TimeLocked = sel;
            });

            group.AddSlider(Translator.Get("SCX_TIME_OF_DAY"), 0f, 24f, 0.25f, WorldController.TimeOfDayHours, v =>
            {
                WorldController.ApplyTime(v);
            });

            group.AddSlider(Translator.Get("SCX_LATITUDE"), -90f, 90f, 0.5f, WorldLat(), v =>
            {
                WorldController.ApplyPosition(v, WorldLon());
            });
            group.AddSlider(Translator.Get("SCX_LONGITUDE"), -180f, 180f, 0.5f, WorldLon(), v =>
            {
                WorldController.ApplyPosition(WorldLat(), v);
            });
            string[] skyNames = Core.SkyMood.Names;
            for (int i = 0; i < skyNames.Length; i++)
            {
                skyNames[i] = skyNames[i];
            }

            group.AddDropdown(Translator.Get("SCX_SKY"), skyNames, Mathf.Clamp(SceneRuntime.Current.SkyMood, 0, skyNames.Length - 1), sel =>
            {
                SceneRuntime.Current.SkyMood = sel;
                SceneRuntime.ApplyCurrent();
            });
        }

        /// <summary>
        /// El clima entero, un canal por fila: la casilla dice quien manda y el deslizador
        /// dice cuanto.
        /// </summary>
        /// <remarks>
        /// <b>Que hace la casilla.</b> Sin marcar, el canal es del juego y sigue su curso.
        /// Marcada, lo fija este mod en el valor del deslizador. Mover el deslizador la marca
        /// sola: quien mueve un control espera verlo aplicado, no tener que armarlo antes.
        ///
        /// <b>Donde esta la nieve.</b> No hay canal de nieve porque el juego no lo tiene. En un
        /// mapa de invierno la nieve es la lluvia; lo que decide cual cae es la casilla
        /// «la lluvia cae como nieve», que sirve tambien en mapas templados.
        /// </remarks>
        private void BuildWeatherPage(UIPanel page)
        {
            var helper = new UIHelper(page);
            var group = helper.AddGroup(Translator.Get("SCX_GROUP_WEATHER"));

            AddChannel(group, "rain", "SCX_RAIN");
            AddChannel(group, "fog", "SCX_FOG");
            AddChannel(group, "cloud", "SCX_CLOUD");
            AddChannel(group, "northernLights", "SCX_NORTHERN_LIGHTS");
            AddChannel(group, "rainbow", "SCX_RAINBOW");
            AddChannel(group, "wetness", "SCX_WETNESS");

            var extra = helper.AddGroup(Translator.Get("SCX_GROUP_CLIMATE"));
            var tempBox = (UICheckBox)extra.AddCheckbox(Translator.Get("SCX_TEMPERATURE_LOCK"),
                WorldController.TemperatureLocked, sel =>
                {
                    if (_suppressEvents) return;
                    WorldController.TemperatureLocked = sel;
                });
            extra.AddSlider(Translator.Get("SCX_TEMPERATURE"), -50f, 50f, 1f, WorldController.Temperature, v =>
            {
                WorldController.Temperature = v;
                Arm(tempBox, () => WorldController.TemperatureLocked = true);
            });

            var windBox = (UICheckBox)extra.AddCheckbox(Translator.Get("SCX_WIND_LOCK"),
                WorldController.WindLocked, sel =>
                {
                    if (_suppressEvents) return;
                    WorldController.WindLocked = sel;
                });
            extra.AddSlider(Translator.Get("SCX_WIND"), 0f, 360f, 5f, WorldController.WindDirection, v =>
            {
                WorldController.WindDirection = v;
                Arm(windBox, () => WorldController.WindLocked = true);
            });

            var flags = helper.AddGroup(Translator.Get("SCX_GROUP_WEATHER_FLAGS"));
            flags.AddCheckbox(Translator.Get("SCX_WEATHER_ON"), WorldController.WeatherEnabled != 0, sel =>
            {
                WorldController.WeatherEnabled = sel ? 1 : 0;
                WorldController.Tick();
            });
            flags.AddCheckbox(Translator.Get("SCX_RAIN_IS_SNOW"), WorldController.RainIsSnow == 1, sel =>
            {
                WorldController.RainIsSnow = sel ? 1 : 0;
                WorldController.Tick();
            });
            flags.AddCheckbox(Translator.Get("SCX_SNOWY_ROADS"), WorldController.SnowyRoads == 1, sel =>
            {
                WorldController.SnowyRoads = sel ? 1 : 0;
                WorldController.Tick();
            });
        }

        private void AddChannel(UIHelperBase group, string channel, string labelId)
        {
            string label = Translator.Get(labelId);
            var box = (UICheckBox)group.AddCheckbox(label + Translator.Get("SCX_LOCK_SUFFIX"),
                WorldController.ChannelLocked(channel), sel =>
                {
                    if (_suppressEvents) return;
                    WorldController.SetChannel(channel, sel ? WorldController.ReadChannel(channel) : -1f);
                });

            group.AddSlider(label, 0f, 1f, 0.02f, WorldController.ReadChannel(channel), v =>
            {
                WorldController.SetChannel(channel, v);
                Arm(box, null);
            });
        }

        /// <summary>Marca la casilla de un control sin volver a disparar su propio evento.</summary>
        private void Arm(UICheckBox box, Action alsoDo)
        {
            if (alsoDo != null)
            {
                alsoDo();
            }

            if (box == null || box.isChecked)
            {
                return;
            }

            _suppressEvents = true;
            try
            {
                box.isChecked = true;
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        /// <summary>
        /// El ritmo: a que velocidad corre el juego y a que velocidad corre el cielo.
        /// </summary>
        /// <remarks>
        /// El ciclo que se acelera aqui es el visual. Cambiarlo de verdad significa mover el
        /// contador de simulacion, que viaja dentro de la partida guardada, y este mod no deja
        /// rastro en una partida por estar activo. El reloj de la ciudad sigue igual.
        /// </remarks>
        private void BuildTimePage(UIPanel page)
        {
            var helper = new UIHelper(page);

            var speed = helper.AddGroup(Translator.Get("SCX_GROUP_SPEED"));
            speed.AddSlider(Translator.Get("SCX_GAME_SPEED"),
                TimeController.MinGameSpeed, TimeController.MaxGameSpeed, 0.1f, TimeController.GameSpeed, v =>
                {
                    TimeController.ApplyGameSpeed(v);
                });
            speed.AddButton(Translator.Get("SCX_SPEED_RESET"), () => TimeController.ApplyGameSpeed(1f));

            var cycle = helper.AddGroup(Translator.Get("SCX_GROUP_CYCLE"));
            var enableBox = (UICheckBox)cycle.AddCheckbox(Translator.Get("SCX_CYCLE_ENABLE"),
                TimeController.CycleSpeedEnabled, sel =>
                {
                    if (_suppressEvents) return;
                    TimeController.CycleSpeedEnabled = sel;
                });

            cycle.AddSlider(Translator.Get("SCX_CYCLE_SPEED"),
                TimeController.MinCycleSpeed, TimeController.MaxCycleSpeed, 0.1f, TimeController.CycleSpeed, v =>
                {
                    TimeController.CycleSpeed = v;
                    Arm(enableBox, () => TimeController.CycleSpeedEnabled = true);
                });

            cycle.AddCheckbox(Translator.Get("SCX_CYCLE_SEPARATE"), TimeController.SeparateDayNight, sel =>
            {
                TimeController.SeparateDayNight = sel;
            });

            cycle.AddSlider(Translator.Get("SCX_CYCLE_NIGHT_SPEED"),
                TimeController.MinCycleSpeed, TimeController.MaxCycleSpeed, 0.1f, TimeController.NightCycleSpeed, v =>
                {
                    TimeController.NightCycleSpeed = v;
                    Arm(enableBox, () => TimeController.CycleSpeedEnabled = true);
                });

            cycle.AddCheckbox(Translator.Get("SCX_CYCLE_PAUSED"), TimeController.CycleWhilePaused, sel =>
            {
                TimeController.CycleWhilePaused = sel;
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
            var names = new List<string> { Translator.Get("SCX_LUT_KEEP") };
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

            RefreshSuiteDropdown();
        }

        private void RefreshSuiteDropdown()
        {
            if (_suiteDropDown == null)
            {
                return;
            }

            _suppressEvents = true;
            try
            {
                string[] suites = SuiteManager.ListSuiteNames();
                _suiteDropDown.items = suites;
            }
            finally
            {
                _suppressEvents = false;
            }
        }
    }
}
