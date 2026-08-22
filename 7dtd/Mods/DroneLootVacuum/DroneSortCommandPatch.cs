using System;
using HarmonyLib;

namespace DroneLootVacuum;

[HarmonyPatch(typeof(EntityDrone), "InitLocalActivationCommands")]
public static class DroneSortCommandPatch
{
	private static void Postfix(Action<EntityActivationCommand> _addCallback)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (Config.SortLootEnabled && _addCallback != null)
			{
				_addCallback(new EntityActivationCommand("dlvSortLoot", "fetch_loot_down", (string)null, (string)null));
				DroneSwitch[] all = DroneToggles.All;
				foreach (DroneSwitch droneSwitch in all)
				{
					_addCallback(new EntityActivationCommand(droneSwitch.CmdId, droneSwitch.CmdIcon, (string)null, (string)null));
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warning("[DroneLootVacuum] add sort command: " + ex.Message);
		}
	}
}
