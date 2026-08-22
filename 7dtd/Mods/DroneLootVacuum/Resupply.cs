using System;
using System.Collections.Generic;
using UnityEngine;

namespace DroneLootVacuum;

public static class Resupply
{
	public const int CatAmmo = 0;

	public const int CatMedical = 1;

	private static readonly Dictionary<int, float> nextDeliver = new Dictionary<int, float>();

	private static readonly Dictionary<int, float> nextAsk = new Dictionary<int, float>();

	private static readonly FastTags<Global> AmmoTag = FastTags<Global>.Parse("ammo");

	public static void Tick(EntityDrone drone, EntityPlayer owner)
	{
		try
		{
			if (!((Object)(object)drone == (Object)null) && owner is EntityPlayerLocal && InRange(drone, owner) && Ready(((Entity)drone).entityId))
			{
				Needs(owner, out var weaponType, out var ammoIdx, out var needMedical);
				ServeEither(drone, owner, weaponType, ammoIdx, needMedical);
			}
		}
		catch (Exception ex)
		{
			Log.Error("[DroneLootVacuum] resupply tick: " + ex.Message);
		}
	}

	public static void TickClient(EntityDrone drone)
	{
		try
		{
			if ((Object)(object)drone == (Object)null || (!Config.AmmoRunnerEnabled && !Config.MedicRunnerEnabled))
			{
				return;
			}
			GameManager instance = GameManager.Instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				World world = instance.World;
				obj = ((world != null) ? ((WorldBase)world).GetPrimaryPlayer() : null);
			}
			EntityPlayerLocal val = (EntityPlayerLocal)obj;
			object obj2;
			if (val == null)
			{
				obj2 = null;
			}
			else
			{
				PersistentPlayerData persistentPlayerData = ((EntityPlayer)val).PersistentPlayerData;
				obj2 = ((persistentPlayerData != null) ? persistentPlayerData.PrimaryId : null);
			}
			PlatformUserIdentifierAbs val2 = (PlatformUserIdentifierAbs)obj2;
			if ((Object)(object)val == (Object)null || val2 == null || drone.OwnerID == null || !val2.Equals(drone.OwnerID))
			{
				return;
			}
			int entityId = ((Entity)drone).entityId;
			if ((DroneToggles.AmmoResupply.IsOff(entityId) && DroneToggles.MedicResupply.IsOff(entityId)) || DroneBusy.Is(entityId) || !InRange(drone, (EntityPlayer)(object)val) || (nextAsk.TryGetValue(entityId, out var value) && Time.time < value))
			{
				return;
			}
			Needs((EntityPlayer)(object)val, out var weaponType, out var ammoIdx, out var needMedical);
			if (weaponType != 0 || needMedical)
			{
				nextAsk[entityId] = Time.time + Mathf.Max(0.5f, Config.ResupplyCooldown);
				ConnectionManager instance2 = SingletonMonoBehaviour<ConnectionManager>.Instance;
				if (!((Object)(object)instance2 == (Object)null) && !instance2.IsServer)
				{
					instance2.SendToServer((NetPackage)(object)NetPackageManager.GetPackage<NetPackageDroneResupply>().Setup(entityId, ((Entity)val).entityId, weaponType, ammoIdx, needMedical), false);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error("[DroneLootVacuum] resupply client: " + ex.Message);
		}
	}

	public static void ServeRequest(EntityDrone drone, EntityPlayer owner, int weaponType, int ammoIdx, bool needMedical)
	{
		try
		{
			if (!((Object)(object)drone == (Object)null) && !((Object)(object)owner == (Object)null) && !((Object)(object)DroneOnUpdatePatch.OwnerOf(drone) != (Object)(object)owner) && InRange(drone, owner) && Ready(((Entity)drone).entityId))
			{
				ServeEither(drone, owner, weaponType, ammoIdx, needMedical);
			}
		}
		catch (Exception ex)
		{
			Log.Error("[DroneLootVacuum] resupply serve: " + ex.Message);
		}
	}

	private static void ServeEither(EntityDrone drone, EntityPlayer owner, int weaponType, int ammoIdx, bool needMedical)
	{
		if ((weaponType == 0 || !ServeAmmo(drone, owner, weaponType, ammoIdx)) && needMedical)
		{
			ServeMedical(drone, owner);
		}
	}

	private static bool ServeAmmo(EntityDrone drone, EntityPlayer owner, int weaponType, int ammoIdx)
	{
		ItemClass forId;
		try
		{
			forId = ItemClass.GetForId(weaponType);
		}
		catch
		{
			return false;
		}
		string[] array = ((ItemActionAttack)(((forId != null) ? FindRanged(forId) : null)?)).MagazineItemNames;
		if (array == null || array.Length == 0)
		{
			return false;
		}
		if (ammoIdx < 0 || ammoIdx >= array.Length)
		{
			ammoIdx = 0;
		}
		for (int i = 0; i < array.Length; i++)
		{
			int num = (ammoIdx + i) % array.Length;
			ItemValue item = ItemClass.GetItem(array[num], false);
			if (IsRealAmmo(item) && Serve(drone, owner, 0, item.type))
			{
				return true;
			}
		}
		return false;
	}

	private static bool ServeMedical(EntityDrone drone, EntityPlayer owner)
	{
		string[] medicItems = Config.MedicItems;
		if (medicItems == null)
		{
			return false;
		}
		for (int i = 0; i < medicItems.Length; i++)
		{
			ItemValue item = ItemClass.GetItem(medicItems[i], false);
			if (item != null && !item.IsEmpty() && Serve(drone, owner, 1, item.type))
			{
				return true;
			}
		}
		return false;
	}

	private static bool Serve(EntityDrone drone, EntityPlayer owner, int cat, int itemType)
	{
		if (((cat == 0) ? DroneToggles.AmmoResupply : DroneToggles.MedicResupply).IsOff(((Entity)drone).entityId))
		{
			return false;
		}
		if (DroneBusy.Is(((Entity)drone).entityId))
		{
			return false;
		}
		if (cat == 0 && !Config.AmmoRunnerEnabled)
		{
			return false;
		}
		if (cat == 1 && !Config.MedicRunnerEnabled)
		{
			return false;
		}
		int want = Mathf.Max(1, (cat == 0) ? Config.AmmoRunnerAmount : Config.MedicRunnerAmount);
		int num = TakeFromDrone(drone, itemType, want);
		if (num <= 0)
		{
			return false;
		}
		nextDeliver[((Entity)drone).entityId] = Time.time + Mathf.Max(0.5f, Config.ResupplyCooldown);
		Deliver(owner, itemType, num);
		return true;
	}

	private static bool InRange(EntityDrone drone, EntityPlayer p)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		float radius = Config.Radius;
		return Vector3.SqrMagnitude(((Entity)p).position - ((Entity)drone).position) <= radius * radius;
	}

	private static bool Ready(int droneId)
	{
		if (DroneToggles.AmmoResupply.IsOff(droneId) && DroneToggles.MedicResupply.IsOff(droneId))
		{
			return false;
		}
		if (nextDeliver.TryGetValue(droneId, out var value))
		{
			return Time.time >= value;
		}
		return true;
	}

	private static void Needs(EntityPlayer p, out int weaponType, out int ammoIdx, out bool needMedical)
	{
		weaponType = 0;
		ammoIdx = 0;
		needMedical = false;
		if (Config.AmmoRunnerEnabled)
		{
			Inventory inventory = ((EntityAlive)p).inventory;
			ItemClass val = ((inventory != null) ? inventory.holdingItem : null);
			ItemValue val2 = ((inventory != null) ? inventory.holdingItemItemValue : null);
			if (val != null && val2 != null)
			{
				ItemActionRanged val3 = FindRanged(val);
				if (((ItemActionAttack)(val3?)).MagazineItemNames != null && ((ItemActionAttack)val3).MagazineItemNames.Length != 0)
				{
					int num = val2.SelectedAmmoTypeIndex;
					if (num < 0 || num >= ((ItemActionAttack)val3).MagazineItemNames.Length)
					{
						num = 0;
					}
					ItemValue item = ItemClass.GetItem(((ItemActionAttack)val3).MagazineItemNames[num], false);
					if (IsRealAmmo(item) && CountCarried(p, item.type) <= Config.AmmoRunnerThreshold)
					{
						weaponType = val2.type;
						ammoIdx = num;
					}
				}
			}
		}
		if (!Config.MedicRunnerEnabled)
		{
			return;
		}
		string[] medicItems = Config.MedicItems;
		if (medicItems == null || medicItems.Length == 0)
		{
			return;
		}
		int num2 = 0;
		for (int i = 0; i < medicItems.Length; i++)
		{
			ItemValue item2 = ItemClass.GetItem(medicItems[i], false);
			if (item2 != null && !item2.IsEmpty())
			{
				num2 += CountCarried(p, item2.type);
			}
		}
		if (num2 <= Config.MedicRunnerThreshold)
		{
			needMedical = true;
		}
	}

	private static bool IsRealAmmo(ItemValue ammo)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		if (ammo != null && !ammo.IsEmpty() && ammo.ItemClass != null)
		{
			return ammo.ItemClass.ItemTags.Test_AnySet(AmmoTag);
		}
		return false;
	}

	private static ItemActionRanged FindRanged(ItemClass ic)
	{
		ItemAction[] actions = ic.Actions;
		if (actions == null)
		{
			return null;
		}
		foreach (ItemAction obj in actions)
		{
			ItemActionRanged val = (ItemActionRanged)(object)((obj is ItemActionRanged) ? obj : null);
			if (val != null)
			{
				return val;
			}
		}
		return null;
	}

	private static int CountCarried(EntityPlayer p, int itemType)
	{
		int num = 0;
		try
		{
			Bag bag = ((Entity)p).bag;
			ItemStack[] array = ((bag != null) ? bag.GetSlots() : null);
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] != null && !array[i].IsEmpty() && array[i].itemValue.type == itemType)
					{
						num += array[i].count;
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			Inventory inventory = ((EntityAlive)p).inventory;
			if (inventory != null)
			{
				for (int j = 0; j < inventory.PUBLIC_SLOTS; j++)
				{
					ItemStack itemStack = inventory.GetItemStack(j);
					if (itemStack != null && !itemStack.IsEmpty() && itemStack.itemValue.type == itemType)
					{
						num += itemStack.count;
					}
				}
			}
		}
		catch
		{
		}
		return num;
	}

	private static int TakeFromDrone(EntityDrone drone, int itemType, int want)
	{
		Bag bag = ((Entity)drone).bag;
		ItemStack[] array = ((bag != null) ? bag.GetSlots() : null);
		if (array == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			if (num >= want)
			{
				break;
			}
			if (array[i] != null && !array[i].IsEmpty() && array[i].itemValue.type == itemType)
			{
				int num2 = Mathf.Min(array[i].count, want - num);
				ItemStack obj = array[i];
				obj.count -= num2;
				if (array[i].count <= 0)
				{
					array[i] = ItemStack.Empty.Clone();
				}
				num += num2;
			}
		}
		if (num > 0)
		{
			bag.SetSlots(array);
			drone.SendSyncData((ushort)8);
		}
		return num;
	}

	private static void Deliver(EntityPlayer p, int itemType, int count)
	{
		if (p is EntityPlayerLocal)
		{
			GiveLocal(p, itemType, count);
			return;
		}
		ConnectionManager instance = SingletonMonoBehaviour<ConnectionManager>.Instance;
		if (!((Object)(object)instance == (Object)null))
		{
			instance.SendPackage((NetPackage)(object)NetPackageManager.GetPackage<NetPackageDroneGiveItem>().Setup(((Entity)p).entityId, itemType, count), false, ((Entity)p).entityId, -1, -1, (Vector3?)null, 192, false);
		}
	}

	public static void GiveLocal(EntityPlayer p, int itemType, int count)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		ItemValue val = new ItemValue(itemType, false);
		if (val.IsEmpty() || count <= 0)
		{
			return;
		}
		int num = 500;
		try
		{
			if (val.ItemClass != null)
			{
				num = Mathf.Max(1, val.ItemClass.Stacknumber.Value);
			}
		}
		catch
		{
		}
		while (count > num)
		{
			GiveOne(p, val, num);
			count -= num;
		}
		GiveOne(p, val, count);
	}

	private static void GiveOne(EntityPlayer p, ItemValue _iv, int count)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected O, but got Unknown
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		ItemValue val = _iv.Clone();
		ItemStack val2 = new ItemStack(val, count);
		bool flag = false;
		try
		{
			int num = default(int);
			flag = ((EntityAlive)p).inventory != null && ((EntityAlive)p).inventory.AddItem(val2, ref num);
		}
		catch
		{
		}
		if (!flag)
		{
			try
			{
				flag = ((Entity)p).bag != null && ((InventoryBase)((Entity)p).bag).AddItem(val2);
			}
			catch
			{
			}
		}
		if (!flag)
		{
			try
			{
				GameManager.Instance.ItemDropServer(val2, ((Entity)p).position, Vector3.zero, ((Entity)p).entityId, 60f, false);
			}
			catch (Exception ex)
			{
				Log.Warning("[DroneLootVacuum] resupply drop failed: " + ex.Message);
			}
		}
		try
		{
			string arg = ((val.ItemClass != null) ? val.ItemClass.GetLocalizedItemName() : "item");
			GameManager.ShowTooltipMP(p, $"Drone resupply: {arg} x{count}", "");
		}
		catch
		{
		}
	}
}
