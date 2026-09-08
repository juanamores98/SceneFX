using UnityEngine;
using SceneFX.Infrastructure;
namespace SceneFX.Core
{
    internal static class CameraEffects
    {
        internal static void Apply(StyleData style)
        {
            var camera = GameObject.Find("Main Camera");
            if (camera != null)
            {
                Set(camera.GetComponent<ColossalFramework.ColorCorrectionLut>(), style.LutEnabled);
                Set(camera.GetComponent<UnityStandardAssets.ImageEffects.Bloom>(), style.BloomEnabled);
            }
            var rain = Object.FindObjectOfType<RainParticleProperties>();
            if (rain == null) return;
            if (style.RainMotionBlur < 0) PropertyLedger.Release(rain, "ForceRainMotionBlur");
            else PropertyLedger.Write(rain, "ForceRainMotionBlur", style.RainMotionBlur == 1);
        }
        private static void Set(Behaviour component, int choice)
        {
            if (component == null) return;
            if (choice < 0) PropertyLedger.Release(component, "enabled");
            else PropertyLedger.Write(component, "enabled", choice == 1);
        }
        internal static void Restore() { PropertyLedger.ReleaseAll(); }
        internal static void Clear() { }
    }
}
