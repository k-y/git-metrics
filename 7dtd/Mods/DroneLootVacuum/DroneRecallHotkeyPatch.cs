using HarmonyLib;
using UnityEngine;

namespace DroneLootVacuum;

[HarmonyPatch(typeof(EntityPlayerLocal), "Update")]
public static class DroneRecallHotkeyPatch
{
	private static void Postfix(EntityPlayerLocal __instance)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		if (Config.Enabled && Config.RecallEnabled && (int)Config.RecallKey != 0 && !((Object)(object)__instance == (Object)null) && !((Entity)__instance).IsDead())
		{
			GUIWindowManager windowManager = __instance.windowManager;
			if ((!((Object)(object)windowManager != (Object)null) || windowManager.IsKeyShortcutsAllowed()) && Input.GetKeyDown(Config.RecallKey))
			{
				DroneRecall.Request((EntityPlayer)(object)__instance);
			}
		}
	}
}
