using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Player-flow hypotheses: a sealed door changes state without replacing
    // its native identity; inhabitants and carried supplies cross native zones.
    public sealed class SanctumRenderingAdversarialTests
    {
        [TestCase("IronKey",false)] [TestCase("IronKey",true)]
        [TestCase("Tepuibone",false)] [TestCase("Tepuibone",true)]
        public void Adversarial_SharedStumpSuppliesKeepTheirShapeInStillleaf(string bp,bool crossDepth)
        {
            var factory=GrovelandsCompositionTests.Factory();var first=new Zone("Overworld.2.4.1");
            var second=crossDepth?new Zone("Overworld.2.4.2"):first;
            var owner=factory.CreateEntity(bp);Assert.IsTrue(owner.GetPart<PhysicsPart>().Takeable);
            Assert.IsTrue(first.AddEntity(owner,10,10));
            var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            string initial=SpawnRing3DRecipes.Resolve(first,owner,catalog).ModelId;Assert.NotNull(initial);
            if(crossDepth){first.RemoveEntity(owner);Assert.IsTrue(second.AddEntity(owner,20,10));}
            for(int x=21;x<30;x++)
            {
                Assert.IsTrue(second.MoveEntity(owner,x,10));
                var current=SpawnRing3DRecipes.Resolve(second,owner,catalog);
                Assert.IsTrue(current.Transient);Assert.IsFalse(current.Batched);Assert.AreEqual(initial,current.ModelId);
            }
        }

        [Test]
        public void Adversarial_EncasedEldersFaceIntoTheNaveAndChangePoseWithoutReplacingTheirBody()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.5.4.2"))
            {
                f.Zone=new Zone("Overworld.5.4.2");f.Bind(f.Zone);f.Set("FullReveal",true);
                var elder=f.Add("EncasedElder",20,9);f.Refresh();
                var view=f.View(elder);Assert.NotNull(view);
                Assert.Less(Vector3.Dot(view.transform.forward,Vector3.forward),-.99f,"North-wall faces point south into the nave.");
                Assert.IsTrue(f.Zone.MoveEntity(elder,20,15));f.Refresh(new HashSet<int>());
                Assert.AreSame(view,f.View(elder),"Moving a native owner retains its body variant and view.");
                Assert.Greater(Vector3.Dot(view.transform.forward,Vector3.forward),.99f,"South-wall faces point north into the nave.");
            }
        }

        // A displaced elder has real eastward VisualFacing, but its permanent
        // casing should settle inward again after a real damage gesture. Both
        // refresh and ordinary-frame expiry must work without replaying a plan
        // or replacing the owner/body. Advancing only the private action deadline
        // makes the two expiry paths deterministic without sleeping in EditMode.
        [TestCase(9,false)] [TestCase(15,false)]
        [TestCase(9,true)] [TestCase(15,true)]
        public void Adversarial_DamagedElderRecoversItsInwardRestPoseAfterActionExpiry(int row,bool expireThroughRefresh)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.5.4.2"))
            {
                f.Zone=new Zone("Overworld.5.4.2");f.Bind(f.Zone);f.Set("FullReveal",true);
                var elder=f.Add("EncasedElder",20,row);f.Refresh();
                var body=f.View(elder);Assert.IsNull(body.GetComponentInChildren<Animator>(true));
                Vector3 inward=row<12?Vector3.back:Vector3.forward;
                Assert.Greater(Vector3.Dot(body.transform.forward,inward),.99f);
                Assert.IsTrue(MovementSystem.ForceMoveTo(elder,f.Zone,21,row));
                Assert.AreEqual(EntityVisualFacing.East,elder.GetPart<RenderPart>().VisualFacing);
                f.Refresh(new HashSet<int>());
                int hp=elder.GetStatValue("Hitpoints");
                CombatSystem.ApplyDamage(elder,1,null,f.Zone);
                Assert.AreEqual(hp-1,elder.GetStatValue("Hitpoints"),"The native damage route must actually land.");
                int version=f.Zone.EntityVersion;
                ExpireAction(f,elder,expireThroughRefresh);
                Assert.AreSame(body,f.View(elder));Assert.IsTrue(f.Zone.GetCell(21,row).Objects.Contains(elder));
                Assert.AreEqual(version,f.Zone.EntityVersion,"Rest pose must not regenerate or move native owners.");
                Assert.Greater(Vector3.Dot(body.transform.forward,inward),.99f,"Finished unrigged hit pose must expose the elder's face toward the nave again.");
                f.Refresh(new HashSet<int>());
                Assert.AreSame(body,f.View(elder));Assert.Greater(Vector3.Dot(body.transform.forward,inward),.99f);
            }
        }

        // EditMode intentionally bypasses movement animation. This countercheck
        // exercises real forced movement and its native facing write, while not
        // claiming coverage of the separate in-play interpolation path.
        [TestCase(9)] [TestCase(15)]
        public void Adversarial_SameRowDisplacementKeepsTheElderBodyAndInwardRestPose(int row)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.5.4.2"))
            {
                f.Zone=new Zone("Overworld.5.4.2");f.Bind(f.Zone);f.Set("FullReveal",true);
                var elder=f.Add("EncasedElder",20,row);f.Refresh();var body=f.View(elder);
                Assert.IsTrue(MovementSystem.ForceMoveTo(elder,f.Zone,21,row));
                Assert.AreEqual(EntityVisualFacing.East,elder.GetPart<RenderPart>().VisualFacing);
                f.Refresh(new HashSet<int>());
                Assert.AreSame(body,f.View(elder));Assert.AreEqual(Village3DProjection.CellCentre(21,row),body.transform.position);
                Assert.Greater(Vector3.Dot(body.transform.forward,row<12?Vector3.back:Vector3.forward),.99f);
            }
        }

        [TestCase(EntityVisualFacing.East,false)] [TestCase(EntityVisualFacing.West,false)]
        [TestCase(EntityVisualFacing.East,true)] [TestCase(EntityVisualFacing.West,true)]
        public void Adversarial_OrdinaryAnimatedPlayerKeepsNativeFacingAfterDamageExpiry(EntityVisualFacing facing,bool expireThroughRefresh)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.5.4.2"))
            {
                f.Zone=new Zone("Overworld.5.4.2");f.Bind(f.Zone);f.Set("FullReveal",true);
                var player=f.Add("Player",20,12);f.Refresh();var body=f.View(player);
                Assert.NotNull(body.GetComponentInChildren<Animator>(true),"This countercontrol must use the real animated player model.");
                player.GetPart<RenderPart>().VisualFacing=facing;
                Vector3 direction=facing==EntityVisualFacing.East?Vector3.right:Vector3.left;
                int hp=player.GetStatValue("Hitpoints");CombatSystem.ApplyDamage(player,1,null,f.Zone);
                Assert.AreEqual(hp-1,player.GetStatValue("Hitpoints"));
                Assert.Greater(Vector3.Dot(body.transform.forward,direction),.99f);
                ExpireAction(f,player,expireThroughRefresh);
                Assert.AreSame(body,f.View(player));Assert.Greater(Vector3.Dot(body.transform.forward,direction),.99f,
                    "The encased-elder correction must not reset an ordinary actor to recipe quarter-turn zero.");
                f.Refresh(new HashSet<int>());Assert.Greater(Vector3.Dot(body.transform.forward,direction),.99f);
            }
        }

        private static void ExpireAction(SpawnRing3DIntegrationFixture f,Entity owner,bool throughRefresh)
        {
            var field=f.Presenter.GetType().GetField("views",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.NotNull(field);var views=(System.Collections.IDictionary)field.GetValue(f.Presenter);
            Assert.IsTrue(views.Contains(owner));var view=views[owner];var until=view.GetType().GetField("ActionUntil");
            Assert.NotNull(until);Assert.Greater((float)until.GetValue(view),0f,"The actual damage hook must arm a gesture before expiry.");
            float expired=Time.unscaledTime*.5f;Assert.Greater(expired,0f);until.SetValue(view,expired);
            if(throughRefresh)
            {f.Light.Compute(f.Zone);f.Call("Refresh",f.Light,new HashSet<int>());}
            else f.Frame();
        }

        [TestCase(false)] [TestCase(true)]
        public void Adversarial_ArchiveDoorArtFollowsRealKeyUnlockAndKeepsTheSameOwner(bool matching)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.2.4.2"))
            {
                f.Set("FullReveal",true);var door=f.Zone.GetAllEntities().Single(e=>e.BlueprintName=="SealedLibraryDoor");
                var before=SpawnRing3DRecipes.Resolve(f.Zone,door,f.Library.Definition);
                Assert.NotNull(before.ModelId,before.Failure);StringAssert.StartsWith("stillleaf-door-",before.ModelId);
                var visitor=new Entity();var inventory=new InventoryPart();visitor.AddPart(inventory);
                var key=new Entity();key.AddPart(new KeyPart{KeyId=matching?SealedLibraryBuilder.KeyID:"wrong-key"});inventory.Objects.Add(key);
                var action=GameEvent.New("InventoryAction");action.SetParameter("Command","Unlock");action.SetParameter("Actor",(object)visitor);
                door.FireEventAndRelease(action);f.Refresh(new HashSet<int>());
                var after=SpawnRing3DRecipes.Resolve(f.Zone,door,f.Library.Definition);
                Assert.AreSame(door,after.Owner);Assert.IsTrue(f.Rendered(door));
                Assert.AreEqual(!matching,door.GetPart<SealedLibraryBarrierPart>().IsClosed);
                if(matching)
                {
                    StringAssert.StartsWith("stillleaf-open-door-",after.ModelId);
                    Assert.AreNotEqual(before.ModelId,after.ModelId);
                    var model=f.Library.FindModel(after.ModelId).GetComponent<MeshFilter>().sharedMesh;
                    Assert.Less(model.bounds.max.y,.30f,"Open art must leave a visibly passable threshold.");
                }
                else Assert.AreEqual(before.ModelId,after.ModelId);
                Assert.AreEqual(0,f.Get<int>("VoxelMissingMeshCount"));
            }
        }

        [TestCase("EncasedElder")] [TestCase("ChoirTendril")]
        [TestCase("CaveBear")] [TestCase("CaveSlime")] [TestCase("IronshodBoots")]
        [TestCase("Torch")] [TestCase("DriedMeat")] [TestCase("HealingTonic")]
        public void Adversarial_PortableOwnersKeepBodyAcrossDepthAndRejectDepartedOrReskinnedActors(string bp)
        {
            var factory=GrovelandsCompositionTests.Factory();var first=new Zone("Overworld.5.4.1");var second=new Zone("Overworld.5.4.2");
            var owner=factory.CreateEntity(bp);Assert.IsTrue(first.AddEntity(owner,10,10));
            var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            var initial=SpawnRing3DRecipes.Resolve(first,owner,catalog);Assert.NotNull(initial.ModelId,initial.Failure);
            Assert.IsTrue(initial.Transient);Assert.IsFalse(initial.Batched);
            first.RemoveEntity(owner);Assert.IsNull(SpawnRing3DRecipes.Resolve(first,owner,catalog).ModelId);
            Assert.IsTrue(second.AddEntity(owner,20,12));
            for(int x=21;x<30;x++)
            {
                Assert.IsTrue(second.MoveEntity(owner,x,12));
                var moved=SpawnRing3DRecipes.Resolve(second,owner,catalog);Assert.AreEqual(initial.ModelId,moved.ModelId);
                Assert.AreEqual(Village3DProjection.CellCentre(x,12),moved.Position);
            }
            if(!owner.HasTag("Creature"))return;
            string glyph=owner.GetPart<RenderPart>().RenderString;owner.GetPart<RenderPart>().RenderString="?";
            Assert.IsNull(SpawnRing3DRecipes.Resolve(second,owner,catalog).ModelId);
            owner.GetPart<RenderPart>().RenderString=glyph;Assert.AreEqual(initial.ModelId,SpawnRing3DRecipes.Resolve(second,owner,catalog).ModelId);
        }

        [TestCase("CaveBear")] [TestCase("CaveSlime")] [TestCase("IronshodBoots")]
        [TestCase("EncasedElder")] [TestCase("ChoirTendril")]
        public void Adversarial_TheSamePortableBodySurvivesCrossingBetweenTheTwoNewAreas(string bp)
        {
            var factory=GrovelandsCompositionTests.Factory();
            var first=new Zone("Overworld.2.4.2");var second=new Zone("Overworld.5.4.2");
            var owner=factory.CreateEntity(bp);first.AddEntity(owner,20,12);
            var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            var before=SpawnRing3DRecipes.Resolve(first,owner,catalog);Assert.NotNull(before.ModelId,before.Failure);
            first.RemoveEntity(owner);second.AddEntity(owner,25,12);
            Assert.AreEqual(before.ModelId,SpawnRing3DRecipes.Resolve(second,owner,catalog).ModelId);
            Assert.IsNull(SpawnRing3DRecipes.Resolve(first,owner,catalog).ModelId);
        }

        [TestCase("Stillleaf",true)] [TestCase("Renamed archive",true)]
        [TestCase("Renamed archive",false)] [TestCase("the Deepest Cathedral",false)]
        public void Adversarial_StillleafUsesTheActualArchiveProfileAcrossRenames(string name,bool retainProfile)
        {
            var manager=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);
            var poi=manager.WorldMap.GetPOI(2,4);poi.Name=name;if(!retainProfile)poi.Profile=null;
            var zone=manager.GetZone("Overworld.2.4.2");
            Assert.AreEqual(retainProfile,AreaCompositionScope.Allows(zone));
            if(retainProfile)Assert.AreEqual(1,zone.GetAllEntities().Count(e=>e.BlueprintName=="SealedLibraryDoor"));
        }

        [TestCase("Overworld.5.4.2","SubstrateVault")]
        [TestCase("Overworld.2.4.2","LibraryTepuiboneWall")]
        public void Adversarial_RemovingOneNativeOwnerDoesNotRestoreItOrEraseItsNeighbor(string id,string bp)
        {
            using(var f=new SpawnRing3DIntegrationFixture(id))
            {
                f.Zone=new Zone(id);f.Bind(f.Zone);f.Set("FullReveal",true);
                var removed=f.Add(bp,20,10);var kept=f.Add(bp,21,10);f.Refresh();
                Assert.IsTrue(f.Rendered(removed));Assert.IsTrue(f.Rendered(kept));
                f.Zone.RemoveEntity(removed);f.Refresh(new HashSet<int>());
                Assert.IsFalse(f.Rendered(removed));Assert.IsTrue(f.Rendered(kept));
                Assert.AreEqual(1,f.Zone.EntityCount);Assert.AreEqual(0,f.Get<int>("VoxelMissingMeshCount"));
            }
        }
    }
}
