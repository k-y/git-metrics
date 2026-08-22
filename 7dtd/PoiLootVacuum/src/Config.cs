using UnityEngine;

namespace PoiLootVacuum
{
    public enum DestinationMode { CrateOnly, InventoryOnly, InventoryThenCrate, CrateThenInventory }

    public static class Config
    {
        public static KeyCode         CollectKey       = KeyCode.F10;
        public static float           Radius           = 40f;
        public static bool            PickupAll        = false;
        public static DestinationMode Destination      = DestinationMode.CrateOnly;
        public static bool            PickupWeapons    = true;
        public static bool            PickupTools      = true;
        public static bool            PickupArmor      = true;
        public static bool            PickupAmmo       = true;
        public static bool            PickupFood       = true;
        public static bool            PickupMedicine   = true;
        public static bool            PickupBooks      = true;
        public static bool            PickupReadBooks  = false;
        public static bool            PickupComponents = true;
        public static bool            PickupResources  = true;
        public static bool            PickupMisc       = true;
    }
}
