using System;
using System.Collections.Generic;
using UnityEngine;

namespace DroneLootVacuum;

public static class LootSorter
{
	public const string CmdId = "dlvSortLoot";

	public const string CmdIcon = "fetch_loot_down";

	public static bool IsPlayerStorage(TileEntityComposite comp, ITileEntityLootable loot)
	{
		try
		{
			TEFeatureStorage val = ((comp != null) ? comp.GetFeature<TEFeatureStorage>() : null);
			if (val != null)
			{
				return val.bPlayerStorage;
			}
			return comp != null && comp.PlayerPlaced;
		}
		catch
		{
			return false;
		}
	}

	public static void SortFor(EntityDrone drone, EntityPlayer player)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!Config.SortLootEnabled || (Object)(object)drone == (Object)null || (Object)(object)player == (Object)null || DroneBusy.Is(((Entity)drone).entityId))
			{
				return;
			}
			World world = ((Entity)drone).world;
			float depositRadius = Config.DepositRadius;
			if (world == null || depositRadius <= 0f)
			{
				return;
			}
			float num = depositRadius * depositRadius;
			Vector3 position = ((Entity)drone).position;
			PersistentPlayerData persistentPlayerData = player.PersistentPlayerData;
			PlatformUserIdentifierAbs me = ((persistentPlayerData != null) ? persistentPlayerData.PrimaryId : null);
			List<ITileEntityLootable> list = new List<ITileEntityLootable>();
			List<ITileEntityLootable> list2 = new List<ITileEntityLootable>();
			List<ITileEntity> list3 = new List<ITileEntity>();
			int num2 = Utils.Fastfloor((position.x - depositRadius) / 16f);
			int num3 = Utils.Fastfloor((position.x + depositRadius) / 16f);
			int num4 = Utils.Fastfloor((position.z - depositRadius) / 16f);
			int num5 = Utils.Fastfloor((position.z + depositRadius) / 16f);
			for (int i = num4; i <= num5; i++)
			{
				for (int j = num2; j <= num3; j++)
				{
					IChunk chunkFromWorldPos = ((WorldBase)world).GetChunkFromWorldPos(j * 16, 0, i * 16);
					List<TileEntity> list4 = ((Chunk)(((chunkFromWorldPos is Chunk) ? chunkFromWorldPos : null)?)).tileEntities?.list;
					if (list4 == null)
					{
						continue;
					}
					for (int k = 0; k < list4.Count; k++)
					{
						TileEntity val = list4[k];
						TileEntityComposite val2 = (TileEntityComposite)(object)((val is TileEntityComposite) ? val : null);
						ITileEntityLootable val3 = (ITileEntityLootable)((val2 != null) ? ((object)val2.GetFeature<ITileEntityLootable>()) : ((object)((val is ITileEntityLootable) ? val : null)));
						if (val3 == null || val3.items == null || !IsPlayerStorage(val2, val3) || ((ITileEntity)val3).IsUserAccessing())
						{
							continue;
						}
						Vector3i val4 = val.ToWorldPos();
						if (!(Vector3.SqrMagnitude(((Vector3i)(ref val4)).ToVector3() + new Vector3(0.5f, 0.5f, 0.5f) - position) > num))
						{
							ITileEntitySignable selfOrFeature = TileEntityExtensions.GetSelfOrFeature<ITileEntitySignable>((ITileEntity)(object)val3);
							object obj;
							if (selfOrFeature == null)
							{
								obj = null;
							}
							else
							{
								AuthoredText authoredText = selfOrFeature.GetAuthoredText();
								obj = ((authoredText != null) ? authoredText.Text : null);
							}
							string text = (string)obj;
							if (!string.IsNullOrEmpty(text) && text.Trim().Equals(Config.DepositLabel, StringComparison.OrdinalIgnoreCase))
							{
								list.Add(val3);
							}
							else if (OwnedByMeOrAlly(val2, me))
							{
								list2.Add(val3);
							}
						}
					}
				}
			}
			if (list2.Count == 0)
			{
				Tell(player, "Sort Loot: no storage of yours nearby");
				return;
			}
			int num6 = 0;
			Bag bag = ((Entity)drone).bag;
			if (bag != null)
			{
				ItemStack[] slots = bag.GetSlots();
				PackedBoolArray lockedSlots = bag.LockedSlots;
				int num7 = 0;
				for (int l = 0; l < slots.Length; l++)
				{
					if (slots[l] == null || slots[l].IsEmpty() || (lockedSlots != null && l < lockedSlots.Length && lockedSlots[l]))
					{
						continue;
					}
					int num8 = MoveStack(slots[l], list2, list3);
					if (num8 > 0)
					{
						if (num8 >= slots[l].count)
						{
							slots[l] = ItemStack.Empty.Clone();
						}
						else
						{
							ItemStack obj2 = slots[l];
							obj2.count -= num8;
						}
						num7++;
					}
				}
				if (num7 > 0)
				{
					bag.SetSlots(slots);
					drone.SendSyncData((ushort)8);
					num6 += num7;
				}
			}
			for (int m = 0; m < list.Count; m++)
			{
				ITileEntityLootable val5 = list[m];
				ItemStack[] items = val5.items;
				if (items == null)
				{
					continue;
				}
				int num9 = 0;
				for (int n = 0; n < items.Length; n++)
				{
					if (items[n] == null || items[n].IsEmpty())
					{
						continue;
					}
					int num10 = MoveStack(items[n], list2, list3);
					if (num10 > 0)
					{
						if (num10 >= items[n].count)
						{
							val5.UpdateSlot(n, ItemStack.Empty);
						}
						else
						{
							ItemStack val6 = items[n].Clone();
							val6.count -= num10;
							val5.UpdateSlot(n, val6);
						}
						num9++;
					}
				}
				if (num9 > 0)
				{
					((ITileEntity)val5).SetModified();
					num6 += num9;
				}
			}
			for (int num11 = 0; num11 < list3.Count; num11++)
			{
				list3[num11].SetModified();
			}
			Tell(player, (num6 > 0) ? string.Format("Sorted {0} stack{1} into nearby storage", num6, (num6 == 1) ? "" : "s") : "Sort Loot: nothing matched nearby storage");
		}
		catch (Exception ex)
		{
			Log.Error("[DroneLootVacuum] sort: " + ex.Message);
		}
	}

	private static int MoveStack(ItemStack stack, List<ITileEntityLootable> targets, List<ITileEntity> touched)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected O, but got Unknown
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Expected O, but got Unknown
		int num = MaxStack(stack);
		if (num <= 0)
		{
			return 0;
		}
		int num2 = stack.count;
		for (int i = 0; i < targets.Count; i++)
		{
			if (num2 <= 0)
			{
				break;
			}
			ITileEntityLootable val = targets[i];
			ItemStack[] array = ((val != null) ? val.items : null);
			if (array == null)
			{
				continue;
			}
			bool flag = false;
			bool flag2 = false;
			for (int j = 0; j < array.Length; j++)
			{
				if (num2 <= 0)
				{
					break;
				}
				ItemStack val2 = array[j];
				if (val2 == null || val2.IsEmpty() || val2.itemValue.type != stack.itemValue.type)
				{
					continue;
				}
				flag2 = true;
				if (val2.CanStackWith(new ItemStack(stack.itemValue.Clone(), 1), false))
				{
					int num3 = num - val2.count;
					if (num3 > 0)
					{
						int num4 = Math.Min(num3, num2);
						ItemStack val3 = val2.Clone();
						val3.count += num4;
						val.UpdateSlot(j, val3);
						num2 -= num4;
						flag = true;
					}
				}
			}
			int num5 = 0;
			while (flag2 && num2 > 0 && num5 < array.Length)
			{
				ItemStack val4 = array[num5];
				if (val4 == null || val4.IsEmpty())
				{
					int num6 = Math.Min(num, num2);
					val.UpdateSlot(num5, new ItemStack(stack.itemValue.Clone(), num6));
					num2 -= num6;
					flag = true;
				}
				num5++;
			}
			if (flag)
			{
				ITileEntity val5 = (ITileEntity)(object)val;
				if (val5 != null && !touched.Contains(val5))
				{
					touched.Add(val5);
				}
			}
		}
		return stack.count - num2;
	}

	private static int MaxStack(ItemStack stack)
	{
		try
		{
			ItemClass forId = ItemClass.GetForId(stack.itemValue.type);
			return (forId == null) ? 1 : Math.Max(1, forId.Stacknumber.Value);
		}
		catch
		{
			return 1;
		}
	}

	private static bool OwnedByMeOrAlly(TileEntityComposite comp, PlatformUserIdentifierAbs me)
	{
		try
		{
			TEFeatureLockable val = ((comp != null) ? comp.GetFeature<TEFeatureLockable>() : null);
			if (val == null)
			{
				return true;
			}
			PlatformUserIdentifierAbs owner = val.GetOwner();
			if (owner == null)
			{
				return true;
			}
			if (me == null)
			{
				return false;
			}
			if (val.IsUserAllowed(me))
			{
				return true;
			}
			PersistentPlayerList val2 = GameManager.Instance?.persistentPlayers;
			return val2?.Allies != null && val2.Allies.IsAlly(owner, me);
		}
		catch
		{
			return false;
		}
	}

	private static void Tell(EntityPlayer p, string text)
	{
		try
		{
			if ((Object)(object)p != (Object)null)
			{
				GameManager.ShowTooltipMP(p, text, "");
			}
		}
		catch
		{
		}
	}
}
