using System;

namespace PoiLootVacuum
{
    public static class LootSorter
    {
        public static bool IsPlayerStorage(TileEntityComposite tec) =>
            tec != null && tec.PlayerPlaced;

        // Returns number of items moved from stack into dest; stack is reduced in-place.
        public static int MoveStack(ref ItemStack stack, ITileEntityLootable dest)
        {
            if (stack == null || stack.IsEmpty()) return 0;
            int max = GetMaxStack(stack.itemValue.ItemClass);
            int moved = 0;

            // Merge with existing partial stacks
            for (int i = 0; i < dest.items.Length && stack.count > 0; i++)
            {
                var slot = dest.items[i];
                if (slot == null || slot.IsEmpty()) continue;
                if (slot.itemValue.type != stack.itemValue.type) continue;
                int canAdd = max - slot.count;
                if (canAdd <= 0) continue;
                int toAdd = Math.Min(canAdd, stack.count);
                dest.UpdateSlot(i, new ItemStack(slot.itemValue, slot.count + toAdd));
                stack = new ItemStack(stack.itemValue, stack.count - toAdd);
                moved += toAdd;
            }

            // Fill empty slots
            for (int i = 0; i < dest.items.Length && stack.count > 0; i++)
            {
                if (dest.items[i] != null && !dest.items[i].IsEmpty()) continue;
                int toAdd = Math.Min(max, stack.count);
                dest.UpdateSlot(i, new ItemStack(stack.itemValue, toAdd));
                stack = new ItemStack(stack.itemValue, stack.count - toAdd);
                moved += toAdd;
            }

            return moved;
        }

        public static int GetMaxStack(ItemClass ic)
        {
            try { return ic?.Stacknumber.Value ?? 1; }
            catch { return 1; }
        }
    }
}
