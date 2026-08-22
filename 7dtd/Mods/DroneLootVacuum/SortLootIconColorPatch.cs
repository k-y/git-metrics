using System;
using HarmonyLib;
using UnityEngine;

namespace DroneLootVacuum;

[HarmonyPatch(typeof(XUiC_Radial), "CreateRadialEntry", new Type[]
{
	typeof(int),
	typeof(string),
	typeof(string),
	typeof(string),
	typeof(string),
	typeof(bool)
})]
public static class SortLootIconColorPatch
{
	private static bool Prefix(XUiC_Radial __instance, int _commandIdx, string _icon, string _atlas, string _text, string _selectionText, bool _highlighted)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			bool num = Config.SortLootEnabled && _icon == "ui_game_symbol_fetch_loot_down";
			DroneSwitch droneSwitch = DroneToggles.ByRadialIcon(_icon);
			if (!num && droneSwitch == null)
			{
				return true;
			}
			Color val = Config.SortIconColor;
			if (droneSwitch != null)
			{
				EntityDrone current = RadialDroneContext.Current;
				bool flag = (Object)(object)current == (Object)null || droneSwitch.IsOn(((Entity)current).entityId);
				val = (flag ? Config.VacuumOnColor : Config.VacuumOffColor);
				_selectionText = droneSwitch.CaptionFor(flag);
			}
			__instance.CreateRadialEntry(_commandIdx, _icon, val, _atlas, _text, _selectionText, _highlighted);
			return false;
		}
		catch (Exception ex)
		{
			Log.Warning("[DroneLootVacuum] sort icon colour: " + ex.Message);
		}
		return true;
	}
}
