using System;

namespace EntityLabels
{
    public enum EntityCategory { Player, Trader, Animal, Zombie, MiniBoss, Boss }

    public static class EntityClassifier
    {
        public static EntityCategory Classify(EntityAlive entity)
        {
            if (entity is EntityPlayer) return EntityCategory.Player;
            if (entity is EntityNPC)   return EntityCategory.Trader;

            // Check boss/miniboss tags before type for animals and zombies
            bool isBoss     = HasTag(entity, "boss");
            bool isMiniB    = !isBoss && HasTag(entity, "miniboss");

            if (entity is EntityAnimal)
            {
                if (isBoss)  return EntityCategory.Boss;
                if (isMiniB) return EntityCategory.MiniBoss;
                return EntityCategory.Animal;
            }

            // EntityZombie and any unknown EntityAlive (modded enemies)
            if (isBoss)  return EntityCategory.Boss;
            if (isMiniB) return EntityCategory.MiniBoss;
            return EntityCategory.Zombie;
        }

        static bool HasTag(EntityAlive entity, string tagName)
        {
            try
            {
                string ts = entity.entityClass?.Tags.ToString();
                return ts != null && ts.IndexOf(tagName, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }
    }
}
