using System;
using HarmonyLib;

namespace DroneLootVacuum;

[HarmonyPatch(typeof(EntityDrone), "openStorageWindow")]
public static class DroneStorageOpenPatch
{
	private static void Postfix(EntityDrone __instance)
	{
		try
		{
			DroneBusy.Report(__instance, busy: true);
		}
		catch (Exception ex)
		{
			Log.Warning("[DroneLootVacuum] storage open: " + ex.Message);
		}
	}
}
