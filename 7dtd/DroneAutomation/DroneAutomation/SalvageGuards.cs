using System;
using System.Collections.Generic;
using UnityEngine;

namespace DroneAutomation;

public static class SalvageGuards
{
	private static readonly List<Vector3i> nearbyClaims = new List<Vector3i>();

	private static int claimHalfSize;

	private static readonly HashSet<string> TriggerBlockTypes = new HashSet<string> { "BlockActivateSwitch", "BlockActivate", "BlockActivateSingle", "BlockQuestActivate", "BlockPowered", "BlockTriggerDowngrade" };

	public static void BeginTick(Vector3 _center, float _radius)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		nearbyClaims.Clear();
		int num = GameStats.GetInt((EnumGameStats)44);
		if (num <= 0)
		{
			num = 41;
		}
		claimHalfSize = (num - 1) / 2;
		GameManager instance = GameManager.Instance;
		PersistentPlayerList val = ((instance != null) ? instance.GetPersistentPlayerList() : null);
		if (val?.m_lpBlockMap == null)
		{
			return;
		}
		int num2 = Utils.Fastfloor(_center.x);
		int num3 = Utils.Fastfloor(_center.z);
		int num4 = claimHalfSize + Mathf.CeilToInt(_radius) + 1;
		foreach (KeyValuePair<Vector3i, PersistentPlayerData> item in val.m_lpBlockMap)
		{
			Vector3i key = item.Key;
			if (Math.Abs(key.x - num2) <= num4 && Math.Abs(key.z - num3) <= num4)
			{
				nearbyClaims.Add(key);
			}
		}
	}

	public static bool InsideClaim(Vector3i _pos)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < nearbyClaims.Count; i++)
		{
			Vector3i val = nearbyClaims[i];
			if (Math.Abs(val.x - _pos.x) <= claimHalfSize && Math.Abs(val.z - _pos.z) <= claimHalfSize)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsTriggerBlock(Block _b)
	{
		Type type = ((object)_b)?.GetType();
		while (type != null)
		{
			if (TriggerBlockTypes.Contains(type.Name))
			{
				return true;
			}
			if (type.Name == "Block")
			{
				break;
			}
			type = type.BaseType;
		}
		return false;
	}
}
