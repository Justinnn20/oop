using System.Collections.Generic;
using System.Drawing;

namespace Gui
{
    internal class ShockwaveRing
    {
        public PointF Center;
        public float Radius, MaxRadius;
        public Color Color;
        public int Alpha;
    }

    internal class FloorCrater
    {
        public float X, Y;
        public int Life;
    }

    internal class AmbientMote
    {
        public float X, Y, Vx, Vy, Size;
        public int Alpha;
    }

    internal class GhostTrail
    {
        public float X, Y;
        public Color Color;
        public int Alpha;
    }

    internal class Particle
    {
        public float X, Y, Vx, Vy;
        public int Life, MaxLife;
        public Color Color;
        public float Size;
    }

    internal class LightningArc
    {
        public List<PointF> Points = new List<PointF>();
        public Color Color;
        public int Life;
    }
}
