using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Gui
{
    /// <summary>
    /// Armory logic: buying, refunding, equipping, and building the CPU loadout.
    /// </summary>
    public partial class Form1 : Form
    {
        private void OpenArmory()
        {
            armoryPlayer = 1;
            armoryTab = 0;
            armoryCursor = 0;
            armoryMessage = "";
            currentState = GameState.Armory;
            Sfx.Play(700, 90);
        }

        private int ArmoryItemCount()
        {
            return armoryTab switch
            {
                0 => GameData.SkillCatalog.Length,
                1 => GameData.ArmorCatalog.Length,
                _ => GameData.SkinCatalog.Length
            };
        }

        private void HandleArmoryInput(KeyEventArgs e)
        {
            Loadout loadout = ActiveLoadout;
            int count = ArmoryItemCount();

            switch (e.KeyCode)
            {
                case Keys.Left:
                    armoryTab = (armoryTab + 2) % 3;
                    armoryCursor = 0;
                    armoryMessage = "";
                    break;
                case Keys.Right:
                    armoryTab = (armoryTab + 1) % 3;
                    armoryCursor = 0;
                    armoryMessage = "";
                    break;
                case Keys.Up:
                    armoryCursor = (armoryCursor - 1 + count) % count;
                    break;
                case Keys.Down:
                    armoryCursor = (armoryCursor + 1) % count;
                    break;
                case Keys.Enter:
                    BuyOrEquipSelected(loadout);
                    break;
                case Keys.D1:
                case Keys.NumPad1:
                    EquipSkillToSlot(loadout, 0);
                    break;
                case Keys.D2:
                case Keys.NumPad2:
                    EquipSkillToSlot(loadout, 1);
                    break;
                case Keys.D3:
                case Keys.NumPad3:
                    EquipSkillToSlot(loadout, 2);
                    break;
                case Keys.R:
                    RefundSelected(loadout);
                    break;
                case Keys.Space:
                    ConfirmLoadout();
                    break;
                case Keys.Back:
                    if (armoryPlayer == 2)
                    {
                        armoryPlayer = 1;
                        armoryTab = 0;
                        armoryCursor = 0;
                        armoryMessage = "";
                    }
                    else
                    {
                        currentState = GameState.ArenaSelect;
                    }
                    break;
            }
        }

        private bool CanRefund(bool owned, int price, string itemName)
        {
            if (!owned)
            {
                armoryMessage = $"You don't own {itemName}.";
                return false;
            }
            if (price == 0)
            {
                armoryMessage = $"{itemName} is free and can't be refunded.";
                return false;
            }
            return true;
        }

        // Returns a free skill that is not already in another slot, used when a refunded skill leaves its slot.
        private int FindFallbackSkill(Loadout loadout)
        {
            for (int i = 0; i < GameData.SkillCatalog.Length; i++)
            {
                if (GameData.SkillCatalog[i].Price == 0 && loadout.OwnedSkills.Contains(i) && Array.IndexOf(loadout.SkillSlots, i) < 0)
                    return i;
            }
            return 0;
        }

        // Sells the selected item back for its full purchase price (R key).
        private void RefundSelected(Loadout loadout)
        {
            int price;
            string itemName;

            if (armoryTab == 0)
            {
                SkillInfo skill = GameData.SkillCatalog[armoryCursor];
                price = skill.Price;
                itemName = skill.Name;
                if (!CanRefund(loadout.OwnedSkills.Contains(armoryCursor), price, itemName)) return;

                loadout.OwnedSkills.Remove(armoryCursor);
                for (int slot = 0; slot < loadout.SkillSlots.Length; slot++)
                {
                    if (loadout.SkillSlots[slot] == armoryCursor)
                        loadout.SkillSlots[slot] = FindFallbackSkill(loadout);
                }
            }
            else if (armoryTab == 1)
            {
                ArmorInfo armor = GameData.ArmorCatalog[armoryCursor];
                price = armor.Price;
                itemName = armor.Name;
                if (!CanRefund(loadout.OwnedArmor.Contains(armoryCursor), price, itemName)) return;

                loadout.OwnedArmor.Remove(armoryCursor);
                if (loadout.ArmorIndex == armoryCursor) { loadout.ArmorIndex = 0; TriggerArmoryFlash(false); }
            }
            else
            {
                SkinInfo skin = GameData.SkinCatalog[armoryCursor];
                price = skin.Price;
                itemName = skin.Name;
                if (!CanRefund(loadout.OwnedSkins.Contains(armoryCursor), price, itemName)) return;

                loadout.OwnedSkins.Remove(armoryCursor);
                if (loadout.SkinIndex == armoryCursor) { loadout.SkinIndex = 0; TriggerArmoryFlash(false); }
            }

            loadout.Gold += price;
            armoryMessage = $"Refunded {itemName} (+{price} gold).";
            Sfx.Play(500, 80);
        }

        private bool TryOwn(Loadout loadout, int price, HashSet<int> owned, int index, string itemName)
        {
            if (owned.Contains(index)) return true;

            if (loadout.Gold < price)
            {
                armoryMessage = $"Not enough gold for {itemName}.";
                Sfx.Play(180, 120);
                return false;
            }

            loadout.Gold -= price;
            owned.Add(index);
            armoryMessage = $"Purchased {itemName}!";
            Sfx.Play(900, 80);
            return true;
        }

        private void BuyOrEquipSelected(Loadout loadout)
        {
            if (armoryTab == 0)
            {
                SkillInfo skill = GameData.SkillCatalog[armoryCursor];
                if (loadout.OwnedSkills.Contains(armoryCursor))
                    armoryMessage = "Press 1, 2 or 3 to put this skill into a slot.";
                else if (TryOwn(loadout, skill.Price, loadout.OwnedSkills, armoryCursor, skill.Name))
                    armoryMessage = $"Purchased {skill.Name}! Press 1, 2 or 3 to equip it.";
            }
            else if (armoryTab == 1)
            {
                ArmorInfo armor = GameData.ArmorCatalog[armoryCursor];
                if (loadout.ArmorIndex == armoryCursor && armoryCursor != 0)
                {
                    loadout.ArmorIndex = 0;
                    armoryMessage = $"Unequipped {armor.Name}.";
                    TriggerArmoryFlash(false);
                }
                else if (TryOwn(loadout, armor.Price, loadout.OwnedArmor, armoryCursor, armor.Name))
                {
                    loadout.ArmorIndex = armoryCursor;
                    armoryMessage = $"Equipped {armor.Name}.";
                    TriggerArmoryFlash(true);
                }
            }
            else
            {
                SkinInfo skin = GameData.SkinCatalog[armoryCursor];
                if (loadout.SkinIndex == armoryCursor && armoryCursor != 0)
                {
                    loadout.SkinIndex = 0;
                    armoryMessage = $"Unequipped {skin.Name}.";
                    TriggerArmoryFlash(false);
                }
                else if (TryOwn(loadout, skin.Price, loadout.OwnedSkins, armoryCursor, skin.Name))
                {
                    loadout.SkinIndex = armoryCursor;
                    armoryMessage = $"Equipped {skin.Name}.";
                    TriggerArmoryFlash(true);
                }
            }
        }

        private void TriggerArmoryFlash(bool equipped)
        {
            armoryFlash = 14;
            armoryFlashColor = equipped ? Color.Gold : Color.Gray;
            Sfx.Play(equipped ? 820 : 360, 70);
        }

        private void EquipSkillToSlot(Loadout loadout, int slot)
        {
            if (armoryTab != 0) return;

            if (!loadout.OwnedSkills.Contains(armoryCursor))
            {
                armoryMessage = "Buy this skill first (ENTER).";
                return;
            }

            // If the skill is already in another slot, swap the two slots.
            int existingSlot = Array.IndexOf(loadout.SkillSlots, armoryCursor);
            if (existingSlot >= 0 && existingSlot != slot)
                loadout.SkillSlots[existingSlot] = loadout.SkillSlots[slot];

            loadout.SkillSlots[slot] = armoryCursor;
            armoryMessage = $"{GameData.SkillCatalog[armoryCursor].Name} equipped to slot {slot + 1}.";
            Sfx.Play(600, 60);
        }

        private void ConfirmLoadout()
        {
            Sfx.Play(750, 100);

            if (!isVsCPU && armoryPlayer == 1)
            {
                armoryPlayer = 2;
                armoryTab = 0;
                armoryCursor = 0;
                armoryMessage = "";
            }
            else
            {
                StartMatch();
            }
        }

        private Loadout BuildCpuLoadout()
        {
            Loadout cpu = new Loadout();
            cpu.SkillSlots[0] = PickRandomSkillOfType(SkillType.Starter);
            cpu.SkillSlots[1] = PickRandomSkillOfType(SkillType.Extender);
            cpu.SkillSlots[2] = PickRandomSkillOfType(SkillType.Finisher);
            cpu.ArmorIndex = rng.Next(0, GameData.ArmorCatalog.Length - 1);
            cpu.SkinIndex = rng.Next(0, GameData.SkinCatalog.Length);
            return cpu;
        }

        private int PickRandomSkillOfType(SkillType type)
        {
            List<int> matches = new List<int>();
            for (int i = 0; i < GameData.SkillCatalog.Length; i++)
            {
                if (GameData.SkillCatalog[i].Type == type) matches.Add(i);
            }
            return matches[rng.Next(matches.Count)];
        }
    }
}
