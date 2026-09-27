using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    // Actual current menu dispatch and reader; direct private input seam, not native keyboard evidence.
    public sealed class SpreadWorldExamineReaderTests
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        HotbarSaveFixture scope; InputHandler input; WorldActionMenuUI menu; AnnouncementUI reader;
        EntityFactory factory; Zone zone; Entity player; Camera main, popup; CameraFollow follow;
        [SetUp] public void Setup()
        {
            scope = new HotbarSaveFixture(true, false); input = scope.Input; zone = input.CurrentZone; player = input.PlayerEntity;
            factory = new EntityFactory(); factory.LoadBlueprints(Resources.Load<TextAsset>("Content/Blueprints/Objects").text);
            var grid = Child("Reader grid"); grid.AddComponent<Grid>();
            menu = scope.Root.AddComponent<WorldActionMenuUI>(); menu.Tilemap = Tiles(grid.transform, "Menu"); menu.BgTilemap = Tiles(grid.transform, "Menu background");
            reader = scope.Root.AddComponent<AnnouncementUI>(); reader.Tilemap = Tiles(grid.transform, "Reader"); reader.BgTilemap = Tiles(grid.transform, "Reader background");
            main = Child("Reader main camera").AddComponent<Camera>(); main.orthographic = true; main.aspect = 16f / 9f; main.orthographicSize = 17;
            main.transform.position = new Vector3(3.5f, 20.5f, -10);
            popup = Child("Reader popup camera").AddComponent<Camera>(); popup.orthographic = true; popup.enabled = false;
            follow = main.gameObject.AddComponent<CameraFollow>(); follow.Player = player; follow.CurrentZone = zone; follow.PopupOverlayCamera = popup;
            input.WorldActionMenuUI = menu; input.AnnouncementUI = reader; input.CameraFollow = follow; menu.PopupCamera = popup; reader.PopupCamera = popup;
            follow.SnapToPlayer(); MessageLog.Clear();
        }
        [TearDown] public void Cleanup() { if (reader != null) reader.Close(); scope?.Dispose(); scope = null; }
        GameObject Child(string name) { var go = new GameObject(name); go.transform.SetParent(scope.Root.transform, false); return go; }
        Tilemap Tiles(Transform parent, string name) { var go = Child(name); go.transform.SetParent(parent, false); return go.AddComponent<Tilemap>(); }
        object Invoke(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Flags).Invoke(owner, args);
        object Get(object owner, string name) => owner.GetType().GetField(name, Flags).GetValue(owner);
        void State(string field, string value) { var info = typeof(InputHandler).GetField(field, Flags); info.SetValue(input, Enum.Parse(info.FieldType, value)); }
        Entity Put(string name, int x = 15, int y = 4)
        { var e = factory.CreateEntity(name); Assert.NotNull(e, name); Assert.True(zone.AddEntity(e, x, y)); zone.GetCell(x,y).IsVisible = true; zone.GetCell(x,y).Explored = true; return e; }
        void Open(Entity owner, string returnState = "LookMode")
        { State("_worldActionMenuReturnState", returnState); Invoke(input, "OpenWorldActionMenuFor", owner, zone.GetEntityCell(owner), false); Assert.True(menu.IsOpen); }
        void Examine()
        {
            var actions = (List<InventoryAction>)Get(menu, "_actions"); var action = actions.Single(a => a.Command == "Examine");
            // Use the UI's ordinary selection close before handing its current target/cell to InputHandler.
            var owner = menu.SelectedTarget; var cell = menu.SelectedCell; bool pile = menu.SelectedCellIsPile;
            Invoke(menu, "SelectAction", actions.IndexOf(action)); Invoke(input, "ExecuteWorldActionSelection", action, owner, cell, pile);
            if (!reader.IsOpen) Invoke(input, "TryOpenAnnouncement");
        }
        string AllText()
        { Assert.True(reader.IsOpen, "Actual world Examine must reach the complete paginated reader."); var lines = new List<string>(); for (int i=0;i<reader.PageCount;i++){ Assert.True(reader.GoToPage(i)); lines.AddRange(reader.VisibleLines); } return string.Join("\n", lines); }
        void Close() { reader.Close(); Invoke(input, "CloseAnnouncement"); }
        void AssertFree(int tick, int energy, int hp, Entity[] owners)
        { Assert.AreEqual(tick,input.TurnManager.TickCount); Assert.AreEqual(energy,input.TurnManager.GetEnergy(player)); Assert.AreEqual(hp,player.GetStatValue("Hitpoints")); CollectionAssert.AreEqual(owners,zone.GetReadOnlyEntities()); }

        [TestCase(true, "LookMode")][TestCase(false, "Normal")]
        public void RealHotOrCoolOwnerAndVisibleGroundOpenTogetherForFree(bool hot, string prior)
        {
            var source=Put("OilSeep"); Put("SteamCloud"); source.ApplyEffect(new SteamEffect(.7f)); source.GetPart<ThermalPart>().Temperature=hot?700:25;
            zone.TileState.AddHeat(15,4,1); string ground=CellStatusReadout.GroundLine(zone,zone.GetCell(15,4)); Assert.NotNull(ground);
            int tick=input.TurnManager.TickCount, energy=input.TurnManager.GetEnergy(player),hp=player.GetStatValue("Hitpoints"); var owners=zone.GetReadOnlyEntities().ToArray();
            var cameraPosition=main.transform.position; float size=main.orthographicSize; var rect=main.rect;
            Open(source,prior); Examine(); string text=AllText(); StringAssert.Contains(source.GetDisplayName(),text); StringAssert.Contains(hot?"Hot steam":"Steam - cools",text);
            StringAssert.Contains("On the ground:",text); StringAssert.Contains("hot",text); Assert.True(popup.enabled); Assert.AreEqual("AnnouncementOpen",Get(input,"_inputState").ToString());
            Assert.AreEqual(.7f,source.GetEffect<SteamEffect>().Density); Close(); Assert.AreEqual(prior,Get(input,"_inputState").ToString()); Assert.False(popup.enabled);
            Assert.AreEqual(cameraPosition,main.transform.position); Assert.AreEqual(size,main.orthographicSize); Assert.AreEqual(rect,main.rect); AssertFree(tick,energy,hp,owners);
        }
        [Test] public void LongFlavorCannotHideCurrentWarningAndEveryParagraphRemainsReachable()
        {
            var source=Put("OilSeep"); source.ApplyEffect(new SteamEffect(.7f)); source.GetPart<ThermalPart>().Temperature=700;
            source.GetPart<ExaminablePart>().Text="FLAVOR-BEGIN\n"+string.Join("\n",Enumerable.Range(0,80).Select(i=>"Paragraph "+i+" retained words."))+"\nFLAVOR-END";
            Open(source); Examine(); Assert.True(reader.IsOpen); Assert.Greater(reader.PageCount,1);
            string first=string.Join("\n",reader.VisibleLines); StringAssert.Contains("Hot steam",first);
            string text=AllText(); StringAssert.Contains("FLAVOR-BEGIN",text); StringAssert.Contains("FLAVOR-END",text);
            for(int i=0;i<80;i++)StringAssert.Contains("Paragraph "+i+" retained words.",text);
        }
        [Test] public void ReaderSnapshotStaysStableButReopeningUsesCooledCurrentSource()
        {
            var source=Put("OilSeep"); source.ApplyEffect(new SteamEffect(.7f)); source.GetPart<ThermalPart>().Temperature=700;
            Open(source); Examine(); string before=AllText(); source.GetPart<ThermalPart>().Temperature=25;
            Assert.AreEqual(before,AllText()); Close(); Open(source); Examine(); string current=AllText(); StringAssert.Contains("Steam - cools",current); StringAssert.DoesNotContain("Hot steam",current);
        }
        [TestCase("removed")][TestCase("moved")][TestCase("hidden")][TestCase("fogged")][TestCase("unexplored")][TestCase("foreign-zone")][TestCase("missing-part")][TestCase("replacement-id")]
        public void ChangedSelectionCannotRevealStaleOwnerOrGround(string change)
        {
            var owner=Put("OilSeep"); owner.GetPart<ExaminablePart>().Text="PRIVATE-OWNER-TEXT"; zone.TileState.AddHeat(15,4,1); Open(owner);
            switch(change)
            {
                case "removed": Assert.True(zone.RemoveEntity(owner)); break;
                case "moved": Assert.True(zone.MoveEntity(owner,16,4)); break;
                case "hidden": owner.GetPart<RenderPart>().Visible=false; break;
                case "fogged": zone.GetCell(15,4).IsVisible=false; break;
                case "unexplored": zone.GetCell(15,4).Explored=false; break;
                case "foreign-zone": input.CurrentZone=new Zone("different-current-zone"); break;
                case "missing-part": Assert.True(owner.RemovePart(owner.GetPart<ExaminablePart>())); break;
                case "replacement-id": string id=owner.ID; Assert.True(zone.RemoveEntity(owner)); var other=Put("OilSeep"); other.ID=id; other.GetPart<ExaminablePart>().Text="OTHER-OWNER-TEXT"; break;
            }
            long serial=MessageLog.NextSerialValue; int tick=input.TurnManager.TickCount; Examine(); Assert.False(reader.IsOpen); Assert.False(MessageLog.HasPendingAnnouncement);
            var newMessages=MessageLog.GetRecentEntries(20).Where(e=>e.Serial>=serial).Select(e=>e.Text).ToArray(); Assert.IsNotEmpty(newMessages,"Refusal must be visible.");
            foreach(var text in newMessages){ StringAssert.DoesNotContain("PRIVATE-OWNER-TEXT",text); StringAssert.DoesNotContain("OTHER-OWNER-TEXT",text); StringAssert.DoesNotContain("On the ground:",text); }
            Assert.AreEqual(tick,input.TurnManager.TickCount);
        }
        [Test] public void CurrentCorpseIsInspectableWithoutALivingActorGate()
        { var corpse=Put("CreatureCorpse"); Open(corpse); Examine(); StringAssert.Contains(corpse.GetDisplayName(),AllText()); }
        [Test] public void ActualPileSummaryIsDistinctFromSelectedOwnersFullDetails()
        {
            var one=Put("Dagger"); var two=Put("LeatherCap"); one.GetPart<ExaminablePart>().Text="ONE-PRIVATE-DETAIL"; two.GetPart<ExaminablePart>().Text="TWO-PRIVATE-DETAIL";
            State("_worldActionMenuReturnState","LookMode"); Invoke(input,"OpenWorldActionMenuOrThrow",15,4); Assert.True(menu.SelectedCellIsPile); Examine();
            string summary=AllText(); StringAssert.Contains("A pile of items",summary); StringAssert.Contains(one.GetDisplayName(),summary); StringAssert.Contains(two.GetDisplayName(),summary); StringAssert.DoesNotContain("ONE-PRIVATE-DETAIL",summary);
            Close(); Open(one); Examine(); string detail=AllText(); StringAssert.Contains("ONE-PRIVATE-DETAIL",detail); StringAssert.DoesNotContain("TWO-PRIVATE-DETAIL",detail);
        }
        [Test] public void EmptyCellDoesNotInventAnOwnerOrQueueAReader()
        { State("_worldActionMenuReturnState","LookMode"); Invoke(input,"OpenWorldActionMenuOrThrow",22,20); Assert.False(reader.IsOpen); Assert.False(MessageLog.HasPendingAnnouncement); }
        [Test] public void LongStatusOverflowPointsToExamineRatherThanAnotherCappedSummary()
        {
            var owner=Put("OilSeep"); for(int i=0;i<8;i++) owner.ApplyEffect(new ReaderTestEffect{Index=i});
            var lines=WorldActionMenuUI.BuildStatusLinesFor(owner,zone.GetEntityCell(owner),zone); Assert.AreEqual(6,lines.Count); StringAssert.Contains("Examine",lines.Last()); StringAssert.DoesNotContain("look mode",lines.Last());
        }
        void Details()
        {
            var method=typeof(InputHandler).GetMethod("OpenWorldActionDetailsReader",Flags);
            Assert.NotNull(method,"The selected action has a free full-text recovery route.");method.Invoke(input,null);
        }
        ReaderActionPart ActionOwner(out Entity owner)
        {
            owner=Put("OilSeep");var part=new ReaderActionPart();owner.AddPart(part);Open(owner);return part;
        }
        [Test] public void FullSelectedActionLabelUsesFreeReaderAndReturnsToCurrentMenu()
        {
            Entity owner;var part=ActionOwner(out owner);int tick=input.TurnManager.TickCount,energy=input.TurnManager.GetEnergy(player),hp=player.GetStatValue("Hitpoints");var camera=main.transform.position;
            Details();string text=AllText();StringAssert.Contains("ACTION-BEGIN",text);StringAssert.Contains("ACTION-END",text);Assert.Zero(part.Executed);
            Close();Assert.True(menu.IsOpen);Assert.AreSame(owner,menu.SelectedTarget);Assert.AreEqual("WorldActionMenuOpen",Get(input,"_inputState").ToString());Assert.True(popup.enabled);
            var actions=(List<InventoryAction>)Get(menu,"_actions");Assert.AreEqual("ReaderAction",actions[(int)Get(menu,"_cursorIndex")].Command);
            Invoke(menu,"Cancel");Invoke(input,"HandleWorldActionMenuInput");Assert.AreEqual("LookMode",Get(input,"_inputState").ToString());Assert.False(popup.enabled);Assert.AreEqual(camera,main.transform.position);
            Assert.AreEqual(tick,input.TurnManager.TickCount);Assert.AreEqual(energy,input.TurnManager.GetEnergy(player));Assert.AreEqual(hp,player.GetStatValue("Hitpoints"));Assert.Zero(part.Executed);
        }
        [Test] public void ActionDetailsUseFreshLabelAndActualOwnerGroundWarning()
        {
            Entity owner;var part=ActionOwner(out owner);part.Label="FRESH-ACTION: currently unavailable until supplied";owner.ApplyEffect(new SteamEffect(.7f));owner.GetPart<ThermalPart>().Temperature=700;zone.TileState.AddHeat(15,4,1);
            Details();string text=AllText();StringAssert.Contains(part.Label,text.Replace("\n"," "));StringAssert.Contains("Hot steam",text);StringAssert.Contains("On the ground:",text);StringAssert.DoesNotContain("ACTION-BEGIN",text);Assert.Zero(part.Executed);
        }
        [Test] public void RemovedActionDoesNotOpenStaleDetailsAndRefreshesMenu()
        {
            Entity owner;var part=ActionOwner(out owner);part.Offered=false;long serial=MessageLog.NextSerialValue;Details();Assert.False(reader.IsOpen);Assert.True(menu.IsOpen);
            Assert.False(((List<InventoryAction>)Get(menu,"_actions")).Any(a=>a.Command=="ReaderAction"));Assert.True(MessageLog.GetRecentEntries(10).Any(x=>x.Serial>=serial));Assert.Zero(part.Executed);
        }
        [Test] public void RemovedOwnerBeforeDetailsReturnsToPriorModeWithoutLeakingLabel()
        {
            Entity owner;var part=ActionOwner(out owner);Assert.True(zone.RemoveEntity(owner));Details();Assert.False(reader.IsOpen);Assert.False(menu.IsOpen);Assert.AreEqual("LookMode",Get(input,"_inputState").ToString());Assert.False(popup.enabled);Assert.Zero(part.Executed);
        }
        [Test] public void RemovedOwnerWhileReadingCannotReopenAStaleActionMenu()
        {
            Entity owner;var part=ActionOwner(out owner);Details();Assert.True(reader.IsOpen);Assert.True(zone.RemoveEntity(owner));Close();Assert.False(menu.IsOpen);Assert.AreEqual("LookMode",Get(input,"_inputState").ToString());Assert.False(popup.enabled);Assert.Zero(part.Executed);
        }
        [Test] public void RemovedActionWhileReadingReturnsOnlyCurrentlyOfferedRows()
        {
            Entity owner;var part=ActionOwner(out owner);Details();part.Offered=false;Close();Assert.True(menu.IsOpen);Assert.False(((List<InventoryAction>)Get(menu,"_actions")).Any(a=>a.Command=="ReaderAction"));Assert.Zero(part.Executed);
        }
        [Test] public void EmptyActionMenuHasNoInventedDetails()
        {
            var owner=Put("OilSeep");menu.Open(player,owner,zone.GetEntityCell(owner),new List<InventoryAction>(),zone);State("_inputState","WorldActionMenuOpen");Details();Assert.False(reader.IsOpen);Assert.False(MessageLog.HasPendingAnnouncement);Assert.True(menu.IsOpen);
        }
        [Test] public void PileReaderOmitsHiddenOwnersWithoutChangingTheWorld()
        {
            var one=Put("Dagger");var two=Put("LeatherCap");var hidden=Put("OilSeep");hidden.Tags.Remove("Terrain");hidden.GetPart<RenderPart>().DisplayName="HIDDEN-PILE-OWNER";hidden.GetPart<RenderPart>().Visible=false;
            var owners=zone.GetReadOnlyEntities().ToArray();State("_worldActionMenuReturnState","LookMode");var cell=zone.GetEntityCell(one);menu.Open(player,one,cell,WorldInteractionSystem.BuildPileSummaryActions(cell,player),zone,true);Examine();string text=AllText();StringAssert.Contains(one.GetDisplayName(),text);StringAssert.Contains(two.GetDisplayName(),text);StringAssert.DoesNotContain("HIDDEN-PILE-OWNER",text);CollectionAssert.AreEqual(owners,zone.GetReadOnlyEntities());
        }
        [Test] public void SelectedPhysicalFootprintCellKeepsItsOwnGroundReadout()
        {
            var owner=Put("OilSeep");zone.RemoveEntity(owner);owner.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});Assert.True(zone.AddEntity(owner,15,4));var physical=zone.GetCell(16,4);physical.IsVisible=true;physical.Explored=true;zone.TileState.AddHeat(16,4,1);
            State("_worldActionMenuReturnState","LookMode");Invoke(input,"OpenWorldActionMenuFor",owner,physical,false);Examine();StringAssert.Contains("On the ground:",AllText());Assert.AreSame(zone.GetCell(15,4),zone.GetEntityCell(owner));
        }
        [Test] public void DescriptionCallbackRemovingOwnerCannotPublishAPrevalidatedSnapshot()
        {
            var owner=Put("OilSeep");owner.AddPart(new ReaderMutationEnhancement{OnDescribe=()=>zone.RemoveEntity(owner)});Open(owner);Examine();Assert.False(reader.IsOpen);Assert.False(MessageLog.HasPendingAnnouncement);Assert.IsNull(zone.GetEntityCell(owner));
        }
        [TestCase("\n")][TestCase("\r\n")][TestCase("\r")]
        public void SharedReaderNormalizesAuthoredNewlinesWithoutControlGlyphs(string newline)
        {reader.Open("ONE"+newline+newline+"TWO");CollectionAssert.AreEqual(new[]{"ONE","","TWO"},reader.VisibleLines);}
        sealed class ReaderActionPart:Part
        {
            public bool Offered=true;public int Executed;public string Label="ACTION-BEGIN "+new string('w',150)+" ACTION-END";
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID=="GetInventoryActions"&&Offered)e.GetParameter<InventoryActionList>("Actions").AddAction("ReaderAction",Label,"ReaderAction",'z',999);
                if(e.ID=="InventoryAction"&&e.GetStringParameter("Command")=="ReaderAction")Executed++;
                return true;
            }
        }
        sealed class ReaderMutationEnhancement:IItemEnhancement
        {public Action OnDescribe;public override string GetEffectDescription(){OnDescribe?.Invoke();return "DO-NOT-PUBLISH";}}
        sealed class ReaderTestEffect:Effect { public int Index; public override string DisplayName=>"Reader status "+Index; }
    }
}
