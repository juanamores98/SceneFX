using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using ColossalFramework.UI;
using ICities;
using SceneFX.Core;
using SceneFX.Locale;

namespace SceneFX.UI
{
    /// <summary>
    /// Native in-game panel (ColossalFramework UI) with 4 ergonomic tabs and
    /// collapsible UICards: Style & Color, Sun & Light, Weather & Atmosphere, Time & Speed.
    /// Includes a persistent sticky header with 1-click Vanilla, Optimized Suite, and quick time.
    /// Toggled with F10 or the Unified UI tray button.
    /// </summary>
    internal sealed class NativePanel
    {
        private static string GetTabLabel(string id, string fallback)
        {
            string t = Translator.Get(id);
            return (string.IsNullOrEmpty(t) || t == id) ? fallback : t;
        }

        private static readonly string[] Tabs =
        {
            GetTabLabel("SCX_TAB_STYLE_COLOR", "Style & Color"),
            GetTabLabel("SCX_TAB_SUN_LIGHT", "Sun & Light"),
            GetTabLabel("SCX_TAB_WEATHER_ATMOS", "Weather & Sky"),
            GetTabLabel("SCX_TAB_TIME_SPEED", "Time & Rhythm"),
        };

        private UIPanel _root;
        private readonly UIScrollablePanel[] _pages = new UIScrollablePanel[Tabs.Length];
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

        // Sticky header components
        private UICheckBox _stickyVanillaCheck;
        private UISlider _stickyTimeSlider;
        private UILabel _stickyTimeLabel;

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
                UpdateStickyHeaderValues();
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
                    UpdateStickyHeaderValues();
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
                _root.size = new Vector2(480f, 780f);
                _root.relativePosition = new Vector3(650f, 60f);
                _root.opacity = 0.96f;

                var drag = _root.AddUIComponent<UIDragHandle>();
                drag.size = new Vector2(420f, 36f);
                drag.target = _root;

                var title = _root.AddUIComponent<UILabel>();
                title.text = "SceneFX Studio";
                title.textScale = 1.15f;
                title.relativePosition = new Vector3(12f, 8f);

                var close = _root.AddUIComponent<UIButton>();
                close.text = "X";
                close.size = new Vector2(28f, 24f);
                close.relativePosition = new Vector3(442f, 7f);
                close.normalBgSprite = "ButtonMenu";
                close.hoveredBgSprite = "ButtonMenuHovered";
                close.eventClicked += (c, p) => Hide();

                // -------------------------------------------------------------
                // Sticky Top Bar (Persistent quick actions)
                // -------------------------------------------------------------
                BuildStickyHeader();

                // -------------------------------------------------------------
                // 4 Ergonomic Tab Buttons
                // -------------------------------------------------------------
                float tabWidth = 111f;
                float x = 8f;
                for (int i = 0; i < Tabs.Length; i++)
                {
                    int index = i;
                    var tab = _root.AddUIComponent<UIButton>();
                    tab.text = Tabs[i];
                    tab.size = new Vector2(tabWidth, 28f);
                    tab.relativePosition = new Vector3(x, 76f);
                    tab.textScale = 0.82f;
                    tab.normalBgSprite = "ButtonMenu";
                    tab.hoveredBgSprite = "ButtonMenuHovered";
                    tab.focusedBgSprite = "ButtonMenuFocused";
                    tab.textColor = new Color32(255, 255, 255, 255);
                    tab.eventClicked += (c, p) => SelectTab(index);
                    _tabButtons[i] = tab;
                    x += tabWidth + 4f;
                }

                // -------------------------------------------------------------
                // 4 Scrollable Tab Pages
                // -------------------------------------------------------------
                _pages[0] = NewPage();
                BuildStyleColorPage(_pages[0]);

                _pages[1] = NewPage();
                BuildSunLightPage(_pages[1]);

                _pages[2] = NewPage();
                BuildWeatherPage(_pages[2]);

                _pages[3] = NewPage();
                BuildTimePage(_pages[3]);

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

        private void BuildStickyHeader()
        {
            var header = _root.AddUIComponent<UIPanel>();
            header.size = new Vector2(464f, 36f);
            header.relativePosition = new Vector3(8f, 36f);
            header.backgroundSprite = "GenericPanel";
            header.color = new Color32(20, 22, 28, 240);

            // 1. Vanilla mode toggle
            var stickyHelper = (UIHelperBase)new UIHelper(header);
            _stickyVanillaCheck = (UICheckBox)stickyHelper.AddCheckbox(Translator.Get("SCX_VANILLA_SHORT"), SceneRuntime.VanillaMode, sel =>
            {
                if (_suppressEvents) return;
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
            _stickyVanillaCheck.relativePosition = new Vector3(8f, 6f);

            // 2. Suite: Optimized Quick Button
            var optBtn = header.AddUIComponent<UIButton>();
            optBtn.text = Translator.Get("SCX_SUITE_OPTIMIZED");
            optBtn.size = new Vector2(140f, 24f);
            optBtn.relativePosition = new Vector3(115f, 6f);
            optBtn.textScale = 0.78f;
            optBtn.normalBgSprite = "ButtonMenu";
            optBtn.hoveredBgSprite = "ButtonMenuHovered";
            optBtn.eventClicked += (c, p) =>
            {
                SuiteManager.ApplySuiteProfile("Optimized");
                RefreshDropdowns();
                UpdateStickyHeaderValues();
            };

            // 3. Quick Time Slider & Label
            _stickyTimeLabel = header.AddUIComponent<UILabel>();
            _stickyTimeLabel.text = "" + FormatHours(WorldController.TimeOfDayHours);
            _stickyTimeLabel.textScale = 0.8f;
            _stickyTimeLabel.relativePosition = new Vector3(264f, 10f);

            _stickyTimeSlider = header.AddUIComponent<UISlider>();
            _stickyTimeSlider.size = new Vector2(130f, 14f);
            _stickyTimeSlider.relativePosition = new Vector3(324f, 11f);
            _stickyTimeSlider.minValue = 0f;
            _stickyTimeSlider.maxValue = 24f;
            _stickyTimeSlider.stepSize = 0.1f;
            _stickyTimeSlider.value = WorldController.TimeOfDayHours;

            var track = _stickyTimeSlider.AddUIComponent<UISlicedSprite>();
            track.size = _stickyTimeSlider.size;
            track.relativePosition = Vector3.zero;
            track.spriteName = "ScrollbarTrack";

            var thumb = _stickyTimeSlider.AddUIComponent<UISlicedSprite>();
            thumb.size = new Vector2(12f, 14f);
            thumb.spriteName = "ScrollbarThumb";
            _stickyTimeSlider.thumbObject = thumb;

            _stickyTimeSlider.eventValueChanged += (c, val) =>
            {
                if (_suppressEvents) return;
                WorldController.ApplyTime(val);
                if (_stickyTimeLabel != null)
                {
                    _stickyTimeLabel.text = "" + FormatHours(val);
                }
            };
        }

        private static string FormatHours(float hours)
        {
            float clamped = Mathf.Repeat(hours, 24f);
            int h = Mathf.FloorToInt(clamped);
            int m = Mathf.FloorToInt((clamped - h) * 60f);
            return string.Format("{0:00}:{1:00}", h, m);
        }

        private void UpdateStickyHeaderValues()
        {
            _suppressEvents = true;
            try
            {
                if (_stickyVanillaCheck != null)
                {
                    _stickyVanillaCheck.isChecked = SceneRuntime.VanillaMode;
                }
                if (_stickyTimeSlider != null)
                {
                    _stickyTimeSlider.value = WorldController.TimeOfDayHours;
                }
                if (_stickyTimeLabel != null)
                {
                    _stickyTimeLabel.text = "" + FormatHours(WorldController.TimeOfDayHours);
                }
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private UIScrollablePanel NewPage()
        {
            var page = _root.AddUIComponent<UIScrollablePanel>();
            page.backgroundSprite = null;
            page.size = new Vector2(468f, 664f);
            page.relativePosition = new Vector3(6f, 108f);
            page.autoLayout = true;
            page.autoLayoutDirection = LayoutDirection.Vertical;
            page.autoLayoutPadding = new RectOffset(0, 0, 0, 8);
            page.scrollWheelDirection = UIOrientation.Vertical;
            page.builtinKeyNavigation = true;
            page.clipChildren = true;
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

        // =====================================================================
        // TAB 1: Style & Color (Estilos, LUTs, Gradación de Color y Suite)
        // =====================================================================
        private void BuildStyleColorPage(UIScrollablePanel page)
        {
            // --- Card 1: Perfiles & LUTs ---
            var cardProfiles = UICard.Create(page, "StyleLutCard", Translator.Get("SCX_CARD_PROFILES"), startCollapsed: false);
            var hProfiles = (UIHelperBase)new UIHelper(cardProfiles.Content);

            string[] styleNames = ListStyleNames();
            _styleDropDown = (UIDropDown)hProfiles.AddDropdown(Translator.Get("SCX_STYLE"), styleNames, 0, sel =>
            {
                if (_suppressEvents) return;
                _activeStyleName = styleNames[sel];
                ApplyStyleByName(_activeStyleName);
                UpdateStickyHeaderValues();
            });

            _nameField = (UITextField)hProfiles.AddTextfield(Translator.Get("SCX_STYLE_NAME"), "My scene", sel => { });

            hProfiles.AddCheckbox(Translator.Get("SCX_INCLUDE_WORLD"), _includeWorldInSavedStyle, sel =>
            {
                _includeWorldInSavedStyle = sel;
            });

            hProfiles.AddButton(Translator.Get("SCX_SAVE_STYLE"), () =>
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

            hProfiles.AddButton(Translator.Get("SCX_RESTORE_GAME"), () =>
            {
                SceneRuntime.RestoreGame();
                WorldController.Restore();
                TimeController.Restore();
                UpdateStickyHeaderValues();
            });

            string[] lutNames = ListLutNames();
            _lutDropDown = (UIDropDown)hProfiles.AddDropdown(Translator.Get("SCX_LUT"), lutNames, 0, sel =>
            {
                if (_suppressEvents) return;
                SceneRuntime.Current.Lut = lutNames[sel].Replace("  (compat)", string.Empty);
                SceneRuntime.ApplyCurrent();
            });

            hProfiles.AddButton(Translator.Get("SCX_BAKE_LUT"), () =>
            {
                string pngPath;
                string bakeName = "Baked_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                Texture3D baked = BakeToLut.Bake(SceneRuntime.Current, bakeName, out pngPath);
                if (baked != null)
                {
                    SceneRuntime.Current.Lut = bakeName;
                    RefreshDropdowns();
                    SceneRuntime.ApplyCurrent();
                }
            });

            hProfiles.AddCheckbox(Translator.Get("SCX_SKY_TONEMAP"), SceneRuntime.Current.SkyTonemap, sel =>
            {
                SceneRuntime.Current.SkyTonemap = sel;
                SceneRuntime.ApplyCurrent();
            });

            // --- Card 2: Gradación de Color en Vivo ---
            var cardGrade = UICard.Create(page, "GradeCard", Translator.Get("SCX_CARD_GRADE"), startCollapsed: false);
            var hGrade = new UIHelper(cardGrade.Content);

            hGrade.AddSlider(Translator.Get("SCX_GAMMA"), 1.2f, 3f, 0.05f, SceneRuntime.Current.Gamma, v =>
            {
                SceneRuntime.Current.Gamma = v;
                SceneRuntime.ApplyCurrent();
            });
            hGrade.AddSlider(Translator.Get("SCX_BRIGHTNESS"), -1f, 1f, 0.05f, SceneRuntime.Current.Brightness, v =>
            {
                SceneRuntime.Current.Brightness = v;
                SceneRuntime.ApplyCurrent();
            });
            hGrade.AddSlider(Translator.Get("SCX_CONTRAST"), -1f, 1f, 0.05f, SceneRuntime.Current.Contrast, v =>
            {
                SceneRuntime.Current.Contrast = v;
                SceneRuntime.ApplyCurrent();
            });
            hGrade.AddSlider(Translator.Get("SCX_EXPOSURE"), 0.5f, 1.5f, 0.02f, SceneRuntime.Current.Exposure, v =>
            {
                SceneRuntime.Current.Exposure = v;
                SceneRuntime.ApplyCurrent();
            });
            hGrade.AddSlider(Translator.Get("SCX_WARMTH"), -1f, 1f, 0.05f, SceneRuntime.Current.Warmth, v =>
            {
                SceneRuntime.Current.Warmth = v;
                SceneRuntime.ApplyCurrent();
            });
            // La ganancia solar vive en la pestana del sol. Tenerla tambien aqui daba dos
            // deslizadores para el mismo valor que no se enteraban el uno del otro: mover uno
            // dejaba al otro mintiendo hasta reabrir el panel.
            hGrade.AddButton(Translator.Get("SCX_RESET_GRADE"), () =>
            {
                SceneRuntime.Current.Gamma = 2.2f;
                SceneRuntime.Current.Brightness = 0f;
                SceneRuntime.Current.Contrast = 0f;
                SceneRuntime.Current.Exposure = 1.0f;
                SceneRuntime.Current.Warmth = 0f;
                SceneRuntime.Current.SunIntensity = 1.0f;
                SceneRuntime.ApplyCurrent();
            });

            // --- Card 3: Perfil de Suite Multi-Mod ---
            var cardSuite = UICard.Create(page, "SuiteCard", Translator.Get("SCX_CARD_SUITE"), startCollapsed: true);
            var hSuite = (UIHelperBase)new UIHelper(cardSuite.Content);

            string[] suiteNames = SuiteManager.ListSuiteNames();
            _suiteDropDown = (UIDropDown)hSuite.AddDropdown(Translator.Get("SCX_SUITE_PROFILE"), suiteNames, 0, sel => { });
            hSuite.AddButton(Translator.Get("SCX_SUITE_APPLY"), () =>
            {
                if (_suiteDropDown != null && _suiteDropDown.selectedIndex >= 0 && _suiteDropDown.selectedIndex < _suiteDropDown.items.Length)
                {
                    string selProfile = _suiteDropDown.items[_suiteDropDown.selectedIndex];
                    SuiteManager.ApplySuiteProfile(selProfile);
                    RefreshDropdowns();
                    UpdateStickyHeaderValues();
                }
            });

            _suiteNameField = (UITextField)hSuite.AddTextfield(Translator.Get("SCX_SUITE_NAME"), "MySuite", sel => { });
            hSuite.AddButton(Translator.Get("SCX_SUITE_EXPORT"), () =>
            {
                string sName = _suiteNameField != null ? _suiteNameField.text : "MySuite";
                SuiteManager.SaveSuiteProfile(sName);
                RefreshSuiteDropdown();
            });
        }

        // =====================================================================
        // TAB 2: Sun & Light (Posición solar, Geografía y Enlace a LumenFX)
        // =====================================================================
        private void BuildSunLightPage(UIScrollablePanel page)
        {
            // --- Card 1: Geografía & Domo Celeste ---
            var cardGeo = UICard.Create(page, "GeoCard", Translator.Get("SCX_CARD_GEO"), startCollapsed: false);
            var hGeo = new UIHelper(cardGeo.Content);

            hGeo.AddSlider(Translator.Get("SCX_LATITUDE"), -90f, 90f, 0.5f, WorldLat(), v =>
            {
                WorldController.ApplyPosition(v, WorldLon());
            });
            hGeo.AddSlider(Translator.Get("SCX_LONGITUDE"), -180f, 180f, 0.5f, WorldLon(), v =>
            {
                WorldController.ApplyPosition(WorldLat(), v);
            });

            string[] skyNames = Core.SkyMood.Names;
            hGeo.AddDropdown(Translator.Get("SCX_SKY"), skyNames, Mathf.Clamp(SceneRuntime.Current.SkyMood, 0, skyNames.Length - 1), sel =>
            {
                SceneRuntime.Current.SkyMood = sel;
                SceneRuntime.ApplyCurrent();
            });

            // --- Card 2: Iluminación Directa & LumenFX ---
            var cardSun = UICard.Create(page, "SunCard", Translator.Get("SCX_CARD_SUN"), startCollapsed: false);
            var hSun = new UIHelper(cardSun.Content);

            hSun.AddSlider(Translator.Get("SCX_SUN_GAIN"), 0f, 3f, 0.05f, SceneRuntime.Current.SunIntensity, v =>
            {
                SceneRuntime.Current.SunIntensity = v;
                SceneRuntime.ApplyCurrent();
            });

            hSun.AddButton(Translator.Get("SCX_OPEN_LUMENFX"), () =>
            {
                TryToggleCompanionWindow("LumenFX", "LumenFX.Core.TunerEngine");
            });
        }

        // =====================================================================
        // TAB 3: Weather & Atmosphere (Canales, Nieve y Enlace a AtmosphereFX)
        // =====================================================================
        private void BuildWeatherPage(UIScrollablePanel page)
        {
            // --- Card 1: Canales del Clima ---
            var cardWeather = UICard.Create(page, "WeatherCard", Translator.Get("SCX_CARD_WEATHER"), startCollapsed: false);
            var hWeather = new UIHelper(cardWeather.Content);

            hWeather.AddCheckbox(Translator.Get("SCX_WEATHER_ON"), WorldController.WeatherEnabled != 0, sel =>
            {
                WorldController.WeatherEnabled = sel ? 1 : 0;
                WorldController.Tick();
            });

            AddChannel(hWeather, "rain", "SCX_RAIN");
            AddChannel(hWeather, "fog", "SCX_FOG");
            AddChannel(hWeather, "cloud", "SCX_CLOUD");
            AddChannel(hWeather, "northernLights", "SCX_NORTHERN_LIGHTS");
            AddChannel(hWeather, "rainbow", "SCX_RAINBOW");
            AddChannel(hWeather, "wetness", "SCX_WETNESS");

            // --- Card 2: Clima, Viento y Nieve ---
            var cardClimate = UICard.Create(page, "ClimateCard", Translator.Get("SCX_CARD_CLIMATE"), startCollapsed: false);
            var hClimate = new UIHelper(cardClimate.Content);

            var tempBox = (UICheckBox)hClimate.AddCheckbox(Translator.Get("SCX_TEMPERATURE_LOCK"),
                WorldController.TemperatureLocked, sel =>
                {
                    if (_suppressEvents) return;
                    WorldController.TemperatureLocked = sel;
                });
            hClimate.AddSlider(Translator.Get("SCX_TEMPERATURE"), -50f, 50f, 1f, WorldController.Temperature, v =>
            {
                WorldController.Temperature = v;
                Arm(tempBox, () => WorldController.TemperatureLocked = true);
            });

            var windBox = (UICheckBox)hClimate.AddCheckbox(Translator.Get("SCX_WIND_LOCK"),
                WorldController.WindLocked, sel =>
                {
                    if (_suppressEvents) return;
                    WorldController.WindLocked = sel;
                });
            hClimate.AddSlider(Translator.Get("SCX_WIND"), 0f, 360f, 5f, WorldController.WindDirection, v =>
            {
                WorldController.WindDirection = v;
                Arm(windBox, () => WorldController.WindLocked = true);
            });

            hClimate.AddCheckbox(Translator.Get("SCX_RAIN_IS_SNOW"), WorldController.RainIsSnow == 1, sel =>
            {
                WorldController.RainIsSnow = sel ? 1 : 0;
                WorldController.Tick();
            });
            hClimate.AddCheckbox(Translator.Get("SCX_SNOWY_ROADS"), WorldController.SnowyRoads == 1, sel =>
            {
                WorldController.SnowyRoads = sel ? 1 : 0;
                WorldController.Tick();
            });

            // --- Card 3: AtmosphereFX ---
            var cardAtmo = UICard.Create(page, "AtmoCard", Translator.Get("SCX_CARD_ATMO"), startCollapsed: false);
            var hAtmo = new UIHelper(cardAtmo.Content);

            hAtmo.AddButton(Translator.Get("SCX_OPEN_ATMOSPHEREFX"), () =>
            {
                TryToggleCompanionWindow("AtmosphereFX", "AtmosphereFX.Runtime.AtmosphereEngine");
            });
        }

        // =====================================================================
        // TAB 4: Time & Speed (Simulación, Ciclo Día/Noche y Pausa)
        // =====================================================================
        private void BuildTimePage(UIScrollablePanel page)
        {
            // --- Card 1: Velocidad de Simulación ---
            var cardSpeed = UICard.Create(page, "SpeedCard", Translator.Get("SCX_CARD_SPEED"), startCollapsed: false);
            var hSpeed = new UIHelper(cardSpeed.Content);

            hSpeed.AddSlider(Translator.Get("SCX_GAME_SPEED"),
                TimeController.MinGameSpeed, TimeController.MaxGameSpeed, 0.1f, TimeController.GameSpeed, v =>
                {
                    TimeController.ApplyGameSpeed(v);
                });

            // Quick speed row container
            var btnRow = cardSpeed.Content.AddUIComponent<UIPanel>();
            btnRow.size = new Vector2(440f, 30f);
            btnRow.autoLayout = false;

            // Pausar no es "velocidad cero": ApplyGameSpeed recorta a 0,1x y el juego se
            // arrastraria en vez de detenerse. La pausa de verdad es la del propio juego, la
            // misma de la barra espaciadora, y se conmuta para poder volver.
            var bPause = btnRow.AddUIComponent<UIButton>();
            bPause.text = Translator.Get("SCX_SPEED_PAUSE");
            bPause.size = new Vector2(100f, 26f);
            bPause.relativePosition = new Vector3(4f, 2f);
            bPause.textScale = 0.8f;
            bPause.normalBgSprite = "ButtonMenu";
            bPause.hoveredBgSprite = "ButtonMenuHovered";
            bPause.eventClicked += (c, p) =>
            {
                var sim = SimulationManager.instance;
                if (sim != null)
                {
                    sim.SimulationPaused = !sim.SimulationPaused;
                }
            };

            var b1x = btnRow.AddUIComponent<UIButton>();
            b1x.text = "1x";
            b1x.size = new Vector2(100f, 26f);
            b1x.relativePosition = new Vector3(112f, 2f);
            b1x.textScale = 0.8f;
            b1x.normalBgSprite = "ButtonMenu";
            b1x.hoveredBgSprite = "ButtonMenuHovered";
            b1x.eventClicked += (c, p) => TimeController.ApplyGameSpeed(1f);

            var b2x = btnRow.AddUIComponent<UIButton>();
            b2x.text = "2x";
            b2x.size = new Vector2(100f, 26f);
            b2x.relativePosition = new Vector3(220f, 2f);
            b2x.textScale = 0.8f;
            b2x.normalBgSprite = "ButtonMenu";
            b2x.hoveredBgSprite = "ButtonMenuHovered";
            b2x.eventClicked += (c, p) => TimeController.ApplyGameSpeed(2f);

            var b3x = btnRow.AddUIComponent<UIButton>();
            b3x.text = "3x";
            b3x.size = new Vector2(100f, 26f);
            b3x.relativePosition = new Vector3(328f, 2f);
            b3x.textScale = 0.8f;
            b3x.normalBgSprite = "ButtonMenu";
            b3x.hoveredBgSprite = "ButtonMenuHovered";
            b3x.eventClicked += (c, p) => TimeController.ApplyGameSpeed(3f);

            // --- Card 2: Ciclo Día / Noche ---
            var cardCycle = UICard.Create(page, "CycleCard", Translator.Get("SCX_CARD_CYCLE"), startCollapsed: false);
            var hCycle = new UIHelper(cardCycle.Content);

            hCycle.AddCheckbox(Translator.Get("SCX_LOCK_TIME"), WorldController.TimeLocked, sel =>
            {
                WorldController.TimeLocked = sel;
            });

            hCycle.AddSlider(Translator.Get("SCX_TIME_OF_DAY"), 0f, 24f, 0.25f, WorldController.TimeOfDayHours, v =>
            {
                WorldController.ApplyTime(v);
                UpdateStickyHeaderValues();
            });

            var enableBox = (UICheckBox)hCycle.AddCheckbox(Translator.Get("SCX_CYCLE_ENABLE"),
                TimeController.CycleSpeedEnabled, sel =>
                {
                    if (_suppressEvents) return;
                    TimeController.CycleSpeedEnabled = sel;
                });

            hCycle.AddSlider(Translator.Get("SCX_CYCLE_SPEED"),
                TimeController.MinCycleSpeed, TimeController.MaxCycleSpeed, 0.1f, TimeController.CycleSpeed, v =>
                {
                    TimeController.CycleSpeed = v;
                    Arm(enableBox, () => TimeController.CycleSpeedEnabled = true);
                });

            hCycle.AddCheckbox(Translator.Get("SCX_CYCLE_SEPARATE"), TimeController.SeparateDayNight, sel =>
            {
                TimeController.SeparateDayNight = sel;
            });

            hCycle.AddSlider(Translator.Get("SCX_CYCLE_NIGHT_SPEED"),
                TimeController.MinCycleSpeed, TimeController.MaxCycleSpeed, 0.1f, TimeController.NightCycleSpeed, v =>
                {
                    TimeController.NightCycleSpeed = v;
                    Arm(enableBox, () => TimeController.CycleSpeedEnabled = true);
                });

            hCycle.AddCheckbox(Translator.Get("SCX_CYCLE_PAUSED"), TimeController.CycleWhilePaused, sel =>
            {
                TimeController.CycleWhilePaused = sel;
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

        private static void TryToggleCompanionWindow(string modName, string typeName)
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name.Equals(modName, StringComparison.OrdinalIgnoreCase))
                    {
                        var type = asm.GetType(typeName);
                        if (type != null)
                        {
                            var method = type.GetMethod("ToggleWindow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                            if (method != null)
                            {
                                method.Invoke(null, null);
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SceneFX] Could not toggle " + modName + ": " + ex.Message);
            }
        }
    }
}
