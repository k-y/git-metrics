using System;
using System.IO;
using System.Xml;
using UnityEngine;

namespace EntityLabels
{
    public class ModApi : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            LoadConfig(_modInstance.Path);
            var go = new GameObject("EntityLabels");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<EntityLabelBehaviour>();
        }

        static void LoadConfig(string modPath)
        {
            try
            {
                string path = Path.Combine(modPath, "Config", "EntityLabels.xml");
                if (!File.Exists(path)) return;
                var doc = new XmlDocument();
                doc.Load(path);
                var root = doc.DocumentElement;
                if (root == null) return;

                string kv = Attr(root, "ToggleKey");
                if (kv != null && Enum.TryParse<KeyCode>(kv, true, out var k))
                    Config.ToggleKey = k;

                string rv = Attr(root, "Radius");
                if (rv != null && float.TryParse(rv, out float r)) Config.Radius = r;

                string uv = Attr(root, "UpdateInterval");
                if (uv != null && float.TryParse(uv, out float u)) Config.UpdateInterval = u;

                Config.ShowType     = Bool(root, "ShowType",     Config.ShowType);
                Config.ShowHealth   = Bool(root, "ShowHealth",   Config.ShowHealth);
                Config.ShowDist     = Bool(root, "ShowDist",     Config.ShowDist);
                Config.ShowCompass  = Bool(root, "ShowCompass",  Config.ShowCompass);
                Config.ShowMap      = Bool(root, "ShowMap",      Config.ShowMap);

                TryColor(root, "PlayerColor",   ref Config.PlayerColor);
                TryColor(root, "TraderColor",   ref Config.TraderColor);
                TryColor(root, "AnimalColor",   ref Config.AnimalColor);
                TryColor(root, "ZombieColor",   ref Config.ZombieColor);
                TryColor(root, "MiniBossColor", ref Config.MiniBossColor);
                TryColor(root, "BossColor",     ref Config.BossColor);

                Config.ShowPlayers    = Bool(root, "ShowPlayers",    Config.ShowPlayers);
                Config.ShowTraders    = Bool(root, "ShowTraders",    Config.ShowTraders);
                Config.ShowAnimals    = Bool(root, "ShowAnimals",    Config.ShowAnimals);
                Config.ShowZombies    = Bool(root, "ShowZombies",    Config.ShowZombies);
                Config.ShowMiniBosses = Bool(root, "ShowMiniBosses", Config.ShowMiniBosses);
                Config.ShowBosses     = Bool(root, "ShowBosses",     Config.ShowBosses);
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

        static void TryColor(XmlElement root, string name, ref Color field)
        {
            string v = Attr(root, name);
            if (v == null) return;
            var p = v.Split(',');
            if (p.Length >= 3
                && float.TryParse(p[0].Trim(), out float r)
                && float.TryParse(p[1].Trim(), out float g)
                && float.TryParse(p[2].Trim(), out float b))
            {
                float a = p.Length >= 4 && float.TryParse(p[3].Trim(), out float pa) ? pa : 1f;
                field = new Color(r, g, b, a);
            }
        }
    }
}
