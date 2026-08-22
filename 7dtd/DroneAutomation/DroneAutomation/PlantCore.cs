using System;
using System.Collections.Generic;
using UnityEngine;

namespace DroneAutomation;

public sealed class PlantCore
{
	private struct Plantable
	{
		public ItemValue item;

		public BlockValue young;
	}

	private readonly PlantSettings settings;

	private readonly Pacer pacer;

	private static readonly List<Vector3i> buffer = new List<Vector3i>();

	private readonly List<Plantable> plantables = new List<Plantable>();

	public PlantCore(PlantSettings _settings)
	{
		settings = _settings;
		pacer = new Pacer(_settings.MaxCatchupSeconds);
	}

	public bool Tick(World _world, EntityPlayer _owner, PersistentPlayerData _ownerData, EntityDrone _drone, Vector3 _scanCenter, int _quality, DroneBoost _boost)
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Invalid comparison between Unknown and I4
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		pacer.Accrue();
		if (settings.Radius <= 0f)
		{
			return false;
		}
		float radius = QualityScale.Reach(settings.Radius, settings.LowQualityReach, _quality) * _boost.ReachMult;
		float vertical = QualityScale.Reach(settings.VerticalRadius, settings.LowQualityReach, _quality) * _boost.ReachMult;
		float num = QualityScale.Time(settings.SecondsPerPlant, settings.LowQualityTimeMult, _quality) * _boost.SpeedMult;
		if (pacer.Credit < num)
		{
			return false;
		}
		CollectPlantables(((Entity)_drone).bag);
		if (plantables.Count == 0)
		{
			return false;
		}
		DroneWorld.CollectParents(_world, _scanCenter, radius, vertical, buffer);
		int num2 = 0;
		Vector3i val2 = default(Vector3i);
		for (int i = 0; i < buffer.Count; i++)
		{
			if (pacer.Credit < num)
			{
				break;
			}
			Vector3i val = buffer[i];
			BlockValue block = ((WorldBase)_world).GetBlock(val);
			if (((BlockValue)(ref block)).isair)
			{
				continue;
			}
			Block block2 = ((BlockValue)(ref block)).Block;
			if (block2 == null || !IsFarmPlot(block2))
			{
				continue;
			}
			((Vector3i)(ref val2))._002Ector(val.x, val.y + 1, val.z);
			BlockValue block3 = ((WorldBase)_world).GetBlock(val2);
			if (((BlockValue)(ref block3)).isair && (int)DroneWorld.Claim(_world, _ownerData, val) == 1)
			{
				int num3 = NextAvailable(((Entity)_drone).bag);
				if (num3 < 0 || !pacer.TrySpend(num))
				{
					break;
				}
				((WorldBase)_world).SetBlockRPC(BlockValueRef.op_Implicit(val2), plantables[num3].young);
				((Entity)_drone).bag.DecItem(plantables[num3].item, 1, false, (IList<ItemStack>)null);
				num2++;
			}
		}
		plantables.Clear();
		return num2 > 0;
	}

	private static bool IsFarmPlot(Block _b)
	{
		string blockName = _b.GetBlockName();
		if (!string.IsNullOrEmpty(blockName))
		{
			return blockName.StartsWith("farmPlot", StringComparison.Ordinal);
		}
		return false;
	}

	private void CollectPlantables(Bag _bag)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		plantables.Clear();
		ItemStack[] slots = _bag.GetSlots();
		foreach (ItemStack val in slots)
		{
			if (val == null || val.IsEmpty())
			{
				continue;
			}
			ItemValue itemValue = val.itemValue;
			if (itemValue != null && !itemValue.IsEmpty() && !AlreadyListed(itemValue))
			{
				BlockValue young = itemValue.ToBlockValue(false);
				if (!((BlockValue)(ref young)).isair && ((BlockValue)(ref young)).Block is BlockPlantGrowing)
				{
					plantables.Add(new Plantable
					{
						item = itemValue,
						young = young
					});
				}
			}
		}
	}

	private bool AlreadyListed(ItemValue _iv)
	{
		for (int i = 0; i < plantables.Count; i++)
		{
			if (plantables[i].item.type == _iv.type)
			{
				return true;
			}
		}
		return false;
	}

	private int NextAvailable(Bag _bag)
	{
		for (int i = 0; i < plantables.Count; i++)
		{
			if (_bag.GetItemCount(plantables[i].item, -1, -1, true) > 0)
			{
				return i;
			}
		}
		return -1;
	}
}
