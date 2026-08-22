using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PoiLootVacuum
{
    public class LootVacuumBehaviour : MonoBehaviour
    {
        void Update()
        {
            if (!Input.GetKeyDown(Config.CollectKey)) return;
            var world = GameManager.Instance?.World;
            var player = world?.GetPrimaryPlayer();
            if (player == null) return;

            bool scan = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            Collect(world, player, Config.Radius, scan);
        }

        static void Collect(World world, EntityPlayerLocal player, float radius, bool scanOnly)
        {
            var pos = player.position;
            int playerId = player.entityId;
            var lm = GameManager.Instance.lootManager;
            var mode = Config.Destination;

            bool useCrates    = mode != DestinationMode.InventoryOnly;
            bool useInventory = mode != DestinationMode.InventoryOnly
                                    ? mode != DestinationMode.CrateOnly
                                    : true;

            var dests = new List<ITileEntityLootable>();
            if (useCrates)
            {
                ForEachTileEntity(world, pos, radius, te =>
                {
                    if (!(te is TileEntityComposite tec) || !tec.PlayerPlaced) return;
                    var loot = tec.GetFeature<ITileEntityLootable>();
                    if (loot != null && !loot.IsUserAccessing()) dests.Add(loot);
                });
            }

            bool cratesRequired = useCrates && !useInventory;
            if (cratesRequired && dests.Count == 0)
            {
                Tip(player, $"[CL] No player-placed crates within {radius:F0}m.");
                return;
            }

            if (scanOnly)
            {
                int wc = 0, eb = 0, freeCrate = 0, freeInv = 0;
                ForEachTileEntity(world, pos, radius, te =>
                {
                    if (!(te is TileEntityComposite tec) || tec.PlayerPlaced) return;
                    var loot = tec.GetFeature<ITileEntityLootable>();
                    if (loot != null && !string.IsNullOrEmpty(loot.lootListName) && !IsLocked(tec)) wc++;
                });
                ForEachEntityBag(world, pos, radius, (bag, ent) => eb++);
                foreach (var d in dests)
                    foreach (var slot in d.items)
                        if (slot == null || slot.IsEmpty()) freeCrate++;
                if (useInventory)
                {
                    int ic = player.inventory.GetItemCount();
                    for (int i = 0; i < ic; i++)
                    {
                        if (i == player.inventory.DUMMY_SLOT_IDX) continue;
                        var s = player.inventory.GetItem(i);
                        if (s == null || s.IsEmpty()) freeInv++;
                    }
                    var bagSlots = player.bag?.GetSlots();
                    if (bagSlots != null)
                        foreach (var s in bagSlots)
                            if (s == null || s.IsEmpty()) freeInv++;
                }
                string destInfo = mode == DestinationMode.CrateOnly      ? $"{dests.Count} crates ({freeCrate} free)"
                                : mode == DestinationMode.InventoryOnly   ? $"inventory ({freeInv} free)"
                                : mode == DestinationMode.InventoryThenCrate ? $"inventory ({freeInv} free) then {dests.Count} crates ({freeCrate} free)"
                                :                                             $"{dests.Count} crates ({freeCrate} free) then inventory ({freeInv} free)";
                Tip(player, $"[Scan r={radius:F0}] {wc} containers + {eb} bags → {destInfo}");
                return;
            }

            int stacks = 0, wContainers = 0, eBags = 0, rolled = 0;

            ForEachTileEntity(world, pos, radius, te =>
            {
                if (!(te is TileEntityComposite tec) || tec.PlayerPlaced) return;
                var loot = tec.GetFeature<ITileEntityLootable>();
                if (loot == null || string.IsNullOrEmpty(loot.lootListName)) return;
                if (IsLocked(tec) || loot.IsUserAccessing()) return;

                if (!loot.bTouched && lm != null)
                {
                    try { lm.LootContainerOpened(loot, playerId, ((ITileEntity)loot).blockValue.Block.Tags); }
                    catch { }
                    rolled++;
                }

                if (TransferItems(loot.items, dests, player, ref stacks))
                {
                    loot.SetModified();
                    wContainers++;
                }
            });

            ForEachEntityBag(world, pos, radius, (bag, ent) =>
            {
                if (!bag.Touched && lm != null)
                {
                    try { lm.LootBagOpened(bag, ent, playerId); }
                    catch { }
                    rolled++;
                }
                if (TransferItems(bag.items, dests, player, ref stacks))
                    eBags++;
            });

            foreach (var d in dests) d.SetModified();

            string dest = mode == DestinationMode.CrateOnly    ? $"{dests.Count} crates"
                        : mode == DestinationMode.InventoryOnly ? "inventory"
                        :                                          "inventory + crates";
            Tip(player, $"Collected {stacks} stacks ({wContainers} containers, {eBags} bags, {rolled} rolls) → {dest}");
        }

        static void Tip(EntityPlayerLocal player, string text)
        {
            try { GameManager.ShowTooltipMP(player, text, ""); } catch { }
        }

        static bool TransferItems(ItemStack[] src, List<ITileEntityLootable> dests, EntityPlayerLocal player, ref int stacks)
        {
            var mode = Config.Destination;
            bool any = false;
            for (int i = 0; i < src.Length; i++)
            {
                var stack = src[i];
                if (stack == null || stack.IsEmpty()) continue;
                if (!LootFilter.ShouldPickUp(stack, player)) continue;

                int moved = 0;
                switch (mode)
                {
                    case DestinationMode.CrateOnly:
                        foreach (var dest in dests)
                            moved += LootSorter.MoveStack(ref stack, dest);
                        break;
                    case DestinationMode.InventoryOnly:
                        moved += MoveToInventory(ref stack, player);
                        break;
                    case DestinationMode.InventoryThenCrate:
                        moved += MoveToInventory(ref stack, player);
                        if (stack.count > 0)
                            foreach (var dest in dests)
                                moved += LootSorter.MoveStack(ref stack, dest);
                        break;
                    case DestinationMode.CrateThenInventory:
                        foreach (var dest in dests)
                            moved += LootSorter.MoveStack(ref stack, dest);
                        if (stack.count > 0)
                            moved += MoveToInventory(ref stack, player);
                        break;
                }

                if (moved > 0)
                {
                    src[i] = stack.IsEmpty() ? new ItemStack(ItemValue.None, 0) : stack;
                    stacks++;
                    any = true;
                }
            }
            return any;
        }

        static int MoveToInventory(ref ItemStack stack, EntityPlayerLocal player)
        {
            try
            {
                int max = LootSorter.GetMaxStack(stack.itemValue.ItemClass);
                int moved = 0;

                // Toolbelt
                var inv = player.inventory;
                int invCount = inv.GetItemCount();

                for (int i = 0; i < invCount && stack.count > 0; i++)
                {
                    if (i == inv.DUMMY_SLOT_IDX) continue;
                    var slot = inv.GetItem(i);
                    if (slot == null || slot.IsEmpty()) continue;
                    if (slot.itemValue.type != stack.itemValue.type) continue;
                    int canAdd = max - slot.count;
                    if (canAdd <= 0) continue;
                    int toAdd = Math.Min(canAdd, stack.count);
                    inv.SetItem(i, new ItemStack(slot.itemValue, slot.count + toAdd));
                    stack = new ItemStack(stack.itemValue, stack.count - toAdd);
                    moved += toAdd;
                }

                for (int i = 0; i < invCount && stack.count > 0; i++)
                {
                    if (i == inv.DUMMY_SLOT_IDX) continue;
                    var slot = inv.GetItem(i);
                    if (slot != null && !slot.IsEmpty()) continue;
                    int toAdd = Math.Min(max, stack.count);
                    inv.SetItem(i, new ItemStack(stack.itemValue, toAdd));
                    stack = new ItemStack(stack.itemValue, stack.count - toAdd);
                    moved += toAdd;
                }

                // Backpack
                var bag = player.bag;
                if (bag != null && stack.count > 0)
                {
                    var bagSlots = bag.GetSlots();
                    if (bagSlots != null)
                    {
                        int movedToBag = 0;

                        for (int i = 0; i < bagSlots.Length && stack.count > 0; i++)
                        {
                            var slot = bagSlots[i];
                            if (slot == null || slot.IsEmpty()) continue;
                            if (slot.itemValue.type != stack.itemValue.type) continue;
                            int canAdd = max - slot.count;
                            if (canAdd <= 0) continue;
                            int toAdd = Math.Min(canAdd, stack.count);
                            bagSlots[i] = new ItemStack(slot.itemValue, slot.count + toAdd);
                            stack = new ItemStack(stack.itemValue, stack.count - toAdd);
                            movedToBag += toAdd;
                        }

                        for (int i = 0; i < bagSlots.Length && stack.count > 0; i++)
                        {
                            if (bagSlots[i] != null && !bagSlots[i].IsEmpty()) continue;
                            int toAdd = Math.Min(max, stack.count);
                            bagSlots[i] = new ItemStack(stack.itemValue, toAdd);
                            stack = new ItemStack(stack.itemValue, stack.count - toAdd);
                            movedToBag += toAdd;
                        }

                        if (movedToBag > 0)
                        {
                            bag.SetSlots(bagSlots);
                            moved += movedToBag;
                        }
                    }
                }

                return moved;
            }
            catch { return 0; }
        }

        static void ForEachTileEntity(World world, Vector3 pos, float radius, Action<TileEntity> action)
        {
            int x0 = Utils.Fastfloor((pos.x - radius) / 16f);
            int x1 = Utils.Fastfloor((pos.x + radius) / 16f);
            int z0 = Utils.Fastfloor((pos.z - radius) / 16f);
            int z1 = Utils.Fastfloor((pos.z + radius) / 16f);
            float r2 = radius * radius;
            for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
            {
                var chunk = ((WorldBase)world).GetChunkFromWorldPos(x * 16, 0, z * 16) as Chunk;
                var list = chunk?.tileEntities?.list;
                if (list == null) continue;
                for (int k = 0; k < list.Count; k++)
                {
                    var te = list[k];
                    if (te == null) continue;
                    var wp = te.ToWorldPos();
                    float dx = wp.x - pos.x, dy = wp.y - pos.y, dz = wp.z - pos.z;
                    if (dx * dx + dy * dy + dz * dz > r2) continue;
                    try { action(te); } catch { }
                }
            }
        }

        static void ForEachEntityBag(World world, Vector3 pos, float radius, Action<Bag, Entity> action)
        {
            float r2 = radius * radius;
            foreach (var kvp in world.Entities.dict)
            {
                var ent = kvp.Value;
                if (ent == null) continue;
                if (!(ent is EntityLootContainer || ent is EntityBackpack || ent is EntitySupplyCrate)) continue;
                float dx = ent.position.x - pos.x, dy = ent.position.y - pos.y, dz = ent.position.z - pos.z;
                if (dx * dx + dy * dy + dz * dz > r2) continue;
                var bag = GetBag(ent);
                if (bag == null) continue;
                try { action(bag, ent); } catch { }
            }
        }

        static Bag GetBag(Entity e)
        {
            try
            {
                var t = e.GetType();
                while (t != null)
                {
                    var f = t.GetField("bag", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    if (f != null) return f.GetValue(e) as Bag;
                    t = t.BaseType;
                }
            }
            catch { }
            return null;
        }

        static bool IsLocked(TileEntityComposite tec)
        {
            try
            {
                var lk = tec.GetFeature<TEFeatureLockable>();
                if (lk != null && lk.IsLocked()) return true;
                var lp = tec.GetFeature<TEFeatureLockPickable>();
                if (lp != null && lp.NeedsLockpicking()) return true;
            }
            catch { }
            return false;
        }
    }
}
