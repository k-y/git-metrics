using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EntityLabels
{
    public class EntityLabelBehaviour : MonoBehaviour
    {
        static readonly Dictionary<int, NavObject> _navObjects = new Dictionary<int, NavObject>();

        bool _visible;
        GUIStyle _fg;
        GUIStyle _shadow;

        void Start()
        {
            StartCoroutine(UpdateLoop());
        }

        void Update()
        {
            if (!Input.GetKeyDown(Config.ToggleKey)) return;
            _visible = !_visible;
            if (!_visible) ClearAll();
        }

        // Refresh entity list every UpdateInterval seconds
        IEnumerator UpdateLoop()
        {
            yield return new WaitForSeconds(3f);
            while (true)
            {
                if (_visible)
                {
                    var world = GameManager.Instance?.World;
                    var local = world != null ? ((WorldBase)world).GetPrimaryPlayer() : null;
                    if (local != null) Refresh(local);
                }
                yield return new WaitForSeconds(Config.UpdateInterval);
            }
        }

        // --- NavObject management (compass + map markers) ----------------------

        static void Refresh(EntityPlayerLocal player)
        {
            if (NavObjectManager.Instance == null) return;
            ClearAll();
            var entities = player.world?.Entities?.dict;
            if (entities == null) return;
            foreach (var kv in entities)
            {
                var entity = kv.Value as EntityAlive;
                if (entity == null || entity.entityId == player.entityId) continue;
                if (!entity.IsAlive() || entity.IsDespawned) continue;
                var cat = EntityClassifier.Classify(entity);
                if (!ShouldShow(cat)) continue;
                float dist = Vector3.Distance(player.position, entity.position);
                if (dist > Config.Radius) continue;
                RegisterNav(entity, cat);
            }
        }

        static void RegisterNav(EntityAlive entity, EntityCategory cat)
        {
            if (NavObjectManager.Instance == null) return;
            var nav = NavObjectManager.Instance.RegisterNavObject(
                "quest", (Entity)(object)entity, GetSprite(cat), !Config.ShowCompass);
            if (nav == null) return;
            _navObjects[entity.entityId] = nav;
            nav.name                = "";        // text drawn by OnGUI instead
            nav.usingLocalizationId = false;
            nav.hiddenOnCompass     = !Config.ShowCompass;
            nav.hiddenOnMap         = !Config.ShowMap;
            nav.UseOverrideColor    = true;
            nav.OverrideColor       = GetColor(cat);
            if (nav.CurrentScreenSettings != null)
            {
                nav.CurrentScreenSettings.MaxDistance = Config.Radius;
                nav.CurrentScreenSettings.MinDistance = 0f;
                // ShowTextType enum name changed in 1.x; suppress via reflection
                try
                {
                    var prop = nav.CurrentScreenSettings.GetType().GetProperty("ShowTextType");
                    if (prop != null)
                        prop.SetValue(nav.CurrentScreenSettings,
                            System.Enum.ToObject(prop.PropertyType, 0));
                }
                catch { }
            }
        }

        static void ClearAll()
        {
            if (NavObjectManager.Instance != null)
                foreach (var nav in _navObjects.Values)
                    NavObjectManager.Instance.UnRegisterNavObject(nav);
            _navObjects.Clear();
        }

        // --- OnGUI small-font text labels via cameraPoint projection -----------

        void OnGUI()
        {
            if (!_visible) return;
            if (Event.current.type != EventType.Repaint) return;
            EnsureStyles();

            var world = GameManager.Instance?.World;
            if (world == null) return;
            var local = ((WorldBase)world).GetPrimaryPlayer();
            if (local == null) return;

            // Build the camera basis from the player's position and look vector —
            // avoids needing Camera.main (wrong position) or cameraPoint (not in 1.x)
            float fovDeg = Config.FieldOfView;
            try { var mc = Camera.main; if (mc != null && mc.fieldOfView > 1f) fovDeg = mc.fieldOfView; } catch { }

            float tanHalfV = Mathf.Tan(fovDeg * 0.5f * Mathf.Deg2Rad);
            float tanHalfH = tanHalfV * ((float)Screen.width / Screen.height);

            _fg.fontSize     = Config.FontSize;
            _shadow.fontSize = Config.FontSize;

            Vector3 camPos = local.position + new Vector3(0f, 1.65f, 0f);
            Quaternion camRot = Quaternion.LookRotation(local.GetLookVector(), Vector3.up);
            Vector3 camFwd   = camRot * Vector3.forward;
            Vector3 camRight = camRot * Vector3.right;
            Vector3 camUp    = camRot * Vector3.up;

            var entities = local.world?.Entities?.dict;
            if (entities == null) return;
            foreach (var kv in entities)
            {
                var entity = kv.Value as EntityAlive;
                if (entity == null || !entity.IsAlive() || entity.IsDespawned) continue;
                if (entity.entityId == local.entityId) continue;

                var cat = EntityClassifier.Classify(entity);
                if (!ShouldShow(cat)) continue;

                float dist = Vector3.Distance(local.position, entity.position);
                if (dist > Config.Radius) continue;

                var worldPt  = entity.position + new Vector3(0f, HeadOffset(entity), 0f);
                var toEntity = worldPt - camPos;

                float dotZ = Vector3.Dot(toEntity, camFwd);
                if (dotZ < 0.1f) continue; // behind the camera
                float dotX = Vector3.Dot(toEntity, camRight);
                float dotY = Vector3.Dot(toEntity, camUp);

                float sx = (dotX / dotZ / tanHalfH * 0.5f + 0.5f) * Screen.width;
                float sy = (1f - (dotY / dotZ / tanHalfV * 0.5f + 0.5f)) * Screen.height;
                if (sx < -300 || sx > Screen.width + 300 || sy < -50 || sy > Screen.height + 50) continue;

                string text = BuildLabel(entity, cat, dist);
                _fg.normal.textColor = GetColor(cat);

                float w = 260f, h = Config.FontSize + 6f;
                float rx = sx - w * 0.5f, ry = sy - h * 0.5f;
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

        // --- Helpers -----------------------------------------------------------

        static float HeadOffset(EntityAlive entity)
        {
            if (entity is EntityPlayer) return 2.5f;
            if (entity is EntityAnimal) return 1.6f;
            return 2.1f;
        }

        static string BuildLabel(EntityAlive entity, EntityCategory cat, float dist)
        {
            var sb = new System.Text.StringBuilder();
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
            string name = entity.GetDebugName();
            if (string.IsNullOrEmpty(name))
                name = entity.EntityClass?.entityClassName ?? "";
            sb.Append(name);
            if (Config.ShowHealth)
                sb.Append($" {entity.Health}/{entity.GetMaxHealth()}");
            if (Config.ShowDist)
                sb.Append($" {dist:F0}m");
            return sb.ToString();
        }

        static string GetSprite(EntityCategory cat)
        {
            switch (cat)
            {
                case EntityCategory.Player:          return "ui_game_symbol_player";
                case EntityCategory.Trader:          return "ui_game_symbol_trader";
                case EntityCategory.Animal:          return "ui_game_symbol_animal";
                case EntityCategory.Boss:
                case EntityCategory.MiniBoss:        return "ui_game_symbol_enemy";
                default:                             return "ui_game_symbol_zombie";
            }
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
