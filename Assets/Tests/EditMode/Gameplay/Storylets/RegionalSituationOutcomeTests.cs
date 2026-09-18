using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class RegionalSituationOutcomeTests
 {
 private const int Seed=64;
 private HotbarSaveFixture scope;
 private EntityFactory factory;
 private OverworldZoneManager manager;
 private Entity player;
 private Zone playerZone;
        [SetUp]
        public void SetUp()
        {
            scope = new HotbarSaveFixture(false, false);
            playerZone = null;
            CinderholdCompositionTests.LoadLoot();
            factory = GrovelandsCompositionTests.Factory();
            manager = OverworldZoneManager.CreateDetached(factory, Seed);
            player = factory.CreateEntity("Player");
            Assert.NotNull(player);
            player.GetPart<InventoryPart>().MaxWeight = -1;
            StoryletPart.Current = new StoryletPart();
            StoryletPart.LocalPlayer = player;
            NarrativeStatePart.Current = new NarrativeStatePart();
            MorrowfastContent.EnsureRegistered();
        }

        [TearDown]
        public void TearDown()
        {
            ConversationManager.EndConversation();
            scope?.Dispose();
            LootTableRegistry.ResetForTests();
        }


 // These tests specify receipt semantics, not punctuation or full prose.
 [TestCase("morrowfast-iron")][TestCase("cinderhold-iron")][TestCase("gantry-grain")]
 public void CompletedSupplyReplacesInstructionsWithActualOutcome(string id)
 {
  var d=RegionalSituations.Find(id);var b=Bind(id);StandBy(b.zone,b.recipient);
  Assert.IsTrue(b.request.TryAct(player,b.zone,"accept"));
  StringAssert.Contains("Bring ",Note());StringAssert.Contains("deliver",Note().ToLowerInvariant());
  Carry(d.ItemBlueprint,d.ItemCount);int money=TradeSystem.GetDrams(player);
  Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));
  AssertCompleted(Note());StringAssert.Contains((TradeSystem.GetDrams(player)-money)+" drams",Note());
  StringAssert.Contains(ItemLabel(d.RewardBlueprint),Note());
  StringAssert.Contains("trade",Note().ToLowerInvariant());
 }
 [TestCase(false)][TestCase(true)]
 public void RecoveryReceiptRecordsActualBankOutcomeAndPaidAmount(bool removeBank)
 {
  var d=RegionalSituations.Find("sumphold-oil");var b=Bind(d.Id);StandBy(b.zone,b.recipient);
  Assert.IsTrue(b.request.TryAct(player,b.zone,"accept"));
  var source=manager.GetZone(d.SourceZoneId);
  var banks=source.GetAllEntities().Where(e=>e.GetPart<RegionalHabitatPart>()?.InstanceId==b.request.InstanceId).ToArray();
  Assert.Greater(banks.Length,0);
  if(removeBank)foreach(var bank in banks)Assert.IsTrue(source.RemoveEntity(bank));
  var cargo=Source(source,d.Id);var at=source.GetEntityCell(cargo);MovePlayer(source,at.X,at.Y);
  Assert.IsTrue(InventorySystem.ExecuteCommand(new PickupCommand(cargo),player,source).Success);
  StandBy(b.zone,b.recipient);int money=TradeSystem.GetDrams(player),stock=Units(b.recipient,"WardOil");
  Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));
  Assert.AreEqual(stock+2,Units(b.recipient,"WardOil"));
  Assert.AreEqual(d.RewardDrams+(removeBank?0:3),TradeSystem.GetDrams(player)-money);
  AssertCompleted(Note());StringAssert.Contains((TradeSystem.GetDrams(player)-money)+" drams",Note());
  StringAssert.Contains(ItemLabel("WardOil"),Note());
  // Both outcomes require an explicit recorded result, not merely a missing bonus sentence.
  StringAssert.Contains(removeBank?"not preserved":"preserved",Note().ToLowerInvariant());
  if(!removeBank)StringAssert.DoesNotContain("not preserved",Note().ToLowerInvariant());
 }
 [Test]
 public void BeatingRecoveryRecordsRealFilterStockWithoutInventingBankOutcome()
 {
  var d=RegionalSituations.Find("wellmeet-filters");var b=Bind(d.Id);StandBy(b.zone,b.recipient);
  Assert.IsTrue(b.request.TryAct(player,b.zone,"accept"));
  var source=manager.GetZone(d.SourceZoneId);var cargo=Source(source,d.Id);
  Assert.IsFalse(source.GetAllEntities().Any(e=>e.GetPart<RegionalHabitatPart>()?.InstanceId==b.request.InstanceId),
   "This real Beating binding has no marked wetland habitat contract.");
  var at=source.GetEntityCell(cargo);MovePlayer(source,at.X,at.Y);
  Assert.IsTrue(InventorySystem.ExecuteCommand(new PickupCommand(cargo),player,source).Success);
  StandBy(b.zone,b.recipient);int money=TradeSystem.GetDrams(player),stock=Units(b.recipient,d.ItemBlueprint),reward=Units(player,d.RewardBlueprint);
  Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));
  Assert.AreEqual(stock+d.ItemCount,Units(b.recipient,d.ItemBlueprint));
  Assert.AreEqual(money+d.RewardDrams,TradeSystem.GetDrams(player));
  Assert.AreEqual(reward+1,Units(player,d.RewardBlueprint));
  AssertCompleted(Note());StringAssert.Contains(d.RewardDrams+" drams",Note());
  StringAssert.Contains(ItemLabel(d.ItemBlueprint),Note());StringAssert.Contains(ItemLabel(d.RewardBlueprint),Note());
  StringAssert.DoesNotContain("bank",Note().ToLowerInvariant());
  StringAssert.DoesNotContain("preserv",Note().ToLowerInvariant());
 }
 [Test]
 public void ReleaseReplacesActiveInstructionsWithoutPretendingPermanentRefusal()
 {
  var b=Bind("gantry-grain");StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"accept"));
  string active=Note();int money=TradeSystem.GetDrams(player);
  Assert.IsTrue(b.request.TryAct(player,b.zone,"release"));
  StringAssert.Contains("[released]",Note());StringAssert.DoesNotContain("Bring ",Note());
  StringAssert.DoesNotContain("Read, deliver, or release",Note());StringAssert.DoesNotContain("Payment:",Note());
  StringAssert.Contains("no",Note().ToLowerInvariant());Assert.AreEqual(money,TradeSystem.GetDrams(player));
  Assert.IsFalse(b.request.CanAct(player,b.zone,"deliver"));
  string released=Note();Assert.IsTrue(b.request.TryAct(player,b.zone,"read"));Assert.AreEqual(released,Note());
  Assert.IsTrue(b.request.TryAct(player,b.zone,"accept"));Assert.AreEqual(active,Note());
 }
 [TestCase(false)][TestCase(true)]
 public void NativePostActionRollbackRestoresPreviousNoteWhileSuccessReplacesIt(bool fail)
 {
  var b=Bind("gantry-grain");StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"accept"));Carry("Emberwheat",2);
  string previous=Note();int money=TradeSystem.GetDrams(player);
  var probe=new OutcomeProbe{Fail=fail};player.AddPart(probe);
  var result=InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(b.recipient,"RegionalRequest:deliver"),player,b.zone);
  Assert.Greater(probe.Calls,0,"Exercise actual native post-action callback.");Assert.AreEqual(!fail,result.Success);
  if(fail){Assert.AreEqual(previous,Note());Assert.AreEqual(money,TradeSystem.GetDrams(player));Assert.IsFalse(b.request.Completed);}
  else{Assert.AreNotEqual(previous,Note());AssertCompleted(Note());}
 }
 [Test]
 public void FullSessionReloadKeepsReceiptAfterStockLeavesAndForbidsSecondPayment()
 {
  var d=RegionalSituations.Find("gantry-grain");var b=Bind(d.Id);StandBy(b.zone,b.recipient);
  Assert.IsTrue(b.request.TryAct(player,b.zone,"accept"));Carry(d.ItemBlueprint,d.ItemCount);
  Assert.IsTrue(b.request.TryAct(player,b.zone,"deliver"));AssertCompleted(Note());
  string receipt=Note(),id=player.ID;int money=TradeSystem.GetDrams(player);
  // Stock can be sold/removed; a historical receipt is not a live shelf promise.
  foreach(var item in b.recipient.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName==d.ItemBlueprint).ToArray())
   b.recipient.GetPart<InventoryPart>().RemoveObject(item);
  RoundTripWorld(id);Assert.AreEqual(receipt,Note());
  b=Bind(d.Id);StandBy(b.zone,b.recipient);Carry(d.ItemBlueprint,d.ItemCount);
  Assert.IsFalse(b.request.TryAct(player,b.zone,"deliver"));Assert.AreEqual(money,TradeSystem.GetDrams(player));
  Assert.AreEqual(receipt,Note());Assert.AreEqual(d.ItemCount,Units(player,d.ItemBlueprint));
 }
 [TestCase(false)][TestCase(true)]
 public void CurrencyCommitFailureCannotPublishAnUnpaidReceipt(bool overflow)
 {
  var b=Bind("gantry-grain");StandBy(b.zone,b.recipient);Assert.IsTrue(b.request.TryAct(player,b.zone,"accept"));Carry("Emberwheat",2);
  string active=Note();TradeSystem.SetDrams(player,overflow?int.MaxValue:0);
  Assert.AreEqual(!overflow,b.request.TryAct(player,b.zone,"deliver"));
  if(overflow){Assert.AreEqual(active,Note());Assert.IsFalse(b.request.Completed);Assert.AreEqual(2,Units(player,"Emberwheat"));}
  else AssertCompleted(Note());
 }
 [Test]
 public void ReloadedJournalRendersDistinctOutcomesWithoutRecipientsOrGeneratingMissingSources()
 {
  var first=Bind("gantry-grain");StandBy(first.zone,first.recipient);
  Assert.IsTrue(first.request.TryAct(player,first.zone,"accept"));Carry("Emberwheat",2);
  Assert.IsTrue(first.request.TryAct(player,first.zone,"deliver"));
  var second=Bind("cinderhold-iron");StandBy(second.zone,second.recipient);
  Assert.IsTrue(second.request.TryAct(player,second.zone,"accept"));Assert.IsTrue(second.request.TryAct(player,second.zone,"release"));
  var before=RegionalSituationNotes.Read(player).ToArray();Assert.AreEqual(2,before.Length);
  RoundTripWorld(player.ID);CollectionAssert.AreEqual(before,RegionalSituationNotes.Read(player));
  // Historical notes remain readable after both actual recipients disappear.
  first=Bind("gantry-grain");second=Bind("cinderhold-iron");
  Assert.IsTrue(first.zone.RemoveEntity(first.recipient));Assert.IsTrue(second.zone.RemoveEntity(second.recipient));
  // Switch to a detached cold manager: opening a receipt must not hydrate either source.
  var cold=OverworldZoneManager.CreateDetached(factory,Seed);
  var empty=cold.GetZone("Overworld.19.19.0");manager=cold;MovePlayer(empty,40,12);
  int zonesBefore=manager.CachedZones.Count;
  var go=new UnityEngine.GameObject("regional-outcome-journal");
  try
  {
   var map=go.AddComponent<UnityEngine.Tilemaps.Tilemap>();var ui=go.AddComponent<CavesOfOoo.Rendering.QuestLogUI>();ui.Tilemap=map;ui.Open();
   ui.HandleInput(new OutcomeKeys(UnityEngine.KeyCode.Tab));Assert.IsTrue(ui.NotesVisible);
   var glyphs=Enumerable.Range(32,95).ToDictionary(i=>CavesOfOoo.Rendering.CP437TilesetGenerator.GetTextTile((char)i),i=>(char)i);
   var seen=new System.Text.StringBuilder();
   for(int page=0;page<6;page++)
   {
    for(int y=0;y<45;y++)for(int x=0;x<80;x++)
    {var tile=map.GetTile(new UnityEngine.Vector3Int(x,44-y,0));seen.Append(tile!=null&&glyphs.TryGetValue((UnityEngine.Tilemaps.Tile)tile,out char c)?c:' ');}
    int old=ui.NotesPage;ui.HandleInput(new OutcomeKeys(UnityEngine.KeyCode.PageDown));if(old==ui.NotesPage)break;
   }
   string visible=System.Text.RegularExpressions.Regex.Replace(seen.ToString(),@"\s+"," ");
   StringAssert.Contains("[completed]",visible);StringAssert.Contains("[released]",visible);
   StringAssert.Contains("cannot pay again",visible);StringAssert.Contains("No goods or payment changed hands",visible);
   Assert.AreEqual(zonesBefore,manager.CachedZones.Count,"Journal reads must not generate source/recipient zones.");
   CollectionAssert.AreEqual(before,RegionalSituationNotes.Read(player));
  }
  finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 private sealed class OutcomeKeys:CavesOfOoo.Rendering.IInputProbe
 {private readonly UnityEngine.KeyCode key;public OutcomeKeys(UnityEngine.KeyCode key){this.key=key;}public bool GetKeyDown(UnityEngine.KeyCode candidate)=>candidate==key;}

 private string ItemLabel(string blueprint)=>factory.Blueprints[blueprint].Parts["Render"]["DisplayName"];
 private string Note(){var notes=RegionalSituationNotes.Read(player);Assert.AreEqual(1,notes.Count);return notes[0];}
 private static void AssertCompleted(string text)
 {
  StringAssert.Contains("[completed]",text);
  foreach(string obsolete in new[]{"Bring ","Recover the marked", "Outside goods still fulfill", "Read, deliver, or release", "Payment:", "earns three extra"})
   StringAssert.DoesNotContain(obsolete,text,"Completed receipt must not advertise an outstanding request.");
 }
 private sealed class OutcomeProbe:Part
 {
  public override string Name=>"RegionalOutcomeProbe";public bool Fail;public int Calls;
  public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction"){Calls++;if(Fail)throw new InvalidOperationException("outcome rollback");}return true;}
 }
        private void RoundTripWorld(string playerId)
        {
            using(var stream=new MemoryStream())
            {
                // The session writes queued entity bodies and runs native load
                // hooks; the manager subrecord alone contains unresolved refs.
                var saved=GameSessionState.Capture("regional-adversarial", "test", manager, null, player);
                saved.Save(new SaveWriter(stream));stream.Position=0;
                var restored=GameSessionState.Load(new SaveReader(stream,factory));
                manager=restored.ZoneManager;
                Assert.AreEqual(playerId,restored.Player.ID);
            }
            playerZone=manager.CachedZones.Values.Single(z=>z.GetAllEntities().Any(e=>e.ID==playerId));
            player=playerZone.GetAllEntities().Single(e=>e.ID==playerId);
            StoryletPart.LocalPlayer=player;manager.SetActiveZone(playerZone);SettlementRuntime.ActiveZone=playerZone;
        }
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
