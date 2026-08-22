using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PoiLootVacuum
{
    public class ConsoleCmdCollectLoot : ConsoleCmdAbstract
    {
        public override string getDescription() =>
            "Collect nearby POI loot into player-placed crates.";

        public override string getHelp() =>
            "Usage:\n" +
            "  cl [radius=40]       collect all POI loot within radius into your crates\n" +
            "  cl scan [radius=40]  dry-run: count sources and free destination slots";

        public override string[] getCommands() => new[] { "collectloot", "cl" };

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            var ci = _senderInfo.RemoteClientInfo;
            if (ci == null) return; // server console — not supported

            var world = GameManager.Instance.World;
            var player = world.GetEntity(ci.entityId) as EntityPlayer;
            if (player == null) return;

            bool scanOnly = false;
            float radius = 40f;
            int argIdx = 0;

            if (_params.Count > argIdx && _params[argIdx].Equals("scan", StringComparison.OrdinalIgnoreCase))
            {
                scanOnly = true;
                argIdx++;
            }
            if (_params.Count > argIdx && float.TryParse(_params[argIdx], out float r))
                radius = Mathf.Max(1f, r);

            var pos = player.position;
            int playerId = player.entityId;
            var lm = GameManager.Instance.lootManager;

            // Collect destination crates (player-placed, not in use)
            var dests = new List<ITileEntityLootable>();
            ForEachTileEntity(world, pos, radius, te =>
            {
                if (!(te is TileEntityComposite tec) || !tec.PlayerPlaced) return;
                var loot = tec.GetFeature<ITileEntityLootable>();
                if (loot != null && !loot.IsUserAccessing()) dests.Add(loot);
            });

            if (dests.Count == 0)
            {
                Reply(ci, $"[CL] No player-placed crates within {radius:F0}m.");
                return;
            }

            if (scanOnly)
            {
                int wc = 0, eb = 0, free = 0;
                ForEachTileEntity(world, pos, radius, te =>
                {
                    if (!(te is TileEntityComposite tec) || tec.PlayerPlaced) return;
                    var loot = tec.GetFeature<ITileEntityLootable>();
                    if (loot != null && !string.IsNullOrEmpty(loot.lootListName) && !IsLocked(tec)) wc++;
                });
                ForEachEntityBag(world, pos, radius, (bag, ent) => eb++);
                foreach (var d in dests)
                    foreach (var slot in d.items)
                        if (slot == null || slot.IsEmpty()) free++;
                Reply(ci, $"[Scan r={radius:F0}] {wc} containers + {eb} bags → {dests.Count} crates ({free} free slots)");
                return;
            }

            int stacks = 0, wContainers = 0, eBags = 0, rolled = 0;

            // World containers
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

            // Entity bags (zombie drops, backpacks, supply crates)
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

            Reply(ci, $"Collected {stacks} stacks ({wContainers} containers, {eBags} bags, {rolled} rolls) → {dests.Count} crates");
        }

        static bool TransferItems(ItemStack[] src, List<ITileEntityLootable> dests, EntityPlayer player, ref int stacks)
        {
            bool any = false;
            for (int i = 0; i < src.Length; i++)
            {
                var stack = src[i];
                if (stack == null || stack.IsEmpty()) continue;
                if (!LootFilter.ShouldPickUp(stack, player)) continue;

                int moved = 0;
                foreach (var dest in dests)
                    moved += LootSorter.MoveStack(ref stack, dest);

                if (moved > 0)
                {
                    src[i] = stack.IsEmpty() ? new ItemStack(ItemValue.None, 0) : stack;
                    stacks++;
                    any = true;
                }
            }
            return any;
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
                    float dx = wp.x - pos.x, dz = wp.z - pos.z;
                    if (dx * dx + dz * dz > r2) continue;
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
                float dx = ent.position.x - pos.x, dz = ent.position.z - pos.z;
                if (dx * dx + dz * dz > r2) continue;
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

        static void Reply(ClientInfo ci, string text)
        {
            try
            {
                GameManager.Instance.ChatMessageServer(
                    null, EChatType.Whisper, -1, text,
                    new List<int> { ci.entityId },
                    EMessageSender.Server);
            }
            catch { }
        }
    }
}
