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
        }

        /// <summary>
        /// Scene hosts created while the main menu is up die when the gameplay
        /// scene loads, so the host is (re)created here for every map.
        /// </summary>
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

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
    }
}
