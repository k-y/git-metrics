using System.Collections.Generic;
using UnityEngine;

namespace DroneLootVacuum;

public static class DroneBusy
{
	private static readonly Dictionary<int, float> busyUntil = new Dictionary<int, float>();

	private const float MaxOpen = 600f;

	public static bool Is(int droneId)
	{
		if (!busyUntil.TryGetValue(droneId, out var value))
		{
			return false;
		}
		if (Time.time >= value)
		{
			busyUntil.Remove(droneId);
			return false;
		}
		return true;
	}

	public static void Set(int droneId, bool busy)
	{
		if (busy)
		{
			busyUntil[droneId] = Time.time + 600f;
		}
		else
		{
			busyUntil.Remove(droneId);
		}
	}

	public static void Report(EntityDrone drone, bool busy)
	{
		if ((Object)(object)drone == (Object)null)
		{
			return;
		}
		Set(((Entity)drone).entityId, busy);
		ConnectionManager instance = SingletonMonoBehaviour<ConnectionManager>.Instance;
		if (!((Object)(object)instance == (Object)null) && !instance.IsServer)
		{
			GameManager instance2 = GameManager.Instance;
			object obj;
			if (instance2 == null)
			{
				obj = null;
			}
			else
			{
				World world = instance2.World;
				obj = ((world != null) ? ((WorldBase)world).GetPrimaryPlayer() : null);
			}
			EntityPlayerLocal val = (EntityPlayerLocal)obj;
			if (!((Object)(object)val == (Object)null))
			{
				instance.SendToServer((NetPackage)(object)NetPackageManager.GetPackage<NetPackageDroneStorage>().Setup(((Entity)drone).entityId, ((Entity)val).entityId, busy), false);
			}
		}
	}
}
