using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Gui
{
    public partial class Form1 : Form
    {
        private void StartMatch()
        {
            if (isVsCPU) p2Loadout = BuildCpuLoadout();

            p1Loadout.ResetMatchState();
            p2Loadout.ResetMatchState();

            p1Health = p1RedHealth = MaxHealth + GameData.SkinCatalog[p1Loadout.SkinIndex].HealthModifier;
            p2Health = p2RedHealth = MaxHealth + GameData.SkinCatalog[p2Loadout.SkinIndex].HealthModifier;
            p1Heat = 50; p2Heat = 50;

            p1X = 260; p2X = 660;
            p1Y = GroundLevel; p2Y = GroundLevel;
            p1VelX = 0; p2VelX = 0;
            p1VelY = 0; p2VelY = 0;
            p1Action = 0; p2Action = 0;
            p1ComboHits = 0; p2ComboHits = 0;

            roundTimer = 60;
            introCounter = 60;

            craters.Clear();
            shockwaves.Clear();
            particles.Clear();
            lightnings.Clear();
            trails.Clear();

            currentState = GameState.FightIntro;
            Sfx.Play(880, 200);
        }

        private void AwardMatchGold()
        {
            int p1Reward, p2Reward;

            if (p1Health == p2Health) p1Reward = p2Reward = GameConfig.DrawReward;
            else if (p1Health > p2Health) { p1Reward = GameConfig.WinReward; p2Reward = GameConfig.LoseReward; }
            else { p1Reward = GameConfig.LoseReward; p2Reward = GameConfig.WinReward; }

            p1Loadout.Gold += p1Reward;
            if (isVsCPU)
            {
                rewardText = $"P1 earned +{p1Reward} gold  (total: {p1Loadout.Gold})";
            }
            else
            {
                p2Loadout.Gold += p2Reward;
                rewardText = $"P1 +{p1Reward} gold   |   P2 +{p2Reward} gold";
            }
        }

        private void UpdateCPUAI()
        {
            if (p2Action == 4 || p2Action == 5) return;

            cpuDecisionTimer--;
            if (cpuDecisionTimer <= 0)
            {
                cpuDecisionTimer = rng.Next(8, 18);
                float dist = p2X - p1X;

                if (dist > 180)
                {
                    p2VelX = -13 * SpeedFactor(2);
                    if (rng.Next(0, 10) == 0 && p2Y >= GroundLevel) p2VelY = -22;
                }
                else if (dist <= 180 && dist >= 0)
                {
                    int moveChoice = rng.Next(0, 10);
                    SkillInfo finisher = GameData.SkillCatalog[p2Loadout.SkillSlots[2]];

                    if (p2Heat >= finisher.HeatCost && moveChoice < 3) ExecuteAttack(2, 2);
                    else if (moveChoice < 6) ExecuteAttack(2, 1);
                    else if (moveChoice < 9) ExecuteAttack(2, 0);
                    else p2VelX = 10 * SpeedFactor(2);
                }
                else
                {
                    p2VelX = 10 * SpeedFactor(2);
                }
            }
        }

        private void UpdateBattlePhysics()
        {
            if (p1RedHealth > p1Health) p1RedHealth--;
            if (p2RedHealth > p2Health) p2RedHealth--;

            p1X += p1VelX; p1VelX *= 0.82f;
            p2X += p2VelX; p2VelX *= 0.82f;

            if (Math.Abs(p1VelX) > 6 || p1Action == 3) trails.Add(new GhostTrail { X = p1X, Y = p1Y, Color = GameData.Roster[p1CharIndex].AuraColor, Alpha = 140, Animation = p1Sprite.CurrentAnimation, Frame = p1Sprite.Frame, FaceRight = true, Age = p1Sprite.CurrentAge });
            if (Math.Abs(p2VelX) > 6 || p2Action == 3) trails.Add(new GhostTrail { X = p2X, Y = p2Y, Color = GameData.Roster[p2CharIndex].AuraColor, Alpha = 140, Animation = p2Sprite.CurrentAnimation, Frame = p2Sprite.Frame, FaceRight = false, Age = p2Sprite.CurrentAge });

            p1Y += p1VelY;
            if (p1Y < GroundLevel) p1VelY += Gravity;
            else
            {
                if (p1VelY > 12)
                {
                    craters.Add(new FloorCrater { X = p1X + 45, Y = GroundLevel + 160, Life = 100 });
                    SpawnShockwave(new PointF(p1X + 45, GroundLevel + 160), 100, Color.White);
                    screenShake = 10;
                }
                p1Y = GroundLevel;
                p1VelY = 0;
            }

            p2Y += p2VelY;
            if (p2Y < GroundLevel) p2VelY += Gravity;
            else
            {
                if (p2VelY > 12)
                {
                    craters.Add(new FloorCrater { X = p2X + 45, Y = GroundLevel + 160, Life = 100 });
                    SpawnShockwave(new PointF(p2X + 45, GroundLevel + 160), 100, Color.White);
                    screenShake = 10;
                }
                p2Y = GroundLevel;
                p2VelY = 0;
            }

            int leftWall = 80;
            int rightWall = this.ClientSize.Width - 160;

            if (p1X <= leftWall)
            {
                p1X = leftWall;
                if (p1Action == 4 && Math.Abs(p1VelX) > 3) TriggerWallSplat(1);
            }
            if (p2X >= rightWall)
            {
                p2X = rightWall;
                if (p2Action == 4 && Math.Abs(p2VelX) > 3) TriggerWallSplat(2);
            }

            if (p1ActionTimer > 0) { p1ActionTimer--; if (p1ActionTimer == 0) p1Action = 0; }
            if (p2ActionTimer > 0) { p2ActionTimer--; if (p2ActionTimer == 0) p2Action = 0; }

            if (p1ComboTimer > 0)
            {
                p1ComboTimer--;
                if (p1ComboTimer == 0) { p1ComboHits = 0; p1Loadout.ComboStep = 0; }
            }
            if (p2ComboTimer > 0)
            {
                p2ComboTimer--;
                if (p2ComboTimer == 0) { p2ComboHits = 0; p2Loadout.ComboStep = 0; }
            }

            if (screenShake > 0) screenShake--;

            // Camera Following has been removed. Forced static camera zoom.
            cameraZoom = 1.0f;
            targetZoom = 1.0f;

            if (p1Health <= 0 || p2Health <= 0 || roundTimer <= 0)
            {
                if (p1Health <= 0) { p1Action = 99; p1ActionTimer = 60; }
                if (p2Health <= 0) { p2Action = 99; p2ActionTimer = 60; }

                currentState = GameState.KO;
                Sfx.Play(150, 450);
                AwardMatchGold();
                if (p1Health == p2Health) matchResultText = "DOUBLE K.O. / DRAW";
                else matchResultText = p1Health > p2Health ? $"{GameData.Roster[p1CharIndex].Name} WINS!" : $"{(isVsCPU ? "CPU (" + GameData.Roster[p2CharIndex].Name + ")" : GameData.Roster[p2CharIndex].Name)} WINS!";
            }
        }

        private void TriggerWallSplat(int fighterId)
        {
            screenShake = 16;
            hitStopFreeze = 5;
            Sfx.Play(200, 220);
            if (fighterId == 1)
            {
                p1Action = 5; p1ActionTimer = 18; p1VelX = 14;
                SpawnShockwave(new PointF(p1X, p1Y + 90), 120, Color.Cyan);
                SpawnImpactParticles(p1X, p1Y + 90, 16, Color.Cyan);
            }
            else
            {
                p2Action = 5; p2ActionTimer = 18; p2VelX = -14;
                SpawnShockwave(new PointF(p2X + 80, p2Y + 90), 120, Color.Crimson);
                SpawnImpactParticles(p2X + 80, p2Y + 90, 16, Color.Crimson);
            }
        }

        private Loadout GetLoadout(int player)
        {
            return player == 1 ? p1Loadout : p2Loadout;
        }

        private float SpeedFactor(int player)
        {
            return CombatRules.DashSpeed(GetLoadout(player));
        }

        private int GetHeatGain(int player)
        {
            return CombatRules.HeatGain(GetLoadout(player));
        }

        private int ComputeDamageTaken(int defender, int rawDamage)
        {
            return CombatRules.DamageTaken(GetLoadout(defender), rawDamage);
        }

        private void RegisterComboStep(Loadout loadout, int slot)
        {
            if (CombatRules.RegisterComboStep(loadout, slot)) Sfx.Play(1100, 120);
        }

        private void ExecuteAttack(int attacker, int slot)
        {
            bool isP1 = attacker == 1;
            int defender = isP1 ? 2 : 1;
            float dir = isP1 ? 1f : -1f;

            Loadout atkLoadout = isP1 ? p1Loadout : p2Loadout;
            FighterInfo atkFighter = isP1 ? GameData.Roster[p1CharIndex] : GameData.Roster[p2CharIndex];
            SkillInfo skill = GameData.SkillCatalog[atkLoadout.SkillSlots[slot]];
            bool isHeavy = skill.Type == SkillType.Finisher;

            ref float atkX = ref (isP1 ? ref p1X : ref p2X);
            ref float atkY = ref (isP1 ? ref p1Y : ref p2Y);
            ref float atkVelX = ref (isP1 ? ref p1VelX : ref p2VelX);
            ref int atkHeat = ref (isP1 ? ref p1Heat : ref p2Heat);
            ref int atkAction = ref (isP1 ? ref p1Action : ref p2Action);
            ref int atkActionTimer = ref (isP1 ? ref p1ActionTimer : ref p2ActionTimer);
            ref int atkComboHits = ref (isP1 ? ref p1ComboHits : ref p2ComboHits);
            ref int atkComboTimer = ref (isP1 ? ref p1ComboTimer : ref p2ComboTimer);

            ref float defX = ref (isP1 ? ref p2X : ref p1X);
            ref float defY = ref (isP1 ? ref p2Y : ref p1Y);
            ref float defVelX = ref (isP1 ? ref p2VelX : ref p1VelX);
            ref float defVelY = ref (isP1 ? ref p2VelY : ref p1VelY);
            ref int defHealth = ref (isP1 ? ref p2Health : ref p1Health);
            ref int defHeat = ref (isP1 ? ref p2Heat : ref p1Heat);
            ref int defAction = ref (isP1 ? ref p2Action : ref p1Action);
            ref int defActionTimer = ref (isP1 ? ref p2ActionTimer : ref p1ActionTimer);

            if (atkHeat < skill.HeatCost) return;

            if (skill.HeatCost > 0)
            {
                atkHeat -= skill.HeatCost;
                if (isHeavy)
                {
                    rageCinematicFlash = 14;
                    CreateLightningBurst(atkX + (isP1 ? 60 : 40), atkY + 60, atkFighter.AuraColor);
                }
            }

            atkAction = skill.Action;
            atkActionTimer = skill.ActionFrames;
            atkVelX += dir * skill.Lunge * SpeedFactor(attacker);

            bool inRange = Math.Abs(p1X - p2X) <= skill.Reach && Math.Abs(p1Y - p2Y) < 100;
            if (!inRange)
            {
                Sfx.Play(280, 50);
                return;
            }

            int damage = skill.Damage;
            if (isHeavy && atkLoadout.ComboCharge > 0)
            {
                damage += atkLoadout.ComboCharge * GameConfig.ComboChargeBonusDamage;
                atkLoadout.ComboCharge = 0;
            }

            defHealth = Math.Max(0, defHealth - ComputeDamageTaken(defender, damage));
            defAction = 4;
            defActionTimer = skill.StunFrames;
            defVelX = dir * skill.Knockback;
            if (skill.Launch < 0) defVelY = skill.Launch;
            defHeat = Math.Max(0, defHeat - skill.HeatDrain);

            atkComboHits++;
            atkComboTimer = 45;
            atkHeat = Math.Min(MaxHeat, atkHeat + GetHeatGain(attacker));
            RegisterComboStep(atkLoadout, slot);

            hitStopFreeze = isHeavy ? 9 : 3;
            screenShake = isHeavy ? 16 : 7;
            Color fxColor = isHeavy ? atkFighter.PrimaryColor : atkFighter.AuraColor;

            float hitOffsetX = isP1 ? 30 : 70;
            PointF hitP = new PointF(defX + hitOffsetX, defY + (skill.Action == 2 ? 30 : 60));
            SpawnShockwave(hitP, isHeavy ? 140 : 80, fxColor);
            SpawnImpactParticles(hitP.X, hitP.Y, 22, fxColor);
            CreateLightningBurst(hitP.X, hitP.Y, Color.Gold);

            speedLineCenter = hitP;
            speedLineTimer = isHeavy ? 14 : 7;

            Sfx.Play(isHeavy ? 980 : (skill.Action == 2 ? 650 : 500), 100);
        }

        private void SpawnShockwave(PointF center, float maxR, Color c)
        {
            shockwaves.Add(new ShockwaveRing { Center = center, Radius = 15, MaxRadius = maxR, Color = c, Alpha = 220 });
        }

        private void CreateLightningBurst(float startX, float startY, Color color)
        {
            for (int i = 0; i < 4; i++)
            {
                var arc = new LightningArc { Color = color, Life = 7 };
                float curX = startX, curY = startY;
                arc.Points.Add(new PointF(curX, curY));

                for (int j = 0; j < 5; j++)
                {
                    curX += rng.Next(-35, 36);
                    curY += rng.Next(-35, 36);
                    arc.Points.Add(new PointF(curX, curY));
                }
                lightnings.Add(arc);
            }
        }

        private void SpawnImpactParticles(float x, float y, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                particles.Add(new Particle
                {
                    X = x,
                    Y = y,
                    Vx = (float)(rng.NextDouble() * 12 - 6),
                    Vy = (float)(rng.NextDouble() * 12 - 6),
                    Life = 16,
                    MaxLife = 16,
                    Color = color,
                    Size = rng.Next(5, 11)
                });
            }
        }

        private void UpdateVisualEffects()
        {
            for (int i = shockwaves.Count - 1; i >= 0; i--)
            {
                shockwaves[i].Radius += 8f;
                shockwaves[i].Alpha -= 14;
                if (shockwaves[i].Alpha <= 0 || shockwaves[i].Radius >= shockwaves[i].MaxRadius) shockwaves.RemoveAt(i);
            }

            for (int i = craters.Count - 1; i >= 0; i--)
            {
                craters[i].Life--;
                if (craters[i].Life <= 0) craters.RemoveAt(i);
            }

            foreach (var m in ambientMotes)
            {
                m.X += m.Vx;
                m.Y += m.Vy;
                if (m.Y < 40)
                {
                    m.Y = 560;
                    m.X = rng.Next(-100, 1200);
                }
            }

            for (int i = trails.Count - 1; i >= 0; i--)
            {
                trails[i].Alpha -= 18;
                if (trails[i].Alpha <= 0) trails.RemoveAt(i);
            }

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                particles[i].X += particles[i].Vx;
                particles[i].Y += particles[i].Vy;
                particles[i].Life--;
                if (particles[i].Life <= 0) particles.RemoveAt(i);
            }

            for (int i = lightnings.Count - 1; i >= 0; i--)
            {
                lightnings[i].Life--;
                if (lightnings[i].Life <= 0) lightnings.RemoveAt(i);
            }
        }
    }
}