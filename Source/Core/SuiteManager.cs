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
        public static string LastResult { get; private set; }

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

                string optPath = Path.Combine(SuiteFolder, "Default-v3.suite.xml");
                if (File.Exists(optPath)) return;

                using (var stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("SceneFX.BuiltIns.Optimized.suite.xml"))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            Infrastructure.FxStorage.WriteText(optPath, reader.ReadToEnd());
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
                list.Add(Path.GetFileName(file).Substring(0, Path.GetFileName(file).Length - ".suite.xml".Length));
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
                var doc = new XmlDocument { XmlResolver = null };
                doc.LoadXml(xml);
                var root = doc.DocumentElement;
                if (root == null)
                {
                    return false;
                }

                if (root.Name != "suiteProfile") throw new InvalidOperationException("Expected suiteProfile root.");
                var types = new Dictionary<string, string> {
                    { "scenefx", "SceneFX.SceneFXMod" }, { "lumenfx", "LumenFX.LumenFXMod" },
                    { "atmospherefx", "AtmosphereFX.AtmosphereFXMod" }, { "classiclightfx", "ClassicLightFX.ClassicLightFXMod" }
                };
                var sections = new List<XmlElement>();
                var previous = new List<string>();
                var seen = new HashSet<string>();
                foreach (XmlNode child in root.ChildNodes)
                {
                    var section = child as XmlElement;
                    if (section == null) continue;
                    if (!types.ContainsKey(section.Name) || !seen.Add(section.Name)) throw new InvalidOperationException("Unknown or duplicate suite section: " + section.Name);
                    var type = FindModType(types[section.Name]);
                    if (type == null) throw new InvalidOperationException("Required participant missing: " + section.Name);
                    var validate = type.GetMethod("ValidateSuiteSection", new[] { typeof(string) });
                    var ready = type.GetProperty("ReadyForSuite");
                    if (validate == null || ready == null) throw new InvalidOperationException("Update all four FX before applying suites: " + section.Name);
                    if (!(bool)ready.GetValue(null, null)) throw new InvalidOperationException("Participant is not ready in this city: " + section.Name);
                    if (!(bool)validate.Invoke(null, new object[] { section.OuterXml }))
                        throw new InvalidOperationException(section.Name + ": " + type.GetProperty("LastApplyError").GetValue(null, null));
                    string before = ExportSection(types[section.Name]);
                    if (string.IsNullOrEmpty(before)) throw new InvalidOperationException("Could not capture " + section.Name);
                    sections.Add(section); previous.Add(before);
                }
                if (sections.Count == 0) throw new InvalidOperationException("The suite has no sections.");

                Infrastructure.FxTransaction.Begin();
                int attempted = -1;
                try
                {
                    for (int i = 0; i < sections.Count; i++)
                    {
                        attempted = i;
                        if (!ApplySection(types[sections[i].Name], sections[i])) throw new InvalidOperationException("Rejected while applying " + sections[i].Name);
                    }
                    Infrastructure.FxTransaction.Commit();
                    LastResult = "Suite applied to settings; visual verification pending";
                    return true;
                }
                catch (Exception failure)
                {
                    bool restored = !failure.Message.StartsWith("PARTIAL:", StringComparison.Ordinal);
                    if (!Infrastructure.FxTransaction.Active) Infrastructure.FxTransaction.Begin();
                    // Include the failing participant: it may have changed state before rejecting.
                    for (int i = attempted; i >= 0; i--)
                    {
                        var before = new XmlDocument(); before.LoadXml(previous[i]);
                        if (!ApplySection(types[sections[i].Name], before.DocumentElement)) restored = false;
                        if (ExportSection(types[sections[i].Name]) != previous[i]) restored = false;
                    }
                    Infrastructure.FxTransaction.Abort();
                    LastResult = (restored ? "Failed; previous settings restored: " : "PARTIAL; rollback could not be verified: ") + failure.Message;
                    Debug.LogWarning("[SceneFX] " + LastResult);
                    return false;
                }
                finally { Infrastructure.FxTransaction.Abort(); }
            }
            catch (Exception e)
            {
                LastResult = "Validation failed; nothing applied: " + e.Message;
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
            Infrastructure.FxStorage.WriteText(path, xml);
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

        /// <summary>Cierto si ese mod de la suite esta cargado.</summary>
        internal static bool ModPresent(string typeFullName)
        {
            return FindModType(typeFullName) != null;
        }

        /// <summary>
        /// Le pide a otro mod que escriba unos ajustes que son suyos.
        /// </summary>
        /// <remarks>
        /// <b>Por que pedir en vez de escribir.</b> Varias propiedades del juego las escribian
        /// dos o tres mods de la suite, y el resultado dependia del orden en que se aplicaran:
        /// poner el mismo preset en uno y luego en otro daba imagenes distintas. La regla ahora
        /// es que cada propiedad tiene un solo dueño, y quien no lo es se la pide por su API
        /// publica —la misma que usa un perfil de suite— en vez de escribirla por su cuenta.
        ///
        /// Si el dueño no esta cargado, quien pide se la apaña solo: la funcion no desaparece
        /// porque falte un mod.
        /// </remarks>
        internal static bool Delegate(string typeFullName, string sectionXml)
        {
            try
            {
                var type = FindModType(typeFullName);
                if (type == null)
                {
                    return false;
                }

                var doc = new XmlDocument { XmlResolver = null };
                doc.LoadXml(sectionXml);
                return ApplySection(typeFullName, doc.DocumentElement);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        internal static bool IsLumenFXToneWriter()
        {
            return IsLumenFXWriting("tone");
        }

        /// <summary>
        /// Cierto si LumenFX esta escribiendo ese campo compartido ahora mismo.
        /// </summary>
        /// <remarks>
        /// Pregunta primero por la lista de reclamaciones por campo. Si el LumenFX instalado
        /// es anterior y no la publica, cae al booleano de tono de siempre, que solo puede
        /// responder por el tono; para el resto de campos responde que no, que es como se
        /// comportaba esto antes de existir el arbitraje.
        /// </remarks>
        internal static bool IsLumenFXWriting(string field)
        {
            try
            {
                var type = FindModType("LumenFX.LumenFXMod");
                if (type == null)
                {
                    return false;
                }

                var claims = type.GetProperty("ActiveClaims", BindingFlags.Public | BindingFlags.Static);
                if (claims != null)
                {
                    string value = claims.GetValue(null, null) as string;
                    if (string.IsNullOrEmpty(value))
                    {
                        return false;
                    }

                    return ("," + value + ",").IndexOf("," + field + ",", StringComparison.Ordinal) >= 0;
                }

                if (field != "tone")
                {
                    return false;
                }

                var legacy = type.GetProperty("ToneWriterActive", BindingFlags.Public | BindingFlags.Static);
                if (legacy == null)
                {
                    return false;
                }

                object flag = legacy.GetValue(null, null);
                return flag is bool && (bool)flag;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
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
