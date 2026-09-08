using UnityEngine;
using SceneFX.Core;
namespace SceneFX
{
    public class PanelEngine : MonoBehaviour
    {
        internal static void OpenFromTray() { FxModule.OpenStandalone(); }
        internal static void CloseLegacy() { FxModule.CloseStandalone(); }
        public static void ToggleWindow() { FxModule.OpenStandalone(true); }
        private void Start() { SceneRuntime.LoadPersisted(); SceneRuntime.ApplyOnLevel(); if (SceneRuntime.Borderless) BorderlessMode.Apply(); }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10) || Input.GetKeyDown(KeyCode.F11)) ToggleWindow();
            if (SceneRuntime.Active && !SceneRuntime.VanillaMode) { WorldController.Tick(); TimeController.Tick(); }
            SceneRuntime.CheckPendingSave();
        }
        private void OnDestroy()
        {
            SceneRuntime.SaveOptions(); SceneRuntime.Flush(); FxModule.CloseStandalone();
            BorderlessMode.Restore();
            SceneRuntime.RestoreGame(); StyleEngine.ClearCache(); WorldController.ClearCache(); TimeController.ClearCache();
        }
    }
}
