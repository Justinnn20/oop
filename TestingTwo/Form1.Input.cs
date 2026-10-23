using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Gui
{
    /// <summary>
    /// Keyboard input for every screen.
    /// </summary>
    public partial class Form1 : Form
    {
        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (currentState == GameState.ModeSelect)
            {
                if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Up) selectedMode = 0;
                if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Down) selectedMode = 1;
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                {
                    isVsCPU = (selectedMode == 1);
                    currentState = GameState.CharSelect;
                    p1Locked = false;
                    Sfx.Play(650, 90);
                }
            }
            else if (currentState == GameState.CharSelect)
            {
                if (!p1Locked)
                {
                    if (e.KeyCode == Keys.Left) p1CharIndex = (p1CharIndex - 1 + GameData.Roster.Length) % GameData.Roster.Length;
                    if (e.KeyCode == Keys.Right) p1CharIndex = (p1CharIndex + 1) % GameData.Roster.Length;
                    if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                    {
                        p1Locked = true;
                        Sfx.Play(600, 80);
                        if (isVsCPU)
                        {
                            p2CharIndex = (p1CharIndex + 1) % GameData.Roster.Length;
                            currentState = GameState.ArenaSelect;
                            Sfx.Play(750, 100);
                        }
                    }
                    if (e.KeyCode == Keys.Back) currentState = GameState.ModeSelect;
                }
                else if (!isVsCPU)
                {
                    if (e.KeyCode == Keys.Left) p2CharIndex = (p2CharIndex - 1 + GameData.Roster.Length) % GameData.Roster.Length;
                    if (e.KeyCode == Keys.Right) p2CharIndex = (p2CharIndex + 1) % GameData.Roster.Length;
                    if (e.KeyCode == Keys.Enter) { currentState = GameState.ArenaSelect; Sfx.Play(750, 100); }
                    if (e.KeyCode == Keys.Back) p1Locked = false;
                }
            }
            else if (currentState == GameState.ArenaSelect)
            {
                if (e.KeyCode == Keys.Left) ChangeArena(-1);
                if (e.KeyCode == Keys.Right) ChangeArena(1);
                if (e.KeyCode == Keys.Enter) OpenArmory();
                if (e.KeyCode == Keys.Back)
                {
                    currentState = GameState.CharSelect;
                    if (isVsCPU) p1Locked = false;
                }
            }
            else if (currentState == GameState.Armory)
            {
                HandleArmoryInput(e);
            }
            else if (currentState == GameState.Battle)
            {
                // Player 1: move/jump, then skill slots 1-3
                if (e.KeyCode == Keys.A && p1X > 80) p1VelX = -13 * SpeedFactor(1);
                if (e.KeyCode == Keys.D && p1X < p2X - 70) p1VelX = 13 * SpeedFactor(1);
                if (e.KeyCode == Keys.W && p1Y >= GroundLevel) { p1VelY = -22; Sfx.Play(440, 60); }
                if (e.KeyCode == Keys.F) ExecuteAttack(1, 0);
                if (e.KeyCode == Keys.G) ExecuteAttack(1, 1);
                if (e.KeyCode == Keys.T) ExecuteAttack(1, 2);

                if (!isVsCPU)
                {
                    // Player 2: move/jump, then skill slots 1-3
                    if (e.KeyCode == Keys.J && p2X > p1X + 70) p2VelX = -13 * SpeedFactor(2);
                    if (e.KeyCode == Keys.L && p2X < this.ClientSize.Width - 160) p2VelX = 13 * SpeedFactor(2);
                    if (e.KeyCode == Keys.I && p2Y >= GroundLevel) { p2VelY = -22; Sfx.Play(440, 60); }
                    if (e.KeyCode == Keys.K) ExecuteAttack(2, 0);
                    if (e.KeyCode == Keys.O) ExecuteAttack(2, 1);
                    if (e.KeyCode == Keys.P) ExecuteAttack(2, 2);
                }
            }
            else if (currentState == GameState.KO && e.KeyCode == Keys.R)
            {
                currentState = GameState.ModeSelect;
                p1Locked = false;
            }
        }
    }
}
