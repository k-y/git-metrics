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
            if (scan)
            {
                Collect(world, player, Config.Radius, true);
            }
            else
            {
                var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
                if (cm == null || cm.IsServer)
                    Collect(world, player, Config.Radius, false);
                else
                    cm.SendToServer(NetPackageManager.GetPackage<NetPackagePoiVacuum>()
                        .Setup(player.entityId, Config.Radius), false);
            }
        }

        internal static void Collect(World world, EntityPlayer player, float radius, bool scanOnly)
        {
            var pos = player.position;
            int playerId = player.entityId;
            var lm = GameManager.Instance.lootManager;
            var mode = Config.Destination;

            bool useDrone     = mode == DestinationMode.DroneOnly
                             || mode == DestinationMode.DroneThenInventory
                             || mode == DestinationMode.DroneThenCrate
                             || mode == DestinationMode.DroneThenInventoryThenCrate
                             || mode == DestinationMode.DroneThenCrateThenInventory;
            bool useCrates    = mode == DestinationMode.CrateOnly
                             || mode == DestinationMode.InventoryThenCrate
                             || mode == DestinationMode.CrateThenInventory
                             || mode == DestinationMode.DroneThenCrate
                             || mode == DestinationMode.DroneThenInventoryThenCrate
                             || mode == DestinationMode.DroneThenCrateThenInventory;
            bool useInventory = mode == DestinationMode.InventoryOnly
                             || mode == DestinationMode.InventoryThenCrate
                             || mode == DestinationMode.CrateThenInventory
                             || mode == DestinationMode.DroneThenInventory
                             || mode == DestinationMode.DroneThenInventoryThenCrate
                             || mode == DestinationMode.DroneThenCrateThenInventory;

            Bag droneBag = useDrone ? GetBag(GetPlayerDrone(world, pos)) : null;

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

            if (mode == DestinationMode.DroneOnly && droneBag == null)
            {
                Tip(player, "[CL] No drone found nearby.");
                return;
            }
            if (mode == DestinationMode.CrateOnly && dests.Count == 0)
            {
                Tip(player, $"[CL] No player-placed crates within {radius:F0}m.");
                return;
            }
            if (mode == DestinationMode.DroneThenCrate && droneBag == null && dests.Count == 0)
            {
                Tip(player, $"[CL] No drone or crates found within {radius:F0}m.");
                return;
            }

            if (scanOnly)
            {
                int wc = 0, eb = 0, freeCrate = 0, freeInv = 0, freeDrone = 0;
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
                if (droneBag != null)
                {
                    var ds = droneBag.GetSlots();
                    if (ds != null)
                        foreach (var s in ds)
                            if (s == null || s.IsEmpty()) freeDrone++;
                }
                string droneLabel  = droneBag != null ? $"drone ({freeDrone} free)" : "drone (none)";
                string invLabel    = $"inventory ({freeInv} free)";
                string crateLabel  = $"{dests.Count} crates ({freeCrate} free)";
                string destInfo;
                switch (mode)
                {
                    case DestinationMode.CrateOnly:                  destInfo = crateLabel; break;
                    case DestinationMode.InventoryOnly:              destInfo = invLabel; break;
                    case DestinationMode.InventoryThenCrate:         destInfo = $"{invLabel} then {crateLabel}"; break;
                    case DestinationMode.CrateThenInventory:         destInfo = $"{crateLabel} then {invLabel}"; break;
                    case DestinationMode.DroneOnly:                  destInfo = droneLabel; break;
                    case DestinationMode.DroneThenInventory:         destInfo = $"{droneLabel} then {invLabel}"; break;
                    case DestinationMode.DroneThenCrate:             destInfo = $"{droneLabel} then {crateLabel}"; break;
                    case DestinationMode.DroneThenInventoryThenCrate:destInfo = $"{droneLabel} then {invLabel} then {crateLabel}"; break;
                    default:                                         destInfo = $"{droneLabel} then {crateLabel} then {invLabel}"; break;
                }
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

                if (TransferItems(loot.items, droneBag, dests, player, ref stacks))
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
                if (TransferItems(bag.items, droneBag, dests, player, ref stacks))
                    eBags++;
            });

            foreach (var d in dests) d.SetModified();

            string destLabel;
            switch (mode)
            {
                case DestinationMode.CrateOnly:                   destLabel = $"{dests.Count} crates"; break;
                case DestinationMode.InventoryOnly:               destLabel = "inventory"; break;
                case DestinationMode.InventoryThenCrate:          destLabel = "inventory then crates"; break;
                case DestinationMode.CrateThenInventory:          destLabel = "crates then inventory"; break;
                case DestinationMode.DroneOnly:                   destLabel = "drone"; break;
                case DestinationMode.DroneThenInventory:          destLabel = "drone then inventory"; break;
                case DestinationMode.DroneThenCrate:              destLabel = "drone then crates"; break;
                case DestinationMode.DroneThenInventoryThenCrate: destLabel = "drone then inventory then crates"; break;
                default:                                          destLabel = "drone then crates then inventory"; break;
            }
            Tip(player, $"Collected {stacks} stacks ({wContainers} containers, {eBags} bags, {rolled} rolls) → {destLabel}");
        }

        static void Tip(EntityPlayer player, string text)
        {
            try { GameManager.ShowTooltipMP(player, text, ""); } catch { }
        }

        static bool TransferItems(ItemStack[] src, Bag droneBag, List<ITileEntityLootable> dests, EntityPlayer player, ref int stacks)
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
                    case DestinationMode.DroneOnly:
                        if (droneBag != null) moved += MoveToBag(ref stack, droneBag);
                        break;
                    case DestinationMode.DroneThenInventory:
                        if (droneBag != null) moved += MoveToBag(ref stack, droneBag);
                        if (stack.count > 0) moved += MoveToInventory(ref stack, player);
                        break;
                    case DestinationMode.DroneThenCrate:
                        if (droneBag != null) moved += MoveToBag(ref stack, droneBag);
                        if (stack.count > 0)
                            foreach (var dest in dests)
                                moved += LootSorter.MoveStack(ref stack, dest);
                        break;
                    case DestinationMode.DroneThenInventoryThenCrate:
                        if (droneBag != null) moved += MoveToBag(ref stack, droneBag);
                        if (stack.count > 0) moved += MoveToInventory(ref stack, player);
                        if (stack.count > 0)
                            foreach (var dest in dests)
                                moved += LootSorter.MoveStack(ref stack, dest);
                        break;
                    case DestinationMode.DroneThenCrateThenInventory:
                        if (droneBag != null) moved += MoveToBag(ref stack, droneBag);
                        if (stack.count > 0)
                            foreach (var dest in dests)
                                moved += LootSorter.MoveStack(ref stack, dest);
                        if (stack.count > 0) moved += MoveToInventory(ref stack, player);
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

        static int MoveToInventory(ref ItemStack stack, EntityPlayer player)
        {
            try
            {
                int max = LootSorter.GetMaxStack(stack.itemValue.ItemClass);
                int moved = 0;
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

                if (stack.count > 0 && player.bag != null)
                    moved += MoveToBag(ref stack, player.bag);

                return moved;
            }
            catch { return 0; }
        }

        static int MoveToBag(ref ItemStack stack, Bag bag)
        {
            try
            {
                var slots = bag.GetSlots();
                if (slots == null) return 0;
                int max = LootSorter.GetMaxStack(stack.itemValue.ItemClass);
                int moved = 0;

                for (int i = 0; i < slots.Length && stack.count > 0; i++)
                {
                    var slot = slots[i];
                    if (slot == null || slot.IsEmpty()) continue;
                    if (slot.itemValue.type != stack.itemValue.type) continue;
                    int canAdd = max - slot.count;
                    if (canAdd <= 0) continue;
                    int toAdd = Math.Min(canAdd, stack.count);
                    slots[i] = new ItemStack(slot.itemValue, slot.count + toAdd);
                    stack = new ItemStack(stack.itemValue, stack.count - toAdd);
                    moved += toAdd;
                }

                for (int i = 0; i < slots.Length && stack.count > 0; i++)
                {
                    if (slots[i] != null && !slots[i].IsEmpty()) continue;
                    int toAdd = Math.Min(max, stack.count);
                    slots[i] = new ItemStack(stack.itemValue, toAdd);
                    stack = new ItemStack(stack.itemValue, stack.count - toAdd);
                    moved += toAdd;
                }

                if (moved > 0) bag.SetSlots(slots);
                return moved;
            }
            catch { return 0; }
        }

        // Drone always follows the player; find it by type name within a close radius.
        static Entity GetPlayerDrone(World world, Vector3 pos)
        {
            try
            {
                float r2 = 20f * 20f;
                foreach (var kvp in world.Entities.dict)
                {
                    var e = kvp.Value;
                    if (e == null) continue;
                    if (e.GetType().Name.IndexOf("Drone", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    float dx = e.position.x - pos.x, dy = e.position.y - pos.y, dz = e.position.z - pos.z;
                    if (dx * dx + dy * dy + dz * dz <= r2) return e;
                }
            }
            catch { }
            return null;
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
