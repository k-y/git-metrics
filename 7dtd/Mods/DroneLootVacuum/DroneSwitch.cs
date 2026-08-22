using System.Collections.Generic;

namespace DroneLootVacuum;

public class DroneSwitch
{
	private readonly HashSet<int> off = new HashSet<int>();

	public readonly string CmdId;

	public readonly string CmdIcon;

	public readonly string DisableKey;

	public readonly string EnableKey;

	public readonly string Name;

	public string RadialIcon => "ui_game_symbol_" + CmdIcon;

	public DroneSwitch(string cmdId, string icon, string disableKey, string enableKey, string name)
	{
		CmdId = cmdId;
		CmdIcon = icon;
		DisableKey = disableKey;
		EnableKey = enableKey;
		Name = name;
	}

	public bool IsOff(int droneId)
	{
		return off.Contains(droneId);
	}

	public bool IsOn(int droneId)
	{
		return !off.Contains(droneId);
	}

	public bool Toggle(int droneId)
	{
		if (off.Remove(droneId))
		{
			return true;
		}
		off.Add(droneId);
		return false;
	}

	public void Set(int droneId, bool enabled)
	{
		if (enabled)
		{
			off.Remove(droneId);
		}
		else
		{
			off.Add(droneId);
		}
	}

	public string CaptionFor(bool enabled)
	{
		return Localization.Get("entitycommand_" + (enabled ? DisableKey : EnableKey), false, (string)null);
	}
}
