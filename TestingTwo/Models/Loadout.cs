using System.Collections.Generic;

namespace Gui
{
    /// <summary>
    /// Everything one player owns and has equipped, plus their combo progress during a match.
    /// </summary>
    internal class Loadout
    {
        public int Gold = GameConfig.StartingGold;
        public int[] SkillSlots = { 0, 2, 4 };
        public HashSet<int> OwnedSkills = new HashSet<int> { 0, 2, 4 };
        public int ArmorIndex = 0;
        public HashSet<int> OwnedArmor = new HashSet<int> { 0 };
        public int SkinIndex = 0;
        public HashSet<int> OwnedSkins = new HashSet<int> { 0 };

        // Match state
        public int ComboStep = 0;
        public int ComboCharge = 0;

        public void ResetMatchState()
        {
            ComboStep = 0;
            ComboCharge = 0;
        }
    }
}
