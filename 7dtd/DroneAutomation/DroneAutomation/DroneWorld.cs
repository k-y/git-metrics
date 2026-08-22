using System;
using System.Collections.Generic;
using UnityEngine;

namespace DroneAutomation;

public static class DroneWorld
{
	private static readonly HashSet<Vector3i> scanSet = new HashSet<Vector3i>();

	public static void CollectParents(World _world, Vector3 _center, float _radius, float _vertical, List<Vector3i> _out)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		_out.Clear();
		scanSet.Clear();
		int num = Utils.Fastfloor(_center.x);
		int num2 = Utils.Fastfloor(_center.y);
		int num3 = Utils.Fastfloor(_center.z);
		int num4 = Mathf.CeilToInt(_radius);
		int num5 = Mathf.CeilToInt(_vertical);
		float num6 = _radius * _radius;
		Vector3i val = default(Vector3i);
		for (int i = -num5; i <= num5; i++)
		{
			for (int j = -num4; j <= num4; j++)
			{
				for (int k = -num4; k <= num4; k++)
				{
					if ((float)(j * j + k * k) > num6)
					{
						continue;
					}
					((Vector3i)(ref val))._002Ector(num + j, num2 + i, num3 + k);
					BlockValue block = ((WorldBase)_world).GetBlock(val);
					if (!((BlockValue)(ref block)).isair)
					{
						Vector3i item = ResolveParent(val, block);
						if (scanSet.Add(item))
						{
							_out.Add(item);
						}
					}
				}
			}
		}
	}

	public static Vector3i ResolveParent(Vector3i _pos, BlockValue _bv)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		Block block = ((BlockValue)(ref _bv)).Block;
		if (block != null && block.isMultiBlock && ((BlockValue)(ref _bv)).ischild && block.multiBlockPos != null)
		{
			return block.multiBlockPos.GetParentPos(_pos, _bv);
		}
		return _pos;
	}

	public static EnumLandClaimOwner Claim(World _world, PersistentPlayerData _ownerData, Vector3i _pos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return ((WorldBase)_world).GetLandClaimOwner(_pos, _ownerData);
	}

	public static int DropCount(SItemDropProb _drop, EntityPlayer _owner, Random _rand)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		if (_drop.prob < 1f && _rand.NextDouble() > (double)_drop.prob)
		{
			return 0;
		}
		int minCount = _drop.minCount;
		int maxCount = _drop.maxCount;
		int num = ((maxCount <= minCount) ? minCount : _rand.Next(minCount, maxCount + 1));
		if (num <= 0)
		{
			return 0;
		}
		FastTags<Global> val = (string.IsNullOrEmpty(_drop.tag) ? default(FastTags<Global>) : FastTags<Global>.Parse(_drop.tag));
		float value = EffectManager.GetValue((PassiveEffects)141, (ItemValue)null, 1f, (EntityAlive)(object)_owner, (Recipe)null, val, true, true, true, true, true, 1, true, false);
		int num2 = Mathf.RoundToInt((float)num * ((value <= 0f) ? 1f : value));
		if (num2 >= 1)
		{
			return num2;
		}
		return 1;
	}

	public static void EmitDrops(Block _b, EnumDropEvent _event, BlockValue _bv, EntityPlayer _owner, EntityDrone _drone, Random _rand, ItemValue _payOne = null)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if (_b.itemsToDrop == null || !_b.itemsToDrop.TryGetValue(_event, out var value) || value == null)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < value.Count; i++)
		{
			SItemDropProb val = value[i];
			int num = DropCount(val, _owner, _rand);
			if (num <= 0)
			{
				continue;
			}
			ItemValue val2 = ((val.name == "*") ? ((BlockValue)(ref _bv)).ToItemValue() : ItemClass.GetItem(val.name, false));
			if (val2 == null || val2.IsEmpty())
			{
				continue;
			}
			if (!flag && _payOne != null && val2.type == _payOne.type)
			{
				flag = true;
				num--;
				if (num <= 0)
				{
					continue;
				}
			}
			Deposit(_drone, val2.Clone(), num);
		}
	}

	public static void Deposit(EntityDrone _drone, ItemValue _iv, int _count)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (_iv != null && _count > 0)
		{
			ItemStack val = new ItemStack(_iv, _count);
			if (!((Entity)_drone).bag.AddItem(val))
			{
				GameManager.Instance.ItemDropServer(val, ((Entity)_drone).position, Vector3.zero, ((Entity)_drone).entityId, 60f, false);
			}
		}
	}
}
