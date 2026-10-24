using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Gui
{
    public partial class Form1 : Form
    {
        // ====== ROUND SYSTEM (Tekken style: best of 3) ======
        // PAALALA: yung currentRound, p1RoundsWon, p2RoundsWon, matchResultText, introCounter
        // ay nasa kabilang file mo na (ginagamit na sa lumang code), kaya hindi ko na dineclare dito.
        // Kung wala pa pala, idagdag mo sa main Form1 file:
        //   private int currentRound = 1; private int p1RoundsWon, p2RoundsWon;
        private const int RoundsToWin = 2;

        private int p1MaxHealth, p2MaxHealth;
        private int koCounter;              // delay bago next round / results
        private bool matchOver;
        private string roundBannerText = "";

        // Tawagin ito sa menu kapag magsisimula ng BAGONG laban
        private void StartMatch()
        {
            if (isVsCPU) p2Loadout = BuildCpuLoadout();

            currentRound = 1;
            p1RoundsWon = 0;
            p2RoundsWon = 0;
            p1AgeState = 0;
            p2AgeState = 0;
            matchOver = false;
            matchResultText = "";

            StartRound();
        }

        // Tinatawag tuwing simula ng bawat round
        private void StartRound()
        {
            p1Loadout.ResetMatchState();
            p2Loadout.ResetMatchState();

            p1MaxHealth = GetAgeMaxHP(p1AgeState) + GameData.SkinCatalog[p1Loadout.SkinIndex].HealthModifier;
            p2MaxHealth = GetAgeMaxHP(p2AgeState) + GameData.SkinCatalog[p2Loadout.SkinIndex].HealthModifier;

            p1Health = p1RedHealth = p1MaxHealth;
            p2Health = p2RedHealth = p2MaxHealth;
            p1Heat = 50; p2Heat = 50;

            p1SkillCooldown = 0;
            p2SkillCooldown = 0;
            p1HasShield = false;
            p2HasShield = false;
            p1ShieldTimer = 0;
            p2ShieldTimer = 0;
            activeProjectiles.Clear();
            damagePopups.Clear();

            p1X = 260; p2X = 660;
            p1Y = GroundLevel; p2Y = GroundLevel;
            p1VelX = 0; p2VelX = 0;
            p1VelY = 0; p2VelY = 0;
            p1Action = 0; p2Action = 0;
            p1ActionTimer = 0; p2ActionTimer = 0;
            p1ComboHits = 0; p2ComboHits = 0;
            p1ComboTimer = 0; p2ComboTimer = 0;

            p1FacingRight = true;
            p2FacingRight = false;

            roundTimer = 60;
            introCounter = 90;          // ~1.5 sec: "ROUND X" tapos "FIGHT!"
            roundBannerText = "";
            koCounter = 0;

            craters.Clear();
            shockwaves.Clear();
            particles.Clear();
            lightnings.Clear();
            trails.Clear();

            currentState = GameState.FightIntro;
            Sfx.Play(880, 200);

            string p1Title = p1AgeState == 0 ? "YOUTH" : (p1AgeState == 1 ? "VETERAN" : "ELDER");
            string p2Title = p2AgeState == 0 ? "YOUTH" : (p2AgeState == 1 ? "VETERAN" : "ELDER");
            AddDamagePopup(p1X, p1Y - 50, $"P1: {p1Title}", Color.Cyan);
            AddDamagePopup(p2X, p2Y - 50, $"P2: {p2Title}", Color.Crimson);
        }

        private string GetRoundTitle()
        {
            // Tekken style: kapag parehong isang panalo na, FINAL ROUND
            bool final = (p1RoundsWon == RoundsToWin - 1 && p2RoundsWon == RoundsToWin - 1);
            return final ? "FINAL ROUND" : $"ROUND {currentRound}";
        }

        private void EndRound()
        {
            if (p1Health <= 0) { p1Action = 99; p1ActionTimer = 60; }
            if (p2Health <= 0) { p2Action = 99; p2ActionTimer = 60; }

            bool knockedOut = p1Health <= 0 || p2Health <= 0;
            roundBannerText = knockedOut ? "K.O.!" : "TIME UP";

            currentState = GameState.KO;
            Sfx.Play(150, 450);

            string p1Name = GameData.Roster[p1CharIndex].Name;
            string p2Name = isVsCPU ? "CPU" : GameData.Roster[p2CharIndex].Name;

            if (p1Health > p2Health)
            {
                p1RoundsWon++;
                p2AgeState = Math.Min(2, p2AgeState + 1);
                matchResultText = $"{p1Name} WINS ROUND {currentRound}!";
            }
            else if (p2Health > p1Health)
            {
                p2RoundsWon++;
                p1AgeState = Math.Min(2, p1AgeState + 1);
                matchResultText = $"{p2Name} WINS ROUND {currentRound}!";
            }
            else
            {
                // Draw: parehong may puntos (parang Tekken)
                p1RoundsWon++;
                p2RoundsWon++;
                p1AgeState = Math.Min(2, p1AgeState + 1);
                p2AgeState = Math.Min(2, p2AgeState + 1);
                matchResultText = "DRAW!";
            }

            bool p1Done = p1RoundsWon >= RoundsToWin;
            bool p2Done = p2RoundsWon >= RoundsToWin;

            // Tapos na ang match kung may nakaabot sa 2 at hindi tabla
            if ((p1Done || p2Done) && p1RoundsWon != p2RoundsWon)
            {
                matchOver = true;
                matchResultText = p1RoundsWon > p2RoundsWon
                    ? $"{p1Name} MATCH WINNER!"
                    : $"{p2Name} MATCH WINNER!";
                AwardMatchGold();
            }

            koCounter = 150;      // ~2.5 sec bago next round / results
        }

        // Tawagin ito sa game loop kapag currentState == GameState.KO
        private void UpdateKO()
        {
            if (koCounter > 0) { koCounter--; return; }

            if (matchOver)
                currentState = GameState.MatchResult;   // palitan kung iba ang pangalan ng results/menu state mo
            else
            {
                currentRound++;   // next round (kasama ang extra round kung tabla pareho sa 2)
                StartRound();
            }
        }

        private void AwardMatchGold()
        {
            int p1Reward, p2Reward;

            if (p1RoundsWon == p2RoundsWon) p1Reward = p2Reward = GameConfig.DrawReward;
            else if (p1RoundsWon > p2RoundsWon) { p1Reward = GameConfig.WinReward; p2Reward = GameConfig.LoseReward; }
            else { p1Reward = GameConfig.LoseReward; p2Reward = GameConfig.WinReward; }

            p1Loadout.Gold += p1Reward;
            if (isVsCPU)
            {
                rewardText = $"P1 earned +{p1Reward} gold   (total: {p1Loadout.Gold})";
            }
            else
            {
                p2Loadout.Gold += p2Reward;
                rewardText = $"P1 +{p1Reward} gold   |   P2 +{p2Reward} gold";
            }
        }

        // ====== HUD: timer, round pips (score) sa taas, at banner ======
        // Tawagin sa OnPaint / draw function mo, pagkatapos ma-draw ang health bars
        private void DrawRoundHUD(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cx = this.ClientSize.Width / 2;

            // Timer sa gitna
            using (var timerFont = new Font("Arial", 28, FontStyle.Bold))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString(Math.Max(0, roundTimer).ToString(), timerFont, Brushes.White, cx, 12, sf);
            }

            // Round pips (score): P1 sa kaliwa ng timer, P2 sa kanan
            for (int i = 0; i < RoundsToWin; i++)
            {
                DrawPip(g, cx - 55 - i * 26, 62, i < p1RoundsWon, Color.Cyan);
                DrawPip(g, cx + 55 + i * 26, 62, i < p2RoundsWon, Color.Crimson);
            }

            // Banner sa gitna ng screen
            string banner = null;
            if (currentState == GameState.FightIntro)
                banner = introCounter > 35 ? GetRoundTitle() : "FIGHT!";
            else if (currentState == GameState.KO)
                banner = roundBannerText;

            if (banner != null)
            {
                DrawCenteredText(g, banner, 72, this.ClientSize.Height / 2 - 90,
                                 banner == "FIGHT!" ? Color.OrangeRed : Color.Gold);

                if (currentState == GameState.KO && koCounter < 110)
                    DrawCenteredText(g, matchResultText, 30, this.ClientSize.Height / 2 + 10, Color.White);
            }
        }

        // Pips lang (walang timer), para hindi mag-doble sa DrawModernHUD.
        // Nasa ILALIM ng timer, sa gitna mismo, para hindi tumama sa health bars.
        // Kung kailangan pang ibaba/itaas, baguhin lang ang pipY.
        private void DrawRoundPips(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cx = this.ClientSize.Width / 2;
            int pipY = 100;

            for (int i = 0; i < RoundsToWin; i++)
            {
                DrawPip(g, cx - 16 - i * 22, pipY, i < p1RoundsWon, Color.Cyan);
                DrawPip(g, cx + 16 + i * 22, pipY, i < p2RoundsWon, Color.Crimson);
            }
        }

        private void DrawPip(Graphics g, int x, int y, bool won, Color color)
        {
            var rect = new Rectangle(x - 8, y - 8, 16, 16);
            if (won)
            {
                using (var b = new SolidBrush(color)) g.FillEllipse(b, rect);
            }
            using (var p = new Pen(Color.White, 2)) g.DrawEllipse(p, rect);
        }

        private void DrawCenteredText(Graphics g, string text, float size, int y, Color color)
        {
            using (var font = new Font("Impact", size, FontStyle.Bold))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                float cx = this.ClientSize.Width / 2f;
                g.DrawString(text, font, Brushes.Black, cx + 3, y + 3, sf); // shadow
                using (var b = new SolidBrush(color))
                    g.DrawString(text, font, b, cx, y, sf);
            }
        }

        // ====== CPU AI ======
        private void UpdateCPUAI()
        {
            if (p2Action == 4 || p2Action == 5) return;

            p2FacingRight = (p1X > p2X);

            cpuDecisionTimer--;
            if (cpuDecisionTimer <= 0)
            {
                cpuDecisionTimer = rng.Next(8, 18);
                float dist = p2X - p1X;

                if (Math.Abs(dist) > 180)
                {
                    p2VelX = (p1X > p2X) ? 13 * SpeedFactor(2) : -13 * SpeedFactor(2);
                    if (rng.Next(0, 10) == 0 && p2Y >= GroundLevel) p2VelY = -22;
                }
                else
                {
                    int moveChoice = rng.Next(0, 10);
                    SkillInfo finisher = GameData.SkillCatalog[p2Loadout.SkillSlots[2]];

                    if (p2Heat >= finisher.HeatCost && moveChoice < 3) ExecuteAttack(2, 2);
                    else if (moveChoice < 6) ExecuteAttack(2, 1);
                    else if (moveChoice < 9) ExecuteAttack(2, 0);
                    else p2VelX = (p1X > p2X) ? 10 * SpeedFactor(2) : -10 * SpeedFactor(2);
                }
            }
        }

        // ====== PHYSICS ======
        private void UpdateBattlePhysics()
        {
            if (p1RedHealth > p1Health) p1RedHealth--;
            if (p2RedHealth > p2Health) p2RedHealth--;

            if (p1SkillCooldown > 0) p1SkillCooldown--;
            if (p2SkillCooldown > 0) p2SkillCooldown--;

            float nextP1X = p1X + p1VelX;
            float nextP2X = p2X + p2VelX;
            float minDistance = 70f;

            if (p1Y >= GroundLevel && p2Y >= GroundLevel)
            {
                if (p1X < p2X)
                {
                    if (nextP1X > p2X - minDistance && p1VelX > 0)
                    {
                        p1X = p2X - minDistance;
                        p1VelX = 0;
                    }
                    if (nextP2X < p1X + minDistance && p2VelX < 0)
                    {
                        p2X = p1X + minDistance;
                        p2VelX = 0;
                    }
                }
                else
                {
                    if (nextP1X < p2X + minDistance && p1VelX < 0)
                    {
                        p1X = p2X + minDistance;
                        p1VelX = 0;
                    }
                    if (nextP2X > p1X - minDistance && p2VelX > 0)
                    {
                        p2X = p1X - minDistance;
                        p2VelX = 0;
                    }
                }
            }

            p1X += p1VelX; p1VelX *= 0.82f;
            p2X += p2VelX; p2VelX *= 0.82f;

            if (Math.Abs(p1VelX) > 6 || p1Action == 3) trails.Add(new GhostTrail { X = p1X, Y = p1Y, Color = GameData.Roster[p1CharIndex].AuraColor, Alpha = 140, Animation = p1Sprite.CurrentAnimation, Frame = p1Sprite.Frame, FaceRight = p1FacingRight, Age = p1Sprite.CurrentAge });
            if (Math.Abs(p2VelX) > 6 || p2Action == 3) trails.Add(new GhostTrail { X = p2X, Y = p2Y, Color = GameData.Roster[p2CharIndex].AuraColor, Alpha = 140, Animation = p2Sprite.CurrentAnimation, Frame = p2Sprite.Frame, FaceRight = p2FacingRight, Age = p2Sprite.CurrentAge });

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

            cameraZoom = 1.0f;
            targetZoom = 1.0f;

            // Guard: huwag ulit-ulitin ang EndRound kapag KO state na
            if (currentState != GameState.KO && (p1Health <= 0 || p2Health <= 0 || roundTimer <= 0))
            {
                EndRound();
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
            int age = (player == 1) ? p1AgeState : p2AgeState;
            return CombatRules.DashSpeed(GetLoadout(player)) * GetAgeSpeedMultiplier(age);
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
            float dir = isP1 ? (p1FacingRight ? 1f : -1f) : (p2FacingRight ? 1f : -1f);

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

            bool isFacingEnemy = isP1 ? (p1X <= p2X ? p1FacingRight : !p1FacingRight)
                                      : (p2X <= p1X ? p2FacingRight : !p2FacingRight);

            bool inRange = isFacingEnemy && Math.Abs(p1X - p2X) <= skill.Reach && Math.Abs(p1Y - p2Y) < 100;
            if (!inRange)
            {
                Sfx.Play(280, 50);
                return;
            }

            int atkAge = isP1 ? p1AgeState : p2AgeState;
            int defAge = isP1 ? p2AgeState : p1AgeState;

            int damage = (int)(skill.Damage * GetAgeDamageMultiplier(atkAge));

            if (isHeavy && atkLoadout.ComboCharge > 0)
            {
                damage += atkLoadout.ComboCharge * GameConfig.ComboChargeBonusDamage;
                atkLoadout.ComboCharge = 0;
            }

            defHealth = Math.Max(0, defHealth - ComputeDamageTaken(defender, damage));

            if (defAge == 2)
            {
                AddDamagePopup(defX, defY - 30, "HYPER ARMOR!", Color.Gold);
            }
            else
            {
                defAction = 4;
                defActionTimer = skill.StunFrames;
            }

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

        // ====== VISUAL EFFECTS ======
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