using System;
using System.Collections.Generic;
using System.Reflection;

namespace SceneFX.Infrastructure
{
    internal static class FxInterop
    {
        private static readonly Dictionary<string, PropertyInfo> Readers = new Dictionary<string, PropertyInfo>();
        internal static bool ClassicRequest(string feature)
        {
            var values = AppDomain.CurrentDomain.GetData("FX.ClassicRequests.v1") as string;
            return !string.IsNullOrEmpty(values) && ("," + values + ",").Contains("," + feature + ",");
        }
        internal static void RefreshCompanions()
        {
            if (Equals(AppDomain.CurrentDomain.GetData("FX.Refreshing.v1"), true)) return;
            AppDomain.CurrentDomain.SetData("FX.Refreshing.v1", true);
            try
            {
                foreach (string name in new[] { "LumenFX.LumenFXMod", "AtmosphereFX.AtmosphereFXMod", "SceneFX.SceneFXMod", "ClassicLightFX.ClassicLightFXMod" })
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var type = assembly.GetType(name, false);
                        if (type == null) continue;
                        var method = type.GetMethod("RefreshDerivedState", BindingFlags.Public | BindingFlags.Static);
                        if (method != null) method.Invoke(null, null);
                        break;
                    }
            }
            finally { AppDomain.CurrentDomain.SetData("FX.Refreshing.v1", false); }
        }
        internal static bool Claims(string typeName, string field)
        {
            PropertyInfo reader;
            if (!Readers.TryGetValue(typeName, out reader))
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType(typeName, false);
                    if (type != null) { reader = type.GetProperty("ActiveClaims"); break; }
                }
                // Assemblies cannot be unloaded independently in this runtime.
                if (reader != null) Readers[typeName] = reader;
            }
            if (reader == null) return false;
            var value = reader.GetValue(null, null) as string;
            return !string.IsNullOrEmpty(value) && ("," + value + ",").Contains("," + field + ",");
        }
    }
}
