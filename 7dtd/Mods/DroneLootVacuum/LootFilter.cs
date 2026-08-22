using System;
using UnityEngine;

namespace DroneLootVacuum;

public static class LootFilter
{
	public enum Cat
	{
		Ammo,
		Weapons,
		Tools,
		Armor,
		Clothing,
		Medical,
		Food,
		Drinks,
		Books,
		Mods,
		Seeds,
		Ores,
		Resources,
		Building,
		Chemicals,
		Misc
	}

	private static readonly FastTags<Global> T_ammo = FastTags<Global>.Parse("ammo");

	private static readonly FastTags<Global> T_weapon = FastTags<Global>.Parse("weapon");

	private static readonly FastTags<Global> T_tool = FastTags<Global>.Parse("tool,miningTool,salvageTool,motorTool,harvestingTool");

	private static readonly FastTags<Global> T_armor = FastTags<Global>.Parse("armor");

	private static readonly FastTags<Global> T_clothing = FastTags<Global>.Parse("clothing");

	private static readonly FastTags<Global> T_medical = FastTags<Global>.Parse("medical");

	private static readonly FastTags<Global> T_food = FastTags<Global>.Parse("food");

	private static readonly FastTags<Global> T_drinks = FastTags<Global>.Parse("drinks");

	private static readonly FastTags<Global> T_books = FastTags<Global>.Parse("books");

	private static readonly FastTags<Global> T_csm = FastTags<Global>.Parse("csm");

	private static bool Tag(ItemClass ic, FastTags<Global> t)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return ic.ItemTags.Test_AnySet(t);
	}

	private static bool Grp(ItemClass ic, string g)
	{
		string[] groups = ic.Groups;
		if (groups == null)
		{
			return false;
		}
		for (int i = 0; i < groups.Length; i++)
		{
			if (string.Equals(groups[i], g, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static bool NameHas(string n, string sub)
	{
		return n.IndexOf(sub, StringComparison.Ordinal) >= 0;
	}

	public static Cat Classify(ItemClass ic)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		string text = ic.GetItemName()?.ToLowerInvariant() ?? "";
		if (Tag(ic, T_books) || Grp(ic, "Books") || Grp(ic, "BooksOnly") || Grp(ic, "TCReading") || Grp(ic, "SchematicsOnly"))
		{
			return Cat.Books;
		}
		if (Tag(ic, T_ammo))
		{
			return Cat.Ammo;
		}
		if (Tag(ic, T_weapon) || Grp(ic, "Ranged Weapons") || Grp(ic, "Melee Weapons"))
		{
			return Cat.Weapons;
		}
		if (Tag(ic, T_tool) || Grp(ic, "Tools/Traps"))
		{
			return Cat.Tools;
		}
		if (Tag(ic, T_armor) || Grp(ic, "Armor") || Grp(ic, "TCArmor"))
		{
			return Cat.Armor;
		}
		if (Tag(ic, T_clothing) || Grp(ic, "Clothing"))
		{
			return Cat.Clothing;
		}
		if (Tag(ic, T_medical) || Grp(ic, "Medical") || Grp(ic, "TCMedical") || Grp(ic, "CFMedical"))
		{
			return Cat.Medical;
		}
		if (Tag(ic, T_drinks) || Grp(ic, "CFDrink/Cooking"))
		{
			return Cat.Drinks;
		}
		if (Tag(ic, T_food) || Grp(ic, "Food/Cooking") || Grp(ic, "CFFood/Cooking"))
		{
			return Cat.Food;
		}
		if (Grp(ic, "Chemicals") || Grp(ic, "CFChemicals") || Grp(ic, "Science"))
		{
			return Cat.Chemicals;
		}
		if (text.StartsWith("mod", StringComparison.Ordinal) && !ic.IsBlock())
		{
			return Cat.Mods;
		}
		if (NameHas(text, "seed"))
		{
			return Cat.Seeds;
		}
		if (NameHas(text, "coal") || NameHas(text, "oilshale") || NameHas(text, "lead") || NameHas(text, "nitrate") || NameHas(text, "ore"))
		{
			return Cat.Ores;
		}
		if (text.StartsWith("resource", StringComparison.Ordinal) || Grp(ic, "Resources"))
		{
			return Cat.Resources;
		}
		if (ic.IsBlock() || Grp(ic, "Building") || Grp(ic, "advBuilding"))
		{
			return Cat.Building;
		}
		return Cat.Misc;
	}

	private static bool Allowed(Cat c)
	{
		return c switch
		{
			Cat.Ammo => Config.PickupAmmo, 
			Cat.Weapons => Config.PickupWeapons, 
			Cat.Tools => Config.PickupTools, 
			Cat.Armor => Config.PickupArmor, 
			Cat.Clothing => Config.PickupClothing, 
			Cat.Medical => Config.PickupMedical, 
			Cat.Food => Config.PickupFood, 
			Cat.Drinks => Config.PickupDrinks, 
			Cat.Books => Config.PickupBooks, 
			Cat.Mods => Config.PickupMods, 
			Cat.Seeds => Config.PickupSeeds, 
			Cat.Ores => Config.PickupOres, 
			Cat.Resources => Config.PickupResources, 
			Cat.Building => Config.PickupBuilding, 
			Cat.Chemicals => Config.PickupChemicals, 
			_ => Config.PickupMisc, 
		};
	}

	public static bool ShouldPickUp(ItemStack stack, EntityPlayer owner)
	{
		if (stack == null || stack.IsEmpty())
		{
			return false;
		}
		ItemValue itemValue = stack.itemValue;
		ItemClass val = ((itemValue != null) ? itemValue.ItemClass : null);
		if (val == null)
		{
			return true;
		}
		Cat cat = Classify(val);
		if (!Allowed(cat))
		{
			return false;
		}
		if (cat == Cat.Books && !Config.PickupReadBooks && (Object)(object)owner != (Object)null && IsBookRead(val, owner))
		{
			return false;
		}
		return true;
	}

	private static bool IsBookRead(ItemClass ic, EntityPlayer owner)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (ic.ItemTags.Test_AnySet(T_csm))
			{
				return false;
			}
			if (ic.Properties == null || !ic.Properties.Values.ContainsKey("Unlocks"))
			{
				return false;
			}
			string text = ic.Properties.Values["Unlocks"];
			if (string.IsNullOrEmpty(text) || (Object)(object)owner == (Object)null)
			{
				return false;
			}
			string[] array = text.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Trim();
				if (text2.Length != 0)
				{
					Progression progression = ((EntityAlive)owner).Progression;
					ProgressionValue val = ((progression != null) ? progression.GetProgressionValue(text2) : null);
					if (val != null && val.Level > 0)
					{
						return true;
					}
					Recipe recipe = CraftingManager.GetRecipe(text2);
					if (recipe != null && recipe.IsUnlocked(owner))
					{
						return true;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warning("[DroneLootVacuum] read-check: " + ex.Message);
		}
		return false;
	}
}
