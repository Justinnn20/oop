using System;
using System.Drawing;

namespace Gui
{
    /// <summary>
    /// Pure C# Player state manager for TestingTwo (WinForms).
    /// Tracks age stages, movements, animations, health pools, and shadow meters.
    /// </summary>
    public class Player
    {
        // --- "THE MORTAL TOLL" AGE STAGES ---
        public AgeStage CurrentAge { get; private set; } = AgeStage.Youth;

        // Health Pool & Stat Tracking
        public float MaxHealth { get; private set; } = 100f;
        public float CurrentHealth { get; set; } = 100f;

        // Chrono-Shadow Meter (0 to 100)
        public float ShadowMeter { get; set; } = 0f;

        // Movement & Mechanic Tracking
        public int AirDashCount { get; set; } = 0;
        public bool IsBlocking { get; set; } = false;

        // Animation State Outputs
        public string CurrentAnimation { get; private set; } = "Idle";
        public int Frame { get; private set; } = 0;
        public bool Hurt { get; private set; } = false;

        private float frameTimer = 0f;

        /// <summary>
        /// Updates the player's age stage and recalibrates their maximum HP pool ("The Mortal Toll").
        /// </summary>
        public void SetAge(AgeStage age)
        {
            CurrentAge = age;
            MaxHealth = age switch
            {
                AgeStage.Veteran => 85f, // -15% Max HP Pool
                AgeStage.Elder => 65f,   // Drastically reduced HP Pool
                _ => 100f
            };
            CurrentHealth = MaxHealth;
        }

        /// <summary>
        /// Returns movement speed multiplier based on current age stage.
        /// </summary>
        public float GetSpeedMultiplier()
        {
            return CurrentAge switch
            {
                AgeStage.Veteran => 0.85f, // -15% Movement speed
                AgeStage.Elder => 0.60f,   // Slowest walk speed
                _ => 1.00f
            };
        }

        /// <summary>
        /// Returns damage multiplier for normal attacks based on current age stage.
        /// </summary>
        public float GetDamageMultiplier()
        {
            return CurrentAge switch
            {
                AgeStage.Veteran => GameConfig.VeteranDmgMult, // +45% Damage
                AgeStage.Elder => GameConfig.ElderDmgMult,     // Massive 3-4 hit KO power
                _ => 1.00f
            };
        }

        /// <summary>
        /// Elder stage gain: Hyper Armor during attacks prevents hitstun flinching.
        /// </summary>
        public bool HasHyperArmor(int currentAction)
        {
            return CurrentAge == AgeStage.Elder && currentAction > 0 && currentAction != 4;
        }

        // Animations that repeat. Everything else plays once and holds its last frame.
        private static bool Loops(string animation)
        {
            return animation == "Idle" || animation == "Running_Walking";
        }

        /// <summary>
        /// Synchronizes player movements, jump/air-dash states, and attack animations each game tick.
        /// Animation names here must match the ones FighterSprites / GodotAtlas know about.
        /// </summary>
        public void SyncFromGame(float x, float velX, float velY, bool isOnGround, int action, int actionTimer, bool isP2)
        {
            // Reset air dash count when landing on floor
            if (isOnGround)
            {
                AirDashCount = 0;
            }

            Hurt = action == 4;

            string next;
            if (action == 99) next = "Defeated";
            else if (action == 4 || action == 5) next = "Hurt";
            else if (!isOnGround) next = action > 0 ? "Jump_Attack" : "Jump";
            else if (action == 1) next = "Punch1";
            else if (action == 2) next = "Kick";
            else if (action == 3) next = "Down_Attack";
            else if (Math.Abs(velX) > 1f) next = "Running_Walking";
            else next = "Idle";

            // A new animation always starts from its first frame
            if (next != CurrentAnimation)
            {
                CurrentAnimation = next;
                Frame = 0;
                frameTimer = 0f;
            }

            // Step through the frames of the current animation
            int frameCount = Math.Max(1, FighterSprites.FrameCountForAge(CurrentAge, CurrentAnimation));
            frameTimer += 0.25f;
            if (frameTimer >= 1f)
            {
                frameTimer = 0f;
                if (Loops(CurrentAnimation)) Frame = (Frame + 1) % frameCount;
                else Frame = Math.Min(Frame + 1, frameCount - 1);
            }
        }
    }
}