using System;
using System.Threading.Tasks;

namespace Gui
{
    /// <summary>
    /// Procedural sound effects.
    /// </summary>
    internal static class Sfx
    {
        /// <summary>Plays a short beep without blocking the game loop.</summary>
        public static void Play(int frequency, int durationMs)
        {
            Task.Run(() =>
            {
                try { Console.Beep(frequency, durationMs); } catch { }
            });
        }
    }
}
