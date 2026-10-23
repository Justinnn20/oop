using System;

namespace Gui
{
    /// <summary>
    /// Pure combat math. Takes loadouts in, gives numbers out, and touches no UI.
    /// </summary>
    internal static class CombatRules
    {
        /// <summary>Movement speed multiplier from the equipped skin.</summary>
        public static float DashSpeed(Loadout loadout)
        {
            return GameData.SkinCatalog[loadout.SkinIndex].DashSpeed;
        }

        /// <summary>Heat gained from landing a hit, after the skin bonus.</summary>
        public static int HeatGain(Loadout loadout)
        {
            return (int)Math.Round(GameConfig.BaseHeatGain * GameData.SkinCatalog[loadout.SkinIndex].HeatGain);
        }

        /// <summary>Damage after armor (flat reduction) and skin modifiers. Always at least 1.</summary>
        public static int DamageTaken(Loadout defender, int rawDamage)
        {
            SkinInfo skin = GameData.SkinCatalog[defender.SkinIndex];

            float armorReduction = GameData.ArmorCatalog[defender.ArmorIndex].DamageReduction * skin.ArmorEffectiveness;
            float damage = (rawDamage - armorReduction) * skin.DamageTaken;
            return Math.Max(1, (int)Math.Round(damage));
        }

        /// <summary>
        /// Landing slot 1 -> 2 -> 3 in order completes a "pre-built combo" and grants Combo Charge.
        /// Returns true when a combo was just completed.
        /// </summary>
        public static bool RegisterComboStep(Loadout loadout, int slot)
        {
            if (slot == loadout.ComboStep) loadout.ComboStep++;
            else loadout.ComboStep = (slot == 0) ? 1 : 0;

            if (loadout.ComboStep < 3) return false;

            loadout.ComboStep = 0;
            int gained = 1 + GameData.SkinCatalog[loadout.SkinIndex].BonusComboCharge;
            loadout.ComboCharge = Math.Min(GameConfig.MaxComboCharge, loadout.ComboCharge + gained);
            return true;
        }
    }
}
