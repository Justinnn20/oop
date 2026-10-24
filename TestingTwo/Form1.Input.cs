using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Gui
{
    public partial class Form1 : Form
    {
        // Cooldown Timer para hindi ma-spam ang Teleport/Skills
        // (bumababa kada frame sa UpdateBattlePhysics, hindi na sa KeyDown)
        private int p1SkillCooldown = 0;
        private int p2SkillCooldown = 0;

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

                // BYPASS SHOP/ARMORY MECHANIC
                if (e.KeyCode == Keys.Enter) StartMatch();

                if (e.KeyCode == Keys.Back)
                {
                    currentState = GameState.CharSelect;
                    if (isVsCPU) p1Locked = false;
                }
            }
            else if (currentState == GameState.Battle)
            {
                // --- PLAYER 1 MOVEMENT & FACING DIRECTION ---
                if (e.KeyCode == Keys.A && p1X > LeftWall)
                {
                    p1VelX = -13 * SpeedFactor(1);
                    p1FacingRight = false; // Haharap sa KALIWA
                }
                if (e.KeyCode == Keys.D && p1X < RightWall)
                {
                    p1VelX = 13 * SpeedFactor(1);
                    p1FacingRight = true;  // Haharap sa KANAN
                }
                if (e.KeyCode == Keys.W && p1Y >= GroundLevel) { p1VelY = -22; Sfx.Play(440, 60); }

                // --- PLAYER 1 ATTACKS & SKILLS ---
                if (e.KeyCode == Keys.F)
                {
                    ExecuteAttack(1, 0); // Quick Jab / Attack
                }
                if (e.KeyCode == Keys.G)
                {
                    if (p1SkillCooldown <= 0)
                    {
                        UseHeroSkill(1, 2);
                        p1SkillCooldown = 120; // 2 Seconds Cooldown
                    }
                    else
                    {
                        AddDamagePopup(p1X, p1Y - 30, "RECHARGING...", Color.Gray);
                    }
                }
                if (e.KeyCode == Keys.T)
                {
                    if (p1Heat >= 100)
                    {
                        UseHeroSkill(1, 3);
                        p1Heat = 0;
                    }
                    else
                    {
                        AddDamagePopup(p1X, p1Y - 30, "NEED FULL HEAT!", Color.Red);
                    }
                }

                if (!isVsCPU)
                {
                    // --- PLAYER 2 MOVEMENT & FACING DIRECTION ---
                    if (e.KeyCode == Keys.J && p2X > LeftWall)
                    {
                        p2VelX = -13 * SpeedFactor(2);
                        p2FacingRight = false; // Haharap sa KALIWA
                    }
                    if (e.KeyCode == Keys.L && p2X < RightWall)
                    {
                        p2VelX = 13 * SpeedFactor(2);
                        p2FacingRight = true;  // Haharap sa KANAN
                    }
                    if (e.KeyCode == Keys.I && p2Y >= GroundLevel) { p2VelY = -22; Sfx.Play(440, 60); }

                    // --- PLAYER 2 ATTACKS & SKILLS ---
                    if (e.KeyCode == Keys.K)
                    {
                        ExecuteAttack(2, 0);
                    }
                    if (e.KeyCode == Keys.O)
                    {
                        if (p2SkillCooldown <= 0)
                        {
                            UseHeroSkill(2, 2);
                            p2SkillCooldown = 120;
                        }
                        else
                        {
                            AddDamagePopup(p2X, p2Y - 30, "RECHARGING...", Color.Gray);
                        }
                    }
                    if (e.KeyCode == Keys.P)
                    {
                        if (p2Heat >= 100)
                        {
                            UseHeroSkill(2, 3);
                            p2Heat = 0;
                        }
                        else
                        {
                            AddDamagePopup(p2X, p2Y - 30, "NEED FULL HEAT!", Color.Red);
                        }
                    }
                }
            }
            // Results screen lang (pagkatapos ng FINAL ROUND / may nanalo na ng 2 rounds)
            else if (currentState == GameState.MatchResult && (e.KeyCode == Keys.R || e.KeyCode == Keys.Enter))
            {
                currentState = GameState.ModeSelect;
                p1Locked = false;
                Sfx.Play(650, 90);
            }
        }
    }
}   