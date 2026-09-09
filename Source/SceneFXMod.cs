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
        public static string ActiveClaims
        {
            get
            {
                if (!SceneRuntime.Active || SceneRuntime.VanillaMode) return string.Empty;
                string claims = StyleEngine.ActiveClaims;
                if (WorldController.PositionSet || Infrastructure.FxInterop.ClassicRequest("sunCoords")) claims += "sunPosition";
                return claims;
            }
        }

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
            SceneRuntime.LoadPersisted();
            // Covers enabling the mod while a map is already running; the
            // gameplay scene replaces menu-time hosts anyway.
            CreateHost();
        }

        public void OnDisabled()
        {
            UI.UuiButton.Unregister();
            SceneRuntime.SaveOptions();
            SceneRuntime.Flush();
            DestroyHosts();
            SceneRuntime.RestoreGame();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            var group = helper.AddGroup("SceneFX");
            group.AddButton("VANILLA", FxModule.Release);
            group.AddButton("OPTIMIZED / Default", FxModule.ApplyOptimized);
            group.AddButton("Open compact panel", () => FxModule.OpenStandalone());
            group.AddCheckbox("Apply saved settings when a city loads", SceneRuntime.ApplyOnLoad, value => { SceneRuntime.ApplyOnLoad = value; SceneRuntime.SaveOptions(); });
            group.AddCheckbox("Borderless game window (Windows)", SceneRuntime.Borderless, value => { SceneRuntime.Borderless = value; if (value) BorderlessMode.Apply(); else BorderlessMode.Restore(); SceneRuntime.SaveOptions(); });
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
            SceneRuntime.SaveOptions();
            SceneRuntime.Flush();
            DestroyHosts();
            SceneRuntime.RestoreGame();
            StyleEngine.ClearCache();
            WorldController.ClearCache();
            TimeController.ClearCache();
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

        public static void RefreshDerivedState() { if (SceneRuntime.Active && !SceneRuntime.VanillaMode) WorldController.RefreshPosition(); NotifyStateChanged(); }
        public static bool ReadyForSuite { get { return UnityEngine.Object.FindObjectOfType<DayNightProperties>() != null && GameObject.Find("Main Camera") != null; } }
        public static string LastApplyError { get; private set; }
        public static string ApplicationStatus { get; private set; }
        public static event System.Action StateChanged;
        public static void NotifyStateChanged()
        {
            var changed = StateChanged;
            if (changed == null) return;
            foreach (System.Action observer in changed.GetInvocationList())
                try { observer(); } catch (System.Exception e) { UnityEngine.Debug.LogException(e); }
        }
        public static bool ValidateSuiteSection(string xml)
        {
            try
            {
                var doc = new System.Xml.XmlDocument { XmlResolver = null }; doc.LoadXml(xml);
                return ParseSection(doc.DocumentElement, false);
            }
            catch (System.Exception e) { LastApplyError = e.Message; return false; }
        }
        public static bool ApplySuiteSection(System.Xml.XmlElement element)
        {
            if (!ParseSection(element, false)) return false;
            if (Infrastructure.FxTransaction.Active) return ParseSection(element, true);
            string previous = ExportSuiteSection();
            Infrastructure.FxTransaction.Begin();
            try
            {
                if (!ParseSection(element, true)) throw new System.InvalidOperationException(LastApplyError);
                Infrastructure.FxTransaction.Commit();
                return true;
            }
            catch (System.Exception failure)
            {
                if (!Infrastructure.FxTransaction.Active) Infrastructure.FxTransaction.Begin();
                var doc = new System.Xml.XmlDocument(); doc.LoadXml(previous);
                bool restored = ParseSection(doc.DocumentElement, true) && ExportSuiteSection() == previous;
                Infrastructure.FxTransaction.Abort();
                LastApplyError = failure.Message;
                ApplicationStatus = (restored && !failure.Message.StartsWith("PARTIAL:") ? "Failed; previous settings restored: " : "PARTIAL; rollback could not be verified: ") + failure.Message;
                NotifyStateChanged();
                return false;
            }
            finally { Infrastructure.FxTransaction.Abort(); }
        }
        private static bool ParseSection(System.Xml.XmlElement element, bool commit)
        {
            if (element == null || !element.Name.Equals("scenefx", System.StringComparison.OrdinalIgnoreCase)) return false;
            try
            {
                LastApplyError = string.Empty;
                Infrastructure.FxStorage.LastError = string.Empty;
                string schema = element.GetAttribute("schema");
                if (schema.Length > 0 && schema != "2" && schema != "3") throw new System.ArgumentException("Unsupported preset schema: " + schema);

                var culture = System.Globalization.CultureInfo.InvariantCulture;
                var current = SceneRuntime.Current.Clone();
                bool vanilla = SceneRuntime.VanillaMode;
                bool worldTouched = false;

                foreach (System.Xml.XmlNode node in element.ChildNodes)
                {
                    if (node.NodeType != System.Xml.XmlNodeType.Element) continue;
                    string name = node.Name.ToLowerInvariant();
                    string val = node.InnerText != null ? node.InnerText.Trim() : string.Empty;




                    if (name == "lut") current.Lut = val;
                    else if (name == "gamma") current.Gamma = float.Parse(val, culture);
                    else if (name == "brightness") current.Brightness = float.Parse(val, culture);
                    else if (name == "contrast") current.Contrast = float.Parse(val, culture);
                    else if (name == "sunintensity") current.SunIntensity = float.Parse(val, culture);
                    else if (name == "exposure") current.Exposure = float.Parse(val, culture);
                    else if (name == "warmth") current.Warmth = float.Parse(val, culture);
                    else if (name == "fogdensity") current.FogDensity = float.Parse(val, culture);
                    else if (name == "fogstart") current.FogStart = float.Parse(val, culture);
                    else if (name == "skytonemap") current.SkyTonemap = bool.Parse(val);
                    else if (name == "worldconfigured") current.WorldConfigured = bool.Parse(val);
                    else if (name == "includeworld") current.IncludeWorld = bool.Parse(val);
                    else if (name == "vanillamode")
                    {
                        vanilla = bool.Parse(val);
                    }
                    else if (name == "timelocked") { current.TimeLocked = bool.Parse(val); worldTouched = true; }
                    else if (name == "timeset") current.TimeSet = bool.Parse(val);
                    else if (name == "positionset") current.PositionSet = bool.Parse(val);
                    else if (name == "lutenabled") current.LutEnabled = int.Parse(val, culture);
                    else if (name == "toneenabled") current.ToneEnabled = int.Parse(val, culture);
                    else if (name == "bloomenabled") current.BloomEnabled = int.Parse(val, culture);
                    else if (name == "rainmotionblur") current.RainMotionBlur = int.Parse(val, culture);
                    else if (name == "timeofday") { current.TimeOfDay = float.Parse(val, culture); current.TimeSet = true; worldTouched = true; }
                    else if (name == "latitude") { current.Latitude = float.Parse(val, culture); current.PositionSet = true; worldTouched = true; }
                    else if (name == "longitude") { current.Longitude = float.Parse(val, culture); current.PositionSet = true; worldTouched = true; }
                    else if (name == "rain") { current.Rain = float.Parse(val, culture); worldTouched = true; }
                    else if (name == "fog") { current.Fog = float.Parse(val, culture); worldTouched = true; }
                    else if (name == "cloud") { current.Cloud = float.Parse(val, culture); worldTouched = true; }
                    else if (name == "northernlights") { current.NorthernLights = float.Parse(val, culture); worldTouched = true; }
                    else if (name == "rainbow") { current.Rainbow = float.Parse(val, culture); worldTouched = true; }
                    else if (name == "groundwetness") { current.GroundWetness = float.Parse(val, culture); worldTouched = true; }
                    else if (name == "temperature") { current.Temperature = float.Parse(val, culture); worldTouched = true; }
                    else if (name == "temperaturelock") { current.TemperatureLock = bool.Parse(val); worldTouched = true; }
                    else if (name == "winddirection") { current.WindDirection = float.Parse(val, culture); worldTouched = true; }
                    else if (name == "windlock") { current.WindLock = bool.Parse(val); worldTouched = true; }
                    else if (name == "weatherenabled") { current.WeatherEnabled = int.Parse(val, culture); worldTouched = true; }
                    else if (name == "rainissnow") { current.RainIsSnow = int.Parse(val, culture); worldTouched = true; }
                    else if (name == "snowyroads") { current.SnowyRoads = int.Parse(val, culture); worldTouched = true; }
                    else if (name == "gamespeed") { current.GameSpeed = float.Parse(val, culture); worldTouched = true; }
                    else if (name == "cyclespeedenabled") { current.CycleSpeedEnabled = bool.Parse(val); worldTouched = true; }
                    else if (name == "cyclespeed") { current.CycleSpeed = float.Parse(val, culture); worldTouched = true; }
                    else if (name == "nightcyclespeed") { current.NightCycleSpeed = float.Parse(val, culture); worldTouched = true; }
                    else if (name == "separatedaynight") { current.SeparateDayNight = bool.Parse(val); worldTouched = true; }
                    else if (name == "cyclewhilepaused") { current.CycleWhilePaused = bool.Parse(val); worldTouched = true; }
                }

                current.Validate();
                if (!vanilla) StyleEngine.ValidateResources(current);
                if (!commit) return true;
                SceneRuntime.ReplaceState(current, vanilla, element.GetAttribute("kind") == "snapshot" ? current.WorldConfigured : worldTouched || current.IncludeWorld);
                SceneRuntime.Flush();
                if (!string.IsNullOrEmpty(Infrastructure.FxStorage.LastError)) throw new System.IO.IOException(Infrastructure.FxStorage.LastError);
                ApplicationStatus = "Applied to settings; verify appearance in game";
                Infrastructure.FxInterop.RefreshCompanions();
                NotifyStateChanged();
                return true;
            }
            catch (System.Exception e)
            {
                LastApplyError = e.Message;
                ApplicationStatus = "Failed: " + e.Message;

                UnityEngine.Debug.LogException(e);
                return false;
            }
        }

        public static string ExportSuiteSection()
        {
            var state = SceneRuntime.Current.Clone();
            var doc = new System.Xml.XmlDocument();
            using (var writer = new System.IO.StringWriter(System.Globalization.CultureInfo.InvariantCulture))
            {
                new System.Xml.Serialization.XmlSerializer(typeof(StyleData)).Serialize(writer, state);
                doc.LoadXml(writer.ToString());
            }
            var result = new System.Xml.XmlDocument();
            var section = result.CreateElement("scenefx");
            result.AppendChild(section);
            section.SetAttribute("schema", "3"); section.SetAttribute("kind", "snapshot");
            foreach (System.Xml.XmlNode child in doc.DocumentElement.ChildNodes)
                section.AppendChild(result.ImportNode(child, true));
            var mode = result.CreateElement("vanillaMode");
            mode.InnerText = SceneRuntime.VanillaMode ? "true" : "false";
            section.AppendChild(mode);
            return section.OuterXml;
        }
    }
}
