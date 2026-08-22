using System;
using HarmonyLib;

namespace DroneLootVacuum;

[HarmonyPatch(typeof(EntityDrone), "StopUIInteraction")]
public static class DroneStorageClosePatch
{
	private static void Postfix(EntityDrone __instance)
	{
		try
		{
			DroneBusy.Report(__instance, busy: false);
		}
		catch (Exception ex)
		{
			Log.Warning("[DroneLootVacuum] storage close: " + ex.Message);
		}
	}
}
