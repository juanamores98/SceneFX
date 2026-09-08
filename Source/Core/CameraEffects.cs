using UnityEngine;

namespace SceneFX.Core
{
    internal static class CameraEffects
    {
        private static Behaviour _lut, _tone, _bloom;
        private static bool _lutBefore, _toneBefore, _bloomBefore;
        private static RainParticleProperties _rain;
        private static bool _rainBefore;

        internal static void Apply(StyleData style)
        {
            var camera = GameObject.Find("Main Camera");
            if (camera != null)
            {
                Set(camera.GetComponent<ColossalFramework.ColorCorrectionLut>(), style.LutEnabled, ref _lut, ref _lutBefore);
                Set(camera.GetComponent<ColossalFramework.ToneMapping>(), style.ToneEnabled, ref _tone, ref _toneBefore);
                Set(camera.GetComponent<UnityStandardAssets.ImageEffects.Bloom>(), style.BloomEnabled, ref _bloom, ref _bloomBefore);
            }
            if (style.RainMotionBlur < 0)
            {
                if (_rain != null) _rain.ForceRainMotionBlur = _rainBefore;
                _rain = null;
            }
            else
            {
                var rain = Object.FindObjectOfType<RainParticleProperties>();
                if (rain == null) return;
                if (_rain == null) { _rain = rain; _rainBefore = rain.ForceRainMotionBlur; }
                rain.ForceRainMotionBlur = style.RainMotionBlur == 1;
            }
        }

        private static void Set(Behaviour component, int choice, ref Behaviour captured, ref bool previous)
        {
            if (choice < 0)
            {
                if (captured != null) captured.enabled = previous;
                captured = null;
            }
            else if (component != null)
            {
                if (captured == null) { captured = component; previous = component.enabled; }
                component.enabled = choice == 1;
            }
        }

        internal static void Restore()
        {
            if (_lut != null) _lut.enabled = _lutBefore;
            if (_tone != null) _tone.enabled = _toneBefore;
            if (_bloom != null) _bloom.enabled = _bloomBefore;
            if (_rain != null) _rain.ForceRainMotionBlur = _rainBefore;
            Clear();
        }

        internal static void Clear() { _lut = _tone = _bloom = null; _rain = null; }
    }
}
