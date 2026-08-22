namespace PoiLootVacuum
{
    public enum LootCategory { Weapon, Tool, Armor, Ammo, Food, Medicine, Book, Component, Resource, Misc }

    public static class LootFilter
    {
        static readonly FastTags<Global> T_Weapon    = FastTags<Global>.Parse("weapon");
        static readonly FastTags<Global> T_Tool      = FastTags<Global>.Parse("tool");
        static readonly FastTags<Global> T_Armor     = FastTags<Global>.Parse("armor");
        static readonly FastTags<Global> T_Ammo      = FastTags<Global>.Parse("ammo");
        static readonly FastTags<Global> T_Food      = FastTags<Global>.Parse("food");
        static readonly FastTags<Global> T_Medicine  = FastTags<Global>.Parse("medical");
        static readonly FastTags<Global> T_Book      = FastTags<Global>.Parse("book");
        static readonly FastTags<Global> T_Component = FastTags<Global>.Parse("component");
        static readonly FastTags<Global> T_Resource  = FastTags<Global>.Parse("resource");

        public static LootCategory Classify(ItemClass ic)
        {
            if (ic == null) return LootCategory.Misc;
            var t = ic.Tags;
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

        public static bool IsBookRead(ItemValue iv, EntityPlayer player)
        {
            try
            {
                if (player != null && iv?.ItemClass is ItemClassBook book)
                    return player.RecipeBook != null && player.RecipeBook.HasUnlockedRecipe(book.UnlockedRecipeName);
            }
            catch { }
            return false;
        }
    }
}
