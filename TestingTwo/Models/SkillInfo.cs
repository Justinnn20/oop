namespace Gui
{
    internal enum SkillType { Starter, Extender, Finisher, Utility }

    /// <summary>
    /// One equippable attack: damage, reach, heat cost, and special effects.
    /// </summary>
    internal class SkillInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public SkillType Type { get; set; }
        public int Damage { get; set; }
        public float Launch { get; set; }
        public int Reach { get; set; } = 140;
        public int HeatCost { get; set; }
        public float Lunge { get; set; }
        public float Knockback { get; set; } = 14;
        public int StunFrames { get; set; } = 14;
        public int ActionFrames { get; set; } = 12;
        public int HeatDrain { get; set; }
        public int Action { get; set; } = 1;
        public int Price { get; set; }
    }
}
