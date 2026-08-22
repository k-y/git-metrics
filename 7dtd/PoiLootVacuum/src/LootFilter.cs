namespace PoiLootVacuum
{
    public enum LootCategory { Weapon, Tool, Armor, Ammo, Food, Medicine, Book, Component, Resource, Misc }

    public static class LootFilter
    {
        static readonly FastTags<TagGroup.Global> T_Weapon    = FastTags<TagGroup.Global>.Parse("weapon");
        static readonly FastTags<TagGroup.Global> T_Tool      = FastTags<TagGroup.Global>.Parse("tool");
        static readonly FastTags<TagGroup.Global> T_Armor     = FastTags<TagGroup.Global>.Parse("armor");
        static readonly FastTags<TagGroup.Global> T_Ammo      = FastTags<TagGroup.Global>.Parse("ammo");
        static readonly FastTags<TagGroup.Global> T_Food      = FastTags<TagGroup.Global>.Parse("food");
        static readonly FastTags<TagGroup.Global> T_Medicine  = FastTags<TagGroup.Global>.Parse("medical");
        static readonly FastTags<TagGroup.Global> T_Book      = FastTags<TagGroup.Global>.Parse("book");
        static readonly FastTags<TagGroup.Global> T_Component = FastTags<TagGroup.Global>.Parse("component");
        static readonly FastTags<TagGroup.Global> T_Resource  = FastTags<TagGroup.Global>.Parse("resource");

        public static LootCategory Classify(ItemClass ic)
        {
            if (ic == null) return LootCategory.Misc;
            var t = ic.ItemTags;
            if (t.Test_AnySet(T_Weapon))    return LootCategory.Weapon;
            if (t.Test_AnySet(T_Tool))      return LootCategory.Tool;
            if (t.Test_AnySet(T_Armor))     return LootCategory.Armor;
            if (t.Test_AnySet(T_Ammo))      return LootCategory.Ammo;
            if (t.Test_AnySet(T_Food))      return LootCategory.Food;
            if (t.Test_AnySet(T_Medicine))  return LootCategory.Medicine;
            if (t.Test_AnySet(T_Book))      return LootCategory.Book;
            if (t.Test_AnySet(T_Component)) return LootCategory.Component;
            if (t.Test_AnySet(T_Resource))  return LootCategory.Resource;
            return LootCategory.Misc;
        }

        public static bool ShouldPickUp(ItemStack stack, EntityPlayer player = null)
        {
            if (stack == null || stack.IsEmpty()) return false;
            var ic = stack.itemValue?.ItemClass;
            if (ic == null) return false;
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

        // Returns true if the player has already read this book.
        // Stubbed — returns false conservatively so all books are picked up.
        public static bool IsBookRead(ItemValue iv, EntityPlayer player) => false;
    }
}
