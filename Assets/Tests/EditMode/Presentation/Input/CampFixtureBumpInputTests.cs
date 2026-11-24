using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Tests
{
    public sealed class CampFixtureBumpInputTests : LookKeyFixture
    {
        Entity Box(int x=11,int y=10,bool playerHeld=true)
        {
            Diag.ResetAll();Diag.SetChannel("movement",true);
            var box=Item("CampBox");
            box.GetPart<PhysicsPart>().Solid=true;
            box.GetPart<PhysicsPart>().Takeable=false;
            if(playerHeld) {
                box.AddPart(new InstallablePart { Mode=InstallablePart.Portable, State=InstallablePart.Installed });
                box.AddPart(new InstallationMarkerPart { Origin=HeldFixtureActions.PlayerOrigin, InstallationId="player:"+box.ID, RetainZone=true });
                box.SetTag("Owned","true");
            }
            Assert.True(Zone.AddEntity(box,x,y));return box;
        }
        [TestCase(1,0,Key.D)] [TestCase(-1,0,Key.A)]
        [TestCase(0,1,Key.S)] [TestCase(0,-1,Key.W)]
        public void RealMovementBumpKeepsOwnedInstalledBoxAndCostsNoTurn(int dx,int dy,Key key)
        {
            var box=Box(10+dx,10+dy);var child=Item("Dagger");
            Assert.True(box.GetPart<ContainerPart>().AddItem(child));
            var input=Input();int hp=box.GetPart<DestructiblePart>().HP;
            int tick=input.TurnManager.TickCount,energy=input.TurnManager.GetEnergy(Player);
            Press(key,()=>Tick(input));
            Assert.AreEqual(hp,box.GetPart<DestructiblePart>().HP,"Movement must not spend the player's own camp box.");
            CollectionAssert.Contains(Zone.GetCell(10+dx,10+dy).Objects,box);Assert.AreEqual((10,10),Zone.GetEntityPosition(Player));
            CollectionAssert.Contains(box.GetPart<ContainerPart>().Contents,child);
            Assert.AreEqual(tick,input.TurnManager.TickCount);Assert.AreEqual(energy,input.TurnManager.GetEnergy(Player));
            StringAssert.Contains("deliberately break",string.Join("|",MessageLog.GetRecent(3)));
            Assert.True(Diag.Snapshot(50).Any(r=>r.Kind=="CampFixtureBumpBlocked"));
        }
        [TestCase("generated")] [TestCase("handed-over")] [TestCase("loose")] [TestCase("owned-tag-only")]
        public void OrdinaryBlockingCratesAndUnheldFixturesRetainBumpBreaking(string state)
        {
            var box=Box(playerHeld:state!="owned-tag-only");
            if(state=="owned-tag-only")box.SetTag("Owned","true");
            else if(state=="generated")box.GetPart<InstallationMarkerPart>().Origin="Generated";
            else if(state=="handed-over"){box.RemovePart(box.GetPart<InstallationMarkerPart>());box.Tags.Remove("Owned");}
            else box.GetPart<InstallablePart>().State=InstallablePart.Loose;
            var input=Input();int hp=box.GetPart<DestructiblePart>().HP;
            Press(Key.D,()=>Tick(input));
            Assert.Less(box.GetPart<DestructiblePart>().HP,hp);
        }
        [Test]
        public void ExplicitBreakRowStillExistsAndDirectStructuralDamageStillWorks()
        {
            var box=Box();var rows=new InventoryActionList();
            var ev=GameEvent.New("GetInventoryActions");ev.SetParameter("Actor",Player);ev.SetParameter("Zone",Zone);ev.SetParameter("Actions",rows);
            box.FireEventAndRelease(ev);
            Assert.True(rows.Actions.Any(r=>r.Command==DestructiblePart.BreakCommand));
            int hp=box.GetPart<DestructiblePart>().HP;
            DestructionSystem.Damage(box,1,Player,Zone);Assert.AreEqual(hp-1,box.GetPart<DestructiblePart>().HP);
        }
        [Test]
        public void InstalledPlayerFixtureWithoutOwnedTagRemainsProtectedLikeLegacyMarkers()
        {
            var box=Box();box.Tags.Remove("Owned");var input=Input();int hp=box.GetPart<DestructiblePart>().HP;
            Press(Key.D,()=>Tick(input));Assert.AreEqual(hp,box.GetPart<DestructiblePart>().HP);
        }
    }
}

