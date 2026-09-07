using ICities;
using UnityEngine;
using SceneFX.Core;

namespace SceneFX
{
    /// <summary>
    /// SceneFX entry point: visual style switching (LUT, tone, sun, fog) for
    /// Cities: Skylines. Original implementation.
    /// </summary>
    public class SceneFXMod : LoadingExtensionBase, IUserMod
    {
        private const string HostObjectName = "SceneFX";

        private GameObject _host;

        public string Name
        {
            get { return "SceneFX"; }
        }

        public string Description
        {
            get { return "Switchable visual styles: LUTs, filmic tone, sun and fog, with a live panel (F10)."; }
        }

        public void OnEnabled()
        {
            // Covers enabling the mod while a map is already running; the
            // gameplay scene replaces menu-time hosts anyway.
            CreateHost();
        }

        public void OnDisabled()
        {
            DestroyHosts();
            NativeLut.ClearRuntimeTextures();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            var group = helper.AddGroup("SceneFX");

            group.AddCheckbox("Apply last style when a map loads", SceneRuntime.ApplyOnLoad, sel =>
            {
                SceneRuntime.ApplyOnLoad = sel;
                SceneRuntime.SaveOptions();
            });

            group.AddCheckbox("Borderless windowed mode", SceneRuntime.Borderless, sel =>
            {
                SceneRuntime.Borderless = sel;
                SceneRuntime.SaveOptions();
                if (sel)
                {
                    Core.BorderlessMode.Apply();
                }
                else
                {
                    Core.BorderlessMode.Restore();
                }
            });

            group.AddCheckbox("Vanilla mode (suspend SceneFX)", SceneRuntime.VanillaMode, sel =>
            {
                SceneRuntime.VanillaMode = sel;
                SceneRuntime.SaveOptions();
                if (sel)
                {
                    WorldController.Restore();
                    Core.SkyMood.Restore();
                    SceneRuntime.RestoreGame();
                }
                else
                {
                    SceneRuntime.ApplyCurrent();
                }
            });

            group.AddButton("Open styles folder", () =>
            {
                if (!System.IO.Directory.Exists(StyleStore.StylesFolder))
                {
                    System.IO.Directory.CreateDirectory(StyleStore.StylesFolder);
                }

                Application.OpenURL("file://" + StyleStore.StylesFolder);
            });

            group.AddButton("Restore game look", () => SceneRuntime.RestoreGame());

            group.AddButton("Vanilla (leave the game untouched)", () => Core.QuickPresets.ApplyVanilla());
            group.AddButton("Optimized (the calibrated recipe)", () => Core.QuickPresets.ApplyOptimized());

            var suiteGroup = helper.AddGroup("Suite Profiles (SceneFX + LumenFX + AtmosphereFX + ClassicLightFX)");
            suiteGroup.AddButton("Apply 'Optimized' suite profile", () =>
            {
                SuiteManager.ApplySuiteProfile("Optimized");
            });
            suiteGroup.AddButton("Export current look to suite profile", () =>
            {
                SuiteManager.SaveSuiteProfile("User_Export_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            });
            suiteGroup.AddButton("Open suite folder", () =>
            {
                if (!System.IO.Directory.Exists(SuiteManager.SuiteFolder))
                {
                    System.IO.Directory.CreateDirectory(SuiteManager.SuiteFolder);
                }
                Application.OpenURL("file://" + SuiteManager.SuiteFolder);
            });
        }

        /// <summary>
        /// Scene hosts created while the main menu is up die when the gameplay
        /// scene loads, so the host is (re)created here for every map.
        /// </summary>
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            // Antes de nada: la referencia del juego, mientras todavia es del juego.
            StyleEngine.CaptureBaseline();

            SuiteManager.EnsureBuiltInSuites();
            CreateHost();

            UI.UuiButton.Register(
                "SceneFX",
                "Visual styles, LUTs and world controls (F10)",
                UI.TrayIcon.Make(),
                show => PanelEngine.OpenFromTray());
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            UI.UuiButton.Unregister();
            WorldController.Restore();
            SkyMood.Restore();
            SceneRuntime.RestoreGame();
            StyleEngine.ClearCache();
            WorldController.ClearCache();
            DestroyHosts();
        }

        private void CreateHost()
        {
            DestroyHosts();
            _host = new GameObject(HostObjectName);
            _host.AddComponent<PanelEngine>();
        }

        private static void DestroyHosts()
        {
            while (true)
            {
                GameObject leftover = GameObject.Find(HostObjectName);
                if (!leftover)
                {
                    break;
                }

                UnityEngine.Object.DestroyImmediate(leftover);
            }
        }

        public static bool ApplySuiteSection(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return false;
            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xml);
                var root = doc.DocumentElement;
                if (root == null) return false;
                if (root.Name.Equals("scenefx", System.StringComparison.OrdinalIgnoreCase))
                {
                    return ApplySuiteSection(root);
                }
                var node = root.SelectSingleNode("scenefx") as System.Xml.XmlElement;
                if (node != null)
                {
                    return ApplySuiteSection(node);
                }
                return false;
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }
        }

        public static bool ApplySuiteSection(System.Xml.XmlElement element)
        {
            if (element == null) return false;
            try
            {
                var culture = System.Globalization.CultureInfo.InvariantCulture;
                var current = SceneRuntime.Current;

                foreach (System.Xml.XmlNode node in element.ChildNodes)
                {
                    if (node.NodeType != System.Xml.XmlNodeType.Element) continue;
                    string name = node.Name.ToLowerInvariant();
                    string val = node.InnerText != null ? node.InnerText.Trim() : string.Empty;
                    bool b;
                    float f;
                    int i;

                    if (name == "lut") current.Lut = val;
                    else if (name == "nativelut") current.NativeLut = val;
                    else if (name == "gamma" && float.TryParse(val, System.Globalization.NumberStyles.Float, culture, out f)) current.Gamma = f;
                    else if (name == "brightness" && float.TryParse(val, System.Globalization.NumberStyles.Float, culture, out f)) current.Brightness = f;
                    else if (name == "contrast" && float.TryParse(val, System.Globalization.NumberStyles.Float, culture, out f)) current.Contrast = f;
                    else if (name == "sunintensity" && float.TryParse(val, System.Globalization.NumberStyles.Float, culture, out f)) current.SunIntensity = f;
                    else if (name == "exposure" && float.TryParse(val, System.Globalization.NumberStyles.Float, culture, out f)) current.Exposure = f;
                    else if (name == "warmth" && float.TryParse(val, System.Globalization.NumberStyles.Float, culture, out f)) current.Warmth = f;
                    else if (name == "fogdensity" && float.TryParse(val, System.Globalization.NumberStyles.Float, culture, out f)) current.FogDensity = f;
                    else if (name == "fogstart" && float.TryParse(val, System.Globalization.NumberStyles.Float, culture, out f)) current.FogStart = f;
                    else if (name == "skytonemap" && bool.TryParse(val, out b)) current.SkyTonemap = b;
                    else if (name == "skymood" && int.TryParse(val, out i)) current.SkyMood = i;
                    else if (name == "includeworld" && bool.TryParse(val, out b)) current.IncludeWorld = b;
                    else if (name == "vanillamode" && bool.TryParse(val, out b))
                    {
                        // Mismo camino que la casilla de opciones: se guarda y se aplica o se
                        // deshace en el acto, no en la proxima carga.
                        Core.SceneRuntime.VanillaMode = b;
                        Core.SceneRuntime.SaveOptions();
                        if (b)
                        {
                            Core.WorldController.Restore();
                            Core.SkyMood.Restore();
                            Core.SceneRuntime.RestoreGame();
                        }
                        else
                        {
                            Core.SceneRuntime.ApplyCurrent();
                        }
                    }
                    else if (name == "timeofday" && float.TryParse(val, System.Globalization.NumberStyles.Float, culture, out f)) current.TimeOfDay = f;
                    else if (name == "latitude" && float.TryParse(val, System.Globalization.NumberStyles.Float, culture, out f)) current.Latitude = f;
                    else if (name == "longitude" && float.TryParse(val, System.Globalization.NumberStyles.Float, culture, out f)) current.Longitude = f;
                    else if (name == "rain" && float.TryParse(val, System.Globalization.NumberStyles.Float, culture, out f)) current.Rain = f;
                    else if (name == "fog" && float.TryParse(val, System.Globalization.NumberStyles.Float, culture, out f)) current.Fog = f;
                    else if (name == "cloud" && float.TryParse(val, System.Globalization.NumberStyles.Float, culture, out f)) current.Cloud = f;
                }

                SceneRuntime.ApplyCurrent();
                StyleStore.SaveState(current);
                return true;
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }
        }

        public static string ExportSuiteSection()
        {
            var c = System.Globalization.CultureInfo.InvariantCulture;
            var cur = SceneRuntime.Current;
            return string.Format(
                "  <scenefx>\n" +
                "    <lut>{0}</lut>\n" +
                "    <nativeLut>{1}</nativeLut>\n" +
                "    <gamma>{2}</gamma>\n" +
                "    <brightness>{3}</brightness>\n" +
                "    <contrast>{4}</contrast>\n" +
                "    <sunIntensity>{5}</sunIntensity>\n" +
                "    <exposure>{6}</exposure>\n" +
                "    <warmth>{7}</warmth>\n" +
                "    <fogDensity>{8}</fogDensity>\n" +
                "    <fogStart>{9}</fogStart>\n" +
                "    <skyTonemap>{10}</skyTonemap>\n" +
                "    <skyMood>{11}</skyMood>\n" +
                "    <includeWorld>{12}</includeWorld>\n" +
                "    <timeOfDay>{13}</timeOfDay>\n" +
                "    <latitude>{14}</latitude>\n" +
                "    <longitude>{15}</longitude>\n" +
                "    <rain>{16}</rain>\n" +
                "    <fog>{17}</fog>\n" +
                "    <cloud>{18}</cloud>\n" +
                "    <vanillaMode>{19}</vanillaMode>\n" +
                "  </scenefx>",
                cur.Lut ?? string.Empty,
                cur.NativeLut ?? string.Empty,
                cur.Gamma.ToString("0.00", c),
                cur.Brightness.ToString("0.00", c),
                cur.Contrast.ToString("0.00", c),
                cur.SunIntensity.ToString("0.00", c),
                cur.Exposure.ToString("0.000", c),
                cur.Warmth.ToString("0.00", c),
                cur.FogDensity.ToString("0.000000", c),
                cur.FogStart.ToString("0.0", c),
                cur.SkyTonemap.ToString().ToLowerInvariant(),
                cur.SkyMood.ToString(c),
                cur.IncludeWorld.ToString().ToLowerInvariant(),
                cur.TimeOfDay.ToString("0.00", c),
                cur.Latitude.ToString("0.00", c),
                cur.Longitude.ToString("0.00", c),
                cur.Rain.ToString("0.00", c),
                cur.Fog.ToString("0.00", c),
                cur.Cloud.ToString("0.00", c),
                SceneRuntime.VanillaMode.ToString().ToLowerInvariant());
        }
    }
}
