using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
namespace SceneFX.Core
{
    internal static class LegacyMigration
    {
        internal static string SaveSuite(string sourcePath)
        {
            string xml = File.ReadAllText(sourcePath);
            var source = new XmlDocument { XmlResolver = null }; source.LoadXml(xml);
            if (source.DocumentElement.Name != "sceneStyle") throw new ArgumentException("Expected a Scene style document.");
            var result = new XmlDocument(); var root = result.CreateElement("suiteProfile"); result.AppendChild(root);
            root.SetAttribute("schema", "3"); root.SetAttribute("source", Path.GetFileName(sourcePath));
            var scene = result.CreateElement("scenefx"); root.AppendChild(scene);
            var lumen = result.CreateElement("lumenfx"); root.AppendChild(lumen);
            var atmo = result.CreateElement("atmospherefx");
            var light = new Dictionary<string, string> { { "gamma", "gamma" }, { "brightness", "brightness" },
                { "contrast", "contrast" }, { "toneEnabled", "toneEnabled" }, { "exposure", "skyExposure" },
                { "skyTonemap", "skyTonemapping" }, { "sunIntensity", "legacySceneSunMultiplier" }, { "warmth", "legacySceneWarmth" } };
            Add(scene, "vanillaMode", "false"); Add(lumen, "vanillaMode", "false"); Add(lumen, "legacySceneLighting", "true");
            foreach (XmlNode field in source.DocumentElement.ChildNodes)
            {
                if (field.NodeType != XmlNodeType.Element) continue;
                if (light.ContainsKey(field.Name)) Add(lumen, light[field.Name], field.InnerText);
                else if (field.Name == "fogDensity" || field.Name == "fogStart")
                {
                    if (float.Parse(field.InnerText, System.Globalization.CultureInfo.InvariantCulture) > 0f)
                        Add(atmo, field.Name == "fogDensity" ? "density" : "startDistance", field.InnerText);
                }
                else scene.AppendChild(result.ImportNode(field, true));
            }
            if (atmo.HasChildNodes) { Add(atmo, "vanillaMode", "false"); root.AppendChild(atmo); }
            string path = Path.Combine(SuiteManager.SuiteFolder, StyleStore.SafeName(Path.GetFileNameWithoutExtension(sourcePath)) + "-migrated-v3.suite.xml");
            if (File.Exists(path) || File.Exists(path + ".source.xml")) throw new IOException("Migration already exists; choose another preset name.");
            Infrastructure.FxStorage.WriteText(path + ".source.xml", xml);
            Infrastructure.FxStorage.WriteText(path, result.OuterXml);
            return path;
        }
        private static void Add(XmlElement parent, string name, string text)
        { var node = parent.OwnerDocument.CreateElement(name); node.InnerText = text; parent.AppendChild(node); }
    }
}
