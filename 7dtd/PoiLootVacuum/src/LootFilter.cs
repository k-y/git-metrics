using System.Collections.Generic;

namespace PoiLootVacuum
{
    public enum LootCategory { Weapon, Tool, Armor, Ammo, Food, Medicine, Book, Component, Resource, Misc }

    public static class LootFilter
    {
        // Default tag-to-category mappings; extended at startup from config XML.
        static readonly Dictionary<LootCategory, List<string>> TagMap = new Dictionary<LootCategory, List<string>>
        {
            { LootCategory.Weapon,    new List<string> { "weapon" } },
            { LootCategory.Tool,      new List<string> { "tool" } },
            { LootCategory.Armor,     new List<string> { "armor" } },
            { LootCategory.Ammo,      new List<string> { "ammo" } },
            { LootCategory.Food,      new List<string> { "food" } },
            { LootCategory.Medicine,  new List<string> { "medical" } },
            { LootCategory.Book,      new List<string> { "books", "csm" } },
            { LootCategory.Component, new List<string> { "component" } },
            { LootCategory.Resource,  new List<string> { "resource", "junk" } },
        };

        // Compiled from TagMap; rebuilt by BuildTagSets() after any AddTag call.
        static Dictionary<LootCategory, FastTags<TagGroup.Global>> _compiled;

        static LootFilter() => BuildTagSets();

        public static void AddTag(string tagName, LootCategory category)
        {
            if (!TagMap.TryGetValue(category, out var list))
            {
                list = new List<string>();
                TagMap[category] = list;
            }
            if (!list.Contains(tagName))
                list.Add(tagName);
            BuildTagSets();
        }

        static void BuildTagSets()
        {
            _compiled = new Dictionary<LootCategory, FastTags<TagGroup.Global>>();
            foreach (var kvp in TagMap)
                _compiled[kvp.Key] = FastTags<TagGroup.Global>.Parse(string.Join(",", kvp.Value));
        }

        public static LootCategory Classify(ItemClass ic)
        {
            if (ic == null) return LootCategory.Misc;
            var t = ic.ItemTags;
            foreach (var kvp in _compiled)
                if (t.Test_AnySet(kvp.Value)) return kvp.Key;
            return LootCategory.Misc;
        }

        public static bool ShouldPickUp(ItemStack stack, EntityPlayer player = null)
        {
            if (stack == null || stack.IsEmpty()) return false;
            var ic = stack.itemValue?.ItemClass;
            if (ic == null) return false;
            if (Config.PickupAll) return true;
            switch (Classify(ic))
            {
                case LootCategory.Weapon:    return Config.PickupWeapons;
                case LootCategory.Tool:      return Config.PickupTools;
                case LootCategory.Armor:     return Config.PickupArmor;
                case LootCategory.Ammo:      return Config.PickupAmmo;
                case LootCategory.Food:      return Config.PickupFood;
                case LootCategory.Medicine:  return Config.PickupMedicine;
                case LootCategory.Book:
                    if (!Config.PickupBooks) return false;
                    if (!Config.PickupReadBooks && IsBookRead(stack.itemValue, player)) return false;
                    return true;
                case LootCategory.Component: return Config.PickupComponents;
                case LootCategory.Resource:  return Config.PickupResources;
                default:                     return Config.PickupMisc;
            }
        }

        // Stubbed — returns false conservatively so all books are picked up.
        public static bool IsBookRead(ItemValue iv, EntityPlayer player) => false;
    }
}
