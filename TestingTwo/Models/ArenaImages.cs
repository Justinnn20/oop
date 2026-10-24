using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Gui
{
    /// <summary>
    /// Loads and caches the background pictures used by maps (embedded or from disk).
    /// </summary>
    internal static class ArenaImages
    {
        private static readonly Dictionary<string, Bitmap?> cache = new Dictionary<string, Bitmap?>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Returns the map's background picture, or null if missing.</summary>
        public static Bitmap? Get(ArenaInfo arena)
        {
            if (string.IsNullOrEmpty(arena.BackgroundFile)) return null;
            if (cache.TryGetValue(arena.BackgroundFile, out Bitmap? cached)) return cached;

            Bitmap? result = LoadImageFile(arena.BackgroundFile);

            // Alias lookup for elevator filename variations
            if (result == null && arena.BackgroundFile.Contains("elevator", StringComparison.OrdinalIgnoreCase))
            {
                result = LoadImageFile("elevator_lobby.png") ?? LoadImageFile("elevator_16bit.png");
            }

            cache[arena.BackgroundFile] = result;
            return result;
        }

        private static Bitmap? LoadImageFile(string filename)
        {
            try
            {
                Assembly asm = typeof(ArenaImages).Assembly;

                // 1. Search Embedded Resources matching filename suffix
                string? resName = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("." + filename, StringComparison.OrdinalIgnoreCase) ||
                                         n.EndsWith("/" + filename, StringComparison.OrdinalIgnoreCase) ||
                                         n.Equals(filename, StringComparison.OrdinalIgnoreCase));

                if (resName != null)
                {
                    using Stream? stream = asm.GetManifestResourceStream(resName);
                    if (stream != null) return ToFastBitmap(new Bitmap(stream));
                }

                // 2. Search File System Paths (Output & Source Directories)
                string baseDir = AppContext.BaseDirectory;
                string currentDir = Directory.GetCurrentDirectory();

                List<string> candidatePaths = new List<string>
                {
                    Path.Combine(baseDir, "Assets", filename),
                    Path.Combine(baseDir, filename),
                    Path.Combine(currentDir, "Assets", filename),
                    Path.Combine(currentDir, filename),
                    filename
                };

                // Traverse up to 4 parent directories (locates files directly in project source root)
                DirectoryInfo? dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 4 && dir != null; i++)
                {
                    candidatePaths.Add(Path.Combine(dir.FullName, "Assets", filename));
                    candidatePaths.Add(Path.Combine(dir.FullName, filename));
                    dir = dir.Parent;
                }

                foreach (string path in candidatePaths.Distinct())
                {
                    if (File.Exists(path))
                    {
                        return ToFastBitmap(new Bitmap(path));
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

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