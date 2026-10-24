using System;
using System.Drawing;

namespace Gui
{
    /// <summary>
    /// Manages 3-second delayed phantom strikes and unblockable cross-up calculations.
    /// </summary>
    public class ChronoShadow
    {
        public int OwnerPlayer { get; set; }       // 1 or 2
        public float X { get; set; }
        public float Y { get; set; }
        public bool FaceRight { get; set; }
        public int FighterIndex { get; set; }
        public int Action { get; set; }            // Recorded attack animation
        public AgeStage Age { get; set; }          // Age stage when phantom was dropped
        public float Timer { get; set; } = GameConfig.ShadowDelaySeconds; // 3.0 second countdown
        public bool Executed { get; set; } = false;
        public float ExecutionTimer { get; set; } = 0.4f;

        public void Update(float dt)
        {
            if (Timer > 0f)
            {
                Timer -= dt;
            }
            else if (!Executed)
            {
                Executed = true;
            }
            else
            {
                ExecutionTimer -= dt;
            }
        }
    }
}