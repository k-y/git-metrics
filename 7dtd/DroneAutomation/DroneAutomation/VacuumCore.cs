using System.Collections.Generic;
using UnityEngine;

namespace DroneAutomation;

public sealed class VacuumCore
{
	public const float MinOpenSeconds = 0.01f;

	private const int MaxTargetsPerTick = 32;

	private readonly VacuumSettings settings;

	private ulong lastTick;

	private float credit;

	private int processedThisPass;

	private bool stalled;

	private float effEntityRadius;

	private float effContainerRadius;

	private float effVerticalRadius;

	private float effMinSecondsPerTarget;

	private float effItemPickupSeconds;

	private float effSpeedMultiplier;

	private static readonly List<Entity> entityBuffer = new List<Entity>();

	private static readonly List<TileEntity> tileEntityBuffer = new List<TileEntity>();

	public VacuumCore(VacuumSettings _settings)
	{
		settings = _settings;
	}

	public static EntityPlayer ResolveOwner(World _world, PlatformUserIdentifierAbs _ownerId, out PersistentPlayerData _ownerData)
	{
		_ownerData = null;
		if (_ownerId == null)
		{
			return null;
		}
		PersistentPlayerList persistentPlayerList = GameManager.Instance.GetPersistentPlayerList();
		if (persistentPlayerList == null)
		{
			return null;
		}
		_ownerData = persistentPlayerList.GetPlayerData(_ownerId);
		if (_ownerData == null || _ownerData.EntityId == -1)
		{
			return null;
		}
		Entity entity = ((WorldBase)_world).GetEntity(_ownerData.EntityId);
		return (EntityPlayer)(object)((entity is EntityPlayer) ? entity : null);
	}

	public bool Tick(World _world, EntityPlayer _owner, PersistentPlayerData _ownerData, PlatformUserIdentifierAbs _ownerId, IVacuumSink _sink, Vector3 _center, int _quality, DroneBoost _boost)
	{
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		AccrueCredit();
		effEntityRadius = QualityScale.Reach(settings.EntityRadius, settings.LowQualityReach, _quality);
		effContainerRadius = QualityScale.Reach(settings.ContainerRadius, settings.LowQualityReach, _quality);
		effVerticalRadius = QualityScale.Reach(settings.VerticalRadius, settings.LowQualityReach, _quality);
		effMinSecondsPerTarget = QualityScale.Time(settings.MinSecondsPerTarget, settings.LowQualityTimeMult, _quality);
		effItemPickupSeconds = QualityScale.Time(settings.ItemPickupSeconds, settings.LowQualityTimeMult, _quality);
		effSpeedMultiplier = QualityScale.Time(settings.SpeedMultiplier, settings.LowQualityTimeMult, _quality);
		effEntityRadius *= _boost.ReachMult;
		effContainerRadius *= _boost.ReachMult;
		effVerticalRadius *= _boost.ReachMult;
		effMinSecondsPerTarget *= _boost.SpeedMult;
		effItemPickupSeconds *= _boost.SpeedMult;
		effSpeedMultiplier *= _boost.SpeedMult;
		if (credit < effMinSecondsPerTarget)
		{
			return false;
		}
		if (!HasFreeSlot(_sink))
		{
			return false;
		}
		processedThisPass = 0;
		stalled = false;
		DrainBags(_world, _owner, _ownerData, _sink, _center);
		if (!stalled && effContainerRadius > 0f)
		{
			DrainContainers(_world, _owner, _ownerData, _ownerId, _sink, _center);
		}
		if (!stalled)
		{
			DrainLooseItems(_world, _ownerData, _sink, _center);
		}
		return processedThisPass > 0;
	}

	private void AccrueCredit()
	{
		ulong ticks = GameTimer.Instance.ticks;
		if (ticks < lastTick)
		{
			lastTick = ticks;
		}
		if (lastTick == 0L)
		{
			lastTick = ticks;
			return;
		}
		float num = GameTimer.Instance.ticksPerSecond;
		if (num <= 0f)
		{
			num = 20f;
		}
		credit += (float)(ticks - lastTick) / num;
		lastTick = ticks;
		if (credit > settings.MaxCatchupSeconds)
		{
			credit = settings.MaxCatchupSeconds;
		}
	}

	private bool TrySpend(float _seconds)
	{
		if (processedThisPass >= 32)
		{
			stalled = true;
			return false;
		}
		if (credit < _seconds)
		{
			stalled = true;
			return false;
		}
		credit -= _seconds;
		processedThisPass++;
		return true;
	}

	private float OpenSeconds(EntityPlayer _owner, string _lootListName)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		float num = LootContainer.GetLootContainer(_lootListName, false)?.openTime ?? 0f;
		float num2 = EffectManager.GetValue((PassiveEffects)138, (ItemValue)null, num, (EntityAlive)(object)_owner, (Recipe)null, default(FastTags<Global>), true, true, true, true, true, 1, true, false) * LootContainer.LootTimerModifier * effSpeedMultiplier;
		if (!(num2 < 0.01f))
		{
			return num2;
		}
		return 0.01f;
	}

	private float Cost(float _seconds)
	{
		if (!(_seconds < effMinSecondsPerTarget))
		{
			return _seconds;
		}
		return effMinSecondsPerTarget;
	}

	private void DrainBags(World _world, EntityPlayer _owner, PersistentPlayerData _ownerData, IVacuumSink _sink, Vector3 _center)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		entityBuffer.Clear();
		_world.GetEntitiesInBounds(typeof(EntityLootContainer), BoundsAround(_center, effEntityRadius), entityBuffer);
		for (int i = 0; i < entityBuffer.Count; i++)
		{
			Entity obj = entityBuffer[i];
			EntityLootContainer val = (EntityLootContainer)(object)((obj is EntityLootContainer) ? obj : null);
			if (val != null && ((Entity)val).bag != null && InRange(_center, ((Entity)val).position, effEntityRadius) && !LockManager.Instance.IsLockedServer((ILockTarget)(object)val, (ushort)0) && !IsBlockedPosition(_world, _ownerData, World.worldToBlockPos(((Entity)val).position)))
			{
				if (!HasFreeSlot(_sink))
				{
					break;
				}
				Bag bag = ((Entity)val).bag;
				float seconds = Cost(bag.Touched ? 0.01f : OpenSeconds(_owner, ((Entity)val).GetLootList()));
				if (!TrySpend(seconds))
				{
					break;
				}
				if (!bag.Touched && bag.IsEmpty())
				{
					GameManager.Instance.lootManager.LootBagOpened(bag, (Entity)(object)val, ((Entity)_owner).entityId);
				}
				MoveSlots(_sink, bag.GetSlots());
				bag.Touched = true;
			}
		}
		entityBuffer.Clear();
	}

	private void DrainContainers(World _world, EntityPlayer _owner, PersistentPlayerData _ownerData, PlatformUserIdentifierAbs _ownerId, IVacuumSink _sink, Vector3 _center)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		CollectTileEntitiesInRange(_world, _center);
		ITileEntityLootable val2 = default(ITileEntityLootable);
		TEFeatureLockable val4 = default(TEFeatureLockable);
		for (int i = 0; i < tileEntityBuffer.Count; i++)
		{
			TileEntity val = tileEntityBuffer[i];
			if (!TileEntityExtensions.TryGetSelfOrFeature<ITileEntityLootable>((ITileEntity)(object)val, ref val2))
			{
				continue;
			}
			Vector3i val3 = val.ToWorldPos();
			if (!InRange(_center, val.ToWorldCenterPos(), effContainerRadius) || val2.bPlayerStorage || LockManager.Instance.IsLockedServer((ILockTarget)(object)val2, (ushort)0) || (TileEntityExtensions.TryGetSelfOrFeature<TEFeatureLockable>((ITileEntity)(object)val, ref val4) && val4.IsLocked() && (_ownerId == null || !val4.IsUserAllowed(_ownerId))) || IsBlockedPosition(_world, _ownerData, val3))
			{
				continue;
			}
			if (!HasFreeSlot(_sink))
			{
				break;
			}
			BlockValue block = ((WorldBase)_world).GetBlock(val3);
			if (EffectManager.GetValue((PassiveEffects)178, (ItemValue)null, 0f, (EntityAlive)(object)_owner, (Recipe)null, ((BlockValue)(ref block)).Block.Tags, true, true, true, true, true, 1, true, false) > 0f)
			{
				continue;
			}
			bool flag = val2.IsEmpty();
			bool bTouched = val2.bTouched;
			if ((!bTouched && !flag) || (bTouched && flag))
			{
				continue;
			}
			float seconds = Cost(bTouched ? 0.01f : OpenSeconds(_owner, val2.lootListName));
			if (!TrySpend(seconds))
			{
				break;
			}
			if (!bTouched)
			{
				GameManager.Instance.lootManager.LootContainerOpened(val2, ((Entity)_owner).entityId, ((BlockValue)(ref block)).Block.Tags);
				val2.bTouched = true;
			}
			if (MoveFromLootable(_sink, val2))
			{
				((ITileEntity)val2).SetModified();
				if (val2.IsEmpty())
				{
					GameManager.Instance.CheckDestroyTileEntity((ITileEntity)(object)val2, val3);
				}
			}
		}
		tileEntityBuffer.Clear();
	}

	private void DrainLooseItems(World _world, PersistentPlayerData _ownerData, IVacuumSink _sink, Vector3 _center)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		entityBuffer.Clear();
		_world.GetEntitiesInBounds(typeof(EntityItem), BoundsAround(_center, effEntityRadius), entityBuffer);
		for (int i = 0; i < entityBuffer.Count; i++)
		{
			Entity val = entityBuffer[i];
			if (val is EntityBackpack || val is EntityLootContainer)
			{
				continue;
			}
			EntityItem val2 = (EntityItem)(object)((val is EntityItem) ? val : null);
			if (val2 != null && val2.itemStack != null && !val2.itemStack.IsEmpty() && IsSettledLoot(val2) && InRange(_center, ((Entity)val2).position, effEntityRadius) && !IsBlockedPosition(_world, _ownerData, World.worldToBlockPos(((Entity)val2).position)))
			{
				if (!HasFreeSlot(_sink) || !TrySpend(Cost(effItemPickupSeconds)))
				{
					break;
				}
				ItemStack val3 = val2.itemStack.Clone();
				if (Absorb(_sink, val3, val3.count) && val3.count <= 0)
				{
					((WorldBase)_world).RemoveEntity(((Entity)val2).entityId, (EnumRemoveEntityReason)2);
				}
			}
		}
		entityBuffer.Clear();
	}

	private bool IsSettledLoot(EntityItem _item)
	{
		if (!((Entity)_item).onGround)
		{
			return false;
		}
		if (!_item.CanCollect())
		{
			return false;
		}
		if (!_item.bWasThrown)
		{
			return true;
		}
		return ((Entity)_item).ticksExisted >= ThrownGraceTicks();
	}

	private int ThrownGraceTicks()
	{
		if (settings.ThrownGraceSeconds <= 0f)
		{
			return 0;
		}
		float num = GameTimer.Instance.ticksPerSecond;
		if (num <= 0f)
		{
			num = 20f;
		}
		return Mathf.CeilToInt(settings.ThrownGraceSeconds * num);
	}

	private void CollectTileEntitiesInRange(World _world, Vector3 _center)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		tileEntityBuffer.Clear();
		float num = effContainerRadius;
		int num2 = World.toChunkXZ(Utils.Fastfloor(_center.x - num));
		int num3 = World.toChunkXZ(Utils.Fastfloor(_center.x + num));
		int num4 = World.toChunkXZ(Utils.Fastfloor(_center.z - num));
		int num5 = World.toChunkXZ(Utils.Fastfloor(_center.z + num));
		for (int i = num4; i <= num5; i++)
		{
			for (int j = num2; j <= num3; j++)
			{
				IChunk chunkSync = ((WorldBase)_world).GetChunkSync(j, i);
				Chunk val = (Chunk)(object)((chunkSync is Chunk) ? chunkSync : null);
				if (val != null)
				{
					tileEntityBuffer.AddRange(val.GetTileEntities().list);
				}
			}
		}
	}

	private bool IsBlockedPosition(World _world, PersistentPlayerData _ownerData, Vector3i _pos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((WorldBase)_world).GetLandClaimOwner(_pos, _ownerData) == 3)
		{
			return true;
		}
		if (settings.SkipIfPlayerWithin <= 0f)
		{
			return false;
		}
		float num = settings.SkipIfPlayerWithin * settings.SkipIfPlayerWithin;
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector((float)_pos.x, (float)_pos.y, (float)_pos.z);
		List<EntityPlayer> list = _world.Players.list;
		for (int i = 0; i < list.Count; i++)
		{
			Vector3 val2 = ((Entity)list[i]).position - val;
			if (((Vector3)(ref val2)).sqrMagnitude <= num)
			{
				return true;
			}
		}
		return false;
	}

	private bool InRange(Vector3 _center, Vector3 _pos, float _horizontalRadius)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (Mathf.Abs(_pos.y - _center.y) > effVerticalRadius)
		{
			return false;
		}
		float num = _pos.x - _center.x;
		float num2 = _pos.z - _center.z;
		return num * num + num2 * num2 <= _horizontalRadius * _horizontalRadius;
	}

	private Bounds BoundsAround(Vector3 _center, float _horizontalRadius)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		return new Bounds(_center, new Vector3(_horizontalRadius * 2f, effVerticalRadius * 2f, _horizontalRadius * 2f));
	}

	private static bool HasFreeSlot(IVacuumSink _sink)
	{
		ItemStack[] items = _sink.Items;
		for (int i = 0; i < items.Length; i++)
		{
			if (items[i].IsEmpty())
			{
				return true;
			}
		}
		return false;
	}

	private static int FirstFreeSlot(IVacuumSink _sink)
	{
		ItemStack[] items = _sink.Items;
		for (int i = 0; i < items.Length; i++)
		{
			if (items[i].IsEmpty())
			{
				return i;
			}
		}
		return -1;
	}

	private static bool MoveSlots(IVacuumSink _sink, ItemStack[] _slots)
	{
		bool result = false;
		for (int i = 0; i < _slots.Length; i++)
		{
			ItemStack val = _slots[i];
			if (val != null && !val.IsEmpty())
			{
				ItemStack val2 = val.Clone();
				if (Absorb(_sink, val2, val.count))
				{
					_slots[i] = ((val2.count == 0) ? ItemStack.Empty : val2.Clone());
					result = true;
				}
			}
		}
		return result;
	}

	private static bool MoveFromLootable(IVacuumSink _sink, ITileEntityLootable _source)
	{
		bool result = false;
		ItemStack[] items = _source.items;
		for (int i = 0; i < items.Length; i++)
		{
			ItemStack val = items[i];
			if (val != null && !val.IsEmpty())
			{
				ItemStack val2 = val.Clone();
				if (Absorb(_sink, val2, val.count))
				{
					_source.UpdateSlot(i, (val2.count == 0) ? ItemStack.Empty : val2);
					result = true;
				}
			}
		}
		return result;
	}

	private static bool Absorb(IVacuumSink _sink, ItemStack _moving, int _originalCount)
	{
		_sink.TryStackItem(0, _moving);
		if (_moving.count > 0)
		{
			int num = FirstFreeSlot(_sink);
			if (num >= 0)
			{
				_sink.SetSlot(num, _moving);
				_moving.count = 0;
			}
		}
		bool num2 = _moving.count != _originalCount;
		if (num2)
		{
			_sink.MarkChanged();
		}
		return num2;
	}
}
