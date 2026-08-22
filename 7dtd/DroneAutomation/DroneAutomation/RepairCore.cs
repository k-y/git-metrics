using System.Collections.Generic;
using UnityEngine;

namespace DroneAutomation;

public sealed class RepairCore
{
	private struct Cost
	{
		public ItemValue item;

		public int count;
	}

	private readonly RepairSettings settings;

	private readonly Pacer pacer;

	private static readonly List<Vector3i> buffer = new List<Vector3i>();

	private static readonly List<Cost> costBuffer = new List<Cost>();

	public RepairCore(RepairSettings _settings)
	{
		settings = _settings;
		pacer = new Pacer(_settings.MaxCatchupSeconds);
	}

	public bool Tick(World _world, EntityPlayer _owner, PersistentPlayerData _ownerData, EntityDrone _drone, Vector3 _scanCenter, int _quality, DroneBoost _boost)
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Invalid comparison between Unknown and I4
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		pacer.Accrue();
		if (settings.Radius <= 0f)
		{
			return false;
		}
		float radius = QualityScale.Reach(settings.Radius, settings.LowQualityReach, _quality) * _boost.ReachMult;
		float vertical = QualityScale.Reach(settings.VerticalRadius, settings.LowQualityReach, _quality) * _boost.ReachMult;
		float num = QualityScale.Time(settings.SecondsPerBlock, settings.LowQualityTimeMult, _quality) * _boost.SpeedMult;
		if (pacer.Credit < num)
		{
			return false;
		}
		DroneWorld.CollectParents(_world, _scanCenter, radius, vertical, buffer);
		int num2 = 0;
		for (int i = 0; i < buffer.Count; i++)
		{
			if (pacer.Credit < num)
			{
				break;
			}
			Vector3i val = buffer[i];
			BlockValue block = ((WorldBase)_world).GetBlock(val);
			if (((BlockValue)(ref block)).isair || block.damage <= 0)
			{
				continue;
			}
			Block block2 = ((BlockValue)(ref block)).Block;
			if (block2 == null)
			{
				continue;
			}
			List<SItemNameCount> repairItems = block2.RepairItems;
			if (repairItems != null && repairItems.Count != 0 && (int)DroneWorld.Claim(_world, _ownerData, val) == 1 && CanAfford(block2, block, ((Entity)_drone).bag, out var _costs))
			{
				if (!pacer.TrySpend(num))
				{
					break;
				}
				for (int j = 0; j < _costs.Count; j++)
				{
					((Entity)_drone).bag.DecItem(_costs[j].item, _costs[j].count, false, (IList<ItemStack>)null);
				}
				block2.DamageBlock((WorldBase)(object)_world, BlockValueRef.op_Implicit(val), block, -block.damage, ((Entity)_drone).entityId, (AttackHitInfo)null, false, false);
				num2++;
			}
		}
		return num2 > 0;
	}

	private static bool CanAfford(Block _b, BlockValue _bv, Bag _bag, out List<Cost> _costs)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		costBuffer.Clear();
		_costs = costBuffer;
		int num = Mathf.Max(1, _b.MaxDamage);
		float num2 = (float)_bv.damage / (float)num;
		float resourceScale = _b.ResourceScale;
		List<SItemNameCount> repairItems = _b.RepairItems;
		for (int i = 0; i < repairItems.Count; i++)
		{
			ItemValue item = ItemClass.GetItem(repairItems[i].ItemName, false);
			if (item == null || item.IsEmpty())
			{
				return false;
			}
			int num3 = Mathf.Max(1, (int)((float)repairItems[i].Count * num2 * resourceScale));
			if (_bag.GetItemCount(item, -1, -1, true) < num3)
			{
				return false;
			}
			costBuffer.Add(new Cost
			{
				item = item,
				count = num3
			});
		}
		return costBuffer.Count > 0;
	}
}
