namespace Gui
{
    /// <summary>
    /// Tuning numbers for the shop, rewards, and combat.
    /// </summary>
    internal static class GameConfig
    {
        public const int StartingGold = 400;
        public const int WinReward = 250;
        public const int LoseReward = 100;
        public const int DrawReward = 150;
        public const int HeartHealth = 20;            // 1 heart = 20 HP (5 hearts = 100 HP)
        public const int BaseHeatGain = 12;
        public const int ComboChargeBonusDamage = 6;  // extra damage per charge, spent on a Finisher
        public const int MaxComboCharge = 5;
    }
}
