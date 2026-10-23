namespace Gui
{
    /// <summary>
    /// A battle skin: a playstyle sidegrade with a bonus and a drawback.
    /// </summary>
    internal class SkinInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public float ArmorEffectiveness { get; set; } = 1f;
        public float DashSpeed { get; set; } = 1f;
        public float HeatGain { get; set; } = 1f;
        public int HealthModifier { get; set; }
        public float DamageTaken { get; set; } = 1f;
        public int BonusComboCharge { get; set; }
        public int Price { get; set; }
    }
}
