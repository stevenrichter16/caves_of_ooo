using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Player-facing failure and journal contracts, independent of transactional
    // correctness gates. Staged outside Assets until the actual RED run.
    public sealed class RegionalSituationUsabilityTests
    {
        private const int Seed=64;
        private HotbarSaveFixture scope;
        private EntityFactory factory;
        private OverworldZoneManager manager;
        private Entity player;
        private Zone playerZone;
        [SetUp] public void SetUp()
        {
            scope=new HotbarSaveFixture(false,false); playerZone=null;
            CinderholdCompositionTests.LoadLoot(); factory=GrovelandsCompositionTests.Factory();
            manager=OverworldZoneManager.CreateDetached(factory,Seed); player=factory.CreateEntity("Player");
            player.GetPart<InventoryPart>().MaxWeight=-1;
            StoryletPart.Current=new StoryletPart(); StoryletPart.LocalPlayer=player;
            NarrativeStatePart.Current=new NarrativeStatePart(); MorrowfastContent.EnsureRegistered();
        }
        [TearDown] public void TearDown()
        {
            ConversationManager.EndConversation(); scope?.Dispose(); LootTableRegistry.ResetForTests();
        }

        // A visible failed action must explain the actual quantity and native
        // item name. The opposite case proves that this is actionable guidance.
        [Test] public void OneGrainExplainsTwoUnitRequirementAndSecondUnitThenWorks()
        {
            var b=Bind("gantry-grain"); StandBy(b.zone,b.recipient);
            Assert.IsTrue(b.request.TryAct(player,b.zone,"read")); Carry("Emberwheat",1); MessageLog.Clear();
            Assert.IsFalse(b.request.TryAct(player,b.zone,"deliver"));
            string text=string.Join(" ",MessageLog.GetMessages()).ToLowerInvariant();
            Assert.IsNotEmpty(text,"The refusal must reach the player, not only diagnostics.");
            StringAssert.Contains(Display("Emberwheat"),text);
            Assert.IsTrue(text.Contains("2")||text.Contains("two"),"State the complete required quantity.");
            Assert.AreEqual(1,Units(player,"Emberwheat")); Assert.IsFalse(b.request.Completed);
            Carry("Emberwheat",1); Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));
        }

        // Ordinary stock cannot satisfy a marked recovery; the refusal must say
        // which physical parcel to recover and where to look, preserving goods.
        [Test] public void WrongCargoExplainsSealedConsignmentAndActualSource()
        {
            foreach(string id in new[]{"sumphold-oil","wellmeet-filters"})
            {
                var d=RegionalSituations.Find(id); var b=Bind(id); StandBy(b.zone,b.recipient);
                Assert.IsTrue(b.request.TryAct(player,b.zone,"read")); Carry(d.ItemBlueprint,d.ItemCount); Carry("Sack",1);
                int before=Units(player,d.ItemBlueprint); MessageLog.Clear();
                Assert.IsFalse(b.request.TryAct(player,b.zone,"deliver"));
                string text=string.Join(" ",MessageLog.GetMessages()).ToLowerInvariant();
                Assert.IsNotEmpty(text); StringAssert.Contains("sealed",text);
                Assert.IsTrue(text.Contains("consignment")||text.Contains("cargo")||text.Contains("parcel"));
                var at=WorldMap.FromZoneID(d.SourceZoneId);
                StringAssert.Contains(at.x+","+at.y,text.Replace(" ",""));
                Assert.AreEqual(before,Units(player,d.ItemBlueprint)); Assert.IsFalse(b.request.Completed);
            }
        }

        // Extraction is optional. Once the one-shot local source is gone the
        // request stays playable with outside goods, but must not promise a vein.
        [Test] public void RemovedSupplySourceGetsTruthfulNoteAndBroughtGoodsStillComplete()
        {
            var d=RegionalSituations.Find("morrowfast-iron"); var source=manager.GetZone(d.SourceZoneId);
            Assert.IsTrue(source.RemoveEntity(Source(source,d.Id)));
            var b=Bind(d.Id); StandBy(b.zone,b.recipient); MessageLog.Clear();
            Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));
            string note=string.Join(" ",RegionalSituationNotes.Read(player)).ToLowerInvariant();
            Assert.IsTrue(note.Contains("exhausted")||note.Contains("unavailable")||note.Contains("gone")||note.Contains("no longer"),
                "The note must disclose that the recorded local source is gone.");
            Assert.IsTrue(note.Contains("bought")||note.Contains("traded")||note.Contains("already carried"));
            Assert.IsFalse(source.GetAllEntities().Any(e=>e.ID==RegionalSituations.SourceId(d,Seed)));
            Carry(d.ItemBlueprint,d.ItemCount); Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));
        }

        // Coordinates complement actual names rather than blueprint identifiers.
        // Display strings are sourced from real native owners/items, not invented
        // spelling. A generic merchant's own native display name is acceptable.
        [Test] public void NotesNameNativeGoodsActualRecipientAndDestinationTown()
        {
            foreach(var d in RegionalSituations.Definitions)
            {
                var b=Bind(d.Id); StandBy(b.zone,b.recipient);
                Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));
                string note=RegionalSituationNotes.Read(player).Single(n=>n.StartsWith(d.Title,StringComparison.Ordinal));
                string lower=note.ToLowerInvariant();
                StringAssert.Contains(b.recipient.GetDisplayName().ToLowerInvariant(),lower,d.Id+" recipient");
                var at=WorldMap.FromZoneID(d.RecipientZoneId); var town=manager.WorldMap.GetPOI(at.x,at.y);
                StringAssert.Contains(town.Name.ToLowerInvariant(),lower,d.Id+" town");
                StringAssert.Contains(Display(d.RewardBlueprint),lower,d.Id+" reward");
                if(d.Kind==RegionalSituationKind.Supply)StringAssert.Contains(Display(d.ItemBlueprint),lower,d.Id+" goods");
                Assert.IsFalse(note.Contains(d.RewardBlueprint),"Do not show internal reward blueprint IDs.");
            }
        }

        // Transaction failure after the Part handled delivery rolls back real
        // payment. A success message/diagnostic must not survive that rollback.
        // The successful control pins that committed work still gets its receipt.
        [TestCase(false)][TestCase(true)]
        public void PostActionFailureCannotPublishSuccessfulDeliveryReceipt(bool throws)
        {
            var b=Bind("gantry-grain"); StandBy(b.zone,b.recipient);
            Assert.IsTrue(b.request.TryAct(player,b.zone,"read")); Carry("Emberwheat",2);
            var hook=new ReceiptProbe{Throw=throws}; player.AddPart(hook);
            MessageLog.Clear(); Diag.ResetAll(); Diag.SetChannel("quest",true);
            var result=InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(b.recipient,"RegionalRequest:deliver"),player,b.zone);
            Assert.AreEqual(!throws,result.Success); Assert.AreEqual(1,hook.Calls);
            int applied=DiagQuery.Count(new DiagQuery.Filter{Category="quest",Kind="RegionalRequestApplied",Actor=player.ID,Target=b.recipient.ID}).Count;
            Assert.AreEqual(throws?0:1,applied,"Only committed actions may publish an applied receipt.");
            bool announced=MessageLog.GetMessages().Any(m=>m.IndexOf("Delivered:",StringComparison.OrdinalIgnoreCase)>=0);
            Assert.AreEqual(!throws,announced,"The player must not be told that a rolled-back delivery succeeded.");
            Assert.AreEqual(throws?2:0,Units(player,"Emberwheat")); Assert.AreEqual(!throws,b.request.Completed);
        }
        private sealed class ReceiptProbe:Part
        {
            public override string Name=>"RegionalReceiptProbe";
            public bool Throw; public int Calls;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID=="AfterInventoryAction") { Calls++; if(Throw)throw new InvalidOperationException("Regional receipt probe"); }
                return true;
            }
        }
        private string Display(string blueprint)=>factory.CreateEntity(blueprint).GetPart<RenderPart>().DisplayName.ToLowerInvariant();
        private (Zone zone, Entity recipient, RegionalRequestPart request) Bind(string id)
        {
            var d = RegionalSituations.Find(id); Assert.NotNull(d, id);
            var z = manager.GetZone(d.RecipientZoneId);
            var matches = z.GetAllEntities().Where(e => e.GetPart<RegionalRequestPart>()?.DefinitionId == id).ToArray();
            Assert.AreEqual(1, matches.Length, "Native recipient binding " + id);
            return (z, matches[0], matches[0].GetPart<RegionalRequestPart>());
        }

        private static Entity Source(Zone zone, string id)
        {
            string sourceId = RegionalSituations.SourceId(RegionalSituations.Find(id), Seed);
            var owners = zone.GetAllEntities().Where(e => e.ID == sourceId).ToArray();
            Assert.AreEqual(1, owners.Length, "Actual generated primary source for " + id);
            return owners[0];
        }

        private void StandBy(Zone zone, Entity owner)
        {
            var at = zone.GetEntityCell(owner); Assert.NotNull(at);
            var standing = CinderholdCompositionTests.Neighbors(at.X, at.Y)
                .Select(p => zone.GetCell(p.x, p.y)).FirstOrDefault(c => c != null && !c.BlocksMovement());
            Assert.NotNull(standing, "Native recipient needs a standing frontage.");
            MovePlayer(zone, standing.X, standing.Y);
        }

        private void MovePlayer(Zone zone, int x, int y)
        {
            if (playerZone != null) Assert.IsTrue(playerZone.RemoveEntity(player));
            Assert.IsTrue(zone.AddEntity(player, x, y)); playerZone = zone;
            manager.SetActiveZone(zone); SettlementRuntime.ActiveZone = zone;
        }

        private void Carry(string blueprint, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = factory.CreateEntity(blueprint); Assert.NotNull(item, blueprint);
                Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(item), blueprint);
            }
        }

        private static int Units(Entity owner, string blueprint) => owner.GetPart<InventoryPart>().Objects
            .Where(e => e.BlueprintName == blueprint && ReferenceEquals(e.GetPart<PhysicsPart>()?.InInventory, owner))
            .Sum(e => Math.Max(0, e.GetPart<StackerPart>()?.StackCount ?? 1));
    }
}
