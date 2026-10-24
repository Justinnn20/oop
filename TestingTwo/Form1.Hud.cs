using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Gui
{
    /// <summary>
    /// In-match HUD: health bars, heat gauge, skill bar, combo text, and announcements.
    ///
    /// LAYOUT (bawat player, mula taas pababa):
    ///   [ health bar ]
    ///   [ PANGALAN ............ HEAT ]   <- iisang row, magkabilang gilid
    ///   [ heat bar ]
    ///   (labas ng glass panel)
    ///   [F] skill hints
    ///   COMBO CHARGE
    ///   HITS COMBO!
    /// </summary>
    public partial class Form1 : Form
    {
        private const int HudMargin = 50;
        private const int HudTopY = 32;
        private const int HudBarH = 26;
        private const int HudCenterGap = 170;   // espasyo sa gitna para sa timer at round pips

        // Lapad ng health bar: kasya kahit maliit ang window
        private int HudBarW()
        {
            return Math.Min(380, (this.ClientSize.Width - (HudMargin * 2) - HudCenterGap) / 2);
        }

        // Text na SIGURADONG kasya. align: 0 = kaliwa, 1 = gitna, 2 = kanan (x ang anchor)
        private void DrawFitAligned(Graphics g, string text, string family, float size, FontStyle style,
                                    Color color, float x, float y, float maxWidth, int align)
        {
            StringFormat fmt = StringFormat.GenericTypographic;
            float s = size;
            Font f = new Font(family, s, style);
            while (s > 6f && g.MeasureString(text, f, int.MaxValue, fmt).Width > maxWidth)
            {
                f.Dispose();
                s -= 0.5f;
                f = new Font(family, s, style);
            }

            using (f)
            using (SolidBrush b = new SolidBrush(color))
            {
                float tw = g.MeasureString(text, f, int.MaxValue, fmt).Width;
                float dx = align == 0 ? x : (align == 1 ? x - (tw / 2f) : x - tw);
                g.DrawString(text, f, b, dx, y, fmt);
            }
        }

        private void DrawModernHUD(Graphics g)
        {
            int w = this.ClientSize.Width;
            int barW = HudBarW();
            int barH = HudBarH, topY = HudTopY;
            int heatH = 6;

            int nameY = topY + barH + 3;        // row ng pangalan + HEAT label
            int heatY = topY + barH + 29;       // heat bar (may espasyo sa ilalim ng pangalan)
            int panelH = barH + 44;             // sakop ng glass panel hanggang sa ilalim ng heat bar

            int p1BarX = HudMargin;
            int p2BarX = w - HudMargin - barW;

            // Max HP bawat fighter (Youth 100 / Veteran 85 / Elder 60 + skin)
            int p1Max = Math.Max(1, p1MaxHealth);
            int p2Max = Math.Max(1, p2MaxHealth);

            // ================= PLAYER 1 =================
            using (SolidBrush glass = new SolidBrush(Color.FromArgb(160, 20, 20, 30)))
                g.FillRectangle(glass, p1BarX - 6, topY - 6, barW + 12, panelH);
            using (Pen frame = new Pen(Color.FromArgb(80, 255, 255, 255), 1))
                g.DrawRectangle(frame, p1BarX - 6, topY - 6, barW + 12, panelH);

            int p1RedW = (int)(barW * Math.Clamp((float)p1RedHealth / p1Max, 0f, 1f));
            g.FillRectangle(Brushes.DarkRed, p1BarX + (barW - p1RedW), topY, p1RedW, barH);

            int p1CurW = (int)(barW * Math.Clamp((float)p1Health / p1Max, 0f, 1f));
            if (p1CurW > 0)
            {
                using (LinearGradientBrush gb = new LinearGradientBrush(new Rectangle(p1BarX, topY, barW, barH), Color.Gold, Color.OrangeRed, LinearGradientMode.Horizontal))
                    g.FillRectangle(gb, p1BarX + (barW - p1CurW), topY, p1CurW, barH);
            }
            using (Pen outline = new Pen(Color.White, 2))
                g.DrawRectangle(outline, p1BarX, topY, barW, barH);

            // Row: pangalan (kaliwa) ........ HEAT (kanan)
            DrawFitAligned(g, GameData.Roster[p1CharIndex].Name, "Impact", 15, FontStyle.Italic, Color.White,
                           p1BarX, nameY, barW - 80, 0);
            DrawFitAligned(g, "HEAT", "Segoe UI", 8, FontStyle.Bold, Color.Cyan,
                           p1BarX + barW, nameY + 5, 70, 2);

            g.FillRectangle(Brushes.Black, p1BarX, heatY, barW, heatH);
            int p1HeatW = (int)(barW * Math.Clamp((float)p1Heat / MaxHeat, 0f, 1f));
            using (SolidBrush hb = new SolidBrush(p1Heat >= 50 ? Color.Cyan : Color.DeepSkyBlue))
                g.FillRectangle(hb, p1BarX + (barW - p1HeatW), heatY, p1HeatW, heatH);

            // ================= TIMER (gitna) =================
            string t = roundTimer.ToString();
            using (Font tf = new Font("Impact", 36, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(t, tf);
                g.DrawString(t, tf, Brushes.Yellow, (w / 2f) - (sz.Width / 2f), topY - 10);
            }

            // ================= PLAYER 2 =================
            using (SolidBrush glass = new SolidBrush(Color.FromArgb(160, 20, 20, 30)))
                g.FillRectangle(glass, p2BarX - 6, topY - 6, barW + 12, panelH);
            using (Pen frame = new Pen(Color.FromArgb(80, 255, 255, 255), 1))
                g.DrawRectangle(frame, p2BarX - 6, topY - 6, barW + 12, panelH);

            int p2RedW = (int)(barW * Math.Clamp((float)p2RedHealth / p2Max, 0f, 1f));
            g.FillRectangle(Brushes.DarkRed, p2BarX, topY, p2RedW, barH);

            int p2CurW = (int)(barW * Math.Clamp((float)p2Health / p2Max, 0f, 1f));
            if (p2CurW > 0)
            {
                using (LinearGradientBrush gb = new LinearGradientBrush(new Rectangle(p2BarX, topY, barW, barH), Color.OrangeRed, Color.Gold, LinearGradientMode.Horizontal))
                    g.FillRectangle(gb, p2BarX, topY, p2CurW, barH);
            }
            using (Pen outline = new Pen(Color.White, 2))
                g.DrawRectangle(outline, p2BarX, topY, barW, barH);

            // Row (naka-mirror): HEAT (kaliwa) ........ pangalan (kanan)
            string p2DisplayName = isVsCPU ? $"CPU ({GameData.Roster[p2CharIndex].Name})" : GameData.Roster[p2CharIndex].Name;
            DrawFitAligned(g, p2DisplayName, "Impact", 15, FontStyle.Italic, Color.White,
                           p2BarX + barW, nameY, barW - 80, 2);
            DrawFitAligned(g, "HEAT", "Segoe UI", 8, FontStyle.Bold, Color.OrangeRed,
                           p2BarX, nameY + 5, 70, 0);

            g.FillRectangle(Brushes.Black, p2BarX, heatY, barW, heatH);
            int p2HeatW = (int)(barW * Math.Clamp((float)p2Heat / MaxHeat, 0f, 1f));
            using (SolidBrush hb = new SolidBrush(p2Heat >= 50 ? Color.OrangeRed : Color.DarkOrange))
                g.FillRectangle(hb, p2BarX + (barW - p2HeatW), heatY, p2HeatW, heatH);

            // ================= SKILL HINTS (labas ng panel) =================
            DrawLoadoutHud(g);

            // ================= COMBO TEXT (sa ilalim ng skill hints) =================
            int comboY = HudTopY + panelH + 38;
            if (p1ComboHits > 1)
            {
                string txt = $"{p1ComboHits} HITS COMBO!";
                DrawFitAligned(g, txt, "Impact", 28, FontStyle.Italic, Color.Red, p1BarX + 2, comboY + 2, barW, 0);
                DrawFitAligned(g, txt, "Impact", 28, FontStyle.Italic, Color.Gold, p1BarX, comboY, barW, 0);
            }
            if (p2ComboHits > 1)
            {
                string txt = $"{p2ComboHits} HITS COMBO!";
                DrawFitAligned(g, txt, "Impact", 28, FontStyle.Italic, Color.Red, p2BarX + barW + 2, comboY + 2, barW, 2);
                DrawFitAligned(g, txt, "Impact", 28, FontStyle.Italic, Color.Gold, p2BarX + barW, comboY, barW, 2);
            }

            // ================= CONTROLS HELP (ibaba, may dark strip para mabasa) =================
            string helpControls = isVsCPU
                ? "P1: [A/D] Move  [W] Jump  [F/G/T] Skill Slots 1-3    |    Opponent: Smart CPU AI"
                : "P1: [A/D/W] Move/Jump  [F/G/T] Skills   |   P2: [J/L/I] Move/Jump  [K/O/P] Skills";
            int stripH = 22;
            int stripY = this.ClientSize.Height - stripH - 8;
            using (SolidBrush strip = new SolidBrush(Color.FromArgb(130, 0, 0, 0)))
                g.FillRectangle(strip, 0, stripY, w, stripH);
            DrawFitAligned(g, helpControls, "Segoe UI", 9, FontStyle.Bold, Color.Silver, w / 2f, stripY + 3, w - 60, 1);
        }

        private void DrawLoadoutHud(Graphics g)
        {
            int barW = HudBarW();
            int p1BarX = HudMargin, p2BarX = this.ClientSize.Width - HudMargin - barW;
            int panelBottom = HudTopY - 6 + (HudBarH + 44);
            int skillY = panelBottom + 6;

            string p1Text = BuildSkillBarText(p1Loadout, new[] { "F", "G", "T" });
            string p2Text = BuildSkillBarText(p2Loadout, isVsCPU ? null : new[] { "K", "O", "P" });

            DrawFitAligned(g, p1Text, "Segoe UI", 8, FontStyle.Bold, Color.Cyan, p1BarX, skillY, barW, 0);
            DrawFitAligned(g, p2Text, "Segoe UI", 8, FontStyle.Bold, Color.OrangeRed, p2BarX + barW, skillY, barW, 2);

            if (p1Loadout.ComboCharge > 0)
            {
                DrawFitAligned(g, $"COMBO CHARGE x{p1Loadout.ComboCharge}", "Segoe UI", 8, FontStyle.Bold, Color.Gold,
                               p1BarX, skillY + 16, barW, 0);
            }
            if (p2Loadout.ComboCharge > 0)
            {
                DrawFitAligned(g, $"COMBO CHARGE x{p2Loadout.ComboCharge}", "Segoe UI", 8, FontStyle.Bold, Color.Gold,
                               p2BarX + barW, skillY + 16, barW, 2);
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
            using (SolidBrush shadow = new SolidBrush(Color.DarkRed))
            using (SolidBrush main = new SolidBrush(color))
            {
                SizeF sz = g.MeasureString(text, af);
                g.DrawString(text, af, shadow, (w / 2f) - (sz.Width / 2f) + 4, 184);
                g.DrawString(text, af, main, (w / 2f) - (sz.Width / 2f), 180);
            }
        }
    }
}