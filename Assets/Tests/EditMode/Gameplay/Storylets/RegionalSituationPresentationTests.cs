using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class RegionalSituationPresentationTests
    {
        HotbarSaveFixture scope; OverworldZoneManager manager; Zone zone; Entity player, giver; RegionalRequestPart request;
        [SetUp] public void SetUp()
        {
            scope=new HotbarSaveFixture(false,false);CinderholdCompositionTests.LoadLoot();
            var factory=GrovelandsCompositionTests.Factory();manager=OverworldZoneManager.CreateDetached(factory,64);
            player=factory.CreateEntity("Player");StoryletPart.LocalPlayer=player;StoryletPart.Current=new StoryletPart();
            zone=manager.GetZone("Overworld.3.6.0");manager.SetActiveZone(zone);SettlementRuntime.ActiveZone=zone;
            giver=MorrowfastSceneRuntime.FindOwner(zone,"southwest-craftsperson");request=giver.GetPart<RegionalRequestPart>();
            Assert.NotNull(request);var at=zone.GetEntityPosition(giver);
            var cell=CinderholdCompositionTests.Neighbors(at.x,at.y).Select(p=>zone.GetCell(p.x,p.y)).First(c=>c!=null&&!c.BlocksMovement());
            Assert.IsTrue(zone.AddEntity(player,cell.X,cell.Y));
        }
        [TearDown] public void TearDown(){scope?.Dispose();LootTableRegistry.ResetForTests();}
        [Test] public void DistantAvailableCueDoesNotGenerateSourceAndBecomesActiveAfterReading()
        {
            int count=manager.CachedZoneCount;var old=zone.GetEntityPosition(player);
            var far=Enumerable.Range(0,Zone.Width).SelectMany(x=>Enumerable.Range(0,Zone.Height).Select(y=>zone.GetCell(x,y))).First(c=>!c.BlocksMovement()&&Math.Abs(c.X-old.x)>10);
            Assert.IsTrue(zone.MoveEntity(player,far.X,far.Y));
            Assert.AreEqual(QuestCueState.Available,QuestCueStateQuery.Evaluate(giver,zone,player));
            Assert.AreEqual(count,manager.CachedZoneCount);
            Assert.IsTrue(zone.MoveEntity(player,old.x,old.y));Assert.IsTrue(request.TryAct(player,zone,"read"));
            Assert.AreEqual(QuestCueState.Active,QuestCueStateQuery.Evaluate(giver,zone,player));
            Assert.IsTrue(request.TryAct(player,zone,"release"));
            Assert.AreEqual(QuestCueState.Available,QuestCueStateQuery.Evaluate(giver,zone,player));
        }
        [TestCase("dead")][TestCase("hidden")][TestCase("detached")][TestCase("hostile")][TestCase("forged-instance")]
        public void IneligibleNativeRecipientNeverPromisesWork(string reason)
        {
            Assert.AreEqual(QuestCueState.Available,QuestCueStateQuery.Evaluate(giver,zone,player));
            string faction=giver.GetTag("Faction");int old=PlayerReputation.Get(faction);
            try
            {
                if(reason=="dead")giver.GetStat("Hitpoints").Value=0;
                if(reason=="hidden")giver.GetPart<RenderPart>().Visible=false;
                if(reason=="detached")zone.RemoveEntity(giver);
                if(reason=="hostile")PlayerReputation.Set(faction,-1000);
                if(reason=="forged-instance")request.InstanceId="forged";
                Assert.AreEqual(QuestCueState.None,QuestCueStateQuery.Evaluate(giver,zone,player));
            }
            finally{PlayerReputation.Set(faction,old);}
        }
        [Test] public void RealWorldActionMenuOffersRequestWithoutSyntheticZoneParameter()
        {
            var actions=WorldInteractionSystem.GatherActions(giver,player);
            Assert.IsTrue(actions.Any(a=>a.Command=="RegionalRequest:read"));
            Assert.IsTrue(actions.Any(a=>a.Command=="Chat"),"The native conversation remains reachable.");
            Assert.IsTrue(request.TryAct(player,zone,"read"));
            actions=WorldInteractionSystem.GatherActions(giver,player);
            Assert.IsTrue(actions.Any(a=>a.Command=="RegionalRequest:deliver"));
            Assert.IsTrue(actions.Any(a=>a.Command=="RegionalRequest:release"));
        }
        [Test] public void JournalContainsPersistentRequestAndExistingDirectionsWithoutOverwritingEither()
        {
            Assert.IsTrue(request.TryAct(player,zone,"read"));
            var clerk=MorrowfastSceneRuntime.FindOwner(zone,"east-robed-resident");var at=zone.GetEntityPosition(clerk);
            var cell=CinderholdCompositionTests.Neighbors(at.x,at.y).Select(p=>zone.GetCell(p.x,p.y)).First(c=>c!=null&&!c.BlocksMovement());
            Assert.IsTrue(zone.MoveEntity(player,cell.X,cell.Y));
            var leads=RegionalGuidance.Build(zone,clerk,player);Assert.IsNotEmpty(leads);
            Assert.IsNull(RegionalGuidance.TryRemember(zone,clerk,player,leads[0].DestinationZoneID));
            var go=new GameObject("regional-journal-test");
            try
            {
                var ui=go.AddComponent<QuestLogUI>();ui.Open();ui.HandleInput(new Keys(KeyCode.Tab));
                Assert.IsTrue(ui.NotesVisible);var lines=(List<string>)typeof(QuestLogUI).GetField("_noteLines",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);
                string text=string.Join(" ",lines).Replace(" ","");
                foreach(var note in RegionalSituationNotes.Read(player).Concat(RegionalTravelNotes.Read(player)))StringAssert.Contains(note.Replace(" ",""),text);
                Assert.IsTrue(request.TryAct(player,zone,"read")==false,"The distant read cannot alter the saved note.");
                Assert.AreEqual(1,RegionalSituationNotes.Read(player).Count);Assert.AreEqual(1,RegionalTravelNotes.Read(player).Count);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        private sealed class Keys:IInputProbe{readonly KeyCode key;public Keys(KeyCode key){this.key=key;}public bool GetKeyDown(KeyCode value)=>value==key;}
    }
}
