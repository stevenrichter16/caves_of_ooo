using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Skills;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Adversarial sweep for the starting-build data path (taxonomy surfaces:
    /// parser malformed inputs, boundary inputs, anti-exploit invariants,
    /// state atomicity). Each test names the bug class a buggy implementation
    /// would fail.
    /// </summary>
    public class StartingBuildAdversarialTests
    {
        private static ScenarioTestHarness _h;

        [OneTimeSetUp] public void Up() { _h = new ScenarioTestHarness(); }
        [OneTimeTearDown] public void Down() { _h?.Dispose(); _h = null; }
        [SetUp] public void Setup() { StartingBuildRegistry.ResetForTests(); }
        [TearDown] public void TearDown() { StartingBuildRegistry.ResetForTests(); }

        private static string Json(string body) => "{\"Builds\":[" + body + "]}";

        private static string Def(string id, string attrs = "\"Strength\":18,\"Agility\":18,\"Toughness\":18,\"Ego\":16",
            string items = "", string skills = "", string kind = "build") =>
            "{\"Id\":\"" + id + "\",\"Name\":\"" + id + "\",\"Kind\":\"" + kind + "\",\"Attributes\":{" + attrs + "},"
            + "\"Items\":[" + items + "],\"Skills\":[" + skills + "]}";

        // ── Parser: malformed input ─────────────────────────────

        [TestCase(null)] [TestCase("")] [TestCase("   ")] [TestCase("not json")] [TestCase("{}")] [TestCase("{\"Builds\":null}")]
        public void MalformedJson_LoadsNothing_AndDoesNotThrow(string json)
        {
            Assert.DoesNotThrow(() => StartingBuildRegistry.LoadFromJson(json));
            Assert.AreEqual(0, StartingBuildRegistry.All.Count);
            Assert.IsNull(StartingBuildRegistry.Get("duelist"));
        }

        [Test]
        public void ReloadReplaces_NotAppends()
        {
            StartingBuildRegistry.LoadFromJson(Json(Def("a")));
            StartingBuildRegistry.LoadFromJson(Json(Def("b")));

            CollectionAssert.AreEqual(new[] { "b" }, StartingBuildRegistry.All.Select(x => x.Id).ToArray());
        }

        [Test]
        public void GetIsCaseInsensitive_AndTrimsNothingElse()
        {
            StartingBuildRegistry.LoadFromJson(Json(Def("duelist")));

            Assert.IsNotNull(StartingBuildRegistry.Get("DUELIST"));
            Assert.IsNull(StartingBuildRegistry.Get("duel ist"));
            Assert.IsNull(StartingBuildRegistry.Get(null));
            Assert.IsNull(StartingBuildRegistry.Get(""));
        }

        // ── Validator: each rule has a counter-check ────────────

        [Test]
        public void Validate_DuplicateIds_AreReported()
        {
            StartingBuildRegistry.LoadFromJson(Json(Def("a") + "," + Def("a")));

            StringAssert.Contains("duplicate", string.Join(";", StartingBuildRegistry.Validate(_h.Factory)).ToLowerInvariant());
        }

        [Test]
        public void Validate_MissingId_IsReported()
        {
            StartingBuildRegistry.LoadFromJson(Json(Def("")));

            Assert.IsNotEmpty(StartingBuildRegistry.Validate(_h.Factory));
        }

        [Test]
        public void Validate_AttributeSumOff70_IsReported_ButClassicIsExempt()
        {
            StartingBuildRegistry.LoadFromJson(Json(
                Def("big", "\"Strength\":22,\"Agility\":22,\"Toughness\":22,\"Ego\":22") + ","
                + Def("classic", "\"Strength\":18,\"Agility\":18,\"Toughness\":18,\"Ego\":17", kind: "classic")));

            var problems = StartingBuildRegistry.Validate(_h.Factory);

            Assert.IsTrue(problems.Any(p => p.Contains("big") && p.Contains("70")), string.Join(";", problems));
            Assert.IsFalse(problems.Any(p => p.Contains("classic")), "Classic's card is metadata, never applied");
        }

        [Test]
        public void Validate_OddAttribute_IsReported()
        {
            StartingBuildRegistry.LoadFromJson(Json(Def("odd", "\"Strength\":19,\"Agility\":17,\"Toughness\":18,\"Ego\":16")));

            Assert.IsTrue(StartingBuildRegistry.Validate(_h.Factory).Any(p => p.Contains("odd") && p.Contains("even")));
        }

        [Test]
        public void Validate_WeaponMustBeEquippedBeforeAnyShield()
        {
            string shieldFirst = "{\"Blueprint\":\"Buckler\",\"Equip\":true},{\"Blueprint\":\"Dagger\",\"Equip\":true}";
            string weaponFirst = "{\"Blueprint\":\"Dagger\",\"Equip\":true},{\"Blueprint\":\"Buckler\",\"Equip\":true}";

            StartingBuildRegistry.LoadFromJson(Json(Def("bad", items: shieldFirst) + "," + Def("good", items: weaponFirst)));
            var problems = StartingBuildRegistry.Validate(_h.Factory);

            Assert.IsTrue(problems.Any(p => p.Contains("bad") && p.ToLowerInvariant().Contains("weapon")), string.Join(";", problems));
            Assert.IsFalse(problems.Any(p => p.Contains("good")), "same items, weapon first: valid");
        }

        [Test]
        public void Validate_EquippingFood_IsReported()
        {
            StartingBuildRegistry.LoadFromJson(Json(Def("meal", items: "{\"Blueprint\":\"DriedMeat\",\"Equip\":true}")));

            Assert.IsTrue(StartingBuildRegistry.Validate(_h.Factory).Any(p => p.Contains("meal") && p.Contains("DriedMeat")));
        }

        [Test]
        public void Validate_MoreActivesThanHotbarSlots_IsReported()
        {
            // 11 distinct active skills; the hotbar has 10 slots.
            string eleven = string.Join(",", new[]
            {
                "Pyromancy_Kindle", "Pyromancy_EmberSpit", "Pyromancy_FlamingHands", "Pyromancy_FlameJet", "Pyromancy_Backdraft",
                "Pyromancy_Oilmark", "Galvanism_ArcBolt", "Galvanism_GroundSurge", "Hydromancy_Quench", "Hydromancy_JetBlast",
                "Cryomancy_RimeGrip"
            }.Select(s => "\"" + s + "\""));
            StartingBuildRegistry.LoadFromJson(Json(Def("many", skills: eleven)));

            Assert.IsTrue(StartingBuildRegistry.Validate(_h.Factory).Any(p => p.Contains("many") && p.Contains("10")));
        }

        [Test]
        public void Validate_DuplicateSkill_IsReported()
        {
            StartingBuildRegistry.LoadFromJson(Json(Def("twice", skills: "\"Pyromancy_Kindle\",\"Pyromancy_Kindle\"")));

            Assert.IsTrue(StartingBuildRegistry.Validate(_h.Factory).Any(p => p.Contains("twice") && p.Contains("Pyromancy_Kindle")));
        }

        [Test]
        public void Validate_UnknownSkillAndBlueprint_AreBothNamed()
        {
            StartingBuildRegistry.LoadFromJson(Json(Def("ghost",
                items: "{\"Blueprint\":\"NoSuchItem\"}", skills: "\"Nope_Skill\"")));

            string all = string.Join(";", StartingBuildRegistry.Validate(_h.Factory));

            StringAssert.Contains("NoSuchItem", all);
            StringAssert.Contains("Nope_Skill", all);
        }

        // ── Boundary inputs ─────────────────────────────────────

        [TestCase(0)] [TestCase(-3)]
        public void ItemCountZeroOrNegative_IsTreatedAsOne_NotZero(int count)
        {
            // JsonUtility cannot apply a field initializer to list elements, so
            // a missing or bad count must still mean "one".
            StartingBuildRegistry.LoadFromJson(Json(Def("one", items: "{\"Blueprint\":\"DriedMeat\",\"Count\":" + count + "}")));
            var p = _h.CreateContext(playerBlueprint: "Player").PlayerEntity;

            var r = StartingBuildService.Apply(p, _h.Factory, StartingBuildRegistry.Get("one"));

            Assert.IsTrue(r.Success, string.Join(";", r.Errors));
            var meat = p.GetPart<InventoryPart>().Objects.First(o => o.BlueprintName == "DriedMeat");
            Assert.AreEqual(1, meat.GetPart<StackerPart>()?.StackCount ?? 1);
        }

        // ── Shipped content: cross-cutting invariants ───────────

        [TestCase("duelist")] [TestCase("breaker")] [TestCase("stormcaller")] [TestCase("bombardier")]
        public void ShippedBuild_CarriesLessThanTheOverburdenLimit(string id)
        {
            // Overburden blocks movement at Str x 15; a starting character
            // must be nowhere near it. (Equipped items count.)
            StartingBuildRegistry.LoadFromJson(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Builds/StartingBuilds.json")));
            var b = StartingBuildRegistry.Get(id);
            var p = _h.CreateContext(playerBlueprint: "Player").PlayerEntity;

            Assert.IsTrue(StartingBuildService.Apply(p, _h.Factory, b).Success);

            int weight = p.GetPart<InventoryPart>().GetCarriedWeight();
            Assert.Less(weight, b.Attributes.Strength * 15 / 2, id + " carries " + weight + " lb");
        }

        [Test]
        public void ShippedBuilds_AreDistinct_EachDifferentlyShaped()
        {
            // Anti-collapse: four builds that all got the same weapon, same
            // spell and same attributes would pass every other test.
            StartingBuildRegistry.LoadFromJson(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Builds/StartingBuilds.json")));
            var builds = new[] { "duelist", "breaker", "stormcaller", "bombardier" }.Select(StartingBuildRegistry.Get).ToList();

            Assert.AreEqual(4, builds.Select(b => b.Skills.FirstOrDefault()).Distinct().Count(), "four different lead skills");
            Assert.AreEqual(4, builds.Select(b => (b.Attributes.Strength, b.Attributes.Agility, b.Attributes.Ego)).Distinct().Count());
            Assert.IsTrue(builds.All(b => !string.IsNullOrWhiteSpace(b.Weakness)), "every card names a weakness");
            Assert.IsTrue(builds.All(b => !string.IsNullOrWhiteSpace(b.Tagline)));
        }

        // ════════════════════════════════════════════════════════════
        // Hypothesis-driven: save/load reach. A build is chosen once, before the
        // first checkpoint, so every later session sees it only through the save.
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Whole-session round trip (the way a real save works). A bare
        /// <c>SaveEntityBody</c> cannot be used here: equipped items are written
        /// as graph references and only resolve inside a full session save.
        /// </summary>
        private static Entity RoundTrip(ScenarioContext c)
        {
            var manager = new OverworldZoneManager(null, 12345);
            manager.ReplaceLoadedState(
                new Dictionary<string, Zone> { { c.Zone.ZoneID, c.Zone } },
                c.Zone.ZoneID,
                new Dictionary<string, List<ZoneConnection>>());
            var state = GameSessionState.Capture("test-game", "test-version", manager, c.Turns, c.PlayerEntity);
            using var stream = new MemoryStream();
            state.Save(new SaveWriter(stream));
            stream.Position = 0;
            return GameSessionState.Load(new SaveReader(stream, _h.Factory)).Player;
        }

        private static List<string> HotbarCommands(Entity e)
        {
            var abilities = e.GetPart<ActivatedAbilitiesPart>();
            var list = new List<string>();
            for (int i = 0; i < ActivatedAbilitiesPart.SlotCount; i++)
            {
                var id = abilities.SlotAssignments[i];
                var ability = id == System.Guid.Empty ? null : abilities.AbilityList.FirstOrDefault(a => a.ID == id);
                list.Add(ability?.Command ?? "-");
            }
            return list;
        }

        [TestCase("duelist")]
        [TestCase("breaker")]
        [TestCase("stormcaller")]
        [TestCase("bombardier")]
        public void SavedBuild_KeepsItsMarker_Stats_Skills_Weapon_AndHotbarOrder(string id)
        {
            // Hypothesis: the build lives only on the entity, so a save that dropped
            // any of it would hand a loaded character a different (or no) start.
            StartingBuildRegistry.LoadFromJson(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Builds/StartingBuilds.json")));
            var ctx = _h.CreateContext(playerBlueprint: "Player");
            var p = ctx.PlayerEntity;
            Assert.IsTrue(StartingBuildService.Apply(p, _h.Factory, StartingBuildRegistry.Get(id)).Success);

            var loaded = RoundTrip(ctx);

            Assert.AreEqual(id, loaded.GetProperty(StartingBuildService.PropertyName), "marker");
            foreach (var stat in new[] { "Strength", "Agility", "Toughness", "Ego" })
                Assert.AreEqual(p.GetStatValue(stat, -1), loaded.GetStatValue(stat, -1), stat);
            CollectionAssert.AreEquivalent(
                p.GetPart<SkillsPart>().SkillList.Select(sk => sk.GetType().Name),
                loaded.GetPart<SkillsPart>().SkillList.Select(sk => sk.GetType().Name), "skills");
            Assert.AreEqual(StartingBuildService.PrimaryHandWeapon(p)?.BlueprintName,
                StartingBuildService.PrimaryHandWeapon(loaded)?.BlueprintName, "primary-hand weapon");
            CollectionAssert.AreEqual(HotbarCommands(p), HotbarCommands(loaded), "hotbar order");
        }

        [Test]
        public void CounterCheck_AnUnbuiltPlayer_RoundTripsWithNoMarkerAndNoWeapon()
        {
            // Without this, the round-trip test above could pass vacuously (e.g. if the
            // harness quietly gave every player a dagger or a marker).
            var ctx = _h.CreateContext(playerBlueprint: "Player");

            var loaded = RoundTrip(ctx);

            Assert.IsTrue(string.IsNullOrEmpty(loaded.GetProperty(StartingBuildService.PropertyName)));
            var weapon = StartingBuildService.PrimaryHandWeapon(loaded)?.BlueprintName;
            Assert.AreNotEqual("Dagger", weapon, "a bare player swings its fist, not a build's weapon");
            Assert.AreNotEqual("Cudgel", weapon);
        }

        [Test]
        public void ALoadedBuiltPlayer_CannotBeHandedASecondBuild()
        {
            // Hypothesis: the double-grant guard is a property on the entity, so it
            // must survive a load or "New game" over a save could stack a second kit.
            StartingBuildRegistry.LoadFromJson(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Builds/StartingBuilds.json")));
            var ctx = _h.CreateContext(playerBlueprint: "Player");
            var p = ctx.PlayerEntity;
            Assert.IsTrue(StartingBuildService.Apply(p, _h.Factory, StartingBuildRegistry.Get("duelist")).Success);

            var loaded = RoundTrip(ctx);
            var again = StartingBuildService.Apply(loaded, _h.Factory, StartingBuildRegistry.Get("breaker"));

            Assert.IsFalse(again.Success);
            StringAssert.Contains("duelist", string.Join(" ", again.Errors));
        }

        [Test]
        public void NoShippedBuild_TakesTheUnbuyableTreesOrQuench()
        {
            // Design rules (STARTING-BUILDS.md §3): no Acrobatics/Persuasion
            // (50-100 SP by design), no Quench (the freeze exploit this change
            // exists to fix must not be a build's centrepiece).
            StartingBuildRegistry.LoadFromJson(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Builds/StartingBuilds.json")));

            foreach (var b in StartingBuildRegistry.All.Where(x => !x.IsClassic))
                foreach (var s in b.Skills)
                {
                    StringAssert.DoesNotStartWith("Acrobatics", s, b.Id);
                    StringAssert.DoesNotStartWith("Persuasion", s, b.Id);
                    Assert.AreNotEqual("Hydromancy_Quench", s, b.Id);
                }
        }
    }
}
