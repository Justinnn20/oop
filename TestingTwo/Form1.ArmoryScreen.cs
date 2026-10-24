using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Gui
{
    /// <summary>
    /// Armory screen drawing, including the live fighter preview.
    /// </summary>
    public partial class Form1 : Form
    {
        private void GetArmoryEntry(int index, Loadout loadout, out string name, out string detail,
            out int price, out bool owned, out string? equippedLabel)
        {
            if (armoryTab == 0)
            {
                SkillInfo skill = GameData.SkillCatalog[index];
                int slot = Array.IndexOf(loadout.SkillSlots, index);
                name = skill.Name;
                detail = $"{skill.Type}  |  DMG {skill.Damage}  |  Reach {skill.Reach}" + (skill.HeatCost > 0 ? $"  |  Heat {skill.HeatCost}" : "");
                price = skill.Price;
                owned = loadout.OwnedSkills.Contains(index);
                equippedLabel = slot >= 0 ? $"SLOT {slot + 1}" : null;
            }
            else if (armoryTab == 1)
            {
                ArmorInfo armor = GameData.ArmorCatalog[index];
                name = armor.Name;
                detail = $"Flat damage reduction: -{armor.DamageReduction} per hit";
                price = armor.Price;
                owned = loadout.OwnedArmor.Contains(index);
                equippedLabel = loadout.ArmorIndex == index ? "EQUIPPED" : null;
            }
            else
            {
                SkinInfo skin = GameData.SkinCatalog[index];
                name = skin.Name;
                detail = skin.Description;
                price = skin.Price;
                owned = loadout.OwnedSkins.Contains(index);
                equippedLabel = loadout.SkinIndex == index ? "EQUIPPED" : null;
            }
        }

        private string GetArmoryDescription(int index)
        {
            if (armoryTab == 0)
            {
                SkillInfo skill = GameData.SkillCatalog[index];
                string heat = skill.HeatCost > 0 ? $"\nHeat cost: {skill.HeatCost}" : "";
                return $"{skill.Name}  ({skill.Type})\n{skill.Description}\nDamage: {skill.Damage}   Reach: {skill.Reach}{heat}";
            }
            if (armoryTab == 1)
            {
                ArmorInfo armor = GameData.ArmorCatalog[index];
                return $"{armor.Name}\n{armor.Description}\nBlocks {armor.DamageReduction} damage from every hit (you always take at least 1).";
            }

            SkinInfo skin = GameData.SkinCatalog[index];
            return $"{skin.Name}\n{skin.Description}";
        }

        private void DrawArmory(Graphics g)
        {
            int w = this.ClientSize.Width, h = this.ClientSize.Height;
            Loadout loadout = ActiveLoadout;
            Color accent = armoryPlayer == 1 ? Color.Cyan : Color.Crimson;
            g.Clear(Color.FromArgb(10, 10, 15));

            using Font titleFont = new Font("Impact", 30, FontStyle.Italic);
            using Font headFont = new Font("Impact", 14);
            using Font itemFont = new Font("Segoe UI", 11, FontStyle.Bold);
            using Font bodyFont = new Font("Segoe UI", 9);
            using SolidBrush accentBrush = new SolidBrush(accent);

            // Header
            g.DrawString($"ARMORY - PLAYER {armoryPlayer}", titleFont, accentBrush, 40, 16);
            string goldText = $"GOLD: {loadout.Gold}";
            SizeF goldSize = g.MeasureString(goldText, headFont);
            g.DrawString(goldText, headFont, Brushes.Gold, w - goldSize.Width - 40, 28);

            // Tabs
            string[] tabNames = { "SKILLS", "ARMOR", "BATTLE SKINS" };
            using (StringFormat center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                for (int t = 0; t < tabNames.Length; t++)
                {
                    Rectangle tabRect = new Rectangle(40 + (t * 170), 74, 160, 30);
                    bool active = t == armoryTab;
                    using (SolidBrush tabBrush = new SolidBrush(active ? accent : Color.FromArgb(28, 28, 40)))
                    {
                        g.FillRectangle(tabBrush, tabRect);
                    }
                    g.DrawString(tabNames[t], headFont, active ? Brushes.Black : Brushes.Gray, tabRect, center);
                }
            }

            // Item list
            int listX = 40, listY = 120, rowH = 46, listW = 520;
            int count = ArmoryItemCount();
            for (int i = 0; i < count; i++)
            {
                GetArmoryEntry(i, loadout, out string name, out string detail, out int price, out bool owned, out string? equippedLabel);

                Rectangle row = new Rectangle(listX, listY + (i * rowH), listW, rowH - 6);
                bool isCursor = i == armoryCursor;

                using (SolidBrush rowBrush = new SolidBrush(isCursor ? Color.FromArgb(45, 40, 70) : Color.FromArgb(20, 20, 30)))
                {
                    g.FillRectangle(rowBrush, row);
                }
                using (Pen rowPen = new Pen(isCursor ? accent : Color.FromArgb(45, 45, 60), isCursor ? 3 : 1))
                {
                    g.DrawRectangle(rowPen, row);
                }

                g.DrawString(name, itemFont, owned ? Brushes.White : Brushes.Gray, row.X + 10, row.Y + 3);
                g.DrawString(detail, bodyFont, Brushes.Silver, row.X + 10, row.Y + 22);

                string tagText;
                Brush tagBrush;
                if (equippedLabel != null) { tagText = equippedLabel; tagBrush = Brushes.LimeGreen; }
                else if (owned) { tagText = "OWNED"; tagBrush = Brushes.Silver; }
                else { tagText = $"{price} G"; tagBrush = price <= loadout.Gold ? Brushes.Gold : Brushes.OrangeRed; }

                SizeF tagSize = g.MeasureString(tagText, itemFont);
                g.DrawString(tagText, itemFont, tagBrush, row.Right - tagSize.Width - 10, row.Y + 10);
            }

            // Side panel: live fighter preview + selected item details
            int panelX = 590, panelY = 120, panelW = w - panelX - 40, previewH = 215;
            Rectangle previewRect = new Rectangle(panelX, panelY, panelW, previewH);
            using (SolidBrush panelBrush = new SolidBrush(Color.FromArgb(18, 18, 28)))
            {
                g.FillRectangle(panelBrush, previewRect);
            }
            using (Pen panelPen = new Pen(accent, 2))
            {
                g.DrawRectangle(panelPen, previewRect);
            }

            // Loadout summary (left half of the preview box)
            g.DrawString("LOADOUT", headFont, accentBrush, panelX + 14, panelY + 8);
            for (int s = 0; s < loadout.SkillSlots.Length; s++)
            {
                g.DrawString($"[{s + 1}] {GameData.SkillCatalog[loadout.SkillSlots[s]].Name}", bodyFont, Brushes.White, panelX + 14, panelY + 42 + (s * 18));
            }
            g.DrawString($"Armor: {GameData.ArmorCatalog[loadout.ArmorIndex].Name}", bodyFont, Brushes.White, panelX + 14, panelY + 106);
            g.DrawString($"Skin: {GameData.SkinCatalog[loadout.SkinIndex].Name}", bodyFont, Brushes.White, panelX + 14, panelY + 124);

            // Live fighter preview (right half): gear changes the moment it is equipped or unequipped
            FighterInfo previewFighter = GameData.Roster[armoryPlayer == 1 ? p1CharIndex : p2CharIndex];
            float previewScale = 0.8f;
            float previewCenterX = panelX + 295;
            Rectangle previewClip = new Rectangle(panelX + 2, panelY + 2, panelW - 4, previewH - 4);

            GraphicsState previewState = g.Save();
            g.SetClip(previewClip);
            g.TranslateTransform(previewCenterX - (54 * previewScale), panelY + 40 - (previewScale * GroundLevel));
            g.ScaleTransform(previewScale, previewScale);
            // y is 1px above the ground so the idle up/down bob is disabled in the preview
            DrawArticulatedFighter(g, 0, GroundLevel - 1f, 0, armoryPlayer == 1, previewFighter, loadout);
            g.Restore(previewState);

            // Equip / unequip flash ring
            if (armoryFlash > 0)
            {
                float radius = 30 + ((14 - armoryFlash) / 14f * 90);
                int ringAlpha = Math.Max(0, armoryFlash * 17);
                GraphicsState ringState = g.Save();
                g.SetClip(previewClip);
                using (Pen ringPen = new Pen(Color.FromArgb(ringAlpha, armoryFlashColor), 4))
                {
                    g.DrawEllipse(ringPen, previewCenterX - radius, panelY + 110 - radius, radius * 2, radius * 2);
                }
                g.Restore(ringState);
            }

            // Selected item details
            int detailY = panelY + previewH + 10;
            g.DrawString("SELECTED ITEM", headFont, accentBrush, panelX + 14, detailY);
            RectangleF descRect = new RectangleF(panelX + 14, detailY + 28, panelW - 28, 480 - (detailY + 28));
            g.DrawString(GetArmoryDescription(armoryCursor), bodyFont, Brushes.LightGray, descRect);

            // Footer
            g.DrawString("TIP: Land slot 1 > 2 > 3 in order to complete a combo and gain Combo Charge. Charge powers up your next Finisher.",
                bodyFont, Brushes.Gold, listX, h - 120);
            g.DrawString(armoryMessage, itemFont, Brushes.Gold, listX, h - 95);

            string prompt = armoryPlayer == 1 && !isVsCPU
                ? "[ LEFT/RIGHT ] Tab   [ UP/DOWN ] Select   [ ENTER ] Equip / Unequip   [ R ] Refund   [ 1 2 3 ] Slot   [ SPACE ] Ready   [ BACKSPACE ] Back"
                : "[ LEFT/RIGHT ] Tab   [ UP/DOWN ] Select   [ ENTER ] Equip / Unequip   [ R ] Refund   [ 1 2 3 ] Slot   [ SPACE ] Start   [ BACKSPACE ] Back";
            g.DrawString(prompt, bodyFont, Brushes.Silver, listX, h - 55);
        }

        // ===== Armory logic (buying, refunding, equipping, CPU loadout) =====

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