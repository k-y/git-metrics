using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EntityLabels
{
    public class EntityLabelBehaviour : MonoBehaviour
    {
        static readonly Dictionary<int, NavObject> _navObjects = new Dictionary<int, NavObject>();
        bool _visible;

        void Start()
        {
            StartCoroutine(UpdateLoop());
        }

        void Update()
        {
            if (!Input.GetKeyDown(Config.ToggleKey)) return;
            _visible = !_visible;
            if (!_visible)
                ClearAll();
        }

        IEnumerator UpdateLoop()
        {
            yield return new WaitForSeconds(3f);
            while (true)
            {
                if (_visible)
                {
                    var world = GameManager.Instance?.World;
                    var local = world != null ? ((WorldBase)world).GetPrimaryPlayer() : null;
                    if (local != null)
                        Refresh(local);
                }
                yield return new WaitForSeconds(Config.UpdateInterval);
            }
        }

        static void Refresh(EntityPlayerLocal player)
        {
            if (NavObjectManager.Instance == null) return;

            ClearAll();

            var entities = player.world?.Entities?.dict;
            if (entities == null) return;

            foreach (var kv in entities)
            {
                var entity = kv.Value as EntityAlive;
                if (entity == null) continue;
                if (entity.entityId == player.entityId) continue;
                if (!entity.IsAlive() || entity.IsDespawned) continue;

                var cat = EntityClassifier.Classify(entity);
                if (!ShouldShow(cat)) continue;

                float dist = Vector3.Distance(player.position, entity.position);
                if (dist > Config.Radius) continue;

                Register(entity, cat, dist);
            }
        }

        static void Register(EntityAlive entity, EntityCategory cat, float dist)
        {
            if (NavObjectManager.Instance == null) return;
            string sprite = GetSprite(cat);
            var nav = NavObjectManager.Instance.RegisterNavObject("quest", (Entity)(object)entity, sprite, !Config.ShowCompass);
            if (nav == null) return;

            _navObjects[entity.entityId] = nav;
            nav.name               = BuildLabel(entity, cat, dist);
            nav.usingLocalizationId= false;
            nav.hiddenOnCompass    = !Config.ShowCompass;
            nav.hiddenOnMap        = !Config.ShowMap;
            nav.UseOverrideColor   = true;
            nav.OverrideColor      = GetColor(cat);

            if (nav.CurrentScreenSettings != null)
            {
                nav.CurrentScreenSettings.MaxDistance  = Config.Radius;
                nav.CurrentScreenSettings.MinDistance  = 0f;
                nav.CurrentScreenSettings.ShowTextType = (ShowTextTypes)2;
            }
        }

        static void ClearAll()
        {
            if (NavObjectManager.Instance != null)
                foreach (var nav in _navObjects.Values)
                    NavObjectManager.Instance.UnRegisterNavObject(nav);
            _navObjects.Clear();
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
                case EntityCategory.Player:   return "ui_game_symbol_player";
                case EntityCategory.Trader:   return "ui_game_symbol_trader";
                case EntityCategory.Animal:   return "ui_game_symbol_animal";
                case EntityCategory.Boss:
                case EntityCategory.MiniBoss: return "ui_game_symbol_enemy";
                default:                      return "ui_game_symbol_zombie";
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
