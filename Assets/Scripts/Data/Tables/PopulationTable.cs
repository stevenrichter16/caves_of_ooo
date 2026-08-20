using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Data
{
    public class PopulationEntry
    {
        public string BlueprintName;
        public int Weight = 1;
        public int MinCount = 0;
        public int MaxCount = 1;
    }

    /// <summary>
    /// Data-driven encounter table for zone population.
    /// Mirrors Qud's PopulationManager tables: weighted entries
    /// with count ranges. Roll() produces a list of blueprint names to spawn.
    /// </summary>
    public class PopulationTable
    {
        public string Name;
        public List<PopulationEntry> Entries = new List<PopulationEntry>();

        /// <summary>
        /// Roll all entries: guaranteed MinCount spawns for each entry,
        /// plus weight-based chance for additional spawns up to MaxCount.
        /// </summary>
        public List<string> Roll(System.Random rng)
        {
            var result = new List<string>();
            int totalWeight = 0;
            foreach (var e in Entries) totalWeight += e.Weight;
            if (totalWeight == 0) return result;

            foreach (var entry in Entries)
            {
                // Always spawn MinCount
                int count = entry.MinCount;

                // Roll for additional spawns up to MaxCount
                if (entry.MaxCount > entry.MinCount)
                {
                    float chance = (float)entry.Weight / totalWeight;
                    if (rng.NextDouble() <= chance)
                        count += rng.Next(1, entry.MaxCount - entry.MinCount + 1);
                }

                for (int i = 0; i < count; i++)
                    result.Add(entry.BlueprintName);
            }
            return result;
        }

        // ── Biome Tier Lookup ──────────────────────────────────────────

        /// <summary>
        /// Get the appropriate population table for a biome and tier.
        /// </summary>
        public static PopulationTable GetBiomeTable(BiomeType biome, int tier)
        {
            // BIOME-OVERHAUL A5: tier 3 was silently aliased to tier 2,
            // making the far ring of the world map no harder than the
            // middle ring (verified: only a `tier >= 2` branch existed).
            if (tier >= 3)
            {
                switch (biome)
                {
                    case BiomeType.Cave: return CaveTier3();
                    case BiomeType.Stump: return CaveTier3();
                    case BiomeType.Desert: return DesertTier3();
                    case BiomeType.Beating: return BeatingTier3();
                    case BiomeType.Jungle: return JungleTier3();
                    case BiomeType.Grovelands: return JungleTier3();
                    case BiomeType.Sodden: return SoddenTier3();
                    case BiomeType.Spread: return SpreadTier3();
                    case BiomeType.Ruins: return RuinsTier3();
                    case BiomeType.Overwrit: return RuinsTier3();
                }
            }

            if (tier >= 2)
            {
                switch (biome)
                {
                    case BiomeType.Cave: return CaveTier2();
                    case BiomeType.Stump: return CaveTier2();
                    case BiomeType.Desert: return DesertTier2();
                    case BiomeType.Beating: return BeatingTier2();
                    case BiomeType.Jungle: return JungleTier2();
                    case BiomeType.Grovelands: return JungleTier2();
                    case BiomeType.Sodden: return SoddenTier2();
                    case BiomeType.Spread: return SpreadTier2();
                    case BiomeType.Ruins: return RuinsTier2();
                    case BiomeType.Overwrit: return RuinsTier2();
                }
            }

            switch (biome)
            {
                case BiomeType.Cave: return CaveTier1();
                case BiomeType.Stump: return CaveTier1();
                case BiomeType.Desert: return DesertTier1();
                case BiomeType.Beating: return BeatingTier1();
                case BiomeType.Jungle: return JungleTier1();
                case BiomeType.Grovelands: return JungleTier1();
                case BiomeType.Sodden: return SoddenTier1();
                case BiomeType.Spread: return SpreadTier1();
                case BiomeType.Ruins: return RuinsTier1();
                case BiomeType.Overwrit: return RuinsTier1();
                default: return CaveTier1();
            }
        }


        // ── The Spread (W1) ────────────────────────────────────────────
        //
        // The Spread borrowed the jungle's bestiary, which is the retrofit
        // the world overhaul exists to undo: settled river country was
        // spawning rotlings and giant spiders in somebody's barley.
        //
        // What makes the Spread ITSELF is that the danger here is not the
        // wilderness. It is the road, and the hedge, and whatever has come
        // down out of the hills because the fields are easier. So: plenty
        // of ordinary fauna, plenty to forage, and a thin, sharp thread of
        // human trouble that thickens with tier.

        public static PopulationTable SpreadTier1()
        {
            return new PopulationTable
            {
                Name = "SpreadTier1",
                Entries = new List<PopulationEntry>
                {
                    // Lived-in country: birds and strays before monsters.
                    new PopulationEntry { BlueprintName = "Magpie", Weight = 5, MinCount = 1, MaxCount = 4 },
                    new PopulationEntry { BlueprintName = "PetDog", Weight = 2, MinCount = 0, MaxCount = 2 },
                    // The hedge is where the snake is.
                    new PopulationEntry { BlueprintName = "Viper", Weight = 2, MinCount = 0, MaxCount = 2 },
                    // Roadside trouble — present, but thin at tier 1.
                    new PopulationEntry { BlueprintName = "Snapjaw", Weight = 2, MinCount = 0, MaxCount = 2 },
                    // Worked country feeds you.
                    new PopulationEntry { BlueprintName = "BerryBush", Weight = 4, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "Beehive", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "HollowStump", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Signpost", Weight = 2, MinCount = 0, MaxCount = 1 },
                    // Dropped tools rather than dropped weapons.
                    new PopulationEntry { BlueprintName = "Hatchet", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Cudgel", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherBoots", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        public static PopulationTable SpreadTier2()
        {
            return new PopulationTable
            {
                Name = "SpreadTier2",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "Magpie", Weight = 3, MinCount = 1, MaxCount = 3 },
                    // Further out, the road stops being safe.
                    new PopulationEntry { BlueprintName = "Snapjaw", Weight = 4, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "SnapjawScavenger", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Viper", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "GiantSpider", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "BerryBush", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Beehive", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "ShortSword", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Hatchet", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherCap", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherGloves", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        public static PopulationTable SpreadTier3()
        {
            return new PopulationTable
            {
                Name = "SpreadTier3",
                Entries = new List<PopulationEntry>
                {
                    // The far Spread: the fields thin out and what walks
                    // them is organised.
                    new PopulationEntry { BlueprintName = "SnapjawHunter", Weight = 4, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "Snapjaw", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "SnapjawScavenger", Weight = 2, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "GiantSpider", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Viper", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Magpie", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "BerryBush", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "LongSword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "ShortSword", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherArmor", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        // ── Tier 1 Tables ──────────────────────────────────────────────

        // ── The Beating (W2.2, Docs/FELLING-W1-W2-PLAN.md §7.6) ──
        //
        // The static roster only. The bestiary's indicator species
        // (Sari-Snake, Sky-Sari, and the Wardline that suppresses them)
        // are DELIBERATELY absent: "only when Urqu is active" is their
        // whole design, and shipping them as static spawns would falsify
        // it. They land with state-reactive spawning (design doc §7.6,
        // plan D3). What lives here now is what lives here always: the
        // baskers, the scorpions, the briar, and the road's cost.

        public static PopulationTable BeatingTier1()
        {
            return new PopulationTable
            {
                Name = "BeatingTier1",
                Entries = new List<PopulationEntry>
                {
                    // The pan by day: baskers and what eats them.
                    new PopulationEntry { BlueprintName = "SunStriker", Weight = 5, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "Scorpion", Weight = 3, MinCount = 0, MaxCount = 2 },
                    // Forage — the wasteland feeds you, sparingly.
                    new PopulationEntry { BlueprintName = "Saltbriar", Weight = 4, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "DryBrush", Weight = 3, MinCount = 1, MaxCount = 3 },
                    // The road's furniture and its cost.
                    new PopulationEntry { BlueprintName = "Signpost", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Bones", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "WaterTonic", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        public static PopulationTable BeatingTier2()
        {
            return new PopulationTable
            {
                Name = "BeatingTier2",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "SunStriker", Weight = 3, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Scorpion", Weight = 3, MinCount = 1, MaxCount = 2 },
                    // The glass fauna arrives.
                    new PopulationEntry { BlueprintName = "GlassScorpion", Weight = 3, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "BrittleHound", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Saltbriar", Weight = 3, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Bones", Weight = 2, MinCount = 0, MaxCount = 2 },
                }
            };
        }

        public static PopulationTable BeatingTier3()
        {
            return new PopulationTable
            {
                Name = "BeatingTier3",
                Entries = new List<PopulationEntry>
                {
                    // Deep pan: what hunts here is patient.
                    new PopulationEntry { BlueprintName = "DuneLurker", Weight = 3, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "BrittleHound", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "GlassScorpion", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "SunStriker", Weight = 2, MinCount = 0, MaxCount = 2 },
                    // Organised human trouble follows the caravans out.
                    new PopulationEntry { BlueprintName = "SnapjawHunter", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Saltbriar", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Bones", Weight = 3, MinCount = 1, MaxCount = 3 },
                }
            };
        }

        public static PopulationTable CaveTier1()
        {
            return new PopulationTable
            {
                Name = "CaveTier1",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "Snapjaw", Weight = 5, MinCount = 2, MaxCount = 5 },
                    new PopulationEntry { BlueprintName = "SnapjawScavenger", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "SnapjawHunter", Weight = 1, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "CaveBat", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "CaveSlime", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Glowmaw", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Dagger", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "LongSword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Stalagmite", Weight = 4, MinCount = 3, MaxCount = 8 },
                    // Round 5 — forageable interactable
                    new PopulationEntry { BlueprintName = "MushroomRing", Weight = 2, MinCount = 0, MaxCount = 2 },
                    // LOOT OVERHAUL SM7 — wider loose-gear pool.
                    new PopulationEntry { BlueprintName = "ShortSword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Cudgel", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Hatchet", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherCap", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherBoots", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherGloves", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        // ── The Sodden (W3.4) ─────────────────────────────────────────
        //
        // The bog's danger is patient. Nothing here chases far: the
        // frogs sit, the toads wait, the greatdew waits better. What
        // thickens with tier is not the crowd but the teeth — the deep
        // Sodden is MawToad and bandfrog country, and the greatdew grows
        // where nothing clears it. Forage is oil on legs (reedfrogs).
        // No human trouble by design: the road crews and peat-cutters
        // are Sumphold's people, and Sumphold is a POI, not wilderness.

        public static PopulationTable SoddenTier1()
        {
            return new PopulationTable
            {
                Name = "SoddenTier1",
                Entries = new List<PopulationEntry>
                {
                    // The bog's ordinary voices.
                    new PopulationEntry { BlueprintName = "Reedfrog", Weight = 5, MinCount = 1, MaxCount = 4 },
                    new PopulationEntry { BlueprintName = "GinFrog", Weight = 3, MinCount = 0, MaxCount = 2 },
                    // The lesson is the skin.
                    new PopulationEntry { BlueprintName = "Bandfrog", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Viper", Weight = 2, MinCount = 0, MaxCount = 1 },
                    // Things that wait.
                    new PopulationEntry { BlueprintName = "Greatdew", Weight = 2, MinCount = 0, MaxCount = 2 },
                }
            };
        }

        public static PopulationTable SoddenTier2()
        {
            return new PopulationTable
            {
                Name = "SoddenTier2",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "Reedfrog", Weight = 4, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "GinFrog", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Bandfrog", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Viper", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "MawToad", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Greatdew", Weight = 3, MinCount = 1, MaxCount = 2 },
                }
            };
        }

        public static PopulationTable SoddenTier3()
        {
            return new PopulationTable
            {
                Name = "SoddenTier3",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "Reedfrog", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Bandfrog", Weight = 4, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "MawToad", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Viper", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Greatdew", Weight = 4, MinCount = 1, MaxCount = 3 },
                }
            };
        }

        public static PopulationTable DesertTier1()
        {
            return new PopulationTable
            {
                Name = "DesertTier1",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "Snapjaw", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "SnapjawScavenger", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Scorpion", Weight = 4, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "DesertBandit", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Rock", Weight = 4, MinCount = 2, MaxCount = 6 },
                    // LOOT OVERHAUL SM7 — wider loose-gear pool.
                    new PopulationEntry { BlueprintName = "ShortSword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Cudgel", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Hatchet", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherCap", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherBoots", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherGloves", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        public static PopulationTable JungleTier1()
        {
            return new PopulationTable
            {
                Name = "JungleTier1",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "Snapjaw", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "SnapjawHunter", Weight = 2, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "GiantSpider", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "Viper", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Glowmaw", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Dagger", Weight = 2, MinCount = 1, MaxCount = 2 },
                    // Round 5 — forageable interactables
                    new PopulationEntry { BlueprintName = "BerryBush", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "Beehive", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "HollowStump", Weight = 1, MinCount = 0, MaxCount = 1 },
                    // LOOT OVERHAUL SM7 — wider loose-gear pool.
                    new PopulationEntry { BlueprintName = "ShortSword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Cudgel", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Hatchet", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherCap", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherBoots", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherGloves", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        public static PopulationTable RuinsTier1()
        {
            return new PopulationTable
            {
                Name = "RuinsTier1",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "SnapjawScavenger", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "SnapjawHunter", Weight = 2, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "RuinScavenger", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "LongSword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherArmor", Weight = 1, MinCount = 0, MaxCount = 1 },
                    // Round 5 — old road markers
                    new PopulationEntry { BlueprintName = "Signpost", Weight = 1, MinCount = 0, MaxCount = 2 },
                    // LOOT OVERHAUL SM7 — wider loose-gear pool.
                    new PopulationEntry { BlueprintName = "ShortSword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Cudgel", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Hatchet", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherCap", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherBoots", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherGloves", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        // ── Tier 2 Tables ──────────────────────────────────────────────

        public static PopulationTable CaveTier2()
        {
            return new PopulationTable
            {
                Name = "CaveTier2",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "CaveBear", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "SnapjawHunter", Weight = 4, MinCount = 2, MaxCount = 4 },
                    new PopulationEntry { BlueprintName = "CaveSlime", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "CaveBat", Weight = 2, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "Glowmaw", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "LongSword", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherArmor", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Stalagmite", Weight = 3, MinCount = 2, MaxCount = 6 },
                    // LOOT OVERHAUL SM7 — wider loose-gear pool.
                    new PopulationEntry { BlueprintName = "Mace", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Spear", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Battleaxe", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Buckler", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "IronHelmet", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Cloak", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        public static PopulationTable DesertTier2()
        {
            return new PopulationTable
            {
                Name = "DesertTier2",
                Entries = new List<PopulationEntry>
                {
                    // Round 5 — buried-mechanic surfacing: heal-over-time spring
                    new PopulationEntry { BlueprintName = "ConvalescencePool", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "SandWurm", Weight = 2, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "DesertBandit", Weight = 4, MinCount = 2, MaxCount = 4 },
                    new PopulationEntry { BlueprintName = "Scorpion", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "LongSword", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Rock", Weight = 3, MinCount = 1, MaxCount = 4 },
                    // LOOT OVERHAUL SM7 — wider loose-gear pool.
                    new PopulationEntry { BlueprintName = "Mace", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Spear", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Battleaxe", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Buckler", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "IronHelmet", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Cloak", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        public static PopulationTable JungleTier2()
        {
            return new PopulationTable
            {
                Name = "JungleTier2",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "JungleApe", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "GiantSpider", Weight = 3, MinCount = 2, MaxCount = 4 },
                    new PopulationEntry { BlueprintName = "Viper", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "Glowmaw", Weight = 2, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "SnapjawHunter", Weight = 2, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Dagger", Weight = 2, MinCount = 0, MaxCount = 2 },
                    // LOOT OVERHAUL SM7 — wider loose-gear pool.
                    new PopulationEntry { BlueprintName = "Mace", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Spear", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Battleaxe", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Buckler", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "IronHelmet", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Cloak", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        public static PopulationTable RuinsTier2()
        {
            return new PopulationTable
            {
                Name = "RuinsTier2",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "SkeletalSentry", Weight = 3, MinCount = 2, MaxCount = 4 },
                    new PopulationEntry { BlueprintName = "StoneGolem", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "RuinScavenger", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "LongSword", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LeatherArmor", Weight = 2, MinCount = 0, MaxCount = 1 },
                    // LOOT OVERHAUL SM7 — wider loose-gear pool.
                    new PopulationEntry { BlueprintName = "Mace", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Spear", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Battleaxe", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Buckler", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "IronHelmet", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Cloak", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        // ── Tier 3 Tables (BIOME-OVERHAUL A5) ──────────────────────────
        // The far ring (Manhattan dist > 8). Hostile backbone comes from
        // Beasts/Snapjaws-faction bruisers; the faction-tagged mutants
        // (GlassScorpion/SporeShambler/BrassHusk/PalimpsestEcho/
        // ChoirTendril) spawn as ECOLOGY — their factions start at rep 0,
        // so they are neutral until provoked, same as the Elemental
        // Crossroads set pieces. Attacking them is a player choice with
        // rep consequences, not a free kill.

        public static PopulationTable CaveTier3()
        {
            return new PopulationTable
            {
                Name = "CaveTier3",
                Entries = new List<PopulationEntry>
                {
                    // Round 5 — buried-mechanic surfacing: heal-over-time spring
                    new PopulationEntry { BlueprintName = "ConvalescencePool", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "CaveBear", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "SnapjawHunter", Weight = 4, MinCount = 2, MaxCount = 4 },
                    new PopulationEntry { BlueprintName = "Glowmaw", Weight = 3, MinCount = 1, MaxCount = 3 },
                    // Phase C: the far ring's leadership and its grazers.
                    new PopulationEntry { BlueprintName = "SnapjawWarlord", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Mosshulk", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "CaveSlime", Weight = 2, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Snapjaw", Weight = 3, MinCount = 2, MaxCount = 4 },
                    new PopulationEntry { BlueprintName = "LongSword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "ChainMail", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Stalagmite", Weight = 3, MinCount = 2, MaxCount = 6 },
                    // LOOT OVERHAUL SM7 — wider loose-gear pool.
                    new PopulationEntry { BlueprintName = "Greatsword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Claymore", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Warhammer", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "IronBuckler", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "IronshodBoots", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "WardedCloak", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        public static PopulationTable DesertTier3()
        {
            return new PopulationTable
            {
                Name = "DesertTier3",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "SandWurm", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "DesertBandit", Weight = 4, MinCount = 2, MaxCount = 4 },
                    new PopulationEntry { BlueprintName = "Scorpion", Weight = 2, MinCount = 1, MaxCount = 2 },
                    // Phase D: the buried bruiser and the glass packs.
                    new PopulationEntry { BlueprintName = "DuneLurker", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "BrittleHound", Weight = 3, MinCount = 0, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "GlassScorpion", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "LongSword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Rock", Weight = 2, MinCount = 1, MaxCount = 4 },
                    // LOOT OVERHAUL SM7 — wider loose-gear pool.
                    new PopulationEntry { BlueprintName = "Greatsword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Claymore", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Warhammer", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "IronBuckler", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "IronshodBoots", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "WardedCloak", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        public static PopulationTable JungleTier3()
        {
            return new PopulationTable
            {
                Name = "JungleTier3",
                Entries = new List<PopulationEntry>
                {
                    // Round 6 — buried-mechanic surfacing: exotic liquid
                    new PopulationEntry { BlueprintName = "MirrorMucilagePool", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "JungleApe", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "GiantSpider", Weight = 3, MinCount = 2, MaxCount = 4 },
                    new PopulationEntry { BlueprintName = "Viper", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "SporeShambler", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "ChoirTendril", Weight = 1, MinCount = 0, MaxCount = 1 },
                    // Phase E: swarms below, stranglers above.
                    new PopulationEntry { BlueprintName = "Rotling", Weight = 3, MinCount = 0, MaxCount = 4 },
                    new PopulationEntry { BlueprintName = "CanopyStrangler", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Dagger", Weight = 1, MinCount = 0, MaxCount = 2 },
                    // LOOT OVERHAUL SM7 — wider loose-gear pool.
                    new PopulationEntry { BlueprintName = "Greatsword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Claymore", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Warhammer", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "IronBuckler", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "IronshodBoots", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "WardedCloak", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        public static PopulationTable RuinsTier3()
        {
            return new PopulationTable
            {
                Name = "RuinsTier3",
                Entries = new List<PopulationEntry>
                {
                    // Round 6 — buried-mechanic surfacing: exotic liquid
                    new PopulationEntry { BlueprintName = "MemoryBathPool", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "SkeletalSentry", Weight = 4, MinCount = 2, MaxCount = 4 },
                    new PopulationEntry { BlueprintName = "StoneGolem", Weight = 2, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "CharredHusk", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "BrassHusk", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "PalimpsestEcho", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "LongSword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "ChainMail", Weight = 1, MinCount = 0, MaxCount = 1 },
                    // LOOT OVERHAUL SM7 — wider loose-gear pool.
                    new PopulationEntry { BlueprintName = "Greatsword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Claymore", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Warhammer", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "IronBuckler", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "IronshodBoots", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "WardedCloak", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        // ── Specialized Tables ─────────────────────────────────────────

        /// <summary>
        /// Lair guard population based on biome. Used by LairPopulationBuilder.
        /// </summary>
        public static PopulationTable LairGuards(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Cave:
                    return new PopulationTable
                    {
                        Name = "LairGuards_Cave",
                        Entries = new List<PopulationEntry>
                        {
                            new PopulationEntry { BlueprintName = "SnapjawHunter", Weight = 4, MinCount = 2, MaxCount = 4 },
                            new PopulationEntry { BlueprintName = "CaveBear", Weight = 2, MinCount = 0, MaxCount = 1 },
                            new PopulationEntry { BlueprintName = "CaveBat", Weight = 3, MinCount = 1, MaxCount = 3 },
                            new PopulationEntry { BlueprintName = "Glowmaw", Weight = 2, MinCount = 0, MaxCount = 2 },
                        }
                    };
                case BiomeType.Desert:
                    return new PopulationTable
                    {
                        Name = "LairGuards_Desert",
                        Entries = new List<PopulationEntry>
                        {
                            new PopulationEntry { BlueprintName = "DesertBandit", Weight = 4, MinCount = 2, MaxCount = 4 },
                            new PopulationEntry { BlueprintName = "Scorpion", Weight = 3, MinCount = 1, MaxCount = 3 },
                            new PopulationEntry { BlueprintName = "SandWurm", Weight = 1, MinCount = 0, MaxCount = 1 },
                        }
                    };
                case BiomeType.Jungle:
                    return new PopulationTable
                    {
                        Name = "LairGuards_Jungle",
                        Entries = new List<PopulationEntry>
                        {
                            new PopulationEntry { BlueprintName = "GiantSpider", Weight = 4, MinCount = 2, MaxCount = 4 },
                            new PopulationEntry { BlueprintName = "JungleApe", Weight = 2, MinCount = 1, MaxCount = 2 },
                            new PopulationEntry { BlueprintName = "Viper", Weight = 3, MinCount = 1, MaxCount = 3 },
                            new PopulationEntry { BlueprintName = "Glowmaw", Weight = 2, MinCount = 0, MaxCount = 1 },
                        }
                    };
                case BiomeType.Ruins:
                    return new PopulationTable
                    {
                        Name = "LairGuards_Ruins",
                        Entries = new List<PopulationEntry>
                        {
                            new PopulationEntry { BlueprintName = "SkeletalSentry", Weight = 4, MinCount = 2, MaxCount = 4 },
                            new PopulationEntry { BlueprintName = "StoneGolem", Weight = 1, MinCount = 0, MaxCount = 1 },
                            new PopulationEntry { BlueprintName = "RuinScavenger", Weight = 3, MinCount = 1, MaxCount = 2 },
                        }
                    };
                // Felling W0.6 — the canon biomes borrow their nearest
                // legacy roster until their own phase authors one.
                case BiomeType.Spread:     return LairGuards(BiomeType.Jungle);
                case BiomeType.Sodden:
                    // W3.4 — the bog guards its own.
                    return new PopulationTable
                    {
                        Name = "SoddenLairGuards",
                        Entries = new List<PopulationEntry>
                        {
                            new PopulationEntry { BlueprintName = "MawToad", Weight = 3, MinCount = 1, MaxCount = 2 },
                            new PopulationEntry { BlueprintName = "Bandfrog", Weight = 3, MinCount = 1, MaxCount = 3 },
                            new PopulationEntry { BlueprintName = "Greatdew", Weight = 2, MinCount = 0, MaxCount = 2 },
                        }
                    };
                case BiomeType.Beating:
                    // W2.2 — the wasteland's own guards.
                    return new PopulationTable
                    {
                        Name = "BeatingLairGuards",
                        Entries = new List<PopulationEntry>
                        {
                            new PopulationEntry { BlueprintName = "BrittleHound", Weight = 3, MinCount = 1, MaxCount = 2 },
                            new PopulationEntry { BlueprintName = "GlassScorpion", Weight = 3, MinCount = 1, MaxCount = 3 },
                            new PopulationEntry { BlueprintName = "DuneLurker", Weight = 2, MinCount = 0, MaxCount = 1 },
                        }
                    };
                case BiomeType.Grovelands: return LairGuards(BiomeType.Jungle);
                case BiomeType.Overwrit:   return LairGuards(BiomeType.Ruins);
                case BiomeType.Stump:      return LairGuards(BiomeType.Cave);
                default:
                    return LairGuards(BiomeType.Cave);
            }
        }

        /// <summary>
        /// Village decoration items (non-creature objects placed in village zones).
        /// </summary>
        public static PopulationTable VillageDecor()
        {
            return new PopulationTable
            {
                Name = "VillageDecor",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "Campfire", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Well", Weight = 2, MinCount = 1, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "MarketStall", Weight = 2, MinCount = 0, MaxCount = 2 },
                }
            };
        }

        // ── Underground ────────────────────────────────────────────────

        /// <summary>
        /// Underground population scaled by depth.
        /// Deeper = more and tougher enemies, fewer friendlies.
        /// </summary>
        public static PopulationTable UndergroundTier(int depth)
        {
            int tier = depth <= 0 ? 1 : System.Math.Min(depth / 3 + 1, 8);

            // Scale enemy counts with tier
            int snapMin = 1 + tier;
            int snapMax = 3 + tier;
            int scavMin = tier;
            int scavMax = 1 + tier;
            int huntMin = System.Math.Max(0, tier - 1);
            int huntMax = tier;

            var table = new PopulationTable
            {
                Name = $"Underground_Depth{depth}",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "Snapjaw", Weight = 5, MinCount = snapMin, MaxCount = snapMax },
                    new PopulationEntry { BlueprintName = "SnapjawScavenger", Weight = 3, MinCount = scavMin, MaxCount = scavMax },
                    new PopulationEntry { BlueprintName = "SnapjawHunter", Weight = 2, MinCount = huntMin, MaxCount = huntMax },
                    new PopulationEntry { BlueprintName = "Stalagmite", Weight = 3, MinCount = 2, MaxCount = 6 },
                    new PopulationEntry { BlueprintName = "Glowmaw", Weight = 2, MinCount = 0, MaxCount = 1 + tier / 2 },
                }
            };

            // Only add loot/weapons at certain depths
            if (tier >= 2)
            {
                table.Entries.Add(new PopulationEntry { BlueprintName = "LongSword", Weight = 1, MinCount = 0, MaxCount = 1 });
            }
            if (tier >= 3)
            {
                table.Entries.Add(new PopulationEntry { BlueprintName = "LeatherArmor", Weight = 1, MinCount = 0, MaxCount = 1 });
            }

            // BIOME-OVERHAUL G: depth scales VARIETY, not just count.
            // Limestone brings bears and rot; the shale band brings the
            // burned and the pale; quartzite brings golems and the brute.
            if (tier >= 2)
            {
                table.Entries.Add(new PopulationEntry { BlueprintName = "CaveBear", Weight = 2, MinCount = 0, MaxCount = 1 });
                table.Entries.Add(new PopulationEntry { BlueprintName = "Rotling", Weight = 2, MinCount = 0, MaxCount = 2 });
            }
            if (tier >= 3)
            {
                table.Entries.Add(new PopulationEntry { BlueprintName = "SkeletalSentry", Weight = 2, MinCount = 0, MaxCount = 2 });
                table.Entries.Add(new PopulationEntry { BlueprintName = "CharredHusk", Weight = 2, MinCount = 0, MaxCount = 1 });
                table.Entries.Add(new PopulationEntry { BlueprintName = "PaleStalker", Weight = 2, MinCount = 0, MaxCount = 1 });
            }
            if (tier >= 4)
            {
                table.Entries.Add(new PopulationEntry { BlueprintName = "StoneGolem", Weight = 1, MinCount = 0, MaxCount = 1 });
                table.Entries.Add(new PopulationEntry { BlueprintName = "ObsidianBrute", Weight = 1, MinCount = 0, MaxCount = 1 });
            }

            // BIOME-OVERHAUL A3: harvestable mineral veins by strata band
            // (quartz from the first shaft, salt in the limestone band,
            // choir iron from shale down). The only spawn source for the
            // three tinker-infusion minerals.
            table.Entries.Add(new PopulationEntry { BlueprintName = "GlowQuartzVein", Weight = 1, MinCount = 0, MaxCount = 1 });
            if (tier >= 2)
            {
                table.Entries.Add(new PopulationEntry { BlueprintName = "PaleSaltVein", Weight = 1, MinCount = 0, MaxCount = 1 });
            }
            if (tier >= 3)
            {
                table.Entries.Add(new PopulationEntry { BlueprintName = "ChoirIronVein", Weight = 1, MinCount = 0, MaxCount = 1 });
            }

            return table;
        }
    }
}
