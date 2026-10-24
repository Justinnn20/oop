using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gui
{
    /// <summary>
    /// Lets the Godot scene you made (Plater.tscn) drive the WinForms game.
    /// WinForms cannot run a .tscn, but the scene is a text file that says, for every character
    /// (youngver, middle, oldver and their shadow versions), which animations exist and which
    /// rectangle of which PNG each frame is. This class reads that text and hands the frames
    /// to FighterSprites. Edit the scene in Godot, copy it over, rebuild, and the game follows.
    /// </summary>
    internal static class GodotAtlas
    {
        // Scene file names to look for.
        private static readonly string[] SceneNames = { "Plater.tscn", "Plater_1.tscn", "Player.tscn" };

        // The AnimatedSprite2D node names used in the scene.
        public const string YoungNode = "youngver";
        public const string YoungShadowNode = "Yshadowver";
        public const string MiddleNode = "middle";
        public const string MiddleShadowNode = "Mshadowver";
        public const string OlderNode = "oldver";
        public const string OlderShadowNode = "Oshadowver";

        // Set to false to ignore the Godot scene and go back to the plain strip files.
        public static readonly bool Enabled = true;

        // Game animation name -> animation names to look for in the scene (first one found wins).
        // Names are compared without case, spaces or underscores, so "Jump_attack", "jump attack" and "jumpattack" are the same.
        private static readonly Dictionary<string, string[]> Aliases = new Dictionary<string, string[]>
        {
            ["idle"] = new[] { "idle" },
            ["runningwalking"] = new[] { "walk" },
            ["jump"] = new[] { "jump" },
            ["duck"] = new[] { "duck" },
            ["hurt"] = new[] { "hurt" },
            ["defeated"] = new[] { "dead" },
            ["punch1"] = new[] { "attack1" },
            ["punch2"] = new[] { "attack2" },
            ["kick"] = new[] { "attack3" },
            ["jumpattack"] = new[] { "jumpattack" },
            ["downattack"] = new[] { "downattack", "attackdown" },
            ["block"] = new[] { "block" },
            ["drop"] = new[] { "drop", "disappear" }
        };

        private sealed class GodotFrame
        {
            public string Path = "";          // e.g. "fighter_all_sets/young/Duck.png"
            public Rectangle Source;
        }

        // node name -> (normalized animation name -> frames)
        private static readonly Dictionary<string, Dictionary<string, List<GodotFrame>>> sets =
            new Dictionary<string, Dictionary<string, List<GodotFrame>>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Bitmap?> sheets = new Dictionary<string, Bitmap?>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> missing = new List<string>();
        private static List<string>? projectPngs;
        private static List<string>? projectScenes;
        private static bool loadTried;
        private static string status = "";

        public static bool Available
        {
            get { EnsureLoaded(); return sets.ContainsKey(YoungNode); }
        }

        public static string Status
        {
            get { EnsureLoaded(); return status; }
        }

        public static string NodeFor(AgeStage age)
        {
            return age switch
            {
                AgeStage.Veteran => MiddleNode,
                AgeStage.Elder => OlderNode,
                _ => YoungNode
            };
        }

        /// <summary>How many frames the scene has for a game animation at this age (0 if none).</summary>
        public static int FrameCount(AgeStage age, string animation)
        {
            return FrameCount(NodeFor(age), animation);
        }

        public static int FrameCount(string node, string animation)
        {
            EnsureLoaded();
            if (!sets.TryGetValue(node, out Dictionary<string, List<GodotFrame>>? set)) return 0;
            return Find(set, animation)?.Count ?? 0;
        }

        public static bool TryGetFrame(AgeStage age, string animation, int frame, out Bitmap? sheet, out Rectangle source)
        {
            return TryGetFrame(NodeFor(age), animation, frame, out sheet, out source);
        }

        /// <summary>Finds the picture and rectangle to draw for one frame of one animation of one character node.</summary>
        public static bool TryGetFrame(string node, string animation, int frame, out Bitmap? sheet, out Rectangle source)
        {
            sheet = null;
            source = Rectangle.Empty;
            EnsureLoaded();

            if (!sets.TryGetValue(node, out Dictionary<string, List<GodotFrame>>? set)) return false;
            List<GodotFrame>? frames = Find(set, animation);
            if (frames == null || frames.Count == 0) return false;

            GodotFrame picked = frames[Math.Clamp(frame, 0, frames.Count - 1)];
            Bitmap? bitmap = GetSheet(picked.Path);
            if (bitmap == null) return false;

            sheet = bitmap;
            source = picked.Source;
            return true;
        }

        private static string Normalize(string name)
        {
            return new string(name.Where(c => c != '_' && c != ' ').ToArray()).ToLowerInvariant();
        }

        private static List<GodotFrame>? Find(Dictionary<string, List<GodotFrame>> set, string animation)
        {
            string key = Normalize(animation);
            string[] candidates = Aliases.TryGetValue(key, out string[]? names) ? names : new[] { key };
            foreach (string candidate in candidates)
            {
                if (set.TryGetValue(candidate, out List<GodotFrame>? frames) && frames.Count > 0) return frames;
            }
            return null;
        }

        // ------------------------------------------------------------------ loading

        private static void EnsureLoaded()
        {
            if (loadTried) return;
            loadTried = true;

            if (!Enabled)
            {
                status = "turned off (strip files are used)";
                return;
            }

            try
            {
                string? usedScene = null;
                foreach (string scene in FindScenes())
                {
                    if (TryParseScene(scene))
                    {
                        usedScene = scene;
                        break;
                    }
                }

                if (usedScene == null)
                {
                    sets.Clear();
                    status = "no scene with a '" + YoungNode + "' character found (looked for " + string.Join(", ", SceneNames) + ")";
                    return;
                }

                // Load every picture the scene uses now, so a missing file shows up in the startup message.
                foreach (string path in sets.Values.SelectMany(s => s.Values).SelectMany(f => f).Select(f => f.Path).Distinct())
                {
                    GetSheet(path);
                }

                int youngAnimations = sets[YoungNode].Count;
                status = $"LOADED {sets.Count} characters from {Path.GetFileName(usedScene)} (young has {youngAnimations} animations)";
                if (missing.Count > 0)
                {
                    status += $"\nMISSING {missing.Count} PNG files: " + string.Join(", ", missing.Take(5)) + (missing.Count > 5 ? ", ..." : "");
                }
            }
            catch (Exception ex)
            {
                sets.Clear();
                status = "failed to read scene: " + ex.Message;
            }
        }

        private static bool TryParseScene(string scenePath)
        {
            sets.Clear();
            string text = File.ReadAllText(scenePath);

            // ext_resource: id -> picture path inside the Godot project, e.g. res://fighter_all_sets/young/Duck.png
            Dictionary<string, string> pictures = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(text, @"\[ext_resource[^\]]*?path=""res://([^""]+)""[^\]]*?id=""([^""]+)""\]"))
            {
                pictures[m.Groups[2].Value] = m.Groups[1].Value;
            }

            // AtlasTexture: id -> (picture, rectangle)
            Dictionary<string, GodotFrame> regions = new Dictionary<string, GodotFrame>();
            foreach (Match m in Regex.Matches(text,
                @"\[sub_resource type=""AtlasTexture"" id=""([^""]+)""\]\s*atlas = ExtResource\(""([^""]+)""\)[^\[]*?region = Rect2\(\s*(-?[\d.]+),\s*(-?[\d.]+),\s*(-?[\d.]+),\s*(-?[\d.]+)\s*\)"))
            {
                if (!pictures.TryGetValue(m.Groups[2].Value, out string? picturePath)) continue;
                regions[m.Groups[1].Value] = new GodotFrame
                {
                    Path = picturePath,
                    Source = new Rectangle(ToInt(m.Groups[3]), ToInt(m.Groups[4]), ToInt(m.Groups[5]), ToInt(m.Groups[6]))
                };
            }

            // SpriteFrames: id -> animations (name -> ordered frames)
            Dictionary<string, Dictionary<string, List<GodotFrame>>> frameSets = new Dictionary<string, Dictionary<string, List<GodotFrame>>>();
            foreach (Match block in Regex.Matches(text, @"\[sub_resource type=""SpriteFrames"" id=""([^""]+)""\]\r?\n(.*?)(?=\r?\n\[)", RegexOptions.Singleline))
            {
                Dictionary<string, List<GodotFrame>> animations = new Dictionary<string, List<GodotFrame>>();
                foreach (Match a in Regex.Matches(block.Groups[2].Value,
                    @"""frames"":\s*\[(.*?)\],\s*""loop"":\s*\w+,\s*""name"":\s*&?""([^""]+)""", RegexOptions.Singleline))
                {
                    List<GodotFrame> frames = new List<GodotFrame>();
                    foreach (Match f in Regex.Matches(a.Groups[1].Value, @"SubResource\(""([^""]+)""\)"))
                    {
                        if (regions.TryGetValue(f.Groups[1].Value, out GodotFrame? region)) frames.Add(region);
                    }

                    string key = Normalize(a.Groups[2].Value);
                    if (frames.Count > 0 && !animations.ContainsKey(key)) animations[key] = frames;
                }
                frameSets[block.Groups[1].Value] = animations;
            }

            // Nodes: AnimatedSprite2D name -> its SpriteFrames
            foreach (Match n in Regex.Matches(text,
                @"\[node name=""([^""]+)"" type=""AnimatedSprite2D""[^\]]*\]([^\[]*?)sprite_frames = SubResource\(""([^""]+)""\)"))
            {
                if (frameSets.TryGetValue(n.Groups[3].Value, out Dictionary<string, List<GodotFrame>>? animations))
                {
                    sets[n.Groups[1].Value] = animations;
                }
            }

            return sets.ContainsKey(YoungNode);
        }

        private static int ToInt(Group group)
        {
            return (int)Math.Round(float.Parse(group.Value, CultureInfo.InvariantCulture));
        }

        // ------------------------------------------------------------------ finding files

        private static string ProjectDir()
        {
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        }

        private static IEnumerable<string> FindScenes()
        {
            string baseDir = AppContext.BaseDirectory;
            string projectDir = ProjectDir();
            List<string> found = new List<string>();

            foreach (string name in SceneNames)
            {
                foreach (string dir in new[] { projectDir, Path.Combine(projectDir, "Assets"), baseDir, Path.Combine(baseDir, "Assets") })
                {
                    string candidate = Path.Combine(dir, name);
                    if (File.Exists(candidate)) found.Add(candidate);
                }
            }

            if (projectScenes == null)
            {
                try { projectScenes = Directory.GetFiles(projectDir, "*.tscn", SearchOption.AllDirectories).ToList(); }
                catch { projectScenes = new List<string>(); }
            }
            foreach (string name in SceneNames)
            {
                found.AddRange(projectScenes.Where(p => string.Equals(Path.GetFileName(p), name, StringComparison.OrdinalIgnoreCase)));
            }

            return found.Distinct();
        }

        private static Bitmap? GetSheet(string resPath)
        {
            if (sheets.TryGetValue(resPath, out Bitmap? cached)) return cached;

            Bitmap? bitmap = null;
            string? file = ResolvePath(resPath);
            if (file != null)
            {
                try { bitmap = ToFastBitmap(new Bitmap(file)); }
                catch { bitmap = null; }
            }

            if (bitmap == null)
            {
                string name = Path.GetFileName(resPath);
                if (!missing.Contains(name)) missing.Add(name);
            }

            sheets[resPath] = bitmap;
            return bitmap;
        }

        // resPath is the path inside the Godot project, e.g. "fighter_all_sets/young/Duck.png" or just "Idle.png".
        private static string? ResolvePath(string resPath)
        {
            string baseDir = AppContext.BaseDirectory;
            string projectDir = ProjectDir();
            string relative = resPath.Replace('/', Path.DirectorySeparatorChar);
            string fileName = Path.GetFileName(resPath);
            bool rootFile = !resPath.Contains('/');

            List<string> candidates = new List<string>
            {
                Path.Combine(projectDir, relative),
                Path.Combine(baseDir, relative),
                Path.Combine(projectDir, "Assets", relative),
                Path.Combine(baseDir, "Assets", relative)
            };
            if (rootFile)
            {
                // The scene keeps the young fighter's pictures at the Godot project root.
                candidates.Add(Path.Combine(projectDir, "fighter_all_sets", "young", fileName));
                candidates.Add(Path.Combine(baseDir, "fighter_all_sets", "young", fileName));
            }
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate)) return candidate;
            }

            if (projectPngs == null)
            {
                try { projectPngs = Directory.GetFiles(projectDir, "*.png", SearchOption.AllDirectories).ToList(); }
                catch { projectPngs = new List<string>(); }
            }

            // Same folder ending anywhere in the project
            if (!rootFile)
            {
                foreach (string p in projectPngs)
                {
                    if (p.Replace('\\', '/').EndsWith("/" + resPath, StringComparison.OrdinalIgnoreCase)) return p;
                }
            }

            // Young fighter pictures: same file name anywhere under a "young" folder (never guess another age).
            if (rootFile || resPath.Contains("/young/"))
            {
                string? inYoung = null;
                foreach (string p in projectPngs)
                {
                    string normalized = p.Replace('\\', '/');
                    if (!string.Equals(Path.GetFileName(p), fileName, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!normalized.Contains("/young/")) continue;
                    if (!normalized.Contains("/shadow_mechanic/")) return p;
                    inYoung ??= p;
                }
                return inYoung;
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