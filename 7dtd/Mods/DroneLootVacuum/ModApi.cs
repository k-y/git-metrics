using System;
using HarmonyLib;

namespace DroneLootVacuum;

public class ModApi : IModApi
{
	public void InitMod(Mod _modInstance)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		try
		{
			ModPathHolder.Path = ((_modInstance != null) ? _modInstance.Path : "");
			Config.Load(ModPathHolder.Path);
			Harmony val = new Harmony("com.rickymartin.dronelootvacuum");
			Type[] types = typeof(ModApi).Assembly.GetTypes();
			foreach (Type type in types)
			{
				try
				{
					val.CreateClassProcessor(type).Patch();
				}
				catch (Exception ex)
				{
					Log.Warning("[DroneLootVacuum] patch class " + type.Name + " skipped: " + ex.Message);
				}
			}
			Log.Out("[DroneLootVacuum] loaded - drone vacuums nearby loot bags into its cargo.");
		}
		catch (Exception ex2)
		{
			Log.Error("[DroneLootVacuum] init failed: " + ex2);
		}
	}
}
