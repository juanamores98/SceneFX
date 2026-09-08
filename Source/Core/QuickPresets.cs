using System.IO;
using System.Reflection;

namespace SceneFX.Core
{
    internal static class QuickPresets
    {
        internal static bool ApplyVanilla()
        {
            return SceneFXMod.ApplySuiteSection("<scenefx><vanillaMode>true</vanillaMode></scenefx>");
        }

        internal static bool ApplyOptimized()
        {
            SceneRuntime.RestoreGame();
            using (var stream = typeof(QuickPresets).Assembly.GetManifestResourceStream("SceneFX.BuiltIns.Optimized.xml"))
            {
                if (stream == null) return false;
                using (var reader = new StreamReader(stream)) return SceneFXMod.ApplySuiteSection(reader.ReadToEnd());
            }
        }
    }
}
