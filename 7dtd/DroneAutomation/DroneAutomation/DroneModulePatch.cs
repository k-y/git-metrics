using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using UnityEngine;
using XMLData.Item;

namespace DroneAutomation;

[HarmonyPatch(typeof(EntityDrone), "OnUpdateEntity")]
public static class DroneModulePatch
{
	private struct MenuClaim
	{
		public int DroneId;

		public ulong Tick;

		public float DistSq;
	}

	private static readonly ConditionalWeakTable<EntityDrone, VacuumCore> autoLootCores = new ConditionalWeakTable<EntityDrone, VacuumCore>();

	private static readonly ConditionalWeakTable<EntityDrone, SalvageCore> salvageCores = new ConditionalWeakTable<EntityDrone, SalvageCore>();

	private static readonly ConditionalWeakTable<EntityDrone, HarvestCore> harvestCores = new ConditionalWeakTable<EntityDrone, HarvestCore>();

	private static readonly ConditionalWeakTable<EntityDrone, RepairCore> repairCores = new ConditionalWeakTable<EntityDrone, RepairCore>();

	private static readonly ConditionalWeakTable<EntityDrone, PlantCore> plantCores = new ConditionalWeakTable<EntityDrone, PlantCore>();

	private static readonly ConditionalWeakTable<EntityDrone, DefenseCore> defenseCores = new ConditionalWeakTable<EntityDrone, DefenseCore>();

	private const float MenuPublishRange = 8f;

	private const ulong MenuClaimStaleTicks = 40uL;

	private static readonly Dictionary<int, MenuClaim> menuClaims = new Dictionary<int, MenuClaim>();

	private static ulong lastDebugTick;

	private static ulong lastErrorTick;

	public static void Postfix(EntityDrone __instance)
	{
		try
		{
			Run(__instance);
		}
		catch (Exception arg)
		{
			ulong ticks = GameTimer.Instance.ticks;
			if (ticks < lastErrorTick || ticks - lastErrorTick >= 200)
			{
				lastErrorTick = ticks;
				Log.Error($"[DroneAutomation][drone {((Entity)(__instance?)).entityId}] module tick failed: {arg}");
			}
		}
	}

	private static void Run(EntityDrone __instance)
	{
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Invalid comparison between Unknown and I4
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_0396: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_043b: Unknown result type (might be due to invalid IL or missing references)
		ConnectionManager instance = SingletonMonoBehaviour<ConnectionManager>.Instance;
		if ((Object)(object)instance == (Object)null || !instance.IsServer || (Object)(object)__instance == (Object)null || ((Entity)__instance).IsDead())
		{
			return;
		}
		if (((Entity)__instance).bag == null)
		{
			Debug(__instance, "no bag");
			return;
		}
		ItemValue originalItemValue = __instance.OriginalItemValue;
		ItemValue val = GetModule(originalItemValue, "modRoboticDroneAutoLootMod");
		ItemValue val2 = GetModule(originalItemValue, "modRoboticDroneAutoSalvageMod");
		ItemValue val3 = GetModule(originalItemValue, "modRoboticDroneAutoHarvestMod");
		ItemValue val4 = GetModule(originalItemValue, "modRoboticDroneAutoRepairMod");
		ItemValue val5 = GetModule(originalItemValue, "modRoboticDroneAutoPlantMod");
		ItemValue val6 = GetModule(originalItemValue, "modRoboticDroneAutoDefenseMod");
		ItemValue module = GetModule(originalItemValue, "modRoboticDroneOverclockMod");
		ItemValue module2 = GetModule(originalItemValue, "modRoboticDroneAntennaMod");
		GameManager instance2 = GameManager.Instance;
		World val7 = ((instance2 != null) ? instance2.World : null);
		if (val7 == null)
		{
			return;
		}
		PersistentPlayerData _ownerData;
		EntityPlayer val8 = VacuumCore.ResolveOwner(val7, __instance.OwnerID, out _ownerData);
		if ((Object)(object)val8 == (Object)null)
		{
			PlatformUserIdentifierAbs ownerID = __instance.OwnerID;
			Debug(__instance, "owner not resolved from OwnerID=" + (((ownerID != null) ? ownerID.ReadablePlatformUserIdentifier : null) ?? "null"));
			return;
		}
		PrimeToggleBuffs(val8);
		PublishFittedModules(val8, __instance, val, val2, val3, val4, val5, val6);
		if (val == null && val2 == null && val3 == null && val4 == null && val5 == null && val6 == null)
		{
			Debug(__instance, "no automation module; mods=[" + DescribeMods(originalItemValue) + "]");
			return;
		}
		if (IsSwitchedOff(val8, "daLootOff"))
		{
			val = null;
		}
		if (IsSwitchedOff(val8, "daSalvageOff"))
		{
			val2 = null;
		}
		if (IsSwitchedOff(val8, "daHarvestOff"))
		{
			val3 = null;
		}
		if (IsSwitchedOff(val8, "daRepairOff"))
		{
			val4 = null;
		}
		if (IsSwitchedOff(val8, "daPlantOff"))
		{
			val5 = null;
		}
		if (IsSwitchedOff(val8, "daDefenseOff"))
		{
			val6 = null;
		}
		if (val == null && val2 == null && val3 == null && val4 == null && val5 == null && val6 == null)
		{
			Debug(__instance, "every installed module is switched off in the drone's talk menu");
			return;
		}
		DroneBoost boost = BuildBoost(module, module2);
		bool flag = false;
		string text = null;
		if (val6 != null)
		{
			DefenseCore value = defenseCores.GetValue(__instance, (EntityDrone _) => new DefenseCore(DroneAutomationMod.DefenseSettings));
			flag |= value.Tick(__instance, val6.Quality, boost);
		}
		if (LockManager.Instance.IsLockedServer((ILockTarget)(object)__instance, (ushort)0))
		{
			Debug(__instance, flag ? "acted this pass" : "bag locked (owner has it open)");
			return;
		}
		bool flag2 = (int)__instance.OrderState == 1;
		if (flag2 && !DroneAutomationMod.WorkWhileParked)
		{
			Debug(__instance, "parked, and WorkWhileParked is off");
			return;
		}
		Vector3 scanCenter = (flag2 ? __instance.SentryPos : ((Entity)val8).position);
		bool num = flag2 || IsNearOwner(__instance, val8);
		if (val != null)
		{
			VacuumCore value2 = autoLootCores.GetValue(__instance, (EntityDrone _) => new VacuumCore(DroneAutomationMod.AutoLootSettings));
			flag |= value2.Tick(val7, val8, _ownerData, __instance.OwnerID, new BagSink(((Entity)__instance).bag), ((Entity)__instance).position, val.Quality, boost);
		}
		if (!num)
		{
			Debug(__instance, "following, but too far from owner to work blocks");
		}
		else
		{
			if (val2 != null)
			{
				SalvageCore value3 = salvageCores.GetValue(__instance, (EntityDrone _) => new SalvageCore(DroneAutomationMod.SalvageSettings));
				flag |= value3.Tick(val7, val8, _ownerData, __instance, scanCenter, val2.Quality, boost);
			}
			if (val3 != null)
			{
				HarvestCore value4 = harvestCores.GetValue(__instance, (EntityDrone _) => new HarvestCore(DroneAutomationMod.HarvestSettings));
				flag |= value4.Tick(val7, val8, _ownerData, __instance, scanCenter, val3.Quality, boost);
				text = value4.LastScan;
			}
			if (val4 != null)
			{
				RepairCore value5 = repairCores.GetValue(__instance, (EntityDrone _) => new RepairCore(DroneAutomationMod.RepairSettings));
				flag |= value5.Tick(val7, val8, _ownerData, __instance, scanCenter, val4.Quality, boost);
			}
			if (val5 != null)
			{
				PlantCore value6 = plantCores.GetValue(__instance, (EntityDrone _) => new PlantCore(DroneAutomationMod.PlantSettings));
				flag |= value6.Tick(val7, val8, _ownerData, __instance, scanCenter, val5.Quality, boost);
			}
		}
		Debug(__instance, flag ? "acted this pass" : ("active, nothing in range/afforded yet" + ((text != null) ? ("  ||  harvest: " + text) : "")));
	}

	private static DroneBoost BuildBoost(ItemValue _overclock, ItemValue _antenna)
	{
		if (_overclock == null && _antenna == null)
		{
			return DroneBoost.None;
		}
		float speedMult = ((_overclock != null) ? DroneAutomationMod.OverclockSettings.SpeedMult(_overclock.Quality) : 1f);
		float reachMult = ((_antenna != null) ? DroneAutomationMod.AntennaSettings.ReachMult(_antenna.Quality) : 1f);
		return new DroneBoost(speedMult, reachMult);
	}

	private static bool IsNearOwner(EntityDrone _drone, EntityPlayer _owner)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		float maxOwnerDistance = DroneAutomationMod.MaxOwnerDistance;
		if (maxOwnerDistance <= 0f)
		{
			return true;
		}
		Vector3 val = ((Entity)_drone).position - ((Entity)_owner).position;
		return ((Vector3)(ref val)).sqrMagnitude <= maxOwnerDistance * maxOwnerDistance;
	}

	private static bool IsSwitchedOff(EntityPlayer _owner, string _cvar)
	{
		if (((EntityAlive)_owner).Buffs != null)
		{
			return ((EntityAlive)_owner).Buffs.GetCustomVar(_cvar) != 0f;
		}
		return false;
	}

	private static void PrimeToggleBuffs(EntityPlayer _owner)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		EntityBuffs buffs = ((EntityAlive)_owner).Buffs;
		if (buffs == null)
		{
			return;
		}
		string[] toggleBuffs = DroneAutomationMod.ToggleBuffs;
		for (int i = 0; i < toggleBuffs.Length; i++)
		{
			if (!buffs.HasBuff(toggleBuffs[i]))
			{
				buffs.AddBuff(toggleBuffs[i], -1, true, false, -1f);
			}
		}
	}

	private static void PublishFittedModules(EntityPlayer _owner, EntityDrone _drone, ItemValue _loot, ItemValue _salvage, ItemValue _harvest, ItemValue _repair, ItemValue _plant, ItemValue _defense)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = ((Entity)_drone).position - ((Entity)_owner).position;
		float sqrMagnitude = ((Vector3)(ref val)).sqrMagnitude;
		if (!(sqrMagnitude > 64f))
		{
			ulong ticks = GameTimer.Instance.ticks;
			if (!menuClaims.TryGetValue(((Entity)_owner).entityId, out var value) || ticks < value.Tick || ticks - value.Tick > 40 || value.DroneId == ((Entity)_drone).entityId || !(sqrMagnitude >= value.DistSq))
			{
				value.DroneId = ((Entity)_drone).entityId;
				value.Tick = ticks;
				value.DistSq = sqrMagnitude;
				menuClaims[((Entity)_owner).entityId] = value;
				PublishOne(_owner, "daLootMissing", _loot);
				PublishOne(_owner, "daSalvageMissing", _salvage);
				PublishOne(_owner, "daHarvestMissing", _harvest);
				PublishOne(_owner, "daRepairMissing", _repair);
				PublishOne(_owner, "daPlantMissing", _plant);
				PublishOne(_owner, "daDefenseMissing", _defense);
			}
		}
	}

	private static void PublishOne(EntityPlayer _owner, string _cvar, ItemValue _module)
	{
		((EntityAlive)_owner).SetCVar(_cvar, (_module == null) ? 1f : 0f);
	}

	private static ItemValue GetModule(ItemValue _droneItem, string _moduleName)
	{
		ItemValue[] array = _droneItem?.Modifications;
		if (array == null)
		{
			return null;
		}
		foreach (ItemValue val in array)
		{
			if (val != null && !val.IsEmpty() && val.ItemClass != null && ((ItemData)val.ItemClass).Name == _moduleName)
			{
				return val;
			}
		}
		return null;
	}

	private static string DescribeMods(ItemValue _droneItem)
	{
		ItemValue[] array = _droneItem?.Modifications;
		if (array == null)
		{
			return "null";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (ItemValue val in array)
		{
			if (val != null && !val.IsEmpty())
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(", ");
				}
				ItemClass itemClass = val.ItemClass;
				stringBuilder.Append(((itemClass != null) ? ((ItemData)itemClass).Name : null) ?? "?");
			}
		}
		return stringBuilder.ToString();
	}

	private static void Debug(EntityDrone _drone, string _msg)
	{
		if (DroneAutomationMod.Debug)
		{
			ulong ticks = GameTimer.Instance.ticks;
			if (ticks < lastDebugTick || ticks - lastDebugTick >= 40)
			{
				lastDebugTick = ticks;
				Log.Out($"[DroneAutomation][drone {((Entity)_drone).entityId}] {_msg}");
			}
		}
	}
}
