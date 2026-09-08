using System;
using System.IO;
using ColossalFramework.UI;
using UnityEngine;
using SceneFX.UI;

namespace SceneFX
{
    /// <summary>Small public boundary for a standalone or embedded native panel.</summary>
    public static class FxModule
    {
        public const float PreferredWidth = 360f;
        private static PanelView _standalone;
        public static string Mode { get { return Core.SceneRuntime.VanillaMode ? "VANILLA" : (Infrastructure.FxStorage.MatchesOptimized(ReadState(), typeof(SceneFXMod)) ? "OPTIMIZED" : "CUSTOM"); } }
        public static string ReadState() { return SceneFXMod.ExportSuiteSection(); }
        public static bool ApplyState(string xml) { return SceneFXMod.ApplySuiteSection(xml); }
        public static void Release() { if (!Core.QuickPresets.ApplyVanilla()) throw new InvalidOperationException("VANILLA could not be applied."); Flush(); }
        public static void ApplyOptimized() { if (!Core.QuickPresets.ApplyOptimized()) throw new InvalidOperationException("OPTIMIZED could not be applied."); Flush(); }
        public static void Flush() { Core.SceneRuntime.SaveOptions(); Core.SceneRuntime.Flush(); }
        public static string Status { get { return !string.IsNullOrEmpty(Infrastructure.FxStorage.LastError) ? Infrastructure.FxStorage.LastError : !string.IsNullOrEmpty(Core.StyleEngine.LastLutError) ? Core.StyleEngine.LastLutError : Mode; } }

        public static PanelView CreatePanel(UIComponent parent, float width = PreferredWidth, float height = 680f)
        {
            var view = new PanelView("SceneFX", parent, width, height, Release, ApplyOptimized, () => Status);
            var look = view.AddPage("Look");
            view.Choice(look, "Colour correction LUT", LutNames, LutIndex, index => Edit(() => Core.SceneRuntime.Current.Lut = index <= 0 ? string.Empty : LutNames()[index]));
            view.Choice(look, "LUT correction", () => SwitchChoices, () => Core.SceneRuntime.Current.LutEnabled + 1, v => Edit(() => Core.SceneRuntime.Current.LutEnabled = v - 1));
            view.Choice(look, "Camera tonemapping", () => SwitchChoices, () => Core.SceneRuntime.Current.ToneEnabled + 1, v => Edit(() => Core.SceneRuntime.Current.ToneEnabled = v - 1));
            view.Choice(look, "Game bloom", () => SwitchChoices, () => Core.SceneRuntime.Current.BloomEnabled + 1, v => Edit(() => Core.SceneRuntime.Current.BloomEnabled = v - 1));
            view.Number(look, "Gamma", () => Core.SceneRuntime.Current.Gamma, v => Edit(() => Core.SceneRuntime.Current.Gamma = v), 1.5f, 3.5f, 0.01f);
            view.Number(look, "Brightness", () => Core.SceneRuntime.Current.Brightness, v => Edit(() => Core.SceneRuntime.Current.Brightness = v), -1f, 4f, 0.01f);
            view.Number(look, "Contrast", () => Core.SceneRuntime.Current.Contrast, v => Edit(() => Core.SceneRuntime.Current.Contrast = v), -1f, 1f, 0.01f);
            view.Number(look, "Warmth", () => Core.SceneRuntime.Current.Warmth, v => Edit(() => Core.SceneRuntime.Current.Warmth = v), -1f, 1f, 0.01f);
            view.Number(look, "Daylight gain", () => Core.SceneRuntime.Current.SunIntensity, v => Edit(() => Core.SceneRuntime.Current.SunIntensity = v), 0f, 3f, 0.01f);
            view.Number(look, "Sky exposure (0 = game)", () => Core.SceneRuntime.Current.Exposure, v => Edit(() => Core.SceneRuntime.Current.Exposure = v), 0f, 5f, 0.001f);
            view.Number(look, "Fog density (0 = unchanged)", () => Core.SceneRuntime.Current.FogDensity, v => Edit(() => Core.SceneRuntime.Current.FogDensity = v), 0f, 0.005f, 1e-05f);
            view.Number(look, "Fog start (0 = unchanged)", () => Core.SceneRuntime.Current.FogStart, v => Edit(() => Core.SceneRuntime.Current.FogStart = v), 0f, 10000f, 1f);
            view.Check(look, "Sky tonemapping", () => Core.SceneRuntime.Current.SkyTonemap, v => Edit(() => Core.SceneRuntime.Current.SkyTonemap = v));
            var world = view.AddPage("Weather");
            view.Check(world, "Lock rain / snow intensity", () => Core.WorldController.RainIntensity >= 0f, v => WorldEdit(() => Core.WorldController.RainIntensity = v ? 0f : -1f));
            view.Number(world, "Rain / snow intensity", () => Mathf.Max(0f, Core.WorldController.RainIntensity), v => WorldEdit(() => Core.WorldController.RainIntensity = v), 0f, 2.5f, 0.01f);
            view.Check(world, "Lock weather fog", () => Core.WorldController.FogIntensity >= 0f, v => WorldEdit(() => Core.WorldController.FogIntensity = v ? 0f : -1f));
            view.Number(world, "Weather fog", () => Mathf.Max(0f, Core.WorldController.FogIntensity), v => WorldEdit(() => Core.WorldController.FogIntensity = v), 0f, 1f, 0.01f);
            view.Check(world, "Lock clouds", () => Core.WorldController.CloudIntensity >= 0f, v => WorldEdit(() => Core.WorldController.CloudIntensity = v ? 0f : -1f));
            view.Number(world, "Clouds", () => Mathf.Max(0f, Core.WorldController.CloudIntensity), v => WorldEdit(() => Core.WorldController.CloudIntensity = v), 0f, 1f, 0.01f);
            view.Check(world, "Lock northern lights", () => Core.WorldController.NorthernLights >= 0f, v => WorldEdit(() => Core.WorldController.NorthernLights = v ? 0f : -1f));
            view.Number(world, "Northern lights", () => Mathf.Max(0f, Core.WorldController.NorthernLights), v => WorldEdit(() => Core.WorldController.NorthernLights = v), 0f, 1f, 0.01f);
            view.Check(world, "Lock rainbow", () => Core.WorldController.Rainbow >= 0f, v => WorldEdit(() => Core.WorldController.Rainbow = v ? 0f : -1f));
            view.Number(world, "Rainbow", () => Mathf.Max(0f, Core.WorldController.Rainbow), v => WorldEdit(() => Core.WorldController.Rainbow = v), 0f, 1f, 0.01f);
            view.Check(world, "Lock ground wetness", () => Core.WorldController.GroundWetness >= 0f, v => WorldEdit(() => Core.WorldController.GroundWetness = v ? 0f : -1f));
            view.Number(world, "Ground wetness", () => Mathf.Max(0f, Core.WorldController.GroundWetness), v => WorldEdit(() => Core.WorldController.GroundWetness = v), 0f, 1f, 0.01f);
            view.Check(world, "Lock temperature", () => Core.WorldController.TemperatureLocked, v => WorldEdit(() => Core.WorldController.TemperatureLocked = v));
            view.Number(world, "Temperature (C)", () => Core.WorldController.Temperature, v => WorldEdit(() => { Core.WorldController.Temperature = v; Core.WorldController.TemperatureLocked = true; }), -100f, 100f, 0.1f);
            view.Check(world, "Lock wind direction", () => Core.WorldController.WindLocked, v => WorldEdit(() => Core.WorldController.WindLocked = v));
            view.Number(world, "Wind direction", () => Core.WorldController.WindDirection, v => WorldEdit(() => { Core.WorldController.WindDirection = v; Core.WorldController.WindLocked = true; }), 0f, 360f, 0.1f);
            view.Choice(world, "Dynamic weather", () => SwitchChoices, () => Core.WorldController.WeatherEnabled + 1, v => WorldEdit(() => Core.WorldController.WeatherEnabled = v - 1));
            view.Choice(world, "Snow instead of rain", () => SwitchChoices, () => Core.WorldController.RainIsSnow + 1, v => WorldEdit(() => Core.WorldController.RainIsSnow = v - 1));
            view.Choice(world, "Snow on roads", () => SwitchChoices, () => Core.WorldController.SnowyRoads + 1, v => WorldEdit(() => Core.WorldController.SnowyRoads = v - 1));
            view.Choice(world, "Rain motion blur", () => SwitchChoices, () => Core.SceneRuntime.Current.RainMotionBlur + 1, v => Edit(() => Core.SceneRuntime.Current.RainMotionBlur = v - 1));
            var time = view.AddPage("Time");
            view.Number(time, "Hour (0â€“24)", () => Core.WorldController.ReadTimeHours(), v => WorldEdit(() => Core.WorldController.ApplyTime(v)), 0f, 24f, 0.01f);
            view.Check(time, "Lock hour", () => Core.WorldController.TimeLocked, v => WorldEdit(() => { if (v) Core.WorldController.ApplyTime(Core.WorldController.ReadTimeHours()); Core.WorldController.TimeLocked = v; }));
            view.Number(time, "Latitude", () => ReadCoordinate(true), v => WorldEdit(() => Core.WorldController.ApplyPosition(v, ReadCoordinate(false))), -90f, 90f, 0.1f);
            view.Number(time, "Longitude", () => ReadCoordinate(false), v => WorldEdit(() => Core.WorldController.ApplyPosition(ReadCoordinate(true), v)), -180f, 180f, 0.1f);
            view.Number(time, "Game speed", () => Core.TimeController.GameSpeed, v => WorldEdit(() => Core.TimeController.ApplyGameSpeed(v)), 0.01f, 5f, 0.01f);
            view.Check(time, "Control visual day/night speed", () => Core.TimeController.CycleSpeedEnabled, v => WorldEdit(() => Core.TimeController.CycleSpeedEnabled = v));
            view.Number(time, "Day speed (1 = game)", () => Core.TimeController.CycleSpeed, v => WorldEdit(() => { Core.TimeController.CycleSpeed = v; Core.TimeController.CycleSpeedEnabled = true; }), 0f, 128f, 0.01f);
            view.Check(time, "Separate night speed", () => Core.TimeController.SeparateDayNight, v => WorldEdit(() => Core.TimeController.SeparateDayNight = v));
            view.Number(time, "Night speed", () => Core.TimeController.NightCycleSpeed, v => WorldEdit(() => Core.TimeController.NightCycleSpeed = v), 0f, 128f, 0.01f);
            view.Check(time, "Continue visual cycle while paused", () => Core.TimeController.CycleWhilePaused, v => WorldEdit(() => Core.TimeController.CycleWhilePaused = v));
            view.Action(time, "Pause / resume simulation", () => { var sim = SimulationManager.instance; if (sim != null) sim.SimulationPaused = !sim.SimulationPaused; });
            var files = view.AddPage("Presets");
            string[] paths = Core.StyleStore.ListStyleFiles(); int selected = 0; string name = "My scene"; bool includeWorld = false;
            view.Choice(files, "Saved style", () => FileNames(paths, ".scene.xml"), () => selected, v => selected = v);
            view.Action(files, "Apply selected style", () => { if (selected >= 0 && selected < paths.Length) { var style = Core.StyleStore.LoadStyle(paths[selected]); if (style != null) Core.SceneRuntime.LoadStyle(style); } });
            view.Text(files, "Name", () => name, v => name = v);
            view.Check(files, "Include world in this preset", () => includeWorld, v => includeWorld = v);
            view.Action(files, "Save style", () => { var copy = Core.SceneRuntime.Current.Clone(); copy.Name = name; copy.IncludeWorld = includeWorld; if (includeWorld) Core.StyleEngine.CaptureWorld(copy, true); Core.StyleStore.SaveStyle(copy); paths = Core.StyleStore.ListStyleFiles(); });
            view.Action(files, "Refresh styles", () => paths = Core.StyleStore.ListStyleFiles());
            string[] suites = Core.SuiteManager.ListSuiteFiles(); int suiteIndex = 0;
            view.Choice(files, "Saved suite", () => FileNames(suites, ".suite.xml"), () => suiteIndex, v => suiteIndex = v);
            view.Action(files, "Apply selected suite", () => { if (suiteIndex >= 0 && suiteIndex < suites.Length && !Core.SuiteManager.ApplySuiteProfile(suites[suiteIndex])) throw new InvalidOperationException("The suite could not be applied."); });
            view.Action(files, "Save all four FX as suite", () => { Core.SuiteManager.SaveSuiteProfile(name); suites = Core.SuiteManager.ListSuiteFiles(); });
            view.Check(files, "Apply settings when a city loads", () => Core.SceneRuntime.ApplyOnLoad, v => { Core.SceneRuntime.ApplyOnLoad = v; Core.SceneRuntime.SaveOptions(); });

            view.Refresh();
            return view;
        }

        internal static void OpenStandalone(bool toggle = false)
        {
            if (_standalone == null || _standalone.Root == null)
            {
                _standalone = CreatePanel(null, PreferredWidth, Mathf.Min(680f, UIView.GetAView().fixedHeight - 24f));
                _standalone.Root.relativePosition = new Vector3(Mathf.Clamp(WindowX, 0f, Mathf.Max(0f, UIView.GetAView().fixedWidth - PreferredWidth)), Mathf.Clamp(WindowY, 0f, Mathf.Max(0f, UIView.GetAView().fixedHeight - _standalone.Root.height)));
                _standalone.Root.eventPositionChanged += (c, value) => { WindowX = value.x; WindowY = value.y; SavePosition(); };
            }
            else _standalone.Root.isVisible = toggle ? !_standalone.Root.isVisible : true;
            var screen = UIView.GetAView();
            _standalone.SetSize(PreferredWidth, Mathf.Min(680f, screen.fixedHeight - 24f));
            var pos = _standalone.Root.relativePosition;
            _standalone.Root.relativePosition = new Vector3(Mathf.Clamp(pos.x, 0f, Mathf.Max(0f, screen.fixedWidth - _standalone.Root.width)), Mathf.Clamp(pos.y, 0f, Mathf.Max(0f, screen.fixedHeight - _standalone.Root.height)));
            _standalone.Refresh();
        }

        internal static void CloseStandalone()
        {
            if (_standalone != null) _standalone.Dispose();
            _standalone = null;
        }
        private static readonly string[] SwitchChoices = { "Game", "Off", "On" };
        private static string[] LutNames() { var names = new System.Collections.Generic.List<string> { "Game LUT" }; names.AddRange(Core.StyleEngine.ListLuts()); return names.ToArray(); }
        private static int LutIndex() { return Mathf.Max(0, Array.IndexOf(LutNames(), Core.SceneRuntime.Current.Lut)); }
        private static string[] FileNames(string[] paths, string suffix) { return paths.Length == 0 ? new[] { "No saved presets" } : Array.ConvertAll(paths, p => Path.GetFileName(p).Substring(0, Path.GetFileName(p).Length - suffix.Length)); }
        private static float ReadCoordinate(bool latitude) { var dn = UnityEngine.Object.FindObjectOfType<DayNightProperties>(); return dn == null ? 0f : (latitude ? dn.m_Latitude : dn.m_Longitude); }
        private static void Edit(Action edit) { edit(); Core.SceneRuntime.VanillaMode = false; Core.SceneRuntime.ApplyCurrent(); Core.SceneRuntime.SaveOptions(); }
        private static void WorldEdit(Action edit) { edit(); Core.WorldController.Tick(); Core.SceneRuntime.WorldChanged(); }
        private static float WindowX { get { return Core.SceneRuntime.WindowX; } set { Core.SceneRuntime.WindowX = value; } }
        private static float WindowY { get { return Core.SceneRuntime.WindowY; } set { Core.SceneRuntime.WindowY = value; } }
        private static void SavePosition() { Core.SceneRuntime.SaveOptions(); }
    }
}
