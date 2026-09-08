using System;
using System.Collections.Generic;
using System.Reflection;

namespace SceneFX.Infrastructure
{
    internal static class FxInterop
    {
        private static readonly Dictionary<string, PropertyInfo> Readers = new Dictionary<string, PropertyInfo>();
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
