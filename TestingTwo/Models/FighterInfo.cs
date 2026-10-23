using System.Drawing;

namespace Gui
{
    /// <summary>
    /// A playable character: name, fighting style, colors, and special skill.
    /// </summary>
    internal class FighterInfo
    {
        public string Name { get; set; } = "";
        public string Style { get; set; } = "";
        public Color PrimaryColor { get; set; }
        public Color AuraColor { get; set; }
        public string SpecialSkill { get; set; } = "";
    }
}
