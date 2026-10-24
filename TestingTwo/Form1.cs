using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Gui
{
    /// <summary>
    /// Main game window: shared game state, setup, and the engine loop. The rest of Form1 lives in the Form1.*.cs files.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class Form1 : Form
    {
        // --- GAME FLOW ---
        // Idinagdag ang MatchResult (results screen pagkatapos ng buong match)
        private enum GameState { ModeSelect, CharSelect, ArenaSelect, Armory, FightIntro, Battle, KO, MatchResult }
        private GameState currentState = GameState.ModeSelect;

        private int selectedMode = 0;
        private bool isVsCPU = false;
        private int p1CharIndex = 0, p2CharIndex = 1;
        private bool p1Locked = false;
        private int selectedArenaIndex = 0;

        // --- FACING DIRECTION STATES (KALIWA O KANAN) ---
        private bool p1FacingRight = true;
        private bool p2FacingRight = false;

        // --- THE MORTAL TOLL (AGING SYSTEM) STATES ---
        // Age States: 0 = Youth, 1 = Veteran, 2 = Elder
        private int p1AgeState = 0;
        private int p2AgeState = 0;

        // Round Wins Tracker & Current Round (Best of 3 / Tekken Style)
        private int p1RoundsWon = 0;
        private int p2RoundsWon = 0;
        private int currentRound = 1;

        // --- LOADOUTS AND ARMORY STATE ---
        private Loadout p1Loadout = new Loadout();
        private Loadout p2Loadout = new Loadout();
        private Loadout ActiveLoadout => armoryPlayer == 1 ? p1Loadout : p2Loadout;
        private int armoryPlayer = 1;
        private int armoryTab = 0;
        private int armoryCursor = 0;
        private string armoryMessage = "";
        private int armoryFlash = 0;
        private Color armoryFlashColor = Color.Gold;

        // Arena select animation
        private float arenaSlide = 0f;          // eased position of the selector (in card indexes)
        private int arenaPrevIndex = 0;          // arena that is sliding out of the preview box
        private int arenaTransitionDir = 1;     // +1 = new arena slides in from the right
        private float arenaTransition = 1f;     // 0 = just changed, 1 = finished
        private string rewardText = "";

        // --- BATTLE STATS ---
        private int p1Health = 100, p2Health = 100;
        private int p1RedHealth = 100, p2RedHealth = 100;
        private const int MaxHealth = 100;   // Para sa health bar, gamitin na ang p1MaxHealth / p2MaxHealth (Form1.Battle.cs)
        private int p1Heat = 60, p2Heat = 60;
        private const int MaxHeat = 100;
        private int p1ComboHits = 0, p2ComboHits = 0;
        private int p1ComboTimer = 0, p2ComboTimer = 0;
        private int roundTimer = 60;
        private int cpuDecisionTimer = 0;

        // --- SUPERPOWER & PROJECTILE MECHANICS ---
        private const float LeftWall = 120f;
        private const float RightWall = 800f;
        private bool p1IsWallBounced = false;
        private bool p2IsWallBounced = false;

        // Shield / Forcefield States
        private bool p1HasShield = false;
        private bool p2HasShield = false;
        private int p1ShieldTimer = 0;
        private int p2ShieldTimer = 0;

        // Active Magic Projectiles (Fireballs, Energy Blasts, Ice Shards)
        private List<MagicProjectile> activeProjectiles = new List<MagicProjectile>();

        private class MagicProjectile
        {
            public int OwnerPlayer; // 1 or 2
            public float X;
            public float Y;
            public float Vx;
            public int Damage;
            public Color ElementColor;
            public int Radius = 12;
            public bool IsExplosive;
        }

        // Floating Combo Damage Indicators
        private List<DamagePopup> damagePopups = new List<DamagePopup>();

        private class DamagePopup
        {
            public float X;
            public float Y;
            public string Text = "";
            public Color Color = Color.Yellow;
            public int Lifetime = 40;
            public float Vy = -1.5f;
        }

        // --- VISUAL FX ---
        private List<ShockwaveRing> shockwaves = new List<ShockwaveRing>();
        private List<FloorCrater> craters = new List<FloorCrater>();
        private List<AmbientMote> ambientMotes = new List<AmbientMote>();
        private List<GhostTrail> trails = new List<GhostTrail>();
        private List<Particle> particles = new List<Particle>();
        private List<LightningArc> lightnings = new List<LightningArc>();

        // Physics
        private const float GroundLevel = 270f;
        private const float Gravity = 1.35f;
        private float p1X = 260, p1Y = GroundLevel;
        private float p2X = 660, p2Y = GroundLevel;
        private float p1VelX = 0, p1VelY = 0;
        private float p2VelX = 0, p2VelY = 0;
        private int p1Action = 0, p2Action = 0;
        private int p1ActionTimer = 0, p2ActionTimer = 0;

        // Visual Engine Timers
        private int speedLineTimer = 0;
        private PointF speedLineCenter = PointF.Empty;
        private int hitStopFreeze = 0;
        private int rageCinematicFlash = 0;
        private float cameraZoom = 1.0f;
        private float targetZoom = 1.0f;
        private int screenShake = 0;
        private int introCounter = 60;
        private string matchResultText = "";
        private float bounceCycle = 0;
        private float stageAnimationCycle = 0;
        private Random rng = new Random();
        private System.Windows.Forms.Timer renderTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer clockTimer = new System.Windows.Forms.Timer();

        public Form1()
        {
            InitializeComponent();

            this.Text = "Clash Arena";
            InitArcadeUi();   // sizes the window and builds the arcade cabinet (see Form1.ArcadeUi.cs)
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            for (int i = 0; i < 45; i++)
            {
                ambientMotes.Add(new AmbientMote
                {
                    X = rng.Next(-200, 1300),
                    Y = rng.Next(50, 580),
                    Vx = (float)(rng.NextDouble() * 1.5 - 0.75),
                    Vy = (float)(rng.NextDouble() * -2.0 - 0.5),
                    Size = rng.Next(3, 8),
                    Alpha = rng.Next(70, 220)
                });
            }

            renderTimer.Interval = 16;
            renderTimer.Tick += EngineLoop_Tick;
            renderTimer.Start();

            clockTimer.Interval = 1000;
            clockTimer.Tick += (s, e) =>
            {
                if (currentState == GameState.Battle && roundTimer > 0 && hitStopFreeze == 0)
                {
                    roundTimer--;
                }
            };
            clockTimer.Start();

            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
        }

        private void EngineLoop_Tick(object? sender, EventArgs e)
        {
            if (hitStopFreeze > 0)
            {
                hitStopFreeze--;
                this.Invalidate();
                return;
            }

            bounceCycle += 0.08f;
            if (armoryFlash > 0) armoryFlash--;
            arenaSlide += (selectedArenaIndex - arenaSlide) * 0.2f;
            if (arenaTransition < 1f) arenaTransition = Math.Min(1f, arenaTransition + 0.09f);
            stageAnimationCycle += 0.04f; // Drives the movement of lava, smoke, and billboards
            if (rageCinematicFlash > 0) rageCinematicFlash--;
            if (speedLineTimer > 0) speedLineTimer--;

            if (currentState == GameState.FightIntro)
            {
                introCounter--;
                if (introCounter <= 0) currentState = GameState.Battle;
            }
            else if (currentState == GameState.Battle)
            {
                if (isVsCPU) UpdateCPUAI();
                UpdateBattlePhysics();
                UpdateSuperpowersAndProjectiles(); // Projectiles and Elemental powers loop
                UpdateWallBounceMechanics();
                UpdateComboTimers();
            }
            else if (currentState == GameState.KO)
            {
                // Countdown pagkatapos ng round: next round o results screen (tingnan ang Form1.Battle.cs)
                UpdateKO();
                UpdateComboTimers();   // para patuloy na gumalaw/mawala ang damage popups
            }

            UpdateVisualEffects();
            UpdateFighterSprites();   // keep the sprite animations in step with the game (see Form1.Sprites.cs)
            this.Invalidate();
        }

        // =========================================================================
        // --- THE MORTAL TOLL (AGING SYSTEM STAT MULTIPLIERS) ---
        // =========================================================================
        private float GetAgeSpeedMultiplier(int age)
        {
            if (age == 1) return 0.8f;   // Veteran: 20% Slower
            if (age == 2) return 0.55f;  // Elder: Drastically Slower
            return 1.0f;               // Youth: Fast Prime
        }

        private float GetAgeDamageMultiplier(int age)
        {
            if (age == 1) return 1.45f;  // Veteran: +45% Damage
            if (age == 2) return 2.8f;   // Elder: Massive Damage
            return 1.0f;               // Youth: Base Damage
        }

        private int GetAgeMaxHP(int age)
        {
            if (age == 1) return 85;     // Veteran: -15% HP Pool
            if (age == 2) return 60;     // Elder: Drastically Reduced HP Pool
            return 100;                  // Youth: 100 HP Prime
        }

        // --- SUPERPOWER LOGIC & ACTIONS ---

        /// <summary>
        /// Fires a elemental projectile (Fireball, Dark Energy Blast, Lightning Orb).
        /// </summary>
        public void SpawnProjectile(int owner, Color color, int damage, bool isExplosive = false)
        {
            bool isFacingRight = (owner == 1) ? p1FacingRight : p2FacingRight;
            float fighterX = (owner == 1) ? p1X : p2X;
            float fighterY = (owner == 1) ? p1Y : p2Y;
            float dir = isFacingRight ? 12.0f : -12.0f;

            // Lalabas sa dibdib (gitna ng katawan), hindi na sa itaas ng ulo
            float startX = fighterX + 54f;
            float startY = fighterY + 80f;

            activeProjectiles.Add(new MagicProjectile
            {
                OwnerPlayer = owner,
                X = startX + (isFacingRight ? 40 : -40),
                Y = startY,
                Vx = dir,
                Damage = damage,
                ElementColor = color,
                IsExplosive = isExplosive,
                Radius = isExplosive ? 20 : 12   // mas malaki ang rage/ultimate
            });

            AddDamagePopup(fighterX, fighterY - 20, "SPELL!", color);
        }

        /// <summary>
        /// Instantly teleports a fighter behind the opponent with a shadow/ghost trail effect.
        /// </summary>
        public void TriggerTeleport(int player)
        {
            if (player == 1)
            {
                p1Action = 0;
                p1ActionTimer = 0;

                trails.Add(new GhostTrail
                {
                    X = p1X,
                    Y = p1Y,
                    Color = Color.Purple,
                    Alpha = 255,
                    Age = 0,
                    FaceRight = p1FacingRight
                });

                // Lumipat sa likod ni P2 at humarap sa kaniya
                if (p1X <= p2X)
                {
                    p1X = p2X + 140f;
                    p1FacingRight = false; // Nasa kanan na si P1 kaya haharap sa KALIWA
                }
                else
                {
                    p1X = p2X - 140f;
                    p1FacingRight = true;  // Nasa kaliwa na si P1 kaya haharap sa KANAN
                }

                p1X = Math.Clamp(p1X, LeftWall + 20f, RightWall - 20f);
                p1VelX = 0;

                trails.Add(new GhostTrail
                {
                    X = p1X,
                    Y = p1Y,
                    Color = Color.MediumVioletRed,
                    Alpha = 255,
                    Age = 0,
                    FaceRight = p1FacingRight
                });

                AddDamagePopup(p1X, p1Y - 30, "SHADOW STEP!", Color.MediumPurple);
            }
            else
            {
                p2Action = 0;
                p2ActionTimer = 0;

                trails.Add(new GhostTrail
                {
                    X = p2X,
                    Y = p2Y,
                    Color = Color.Purple,
                    Alpha = 255,
                    Age = 0,
                    FaceRight = p2FacingRight
                });

                if (p2X <= p1X)
                {
                    p2X = p1X + 140f;
                    p2FacingRight = false;
                }
                else
                {
                    p2X = p1X - 140f;
                    p2FacingRight = true;
                }

                p2X = Math.Clamp(p2X, LeftWall + 20f, RightWall - 20f);
                p2VelX = 0;

                trails.Add(new GhostTrail
                {
                    X = p2X,
                    Y = p2Y,
                    Color = Color.MediumVioletRed,
                    Alpha = 255,
                    Age = 0,
                    FaceRight = p2FacingRight
                });

                AddDamagePopup(p2X, p2Y - 30, "SHADOW STEP!", Color.MediumPurple);
            }
            screenShake = 12;
        }

        /// <summary>
        /// Activates a magical Forcefield / Barrier.
        /// </summary>
        public void TriggerShield(int player)
        {
            if (player == 1)
            {
                p1HasShield = true;
                p1ShieldTimer = 120; // 2 seconds
                AddDamagePopup(p1X, p1Y - 30, "BARRIER!", Color.Cyan);
            }
            else
            {
                p2HasShield = true;
                p2ShieldTimer = 120;
                AddDamagePopup(p2X, p2Y - 30, "BARRIER!", Color.Cyan);
            }
        }

        /// <summary>
        /// UNIQUE HERO SKILLS: Direct mapping base sa totoong Roster ng Clash Arena
        /// </summary>
        public void UseHeroSkill(int player, int skillSlot)
        {
            int charIndex = (player == 1) ? p1CharIndex : p2CharIndex;

            // --- HERO 0: Insu KAZAMA ---
            if (charIndex == 0)
            {
                switch (skillSlot)
                {
                    case 1: // F Key: Quick Punch
                        SpawnProjectile(player, Color.DeepSkyBlue, 15, false);
                        break;
                    case 2: // G Key: Kazama Electric Fist
                        if (player == 1) p1VelX = p1FacingRight ? 18f : -18f;
                        else p2VelX = p2FacingRight ? 18f : -18f;
                        AddDamagePopup((player == 1 ? p1X : p2X), (player == 1 ? p1Y : p2Y) - 30, "ELECTRIC FIST!", Color.Cyan);
                        break;
                    case 3: // T Key: Ultimate Strike
                        SpawnProjectile(player, Color.Cyan, 30, true);
                        break;
                }
            }
            // --- HERO 1: Andre MISHIMA ---
            else if (charIndex == 1)
            {
                switch (skillSlot)
                {
                    case 1: // F Key: Devil Laser Beam
                        SpawnProjectile(player, Color.Purple, 20, false);
                        break;
                    case 2: // G Key: SHADOW STEP / INSTANT TELEPORT!
                        TriggerTeleport(player);
                        break;
                    case 3: // T Key: Devil Laser Explosion (Ultimate)
                        SpawnProjectile(player, Color.MediumPurple, 35, true);
                        break;
                }
            }
            // --- HERO 2: Justin Phoenix ---
            else if (charIndex == 2)
            {
                switch (skillSlot)
                {
                    case 1: // F Key: Heavy Jab
                        SpawnProjectile(player, Color.OrangeRed, 18, false);
                        break;
                    case 2: // G Key: Phoenix Smasher (Barrier / Super Armor)
                        TriggerShield(player);
                        AddDamagePopup((player == 1 ? p1X : p2X), (player == 1 ? p1Y : p2Y) - 30, "PHOENIX SMASHER!", Color.Orange);
                        break;
                    case 3: // T Key: Infernal Phoenix Blast (Ultimate)
                        SpawnProjectile(player, Color.Red, 35, true);
                        break;
                }
            }
            // --- HERO 3: Jayvee Hworang ---
            else if (charIndex == 3)
            {
                switch (skillSlot)
                {
                    case 1: // F Key: Tae Kwon Do Kick
                        SpawnProjectile(player, Color.LawnGreen, 14, false);
                        break;
                    case 2: // G Key: Hunting Hawk Kick (High Air Jump)
                        if (player == 1) { p1VelY = -25f; p1VelX = p1FacingRight ? 12f : -12f; }
                        else { p2VelY = -25f; p2VelX = p2FacingRight ? 12f : -12f; }
                        AddDamagePopup((player == 1 ? p1X : p2X), (player == 1 ? p1Y : p2Y) - 30, "HUNTING HAWK!", Color.LimeGreen);
                        break;
                    case 3: // T Key: Rapid Kick Flurry (Ultimate)
                        SpawnProjectile(player, Color.GreenYellow, 30, true);
                        break;
                }
            }
        }

        // Tama ba ang projectile sa katawan ng fighter? (swept check para hindi lumagpas)
        private bool ProjectileHits(MagicProjectile proj, float prevX, float fighterX, float fighterY)
        {
            float centerX = fighterX + 54f;                                   // gitna ng katawan
            float halfW = 40f + proj.Radius + (proj.IsExplosive ? 25f : 0f);  // mas malapad ang hit ng rage

            bool xHit = Math.Max(prevX, proj.X) >= centerX - halfW &&
                        Math.Min(prevX, proj.X) <= centerX + halfW;

            // Buong taas ng katawan (ulo hanggang paa), kaya tatama kahit tumalon
            bool yHit = proj.Y + proj.Radius >= fighterY - 10 &&
                        proj.Y - proj.Radius <= fighterY + 190;

            return xHit && yHit;
        }

        private void UpdateSuperpowersAndProjectiles()
        {
            if (p1ShieldTimer > 0)
            {
                p1ShieldTimer--;
                if (p1ShieldTimer == 0) p1HasShield = false;
            }
            if (p2ShieldTimer > 0)
            {
                p2ShieldTimer--;
                if (p2ShieldTimer == 0) p2HasShield = false;
            }

            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                var proj = activeProjectiles[i];
                bool ownerIsP1 = proj.OwnerPlayer == 1;
                int defender = ownerIsP1 ? 2 : 1;

                float defX = ownerIsP1 ? p2X : p1X;
                float defY = ownerIsP1 ? p2Y : p1Y;
                bool defShield = ownerIsP1 ? p2HasShield : p1HasShield;

                // Rage/ultimate: sumusunod ng bahagya sa taas ng kalaban (hindi na madaling mamiss)
                if (proj.IsExplosive)
                {
                    float targetCenterX = defX + 54f;
                    bool targetAhead = (proj.Vx > 0 && targetCenterX > proj.X) || (proj.Vx < 0 && targetCenterX < proj.X);
                    if (targetAhead) proj.Y += ((defY + 90f) - proj.Y) * 0.08f;
                }

                float prevX = proj.X;
                proj.X += proj.Vx;

                if (ProjectileHits(proj, prevX, defX, defY))
                {
                    float hitCenterX = defX + 54f;

                    if (defShield)
                    {
                        AddDamagePopup(defX, defY - 30, "BLOCKED!", Color.Cyan);
                    }
                    else
                    {
                        int dmg = ComputeDamageTaken(defender, proj.Damage);   // kasama na ang armor

                        if (ownerIsP1)
                        {
                            p2Health = Math.Max(0, p2Health - dmg);
                            p1ComboHits++;
                            p1ComboTimer = 90;
                        }
                        else
                        {
                            p1Health = Math.Max(0, p1Health - dmg);
                            p2ComboHits++;
                            p2ComboTimer = 90;
                        }

                        AddDamagePopup(defX, defY - 30, $"{dmg} MAG!", proj.ElementColor);

                        // Stun + knockback (maliban sa Elder na may hyper armor)
                        int defAge = ownerIsP1 ? p2AgeState : p1AgeState;
                        float push = Math.Sign(proj.Vx) * (proj.IsExplosive ? 14f : 7f);
                        if (defAge != 2)
                        {
                            int stun = proj.IsExplosive ? 24 : 12;
                            if (ownerIsP1) { p2Action = 4; p2ActionTimer = stun; p2VelX = push; }
                            else { p1Action = 4; p1ActionTimer = stun; p1VelX = push; }
                        }

                        SpawnShockwave(new PointF(hitCenterX, proj.Y), proj.IsExplosive ? 140 : 80, proj.ElementColor);
                        SpawnImpactParticles(hitCenterX, proj.Y, proj.IsExplosive ? 28 : 14, proj.ElementColor);
                        hitStopFreeze = proj.IsExplosive ? 8 : 3;
                        screenShake = proj.IsExplosive ? 14 : 7;
                    }

                    activeProjectiles.RemoveAt(i);
                    continue;
                }

                if (proj.X < LeftWall - 150 || proj.X > RightWall + 200)   // luwag para tumama kahit nasa pader ang kalaban
                {
                    activeProjectiles.RemoveAt(i);
                }
            }
        }

        private void UpdateWallBounceMechanics()
        {
            if (p1X <= LeftWall || p1X >= RightWall)
            {
                p1X = Math.Clamp(p1X, LeftWall, RightWall);
                if (Math.Abs(p1VelX) > 8.0f && !p1IsWallBounced)
                {
                    p1VelX = -p1VelX * 0.6f;
                    p1VelY = -6.0f;
                    p1IsWallBounced = true;
                    screenShake = 12;
                    AddDamagePopup(p1X, p1Y - 40, "WALL SPLAT!", Color.OrangeRed);
                }
            }
            else if (p1Y >= GroundLevel)
            {
                p1IsWallBounced = false;
            }

            if (p2X <= LeftWall || p2X >= RightWall)
            {
                p2X = Math.Clamp(p2X, LeftWall, RightWall);
                if (Math.Abs(p2VelX) > 8.0f && !p2IsWallBounced)
                {
                    p2VelX = -p2VelX * 0.6f;
                    p2VelY = -6.0f;
                    p2IsWallBounced = true;
                    screenShake = 12;
                    AddDamagePopup(p2X, p2Y - 40, "WALL SPLAT!", Color.OrangeRed);
                }
            }
            else if (p2Y >= GroundLevel)
            {
                p2IsWallBounced = false;
            }
        }

        private void UpdateComboTimers()
        {
            if (p1ComboTimer > 0)
            {
                p1ComboTimer--;
                if (p1ComboTimer == 0) p1ComboHits = 0;
            }

            if (p2ComboTimer > 0)
            {
                p2ComboTimer--;
                if (p2ComboTimer == 0) p2ComboHits = 0;
            }

            for (int i = damagePopups.Count - 1; i >= 0; i--)
            {
                var popup = damagePopups[i];
                popup.Y += popup.Vy;
                popup.Lifetime--;
                if (popup.Lifetime <= 0)
                {
                    damagePopups.RemoveAt(i);
                }
            }
        }

        public void AddDamagePopup(float x, float y, string text, Color color)
        {
            damagePopups.Add(new DamagePopup
            {
                X = x,
                Y = y,
                Text = text,
                Color = color,
                Lifetime = 45,
                Vy = -1.2f
            });
        }
    }
}