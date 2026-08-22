using System;
using HarmonyLib;
using UnityEngine;

namespace DroneLootVacuum;

[HarmonyPatch(typeof(EntityDrone), "OnEntityActivated")]
public static class DroneSortActivatePatch
{
	private static void Postfix(EntityDrone __instance, EntityActivationCommand _command, EntityPlayerLocal _playerFocusing)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if ((Object)(object)__instance == (Object)null || (Object)(object)_playerFocusing == (Object)null)
			{
				return;
			}
			ConnectionManager instance = SingletonMonoBehaviour<ConnectionManager>.Instance;
			bool flag = (Object)(object)instance == (Object)null || instance.IsServer;
			DroneSwitch droneSwitch = DroneToggles.ByCmdId(_command.commandId);
			if (droneSwitch != null)
			{
				bool flag2 = droneSwitch.Toggle(((Entity)__instance).entityId);
				GameManager.ShowTooltipMP((EntityPlayer)(object)_playerFocusing, droneSwitch.Name + ": " + (flag2 ? "ON" : "OFF"), "");
				if (!flag)
				{
					instance.SendToServer((NetPackage)(object)NetPackageManager.GetPackage<NetPackageDroneSortLoot>().Setup(((Entity)__instance).entityId, ((Entity)_playerFocusing).entityId, 1, flag2 ? 1 : 0, DroneToggles.IndexOf(droneSwitch)), false);
				}
			}
			else if (Config.SortLootEnabled && string.Equals(_command.commandId, "dlvSortLoot", StringComparison.Ordinal))
			{
				if (flag)
				{
					LootSorter.SortFor(__instance, (EntityPlayer)(object)_playerFocusing);
				}
				else
				{
					instance.SendToServer((NetPackage)(object)NetPackageManager.GetPackage<NetPackageDroneSortLoot>().Setup(((Entity)__instance).entityId, ((Entity)_playerFocusing).entityId), false);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error("[DroneLootVacuum] sort activate: " + ex.Message);
		}
	}
}
