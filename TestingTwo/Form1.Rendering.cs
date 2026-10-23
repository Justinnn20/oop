using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Gui
{
    /// <summary>
    /// Main paint routine, arena scenery, and fighter drawing (including armor and skin visuals).
    /// </summary>
    public partial class Form1 : Form
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (currentState == GameState.ModeSelect)
            {
                DrawModeSelection(g);
            }
            else if (currentState == GameState.CharSelect)
            {
                DrawCharacterSelection(g);
            }
            else if (currentState == GameState.ArenaSelect)
            {
                DrawArenaSelection(g);
            }
            else if (currentState == GameState.Armory)
            {
                DrawArmory(g);
            }
            else
            {
                int w = this.ClientSize.Width, h = this.ClientSize.Height;

                if (rageCinematicFlash > 0)
                {
                    using (SolidBrush darkFlash = new SolidBrush(Color.FromArgb(195, 5, 5, 10)))
                    {
                        g.FillRectangle(darkFlash, 0, 0, w, h);
                    }
                }

                float midX = (p1X + p2X) / 2f;
                float midY = (p1Y + p2Y) / 2f + 50f;

                // Maps with a background picture show it exactly as it is, fixed to the screen
                Bitmap? stageBg = ArenaImages.Get(GameData.Arenas[selectedArenaIndex]);
                if (stageBg != null) g.DrawImage(stageBg, 0, 0, w, h);

                g.TranslateTransform(w / 2f, h / 2f);
                g.ScaleTransform(cameraZoom, cameraZoom);
                g.TranslateTransform(-midX, -midY);

                if (screenShake > 0)
                {
                    g.TranslateTransform(rng.Next(-screenShake, screenShake + 1), rng.Next(-screenShake, screenShake + 1));
                }

                // Draw the full arena scenery for the selected stage (Volcano, Dojo, Cyber, Forest, or Elevator Lobby)
                DrawDynamicArenaScenery(g, midX);

                foreach (var c in craters)
                {
                    int alpha = Math.Min(255, c.Life * 3);
                    using (Pen cp = new Pen(Color.FromArgb(alpha, 10, 10, 10), 3))
                    {
                        g.DrawLine(cp, c.X, c.Y, c.X - 18, c.Y - 6);
                        g.DrawLine(cp, c.X, c.Y, c.X + 22, c.Y - 5);
                        g.DrawLine(cp, c.X, c.Y, c.X - 8, c.Y + 8);
                        g.DrawLine(cp, c.X, c.Y, c.X + 15, c.Y + 7);
                    }
                }

                var ar = GameData.Arenas[selectedArenaIndex];
                foreach (var m in ambientMotes)
                {
                    if (ar.BackgroundFile != null) break;   // maps with a picture have no floating particles
                    using (SolidBrush amb = new SolidBrush(Color.FromArgb(m.Alpha, ar.ParticleColor)))
                    {
                        g.FillEllipse(amb, m.X, m.Y, m.Size, m.Size);
                    }
                }

                foreach (var t in trails)
                {
                    using (SolidBrush tb = new SolidBrush(Color.FromArgb(Math.Max(0, t.Alpha), t.Color)))
                    {
                        g.FillEllipse(tb, t.X + 35, t.Y, 38, 44);
                        g.FillRectangle(tb, t.X + 30, t.Y + 42, 48, 58);
                    }
                }

                DrawFighterReflection(g, p1X, p1Y, p1Action, true, GameData.Roster[p1CharIndex]);
                DrawFighterReflection(g, p2X, p2Y, p2Action, false, GameData.Roster[p2CharIndex]);

                DrawArticulatedFighter(g, p1X, p1Y, p1Action, true, GameData.Roster[p1CharIndex], p1Loadout);
                DrawArticulatedFighter(g, p2X, p2Y, p2Action, false, GameData.Roster[p2CharIndex], p2Loadout);

                foreach (var s in shockwaves)
                {
                    using (Pen sp = new Pen(Color.FromArgb(s.Alpha, s.Color), 4))
                    {
                        g.DrawEllipse(sp, s.Center.X - s.Radius, s.Center.Y - (s.Radius * 0.5f), s.Radius * 2, s.Radius);
                    }
                }

                foreach (var l in lightnings)
                {
                    using (Pen lp = new Pen(l.Color, 3))
                    {
                        for (int i = 0; i < l.Points.Count - 1; i++) g.DrawLine(lp, l.Points[i], l.Points[i + 1]);
                    }
                }

                foreach (var p in particles)
                {
                    int alpha = (p.Life * 255) / p.MaxLife;
                    using (SolidBrush pb = new SolidBrush(Color.FromArgb(Math.Max(0, alpha), p.Color)))
                    {
                        g.FillEllipse(pb, p.X, p.Y, p.Size, p.Size);
                    }
                }

                if (speedLineTimer > 0)
                {
                    using (Pen slp = new Pen(Color.FromArgb(160, 255, 255, 255), 2))
                    {
                        for (int a = 0; a < 360; a += 15)
                        {
                            double rad = a * Math.PI / 180.0;
                            float x1 = speedLineCenter.X + (float)(Math.Cos(rad) * 90);
                            float y1 = speedLineCenter.Y + (float)(Math.Sin(rad) * 90);
                            float x2 = speedLineCenter.X + (float)(Math.Cos(rad) * 450);
                            float y2 = speedLineCenter.Y + (float)(Math.Sin(rad) * 450);
                            g.DrawLine(slp, x1, y1, x2, y2);
                        }
                    }
                }

                g.ResetTransform();
                DrawModernHUD(g);

                if (currentState == GameState.FightIntro)
                {
                    DrawAnnouncement(g, introCounter > 25 ? "ROUND 1" : "FIGHT!", Color.Yellow);
                }
                else if (currentState == GameState.KO)
                {
                    DrawAnnouncement(g, "K.O.", Color.Red);
                    using Font wf = new Font("Impact", 24, FontStyle.Italic);
                    SizeF sz = g.MeasureString(matchResultText, wf);
                    g.DrawString(matchResultText, wf, Brushes.Gold, (w / 2f) - (sz.Width / 2f), 280);

                    using Font infoFont = new Font("Segoe UI", 12, FontStyle.Bold);
                    SizeF rewardSize = g.MeasureString(rewardText, infoFont);
                    g.DrawString(rewardText, infoFont, Brushes.Gold, (w / 2f) - (rewardSize.Width / 2f), 330);

                    string restartText = "[ R ] Return to Mode Menu";
                    SizeF restartSize = g.MeasureString(restartText, infoFont);
                    g.DrawString(restartText, infoFont, Brushes.White, (w / 2f) - (restartSize.Width / 2f), 365);
                }
            }
        }

        // Maps with a background picture: in battle the picture is drawn fixed to the screen (see OnPaint).
        // The stage-select preview draws it here instead, filling the same 1024x620 view.
        private bool TryDrawImageScenery(Graphics g, ArenaInfo ar, bool isPreview)
        {
            Bitmap? bg = ArenaImages.Get(ar);
            if (bg == null) return false;

            if (isPreview) g.DrawImage(bg, 0, -20, 1024, 620);

            return true;
        }

        private void DrawDynamicArenaScenery(Graphics g, float midX, int arenaIndex = -1)
        {
            int arenaIdx = arenaIndex >= 0 ? arenaIndex : selectedArenaIndex;
            var ar = GameData.Arenas[arenaIdx];

            // Maps with a background picture (like the Elevator Lobby) are drawn from that picture.
            if (ar.BackgroundFile != null && TryDrawImageScenery(g, ar, arenaIndex >= 0)) return;

            int w = 1800;

            // 1. SKY
            using (LinearGradientBrush sky = new LinearGradientBrush(new Rectangle(-400, -150, w, 520), ar.SkyTop, ar.SkyBottom, LinearGradientMode.Vertical))
            {
                g.FillRectangle(sky, -400, -150, w, 520);
            }

            float px = (midX - 512) * 0.12f;

            // 2. STAGE-SPECIFIC BACKGROUND ELEMENTS
            if (arenaIdx == 0) // === VOLCANO INFERNO ===
            {
                // Towering volcano with smoke
                PointF[] volcanoPeak = {
                    new PointF(-300, 360),
                    new PointF(-50 - px, 160),
                    new PointF(20 - px, 190), // Crater top
                    new PointF(120 - px, 150),
                    new PointF(400 - px, 360),
                    new PointF(650 - px, 130), // Main Caldera
                    new PointF(720 - px, 155),
                    new PointF(850 - px, 140),
                    new PointF(1200, 360)
                };
                using (SolidBrush vRock = new SolidBrush(Color.FromArgb(40, 10, 10))) g.FillPolygon(vRock, volcanoPeak);

                // Glowing lava in the crater
                using (SolidBrush lavaGlow = new SolidBrush(Color.FromArgb(240, 255, 60, 0)))
                {
                    g.FillEllipse(lavaGlow, 650 - px, 135, 75, 25);
                    g.FillEllipse(lavaGlow, -40 - px, 165, 60, 20);
                }

                // Lava river flowing along the back floor
                using (Pen lavaRiver = new Pen(Color.FromArgb(220, 255, 80, 0), 8))
                {
                    g.DrawCurve(lavaRiver, new PointF[] {
                        new PointF(-200, 360),
                        new PointF(200, 355),
                        new PointF(600, 365),
                        new PointF(1200, 358)
                    });
                }
            }
            else if (arenaIdx == 1) // === MISHIMA DOJO ===
            {
                // Japanese Pagoda Roof Silhouette
                float dox = 360 - px;
                using (SolidBrush dojo = new SolidBrush(Color.FromArgb(30, 20, 15)))
                {
                    // Level 1 roof
                    g.FillPolygon(dojo, new PointF[] { new PointF(dox - 160, 270), new PointF(dox + 160, 270), new PointF(dox + 110, 220), new PointF(dox - 110, 220) });
                    // Level 2 roof
                    g.FillPolygon(dojo, new PointF[] { new PointF(dox - 120, 220), new PointF(dox + 120, 220), new PointF(dox + 70, 170), new PointF(dox - 70, 170) });
                    // Pagoda spire
                    g.FillRectangle(dojo, dox - 8, 110, 16, 60);
                }
                // Red torii gates on both sides
                using (Pen torii = new Pen(Color.Crimson, 7))
                {
                    g.DrawLine(torii, -50 - px, 240, 70 - px, 240);
                    g.DrawLine(torii, -30 - px, 240, -30 - px, 360);
                    g.DrawLine(torii, 50 - px, 240, 50 - px, 360);
                }
            }
            else if (arenaIdx == 2) // === CYBER HIGHWAY ===
            {
                // Futuristic Skyscraper Skyline
                for (int bx = -200; bx <= 1200; bx += 90)
                {
                    int bH = 140 + ((bx * 7) % 120);
                    using (SolidBrush bldg = new SolidBrush(Color.FromArgb(15, 18, 30)))
                    {
                        g.FillRectangle(bldg, bx - px, 360 - bH, 75, bH);
                    }
                    // Glowing Windows
                    using (SolidBrush winB = new SolidBrush(Color.FromArgb(90, Color.Cyan)))
                    {
                        for (int wy = 360 - bH + 15; wy < 340; wy += 22)
                        {
                            g.FillRectangle(winB, bx - px + 10, wy, 15, 8);
                            g.FillRectangle(winB, bx - px + 45, wy, 15, 8);
                        }
                    }
                }
            }
            else if (arenaIdx == 3) // === YAKUSHIMA FOREST ===
            {
                // Giant Full Moon
                using (SolidBrush moon = new SolidBrush(Color.FromArgb(200, 230, 245, 220)))
                {
                    g.FillEllipse(moon, 450 - px, 70, 110, 110);
                }
                // Pine Trees Layer
                for (int tx = -200; tx <= 1200; tx += 120)
                {
                    PointF[] tree = {
                        new PointF(tx - px, 360),
                        new PointF(tx + 40 - px, 180),
                        new PointF(tx + 80 - px, 360)
                    };
                    using (SolidBrush pine = new SolidBrush(Color.FromArgb(12, 28, 20))) g.FillPolygon(pine, tree);
                }
            }

            // 3. 3D OCTAGON RING FLOOR
            Point[] octagon = {
                new Point(20, 360),
                new Point(1004, 360),
                new Point(1200, 720),
                new Point(-180, 720)
            };
            using (SolidBrush floorB = new SolidBrush(ar.FloorColor)) g.FillPolygon(floorB, octagon);

            // Ring Boundary Line
            using (Pen ringGlow = new Pen(Color.FromArgb(90, ar.RingNeon), 10)) g.DrawLine(ringGlow, 20, 360, 1004, 360);
            using (Pen ringSolid = new Pen(ar.RingNeon, 4)) g.DrawLine(ringSolid, 20, 360, 1004, 360);

            // Floor Details / Cracks / Tatami Lines
            Color gridLineColor = (arenaIdx == 0) ? Color.FromArgb(90, 255, 80, 0) : Color.FromArgb(45, 255, 255, 255);
            using (Pen gridPen = new Pen(gridLineColor, 2))
            {
                for (int x = -100; x <= 1120; x += 110)
                {
                    g.DrawLine(gridPen, 512 + ((x - 512) * 0.35f), 360, x, 720);
                }
                g.DrawLine(gridPen, -10, 440, 1034, 440);
                g.DrawLine(gridPen, -90, 530, 1114, 530);
                g.DrawLine(gridPen, -160, 630, 1184, 630);
            }

            // Splat Pillars
            using (Pen wallSolid = new Pen(ar.RingNeon, 5))
            {
                g.DrawLine(wallSolid, 80, 140, 80, 500);
                g.DrawLine(wallSolid, this.ClientSize.Width - 80, 140, this.ClientSize.Width - 80, 500);
            }
        }

        private void DrawArticulatedFighter(Graphics g, float x, float y, int action, bool faceRight, FighterInfo fighter, Loadout? loadout = null)
        {
            float bob = (y >= GroundLevel && action == 0) ? (float)Math.Sin(bounceCycle + (faceRight ? 0 : 1.5f)) * 5 : 0;
            float cy = y + bob;

            float shadowScale = Math.Max(0.4f, 1f - ((GroundLevel - y) / 250f));
            using (SolidBrush sh = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
            {
                g.FillEllipse(sh, x + 15, GroundLevel + 160, 90 * shadowScale, 20 * shadowScale);
            }

            Color skin = (action == 4 || action == 5) ? Color.FromArgb(255, 120, 120) : Color.FromArgb(235, 195, 165);

            if (action == 3)
            {
                using (GraphicsPath auraPath = new GraphicsPath())
                {
                    auraPath.AddEllipse(x - 25, cy - 25, 170, 230);
                    using (PathGradientBrush pgb = new PathGradientBrush(auraPath))
                    {
                        pgb.CenterColor = Color.FromArgb(210, fighter.AuraColor);
                        pgb.SurroundColors = new Color[] { Color.Transparent };
                        g.FillPath(pgb, auraPath);
                    }
                }
            }

            if (loadout != null) DrawSkinBackdrop(g, x, cy, faceRight, loadout.SkinIndex);

            g.FillEllipse(new SolidBrush(skin), x + 35, cy, 38, 44);
            Point[] hair = faceRight
                ? new Point[] { new Point((int)x + 35, (int)cy + 15), new Point((int)x + 22, (int)cy - 12), new Point((int)x + 60, (int)cy - 8), new Point((int)x + 75, (int)cy + 15) }
                : new Point[] { new Point((int)x + 35, (int)cy + 15), new Point((int)x + 55, (int)cy - 12), new Point((int)x + 88, (int)cy - 8), new Point((int)x + 75, (int)cy + 15) };
            g.FillPolygon(new SolidBrush(fighter.PrimaryColor == Color.FromArgb(255, 185, 0) ? Color.Gold : Color.Black), hair);

            g.FillRectangle(new SolidBrush(skin), x + 30, cy + 42, 48, 58);
            using (Pen rib = new Pen(Color.FromArgb(190, 150, 120), 2)) g.DrawLine(rib, x + 54, cy + 48, x + 54, cy + 96);

            g.FillRectangle(new SolidBrush(Color.FromArgb(15, 15, 20)), x + 25, cy + 100, 58, 20);
            using (Pen belt = new Pen(fighter.PrimaryColor, 3)) g.DrawLine(belt, x + 25, cy + 103, x + 83, cy + 103);

            if (action == 2)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(15, 15, 20)), x + 40, cy + 115, 22, 65);
                if (faceRight)
                {
                    g.FillRectangle(new SolidBrush(fighter.PrimaryColor), x + 58, cy + 48, 50, 22);
                    g.FillRectangle(new SolidBrush(fighter.PrimaryColor), x + 105, cy + 34, 55, 20);
                    g.FillRectangle(Brushes.Gold, x + 155, cy + 32, 18, 24);
                }
                else
                {
                    g.FillRectangle(new SolidBrush(fighter.PrimaryColor), x - 2, cy + 48, 50, 22);
                    g.FillRectangle(new SolidBrush(fighter.PrimaryColor), x - 52, cy + 34, 55, 20);
                    g.FillRectangle(Brushes.Gold, x - 65, cy + 32, 18, 24);
                }
            }
            else
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(15, 15, 20)), x + 26, cy + 115, 24, 65);
                g.FillRectangle(new SolidBrush(Color.FromArgb(15, 15, 20)), x + 58, cy + 115, 24, 65);
                g.FillRectangle(new SolidBrush(fighter.PrimaryColor), x + 26, cy + 172, 24, 12);
                g.FillRectangle(new SolidBrush(fighter.PrimaryColor), x + 58, cy + 172, 24, 12);
            }

            if (loadout != null) DrawFighterGear(g, x, cy, action, faceRight, loadout, fighter);

            if (action == 1)
            {
                if (faceRight)
                {
                    g.FillRectangle(new SolidBrush(skin), x + 55, cy + 46, 35, 18);
                    g.FillRectangle(new SolidBrush(skin), x + 85, cy + 46, 40, 16);
                    g.FillRectangle(new SolidBrush(fighter.AuraColor), x + 120, cy + 43, 24, 22);
                }
                else
                {
                    g.FillRectangle(new SolidBrush(skin), x + 18, cy + 46, 35, 18);
                    g.FillRectangle(new SolidBrush(skin), x - 18, cy + 46, 40, 16);
                    g.FillRectangle(new SolidBrush(fighter.AuraColor), x - 36, cy + 43, 24, 22);
                }
            }
            else if (action == 3)
            {
                if (faceRight)
                {
                    g.FillRectangle(new SolidBrush(fighter.AuraColor), x + 80, cy + 38, 130, 40);
                    g.FillEllipse(Brushes.White, x + 185, cy + 30, 56, 56);
                }
                else
                {
                    g.FillRectangle(new SolidBrush(fighter.AuraColor), x - 105, cy + 38, 130, 40);
                    g.FillEllipse(Brushes.White, x - 125, cy + 30, 56, 56);
                }
            }
            else if (action != 2)
            {
                g.FillRectangle(new SolidBrush(fighter.AuraColor), x + (faceRight ? 70 : 15), cy + 55, 20, 30);
                g.FillRectangle(new SolidBrush(fighter.AuraColor), x + (faceRight ? 20 : 75), cy + 55, 20, 30);
            }

            using (Font nf = new Font("Impact", 11, FontStyle.Italic))
            {
                string tag = (!faceRight && isVsCPU) ? "CPU: " + fighter.Name : fighter.Name;
                g.DrawString(tag, nf, Brushes.White, x + 5, cy - 25);
            }
        }

        // Drawn behind the fighter: Berserker rage glow, Duelist cape.
        private void DrawSkinBackdrop(Graphics g, float x, float cy, bool faceRight, int skinIndex)
        {
            if (skinIndex == 2)
            {
                float pulse = (float)Math.Sin(bounceCycle * 2f) * 4f;
                using (SolidBrush outer = new SolidBrush(Color.FromArgb(45, 255, 40, 20)))
                using (SolidBrush inner = new SolidBrush(Color.FromArgb(55, 255, 90, 30)))
                {
                    g.FillEllipse(outer, x - 10 - pulse, cy - 18 - pulse, 130 + (pulse * 2), 212 + (pulse * 2));
                    g.FillEllipse(inner, x + 8, cy - 4, 94, 180);
                }
            }
            else if (skinIndex == 3)
            {
                float wave = (float)Math.Sin(bounceCycle * 2f) * 6f;
                PointF[] cape = faceRight
                    ? new PointF[] { new PointF(x + 32, cy + 46), new PointF(x + 32, cy + 104), new PointF(x - 24, cy + 132 + wave), new PointF(x - 10, cy + 74) }
                    : new PointF[] { new PointF(x + 76, cy + 46), new PointF(x + 76, cy + 104), new PointF(x + 132, cy + 132 + wave), new PointF(x + 118, cy + 74) };

                using (SolidBrush capeBrush = new SolidBrush(Color.FromArgb(210, 110, 30, 150)))
                using (Pen capeEdge = new Pen(Color.Gold, 2))
                {
                    g.FillPolygon(capeBrush, cape);
                    g.DrawPolygon(capeEdge, cape);
                }
            }
        }

        // Drawn on the fighter: armor pieces first, then the skin details on top.
        private void DrawFighterGear(Graphics g, float x, float cy, int action, bool faceRight, Loadout loadout, FighterInfo fighter)
        {
            float[] legXs = action == 2 ? new[] { x + 40f } : new[] { x + 26f, x + 58f };
            float legW = action == 2 ? 22f : 24f;

            switch (loadout.ArmorIndex)
            {
                case 1: // Leather Wraps
                    using (SolidBrush leather = new SolidBrush(Color.FromArgb(139, 90, 43)))
                    {
                        g.FillRectangle(leather, x + 30, cy + 58, 48, 7);
                        g.FillRectangle(leather, x + 30, cy + 76, 48, 7);
                        foreach (float lx in legXs)
                        {
                            g.FillRectangle(leather, lx, cy + 138, legW, 8);
                            g.FillRectangle(leather, lx, cy + 154, legW, 8);
                        }
                    }
                    break;

                case 2: // Steel Plating
                    using (SolidBrush steel = new SolidBrush(Color.FromArgb(150, 160, 175)))
                    using (SolidBrush steelLight = new SolidBrush(Color.FromArgb(205, 215, 228)))
                    using (Pen steelEdge = new Pen(Color.FromArgb(70, 75, 90), 2))
                    {
                        g.FillRectangle(steel, x + 32, cy + 46, 44, 38);
                        g.DrawRectangle(steelEdge, x + 32, cy + 46, 44, 38);
                        g.FillRectangle(steelLight, x + 36, cy + 50, 12, 28);
                        g.FillEllipse(steel, x + 16, cy + 40, 24, 18);
                        g.FillEllipse(steel, x + 68, cy + 40, 24, 18);
                        g.DrawEllipse(steelEdge, x + 16, cy + 40, 24, 18);
                        g.DrawEllipse(steelEdge, x + 68, cy + 40, 24, 18);
                        foreach (float lx in legXs)
                        {
                            g.FillRectangle(steel, lx, cy + 138, legW, 28);
                            g.DrawRectangle(steelEdge, lx, cy + 138, legW, 28);
                        }
                    }
                    break;

                case 3: // Titan Plate
                    using (SolidBrush plate = new SolidBrush(Color.FromArgb(55, 55, 72)))
                    using (SolidBrush gold = new SolidBrush(Color.Gold))
                    using (SolidBrush core = new SolidBrush(fighter.AuraColor))
                    using (Pen trim = new Pen(Color.Gold, 2))
                    {
                        g.FillRectangle(plate, x + 29, cy + 44, 50, 48);
                        g.DrawRectangle(trim, x + 29, cy + 44, 50, 48);
                        g.FillEllipse(core, x + 46, cy + 60, 16, 16);
                        g.FillEllipse(gold, x + 10, cy + 34, 32, 24);
                        g.FillEllipse(gold, x + 66, cy + 34, 32, 24);
                        foreach (float lx in legXs)
                        {
                            g.FillRectangle(plate, lx, cy + 134, legW, 34);
                            g.DrawRectangle(trim, lx, cy + 134, legW, 34);
                            g.FillEllipse(gold, lx + 4, cy + 126, legW - 8, 14);
                        }
                        g.FillPie(plate, x + 32, cy - 6, 44, 44, 180, 180);
                        g.DrawArc(trim, x + 32, cy - 6, 44, 44, 180, 180);
                        g.FillRectangle(gold, x + 52, cy - 14, 4, 12);
                    }
                    break;
            }

            switch (loadout.SkinIndex)
            {
                case 1: // Vanguard: blue guard emblem + shoulder guards
                    PointF[] emblem =
                    {
                        new PointF(x + 44, cy + 50), new PointF(x + 64, cy + 50), new PointF(x + 64, cy + 68),
                        new PointF(x + 54, cy + 80), new PointF(x + 44, cy + 68)
                    };
                    using (SolidBrush blue = new SolidBrush(Color.FromArgb(50, 110, 230)))
                    using (Pen outline = new Pen(Color.White, 2))
                    {
                        g.FillPolygon(blue, emblem);
                        g.DrawPolygon(outline, emblem);
                        g.FillEllipse(blue, x + 16, cy + 42, 22, 14);
                        g.FillEllipse(blue, x + 70, cy + 42, 22, 14);
                    }
                    break;

                case 2: // Berserker: red headband with tails + war paint
                    float wave = (float)Math.Sin(bounceCycle * 3f) * 4f;
                    PointF[] tails = faceRight
                        ? new PointF[] { new PointF(x + 35, cy + 9), new PointF(x + 10, cy + 2 + wave), new PointF(x + 14, cy + 18 - wave), new PointF(x + 35, cy + 15) }
                        : new PointF[] { new PointF(x + 73, cy + 9), new PointF(x + 98, cy + 2 + wave), new PointF(x + 94, cy + 18 - wave), new PointF(x + 73, cy + 15) };
                    using (SolidBrush band = new SolidBrush(Color.Crimson))
                    using (Pen paint = new Pen(Color.Crimson, 3))
                    {
                        g.FillRectangle(band, x + 35, cy + 9, 38, 6);
                        g.FillPolygon(band, tails);
                        if (loadout.ArmorIndex < 2)
                        {
                            g.DrawLine(paint, x + 40, cy + 52, x + 48, cy + 72);
                            g.DrawLine(paint, x + 52, cy + 52, x + 60, cy + 72);
                            g.DrawLine(paint, x + 64, cy + 52, x + 72, cy + 72);
                        }
                    }
                    break;

                case 3: // Duelist: gold sash
                    using (Pen sash = new Pen(Color.Gold, 6))
                    {
                        g.DrawLine(sash, x + 32, cy + 46, x + 78, cy + 98);
                    }
                    break;
            }
        }

        private void DrawFighterReflection(Graphics g, float x, float y, int action, bool faceRight, FighterInfo fighter)
        {
            if (y < GroundLevel - 30) return;
            float refY = GroundLevel + 160 + (GroundLevel + 160 - (y + 160));
            using (SolidBrush refBrush = new SolidBrush(Color.FromArgb(35, fighter.AuraColor)))
            {
                g.FillEllipse(refBrush, x + 35, refY + 20, 38, 30);
                g.FillRectangle(refBrush, x + 30, refY - 20, 48, 40);
            }
        }
    }
}