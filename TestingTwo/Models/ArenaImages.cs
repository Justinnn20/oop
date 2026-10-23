using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Gui
{
    /// <summary>
    /// Loads and caches the background pictures used by maps (embedded in the program).
    /// </summary>
    internal static class ArenaImages
    {
        private static readonly Dictionary<string, Bitmap?> cache = new Dictionary<string, Bitmap?>();

        /// <summary>Returns the map's background picture, or null if it has none or the file is missing.</summary>
        public static Bitmap? Get(ArenaInfo arena)
        {
            if (string.IsNullOrEmpty(arena.BackgroundFile)) return null;
            if (cache.TryGetValue(arena.BackgroundFile, out Bitmap? cached)) return cached;

            Bitmap? result = null;
            try
            {
                // The picture is embedded in the program (see TestingTwo.csproj), so there is no folder to get wrong
                using Stream? embedded = typeof(ArenaImages).Assembly.GetManifestResourceStream(arena.BackgroundFile);
                string path = Path.Combine(AppContext.BaseDirectory, "Assets", arena.BackgroundFile);

                if (embedded != null) result = ToFastBitmap(new Bitmap(embedded));
                else if (File.Exists(path)) result = ToFastBitmap(new Bitmap(path));
            }
            catch
            {
                result = null;
            }

            cache[arena.BackgroundFile] = result;
            return result;
        }

        // Premultiplied alpha format draws much faster every frame
        private static Bitmap ToFastBitmap(Bitmap original)
        {
            using (original)
            {
                Bitmap fast = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppPArgb);
                using Graphics g = Graphics.FromImage(fast);
                g.DrawImage(original, 0, 0, original.Width, original.Height);
                return fast;
            }
        }
    }
}