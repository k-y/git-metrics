using System;
using System.IO;
using System.Xml;
using UnityEngine;

namespace PoiLootVacuum
{
    public class ModApi : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            LoadConfig(_modInstance.Path);
            var go = new GameObject("PoiLootVacuum");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<LootVacuumBehaviour>();
        }

        static void LoadConfig(string modPath)
        {
            try
            {
                string path = Path.Combine(modPath, "Config", "PoiLootVacuum.xml");
                if (!File.Exists(path)) return;
                var doc = new XmlDocument();
                doc.Load(path);
                var root = doc.DocumentElement;
                if (root == null) return;

                string keyVal = Attr(root, "CollectKey");
                if (keyVal != null && Enum.TryParse<KeyCode>(keyVal, true, out var k))
                    Config.CollectKey = k;

                string magnetKeyVal = Attr(root, "MagnetKey");
                if (magnetKeyVal != null && Enum.TryParse<KeyCode>(magnetKeyVal, true, out var mk))
                    Config.MagnetKey = mk;

                string rad = Attr(root, "Radius");
                if (rad != null && float.TryParse(rad, out float r))
                    Config.Radius = r;

                Config.PickupAll = Bool(root, "PickupAll", Config.PickupAll);

                string dest = Attr(root, "Destination");
                if (dest != null && System.Enum.TryParse<DestinationMode>(dest, true, out var dm))
                    Config.Destination = dm;

                Config.PickupWeapons    = Bool(root, "PickupWeapons",    Config.PickupWeapons);
                Config.PickupTools      = Bool(root, "PickupTools",      Config.PickupTools);
                Config.PickupArmor      = Bool(root, "PickupArmor",      Config.PickupArmor);
                Config.PickupAmmo       = Bool(root, "PickupAmmo",       Config.PickupAmmo);
                Config.PickupFood       = Bool(root, "PickupFood",       Config.PickupFood);
                Config.PickupMedicine   = Bool(root, "PickupMedicine",   Config.PickupMedicine);
                Config.PickupBooks      = Bool(root, "PickupBooks",      Config.PickupBooks);
                Config.PickupReadBooks  = Bool(root, "PickupReadBooks",  Config.PickupReadBooks);
                Config.PickupComponents = Bool(root, "PickupComponents", Config.PickupComponents);
                Config.PickupResources  = Bool(root, "PickupResources",  Config.PickupResources);
                Config.PickupMisc       = Bool(root, "PickupMisc",       Config.PickupMisc);

                var mappings = root.SelectSingleNode("TagMappings");
                if (mappings != null)
                {
                    foreach (XmlNode node in mappings.ChildNodes)
                    {
                        if (node.NodeType != XmlNodeType.Element) continue;
                        string tagName  = node.Attributes?["name"]?.Value;
                        string catName  = node.Attributes?["category"]?.Value;
                        if (tagName == null || catName == null) continue;
                        if (System.Enum.TryParse<LootCategory>(catName, true, out var cat))
                            LootFilter.AddTag(tagName, cat);
                    }
                }
            }
            catch { }
        }

        static string Attr(XmlElement root, string name) =>
            root.SelectSingleNode(name)?.Attributes?["value"]?.Value;

        static bool Bool(XmlElement root, string name, bool fallback)
        {
            string v = Attr(root, name);
            return v != null && bool.TryParse(v, out bool b) ? b : fallback;
        }
    }
}
