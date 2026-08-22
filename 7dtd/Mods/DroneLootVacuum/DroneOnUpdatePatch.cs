using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace DroneLootVacuum;

[HarmonyPatch(typeof(EntityDrone), "OnUpdateEntity")]
public static class DroneOnUpdatePatch
{
	private struct Depot
	{
		public ITileEntityLootable Loot;

		public TileEntity Te;

		public float D2;
	}

	private static readonly Dictionary<int, float> _nextScan = new Dictionary<int, float>();

	private static readonly Dictionary<int, float> _nextFullMsg = new Dictionary<int, float>();

	private static readonly Dictionary<int, float> _nextCrateMsg = new Dictionary<int, float>();

	private static float _nextDepDiag;

	private static float _nextVacDiag;

	private static float _nextGateDiag;

	private static readonly List<Vector3i> _rmPos = new List<Vector3i>();

	private static readonly List<BlockValue> _rmBv = new List<BlockValue>();

	public static bool Diag;

	private static readonly List<Depot> _depots = new List<Depot>();

	private static bool DiagOn
	{
		get
		{
			if (!Config.DebugLog)
			{
				return Diag;
			}
			return true;
		}
	}

	public static EntityPlayer OwnerOf(EntityDrone drone)
	{
		if ((Object)(object)drone == (Object)null)
		{
			return null;
		}
		EntityAlive owner = drone.Owner;
		EntityPlayer val = (EntityPlayer)(object)((owner is EntityPlayer) ? owner : null);
		if (val != null)
		{
			return val;
		}
		GameManager instance = GameManager.Instance;
		World val2 = ((instance != null) ? instance.World : null);
		PlatformUserIdentifierAbs ownerID = drone.OwnerID;
		if (val2 == null || ownerID == null)
		{
			return null;
		}
		PersistentPlayerList persistentPlayerList = GameManager.Instance.GetPersistentPlayerList();
		PersistentPlayerData val3 = ((persistentPlayerList != null) ? persistentPlayerList.GetPlayerData(ownerID) : null);
		if (val3 != null && val3.EntityId != -1)
		{
			Entity entity = ((WorldBase)val2).GetEntity(val3.EntityId);
			EntityPlayer val4 = (EntityPlayer)(object)((entity is EntityPlayer) ? entity : null);
			if (val4 != null)
			{
				return val4;
			}
		}
		EntityPlayerLocal primaryPlayer = ((WorldBase)val2).GetPrimaryPlayer();
		object obj;
		if (primaryPlayer == null)
		{
			obj = null;
		}
		else
		{
			PersistentPlayerData persistentPlayerData = ((EntityPlayer)primaryPlayer).PersistentPlayerData;
			obj = ((persistentPlayerData != null) ? persistentPlayerData.PrimaryId : null);
		}
		PlatformUserIdentifierAbs val5 = (PlatformUserIdentifierAbs)obj;
		if (val5 == null || !val5.Equals(ownerID))
		{
			return null;
		}
		return (EntityPlayer)(object)primaryPlayer;
	}

	public static void Postfix(EntityDrone __instance)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!Config.Enabled || (Object)(object)__instance == (Object)null)
			{
				return;
			}
			if (((Entity)__instance).isEntityRemote)
			{
				Resupply.TickClient(__instance);
				return;
			}
			Bag bag = ((Entity)__instance).bag;
			if (bag == null)
			{
				return;
			}
			int entityId = ((Entity)__instance).entityId;
			if (DroneBusy.Is(entityId))
			{
				return;
			}
			float time = Time.time;
			if (_nextScan.TryGetValue(entityId, out var value) && time < value)
			{
				return;
			}
			_nextScan[entityId] = time + Config.ScanInterval;
			float radius = Config.Radius;
			if (radius <= 0f)
			{
				return;
			}
			float num = radius * radius;
			Vector3 position = ((Entity)__instance).position;
			List<Entity> list = ((Entity)__instance).world.Entities.list;
			ItemStack[] slots = bag.GetSlots();
			PackedBoolArray lockedSlots = bag.LockedSlots;
			EntityPlayer val = OwnerOf(__instance);
			bool changed = false;
			bool droneFull = false;
			bool flag = DroneToggles.Vacuum.IsOn(entityId);
			int num2 = 0;
			while (flag && num2 < list.Count)
			{
				Entity val2 = list[num2];
				if (TryGetLootSource(val2, out var bag2, out var isZombie, out var needsRoll) && (!Config.ZombieBagsOnly || isZombie) && bag2 != null && !(Vector3.SqrMagnitude(val2.position - position) > num))
				{
					if (needsRoll && !bag2.Touched && (Object)(object)val != (Object)null)
					{
						LootManager lootManager = GameManager.Instance.lootManager;
						if (lootManager != null)
						{
							lootManager.LootBagOpened(bag2, val2, ((Entity)val).entityId);
						}
					}
					ItemStack[] slots2 = bag2.GetSlots();
					if (slots2 != null)
					{
						bool flag2 = false;
						for (int i = 0; i < slots2.Length; i++)
						{
							if (slots2[i] != null && !slots2[i].IsEmpty() && LootFilter.ShouldPickUp(slots2[i], val))
							{
								ItemStack stack = slots2[i].Clone();
								if (AddToUnlocked(slots, lockedSlots, stack))
								{
									slots2[i] = ItemStack.Empty.Clone();
									flag2 = true;
									changed = true;
								}
								else
								{
									droneFull = true;
								}
							}
						}
						if (flag2)
						{
							bag2.SetSlots(slots2);
							bag2.Touched = true;
						}
					}
				}
				num2++;
			}
			bool flag3 = flag && !Config.ZombieBagsOnly && Config.VacuumWorldContainers;
			if (DiagOn && Time.time >= _nextGateDiag)
			{
				_nextGateDiag = Time.time + 3f;
				Log.Out($"[DLV-gate] drone={entityId} enabled={Config.Enabled} zombie_bags_only={Config.ZombieBagsOnly} " + $"vacuum_world_containers={Config.VacuumWorldContainers} radius={Config.Radius} -> containerScan={flag3}");
			}
			if (flag3)
			{
				VacuumWorldContainers(__instance, val, slots, lockedSlots, position, ref changed, ref droneFull);
			}
			if (changed)
			{
				bag.SetSlots(slots);
				__instance.SendSyncData((ushort)8);
			}
			if (droneFull)
			{
				NotifyDroneFull(__instance);
			}
			Resupply.Tick(__instance, val);
			if (Config.DepositEnabled && HasUnlockedLoot(bag) && (!Config.DepositWhenFull || BagIsFull(bag)))
			{
				TryDeposit(__instance, bag, position);
			}
		}
		catch (Exception ex)
		{
			Log.Error("[DroneLootVacuum] " + ex.Message);
		}
	}

	private static bool BagIsFull(Bag bag)
	{
		ItemStack[] slots = bag.GetSlots();
		PackedBoolArray lockedSlots = bag.LockedSlots;
		if (slots == null)
		{
			return false;
		}
		for (int i = 0; i < slots.Length; i++)
		{
			if (!IsLocked(lockedSlots, i) && (slots[i] == null || slots[i].IsEmpty()))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsLocked(PackedBoolArray mask, int i)
	{
		if (mask != null && i < mask.Length)
		{
			return mask[i];
		}
		return false;
	}

	private static bool BlockMatches(Block blk, string[] names)
	{
		if (blk == null || names == null || names.Length == 0)
		{
			return false;
		}
		string text = blk.GetBlockName()?.ToLowerInvariant() ?? "";
		string text2 = "";
		try
		{
			text2 = blk.GetLocalizedBlockName()?.ToLowerInvariant() ?? "";
		}
		catch
		{
		}
		foreach (string value in names)
		{
			if (!string.IsNullOrEmpty(value) && (text.IndexOf(value, StringComparison.Ordinal) >= 0 || text2.IndexOf(value, StringComparison.Ordinal) >= 0))
			{
				return true;
			}
		}
		return false;
	}

	private static bool AddToUnlocked(ItemStack[] arr, PackedBoolArray lockMask, ItemStack stack)
	{
		for (int i = 0; i < arr.Length; i++)
		{
			if (!IsLocked(lockMask, i) && arr[i] != null && arr[i].CanStackWith(stack, false))
			{
				ItemStack obj = arr[i];
				obj.count += stack.count;
				stack.count = 0;
				return true;
			}
		}
		for (int j = 0; j < arr.Length; j++)
		{
			if (!IsLocked(lockMask, j) && (arr[j] == null || arr[j].IsEmpty()))
			{
				arr[j] = stack;
				return true;
			}
		}
		return false;
	}

	private static bool HasUnlockedLoot(Bag bag)
	{
		ItemStack[] slots = bag.GetSlots();
		PackedBoolArray lockedSlots = bag.LockedSlots;
		if (slots == null)
		{
			return false;
		}
		for (int i = 0; i < slots.Length; i++)
		{
			if (!IsLocked(lockedSlots, i) && slots[i] != null && !slots[i].IsEmpty())
			{
				return true;
			}
		}
		return false;
	}

	private static bool TryGetLootSource(Entity e, out Bag bag, out bool isZombie, out bool needsRoll)
	{
		bag = null;
		isZombie = false;
		needsRoll = false;
		EntityLootContainer val = (EntityLootContainer)(object)((e is EntityLootContainer) ? e : null);
		if (val != null)
		{
			if (val.bRemoved)
			{
				return false;
			}
			bag = Traverse.Create((object)e).Field("bag").GetValue<Bag>();
			isZombie = IsZombieBag(val);
			needsRoll = true;
			return true;
		}
		EntityBackpack val2 = (EntityBackpack)(object)((e is EntityBackpack) ? e : null);
		if (val2 != null)
		{
			if (val2.bRemoved)
			{
				return false;
			}
			bag = Traverse.Create((object)e).Field("bag").GetValue<Bag>();
			return true;
		}
		if (e is EntitySupplyCrate)
		{
			bag = Traverse.Create((object)e).Field("bag").GetValue<Bag>();
			return true;
		}
		return false;
	}

	private static void VacuumWorldContainers(EntityDrone drone, EntityPlayer owner, ItemStack[] dslots, PackedBoolArray dlock, Vector3 dpos, ref bool changed, ref bool droneFull)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
		//IL_0452: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_0396: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		World world = ((Entity)drone).world;
		float radius = Config.Radius;
		if (world == null || radius <= 0f)
		{
			return;
		}
		float num = radius * radius;
		int num2 = ((Entity)(owner?)).entityId ?? (-1);
		bool flag = DiagOn && Time.time >= _nextVacDiag;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		int num9 = 0;
		int num10 = 0;
		int num11 = 0;
		int num12 = 0;
		int num13 = 0;
		int num14 = 0;
		_rmPos.Clear();
		_rmBv.Clear();
		int num15 = Utils.Fastfloor((dpos.x - radius) / 16f);
		int num16 = Utils.Fastfloor((dpos.x + radius) / 16f);
		int num17 = Utils.Fastfloor((dpos.z - radius) / 16f);
		int num18 = Utils.Fastfloor((dpos.z + radius) / 16f);
		for (int i = num17; i <= num18; i++)
		{
			for (int j = num15; j <= num16; j++)
			{
				IChunk chunkFromWorldPos = ((WorldBase)world).GetChunkFromWorldPos(j * 16, 0, i * 16);
				List<TileEntity> list = ((Chunk)(((chunkFromWorldPos is Chunk) ? chunkFromWorldPos : null)?)).tileEntities?.list;
				if (list == null)
				{
					continue;
				}
				num3++;
				for (int k = 0; k < list.Count; k++)
				{
					num4++;
					TileEntity val = list[k];
					TileEntityComposite val2 = (TileEntityComposite)(object)((val is TileEntityComposite) ? val : null);
					ITileEntityLootable val3 = (ITileEntityLootable)((val2 != null) ? ((object)val2.GetFeature<ITileEntityLootable>()) : ((object)((val is ITileEntityLootable) ? val : null)));
					if (val3 == null || val3.items == null)
					{
						continue;
					}
					num5++;
					if (LootSorter.IsPlayerStorage(val2, val3))
					{
						continue;
					}
					if (Config.OnlyLootUntouchedContainers && val3.bTouched)
					{
						num11++;
					}
					else
					{
						if (string.IsNullOrEmpty(val3.lootListName))
						{
							continue;
						}
						num6++;
						if (((ITileEntity)val3).IsUserAccessing())
						{
							num9++;
							continue;
						}
						if (val2 != null)
						{
							TEFeatureLockable feature = val2.GetFeature<TEFeatureLockable>();
							if (feature != null && feature.IsLocked())
							{
								num8++;
								continue;
							}
							TEFeatureLockPickable feature2 = val2.GetFeature<TEFeatureLockPickable>();
							if (feature2 != null && feature2.NeedsLockpicking())
							{
								num8++;
								continue;
							}
						}
						BlockValue blockValue = ((ITileEntity)val3).blockValue;
						Block block = ((BlockValue)(ref blockValue)).Block;
						if (block == null)
						{
							continue;
						}
						if (block.GetBlockName().IndexOf("policecar", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							num10++;
							continue;
						}
						Vector3i val4 = val.ToWorldPos();
						if (Vector3.SqrMagnitude(((Vector3i)(ref val4)).ToVector3() + new Vector3(0.5f, 0.5f, 0.5f) - dpos) > num)
						{
							continue;
						}
						num7++;
						bool flag2 = false;
						if (!val3.bTouched)
						{
							if (num2 == -1)
							{
								continue;
							}
							LootManager lootManager = GameManager.Instance.lootManager;
							if (lootManager != null)
							{
								lootManager.LootContainerOpened(val3, num2, block.Tags);
							}
							num12++;
							flag2 = true;
						}
						bool flag3 = false;
						if (!val3.IsEmpty())
						{
							ItemStack[] items = val3.items;
							for (int l = 0; l < items.Length; l++)
							{
								if (items[l] != null && !items[l].IsEmpty() && LootFilter.ShouldPickUp(items[l], owner))
								{
									ItemStack stack = items[l].Clone();
									if (AddToUnlocked(dslots, dlock, stack))
									{
										items[l] = ItemStack.Empty.Clone();
										flag3 = true;
										changed = true;
										num13++;
									}
									else
									{
										droneFull = true;
									}
								}
							}
							if (flag3)
							{
								((ITileEntity)val3).SetModified();
							}
						}
						if (Config.RemoveEmptyContainers && (flag2 || flag3) && val3.IsEmpty())
						{
							if (BlockMatches(block, Config.DowngradeContainerNames) && !((BlockValue)(ref block.DowngradeBlock)).isair)
							{
								BlockValue downgradeBlock = block.DowngradeBlock;
								blockValue = ((ITileEntity)val3).blockValue;
								((BlockValue)(ref downgradeBlock)).rotation = ((BlockValue)(ref blockValue)).rotation;
								_rmPos.Add(val.ToWorldPos());
								_rmBv.Add(downgradeBlock);
								num14++;
							}
							else if (BlockMatches(block, Config.RemoveContainerNames))
							{
								_rmPos.Add(val.ToWorldPos());
								_rmBv.Add(BlockValue.Air);
								num14++;
							}
						}
					}
				}
			}
		}
		for (int m = 0; m < _rmPos.Count; m++)
		{
			((WorldBase)world).SetBlockRPC(new BlockValueRef(_rmPos[m]), _rmBv[m]);
		}
		if (flag)
		{
			_nextVacDiag = Time.time + 3f;
			Log.Out($"[DLV-vac] owner={num2} r={radius} chunks={num3} te={num4} lootable={num5} withLootList={num6} " + $"inRange={num7} skippedTouched={num11} skippedLocked={num8} skippedUserOpen={num9} skippedPolice={num10} rolled={num12} movedItems={num13} removed={num14}");
		}
	}

	private static void TryDeposit(EntityDrone drone, Bag droneBag, Vector3 dpos)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		World world = ((Entity)drone).world;
		float depositRadius = Config.DepositRadius;
		if (world == null || depositRadius <= 0f)
		{
			return;
		}
		float num = depositRadius * depositRadius;
		bool flag = DiagOn && Time.time >= _nextDepDiag;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		StringBuilder stringBuilder = new StringBuilder();
		_depots.Clear();
		int num6 = Utils.Fastfloor((dpos.x - depositRadius) / 16f);
		int num7 = Utils.Fastfloor((dpos.x + depositRadius) / 16f);
		int num8 = Utils.Fastfloor((dpos.z - depositRadius) / 16f);
		int num9 = Utils.Fastfloor((dpos.z + depositRadius) / 16f);
		for (int i = num8; i <= num9; i++)
		{
			for (int j = num6; j <= num7; j++)
			{
				IChunk chunkFromWorldPos = ((WorldBase)world).GetChunkFromWorldPos(j * 16, 0, i * 16);
				List<TileEntity> list = ((Chunk)(((chunkFromWorldPos is Chunk) ? chunkFromWorldPos : null)?)).tileEntities?.list;
				if (list == null)
				{
					continue;
				}
				num5++;
				for (int k = 0; k < list.Count; k++)
				{
					num2++;
					TileEntity obj = list[k];
					TileEntityComposite val = (TileEntityComposite)(object)((obj is TileEntityComposite) ? obj : null);
					ITileEntityLootable val2 = (ITileEntityLootable)((val != null) ? ((object)val.GetFeature<ITileEntityLootable>()) : ((object)/*isinst with value type is only supported in some contexts*/));
					if (val2 != null && val2.items != null)
					{
						num3++;
						ITileEntitySignable selfOrFeature = TileEntityExtensions.GetSelfOrFeature<ITileEntitySignable>((ITileEntity)(object)val2);
						if (selfOrFeature != null)
						{
							num4++;
						}
						object obj2;
						if (selfOrFeature == null)
						{
							obj2 = null;
						}
						else
						{
							AuthoredText authoredText = selfOrFeature.GetAuthoredText();
							obj2 = ((authoredText != null) ? authoredText.Text : null);
						}
						string text = (string)obj2;
						Vector3i val3 = list[k].ToWorldPos();
						float num10 = Vector3.SqrMagnitude(((Vector3i)(ref val3)).ToVector3() + new Vector3(0.5f, 0.5f, 0.5f) - dpos);
						if (flag)
						{
							stringBuilder.Append(" [").Append(string.IsNullOrEmpty(text) ? "(none)" : text).Append("]@")
								.Append(Mathf.Sqrt(num10).ToString("0.0"));
						}
						if (!string.IsNullOrEmpty(text) && text.Trim().Equals(Config.DepositLabel, StringComparison.OrdinalIgnoreCase) && num10 <= num)
						{
							_depots.Add(new Depot
							{
								Loot = val2,
								Te = list[k],
								D2 = num10
							});
						}
					}
				}
			}
		}
		if (flag)
		{
			_nextDepDiag = Time.time + 2f;
			if (num3 > 0)
			{
				Log.Out($"[DLV-dep] scan r={depositRadius} chunks={num5} te={num2} lootable={num3} signable={num4} matches={_depots.Count} want='{Config.DepositLabel}' | crates:{stringBuilder}");
			}
		}
		if (_depots.Count == 0)
		{
			return;
		}
		_depots.Sort((Depot a, Depot b) => a.D2.CompareTo(b.D2));
		ItemStack[] slots = droneBag.GetSlots();
		PackedBoolArray lockedSlots = droneBag.LockedSlots;
		bool flag2 = false;
		for (int l = 0; l < _depots.Count; l++)
		{
			Depot depot = _depots[l];
			bool flag3 = false;
			for (int m = 0; m < slots.Length; m++)
			{
				if (slots[m] != null && !slots[m].IsEmpty() && !IsLocked(lockedSlots, m))
				{
					ItemStack val4 = slots[m].Clone();
					if (ItemStack.AddToItemStackArray(depot.Loot.items, val4, -1) >= 0)
					{
						slots[m] = ItemStack.Empty.Clone();
						flag2 = true;
						flag3 = true;
					}
				}
			}
			if (flag3)
			{
				depot.Te.SetModified();
			}
			if (AllUnlockedEmpty(slots, lockedSlots))
			{
				break;
			}
		}
		if (flag2)
		{
			droneBag.SetSlots(slots);
			drone.SendSyncData((ushort)8);
		}
		if (!AllUnlockedEmpty(slots, lockedSlots))
		{
			NotifyCrateFull(drone);
		}
	}

	private static bool AllUnlockedEmpty(ItemStack[] slots, PackedBoolArray lk)
	{
		for (int i = 0; i < slots.Length; i++)
		{
			if (!IsLocked(lk, i) && slots[i] != null && !slots[i].IsEmpty())
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsZombieBag(EntityLootContainer loot)
	{
		try
		{
			EntityClass val = EntityClass.list[((Entity)loot).entityClass];
			return val != null && val.entityClassName != null && val.entityClassName.StartsWith("EntityLootContainer", StringComparison.Ordinal);
		}
		catch
		{
			return false;
		}
	}

	private static void NotifyDroneFull(EntityDrone drone)
	{
		int entityId = ((Entity)drone).entityId;
		float time = Time.time;
		if (_nextFullMsg.TryGetValue(entityId, out var value) && time < value)
		{
			return;
		}
		_nextFullMsg[entityId] = time + 5f;
		Tell(drone, "Drone storage is full");
		try
		{
			((Entity)drone).PlayOneShot("drone_command", false, false, false, (AnimationEvent)null, 1f);
		}
		catch
		{
		}
	}

	private static void NotifyCrateFull(EntityDrone drone)
	{
		int entityId = ((Entity)drone).entityId;
		float time = Time.time;
		if (!_nextCrateMsg.TryGetValue(entityId, out var value) || !(time < value))
		{
			_nextCrateMsg[entityId] = time + 5f;
			Tell(drone, "Drone drop-off crate is full");
		}
	}

	private static void Tell(EntityDrone drone, string text)
	{
		EntityPlayer val = OwnerOf(drone);
		if ((Object)(object)val != (Object)null)
		{
			GameManager.ShowTooltipMP(val, text, "");
		}
	}
}
