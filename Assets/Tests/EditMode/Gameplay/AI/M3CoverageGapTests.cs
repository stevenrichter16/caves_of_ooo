using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M3 (AIPetter / AIHoarder / AIRetriever / AIFleeToShrine /
    /// SanctuaryPart) coverage-gap tests, written TDD-style per
    /// Docs/QUD-PARITY.md Part 2.1.
    ///
    /// Existing M3 coverage in AIBehaviorPartTests.cs is broad —
    /// happy-path triggers, idempotency gates, blueprint loading,
    /// alliance / chance gates. The remaining gaps focus on:
    ///
    ///   - Tuning-constant anchors (DefaultChance, DefaultTargetTag,
    ///     DefaultThreshold, DefaultMaxTurns). Future tuning changes
    ///     should be intentional, not silent.
    ///   - The behaviorally-important constructor arguments that the
    ///     production code uses when pushing goals (returnHome=true on
    ///     Hoarder, returnHome=false on Retriever, endWhenNotFleeing=
    ///     false on AIFleeToShrine). These are easy to flip in a
    ///     refactor and the existing tests don't pin them.
    ///   - Defensive null-safety (null brain, null Rng, null event
    ///     parameters).
    ///   - Counter-checks (AlliesOnly=false fetches enemy throws —
    ///     pairs with the existing "ignores enemy throw" test).
    ///   - Boundary cases (HP exactly at the flee threshold, HP=0).
    ///
    /// All tests are regression shields. Any failure points either
    /// to a real bug or a flawed test expectation; analysis goes in
    /// the commit message.
    /// </summary>
    [TestFixture]
    public class M3CoverageGapTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // Mirrors the Phase6GoalsTests creature helper.
        private Entity CreateCreature(Zone zone, int x, int y,
            string faction = "Villagers", int hp = 20)
        {
            var entity = new Entity { BlueprintName = "TestCreature" };
            entity.Tags["Creature"] = "";
            if (!string.IsNullOrEmpty(faction))
                entity.Tags["Faction"] = faction;
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = faction ?? "creature" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d4" });
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = 100 });
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            entity.AddPart(brain);
            zone.AddEntity(entity, x, y);
            brain.StartingCellX = x;
            brain.StartingCellY = y;
            return entity;
        }

        private Entity CreateShinyItem(Zone zone, int x, int y, string name = "coin")
        {
            var item = new Entity { BlueprintName = name };
            item.Tags["Shiny"] = "";
            item.AddPart(new RenderPart { DisplayName = name });
            item.AddPart(new PhysicsPart { Solid = false, Takeable = true, Weight = 1 });
            zone.AddEntity(item, x, y);
            return item;
        }

        private Entity CreateShrine(Zone zone, int x, int y)
        {
            var shrine = new Entity { BlueprintName = "Shrine" };
            shrine.Tags["Furniture"] = "";
            shrine.AddPart(new RenderPart { DisplayName = "shrine" });
            shrine.AddPart(new PhysicsPart { Solid = false });
            shrine.AddPart(new SanctuaryPart());
            zone.AddEntity(shrine, x, y);
            return shrine;
        }

        // ============================================================
        // AIPetterPart — defaults + null-safety + non-bored events
        // ============================================================

        [Test]
        public void AIPetter_DefaultChance_IsThree()
        {
            // Tuning anchor: 3% per bored tick is the M3.1 design value.
            // Future blueprint authors override per-NPC; the field default
            // matters when Params omits a Chance entry.
            var part = new AIPetterPart();
            Assert.AreEqual(3, part.Chance,
                "Default Chance must remain 3. A change shifts every " +
                "AIPetter NPC that doesn't specify Chance in their blueprint.");
        }

        [Test]
        public void AIPetter_NonBoredEvent_PassesThrough_DoesNotConsume()
        {
            // Sanity: the part must only react to AIBoredEvent. Other
            // events must pass through unconsumed (return true).
            var entity = new Entity { BlueprintName = "TestKid" };
            entity.AddPart(new BrainPart { Rng = new Random(0) });
            entity.AddPart(new AIPetterPart { Chance = 100 });

            var ev = GameEvent.New("TakeDamage");
            bool result = entity.FireEvent(ev);

            Assert.IsTrue(result,
                "Non-AIBored events must pass through. If this fails, " +
                "AIPetter is hooking events it shouldn't.");
        }

        [Test]
        public void AIPetter_NullCurrentZone_DoesNotPushOrThrow()
        {
            // Defensive: brain present but no zone wired (test or partly-
            // initialised entity). Must not throw, must not push.
            var entity = new Entity { BlueprintName = "OrphanKid" };
            var brain = new BrainPart { Rng = new Random(0) }; // CurrentZone left null
            entity.AddPart(brain);
            entity.AddPart(new AIPetterPart { Chance = 100 });

            Assert.DoesNotThrow(() => entity.FireEvent(GameEvent.New("AIBored")),
                "Null CurrentZone must early-return cleanly.");
            Assert.IsFalse(brain.HasGoal<PetGoal>(),
                "No goal pushed when zone is null.");
        }

        // ============================================================
        // AIHoarderPart — tuning anchors + Takeable / InInventory
        // gates + the returnHome=true contract
        // ============================================================

        [Test]
        public void AIHoarder_DefaultTargetTag_IsShiny()
        {
            // Tuning anchor. Without this pin, a refactor that changed
            // the default would silently shift Magpie behavior (Magpie
            // blueprint configures TargetTag explicitly, but other
            // future authors might rely on the default).
            var part = new AIHoarderPart();
            Assert.AreEqual("Shiny", part.TargetTag);
        }

        [Test]
        public void AIHoarder_DefaultChance_IsFifteen()
        {
            var part = new AIHoarderPart();
            Assert.AreEqual(15, part.Chance);
        }

        [Test]
        public void AIHoarder_PushedGoFetchGoal_HasReturnHomeTrue()
        {
            // CRITICAL behavioral pin: the difference between Hoarder
            // and Retriever is exactly that Hoarder fetches WITH
            // returnHome=true (carry it back to nest), while Retriever
            // fetches with returnHome=false (just stop on arrival).
            // If a refactor flips this, Magpies stop returning gold
            // to their nests — the M3.2 acceptance behavior is broken.
            var zone = new Zone("HoarderZone");
            var magpie = CreateCreature(zone, 5, 5, faction: "Wildlife");
            var brain = magpie.GetPart<BrainPart>();
            magpie.AddPart(new AIHoarderPart { Chance = 100, TargetTag = "Shiny" });

            CreateShinyItem(zone, 7, 5, "gold");

            magpie.FireEvent(GameEvent.New("AIBored"));

            var goal = brain.FindGoal<GoFetchGoal>();
            Assert.IsNotNull(goal, "Hoarder must push GoFetchGoal when a Shiny is in zone.");
            Assert.IsTrue(goal.ReturnHome,
                "Hoarder MUST set returnHome=true on the pushed GoFetchGoal " +
                "— otherwise Magpie picks up gold and stops in place instead " +
                "of carrying it back to its nest.");
        }

        [Test]
        public void AIHoarder_IgnoresItemHeldByAnotherEntity()
        {
            // Production code at line 119 skips items where
            // physics.InInventory != null. Pin that behavior — without it,
            // a Magpie could "fetch" an item that's already in another
            // entity's inventory, leading to undefined ownership.
            var zone = new Zone("HoarderZone");
            var magpie = CreateCreature(zone, 5, 5, faction: "Wildlife");
            magpie.AddPart(new AIHoarderPart { Chance = 100, TargetTag = "Shiny" });

            // Owner has a coin in their inventory (modelled by setting
            // InInventory on the coin's PhysicsPart).
            var owner = CreateCreature(zone, 8, 5, faction: "Villagers");
            var coin = CreateShinyItem(zone, 7, 5, "carried-coin");
            coin.GetPart<PhysicsPart>().InInventory = owner;

            var brain = magpie.GetPart<BrainPart>();
            magpie.FireEvent(GameEvent.New("AIBored"));

            Assert.IsFalse(brain.HasGoal<GoFetchGoal>(),
                "Hoarder must skip items already in another entity's inventory. " +
                "Otherwise the Magpie would 'fetch' a coin that's already " +
                "owned, breaking ownership invariants.");
        }

        [Test]
        public void AIHoarder_IgnoresNonTakeableItems_EvenWithMatchingTag()
        {
            // Production line 112 requires physics.Takeable. A "Shiny"
            // sun (giant non-takeable cosmetic) shouldn't pull the Magpie
            // toward it.
            var zone = new Zone("HoarderZone");
            var magpie = CreateCreature(zone, 5, 5, faction: "Wildlife");
            magpie.AddPart(new AIHoarderPart { Chance = 100, TargetTag = "Shiny" });

            var fixedShiny = new Entity { BlueprintName = "Sun" };
            fixedShiny.Tags["Shiny"] = "";
            fixedShiny.AddPart(new RenderPart { DisplayName = "sun" });
            fixedShiny.AddPart(new PhysicsPart { Solid = false, Takeable = false });
            zone.AddEntity(fixedShiny, 7, 5);

            var brain = magpie.GetPart<BrainPart>();
            magpie.FireEvent(GameEvent.New("AIBored"));

            Assert.IsFalse(brain.HasGoal<GoFetchGoal>(),
                "Non-takeable Shiny items must NOT trigger a fetch — " +
                "GoFetchGoal would walk up and fail at the Pickup phase.");
        }

        // ============================================================
        // AIRetrieverPart — defaults + null-safety + the
        // returnHome=false contract + AlliesOnly=false counter-check
        // ============================================================

        [Test]
        public void AIRetriever_DefaultAlliesOnly_IsTrue()
        {
            var part = new AIRetrieverPart();
            Assert.IsTrue(part.AlliesOnly,
                "Default AlliesOnly must be true. Without it, pets would " +
                "fetch enemy-thrown projectiles by default — a UX surprise.");
        }

        [Test]
        public void AIRetriever_DefaultNoticeRadius_IsEight()
        {
            var part = new AIRetrieverPart();
            Assert.AreEqual(8, part.NoticeRadius,
                "Default NoticeRadius=8 matches the default sight radius. " +
                "Tuning anchor — preventing cross-zone teleport-fetch.");
        }

        [Test]
        public void AIRetriever_PushedGoFetchGoal_HasReturnHomeFalse()
        {
            // Counter-pin to AIHoarder's returnHome=true. Retriever
            // fetches but does NOT return — pet stops on the bone, no
            // round-trip. The TODO(pet-ux) comment in production
            // acknowledges the better UX is "drop at thrower," but
            // returnHome=false is what ships today.
            var zone = new Zone("RetrieverZone");
            var dog = CreateCreature(zone, 5, 5, faction: "Villagers");
            var brain = dog.GetPart<BrainPart>();
            dog.AddPart(new AIRetrieverPart { AlliesOnly = false, NoticeRadius = 10 });

            var bone = CreateShinyItem(zone, 7, 5, "bone");
            var thrower = CreateCreature(zone, 4, 5, faction: "Villagers");

            var ev = GameEvent.New(ItemLandedEvent.ID);
            ev.SetParameter("Item", (object)bone);
            ev.SetParameter("Thrower", (object)thrower);
            ev.SetParameter("LandingCell", (object)zone.GetCell(7, 5));
            dog.FireEvent(ev);

            var goal = brain.FindGoal<GoFetchGoal>();
            Assert.IsNotNull(goal, "Retriever must push GoFetchGoal on ItemLanded.");
            Assert.IsFalse(goal.ReturnHome,
                "Retriever MUST set returnHome=false. Counter-pin to " +
                "AIHoarder's returnHome=true — these two parts use the " +
                "SAME GoFetchGoal class but the carry-back-home semantic " +
                "differs. A refactor that unifies them would silently " +
                "break this distinction.");
        }

        [Test]
        public void AIRetriever_AlliesOnlyFalse_FetchesEnemyThrow()
        {
            // Counter-check to the existing "ignores enemy throw when
            // AlliesOnly=true" test. This one inverts: with AlliesOnly=
            // false, the retriever DOES fetch. Without this counter-check,
            // the existing test could pass vacuously (e.g., the alliance
            // check might be unconditionally returning true).
            var zone = new Zone("RetrieverZone");
            var dog = CreateCreature(zone, 5, 5, faction: "Villagers");
            var brain = dog.GetPart<BrainPart>();
            dog.AddPart(new AIRetrieverPart { AlliesOnly = false, NoticeRadius = 10 });

            var bone = CreateShinyItem(zone, 7, 5, "bone");
            var enemy = CreateCreature(zone, 4, 5, faction: "Snapjaws");

            var ev = GameEvent.New(ItemLandedEvent.ID);
            ev.SetParameter("Item", (object)bone);
            ev.SetParameter("Thrower", (object)enemy);
            ev.SetParameter("LandingCell", (object)zone.GetCell(7, 5));
            dog.FireEvent(ev);

            Assert.IsTrue(brain.HasGoal<GoFetchGoal>(),
                "With AlliesOnly=false, the Retriever must fetch even " +
                "from an enemy thrower. Counter-check — without this, " +
                "the AlliesOnly=true test could pass vacuously.");
        }

        [Test]
        public void AIRetriever_NullItemParameter_NoOpsDoesNotThrow()
        {
            // Defensive: an ItemLanded event without an Item parameter
            // must not crash. Production code line 86: `if (item == null) return true`.
            var zone = new Zone("RetrieverZone");
            var dog = CreateCreature(zone, 5, 5, faction: "Villagers");
            dog.AddPart(new AIRetrieverPart());

            var ev = GameEvent.New(ItemLandedEvent.ID);
            // No Item parameter set.
            ev.SetParameter("LandingCell", (object)zone.GetCell(5, 5));

            Assert.DoesNotThrow(() => dog.FireEvent(ev),
                "ItemLanded with null Item must early-return safely.");
        }

        [Test]
        public void AIRetriever_NullLandingCellParameter_NoOpsDoesNotThrow()
        {
            // Defensive — production line 101: `if (landingCell == null) return true`.
            var zone = new Zone("RetrieverZone");
            var dog = CreateCreature(zone, 5, 5, faction: "Villagers");
            dog.AddPart(new AIRetrieverPart { AlliesOnly = false });

            var bone = CreateShinyItem(zone, 7, 5, "bone");

            var ev = GameEvent.New(ItemLandedEvent.ID);
            ev.SetParameter("Item", (object)bone);
            // No LandingCell parameter.

            Assert.DoesNotThrow(() => dog.FireEvent(ev),
                "ItemLanded with null LandingCell must early-return safely.");
        }

        // ============================================================
        // AIFleeToShrinePart — endWhenNotFleeing regression + tuning
        // anchors + boundary cases
        // ============================================================

        [Test]
        public void AIFleeToShrine_PushedGoal_EndWhenNotFleeingFalse()
        {
            // CRITICAL regression pin. Production code lines 106-115
            // explicitly call out why endWhenNotFleeing MUST be false:
            // FleeLocationGoal default is to terminate as soon as HP
            // recovers above BrainPart.FleeThreshold (0.25). For Scribe/
            // Elder with FleeThreshold=0.8, the goal would pop almost
            // immediately because their HP is "fine" by FleeGoal's
            // standards. The semantic is "make the pilgrimage" — let
            // it run to MaxTurns or arrival.
            var zone = new Zone("ShrineZone");
            var scribe = CreateCreature(zone, 5, 5, hp: 100);
            scribe.AddPart(new AIFleeToShrinePart { FleeThreshold = 0.8f });
            scribe.GetStat("Hitpoints").BaseValue = 60; // 60% — wounded by 0.8f standard

            CreateShrine(zone, 10, 5);

            scribe.FireEvent(GameEvent.New("AIBored"));

            var brain = scribe.GetPart<BrainPart>();
            var goal = brain.FindGoal<FleeLocationGoal>();
            Assert.IsNotNull(goal, "Wounded scribe should push FleeLocationGoal.");
            Assert.IsFalse(goal.EndWhenNotFleeing,
                "AIFleeToShrine MUST push with endWhenNotFleeing=false. If " +
                "this flips to true, the goal pops immediately for Scribes/" +
                "Elders (FleeThreshold=0.8) because BrainPart.ShouldFlee " +
                "uses 0.25 as the cutoff — the goal would self-terminate " +
                "before the NPC reaches the shrine.");
        }

        [Test]
        public void AIFleeToShrine_DefaultFleeThreshold_IsZeroPointFour()
        {
            // Tuning anchor. 0.4 deliberately matches AISelfPreservation's
            // RetreatThreshold so the blueprint-order priority note
            // applies (per AIFleeToShrinePart.cs:28-31).
            var part = new AIFleeToShrinePart();
            Assert.AreEqual(0.4f, part.FleeThreshold, 0.0001f,
                "Default FleeThreshold must be 0.4 to match " +
                "AISelfPreservation's default RetreatThreshold. Otherwise " +
                "the blueprint-order priority semantics break.");
        }

        [Test]
        public void AIFleeToShrine_DefaultMaxTurns_IsFifty()
        {
            var part = new AIFleeToShrinePart();
            Assert.AreEqual(50, part.MaxTurns,
                "Default MaxTurns=50 matches the M3.3 plan spec.");
        }

        [Test]
        public void AIFleeToShrine_HpAtExactThreshold_DoesPush()
        {
            // Boundary: production code uses `fraction > FleeThreshold`
            // to early-return (don't push). At fraction == threshold,
            // the strict-greater check fails, so we DO push. Mirrors
            // the AISelfPreservation gate semantic: threshold is the
            // ceiling on "not wounded."
            var zone = new Zone("ShrineZone");
            var npc = CreateCreature(zone, 5, 5, hp: 100);
            npc.AddPart(new AIFleeToShrinePart { FleeThreshold = 0.4f });
            CreateShrine(zone, 10, 5);

            npc.GetStat("Hitpoints").BaseValue = 40; // exactly 40%

            npc.FireEvent(GameEvent.New("AIBored"));

            var brain = npc.GetPart<BrainPart>();
            Assert.IsTrue(brain.HasGoal<FleeLocationGoal>(),
                "At HP fraction == FleeThreshold, the part must still push. " +
                "The threshold is the ceiling on 'safe', not strict-less-than.");
        }

        [Test]
        public void AIFleeToShrine_HpJustAbove_DoesNotPush()
        {
            // Counter-check to the boundary test.
            var zone = new Zone("ShrineZone");
            var npc = CreateCreature(zone, 5, 5, hp: 100);
            npc.AddPart(new AIFleeToShrinePart { FleeThreshold = 0.4f });
            CreateShrine(zone, 10, 5);

            npc.GetStat("Hitpoints").BaseValue = 41; // 41/100 = 41% > 40%

            npc.FireEvent(GameEvent.New("AIBored"));

            var brain = npc.GetPart<BrainPart>();
            Assert.IsFalse(brain.HasGoal<FleeLocationGoal>(),
                "At HP strictly above FleeThreshold, no push — counter-check " +
                "to the at-threshold test.");
        }

        [Test]
        public void AIFleeToShrine_FindsNearestShrine_ChebyshevWinner()
        {
            // Counter-check that production picks the genuinely-nearest
            // sanctuary, not just the first one in registration order.
            // Existing AIBehaviorPartTests has FindsNearestShrine_WhenMultiplePresent
            // — this test pins the SAME contract from a different angle:
            // place far shrine first (registration order), near shrine
            // second; assert the near shrine wins.
            var zone = new Zone("ShrineZone");
            var npc = CreateCreature(zone, 5, 5, hp: 100);
            npc.AddPart(new AIFleeToShrinePart { FleeThreshold = 0.5f });
            npc.GetStat("Hitpoints").BaseValue = 30; // 30%, below 50%

            // Far shrine registered FIRST (insertion-order trap).
            var farShrine = CreateShrine(zone, 15, 5);   // dist 10
            var nearShrine = CreateShrine(zone, 7, 5);   // dist 2

            npc.FireEvent(GameEvent.New("AIBored"));

            var brain = npc.GetPart<BrainPart>();
            var goal = brain.FindGoal<FleeLocationGoal>();
            Assert.IsNotNull(goal);
            Assert.AreEqual(7, goal.SafeX,
                "Must target the NEAREST shrine (dist 2), not the first-" +
                "registered one (dist 10). Insertion-order tiebreak would " +
                "have picked the wrong one.");
            Assert.AreEqual(5, goal.SafeY);
        }

        // ============================================================
        // SanctuaryPart — pure-marker contract
        // ============================================================

        [Test]
        public void SanctuaryPart_Name_IsSanctuary()
        {
            // The blueprint loader maps Param "Name" to the part via
            // string match against Name. Pin it.
            var part = new SanctuaryPart();
            Assert.AreEqual("Sanctuary", part.Name);
        }

        [Test]
        public void SanctuaryPart_PassesThroughEvents_HasNoBehavior()
        {
            // Pure marker contract — SanctuaryPart must not consume or
            // mutate ANY event. AIFleeToShrine relies on the part's
            // PRESENCE on the wearer entity, not on its event handling.
            var entity = new Entity { BlueprintName = "Shrine" };
            entity.AddPart(new SanctuaryPart());

            // Try a few common event types — all must propagate through
            // unconsumed.
            foreach (var eventId in new[] { "AIBored", "TakeTurn", "TakeDamage", "Died", "EndTurn" })
            {
                var ev = GameEvent.New(eventId);
                bool result = entity.FireEvent(ev);
                Assert.IsTrue(result,
                    $"SanctuaryPart must pass through '{eventId}' event without " +
                    "consuming it. The part is a pure marker — any event handling " +
                    "would be a regression of its 'no behavior' contract.");
                Assert.IsFalse(ev.Handled,
                    $"SanctuaryPart must not set Handled=true on '{eventId}'.");
            }
        }
    }
}
