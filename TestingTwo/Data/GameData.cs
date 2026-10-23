using System.Drawing;

namespace Gui
{
    /// <summary>
    /// All fixed game content: fighters, arenas, skills, armor, and skins.
    /// </summary>
    internal static class GameData
    {
        // --- CHARACTER ROSTER ---
        public static readonly FighterInfo[] Roster = new FighterInfo[]
        {
            new FighterInfo { Name = "Insu KAZAMA", Style = "Traditional Karate", PrimaryColor = Color.FromArgb(0, 150, 255), AuraColor = Color.Cyan, SpecialSkill = "Kazama Electric Fist" },
            new FighterInfo { Name = "Andre MISHIMA", Style = "Mishima Style Karate", PrimaryColor = Color.FromArgb(220, 20, 60), AuraColor = Color.MediumPurple, SpecialSkill = "Devil Laser Beam" },
            new FighterInfo { Name = "Justin Phoenix", Style = "Judo & Brawling", PrimaryColor = Color.FromArgb(255, 185, 0), AuraColor = Color.OrangeRed, SpecialSkill = "Phoenix Smasher" },
            new FighterInfo { Name = "Jayvee Hworang", Style = "Tae Kwon Do", PrimaryColor = Color.FromArgb(0, 230, 120), AuraColor = Color.Lime, SpecialSkill = "Hunting Hawk Kick" }
        };

        // --- ARENAS ---
        public static readonly ArenaInfo[] Arenas = new ArenaInfo[]
        {
            new ArenaInfo {
                Name = "VOLCANIC INFERNO",
                Subtitle = "Active Crater & Molten Lava Surface",
                SkyTop = Color.FromArgb(35, 5, 5),
                SkyBottom = Color.FromArgb(140, 25, 0),
                FloorColor = Color.FromArgb(28, 12, 10),
                RingNeon = Color.FromArgb(255, 80, 0),
                ParticleColor = Color.OrangeRed
            },
            new ArenaInfo {
                Name = "MISHIMA DOJO",
                Subtitle = "Ancient Pagoda Temple & Mystic Fog",
                SkyTop = Color.FromArgb(18, 14, 25),
                SkyBottom = Color.FromArgb(60, 45, 30),
                FloorColor = Color.FromArgb(42, 28, 16),
                RingNeon = Color.Gold,
                ParticleColor = Color.FromArgb(255, 215, 120)
            },
            new ArenaInfo {
                Name = "CYBER HIGHWAY",
                Subtitle = "Futuristic Skyscrapers & Neon Lights",
                SkyTop = Color.FromArgb(5, 5, 20),
                SkyBottom = Color.FromArgb(25, 15, 55),
                FloorColor = Color.FromArgb(14, 16, 26),
                RingNeon = Color.Cyan,
                ParticleColor = Color.DeepSkyBlue
            },
            new ArenaInfo {
                Name = "YAKUSHIMA FOREST",
                Subtitle = "Deep Moonlit Wilderness & Sacred Pines",
                SkyTop = Color.FromArgb(5, 18, 15),
                SkyBottom = Color.FromArgb(15, 45, 30),
                FloorColor = Color.FromArgb(16, 28, 18),
                RingNeon = Color.LimeGreen,
                ParticleColor = Color.PaleGreen
            },
            new ArenaInfo {
                Name = "ELEVATOR LOBBY",
                Subtitle = "Polished Marble Hall & Gleaming Elevators",
                SkyTop = Color.FromArgb(165, 145, 115),
                SkyBottom = Color.FromArgb(235, 215, 170),
                FloorColor = Color.FromArgb(205, 175, 125),
                RingNeon = Color.FromArgb(70, 160, 255),
                ParticleColor = Color.FromArgb(255, 240, 190),
                BackgroundFile = "elevator_lobby.png"
            }
        };

        // --- SKILLS ---
        public static readonly SkillInfo[] SkillCatalog = new SkillInfo[]
        {
            new SkillInfo { Name = "Quick Jab", Type = SkillType.Starter, Damage = 7, Reach = 140, Action = 1, Price = 0,
                Description = "A fast straight punch. A reliable combo opener." },
            new SkillInfo { Name = "Dash Strike", Type = SkillType.Starter, Damage = 9, Reach = 200, Lunge = 16, Action = 1, Price = 100,
                Description = "Lunges forward to close the gap, then hits." },
            new SkillInfo { Name = "Rising Launcher", Type = SkillType.Extender, Damage = 12, Launch = -18, Reach = 140, Action = 2, Price = 0,
                Description = "Pops the opponent into the air to extend a combo." },
            new SkillInfo { Name = "Air Juggle Kick", Type = SkillType.Extender, Damage = 10, Launch = -24, Reach = 150, Action = 2, Price = 120,
                Description = "A higher launch built for long juggles." },
            new SkillInfo { Name = "Rage Art", Type = SkillType.Finisher, Damage = 26, Launch = -10, Reach = 220, HeatCost = 50, Knockback = 26, ActionFrames = 20, Action = 3, Price = 0,
                Description = "A heat-powered finisher with heavy knockback." },
            new SkillInfo { Name = "Meteor Smash", Type = SkillType.Finisher, Damage = 34, Launch = -6, Reach = 200, HeatCost = 70, Knockback = 30, ActionFrames = 22, Action = 3, Price = 220,
                Description = "A devastating finisher. Needs a full heat gauge." },
            new SkillInfo { Name = "Shock Trap", Type = SkillType.Utility, Damage = 5, Reach = 260, HeatCost = 20, Knockback = 4, StunFrames = 36, ActionFrames = 16, Action = 3, Price = 130,
                Description = "A long-range shockwave that stuns the target for a long time." },
            new SkillInfo { Name = "Guard Break", Type = SkillType.Utility, Damage = 6, Reach = 140, Knockback = 8, StunFrames = 26, HeatDrain = 25, Action = 1, Price = 110,
                Description = "Stuns the target and drains 25 of their heat." }
        };

        // --- ARMOR ---
        public static readonly ArmorInfo[] ArmorCatalog = new ArmorInfo[]
        {
            new ArmorInfo { Name = "No Armor", DamageReduction = 0, Price = 0, Description = "No protection. Maximum mobility." },
            new ArmorInfo { Name = "Leather Wraps", DamageReduction = 1, Price = 80, Description = "Light padding that shaves a little off every hit." },
            new ArmorInfo { Name = "Steel Plating", DamageReduction = 2, Price = 160, Description = "Solid plating that blunts incoming attacks." },
            new ArmorInfo { Name = "Titan Plate", DamageReduction = 4, Price = 280, Description = "Heavy armor. Weak attacks barely scratch you." }
        };

        // --- BATTLE SKINS ---
        public static readonly SkinInfo[] SkinCatalog = new SkinInfo[]
        {
            new SkinInfo { Name = "Standard", Price = 0,
                Description = "No bonuses and no penalties." },
            new SkinInfo { Name = "Vanguard Skin", ArmorEffectiveness = 1.25f, DashSpeed = 0.85f, Price = 150,
                Description = "+25% armor damage reduction, -15% dash speed." },
            new SkinInfo { Name = "Berserker Skin", HeatGain = 1.30f, HealthModifier = -GameConfig.HeartHealth, Price = 150,
                Description = "+30% faster ult gauge gain, -1 heart of base health." },
            new SkinInfo { Name = "Duelist Skin", BonusComboCharge = 1, DamageTaken = 1.15f, Price = 150,
                Description = "+1 combo charge per completed combo, +15% damage taken." }
        };
    }
}