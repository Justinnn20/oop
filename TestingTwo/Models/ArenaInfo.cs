using System.Drawing;

namespace Gui
{
    /// <summary>
    /// A battle stage: its name and the color palette used to draw it.
    /// </summary>
    internal class ArenaInfo
    {
        public string Name { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public Color SkyTop { get; set; }
        public Color SkyBottom { get; set; }
        public Color FloorColor { get; set; }
        public Color RingNeon { get; set; }
        public Color ParticleColor { get; set; }

        /// <summary>Optional picture drawn as the stage background (file name inside the Assets folder).</summary>
        public string? BackgroundFile { get; set; }
    }
}