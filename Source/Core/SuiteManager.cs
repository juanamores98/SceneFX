using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEngine;

namespace SceneFX.Core
{
    /// <summary>
    /// Suite coordinator: orchestrates multi-mod profiles across the 4-mod suite:
    /// SceneFX, LumenFX, AtmosphereFX, and ClassicLightFX.
    /// Uses dynamic reflection discovery to apply and export suite sections.
    /// </summary>
    public static class SuiteManager
    {
        public static string SuiteFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Path.Combine("Colossal Order", Path.Combine("Cities_Skylines", "ModConfig\\SceneFX")));
            }
        }

        public static void EnsureBuiltInSuites()
        {
            try
            {
                if (!Directory.Exists(SuiteFolder))
                {
                    Directory.CreateDirectory(SuiteFolder);
                }

                string optPath = Path.Combine(SuiteFolder, "Optimized.suite.xml");
                if (!File.Exists(optPath))
                {
                    using (var stream = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("SceneFX.BuiltIns.Optimized.suite.xml"))
                    {
                        if (stream != null)
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                File.WriteAllText(optPath, reader.ReadToEnd());
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static string[] ListSuiteFiles()
        {
            if (!Directory.Exists(SuiteFolder))
            {
                return new string[0];
            }

            return Directory.GetFiles(SuiteFolder, "*.suite.xml");
        }

        public static string[] ListSuiteNames()
        {
            var list = new List<string>();
            foreach (string file in ListSuiteFiles())
            {
                list.Add(Path.GetFileNameWithoutExtension(file).Replace("-", " "));
            }

            if (list.Count == 0)
            {
                list.Add("Optimized");
            }

            return list.ToArray();
        }

        public static bool ApplySuiteProfile(string nameOrPath)
        {
            if (string.IsNullOrEmpty(nameOrPath))
            {
                return false;
            }

            string xml = null;
            if (File.Exists(nameOrPath))
            {
                xml = File.ReadAllText(nameOrPath);
            }
            else
            {
                string path = Path.Combine(SuiteFolder, StyleStore.SafeName(nameOrPath) + ".suite.xml");
                if (File.Exists(path))
                {
                    xml = File.ReadAllText(path);
                }
                else if (nameOrPath.TrimStart().StartsWith("<"))
                {
                    xml = nameOrPath;
                }
            }

            if (string.IsNullOrEmpty(xml))
            {
                Debug.LogWarning("[SceneFX] Suite profile not found: " + nameOrPath);
                return false;
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var root = doc.DocumentElement;
                if (root == null)
                {
                    return false;
                }

                bool appliedAny = false;

                // 1. SceneFX
                var sceneNode = root.SelectSingleNode("scenefx") as XmlElement;
                if (sceneNode != null)
                {
                    if (SceneFXMod.ApplySuiteSection(sceneNode))
                    {
                        appliedAny = true;
                    }
                    else
                    {
                        Debug.LogWarning("[SceneFX] Suite section 'scenefx' not applied (rejected)");
                    }
                }

                // 2. LumenFX
                var lumenNode = root.SelectSingleNode("lumenfx") as XmlElement;
                if (lumenNode != null)
                {
                    if (ApplySection("LumenFX.LumenFXMod", lumenNode))
                    {
                        appliedAny = true;
                    }
                    else
                    {
                        Debug.LogWarning("[SceneFX] Suite section 'lumenfx' not applied (mod missing or rejected)");
                    }
                }

                // 3. AtmosphereFX
                var atmoNode = root.SelectSingleNode("atmospherefx") as XmlElement;
                if (atmoNode != null)
                {
                    if (ApplySection("AtmosphereFX.AtmosphereFXMod", atmoNode))
                    {
                        appliedAny = true;
                    }
                    else
                    {
                        Debug.LogWarning("[SceneFX] Suite section 'atmospherefx' not applied (mod missing or rejected)");
                    }
                }

                // 4. ClassicLightFX
                var classicNode = root.SelectSingleNode("classiclightfx") as XmlElement;
                if (classicNode != null)
                {
                    if (ApplySection("ClassicLightFX.ClassicLightFXMod", classicNode))
                    {
                        appliedAny = true;
                    }
                    else
                    {
                        Debug.LogWarning("[SceneFX] Suite section 'classiclightfx' not applied (mod missing or rejected)");
                    }
                }

                return appliedAny;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        public static string ExportSuiteProfile(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "SuiteProfile";
            }

            var sb = new StringBuilder();
            string escapedName = System.Security.SecurityElement.Escape(name);
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendFormat("<suiteProfile name=\"{0}\">\n", escapedName);

            // SceneFX
            string sceneXml = SceneFXMod.ExportSuiteSection();
            if (!string.IsNullOrEmpty(sceneXml))
            {
                sb.AppendLine(sceneXml);
            }

            // LumenFX
            string lumenXml = ExportSection("LumenFX.LumenFXMod");
            if (!string.IsNullOrEmpty(lumenXml))
            {
                sb.AppendLine(lumenXml);
            }

            // AtmosphereFX
            string atmoXml = ExportSection("AtmosphereFX.AtmosphereFXMod");
            if (!string.IsNullOrEmpty(atmoXml))
            {
                sb.AppendLine(atmoXml);
            }

            // ClassicLightFX
            string classicXml = ExportSection("ClassicLightFX.ClassicLightFXMod");
            if (!string.IsNullOrEmpty(classicXml))
            {
                sb.AppendLine(classicXml);
            }

            sb.AppendLine("</suiteProfile>");
            return sb.ToString();
        }

        public static string SaveSuiteProfile(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "MySuite";
            }

            if (!Directory.Exists(SuiteFolder))
            {
                Directory.CreateDirectory(SuiteFolder);
            }

            string xml = ExportSuiteProfile(name);
            string path = Path.Combine(SuiteFolder, StyleStore.SafeName(name) + ".suite.xml");
            File.WriteAllText(path, xml);
            return path;
        }

        private static Type FindModType(string typeFullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(typeFullName, false);
                if (t != null)
                {
                    return t;
                }
            }
            return null;
        }

        internal static bool IsLumenFXToneWriter()
        {
            var type = FindModType("LumenFX.LumenFXMod");
            if (type == null)
            {
                return false;
            }

            var prop = type.GetProperty("ToneWriterActive", BindingFlags.Public | BindingFlags.Static);
            if (prop == null)
            {
                return false;
            }

            object value = prop.GetValue(null, null);
            return value is bool && (bool)value;
        }

        private static string ExportSection(string typeFullName)
        {
            try
            {
                var type = FindModType(typeFullName);
                if (type == null)
                {
                    return null;
                }

                var method = type.GetMethod("ExportSuiteSection", BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    return method.Invoke(null, null) as string;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return null;
        }

        private static bool ApplySection(string typeFullName, XmlElement element)
        {
            if (element == null)
            {
                return false;
            }

            try
            {
                var type = FindModType(typeFullName);
                if (type == null)
                {
                    return false;
                }

                var methodElem = type.GetMethod("ApplySuiteSection", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(XmlElement) }, null);
                if (methodElem != null)
                {
                    object res = methodElem.Invoke(null, new object[] { element });
                    return res is bool ? (bool)res : true;
                }

                var methodStr = type.GetMethod("ApplySuiteSection", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null);
                if (methodStr != null)
                {
                    object res = methodStr.Invoke(null, new object[] { element.OuterXml });
                    return res is bool ? (bool)res : true;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return false;
        }
    }
}
