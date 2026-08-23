using System.Text;
using UnityEngine;

namespace EntityLabels
{
    public class EntityLabelBehaviour : MonoBehaviour
    {
        bool _visible;
        GUIStyle _fg;
        GUIStyle _shadow;

        void Update()
        {
            if (Input.GetKeyDown(Config.ToggleKey))
                _visible = !_visible;
        }

        void OnGUI()
        {
            if (!_visible) return;
            if (Event.current.type != EventType.Repaint) return;

            EnsureStyles();

            var world = GameManager.Instance?.World;
            if (world == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            var local = world.GetPrimaryPlayer();
            if (local == null) return;

            _fg.fontSize     = Config.FontSize;
            _shadow.fontSize = Config.FontSize;

            var entities = world.Entities.list;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i] as EntityAlive;
                if (entity == null || !entity.IsAlive()) continue;
                if (entity.entityId == local.entityId) continue;

                var cat = EntityClassifier.Classify(entity);
                if (!ShouldShow(cat)) continue;

                float dist = Vector3.Distance(local.position, entity.position);
                if (dist > Config.Radius) continue;

                // Project to screen; skip if behind the camera
                var worldPt  = entity.position + new Vector3(0f, HeadOffset(entity), 0f);
                var screenPt = cam.WorldToScreenPoint(worldPt);
                if (screenPt.z < 0f) continue;

                float sx = screenPt.x;
                float sy = Screen.height - screenPt.y; // flip Y for GUI coords

                string text = BuildLabel(entity, cat, dist);
                _fg.normal.textColor = GetColor(cat);

                float w = 260f;
                float h = Config.FontSize + 6f;
                float rx = sx - w * 0.5f;
                float ry = sy - h * 0.5f;

                GUI.Label(new Rect(rx + 1f, ry + 1f, w, h), text, _shadow);
                GUI.Label(new Rect(rx,       ry,      w, h), text, _fg);
            }
        }

        void EnsureStyles()
        {
            if (_fg != null) return;
            _fg               = new GUIStyle(GUI.skin.label);
            _fg.alignment     = TextAnchor.MiddleCenter;
            _shadow           = new GUIStyle(GUI.skin.label);
            _shadow.alignment = TextAnchor.MiddleCenter;
            _shadow.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
        }

        static float HeadOffset(EntityAlive entity)
        {
            if (entity is EntityPlayer) return 2.5f;
            if (entity is EntityAnimal) return 1.6f;
            return 2.1f;
        }

        static string BuildLabel(EntityAlive entity, EntityCategory cat, float dist)
        {
            var sb = new StringBuilder();

            if (Config.ShowType)
            {
                switch (cat)
                {
                    case EntityCategory.Player:   sb.Append("[P] ");  break;
                    case EntityCategory.Trader:   sb.Append("[T] ");  break;
                    case EntityCategory.Animal:   sb.Append("[A] ");  break;
                    case EntityCategory.Zombie:   sb.Append("[Z] ");  break;
                    case EntityCategory.MiniBoss: sb.Append("[MB] "); break;
                    case EntityCategory.Boss:     sb.Append("[B] ");  break;
                }
            }

            sb.Append(GetDisplayName(entity));

            if (Config.ShowHealth)
                sb.Append($" {entity.Health}/{entity.GetMaxHealth()}");

            if (Config.ShowDist)
                sb.Append($" {dist:F0}m");

            return sb.ToString();
        }

        static string GetDisplayName(EntityAlive entity)
        {
            string key = entity.EntityName ?? "";
            if (string.IsNullOrEmpty(key)) return "";
            if (entity is EntityPlayer)
                return key; // player names are not localization keys
            try
            {
                string loc = Localization.Get(key);
                return !string.IsNullOrEmpty(loc) ? loc : key;
            }
            catch { return key; }
        }

        static bool ShouldShow(EntityCategory cat)
        {
            switch (cat)
            {
                case EntityCategory.Player:   return Config.ShowPlayers;
                case EntityCategory.Trader:   return Config.ShowTraders;
                case EntityCategory.Animal:   return Config.ShowAnimals;
                case EntityCategory.Zombie:   return Config.ShowZombies;
                case EntityCategory.MiniBoss: return Config.ShowMiniBosses;
                case EntityCategory.Boss:     return Config.ShowBosses;
                default: return true;
            }
        }

        static Color GetColor(EntityCategory cat)
        {
            switch (cat)
            {
                case EntityCategory.Player:   return Config.PlayerColor;
                case EntityCategory.Trader:   return Config.TraderColor;
                case EntityCategory.Animal:   return Config.AnimalColor;
                case EntityCategory.Zombie:   return Config.ZombieColor;
                case EntityCategory.MiniBoss: return Config.MiniBossColor;
                case EntityCategory.Boss:     return Config.BossColor;
                default: return Color.white;
            }
        }
    }
}
