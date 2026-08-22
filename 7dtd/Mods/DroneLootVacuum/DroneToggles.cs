namespace DroneLootVacuum;

public static class DroneToggles
{
	public static readonly DroneSwitch Vacuum = new DroneSwitch("dlvVacuumToggle", "electric_power", "dlvVacuumDisable", "dlvVacuumEnable", "Loot vacuum");

	public static readonly DroneSwitch AmmoResupply = new DroneSwitch("dlvAmmoToggle", "shape_ammo", "dlvAmmoDisable", "dlvAmmoEnable", "Ammo resupply");

	public static readonly DroneSwitch MedicResupply = new DroneSwitch("dlvMedicToggle", "medical", "dlvMedicDisable", "dlvMedicEnable", "First aid resupply");

	public static readonly DroneSwitch[] All = new DroneSwitch[3] { Vacuum, AmmoResupply, MedicResupply };

	public static int IndexOf(DroneSwitch sw)
	{
		for (int i = 0; i < All.Length; i++)
		{
			if (All[i] == sw)
			{
				return i;
			}
		}
		return -1;
	}

	public static DroneSwitch ByIndex(int i)
	{
		if (i < 0 || i >= All.Length)
		{
			return null;
		}
		return All[i];
	}

	public static DroneSwitch ByRadialIcon(string icon)
	{
		for (int i = 0; i < All.Length; i++)
		{
			if (All[i].RadialIcon == icon)
			{
				return All[i];
			}
		}
		return null;
	}

	public static DroneSwitch ByCmdId(string id)
	{
		for (int i = 0; i < All.Length; i++)
		{
			if (All[i].CmdId == id)
			{
				return All[i];
			}
		}
		return null;
	}
}
