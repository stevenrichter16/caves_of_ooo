using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Data
{
    public class PopulationEntry
    {
        public string BlueprintName;
        public int Weight = 1;
        public int MinCount = 0;
        public int MaxCount = 1;

        /// <summary>Optional named pick-one group within this table. Exactly
        /// one eligible positive-weight row is selected per group, then its
        /// inclusive MinCount..MaxCount is rolled. Ungrouped rows retain the
        /// independent optional-roll semantics. Groups do not dilute ambient
        /// probabilities, and world-state gates filter before selection.</summary>
        public string EncounterGroup;

        /// <summary>§7.6 — spawn only while this world flag is SET.
        /// Canon: "roughly a third of the bestiary exists to indicate a
        /// band or a state", and "static tables kill the design". The
        /// Sari-Snake is the shipped user: Urqu's signs in flesh, which
        /// should not exist while Urqu is quiet.
        ///
        /// <para><b>Fails closed.</b> With no world state to read
        /// (worldgen before the narrative part exists, or a test with
        /// no fixture) a REQUIRED flag counts as clear — never spawn
        /// state-gated content into a world that has no such
        /// state.</para></summary>
        public string RequiresWorldFlag;

        /// <summary>§7.6 — spawn only while this world flag is CLEAR.
        /// The Cascade-Father is the shipped user: an indicator whose
        /// ABSENCE is the alarm ("a village whose nearby cascade no
        /// longer hosts Cascade-Fathers is a village in ecological
        /// trouble"). Fails OPEN, mirroring Requires: no world state
        /// means nothing has gone wrong yet.</summary>
        public string ForbidsWorldFlag;

        /// <summary>Does the world currently allow this entry? Null
        /// predicates always pass, so every shipped table is
        /// unaffected.</summary>
        public bool AllowedByWorldState()
        {
            var state = NarrativeStatePart.Current;
            if (!string.IsNullOrEmpty(RequiresWorldFlag))
            {
                if (state == null) return false;              // fail closed
                if (state.GetFact(RequiresWorldFlag) == 0) return false;
            }
            if (!string.IsNullOrEmpty(ForbidsWorldFlag))
            {
                if (state != null && state.GetFact(ForbidsWorldFlag) != 0)
                    return false;
            }
            return true;
        }
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
        /// Roll independent ambient entries and each named encounter group.
        /// Invalid rows (blank blueprint, nonpositive weight, negative or
        /// inverted count range) are excluded. Diagnostics report every row,
        /// including world-gated and unselected alternatives; zoneID is optional
        /// for direct table callers and supplied by world-generation builders.
        /// </summary>
        public List<string> Roll(System.Random rng, string zoneID = null)
        {
            if (rng == null) throw new System.ArgumentNullException(nameof(rng));
            var result = new List<string>();
            if (Entries == null) return result;
            var eligible = new bool[Entries.Count];
            long ambientWeight = 0;
            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                if (!Valid(entry))
                    RecordRoll(entry, zoneID, 0, -1, null, "invalid_entry", "not_rolled");
                else if (!entry.AllowedByWorldState())
                    RecordRoll(entry, zoneID, 0, -1, null, "world_flag", "not_rolled");
                else
                {
                    eligible[i] = true;
                    if (string.IsNullOrEmpty(entry.EncounterGroup)) ambientWeight += entry.Weight;
                }
            }

            var rolledGroups = new HashSet<string>();
            for (int i = 0; i < Entries.Count; i++)
            {
                if (!eligible[i]) continue;
                var entry = Entries[i];
                if (!string.IsNullOrEmpty(entry.EncounterGroup))
                {
                    if (!rolledGroups.Add(entry.EncounterGroup)) continue;
                    long total = 0;
                    for (int j = 0; j < Entries.Count; j++)
                        if (eligible[j] && Entries[j].EncounterGroup == entry.EncounterGroup)
                            total += Entries[j].Weight;
                    double roll = rng.NextDouble();
                    double choice = roll * total;
                    long cumulative = 0;
                    int selected = -1;
                    for (int j = 0; j < Entries.Count; j++)
                    {
                        if (!eligible[j] || Entries[j].EncounterGroup != entry.EncounterGroup) continue;
                        cumulative += Entries[j].Weight;
                        if (selected < 0 && choice < cumulative) selected = j;
                    }
                    cumulative = 0;
                    for (int j = 0; j < Entries.Count; j++)
                    {
                        var candidate = Entries[j];
                        if (!eligible[j] || candidate.EncounterGroup != entry.EncounterGroup) continue;
                        int count = j == selected ? RollCount(rng, candidate.MinCount, candidate.MaxCount) : 0;
                        for (int n = 0; n < count; n++) result.Add(candidate.BlueprintName);
                        double selectionStart = (double)cumulative / total;
                        cumulative += candidate.Weight;
                        RecordRoll(candidate, zoneID, count, roll, null,
                            j == selected ? "selected" : "not_selected", "weighted_choice",
                            selectionStart, (double)cumulative / total);
                    }
                    continue;
                }

                int ambientCount = entry.MinCount;
                double optionalRoll = -1;
                double chance = (double)entry.Weight / ambientWeight;
                if (entry.MaxCount > entry.MinCount)
                {
                    optionalRoll = rng.NextDouble();
                    if (optionalRoll < chance)
                        ambientCount += RollCount(rng, 1, entry.MaxCount - entry.MinCount);
                }
                for (int n = 0; n < ambientCount; n++) result.Add(entry.BlueprintName);
                RecordRoll(entry, zoneID, ambientCount, optionalRoll,
                    optionalRoll < 0 ? (double?)null : chance,
                    ambientCount > 0 ? "selected" : "roll_missed",
                    optionalRoll < 0 ? "fixed_count" : "chance");
            }
            return result;
        }

        private static bool Valid(PopulationEntry entry) => entry != null
            && !string.IsNullOrWhiteSpace(entry.BlueprintName) && entry.Weight > 0
            && entry.MinCount >= 0 && entry.MaxCount >= entry.MinCount;

        private static int RollCount(System.Random rng, int min, int max)
        {
            if (min == max) return min;
            // Avoid max + 1 overflow while retaining normal Random.Next semantics.
            if (max == int.MaxValue)
                return min + (int)(rng.NextDouble() * ((long)max - min + 1));
            return rng.Next(min, max + 1);
        }

        // A weighted choice occupies a half-open interval of the group draw;
        // its individual probability mass is not a threshold against that draw.
        // Only an independent chance roll has a scalar threshold. -1 marks no RNG draw.
        private void RecordRoll(PopulationEntry entry, string zoneID, int count,
            double roll, double? threshold, string reason, string rollKind,
            double? selectionStart = null, double? selectionEnd = null)
        {
            if (!Diag.IsChannelEnabled("worldgen")) return;
            Diag.Record("worldgen", "PopulationRolled", payload: new
            {
                table = Name, zone = zoneID, blueprint = entry?.BlueprintName,
                group = entry?.EncounterGroup, count, roll, rollKind, threshold,
                selectionStart, selectionEnd, reason
            });
        }

        // ── The Stump, by elevation band (W6.3) ────────────────────────

        /// <summary>
        /// W6.3 — the tepui's fauna, sited by band. Canon puts a third
        /// of the bestiary to work indicating a band or a state
        /// (FELLING-WORLD-DESIGN §3.6), and until now the Stump
        /// borrowed the CAVE tables outright: the god-tree's stump was
        /// populated by marlbacks.
        ///
        /// <para><c>StumpBand.None</c> (a Stump-biome cell off the
        /// authored band map, or a pipeline built without coordinates)
        /// falls back to the foothills — the mountain's most ordinary
        /// ground — rather than back to the cave.</para>
        /// </summary>
        public static PopulationTable GetStumpTable(StumpBand band, int tier)
        {
            switch (band)
            {
                case StumpBand.Summit:  return StumpSummit(tier);
                case StumpBand.Slopes:  return StumpSlopes(tier);
                default:                return StumpFoothills(tier);
            }
        }

        /// <summary>Foothills: blackwater creeks and spray zones — the
        /// biodiversity hotspot, and the gentlest band.</summary>
        private static PopulationTable StumpFoothills(int tier)
        {
            var t = new PopulationTable { Name = $"StumpFoothills{tier}" };
            // The cascade's indicator. Its ABSENCE is the alarm, so it
            // is gated on the ecology flag rather than on danger.
            t.Entries.Add(new PopulationEntry
            {
                BlueprintName = "CascadeFather", Weight = 4, MinCount = 1, MaxCount = 3,
                ForbidsWorldFlag = "EcologyDamaged",
            });
            t.Entries.Add(new PopulationEntry
            { BlueprintName = "GlasspaneFrog", Weight = 3, MinCount = 1, MaxCount = 2 });
            t.Entries.Add(new PopulationEntry
            { BlueprintName = "YellowfootWayfarer", Weight = 2, MinCount = 0, MaxCount = 1 });
            t.Entries.Add(new PopulationEntry
            { BlueprintName = "Wardline", Weight = 2, MinCount = 0, MaxCount = 1 });
            if (tier >= 2)
                t.Entries.Add(new PopulationEntry
                { BlueprintName = "MawToad", Weight = 2, MinCount = 0, MaxCount = 2 });
            return t;
        }

        /// <summary>Slopes: fungal forest over the root-buttress
        /// ridges. Where Urqu's signs show first.</summary>
        private static PopulationTable StumpSlopes(int tier)
        {
            var t = new PopulationTable { Name = $"StumpSlopes{tier}" };
            // "Urqu's signs in flesh" — increased spawning during
            // manifest periods, and none at all while it is quiet.
            t.Entries.Add(new PopulationEntry
            {
                BlueprintName = "SariSnake", Weight = 4, MinCount = 1, MaxCount = 3,
                RequiresWorldFlag = "UrquActive",
            });
            // The structural opposite: Urqu-opposed wild fauna.
            t.Entries.Add(new PopulationEntry
            { BlueprintName = "Wardline", Weight = 3, MinCount = 1, MaxCount = 2 });
            t.Entries.Add(new PopulationEntry
            { BlueprintName = "YellowfootWayfarer", Weight = 2, MinCount = 0, MaxCount = 1 });
            if (tier >= 2)
                t.Entries.Add(new PopulationEntry
                { BlueprintName = "MawToad", Weight = 2, MinCount = 0, MaxCount = 2 });
            t.Entries.Add(new PopulationEntry
            { BlueprintName = "SkySari", Weight = 1, MinCount = 0, MaxCount = 1,
              RequiresWorldFlag = "UrquActive" });
            return t;
        }

        /// <summary>Summit endemics are filtered to their real microhabitat
        /// by the Stump pipeline. Singer silence is W8's clock, not despawn.</summary>
        private static PopulationTable StumpSummit(int tier)
        {
            var t = new PopulationTable { Name = $"StumpSummit{tier}" };
            t.Entries.Add(new PopulationEntry
            { BlueprintName = "SummitSinger", Weight = 4, MinCount = 2, MaxCount = 4 });
            t.Entries.Add(new PopulationEntry
            { BlueprintName = "BrocchiniaSentinel", Weight = 2, MinCount = 1, MaxCount = 2 });
            t.Entries.Add(new PopulationEntry
            { BlueprintName = "SkySari", Weight = 1, MinCount = 0, MaxCount = 1,
              RequiresWorldFlag = "UrquActive" });
            t.Entries.Add(new PopulationEntry
            { BlueprintName = "Wardline", Weight = 2, MinCount = 0, MaxCount = 1 });
            t.Entries.Add(new PopulationEntry
            { BlueprintName = "SariSnake", Weight = 2, MinCount = 0, MaxCount = 2,
              RequiresWorldFlag = "UrquActive" });
            return t;
        }

        public static PopulationTable StumpSima()
        {
            var t = new PopulationTable { Name = "StumpSima" };
            t.Entries.Add(new PopulationEntry
            { BlueprintName = "PrickleBrowGecko", Weight = 1, MinCount = 1, MaxCount = 3 });
            return t;
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
                    case BiomeType.Grovelands: return GrovelandsTier3();
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
                    case BiomeType.Grovelands: return GrovelandsTier2();
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
                case BiomeType.Grovelands: return GrovelandsTier1();
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
                    new PopulationEntry { BlueprintName = "Viper", Weight = 2, MinCount = 1, MaxCount = 2, EncounterGroup = "SpreadTier1Encounter" },
                    // One small encounter per zone: roadside trouble OR a snake hedge.
                    new PopulationEntry { BlueprintName = "MarlbackScrabbler", Weight = 2, MinCount = 1, MaxCount = 2, EncounterGroup = "SpreadTier1Encounter" },
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
                    new PopulationEntry { BlueprintName = "MarlbackScrabbler", Weight = 4, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "MarlbackGleaner", Weight = 3, MinCount = 1, MaxCount = 2 },
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
                    new PopulationEntry { BlueprintName = "MarlbackTunnelguard", Weight = 4, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "MarlbackScrabbler", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "MarlbackGleaner", Weight = 2, MinCount = 1, MaxCount = 2 },
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
                    new PopulationEntry { BlueprintName = "MarlbackTunnelguard", Weight = 2, MinCount = 0, MaxCount = 2 },
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
                    new PopulationEntry { BlueprintName = "MarlbackScrabbler", Weight = 5, MinCount = 2, MaxCount = 5 },
                    new PopulationEntry { BlueprintName = "MarlbackGleaner", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "MarlbackTunnelguard", Weight = 1, MinCount = 0, MaxCount = 2 },
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
                    new PopulationEntry { BlueprintName = "Bandfrog", Weight = 3, MinCount = 1, MaxCount = 2, EncounterGroup = "SoddenTier2Encounter" },
                    new PopulationEntry { BlueprintName = "Viper", Weight = 2, MinCount = 1, MaxCount = 2, EncounterGroup = "SoddenTier2Encounter" },
                    new PopulationEntry { BlueprintName = "MawToad", Weight = 2, MinCount = 1, MaxCount = 1, EncounterGroup = "SoddenTier2Encounter" },
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
                    new PopulationEntry { BlueprintName = "Bandfrog", Weight = 4, MinCount = 1, MaxCount = 3, EncounterGroup = "SoddenTier3Encounter" },
                    new PopulationEntry { BlueprintName = "MawToad", Weight = 3, MinCount = 1, MaxCount = 2, EncounterGroup = "SoddenTier3Encounter" },
                    new PopulationEntry { BlueprintName = "Viper", Weight = 2, MinCount = 1, MaxCount = 2, EncounterGroup = "SoddenTier3Encounter" },
                    new PopulationEntry { BlueprintName = "Greatdew", Weight = 4, MinCount = 1, MaxCount = 3 },
                }
            };
        }


        // ── The Grovelands (W4.3) ─────────────────────────────────────
        //
        // Choir country is not dangerous the way a jungle is dangerous.
        // Most of what moves here is gentle (moths, the slow shapes that
        // used to be somebody) and most of what kills you was patient
        // about it (the sundews). Rotlings and mosshulks stay — they are
        // fungal fauna, Choir-adjacent by nature, not jungle leftovers.
        // The tendrils in the tables are AMBIENT extras; the fens
        // guarantee their own (R7).

        public static PopulationTable GrovelandsTier1()
        {
            return new PopulationTable
            {
                Name = "GrovelandsTier1",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "HelmwoodFrog", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "GroveLanternMoth", Weight = 5, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "Rotling", Weight = 2, MinCount = 0, MaxCount = 2 },
                    // Review: the slow shapes arrive with tier 2, like
                    // the Sodden's toads — the gentle ring stays gentle.
                    new PopulationEntry { BlueprintName = "WineLeafSundew", Weight = 2, MinCount = 0, MaxCount = 2 },
                }
            };
        }

        public static PopulationTable GrovelandsTier2()
        {
            return new PopulationTable
            {
                Name = "GrovelandsTier2",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "HelmwoodFrog", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "GroveLanternMoth", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Rotling", Weight = 3, MinCount = 1, MaxCount = 2, EncounterGroup = "GrovelandsTier2Encounter" },
                    new PopulationEntry { BlueprintName = "Shambler", Weight = 3, MinCount = 1, MaxCount = 2, EncounterGroup = "GrovelandsTier2Encounter" },
                    new PopulationEntry { BlueprintName = "WineLeafSundew", Weight = 3, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Mosshulk", Weight = 1, MinCount = 1, MaxCount = 1, EncounterGroup = "GrovelandsTier2Encounter" },
                    new PopulationEntry { BlueprintName = "ChoirTendril", Weight = 1, MinCount = 0, MaxCount = 1 },
                }
            };
        }

        public static PopulationTable GrovelandsTier3()
        {
            return new PopulationTable
            {
                Name = "GrovelandsTier3",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "HelmwoodFrog", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Shambler", Weight = 4, MinCount = 1, MaxCount = 3, EncounterGroup = "GrovelandsTier3Encounter" },
                    new PopulationEntry { BlueprintName = "Mosshulk", Weight = 2, MinCount = 1, MaxCount = 2, EncounterGroup = "GrovelandsTier3Encounter" },
                    new PopulationEntry { BlueprintName = "WineLeafSundew", Weight = 4, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "Rotling", Weight = 3, MinCount = 1, MaxCount = 2, EncounterGroup = "GrovelandsTier3Encounter" },
                    new PopulationEntry { BlueprintName = "ChoirTendril", Weight = 1, MinCount = 0, MaxCount = 1 },
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
                    new PopulationEntry { BlueprintName = "MarlbackScrabbler", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "MarlbackGleaner", Weight = 2, MinCount = 0, MaxCount = 2 },
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
                    new PopulationEntry { BlueprintName = "MarlbackScrabbler", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "MarlbackTunnelguard", Weight = 2, MinCount = 1, MaxCount = 3 },
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
                    new PopulationEntry { BlueprintName = "MarlbackGleaner", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "MarlbackTunnelguard", Weight = 2, MinCount = 1, MaxCount = 2 },
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
                    new PopulationEntry { BlueprintName = "MarlbackTunnelguard", Weight = 4, MinCount = 2, MaxCount = 4 },
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
                    new PopulationEntry { BlueprintName = "MarlbackTunnelguard", Weight = 2, MinCount = 1, MaxCount = 2 },
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
        // Beasts/OutlandRaiders-faction bruisers; the faction-tagged mutants
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
                    new PopulationEntry { BlueprintName = "MarlbackTunnelguard", Weight = 4, MinCount = 2, MaxCount = 4 },
                    new PopulationEntry { BlueprintName = "Glowmaw", Weight = 3, MinCount = 1, MaxCount = 3 },
                    // Phase C: the far ring's leadership and its grazers.
                    new PopulationEntry { BlueprintName = "MarlbackBreacher", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Mosshulk", Weight = 2, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "CaveSlime", Weight = 2, MinCount = 1, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "MarlbackScrabbler", Weight = 3, MinCount = 2, MaxCount = 4 },
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
                            new PopulationEntry { BlueprintName = "MarlbackTunnelguard", Weight = 4, MinCount = 2, MaxCount = 4 },
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
                case BiomeType.Grovelands:
                    // W4.3 — the groves guard their own, slowly.
                    return new PopulationTable
                    {
                        Name = "GrovelandsLairGuards",
                        Entries = new List<PopulationEntry>
                        {
                            new PopulationEntry { BlueprintName = "Shambler", Weight = 3, MinCount = 1, MaxCount = 2 },
                            new PopulationEntry { BlueprintName = "Mosshulk", Weight = 2, MinCount = 0, MaxCount = 1 },
                            new PopulationEntry { BlueprintName = "WineLeafSundew", Weight = 2, MinCount = 1, MaxCount = 2 },
                        }
                    };
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

            // One group per depth roll; cap pack size instead of filling deep caves with marlbacks.
            int snapMin = 1 + System.Math.Min(tier - 1, 2);
            int snapMax = 3 + System.Math.Min(tier - 1, 2);
            int scavMin = 1;
            int scavMax = 2 + System.Math.Min(tier - 1, 2);
            int huntMin = 1;
            int huntMax = System.Math.Min(tier, 3);

            var table = new PopulationTable
            {
                Name = $"Underground_Depth{depth}",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "MarlbackScrabbler", Weight = tier == 1 ? 5 : 2, MinCount = snapMin, MaxCount = snapMax, EncounterGroup = "DepthEncounter" },
                    new PopulationEntry { BlueprintName = "MarlbackGleaner", Weight = tier == 1 ? 3 : 1, MinCount = scavMin, MaxCount = scavMax, EncounterGroup = "DepthEncounter" },
                    new PopulationEntry { BlueprintName = "MarlbackTunnelguard", Weight = tier == 1 ? 2 : 1, MinCount = huntMin, MaxCount = huntMax, EncounterGroup = "DepthEncounter" },
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
                table.Entries.Add(new PopulationEntry { BlueprintName = "CaveBear", Weight = 3, MinCount = 1, MaxCount = 1, EncounterGroup = "DepthEncounter" });
                table.Entries.Add(new PopulationEntry { BlueprintName = "IceWight", Weight = 2, MinCount = 1, MaxCount = 1, EncounterGroup = "DepthEncounter" });
                table.Entries.Add(new PopulationEntry { BlueprintName = "Rotling", Weight = 3, MinCount = 1, MaxCount = 2, EncounterGroup = "DepthEncounter" });
            }
            if (tier >= 3)
            {
                table.Entries.Add(new PopulationEntry { BlueprintName = "SkeletalSentry", Weight = 3, MinCount = 1, MaxCount = 2, EncounterGroup = "DepthEncounter" });
                table.Entries.Add(new PopulationEntry { BlueprintName = "CharredHusk", Weight = 3, MinCount = 1, MaxCount = 1, EncounterGroup = "DepthEncounter" });
                table.Entries.Add(new PopulationEntry { BlueprintName = "PaleStalker", Weight = 3, MinCount = 1, MaxCount = 1, EncounterGroup = "DepthEncounter" });
            }
            if (tier >= 4)
            {
                table.Entries.Add(new PopulationEntry { BlueprintName = "StoneGolem", Weight = 3, MinCount = 1, MaxCount = 1, EncounterGroup = "DepthEncounter" });
                table.Entries.Add(new PopulationEntry { BlueprintName = "ObsidianBrute", Weight = 3, MinCount = 1, MaxCount = 1, EncounterGroup = "DepthEncounter" });
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
