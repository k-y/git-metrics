using System;
using System.Reflection;
using HarmonyLib;
using InControl;

namespace ToolbeltExpansion
{
    public static class ToolbeltHotkeyPatches
    {
        private static readonly MethodInfo CreatePlayerActionMethod =
            AccessTools.Method(typeof(PlayerActionSet), "CreatePlayerAction", new[] { typeof(string) });

        public static void AddHotkeys(PlayerActionsLocal actions)
        {
            if (actions == null) return;
            if (CreatePlayerActionMethod == null)
            {
                Log.Warning("[ToolbeltExpansion] PlayerActionSet.CreatePlayerAction not found; expanded slot hotkeys not added.");
                return;
            }

            int added = 0;
            for (int slotNumber = ToolbeltSlotPatches.VanillaPlaySlots + 1;
                 slotNumber <= ToolbeltSlotPatches.ExpandedSlots;
                 slotNumber++)
            {
                string actionName = GetActionName(slotNumber);
                if (HasAction(actions, actionName)) continue;
                AddToolbeltSlot(actions, slotNumber, actionName);
                added++;
            }

            if (added > 0)
            {
                Log.Out("[ToolbeltExpansion] Added " + added + " expanded toolbelt hotkey action(s) for slots " +
                        "11-" + ToolbeltSlotPatches.ExpandedSlots + ". Bind them via Options > Controls > Toolbelt.");
            }
        }

        private static bool HasAction(PlayerActionsLocal actions, string actionName)
        {
            for (int i = 0; i < actions.InventoryActions.Count; i++)
            {
                if (actions.InventoryActions[i]?.Name == actionName) return true;
            }
            return false;
        }

        private static string GetActionName(int slotNumber)
        {
            return "Inventory" + slotNumber;
        }

        private static void AddToolbeltSlot(PlayerActionsLocal actions, int slotNumber, string actionName)
        {
            string nameKey = "inpActInventorySlot" + slotNumber + "Name";
            var action = (PlayerAction)CreatePlayerActionMethod.Invoke(actions, new object[] { actionName });
            action.UserData = new PlayerActionData.ActionUserData(
                nameKey,
                "inpActInventorySlot" + slotNumber + "Desc",
                PlayerActionData.GroupToolbelt);
            actions.InventoryActions.Add(action);
        }

        [HarmonyPatch(typeof(PlayerActionsLocal), MethodType.Constructor, new Type[0])]
        public static class PlayerActionsLocalCtor_Patch
        {
            public static void Postfix(PlayerActionsLocal __instance) { AddHotkeys(__instance); }
        }
    }
}
