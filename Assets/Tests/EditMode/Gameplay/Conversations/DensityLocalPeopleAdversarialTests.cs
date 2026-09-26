using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class DensityLocalPeopleAdversarialTests
    {
        DensityLootTestScope scope;
        bool oldChannel;
        EntityFactory Factory => scope.Factory;
        [SetUp] public void Setup() { scope = new DensityLootTestScope(); oldChannel = Diag.IsChannelEnabled("worldgen"); Diag.SetChannel("worldgen", true); }
        [TearDown] public void Cleanup() { Diag.SetChannel("worldgen", oldChannel); scope.Dispose(); }
        OverworldZoneManager Manager(int seed = 801) => OverworldZoneManager.CreateDetached(Factory, seed);
        Entity Add(Zone zone, string blueprint = "Villager", int x = 10, int y = 10)
        { var actor = Factory.CreateEntity(blueprint); Assert.IsTrue(zone.AddEntity(actor, x, y)); return actor; }
        static string Label(Entity actor) => actor.GetPart<RenderPart>().DisplayName;
        static bool Named(Entity actor) => actor.Properties.ContainsKey("LocalPersonalName");
        static IReadOnlyList<Diag.Entry> Records(string kind, Entity actor)
            => DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = kind, Actor = actor.ID }).Records;

        [TestCase("custom-label")] [TestCase("custom-id")] [TestCase("quest-conversation")]
        [TestCase("quest-owner")] [TestCase("unknown-culture")]
        public void Adversarial_AuthoredOverridesAndUnknownCultureAreNotRenamed(string condition)
        {
            var manager = Manager(); var zone = new Zone("Overworld.10.10.0"); var actor = Add(zone);
            switch (condition)
            {
                case "custom-label": actor.GetPart<RenderPart>().DisplayName = "An authored person"; break;
                case "custom-id": actor.ID = "canonical-person"; break;
                case "quest-conversation": actor.GetPart<ConversationPart>().ConversationID = "RootBeerGuy_Quest"; break;
                case "quest-owner": actor.AddPart(new CavesOfOoo.Storylets.QuestBeaconPart { Quest = "Example" }); break;
                case "unknown-culture": actor.Tags["Faction"] = "GlassblownRemnant"; break;
            }
            string label = Label(actor), id = actor.ID; LocalPeople.Apply(zone, manager);
            Assert.IsFalse(Named(actor)); Assert.AreEqual(label, Label(actor)); Assert.AreEqual(id, actor.ID);
            var control = Add(zone, x: 11); LocalPeople.Apply(zone, manager); Assert.IsTrue(Named(control));
        }

        [Test]
        public void Adversarial_FiniteNamePoolExhaustionIsExplicitAndDoesNotDuplicatePersonalNames()
        {
            var manager = Manager(); var zone = new Zone("Overworld.10.10.0"); var actors = new List<Entity>();
            for (int i = 0; i < 70; i++) actors.Add(Add(zone, x: 2 + i % 20, y: 2 + i / 20));
            LocalPeople.Apply(zone, manager); var named = actors.Where(Named).ToArray();
            Assert.AreEqual(64, named.Length); Assert.AreEqual(64, named.Select(a => a.GetProperty("LocalPersonalName")).Distinct().Count());
            foreach (var actor in actors.Where(a => !Named(a)))
            { Assert.AreEqual("villager", Label(actor)); Assert.IsTrue(Records("LocalResidentNameRejected", actor).Any(r => r.PayloadJson.Contains("name-pool-exhausted"))); }
        }

        [Test]
        public void Adversarial_CustomPersonReservesTheirNameBeforeGenericRolesAreNamed()
        {
            var manager = Manager(); var first = new Zone("Overworld.10.10.0"); var original = Add(first);
            LocalPeople.Apply(first, manager); string reserved = original.GetProperty("LocalPersonalName");
            var otherManager = Manager(); var other = new Zone(first.ZoneID); var generic = Add(other); var authored = Add(other, x: 11);
            authored.GetPart<RenderPart>().DisplayName = reserved; LocalPeople.Apply(other, otherManager);
            Assert.AreNotEqual(reserved, generic.GetProperty("LocalPersonalName")); Assert.AreEqual(reserved, Label(authored));
        }

        [Test]
        public void Adversarial_RepeatedFreshApplicationPreservesNameOwnerAndItems()
        {
            var manager = Manager(); var zone = new Zone("Overworld.10.10.0"); var actor = Add(zone); var unique = Add(zone, "Mogu", 12);
            LocalPeople.Apply(zone, manager); string label = Label(actor); var props = actor.Properties.ToArray();
            LocalPeople.Apply(zone, manager); Assert.AreEqual(label, Label(actor)); CollectionAssert.AreEquivalent(props, actor.Properties);
            Assert.That(zone.GetAllEntities(), Does.Contain(unique));
            Assert.AreEqual(1, LocalPeople.BindForSave(manager, null).GetPart<LocalPeopleLedgerPart>().Count);
        }

        [Test]
        public void Adversarial_NamingDoesNotAdvanceCombatOrLootRandomStreams()
        {
            var manager = Manager(); var zone = new Zone("Overworld.10.10.0"); Add(zone);
            LoadoutPart.Rng = new System.Random(332); LootDropSystem.Rng = new System.Random(415);
            LocalPeople.Apply(zone, manager);
            Assert.AreEqual(new System.Random(332).Next(), LoadoutPart.Rng.Next());
            Assert.AreEqual(new System.Random(415).Next(), LootDropSystem.Rng.Next());
        }

        [Test]
        public void Adversarial_SameWorldSeedUsesInvariantNumericFormatting()
        {
            var before = CultureInfo.CurrentCulture;
            try
            {
                var altered = (CultureInfo)CultureInfo.InvariantCulture.Clone(); altered.NumberFormat.NegativeSign = "negative";
                string Name(CultureInfo culture)
                { CultureInfo.CurrentCulture = culture; var zone = new Zone("Overworld.10.10.0"); var actor = Add(zone); LocalPeople.Apply(zone, Manager(-33)); return Label(actor); }
                Assert.AreEqual(Name(CultureInfo.InvariantCulture), Name(altered));
            }
            finally { CultureInfo.CurrentCulture = before; }
        }

        [TestCase(false)] [TestCase(true)]
        public void Adversarial_ReusedFactoryIdCannotImpersonateAClaimedPerson(bool sameZone)
        {
            var manager = Manager(); var original = new Zone("Overworld.1.1.0"); var owner = Add(original, "Mogu"); owner.ID = "42";
            LocalPeople.Apply(original, manager); manager.SetActiveZone(original); manager.UnloadZone(original.ZoneID);
            var next = new Zone(sameZone ? original.ZoneID : "Overworld.2.1.0"); var clone = Add(next, "Mogu"); clone.ID = owner.ID;
            LocalPeople.Apply(next, manager); Assert.IsEmpty(next.GetAllEntities());
            Assert.That(original.GetAllEntities(), Does.Contain(owner));
        }

        [Test]
        public void Adversarial_NoActorPlacementMakesNoClaimAndRepeatableChoirRolesStayRepeatable()
        {
            var manager = Manager(); var empty = new Zone("Overworld.1.1.0"); LocalPeople.Apply(empty, manager);
            Assert.IsNull(LocalPeople.BindForSave(manager, null));
            var next = new Zone("Overworld.2.1.0"); Add(next, "Mogu"); Add(next, "ChoirTendril", 11); Add(next, "ChoirTendril", 12); Add(next, "EncasedElder", 13);
            LocalPeople.Apply(next, manager); Assert.AreEqual(4, next.GetAllEntities().Count());
        }

        [Test]
        public void Adversarial_SaveCannotClobberAnotherWorldsLedger()
        {
            var a = Manager(); var za = new Zone("Overworld.1.1.0"); Add(za, "Mogu"); LocalPeople.Apply(za, a);
            var b = Manager(); var zb = new Zone("Overworld.1.1.0"); Add(zb, "Grib"); LocalPeople.Apply(zb, b);
            var world = LocalPeople.BindForSave(a, null); var original = world.GetPart<LocalPeopleLedgerPart>();
            Assert.Throws<InvalidOperationException>(() => LocalPeople.BindForSave(b, world));
            Assert.AreSame(original, world.GetPart<LocalPeopleLedgerPart>()); Assert.AreEqual(1, original.Count);
            Assert.Throws<InvalidOperationException>(() => LocalPeople.Restore(b, world));
            Assert.AreEqual(1, LocalPeople.BindForSave(b, null).GetPart<LocalPeopleLedgerPart>().Count);
        }

        [Test]
        public void Adversarial_LedgerSaveOrderIsDeterministicAndClaimsRoundTrip()
        {
            byte[] Build(bool reverse)
            {
                var manager = Manager(); var zone = new Zone("Overworld.1.1.0");
                foreach (var bp in reverse ? LocalPeople.UniqueBlueprints.Reverse() : LocalPeople.UniqueBlueprints)
                { var actor = Add(zone, bp); actor.ID = "owner-" + bp; }
                LocalPeople.Apply(zone, manager); var ledger = LocalPeople.BindForSave(manager, null).GetPart<LocalPeopleLedgerPart>();
                using (var stream = new MemoryStream()) { ledger.Save(new SaveWriter(stream)); return stream.ToArray(); }
            }
            byte[] bytes = Build(false); CollectionAssert.AreEqual(bytes, Build(true));
            var loaded = new LocalPeopleLedgerPart(); loaded.Load(new SaveReader(new MemoryStream(bytes), Factory)); Assert.AreEqual(5, loaded.Count);
        }

        [TestCase("version")] [TestCase("negative-count")] [TestCase("too-many")] [TestCase("unknown-person")]
        [TestCase("duplicate")] [TestCase("empty-id")] [TestCase("empty-zone")] [TestCase("long-id")]
        public void Adversarial_MalformedLedgerIsRejectedWithoutPublishingPartialClaims(string condition)
        {
            var manager = Manager(); var zone = new Zone("Overworld.1.1.0"); Add(zone, "Mogu"); LocalPeople.Apply(zone, manager);
            var ledger = LocalPeople.BindForSave(manager, null).GetPart<LocalPeopleLedgerPart>();
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); writer.Write(condition == "version" ? 17 : 1);
                int count = condition == "negative-count" ? -1 : condition == "too-many" ? 6 : condition == "duplicate" ? 2 : 1;
                writer.Write(count);
                for (int i = 0; i < Math.Max(0, Math.Min(count, 5)); i++)
                {
                    writer.WriteString(condition == "unknown-person" ? "Villager" : "Grib");
                    writer.WriteString(condition == "empty-id" ? "" : condition == "long-id" ? new string('x', 129) : "saved-id");
                    writer.WriteString(condition == "empty-zone" ? "" : zone.ZoneID);
                }
                stream.Position = 0; Assert.Throws<InvalidDataException>(() => ledger.Load(new SaveReader(stream, Factory)));
                Assert.AreEqual(1, ledger.Count);
            }
            // The original owner is still claimed after a failed decode.
            var attempt = new Zone("Overworld.2.1.0"); Add(attempt, "Mogu"); LocalPeople.Apply(attempt, manager); Assert.IsEmpty(attempt.GetAllEntities());
        }

        [Test]
        public void Adversarial_DiagnosticsIdentifyNamedAndSuppressedOwnersAndCanBeDisabled()
        {
            var manager = Manager(); var zone = new Zone("Overworld.10.10.0"); var actor = Add(zone); Add(zone, "Mogu", 12);
            actor.ID = Guid.NewGuid().ToString("N");
            LocalPeople.Apply(zone, manager); var record = Records("LocalResidentNamed", actor).Single(); StringAssert.Contains(zone.ZoneID, record.PayloadJson);
            var next = new Zone("Overworld.2.1.0"); var clone = Add(next, "Mogu"); clone.ID = Guid.NewGuid().ToString("N"); LocalPeople.Apply(next, manager);
            Assert.IsTrue(Records("UniqueResidentSuppressed", clone).Any(r => r.PayloadJson.Contains("already-claimed")));
            Diag.SetChannel("worldgen", false); var quiet = Add(next, x: 12); quiet.ID = Guid.NewGuid().ToString("N"); LocalPeople.Apply(next, manager);
            Assert.IsTrue(Named(quiet)); Assert.IsEmpty(Records("LocalResidentNamed", quiet));
        }

        [TestCase("detached")] [TestCase("dead-speaker")] [TestCase("dead-listener")]
        [TestCase("foreign-listener")] [TestCase("hostile")] [TestCase("death-handled-listener")]
        public void Adversarial_LocalSpeechCannotBorrowMissingOrForeignAuthority(string condition)
        {
            var manager = Manager(); var zone = manager.GetZone("Overworld.10.10.0"); var speaker = zone.GetAllEntities().First(e => e.BlueprintName == "Villager" && e.GetPart<ConversationPart>()?.ConversationID == "Villager_1");
            var player = Add(zone, "Player", 2, 2);
            Assert.That(LocalPeople.DescribeConversation(speaker, player, "Villager_1", "Start", "fallback"), Does.Contain("Sill"));
            switch (condition)
            {
                case "detached": zone.RemoveEntity(speaker); break;
                case "dead-speaker": speaker.Statistics["Hitpoints"].BaseValue = 0; break;
                case "dead-listener": player.Statistics["Hitpoints"].BaseValue = 0; break;
                case "death-handled-listener": player.SetTag("_DeathHandled"); break;
                case "foreign-listener": zone.RemoveEntity(player); new Zone("elsewhere").AddEntity(player, 2, 2); break;
                case "hostile": speaker.GetPart<BrainPart>().SetPersonallyHostile(player); break;
            }
            Assert.AreEqual("fallback", LocalPeople.DescribeConversation(speaker, player, "Villager_1", "Start", "fallback"));
        }

        [Test]
        public void Adversarial_NoLivingContactsCannotFabricateOneOrGenerateAnotherZone()
        {
            var manager = Manager(); var zone = manager.GetZone("Overworld.10.10.0"); var speaker = zone.GetAllEntities().First(e => e.BlueprintName == "Villager" && e.GetPart<ConversationPart>()?.ConversationID == "Villager_1");
            var player = Add(zone, "Player", 2, 2);
            foreach (var actor in zone.GetAllEntities().Where(e => new[] { "Scribe", "Innkeeper", "WellKeeper", "Merchant" }.Contains(e.BlueprintName)).ToArray())
                actor.Statistics["Hitpoints"].BaseValue = 0;
            int zones = manager.CachedZoneCount, states = manager.SettlementManager.GetAllSettlementsSnapshot().Count;
            Assert.AreEqual("I have no local contact to send you to just now.", LocalPeople.DescribeConversation(speaker, player, "Villager_1", "PassingThrough", "fallback"));
            Assert.AreEqual(zones, manager.CachedZoneCount); Assert.AreEqual(states, manager.SettlementManager.GetAllSettlementsSnapshot().Count);
        }
    }
}
