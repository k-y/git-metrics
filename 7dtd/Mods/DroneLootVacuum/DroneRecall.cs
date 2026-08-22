using System;
using System.Collections.Generic;
using UnityEngine;

namespace DroneLootVacuum;

public static class DroneRecall
{
	public static void Request(EntityPlayer player)
	{
		if (!((Object)(object)player == (Object)null))
		{
			ConnectionManager instance = SingletonMonoBehaviour<ConnectionManager>.Instance;
			if ((Object)(object)instance == (Object)null || instance.IsServer)
			{
				RecallFor(player);
			}
			else
			{
				instance.SendToServer((NetPackage)(object)NetPackageManager.GetPackage<NetPackageDroneRecall>().Setup(((Entity)player).entityId), false);
			}
		}
	}

	public static int RecallFor(EntityPlayer player)
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		GameManager instance = GameManager.Instance;
		World val = ((instance != null) ? instance.World : null);
		if (val == null || (Object)(object)player == (Object)null)
		{
			return 0;
		}
		List<Entity> list = val.Entities?.list;
		if (list == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			Entity obj = list[i];
			EntityDrone val2 = (EntityDrone)(object)((obj is EntityDrone) ? obj : null);
			if (val2 == null || ((Entity)val2).IsDead())
			{
				continue;
			}
			EntityPlayer val3 = DroneOnUpdatePatch.OwnerOf(val2);
			if ((Object)(object)val3 == (Object)null || ((Entity)val3).entityId != ((Entity)player).entityId)
			{
				continue;
			}
			try
			{
				if ((Object)(object)val2.Owner != (Object)null)
				{
					val2.TeleportOutOfRange();
				}
				else
				{
					val2.TeleportToPosition(((Entity)player).position + Vector3.up * 1.5f);
				}
				num++;
			}
			catch (Exception ex)
			{
				Log.Warning("[DroneLootVacuum] recall: " + ex.Message);
			}
		}
		string text = ((num <= 0) ? "No drone to recall." : ((num == 1) ? "Drone recalled." : (num + " drones recalled.")));
		try
		{
			GameManager.ShowTooltipMP(player, text, "");
		}
		catch
		{
		}
		return num;
	}
}
