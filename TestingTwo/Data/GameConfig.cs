namespace Gui
{
    /// <summary>
    /// Tuning numbers for shop, rewards, 3-round aging, and Chrono-Shadow mechanics.
    /// </summary>
    internal static class GameConfig
    {
        public const int StartingGold = 400;
        public const int WinReward = 250;
        public const int LoseReward = 100;
        public const int DrawReward = 150;
        public const int HeartHealth = 20;            // 1 heart = 20 HP
        public const int BaseHeatGain = 12;
        public const int ComboChargeBonusDamage = 6;
        public const int MaxComboCharge = 5;

        // --- ROUNDS & MORTAL TOLL AGING ---
        public const int RoundsToWin = 2;              // Best of 3 rounds
        public const float YouthHpMult = 1.00f;        // 100% HP (Round 1)
        public const float VeteranHpMult = 0.85f;      // -15% Max HP
        public const float ElderHpMult = 0.65f;        // Drastically reduced HP

        public const float VeteranDmgMult = 1.45f;     // Increased attack damage
        public const float ElderDmgMult = 2.40f;       // Massive damage (3-4 hit KO power)

        public const float VeteranChipDmg = 0.35f;      // Heavy chip damage through block
        public const float ElderChipDmg = 0.50f;        // Devastating chip damage

        // --- CHRONO-SHADOW MECHANIC ---
        public const float MaxShadowMeter = 100f;
        public const float ShadowCost = 35f;            // Meter spent per phantom
        public const float ShadowDelaySeconds = 3.0f;  // 3-second delay
    }

    public enum AgeStage
    {
        Youth = 0,    // Double-dash, fast movement, long combo strings
        Veteran = 1,  // No double dash, -15% HP, +45% Dmg, high chip damage
        Elder = 2     // Slowest walk, low HP, Hyper Armor, massive 3-4 hit KO power
    }
}