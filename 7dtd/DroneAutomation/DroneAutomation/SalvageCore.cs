using System;
using System.Collections.Generic;
using UnityEngine;

namespace DroneAutomation;

public sealed class SalvageCore
{
	private readonly SalvageSettings settings;

	private readonly Pacer pacer;

	private readonly Random rand = new Random();

	private static readonly List<Vector3i> buffer = new List<Vector3i>();

	public SalvageCore(SalvageSettings _settings)
	{
		settings = _settings;
		pacer = new Pacer(_settings.MaxCatchupSeconds);
	}

	public bool Tick(World _world, EntityPlayer _owner, PersistentPlayerData _ownerData, EntityDrone _drone, Vector3 _scanCenter, int _quality, DroneBoost _boost)
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		pacer.Accrue();
		if (settings.Radius <= 0f)
		{
			return false;
		}
		float radius = QualityScale.Reach(settings.Radius, settings.LowQualityReach, _quality) * _boost.ReachMult;
		float vertical = QualityScale.Reach(settings.VerticalRadius, settings.LowQualityReach, _quality) * _boost.ReachMult;
		float num = QualityScale.Time(settings.SecondsPerStep, settings.LowQualityTimeMult, _quality) * _boost.SpeedMult;
		if (pacer.Credit < num)
		{
			return false;
		}
		DroneWorld.CollectParents(_world, _scanCenter, radius, vertical, buffer);
		SalvageGuards.BeginTick(_scanCenter, radius);
		int num2 = 0;
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
			if (block2 != null && IsSalvageable(block2) && (int)DroneWorld.Claim(_world, _ownerData, val) == 0 && !SalvageGuards.InsideClaim(val) && (settings.SalvageSwitches || !SalvageGuards.IsTriggerBlock(block2)) && !_world.IsWithinTraderArea(val) && (settings.SalvageWorkstations || !IsWorkstation(_world, val)) && (settings.ExcludedBlocks.Count <= 0 || !settings.ExcludedBlocks.Contains(block2.GetBlockName())) && (settings.SalvageInPOIs || !IsInsidePOI(_world, val)) && !HoldsUnlootedContents(_world, val))
			{
				if (!pacer.TrySpend(num))
				{
					break;
				}
				DroneWorld.EmitDrops(block2, (EnumDropEvent)2, block, _owner, _drone, rand);
				block2.DamageBlock((WorldBase)(object)_world, BlockValueRef.op_Implicit(val), block, block2.MaxDamage, ((Entity)_drone).entityId, (AttackHitInfo)null, true, false);
				num2++;
			}
		}
		return num2 > 0;
	}

	private static bool HoldsUnlootedContents(World _world, Vector3i _pos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		TileEntity tileEntity = ((WorldBase)_world).GetTileEntity(_pos);
		if (tileEntity == null)
		{
			return false;
		}
		ITileEntityLootable val = default(ITileEntityLootable);
		if (!TileEntityExtensions.TryGetSelfOrFeature<ITileEntityLootable>((ITileEntity)(object)tileEntity, ref val))
		{
			return false;
		}
		if (val.bPlayerStorage)
		{
			return true;
		}
		if (!val.bTouched)
		{
			return true;
		}
		if (!val.IsEmpty())
		{
			return true;
		}
		return false;
	}

	private static bool IsWorkstation(World _world, Vector3i _pos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ((WorldBase)_world).GetTileEntity(_pos) is TileEntityWorkstation;
	}

	private static bool IsInsidePOI(World _world, Vector3i _pos)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		ChunkCluster chunkCache = ((WorldBase)_world).ChunkCache;
		object obj;
		if (chunkCache == null)
		{
			obj = null;
		}
		else
		{
			IChunkProvider chunkProvider = chunkCache.ChunkProvider;
			obj = ((chunkProvider != null) ? chunkProvider.GetDynamicPrefabDecorator() : null);
		}
		DynamicPrefabDecorator val = (DynamicPrefabDecorator)obj;
		if (val != null)
		{
			return val.GetPrefabAtPosition(((Vector3i)(ref _pos)).ToVector3(), (FastTags<Poi>?)null, (FastTags<Poi>?)null) != null;
		}
		return false;
	}

	private static bool IsSalvageable(Block _b)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
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
			string tag = value[i].tag;
			if (!string.IsNullOrEmpty(tag) && tag.Contains("salvage"))
			{
				return true;
			}
		}
		return false;
	}
}
