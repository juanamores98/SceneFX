using System;
using System.Collections.Generic;
using System.IO;

namespace SceneFX.Infrastructure
{
    // Stages writes from all four assemblies. Only the suite coordinator commits.
    internal static class FxTransaction
    {
        private const string Key = "FX.PendingFiles.v1";
        internal static bool Active { get { return AppDomain.CurrentDomain.GetData(Key) != null; } }
        internal static bool Stage(string path, string text)
        {
            var files = AppDomain.CurrentDomain.GetData(Key) as Dictionary<string, string>;
            if (files == null) return false;
            files[Path.GetFullPath(path)] = text; return true;
        }
        internal static void Begin()
        {
            if (Active) throw new InvalidOperationException("A suite operation is already running.");
            AppDomain.CurrentDomain.SetData(Key, new Dictionary<string, string>());
        }
        internal static void Abort() { AppDomain.CurrentDomain.SetData(Key, null); }
        internal static void Commit()
        {
            var pending = (Dictionary<string, string>)AppDomain.CurrentDomain.GetData(Key);
            var previous = new Dictionary<string, byte[]>();
            foreach (var path in pending.Keys)
            {
                previous[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
                previous[path + ".bak"] = File.Exists(path + ".bak") ? File.ReadAllBytes(path + ".bak") : null;
            }
            Abort();
            try { foreach (var file in pending) FxStorage.WriteText(file.Key, file.Value); }
            catch (Exception failure)
            {
                var errors = new List<string>();
                foreach (var file in previous)
                {
                    try { if (file.Value == null) { if (File.Exists(file.Key)) File.Delete(file.Key); } else File.WriteAllBytes(file.Key, file.Value); }
                    catch (Exception e) { errors.Add(file.Key + ": " + e.Message); }
                }
                throw new IOException((errors.Count > 0 ? "PARTIAL: disk rollback failed: " + string.Join("; ", errors.ToArray()) : "Settings write failed; files restored") + ". " + failure.Message, failure);
            }
        }
    }
}
