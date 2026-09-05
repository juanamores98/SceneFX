using UnityEngine;
using SceneFX.Core;
using SceneFX.UI;

namespace SceneFX
{
    /// <summary>
    /// Scene host: hotkey (F10) and the style panel lifecycle.
    /// </summary>
    public class PanelEngine : MonoBehaviour
    {
        private static bool _open;

        private StylePanel _panel;
        private int _windowId;

        internal static void Close()
        {
            _open = false;
        }

        private void Start()
        {
            _windowId = GetInstanceID();
            SceneRuntime.LoadPersisted();
            _panel = new StylePanel(OnSceneChanged);

            if (SceneRuntime.ApplyOnLoad)
            {
                SceneRuntime.ApplyCurrent();
            }
        }

        private static void OnSceneChanged()
        {
            SceneRuntime.SaveOptions();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                _open = !_open;
            }
        }

        private void OnGUI()
        {
            if (_open)
            {
                _panel.Draw(_windowId);
            }
        }
    }
}
