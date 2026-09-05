using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using ColossalFramework.Packaging;
using ColossalFramework.IO;

namespace SceneFX.Core
{
    /// <summary>
    /// Compatibility mode: reads color grading tables (.crp) that the user
    /// already has installed on this machine and makes them available to
    /// styles at runtime. The mod never ships, copies or modifies those
    /// files; if they are not present, the styles still apply their other
    /// parameters.
    /// </summary>
    internal static class LutCompat
    {
        private static readonly Dictionary<string, Texture3D> Loaded =
            new Dictionary<string, Texture3D>(StringComparer.OrdinalIgnoreCase);

        private static bool _scanned;

        internal static void ScanFolders()
        {
            _scanned = true;
            Loaded.Clear();

            // 1. A Luts folder next to the mod DLL (user-provided files).
            string modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Scan(Path.Combine(modDir ?? "", "Luts"));

            // 2. Luts folders shipped by other locally installed mods.
            string modsRoot = Path.Combine(
                Path.Combine(Path.Combine(DataLocation.localApplicationData, "Colossal Order"), "Cities_Skylines"),
                Path.Combine("Addons", "Mods"));
            try
            {
                if (Directory.Exists(modsRoot))
                {
                    foreach (string dir in Directory.GetDirectories(modsRoot, "Luts", SearchOption.AllDirectories))
                    {
                        Scan(dir);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SceneFX] compatible scan failed: " + e.Message);
            }
        }

        private static void Scan(string folder)
        {
            try
            {
                if (!Directory.Exists(folder))
                {
                    return;
                }

                foreach (string file in Directory.GetFiles(folder, "*.crp"))
                {
                    LoadFile(file);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SceneFX] could not read " + folder + ": " + e.Message);
            }
        }

        private static void LoadFile(string path)
        {
            string key = Path.GetFileNameWithoutExtension(path);
            try
            {
                var package = new Package(path);
                var enumerator = package.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var asset = enumerator.Current as Package.Asset;
                    if (asset == null)
                    {
                        continue;
                    }

                    try
                    {
                        string typeName = asset.type.ToString();
                        if (typeName.IndexOf("Texture", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        Texture3D texture = asset.Instantiate<Texture3D>();
                        if (texture == null)
                        {
                            continue;
                        }

                        texture.name = key;
                        Register(key, texture);
                        if (!string.IsNullOrEmpty(asset.name))
                        {
                            Register(asset.name, texture);
                        }

                        break;
                    }
                    catch
                    {
                        // Not a readable texture asset; skip it.
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SceneFX] compatible load skipped " + key + ": " + e.Message);
            }
        }

        private static void Register(string name, Texture3D texture)
        {
            if (!string.IsNullOrEmpty(name) && !Loaded.ContainsKey(name))
            {
                Loaded[name] = texture;
            }
        }

        internal static bool EnsureScanned()
        {
            if (!_scanned)
            {
                ScanFolders();
            }

            return Loaded.Count > 0;
        }

        /// <summary>
        /// Exact key, then suffix match (id.name / pack.name).
        /// </summary>
        internal static bool TryGet(string name, out Texture3D texture)
        {
            EnsureScanned();
            texture = null;
            if (string.IsNullOrEmpty(name) || Loaded.Count == 0)
            {
                return false;
            }

            if (Loaded.TryGetValue(name, out texture))
            {
                return true;
            }

            foreach (var pair in Loaded)
            {
                if (pair.Key.EndsWith("." + name, StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith("." + pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    texture = pair.Value;
                    return true;
                }
            }

            return false;
        }

        internal static ICollection<string> Names()
        {
            EnsureScanned();
            return Loaded.Keys;
        }
    }
}
