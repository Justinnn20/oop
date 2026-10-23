using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Gui
{
    /// <summary>
    /// In-match HUD: health bars, heat gauge, skill bar, combo text, and announcements.
    /// </summary>
    public partial class Form1 : Form
    {
        private void DrawModernHUD(Graphics g)
        {
            int w = this.ClientSize.Width;
            int barW = 380, barH = 26, topY = 32;

            int p1BarX = 50;
            using (SolidBrush glass = new SolidBrush(Color.FromArgb(160, 20, 20, 30)))
            {
                g.FillRectangle(glass, p1BarX - 6, topY - 6, barW + 12, barH + 46);
            }
            g.DrawRectangle(new Pen(Color.FromArgb(80, 255, 255, 255), 1), p1BarX - 6, topY - 6, barW + 12, barH + 46);

            int p1RedW = (int)(barW * ((float)p1RedHealth / MaxHealth));
            g.FillRectangle(Brushes.DarkRed, p1BarX + (barW - p1RedW), topY, p1RedW, barH);

            int p1CurW = (int)(barW * ((float)p1Health / MaxHealth));
            if (p1CurW > 0)
            {
                using (LinearGradientBrush gb = new LinearGradientBrush(new Rectangle(p1BarX, topY, barW, barH), Color.Gold, Color.OrangeRed, LinearGradientMode.Horizontal))
                    g.FillRectangle(gb, p1BarX + (barW - p1CurW), topY, p1CurW, barH);
            }
            g.DrawRectangle(new Pen(Color.White, 2), p1BarX, topY, barW, barH);
            g.DrawString(GameData.Roster[p1CharIndex].Name, new Font("Impact", 15, FontStyle.Italic), Brushes.White, p1BarX, topY + barH + 4);

            int heatH = 6;
            g.FillRectangle(Brushes.Black, p1BarX, topY + barH + 24, barW, heatH);
            int p1HeatW = (int)(barW * ((float)p1Heat / MaxHeat));
            using (SolidBrush hb = new SolidBrush(p1Heat >= 50 ? Color.Cyan : Color.DeepSkyBlue))
            {
                g.FillRectangle(hb, p1BarX + (barW - p1HeatW), topY + barH + 24, p1HeatW, heatH);
            }
            g.DrawString("HEAT GAUGE", new Font("Segoe UI", 7, FontStyle.Bold), Brushes.Cyan, p1BarX, topY + barH + 31);

            string t = roundTimer.ToString();
            using (Font tf = new Font("Impact", 36, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(t, tf);
                g.DrawString(t, tf, Brushes.Yellow, (w / 2f) - (sz.Width / 2f), topY - 10);
            }

            int p2BarX = w - 50 - barW;
            using (SolidBrush glass = new SolidBrush(Color.FromArgb(160, 20, 20, 30)))
            {
                g.FillRectangle(glass, p2BarX - 6, topY - 6, barW + 12, barH + 46);
            }
            g.DrawRectangle(new Pen(Color.FromArgb(80, 255, 255, 255), 1), p2BarX - 6, topY - 6, barW + 12, barH + 46);

            int p2RedW = (int)(barW * ((float)p2RedHealth / MaxHealth));
            g.FillRectangle(Brushes.DarkRed, p2BarX, topY, p2RedW, barH);

            int p2CurW = (int)(barW * ((float)p2Health / MaxHealth));
            if (p2CurW > 0)
            {
                using (LinearGradientBrush gb = new LinearGradientBrush(new Rectangle(p2BarX, topY, barW, barH), Color.OrangeRed, Color.Gold, LinearGradientMode.Horizontal))
                    g.FillRectangle(gb, p2BarX, topY, p2CurW, barH);
            }
            g.DrawRectangle(new Pen(Color.White, 2), p2BarX, topY, barW, barH);
            string p2DisplayName = isVsCPU ? $"CPU ({GameData.Roster[p2CharIndex].Name})" : GameData.Roster[p2CharIndex].Name;
            g.DrawString(p2DisplayName, new Font("Impact", 15, FontStyle.Italic), Brushes.White, p2BarX + barW - 170, topY + barH + 4);

            g.FillRectangle(Brushes.Black, p2BarX, topY + barH + 24, barW, heatH);
            int p2HeatW = (int)(barW * ((float)p2Heat / MaxHeat));
            using (SolidBrush hb = new SolidBrush(p2Heat >= 50 ? Color.OrangeRed : Color.DarkOrange))
            {
                g.FillRectangle(hb, p2BarX + (barW - p2HeatW), topY + barH + 24, p2HeatW, heatH);
            }
            g.DrawString("HEAT GAUGE", new Font("Segoe UI", 7, FontStyle.Bold), Brushes.OrangeRed, p2BarX + barW - 65, topY + barH + 31);

            DrawLoadoutHud(g);

            if (p1ComboHits > 1)
            {
                using (Font cf = new Font("Impact", 28, FontStyle.Italic))
                {
                    g.DrawString($"{p1ComboHits} HITS COMBO!", cf, Brushes.Red, 60, 130);
                    g.DrawString($"{p1ComboHits} HITS COMBO!", cf, Brushes.Gold, 58, 128);
                }
            }
            if (p2ComboHits > 1)
            {
                using (Font cf = new Font("Impact", 28, FontStyle.Italic))
                {
                    g.DrawString($"{p2ComboHits} HITS COMBO!", cf, Brushes.Red, w - 280, 130);
                    g.DrawString($"{p2ComboHits} HITS COMBO!", cf, Brushes.Gold, w - 282, 128);
                }
            }

            using (Font gf = new Font("Segoe UI", 9, FontStyle.Bold))
            {
                string helpControls = isVsCPU
                    ? "P1: [A/D] Move  [W] Jump  [F/G/T] Skill Slots 1-3    |    Opponent: Smart CPU AI"
                    : "P1: [A/D/W] Move/Jump  [F/G/T] Skills   |   P2: [J/L/I] Move/Jump  [K/O/P] Skills";
                g.DrawString(helpControls, gf, Brushes.Silver, 70, this.ClientSize.Height - 30);
            }
        }

        private void DrawLoadoutHud(Graphics g)
        {
            int barW = 380, topY = 32;
            int p1BarX = 50, p2BarX = this.ClientSize.Width - 50 - barW;
            int skillY = topY + 70;
            using Font hudFont = new Font("Segoe UI", 8, FontStyle.Bold);

            string p1Text = BuildSkillBarText(p1Loadout, new[] { "F", "G", "T" });
            string p2Text = BuildSkillBarText(p2Loadout, isVsCPU ? null : new[] { "K", "O", "P" });
            SizeF p2Size = g.MeasureString(p2Text, hudFont);
            g.DrawString(p1Text, hudFont, Brushes.Cyan, p1BarX, skillY);
            g.DrawString(p2Text, hudFont, Brushes.OrangeRed, p2BarX + barW - p2Size.Width, skillY);

            if (p1Loadout.ComboCharge > 0)
            {
                g.DrawString($"COMBO CHARGE x{p1Loadout.ComboCharge}", hudFont, Brushes.Gold, p1BarX, skillY + 14);
            }
            if (p2Loadout.ComboCharge > 0)
            {
                string chargeText = $"COMBO CHARGE x{p2Loadout.ComboCharge}";
                SizeF chargeSize = g.MeasureString(chargeText, hudFont);
                g.DrawString(chargeText, hudFont, Brushes.Gold, p2BarX + barW - chargeSize.Width, skillY + 14);
            }
        }

        private static string BuildSkillBarText(Loadout loadout, string[]? keys)
        {
            List<string> parts = new List<string>();
            for (int i = 0; i < loadout.SkillSlots.Length; i++)
            {
                string skillName = GameData.SkillCatalog[loadout.SkillSlots[i]].Name;
                parts.Add(keys == null ? skillName : $"[{keys[i]}] {skillName}");
            }
            return string.Join("   ", parts);
        }

        private void DrawAnnouncement(Graphics g, string text, Color color)
        {
            int w = this.ClientSize.Width;
            using (Font af = new Font("Impact", 60, FontStyle.Italic))
            {
                SizeF sz = g.MeasureString(text, af);
                g.DrawString(text, af, Brushes.DarkRed, (w / 2f) - (sz.Width / 2f) + 4, 184);
                g.DrawString(text, af, new SolidBrush(color), (w / 2f) - (sz.Width / 2f), 180);
            }
        }
    }
}
