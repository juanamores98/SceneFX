using System;
using System.Collections.Generic;
using System.Reflection;

namespace SceneFX.Infrastructure
{
    // Shared BCL-only protocol: four independent assemblies, no fifth dependency.
    // Entry = baseline, last write, owner. Target identity scopes it to this scene.
    internal static class PropertyLedger
    {
        private const string Key = "FX.PropertyLedger.v1";
        internal static string LastWarning = string.Empty;
        private static Dictionary<object, Dictionary<string, object[]>> Entries
        {
            get
            {
                var value = AppDomain.CurrentDomain.GetData(Key) as Dictionary<object, Dictionary<string, object[]>>;
                if (value == null) { value = new Dictionary<object, Dictionary<string, object[]>>(); AppDomain.CurrentDomain.SetData(Key, value); }
                return value;
            }
        }
        private static object Read(object target, string path)
        {
            int dot = path.IndexOf('.');
            string name = dot < 0 ? path : path.Substring(0, dot);
            var type = target as Type ?? target.GetType();
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            object value = field != null ? field.GetValue(target is Type ? null : target)
                : type.GetProperty(name).GetValue(target is Type ? null : target, null);
            return dot < 0 ? value : Read(value, path.Substring(dot + 1));
        }
        private static void Set(object target, string path, object value)
        {
            int dot = path.IndexOf('.');
            string name = dot < 0 ? path : path.Substring(0, dot);
            var type = target as Type ?? target.GetType();
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            var property = field == null ? type.GetProperty(name) : null;
            if (dot >= 0) { object child = Read(target, name); Set(child, path.Substring(dot + 1), value); value = child; }
            Type valueType = field != null ? field.FieldType : property.PropertyType;
            if (value != null && !valueType.IsInstanceOfType(value)) value = Convert.ChangeType(value, valueType);
            if (field != null) field.SetValue(target is Type ? null : target, value);
            else property.SetValue(target is Type ? null : target, value, null);
        }
        private static object[] Entry(object target, string path)
        {
            Dictionary<string, object[]> fields;
            if (!Entries.TryGetValue(target, out fields)) { fields = new Dictionary<string, object[]>(); Entries[target] = fields; }
            object[] entry;
            object live = Read(target, path);
            if (!fields.TryGetValue(path, out entry)) { entry = new object[] { live, live, null }; fields[path] = entry; }
            else if (!Equals(live, entry[1]))
            {
                // A later theme/mod write becomes the reference, never our transformed output.
                entry[0] = entry[1] = live;
                LastWarning = "External change detected: " + path;
            }
            return entry;
        }
        internal static T Baseline<T>(object target, string path) { return (T)Entry(target, path)[0]; }
        internal static void Write(object target, string path, object value, string owner = "SceneFX")
        {
            if (target == null) return;
            var entry = Entry(target, path);
            Set(target, path, value);
            entry[1] = Read(target, path);
            entry[2] = owner;
        }
        internal static void Release(object target, string path, string owner = "SceneFX")
        {
            if (target == null) return;
            Dictionary<string, object[]> fields; object[] entry;
            if (!Entries.TryGetValue(target, out fields) || !fields.TryGetValue(path, out entry) || !Equals(entry[2], owner)) return;
            if (Equals(Read(target, path), entry[1])) Set(target, path, entry[0]);
            else LastWarning = "Release preserved a newer external value: " + path;
            fields.Remove(path);
            if (fields.Count == 0) Entries.Remove(target);
        }
        internal static void ReleaseAll(string owner = "SceneFX")
        {
            foreach (var target in new List<object>(Entries.Keys))
                foreach (var path in new List<string>(Entries[target].Keys)) Release(target, path, owner);
        }
        internal static void Forget(string owner = "SceneFX")
        {
            foreach (var target in new List<object>(Entries.Keys))
            {
                var fields = Entries[target];
                foreach (var path in new List<string>(fields.Keys))
                    if (fields[path][2] == null || Equals(fields[path][2], owner)) fields.Remove(path);
                if (fields.Count == 0) Entries.Remove(target);
            }
        }
    }
}
