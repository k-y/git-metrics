using UnityEngine;

namespace EntityLabels
{
    public static class Config
    {
        public static KeyCode ToggleKey     = KeyCode.F9;
        public static float   Radius        = 80f;
        public static int     FontSize      = 11;
        public static bool    ShowType      = true;
        public static bool    ShowHealth    = true;
        public static bool    ShowDist      = true;
        public static Color   PlayerColor   = new Color(0f,    0.8f, 1f);
        public static Color   TraderColor   = new Color(0f,    1f,   0.4f);
        public static Color   AnimalColor   = new Color(0.9f,  0.7f, 0.2f);
        public static Color   ZombieColor   = new Color(0.85f, 0.85f,0.85f);
        public static Color   MiniBossColor = new Color(1f,    0.5f, 0f);
        public static Color   BossColor     = new Color(1f,    0.15f,0.15f);
        public static bool    ShowPlayers   = true;
        public static bool    ShowTraders   = true;
        public static bool    ShowAnimals   = true;
        public static bool    ShowZombies   = true;
        public static bool    ShowMiniBosses= true;
        public static bool    ShowBosses    = true;
    }
}
