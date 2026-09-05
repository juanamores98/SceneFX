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
            DestroyHosts();
            _host = new GameObject(HostObjectName);
            _host.AddComponent<PanelEngine>();
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

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
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
            SceneRuntime.RestoreGame();
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
