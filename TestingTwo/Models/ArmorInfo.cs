namespace Gui
{
    /// <summary>
    /// A piece of armor that blocks a flat amount of damage from every hit.
    /// </summary>
    internal class ArmorInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int DamageReduction { get; set; }
        public int Price { get; set; }
    }
}
