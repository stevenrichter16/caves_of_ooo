using System.Collections.Generic;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class SecondExplorationRopeTravelTests : SecondExplorationFixture
    {
        [Test] public void ExistingTaggedStairsCannotDivertTheInstalledRopeRoundTrip()
        {
            Zone.RemoveEntity(Actor); Zone=new Zone("Overworld.2.7.1"); SettlementRuntime.ActiveZone=Zone;
            Assert.True(Zone.AddEntity(Actor,10,10)); var lower=new Zone("Overworld.2.7.2");
            var a=Place("RopeAnchor");var b=Factory.CreateEntity("RopeAnchor");Assert.True(lower.AddEntity(b,11,10));
            a.AddPart(new RopeShortcutPart{ZoneID=Zone.ZoneID,OtherZoneID=lower.ZoneID,X=11,Y=10,OtherX=11,OtherY=10,Down=true});
            b.AddPart(new RopeShortcutPart{ZoneID=lower.ZoneID,OtherZoneID=Zone.ZoneID,X=11,Y=10,OtherX=11,OtherY=10,Down=false});
            var oldDown=Place("StairsDown",40,10);var oldUp=Factory.CreateEntity("StairsUp");Assert.True(lower.AddEntity(oldUp,40,10));
            Assert.True(oldDown.HasTag("StairsDown"));Assert.True(oldUp.HasTag("StairsUp"));
            var manager=OverworldZoneManager.CreateDetached(Factory,64);
            manager.ReplaceLoadedState(new Dictionary<string,Zone>{{Zone.ZoneID,Zone},{lower.ZoneID,lower}},Zone.ZoneID,new Dictionary<string,List<ZoneConnection>>());
            Carry("KnotflaxCord");Carry("KnotflaxCord");Assert.True(Act(a,"RigRopeShortcut"));Assert.True(Zone.MoveEntity(Actor,11,10));
            var down=ZoneTransitionSystem.TransitionPlayerVertical(Actor,Zone,true,11,10,manager);
            Assert.True(down.Success);Assert.AreSame(lower,down.NewZone);Assert.AreEqual((11,10),(down.NewPlayerX,down.NewPlayerY),"The existing distant staircase must not capture the rope landing.");
            var up=ZoneTransitionSystem.TransitionPlayerVertical(Actor,lower,false,11,10,manager);
            Assert.True(up.Success);Assert.AreSame(Zone,up.NewZone);Assert.AreEqual((11,10),(up.NewPlayerX,up.NewPlayerY));
            Assert.NotNull(Zone.GetEntityCell(oldDown));Assert.NotNull(lower.GetEntityCell(oldUp));Assert.AreEqual(0,Count("KnotflaxCord"));
        }
    }
}
