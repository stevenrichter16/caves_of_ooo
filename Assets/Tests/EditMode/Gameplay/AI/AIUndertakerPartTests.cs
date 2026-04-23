using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M5.3 — <see cref="AIUndertakerPart"/>. AIBehaviorPart subclass that
    /// responds to <see cref="AIBoredEvent"/> by finding a corpse + graveyard
    /// and pushing <see cref="DisposeOfCorpseGoal"/>.
    ///
    /// CoO-adapts Qud's container-side <c>DepositCorpses</c> to a
    /// NPC-side behaviour part (see <c>Docs/QUD-PARITY.md</c> §M5.3 design
    /// table for rationale).
    /// </summary>
    [TestFixture]
    public class AIUndertakerPartTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // ====================================================================
        // Manual-construction helpers (isolate the part from blueprint loading).
        // ====================================================================

        private Entity CreateUndertaker(Zone zone, int x, int y, int chance = 100)
        {
            var entity = new Entity { BlueprintName = "Undertaker", ID = "UT-1" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 14, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 12, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "undertaker" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new InventoryPart { MaxWeight = 150 });
            var brain = new BrainPart { CurrentZone = zone, Rng = new System.Random(0) };
            entity.AddPart(brain);
            entity.AddPart(new AIUndertakerPart { Chance = chance });
            zone.AddEntity(entity, x, y);
            brain.StartingCellX = x;
            brain.StartingCellY = y;
            return entity;
        }

        private Entity CreateCorpse(Zone zone, int x, int y, int weight = 10, string id = "Corpse-1")
        {
            var corpse = new Entity { BlueprintName = "SnapjawCorpse", ID = id };
            corpse.Tags["Corpse"] = "";
            corpse.AddPart(new RenderPart { DisplayName = "snapjaw corpse", RenderString = "%", ColorString = "&r" });
            corpse.AddPart(new PhysicsPart { Takeable = true, Weight = weight });
            zone.AddEntity(corpse, x, y);
            return corpse;
        }

        private Entity CreateGraveyard(Zone zone, int x, int y)
        {
            var grave = new Entity { BlueprintName = "Graveyard", ID = "Grave-1" };
            grave.Tags["Graveyard"] = "";
            grave.AddPart(new RenderPart { DisplayName = "graveyard", RenderString = "+", ColorString = "&K" });
            grave.AddPart(new PhysicsPart { Solid = true });
            grave.AddPart(new ContainerPart { MaxItems = -1 });
            zone.AddEntity(grave, x, y);
            return grave;
        }

        // ====================================================================
        // Core pipeline
        // ====================================================================

        [Test]
        public void AIUndertaker_WithCorpseAndGraveyard_PushesDisposeOfCorpseGoal()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 15, 10);
            CreateGraveyard(zone, 20, 10);

            AIBoredEvent.Check(npc);

            Assert.IsTrue(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "AIUndertaker with a corpse + graveyard in zone should push DisposeOfCorpseGoal on bored.");
            Assert.AreEqual(50, corpse.GetIntProperty("DepositCorpsesReserve", 0),
                "Corpse should be reserved with DepositCorpsesReserve=50 (mirrors Qud DepositCorpses.cs line 115).");
        }

        [Test]
        public void AIUndertaker_NoGraveyard_DoesNothing()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            CreateCorpse(zone, 15, 10);
            // No graveyard in zone.

            AIBoredEvent.Check(npc);

            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "With no graveyard in zone, AIUndertaker must no-op (event passes through).");
        }

        [Test]
        public void AIUndertaker_NoCorpse_DoesNothing()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            CreateGraveyard(zone, 20, 10);
            // No corpse in zone.

            AIBoredEvent.Check(npc);

            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "With no corpse in zone, AIUndertaker must no-op.");
        }

        // ====================================================================
        // Reservation / multi-NPC race
        // ====================================================================

        [Test]
        public void AIUndertaker_ReservedCorpse_IsSkipped()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 15, 10);
            CreateGraveyard(zone, 20, 10);

            // Another undertaker's in-flight goal has already claimed this corpse.
            corpse.SetIntProperty("DepositCorpsesReserve", 50);

            AIBoredEvent.Check(npc);

            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "A reserved corpse must be skipped so two undertakers don't fight over one body.");
        }

        [Test]
        public void AIUndertaker_TwoNPCs_SecondSkipsClaimedCorpse()
        {
            // Integration: UT-A claims the only corpse; UT-B's bored-check
            // must then find no available corpse and no-op.
            var zone = new Zone("TestZone");
            var utA = CreateUndertaker(zone, 10, 10);
            // Second undertaker — fresh ID so zone entity dict doesn't collide.
            var utB = new Entity { BlueprintName = "Undertaker", ID = "UT-2" };
            utB.Tags["Creature"] = "";
            utB.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 14, Min = 1, Max = 50 };
            utB.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            utB.AddPart(new RenderPart { DisplayName = "undertaker" });
            utB.AddPart(new PhysicsPart { Solid = true });
            utB.AddPart(new InventoryPart { MaxWeight = 150 });
            var brainB = new BrainPart { CurrentZone = zone, Rng = new System.Random(0) };
            utB.AddPart(brainB);
            utB.AddPart(new AIUndertakerPart { Chance = 100 });
            zone.AddEntity(utB, 12, 10);

            CreateCorpse(zone, 15, 10);
            CreateGraveyard(zone, 20, 10);

            AIBoredEvent.Check(utA);
            Assert.IsTrue(utA.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "First undertaker should claim the only corpse.");

            AIBoredEvent.Check(utB);
            Assert.IsFalse(brainB.HasGoal<DisposeOfCorpseGoal>(),
                "Second undertaker should find nothing to claim — corpse already reserved.");
        }

        // ====================================================================
        // Skip conditions
        // ====================================================================

        [Test]
        public void AIUndertaker_NoHaulingTag_SkippedEntirely()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            CreateCorpse(zone, 15, 10);
            CreateGraveyard(zone, 20, 10);
            // Opt-out tag — Qud parity with DepositCorpses.cs line 75.
            npc.SetTag("NoHauling");

            AIBoredEvent.Check(npc);

            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "NoHauling-tagged NPC must never push a DisposeOfCorpseGoal.");
        }

        [Test]
        public void AIUndertaker_AlreadyHasDisposeGoal_Idempotent()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse1 = CreateCorpse(zone, 15, 10);
            CreateGraveyard(zone, 20, 10);

            // Simulate an already-in-flight goal.
            var brain = npc.GetPart<BrainPart>();
            brain.PushGoal(new DisposeOfCorpseGoal(corpse1, new Entity()));

            int goalCountBefore = brain.GoalCount;
            AIBoredEvent.Check(npc);
            int goalCountAfter = brain.GoalCount;

            Assert.AreEqual(goalCountBefore, goalCountAfter,
                "AIUndertaker must NOT stack a second DisposeOfCorpseGoal when one is already on the stack.");
        }

        [Test]
        public void AIUndertaker_CorpseTooHeavy_IsSkipped()
        {
            // NPC with Str=4 → max carry = 60. Corpse weight=100 → would
            // overburden. AIUndertaker should skip this corpse.
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            // Override default Str=14 → 4.
            npc.Statistics["Strength"].BaseValue = 4;
            CreateCorpse(zone, 15, 10, weight: 100);
            CreateGraveyard(zone, 20, 10);

            AIBoredEvent.Check(npc);

            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "A corpse whose weight exceeds the NPC's max carry should be skipped " +
                "(Qud parity: DepositCorpses.cs line 102 WouldBeOverburdened check).");
        }

        // ====================================================================
        // Blueprint wiring (integration with Objects.json)
        // ====================================================================

        [Test]
        public void UndertakerBlueprint_HasAIUndertakerPart()
        {
            var factory = new EntityFactory();
            var text = Resources.Load<TextAsset>("Content/Blueprints/Objects");
            Assert.IsNotNull(text, "Real Objects.json must be loadable via Resources.");
            factory.LoadBlueprints(text.text);

            var undertaker = factory.CreateEntity("Undertaker");
            Assert.IsNotNull(undertaker, "Undertaker blueprint must exist and be instantiable.");
            Assert.IsNotNull(undertaker.GetPart<AIUndertakerPart>(),
                "Undertaker blueprint must attach AIUndertakerPart (ships AI behaviour by default).");
        }

        [Test]
        public void GraveyardBlueprint_HasUnlimitedContainer_And_GraveyardTag()
        {
            var factory = new EntityFactory();
            var text = Resources.Load<TextAsset>("Content/Blueprints/Objects");
            Assert.IsNotNull(text);
            factory.LoadBlueprints(text.text);

            var grave = factory.CreateEntity("Graveyard");
            Assert.IsNotNull(grave, "Graveyard blueprint must exist and be instantiable.");
            Assert.IsTrue(grave.HasTag("Graveyard"),
                "Graveyard blueprint must carry the Graveyard tag so AIUndertaker can find it.");
            var container = grave.GetPart<ContainerPart>();
            Assert.IsNotNull(container, "Graveyard must have ContainerPart.");
            Assert.AreEqual(-1, container.MaxItems,
                "Graveyard's ContainerPart must have MaxItems=-1 (unlimited capacity — Qud parity).");
        }
    }
}
