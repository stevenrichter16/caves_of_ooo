using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Concerns;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// C3a (Docs/PLACE-WORK-AND-PROGRESSION.md, approach kinds): Supply is a counted
    /// hand-over of real carried items; Carry is a sealed item the owner hands over and
    /// only the named resident of the named place can take; Read is a document placed at
    /// a place while the concern is open, finished by actually reading it. Exercised
    /// through the owner's and the recipient's real conversations.
    /// </summary>
    public sealed class ConcernApproachKindsTests
    {
        static EntityFactory factory;
        StoryletPart storylets;
        Entity player, keeper;
        Zone sill, tine;
        const string Id = "probe.kinds";
        static string Q => ConcernRegistry.QuestIdFor(Id);

        const string Json = @"{""Concerns"":[{
  ""Id"":""probe.kinds"",""Place"":""Sill"",""Owner"":""WellKeeper"",""Faction"":""Villagers"",""Tier"":1,
  ""Title"":""Probe Kinds"",""Pitch"":""Three ways."",""Thanks"":""Done."",
  ""Approaches"":[
    {""Id"":""sand"",""Kind"":""Supply"",""Label"":""Bring sand."",""Objective"":""Bring three bags of silver sand"",
     ""Supply"":{""Blueprint"":""SilverSand"",""Count"":3}},
    {""Id"":""letter"",""Kind"":""Carry"",""Label"":""Carry a letter."",""Objective"":""Carry the keeper's letter to the scribe at Tine"",
     ""Carry"":{""Item"":""SealedLetter"",""ItemName"":""the well keeper's letter"",""Place"":""Tine"",""Recipient"":""Scribe"",
               ""Handover"":""The well keeper at Sill sent this."",""Reply"":""Sill again. I'll write back.""}},
    {""Id"":""notice"",""Kind"":""Read"",""Label"":""Read the notice."",""Objective"":""Read the old notice in Sill"",
     ""Read"":{""Document"":""codex-01"",""Place"":""Sill""}}
  ],
  ""Refusal"":{""Text"":""No."",""Reply"":""Fine.""},""Reward"":{""XP"":10}}]}";

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); ConcernRegistry.LoadFromJson(Json, "probe");
            MessageLog.Clear(); Diag.ResetAll();
            ConversationActions.Factory = factory;
            storylets = new StoryletPart(); StoryletPart.Current = storylets;
            NarrativeStatePart.Current = new NarrativeStatePart();
            sill = new Zone("Overworld.10.10.0"); tine = new Zone("Overworld.13.7.0");
            player = factory.CreateEntity("Player"); StoryletPart.LocalPlayer = player;
            keeper = factory.CreateEntity("WellKeeper");
            sill.AddEntity(player, 10, 10); sill.AddEntity(keeper, 11, 10);
        }
        [TearDown] public void Cleanup()
        {
            ConversationManager.EndConversation();
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null; ConversationActions.Factory = null;
        }

        void Talk(Entity speaker)
        {
            ConversationManager.Speaker = speaker; ConversationManager.Listener = player;
            var start = new NodeData { ID = "Start", Text = "authored" };
            start.Choices.Add(new ChoiceData { Text = "[Leave]", Target = "End" });
            ConversationManager.CurrentConversation = new ConversationData { ID = "Probe", Nodes = { start } };
            ConversationManager.CurrentNode = start;
            ConversationManager.RefreshVisibleChoices();
        }
        static int Index(string text)
        {
            var list = ConversationManager.VisibleChoices;
            for (int i = 0; i < list.Count; i++) if (list[i].Text.Contains(text)) return i;
            Assert.Fail("no choice containing '" + text + "' in: " + string.Join(" | ", list.Select(c => c.Text)));
            return -1;
        }
        static bool Has(string text) => ConversationManager.VisibleChoices.Any(c => c.Text.Contains(text));
        void Accept() { Assert.IsNull(ConcernConversation.Accept(keeper, player, Id)); }
        void Status() { Talk(keeper); ConversationManager.SelectChoice(Index("About probe kinds")); }
        void Give(string bp, int n) { for (int i = 0; i < n; i++) player.GetPart<InventoryPart>().AddObject(factory.CreateEntity(bp)); }
        int Carried(string bp) => ConcernItems.CountCarried(player, bp);
        int Approach => NarrativeStatePart.Current.GetFact(ConcernRegistry.ApproachFact(Id));

        // ════════════════ compile and lint ════════════════

        [Test] public void KindsWithoutAuthoredTriggersCompileTheirOwnFinishCondition()
        {
            var work = StoryletRegistry.FindQuest(Q).Stages[0];
            Assert.True(work.Objectives.All(o => o.Triggers.Count == 1), "one implicit trigger each");
            StringAssert.Contains(ConcernRegistry.DoneFact(Id, "sand"), work.Objectives[0].Triggers[0].Value);
            StringAssert.Contains(ConcernRegistry.DoneFact(Id, "letter"), work.Objectives[1].Triggers[0].Value);
            StringAssert.Contains(ReadableDocumentPart.ReadFact("codex-01"), work.Objectives[2].Triggers[0].Value);
            Assert.IsEmpty(ConcernRegistry.Lint(ConcernRegistry.Get(Id), factory));
        }

        [Test] public void LintRejectsKindsThatCannotBeFinished()
        {
            var c = JsonUtility.FromJson<ConcernData>(JsonUtility.ToJson(ConcernRegistry.Get(Id)));
            c.Approaches[0].Supply.Blueprint = "NoSuchSand"; c.Approaches[0].Supply.Count = 0;
            c.Approaches[1].Carry.Recipient = "NoSuchScribe"; c.Approaches[1].Carry.Place = "Atlantis";
            c.Approaches[2].Read.Document = "no-such-document";
            var lines = string.Join("|", ConcernRegistry.Lint(c, factory));
            StringAssert.Contains("supply blueprint", lines); StringAssert.Contains("supply count", lines);
            StringAssert.Contains("recipient", lines); StringAssert.Contains("carry place", lines);
            StringAssert.Contains("document", lines);
        }

        // ════════════════ Supply ════════════════

        [Test] public void SupplyIsHandedOverOnlyInFullAndTakesExactlyTheCount()
        {
            Accept(); Give("SilverSand", 2);
            Status();
            StringAssert.Contains("(you carry 2 of 3)", ConversationManager.CurrentText);
            Assert.False(Has("Hand over"), "short of the count, nothing is offered");
            Assert.AreEqual("short", ConcernConversation.HandSupply(keeper, player, Id + "|sand"));
            Assert.AreEqual(2, Carried("SilverSand"), "a refused hand-over takes nothing");

            Give("SilverSand", 2);
            Status();
            ConversationManager.SelectChoice(Index("Hand over 3"));
            Assert.AreEqual(1, Carried("SilverSand"), "exactly three taken from four");
            storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.AreEqual(1, Approach, "supply was approach 1");
        }

        [Test] public void SupplyCountsUnitsInStacksAndIgnoresEquippedOrFloorItems()
        {
            var stack = factory.CreateEntity("KnotflaxCord"); stack.GetPart<StackerPart>().StackCount = 2;
            player.GetPart<InventoryPart>().AddObject(stack); Give("KnotflaxCord", 1);
            Assert.AreEqual(3, ConcernItems.CountCarried(player, "KnotflaxCord"), "two in a stack plus one");
            Assert.True(ConcernItems.TakeCarried(player, "KnotflaxCord", 3));
            Assert.AreEqual(0, ConcernItems.CountCarried(player, "KnotflaxCord"));
            sill.AddEntity(factory.CreateEntity("SilverSand"), 10, 11);
            Assert.AreEqual(0, ConcernItems.CountCarried(player, "SilverSand"), "on the floor is not carried");
            Assert.False(ConcernItems.TakeCarried(player, "SilverSand", 1));
        }

        [Test] public void TheCountedPredicateAndActionAreGeneral()
        {
            Give("SilverSand", 3);
            Assert.True(ConversationPredicates.Evaluate("IfHaveItemCount", keeper, player, "SilverSand:3"));
            Assert.False(ConversationPredicates.Evaluate("IfHaveItemCount", keeper, player, "SilverSand:4"), "counter-check");
            Assert.AreEqual("short", ConversationActions.ExecuteRequired("TakeItemCount", keeper, player, "SilverSand:4"));
            Assert.AreEqual(3, Carried("SilverSand"));
            Assert.IsNull(ConversationActions.ExecuteRequired("TakeItemCount", keeper, player, "SilverSand:3"));
            Assert.AreEqual(0, Carried("SilverSand"));
        }

        // ════════════════ Carry ════════════════

        [Test] public void TheOwnerHandsOverOneNamedSealedLetter()
        {
            Accept(); Status();
            ConversationManager.SelectChoice(Index("I'll carry"));
            var letters = player.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == "SealedLetter").ToList();
            Assert.AreEqual(1, letters.Count);
            Assert.AreEqual("the well keeper's letter", letters[0].GetPart<RenderPart>().DisplayName);
            Assert.AreEqual(Id + "|letter", letters[0].GetProperty(ConcernItems.CargoProperty));
            Status(); Assert.False(Has("I'll carry"), "one letter at a time");
        }

        [Test] public void OnlyTheNamedResidentOfTheNamedPlaceTakesIt()
        {
            Accept(); Status(); ConversationManager.SelectChoice(Index("I'll carry"));
            var scribeHere = factory.CreateEntity("Scribe"); sill.AddEntity(scribeHere, 12, 10);
            Talk(scribeHere); Assert.False(Has("The well keeper at Sill sent this."), "a scribe, but not at Tine");
            var villager = factory.CreateEntity("Villager"); tine.AddEntity(villager, 5, 5);
            Talk(villager); Assert.False(Has("The well keeper at Sill sent this."), "at Tine, but not the scribe");

            var scribe = factory.CreateEntity("Scribe"); tine.AddEntity(scribe, 6, 5);
            Talk(scribe); ConversationManager.SelectChoice(Index("The well keeper at Sill sent this."));
            StringAssert.Contains("Sill again", ConversationManager.CurrentText);
            Assert.AreEqual(0, ConcernItems.CountCarried(player, "SealedLetter"), "the letter is handed over");
            storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.AreEqual(2, Approach, "carry was approach 2");
        }

        [Test] public void AnotherConcernsLetterIsNotThisOne()
        {
            Accept();
            var forged = factory.CreateEntity("SealedLetter"); forged.Properties[ConcernItems.CargoProperty] = "other.concern|letter";
            player.GetPart<InventoryPart>().AddObject(forged);
            var scribe = factory.CreateEntity("Scribe"); tine.AddEntity(scribe, 6, 5);
            Talk(scribe); Assert.False(Has("The well keeper at Sill sent this."));
        }

        [Test] public void ALostLetterIsReissued()
        {
            Accept(); Status(); ConversationManager.SelectChoice(Index("I'll carry"));
            var letter = player.GetPart<InventoryPart>().Objects.First(e => e.BlueprintName == "SealedLetter");
            player.GetPart<InventoryPart>().RemoveObject(letter);
            Status(); Assert.True(Has("I'll carry"), "the keeper writes it again");
        }

        // ════════════════ Read ════════════════

        [Test] public void EnteringThePlaceWhileOpenPlacesTheDocumentOnce()
        {
            Assert.AreEqual(0, ConcernWorld.OnZoneEntered(player, sill), "not undertaken: nothing placed");
            Accept();
            Assert.AreEqual(1, sill.GetAllEntities().Count(e => e.BlueprintName == "Codex01"), "accepting in the place puts it there at once");
            Assert.AreEqual(0, ConcernWorld.OnZoneEntered(player, sill), "idempotent on re-entry");
            Assert.AreEqual(1, sill.GetAllEntities().Count(e => e.BlueprintName == "Codex01"));
            Assert.AreEqual(0, ConcernWorld.OnZoneEntered(player, tine), "only in the named place");
        }

        [Test] public void AcceptedElsewhereTheDocumentAppearsOnArrival()
        {
            sill.RemoveEntity(player); tine.AddEntity(player, 3, 3);
            Accept();
            Assert.AreEqual(0, sill.GetAllEntities().Count(e => e.BlueprintName == "Codex01"), "not there before the player arrives");
            tine.RemoveEntity(player); sill.AddEntity(player, 10, 10);
            Assert.AreEqual(1, ConcernWorld.OnZoneEntered(player, sill));
            var doc = sill.GetAllEntities().Single(e => e.BlueprintName == "Codex01");
            Assert.AreEqual(ConcernWorld.DocumentEntityId(Id, "notice"), doc.ID);
        }

        [Test] public void ReadingTheDocumentFinishesTheApproach()
        {
            Accept(); ConcernWorld.OnZoneEntered(player, sill);
            var doc = sill.GetAllEntities().First(e => e.BlueprintName == "Codex01");
            var at = sill.GetEntityPosition(doc); sill.MoveEntity(player, at.x, at.y);
            Assert.True(doc.GetPart<ReadableDocumentPart>().TryRead(player, sill));
            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact(ReadableDocumentPart.ReadFact("codex-01")));
            storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.AreEqual(3, Approach, "read was approach 3");
        }
        // ════════════════ adversarial ════════════════

        [Test] public void Adversarial_HandingOverTwiceTakesOnce()
        {
            Accept(); Give("SilverSand", 6);
            Assert.IsNull(ConcernConversation.HandSupply(keeper, player, Id + "|sand"));
            Assert.AreEqual("already_done", ConcernConversation.HandSupply(keeper, player, Id + "|sand"));
            Assert.AreEqual(3, Carried("SilverSand"));
        }

        [Test] public void Adversarial_WrongRecipientsAndMissingCargoAreRefused()
        {
            Accept();
            var scribe = factory.CreateEntity("Scribe"); tine.AddEntity(scribe, 6, 5);
            Assert.AreEqual("not_carried", ConcernConversation.DeliverCargo(scribe, player, Id + "|letter"));
            Status(); ConversationManager.SelectChoice(Index("I'll carry"));
            Assert.AreEqual("wrong_recipient", ConcernConversation.DeliverCargo(keeper, player, Id + "|letter"), "the owner is not the recipient");
            Assert.AreEqual("no_cargo", ConcernConversation.DeliverCargo(scribe, player, Id + "|sand"));
            Assert.AreEqual(1, Carried("SealedLetter"), "nothing handed over by a refused delivery");
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact(ConcernRegistry.DoneFact(Id, "letter")));
        }

        [Test] public void Adversarial_OnceSettledAnotherWayTheOtherKindsTakeNothing()
        {
            Accept(); Give("SilverSand", 3);
            Status(); ConversationManager.SelectChoice(Index("I'll carry"));
            NarrativeStatePart.Current.SetFact(ReadableDocumentPart.ReadFact("codex-01"), 1);
            storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.AreEqual(3, Approach, "read first");
            Assert.AreEqual("not_working", ConcernConversation.HandSupply(keeper, player, Id + "|sand"));
            Assert.AreEqual(3, Carried("SilverSand"), "the sand stays with the player");
            var scribe = factory.CreateEntity("Scribe"); tine.AddEntity(scribe, 6, 5);
            Talk(scribe); Assert.False(Has("The well keeper at Sill sent this."), "nothing to deliver for a settled concern");
        }

        [Test] public void Adversarial_ANonPlayerReadingSetsNoFact()
        {
            var doc = factory.CreateEntity("Codex01"); sill.AddEntity(doc, 12, 10);
            var reader = factory.CreateEntity("Scribe"); sill.AddEntity(reader, 12, 11);
            doc.GetPart<ReadableDocumentPart>().TryRead(reader, sill);
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact(ReadableDocumentPart.ReadFact("codex-01")));
        }

        [TestCase(null)] [TestCase("")] [TestCase("SilverSand")] [TestCase("SilverSand:0")] [TestCase("SilverSand:-2")] [TestCase(":3")]
        public void Adversarial_MalformedCountsTakeNothing(string arg)
        {
            Give("SilverSand", 3);
            Assert.False(ConversationPredicates.Evaluate("IfHaveItemCount", keeper, player, arg));
            Assert.AreEqual("invalid_argument", ConversationActions.ExecuteRequired("TakeItemCount", keeper, player, arg));
            Assert.AreEqual(3, Carried("SilverSand"));
        }
    }
}
