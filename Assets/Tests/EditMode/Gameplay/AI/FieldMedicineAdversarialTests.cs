using System;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FieldMedicineAdversarialTests
    {
        FieldMedicineFixture f;
        [SetUp] public void Setup() => f = new FieldMedicineFixture();
        [TearDown] public void Teardown() => f.Dispose();

        [TestCase("player")] [TestCase("dead")] [TestCase("not-creature")]
        [TestCase("missing-hp")] [TestCase("invalid-max")] [TestCase("removed")]
        [TestCase("wrong-brain-zone")] [TestCase("conversation")] [TestCase("calm")]
        [TestCase("party-follower")] [TestCase("party-target")] [TestCase("stun")] [TestCase("sleep")]
        public void IneligibleActorCannotSpendOrHealEvenThroughDirectDecision(string condition)
        {
            var actor = f.Actor(); var target = f.Threat(actor); var tonic = f.Supply(actor);
            var brain = actor.GetPart<BrainPart>();
            switch (condition)
            {
                case "player": actor.SetTag("Player"); break;
                case "dead": actor.GetStat("Hitpoints").BaseValue = 0; break;
                case "not-creature": actor.Tags.Remove("Creature"); break;
                case "missing-hp": actor.Statistics.Remove("Hitpoints"); break;
                case "invalid-max": actor.GetStat("Hitpoints").Max = 0; break;
                case "removed": f.Zone.RemoveEntity(actor); break;
                case "wrong-brain-zone": brain.CurrentZone = new Zone("foreign"); break;
                case "conversation": brain.InConversation = true; break;
                case "calm": brain.PushGoal(new NoFightGoal()); break;
                case "party-follower": brain.PartyLeader = new Entity(); break;
                case "party-target": target.GetPart<BrainPart>().PartyLeader = actor; break;
                case "stun": actor.ApplyEffect(new StunnedEffect(5)); break;
                case "sleep": actor.ApplyEffect(new AsleepByGasEffect(5)); break;
            }
            int hp = actor.GetStatValue("Hitpoints");
            Assert.IsFalse(f.Use(actor, target)); Assert.AreEqual(hp, actor.GetStatValue("Hitpoints"));
            Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(tonic)); Assert.AreEqual(0, f.Rng.Calls);
        }

        [TestCase("dead")] [TestCase("removed")] [TestCase("self")]
        [TestCase("not-creature")] [TestCase("nonhostile")]
        public void InvalidThreatNeverTriggersMedicine(string condition)
        {
            var actor = f.Actor(); var target = f.Threat(actor); var tonic = f.Supply(actor);
            switch (condition)
            {
                case "dead": target.GetStat("Hitpoints").BaseValue = 0; break;
                case "removed": f.Zone.RemoveEntity(target); break;
                case "self": target = actor; break;
                case "not-creature": target.Tags.Remove("Creature"); break;
                case "nonhostile": target = f.Actor(20, false, 13, 10); break;
            }
            Assert.IsFalse(f.Use(actor, target)); Assert.AreEqual(7, actor.GetStatValue("Hitpoints"));
            Assert.AreSame(tonic, FieldMedicineFixture.Available(actor)); Assert.AreEqual(0, f.Rng.Calls);
        }

        [TestCase("null-zone")] [TestCase("null-rng")] [TestCase("null-target")]
        [TestCase("foreign-zone")]
        public void MissingContextDoesNotUseAmbientState(string condition)
        {
            var actor = f.Actor(); var target = f.Threat(actor); var tonic = f.Supply(actor);
            var zone = condition == "foreign-zone" ? new Zone("foreign") : condition == "null-zone" ? null : f.Zone;
            Assert.IsFalse(FieldMedicineFixture.Invoke(actor, condition == "null-target" ? null : target,
                zone, condition == "null-rng" ? null : f.Rng));
            Assert.AreSame(tonic, FieldMedicineFixture.Available(actor)); Assert.AreEqual(7, actor.GetStatValue("Hitpoints"));
        }

        [TestCase("floor")] [TestCase("backlink-only")] [TestCase("foreign-owner")]
        [TestCase("equipped")] [TestCase("zero")] [TestCase("negative")]
        [TestCase("wrong-blueprint")] [TestCase("missing-payload")] [TestCase("empty-healing")]
        [TestCase("spatial-alias")]
        public void AvailabilityRequiresExactUsableCarriedMedicineWithoutScanningOtherOwners(string condition)
        {
            var actor = f.Actor(); var target = f.Threat(actor);
            var tonic = f.Supply(actor, condition == "wrong-blueprint" ? "PoisonTonic" : "HealingTonic");
            var inventory = actor.GetPart<InventoryPart>(); var physics = tonic.GetPart<PhysicsPart>();
            switch (condition)
            {
                case "floor": inventory.RemoveObject(tonic); f.Zone.AddEntity(tonic, 10, 10); break;
                case "backlink-only": inventory.Objects.Remove(tonic); break;
                case "foreign-owner": physics.InInventory = target; break;
                case "equipped": physics.Equipped = actor; break;
                case "zero": tonic.GetPart<StackerPart>().StackCount = 0; break;
                case "negative": tonic.GetPart<StackerPart>().StackCount = -1; break;
                case "missing-payload": tonic.RemovePart(tonic.GetPart<TonicPart>()); break;
                case "empty-healing": tonic.GetPart<TonicPart>().Healing = ""; break;
                case "spatial-alias": f.Zone.AddEntity(tonic, 10, 10); break;
            }
            int count = tonic.GetPart<StackerPart>().StackCount;
            Assert.IsNull(FieldMedicineFixture.Available(actor)); Assert.IsFalse(f.Use(actor, target));
            Assert.AreEqual(7, actor.GetStatValue("Hitpoints")); Assert.AreEqual(count, tonic.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(0, f.Rng.Calls);
        }

        [Test] public void UnusableFirstEntryDoesNotHideLaterActualMedicine()
        {
            var actor = f.Actor(); var target = f.Threat(actor);
            var empty = f.Supply(actor); empty.GetPart<StackerPart>().StackCount = 0;
            // Avoid the normal stack merge: distinct rendered name represents a separate real stack.
            empty.GetPart<RenderPart>().DisplayName = "empty bottle";
            var good = f.Supply(actor);
            Assert.AreSame(good, FieldMedicineFixture.Available(actor)); Assert.IsTrue(f.Use(actor, target));
            Assert.AreEqual(0, empty.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(15, actor.GetStatValue("Hitpoints"));
        }

        [TestCase(0)] [TestCase(-1)] [TestCase(101)]
        public void InvalidAuthoredThresholdDoesNotSpendMedicine(int percent)
        {
            var actor = f.Actor(); var target = f.Threat(actor); var tonic = f.Supply(actor);
            var part = actor.GetPart("FieldMedicine");
            part.GetType().GetField("UseAtOrBelowPercent").SetValue(part, percent);
            Assert.IsFalse(f.Use(actor, target)); Assert.AreEqual(7, actor.GetStatValue("Hitpoints"));
            Assert.AreSame(tonic, FieldMedicineFixture.Available(actor));
        }

        [TestCase(false)] [TestCase(true)]
        public void CommittedDeathCannotBeUndoneByPositiveHpForMedicineDecision(bool actorDied)
        {
            var actor = f.Actor(); var target = f.Threat(actor);
            var dead = actorDied ? actor : target;
            dead.Tags.Remove("Player");
            CombatSystem.HandleDeath(dead, null, f.Zone);
            Assert.IsTrue(CombatSystem.IsDeathHandled(dead));
            dead.GetStat("Hitpoints").BaseValue = actorDied ? 7 : 20;
            Assert.IsTrue(f.Zone.AddEntity(dead, actorDied ? 10 : 11, 10));
            var tonic = f.Supply(actor);
            Assert.IsFalse(f.Use(actor, target)); Assert.AreEqual(7, actor.GetStatValue("Hitpoints"));
            Assert.AreSame(tonic, FieldMedicineFixture.Available(actor));
        }

        [TestCase(false)] [TestCase(true)]
        public void ThreatMustBePhysicalWorldOwnerNotCarriedOrEquippedAlias(bool equipped)
        {
            var actor = f.Actor(); var target = f.Threat(actor); var tonic = f.Supply(actor);
            if (equipped) target.GetPart<PhysicsPart>().Equipped = actor;
            else target.GetPart<PhysicsPart>().InInventory = actor;
            Assert.IsFalse(f.Use(actor, target)); Assert.AreEqual(7, actor.GetStatValue("Hitpoints"));
            Assert.AreSame(tonic, FieldMedicineFixture.Available(actor));
        }

        [TestCase(1, false, 0)] [TestCase(2, false, 1)] [TestCase(1, true, 3)]
        public void UsedDiagnosticIdentifiesActualThreatSupplyAndAllRemainingOwnedUnits(int count, bool secondStack, int remaining)
        {
            var actor = f.Actor(); var target = f.Threat(actor); var tonic = f.Supply(actor, count: count);
            if (secondStack)
            {
                tonic.GetPart<RenderPart>().DisplayName = "first healing bottle";
                f.Supply(actor, count: 3);
            }
            bool wasEnabled = Diag.IsChannelEnabled("ai");
            try
            {
                Diag.SetChannel("ai", true);
                Assert.IsTrue(f.Use(actor, target));
                var records = DiagQuery.Apply(new DiagQuery.Filter
                    { Category = "ai", Kind = "FieldMedicineUsed", Actor = actor.ID }).Records;
                Assert.AreEqual(1, records.Count); Assert.AreEqual(actor.ID, records[0].ActorId);
                Assert.AreEqual(tonic.ID, records[0].TargetId);
                StringAssert.Contains("\"threatId\":\"" + target.ID + "\"", records[0].PayloadJson);
                StringAssert.Contains("\"remainingUnits\":" + remaining, records[0].PayloadJson);
                StringAssert.Contains("\"hpBefore\":7", records[0].PayloadJson);
                StringAssert.Contains("\"hpAfter\":15", records[0].PayloadJson);
            }
            finally { Diag.SetChannel("ai", wasEnabled); }
        }

        [Test] public void OrdinaryExaminePreservesAuthoredProseAndUpdatesUnspentMedicineDescription()
        {
            var actor = f.Actor(); var target = f.Threat(actor); f.Supply(actor);
            var examine = new ExaminablePart { Text = "Bottles nest between shale plates." }; actor.AddPart(examine);
            string before = examine.BuildExamineLine();
            StringAssert.Contains(examine.Text, before); StringAssert.Contains("40%", before);
            StringAssert.Contains("healing tonic", before);
            Assert.IsTrue(f.Use(actor, target));
            string after = examine.BuildWorldExamineLine(f.Zone, f.Zone.GetEntityCell(actor), target);
            StringAssert.Contains(examine.Text, after); StringAssert.Contains("bottle harness is empty", after);
        }

        [Test] public void DescribeReadsRealStockAndThresholdWithoutConsumingOrDrawingRandomness()
        {
            var actor = f.Actor(); var target = f.Threat(actor); var tonic = f.Supply(actor);
            var part = actor.GetPart("FieldMedicine"); var method = part.GetType().GetMethod("Describe");
            Assert.NotNull(method);
            string full = (string)method.Invoke(part, null);
            StringAssert.Contains("40%", full); StringAssert.Contains("healing tonic", full.ToLowerInvariant());
            StringAssert.Contains("turn", full.ToLowerInvariant());
            Assert.AreSame(tonic, FieldMedicineFixture.Available(actor)); Assert.AreEqual(0, f.Rng.Calls);
            Assert.IsTrue(f.Use(actor, target));
            string spent = (string)method.Invoke(part, null);
            StringAssert.Contains("empty", spent.ToLowerInvariant()); Assert.AreNotEqual(full, spent);
            Assert.AreEqual(4, f.Rng.Calls);
        }
    }
}
