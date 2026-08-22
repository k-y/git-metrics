using System;
using System.Collections.Generic;
using UnityEngine;

namespace DroneAutomation;

public sealed class HarvestCore
{
	private readonly HarvestSettings settings;

	private readonly Pacer pacer;

	private readonly Random rand = new Random();

	private static readonly List<Vector3i> buffer = new List<Vector3i>();

	private static readonly FastTags<Global> cropsTag = FastTags<Global>.Parse("crops");

	public string LastScan { get; private set; }

	public HarvestCore(HarvestSettings _settings)
	{
		settings = _settings;
		pacer = new Pacer(_settings.MaxCatchupSeconds);
	}

	public bool Tick(World _world, EntityPlayer _owner, PersistentPlayerData _ownerData, EntityDrone _drone, Vector3 _scanCenter, int _quality, DroneBoost _boost)
	{
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Invalid comparison between Unknown and I4
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		pacer.Accrue();
		LastScan = null;
		if (settings.Radius <= 0f)
		{
			LastScan = "Radius is 0 in config";
			return false;
		}
		float num = QualityScale.Reach(settings.Radius, settings.LowQualityReach, _quality) * _boost.ReachMult;
		float num2 = QualityScale.Reach(settings.VerticalRadius, settings.LowQualityReach, _quality) * _boost.ReachMult;
		float num3 = QualityScale.Time(settings.SecondsPerTarget, settings.LowQualityTimeMult, _quality) * _boost.SpeedMult;
		if (pacer.Credit < num3)
		{
			LastScan = $"banking time ({pacer.Credit:0.0}/{num3:0.0}s)";
			return false;
		}
		DroneWorld.CollectParents(_world, _scanCenter, num, num2, buffer);
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		string text = null;
		for (int i = 0; i < buffer.Count; i++)
		{
			if (pacer.Credit < num3)
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
			if (block2 == null || !IsGrownCrop(block2))
			{
				continue;
			}
			num5++;
			if ((int)DroneWorld.Claim(_world, _ownerData, val) != 1)
			{
				num6++;
				continue;
			}
			if (!TryGetReplant(block2, out var _young))
			{
				num7++;
				if (text == null)
				{
					text = block2.GetBlockName();
				}
				continue;
			}
			if (!pacer.TrySpend(num3))
			{
				break;
			}
			DroneWorld.EmitDrops(block2, (EnumDropEvent)2, block, _owner, _drone, rand, ((BlockValue)(ref _young)).ToItemValue());
			((WorldBase)_world).SetBlockRPC(BlockValueRef.op_Implicit(val), _young);
			num4++;
		}
		if (num4 == 0)
		{
			LastScan = $"anchor ({_scanCenter.x:0},{_scanCenter.y:0},{_scanCenter.z:0}) r={num:0.0}/{num2:0.0} q{_quality}: " + $"{buffer.Count} blocks, {num5} grown crops" + ((num5 == 0) ? " -> none in reach" : ($", {num6} outside your claim, {num7} with no replant" + ((text != null) ? (" (e.g. " + text + ")") : "")));
		}
		return num4 > 0;
	}

	private static bool IsGrownCrop(Block _b)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (_b is BlockPlantGrowing)
		{
			return false;
		}
		if (!_b.HasAnyFastTags(cropsTag))
		{
			return false;
		}
		return _b.HasItemsToDropForEvent((EnumDropEvent)2);
	}

	private static bool TryGetReplant(Block _b, out BlockValue _young)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		_young = default(BlockValue);
		if (_b.itemsToDrop == null)
		{
			return false;
		}
		if (!_b.itemsToDrop.TryGetValue((EnumDropEvent)2, out var value) || value == null)
		{
			return false;
		}
		for (int i = 0; i < value.Count; i++)
		{
			string name = value[i].name;
			if (string.IsNullOrEmpty(name) || name == "*")
			{
				continue;
			}
			ItemValue item = ItemClass.GetItem(name, false);
			if (item != null && !item.IsEmpty())
			{
				BlockValue val = item.ToBlockValue(false);
				if (!((BlockValue)(ref val)).isair && ((BlockValue)(ref val)).Block is BlockPlantGrowing)
				{
					_young = val;
					return true;
				}
			}
		}
		return false;
	}
}
