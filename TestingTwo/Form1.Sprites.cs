using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Gui
{
    /// <summary>
    /// Sprite fighters: one Player per fighter keeps the animation state, and DrawSpriteFighter draws it.
    /// The game engine (Form1.Battle.cs) still does all the moving, hitting and AI.
    /// </summary>
    public partial class Form1 : Form
    {
        private const float SpriteScale = 3f;            // each sprite pixel becomes 3x3 screen pixels
        private const float SpriteFootOffset = 184f;     // feet sit this far below the fighter's y (same as the old shape fighters)

        // Shows a message when the game starts, saying whether the sprite picture was found.
        // Set to false once your character shows up in the game.
        private const bool ShowSpriteCheck = true;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!ShowSpriteCheck) return;

            bool renderingUpdated = typeof(Form1)
                .GetMethod("DrawArticulatedFighter", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetParameters().Any(p => p.Name == "sprite") ?? false;

            MessageBox.Show(this,
                "Sprite picture: " + FighterSprites.Status +
                "\nForm1.Rendering.cs is the new version: " + (renderingUpdated ? "yes" : "NO - replace it with the new file"),
                "Sprite check");
        }

        private readonly Player p1Sprite = new Player();
        private readonly Player p2Sprite = new Player();

        // Called once per game tick (see EngineLoop_Tick in Form1.cs)
        private void UpdateFighterSprites()
        {
            p1Sprite.SyncFromGame(p1X, p1VelX, p1VelY, p1Y >= GroundLevel, p1Action, p1ActionTimer, false);
            p2Sprite.SyncFromGame(p2X, p2VelX, p2VelY, p2Y >= GroundLevel, p2Action, p2ActionTimer, true);
        }

        // Replaces the shape-drawn fighter whenever the sprite picture is available.
        // sprite = null draws the standing pose (character select, armory preview).
        private void DrawSpriteFighter(Graphics g, float x, float y, int action, bool faceRight, FighterInfo fighter, Loadout? loadout, Player? sprite)
        {
            float bob = 0f; // Disabled idle up-and-down floating motion
            float cy = y + bob;
            float footY = cy + SpriteFootOffset;
            float centerX = x + 54f;

            // Simple ground shadow (ring indicator removed)
            float shadowScale = Math.Max(0.4f, 1f - ((GroundLevel - y) / 250f));
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
            {
                g.FillEllipse(shadow, x + 15, GroundLevel + 160, 90 * shadowScale, 20 * shadowScale);
            }

            // Finisher glow behind the fighter
            if (action == 3)
            {
                using (GraphicsPath auraPath = new GraphicsPath())
                {
                    auraPath.AddEllipse(x - 25, cy - 25, 170, 230);
                    using (PathGradientBrush glow = new PathGradientBrush(auraPath))
                    {
                        glow.CenterColor = Color.FromArgb(210, fighter.AuraColor);
                        glow.SurroundColors = new Color[] { Color.Transparent };
                        g.FillPath(glow, auraPath);
                    }
                }
            }

            if (loadout != null) DrawSkinBackdrop(g, x, cy, faceRight, loadout.SkinIndex);

            string animation = sprite?.CurrentAnimation ?? "Idle";
            int frame = sprite?.Frame ?? 0;
            bool hurt = sprite?.Hurt ?? false;
            FighterSprites.Draw(g, Array.IndexOf(GameData.Roster, fighter), animation, frame, centerX, footY, SpriteScale, faceRight, hurt, sprite?.CurrentAge ?? AgeStage.Youth);

            using (Font nameFont = new Font("Impact", 11, FontStyle.Italic))
            {
                string tag = (!faceRight && isVsCPU) ? "CPU: " + fighter.Name : fighter.Name;
                g.DrawString(tag, nameFont, Brushes.White, x + 5, footY - (FighterSprites.CellH * SpriteScale) - 22f);
            }
        }
    }
}