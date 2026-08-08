using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BIOME-OVERHAUL Phase A5 — spawn &amp; difficulty repairs
    /// (Docs/BIOME-OVERHAUL.md §2 A5, log in Docs/BIOME-OVERHAUL-LOG.md).
    /// Three verified defects: (1) GetBiomeTable has no tier-3 branch, so
    /// the far ring of the world is no harder than the middle ring;
    /// (2) boss XP is inverted (SnapjawChieftain: 40 HP for an inherited
    /// 15 XP — less than a SnapjawHunter); (3) five hostiles punch with
    /// the 1d2 default fist because their blueprints carry no
    /// NaturalWeapon prop (DesertBandit, BrassHusk, GlassScorpion,
    /// SporeShambler, RuneCultist).
    /// </summary>
    [TestFixture]
    public class BiomeSpawnRepairTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        // ── Helpers (mirror AlphaCombatStakesTests) ──────────────

        private static MeleeWeaponPart ResolvedHandWeapon(Entity creature)
        {
            var body = creature.GetPart<Body>();
            Assert.IsNotNull(body, $"{creature.BlueprintName} must have a Body");
            body.RegenerateDefaultEquipment();
            var parts = body.GetBody().GetParts();
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i]._DefaultBehavior != null)
                {
                    var w = parts[i]._DefaultBehavior.GetPart<MeleeWeaponPart>();
                    if (w != null) return w;
                }
            }
            Assert.Fail($"{creature.BlueprintName}: no resolved natural weapon");
            return null;
        }

        private static int XpOf(string blueprint)
        {
            var e = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(e, blueprint);
            var stat = e.GetStat("XPValue");
            Assert.IsNotNull(stat, $"{blueprint} must have an XPValue stat");
            return stat.Value;
        }

        // ── 1. Tier-3 tables exist and are selected ──────────────

        [Test]
        public void GetBiomeTable_Tier3_ReturnsTier3Tables()
        {
            Assert.AreEqual("CaveTier3", PopulationTable.GetBiomeTable(BiomeType.Cave, 3).Name);
            Assert.AreEqual("DesertTier3", PopulationTable.GetBiomeTable(BiomeType.Desert, 3).Name);
            Assert.AreEqual("JungleTier3", PopulationTable.GetBiomeTable(BiomeType.Jungle, 3).Name);
            Assert.AreEqual("RuinsTier3", PopulationTable.GetBiomeTable(BiomeType.Ruins, 3).Name);
        }

        [Test]
        public void GetBiomeTable_LowerTiers_Unchanged()
        {
            // Counter-check: adding the tier-3 branch must not shift
            // what tiers 1 and 2 resolve to.
            foreach (var biome in new[] { BiomeType.Cave, BiomeType.Desert, BiomeType.Jungle, BiomeType.Ruins })
            {
                StringAssert.EndsWith("Tier1", PopulationTable.GetBiomeTable(biome, 1).Name, biome.ToString());
                StringAssert.EndsWith("Tier2", PopulationTable.GetBiomeTable(biome, 2).Name, biome.ToString());
            }
        }

        [Test]
        public void Tier3Tables_CarryGuaranteedEliteBackbone()
        {
            // The point of tier 3 is a REAL difficulty step. Each table
            // guarantees at least one tier-2+ bruiser (MinCount >= 1) and
            // fields an activated set-piece species so the far ring
            // reads differently from the middle ring.
            Assert.GreaterOrEqual(MinOf(PopulationTable.CaveTier3(), "CaveBear"), 1);
            Assert.GreaterOrEqual(MinOf(PopulationTable.DesertTier3(), "SandWurm"), 1);
            Assert.GreaterOrEqual(MinOf(PopulationTable.JungleTier3(), "JungleApe"), 1);
            Assert.GreaterOrEqual(MinOf(PopulationTable.RuinsTier3(), "StoneGolem"), 1);

            Assert.IsTrue(HasEntry(PopulationTable.DesertTier3(), "GlassScorpion"), "desert ecology");
            Assert.IsTrue(HasEntry(PopulationTable.JungleTier3(), "ChoirTendril"), "jungle elite");
            Assert.IsTrue(HasEntry(PopulationTable.RuinsTier3(), "CharredHusk"), "ruins fire twin");
            Assert.IsTrue(HasEntry(PopulationTable.RuinsTier3(), "PalimpsestEcho"), "ruins elite");
        }

        private static int MinOf(PopulationTable t, string blueprint)
        {
            foreach (var e in t.Entries)
                if (e.BlueprintName == blueprint) return e.MinCount;
            Assert.Fail($"{t.Name} has no entry for {blueprint}");
            return 0;
        }

        private static bool HasEntry(PopulationTable t, string blueprint)
        {
            foreach (var e in t.Entries)
                if (e.BlueprintName == blueprint) return true;
            return false;
        }

        // ── 2. Content integrity: every table names real blueprints ──

        [Test]
        public void AllPopulationTables_ReferenceOnlyRealBlueprints()
        {
            // A table entry for a blueprint that doesn't exist silently
            // spawns nothing — this pin makes that a loud test failure
            // for every table current and future.
            var tables = new List<PopulationTable>
            {
                PopulationTable.CaveTier1(), PopulationTable.DesertTier1(),
                PopulationTable.JungleTier1(), PopulationTable.RuinsTier1(),
                PopulationTable.CaveTier2(), PopulationTable.DesertTier2(),
                PopulationTable.JungleTier2(), PopulationTable.RuinsTier2(),
                PopulationTable.CaveTier3(), PopulationTable.DesertTier3(),
                PopulationTable.JungleTier3(), PopulationTable.RuinsTier3(),
                PopulationTable.LairGuards(BiomeType.Cave),
                PopulationTable.LairGuards(BiomeType.Desert),
                PopulationTable.LairGuards(BiomeType.Jungle),
                PopulationTable.LairGuards(BiomeType.Ruins),
                PopulationTable.VillageDecor(),
                PopulationTable.UndergroundTier(1),
                PopulationTable.UndergroundTier(5),
                PopulationTable.UndergroundTier(10),
            };
            foreach (var table in tables)
                foreach (var entry in table.Entries)
                    Assert.IsTrue(_factory.Blueprints.ContainsKey(entry.BlueprintName),
                        $"{table.Name} references unknown blueprint '{entry.BlueprintName}'");
        }

        // ── 3. Boss XP ladder ────────────────────────────────────

        [Test]
        public void BossXP_ScalesAboveTheirMinions()
        {
            // Tier-2 lair bosses land at 65 (above SnapjawHunter's 40),
            // the tier-3 boss at 100 (above non-boss ChoirTendril's 70).
            // Chieftain previously INHERITED Snapjaw's 15.
            Assert.AreEqual(65, XpOf("SnapjawChieftain"), "chieftain");
            Assert.AreEqual(65, XpOf("DesertProwler"), "prowler");
            Assert.AreEqual(65, XpOf("JungleStalker"), "stalker");
            Assert.AreEqual(100, XpOf("AncientGuardian"), "guardian");
        }

        [Test]
        public void BossXP_MinionValuesUntouched()
        {
            // Counter-check: the boss retune must not drift the rank
            // and file that AlphaCombatStakesTests already pinned.
            Assert.AreEqual(15, XpOf("Snapjaw"));
            Assert.AreEqual(40, XpOf("SnapjawHunter"));
            Assert.AreEqual(70, XpOf("ChoirTendril"));
        }

        // ── 4. The five 1d2-fist hostiles get real weapons ───────

        [Test]
        public void DesertBandit_ResolvesBanditBlade()
        {
            var w = ResolvedHandWeapon(_factory.CreateEntity("DesertBandit"));
            Assert.AreEqual("1d6", w.BaseDamage);
        }

        [Test]
        public void BrassHusk_ResolvesShockingHuskFist()
        {
            var w = ResolvedHandWeapon(_factory.CreateEntity("BrassHusk"));
            Assert.AreEqual("1d6", w.BaseDamage);
            StringAssert.Contains("Electrified", w.OnHitEffectsRaw,
                "the brass husk's whole identity is the arc on touch");
        }

        [Test]
        public void GlassScorpion_ResolvesGlassSting()
        {
            var w = ResolvedHandWeapon(_factory.CreateEntity("GlassScorpion"));
            Assert.AreEqual("1d4", w.BaseDamage);
            Assert.AreEqual(2, w.PenBonus, "glass punches through armor");
        }

        [Test]
        public void SporeShambler_ResolvesSporeTouchWithGasEmit()
        {
            var w = ResolvedHandWeapon(_factory.CreateEntity("SporeShambler"));
            Assert.AreEqual("1d4", w.BaseDamage);
            StringAssert.StartsWith("fungal-spores", w.EmitGasOnHitRaw,
                "shambler hits release a spore puff (gas id from Content/Data/GasDefinitions)");
        }

        [Test]
        public void RuneCultist_ResolvesCultistKnife()
        {
            var w = ResolvedHandWeapon(_factory.CreateEntity("RuneCultist"));
            Assert.AreEqual("1d4", w.BaseDamage);
        }

        [Test]
        public void CreatureWithoutNaturalWeaponProp_StillGetsDefaultFist()
        {
            // Counter-check: arming the five must not change the default
            // path for prop-less creatures.
            var w = ResolvedHandWeapon(_factory.CreateEntity("Villager"));
            Assert.AreEqual("1d2", w.BaseDamage);
        }
    }
}
