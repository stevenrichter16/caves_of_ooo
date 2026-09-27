using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    // Actual factory/InventorySystem fixture gear. This is attachment evidence,
    // not ordinary-game acquisition, combat balance or visual-fit acceptance.
    public sealed class SpreadEquipmentIntegrationTests
    {
        private const string Spread = "Overworld.12.10.0";
        private static Mesh[] Meshes(GameObject root)
            => root.GetComponentsInChildren<MeshFilter>(true).Select(x => x.sharedMesh)
                .Concat(root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(x => x.sharedMesh)).ToArray();
        private static Mesh Expected(string modelId)
        {
            if (modelId.StartsWith("spread-portable-",StringComparison.Ordinal))
                return SpreadPortable3DLibrary.Load().Find(modelId).Mesh;
            var library = Resources.Load("SpreadEquipment3D/Library");
            Assert.NotNull(library,"Missing persistent fitted-wearable library.");
            var method = library.GetType().GetMethod("Find");Assert.NotNull(method);
            var entry = method.Invoke(library,new object[]{modelId});Assert.NotNull(entry,modelId);
            return (Mesh)entry.GetType().GetField("Mesh").GetValue(entry);
        }
        private static GameObject Exact(SpawnRing3DIntegrationFixture f,Entity actor,Entity item,string modelId,int pieces=1)
        {
            Assert.True(f.Equipment(actor,item,out var view),"No exact current equipped view: "+item.BlueprintName);
            Assert.True(SpawnRing3DIntegrationFixture.Drawn(view));
            var meshes=Meshes(view);Assert.AreEqual(pieces,meshes.Length,"One native item owns the declared fitted pieces.");
            var expected=Expected(modelId);Assert.NotNull(expected);foreach(var mesh in meshes)Assert.AreSame(expected,mesh,modelId);
            var library=SpreadPortable3DLibrary.Load();
            var material=f.Get<NativeZone3DRenderSurface>("ActiveSurface").MaterialFor(library.Material);
            foreach(var r in view.GetComponentsInChildren<Renderer>(true))
            {Assert.AreEqual(1,r.sharedMaterials.Length);Assert.AreSame(material,r.sharedMaterial);Assert.AreSame(library.Palette,r.sharedMaterial.GetTexture("_BaseMap"));}
            Assert.AreEqual(0,view.GetComponentsInChildren<Collider>(true).Length);
            Assert.AreSame(actor,item.GetPart<PhysicsPart>().Equipped);Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
            Assert.IsNotNull(actor.GetPart<InventoryPart>().FindEquippedBodyPart(item));return view;
        }
        [TestCase("Dagger","Hand")]
        [TestCase("ForgedWeapon","Hand")]
        [TestCase("LeatherArmor","Body")]
        [TestCase("ChainMail","Body")]
        [TestCase("LongSword","Hand")]
        [TestCase("Battleaxe","Hand")]
        [TestCase("Greatsword","Hand")]
        [TestCase("ShortSword","Hand")]
        [TestCase("Mace","Hand")]
        [TestCase("Spear","Hand")]
        [TestCase("Hatchet","Hand")]
        [TestCase("Claymore","Hand")]
        [TestCase("Cudgel","Hand")]
        [TestCase("Buckler","Hand")]
        [TestCase("IronHelmet","Head")]
        [TestCase("LeatherBoots","Feet")]
        [TestCase("LeatherGloves","Handwear")]
        [TestCase("LeatherCap","Head")]
        [TestCase("IronshodBoots","Feet")]
        [TestCase("WardedCloak","Back")]
        [TestCase("IronBuckler","Hand")]
        [TestCase("PlateArmor","Body")]
        [TestCase("Cloak","Back")]
        [TestCase("LoanerDagger","Hand")]
        [TestCase("LoanerSpear","Hand")]
        [TestCase("LoanerLongsword","Hand")]
        [TestCase("Warhammer","Hand")]
        [TestCase("ChoirSpine","Hand")]
        [TestCase("OldWorldPipe","Hand")]
        [TestCase("Sporeblade","Hand")]
        [TestCase("FlamingSword","Hand")]
        [TestCase("IceSword","Hand")]
        [TestCase("CryoLance","Hand")]
        [TestCase("EmberSpear","Hand")]
        [TestCase("AcidicDagger","Hand")]
        [TestCase("VenomDagger","Hand")]
        [TestCase("ThunderHammer","Hand")]
        [TestCase("EchoKnife","Hand")]
        [TestCase("TemporalShard","Hand")]
        [TestCase("SeveranceEdge","Hand")]
        [TestCase("GlassblownStiletto","Hand")]
        [TestCase("DissolutionMaul","Hand")]
        [TestCase("FirstRootGlaive","Hand")]
        [TestCase("PalimpsestBlade","Hand")]
        [TestCase("BreacherCleaver","Hand")]
        [TestCase("Torch","Hand")]
        [TestCase("TemperedLongSword","Hand")]
        [TestCase("CounterweightMaul","Hand")]
        [TestCase("FineRingMail","Body")]
        [TestCase("RivetedPlate","Body")]
        public void AllConcreteEquippedItemsUseExactPersistentFormAndOwnedPalette(string name,string slot)
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                f.CleanGear(f.Player);var item=f.Equip(f.Player,name);int version=EquipmentChangeBus.GlobalVersion;var slots=f.Player.GetPart<InventoryPart>().EquippedItems.ToArray();
                f.Refresh();var view=Exact(f,f.Player,item,(slot=="Hand"?"spread-portable-":"spread-worn-")+name.ToLowerInvariant(),slot=="Feet"||slot=="Handwear"?2:1);
                f.Refresh();Assert.True(f.Equipment(f.Player,item,out var same));Assert.AreSame(view,same);
                Assert.AreEqual(version,EquipmentChangeBus.GlobalVersion);CollectionAssert.AreEqual(slots,f.Player.GetPart<InventoryPart>().EquippedItems);
                Assert.AreEqual(0,f.Get<IReadOnlyList<Village3DEquipmentFallback>>("EquipmentFallbacks").Count(x=>ReferenceEquals(x.Actor,f.Player)&&ReferenceEquals(x.Item,item)));
            }
        }
        [TestCase("foreign-physics")][TestCase("foreign-equipped-owner")][TestCase("carried")][TestCase("missing-body")][TestCase("foreign-equip")][TestCase("foreign-render")][TestCase("natural")]
        public void MalformedNativeEquippedStateCannotBorrowTheScopedItemForm(string mode)
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                f.CleanGear(f.Player);var item=f.Equip(f.Player,"Dagger");
                if(mode=="foreign-physics")item.GetPart<PhysicsPart>().ParentEntity=new Entity();
                if(mode=="foreign-equipped-owner")item.GetPart<PhysicsPart>().Equipped=new Entity();
                if(mode=="carried")Assert.True(InventorySystem.UnequipItem(f.Player,item));
                if(mode=="missing-body")f.Player.GetPart<InventoryPart>().FindEquippedBodyPart(item)._Equipped=null;
                if(mode=="foreign-equip")item.GetPart<EquippablePart>().ParentEntity=new Entity();
                if(mode=="foreign-render")item.GetPart<RenderPart>().ParentEntity=new Entity();
                if(mode=="natural")item.SetTag("Natural");
                f.Refresh();Assert.False(f.Equipment(f.Player,item,out _),mode);
            }
        }
        [Test] public void NativePairSlotsFollowTwoRealBonesWithoutClaimingHeldSockets()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                f.CleanGear(f.Player);var gloves=f.Equip(f.Player,"LeatherGloves");var boots=f.Equip(f.Player,"LeatherBoots");var sword=f.Equip(f.Player,"Greatsword");f.Refresh();
                var g=Exact(f,f.Player,gloves,"spread-worn-leathergloves",2);var b=Exact(f,f.Player,boots,"spread-worn-leatherboots",2);Exact(f,f.Player,sword,"spread-portable-greatsword");
                foreach(var pair in new[]{(g,"Hand"),(b,"Feet")})
                {
                    var skins=pair.Item1.GetComponentsInChildren<SkinnedMeshRenderer>(true);Assert.AreEqual(2,skins.Length);
                    var bones=skins.Select(x=>x.bones.Single()).ToArray();Assert.AreNotSame(bones[0],bones[1]);
                    Assert.True(bones.All(x=>x.IsChildOf(f.View(f.Player).transform)));
                    Assert.True(bones.Any(x=>x.name.EndsWith(".L",StringComparison.Ordinal)));Assert.True(bones.Any(x=>x.name.EndsWith(".R",StringComparison.Ordinal)));
                    Assert.True(bones.All(x=>x.name.Contains(pair.Item2)));
                }
                Assert.AreEqual(1,f.Player.GetPart<InventoryPart>().EquippedItems.Values.Count(x=>ReferenceEquals(x,gloves)));
                Assert.AreEqual(1,f.Player.GetPart<InventoryPart>().EquippedItems.Values.Count(x=>ReferenceEquals(x,boots)));
            }
        }
        [Test] public void SourceRigWithoutHumanoidSocketsKeepsExplicitAnatomyFallback()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                var actor=f.Add("YellowfootWayfarer");f.CleanGear(actor);var dagger=f.Equip(actor,"Dagger");f.Refresh();
                Assert.False(f.Equipment(actor,dagger,out _));Assert.AreSame(actor,dagger.GetPart<PhysicsPart>().Equipped);
                Assert.AreEqual(1,f.Get<IReadOnlyList<Village3DEquipmentFallback>>("EquipmentFallbacks").Count(x=>ReferenceEquals(x.Actor,actor)&&ReferenceEquals(x.Item,dagger)));
            }
        }
        [Test] public void OutsideSpreadRetainsLegacyModelAndDoesNotBorrowScopedPalette()
        {
            using(var f=new SpawnRing3DIntegrationFixture(SpawnRing3DIntegrationFixture.Grove))
            {
                Assert.False(SpreadPresentationScope.IsActive(f.Zone));f.CleanGear(f.Player);var item=f.Equip(f.Player,"Dagger");f.Refresh();
                Assert.True(f.Equipment(f.Player,item,out var view));Assert.True(SpawnRing3DIntegrationFixture.Drawn(view));
                Assert.False(Meshes(view).Contains(SpreadPortable3DLibrary.Load().Find("spread-portable-dagger").Mesh));
            }
        }
        [Test] public void NativeHideUnequipAndSaveReplaceOnlyOwnedAttachmentGraph()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                f.CleanGear(f.Player);var item=f.Equip(f.Player,"Hatchet");f.Refresh();var old=Exact(f,f.Player,item,"spread-portable-hatchet");
                Assert.True(InventorySystem.UnequipItem(f.Player,item));f.Frame();Assert.False(f.Equipment(f.Player,item,out _));SpawnRing3DIntegrationFixture.Hidden(old);
                Assert.True(InventorySystem.Equip(f.Player,item));f.Frame();old=Exact(f,f.Player,item,"spread-portable-hatchet");
                var loaded=f.RoundTrip();f.BindLoaded(loaded);var current=f.Player.GetPart<InventoryPart>().GetAllEquipped().Single(x=>x.ID==item.ID);
                Assert.AreNotSame(item,current);SpawnRing3DIntegrationFixture.Hidden(old);Exact(f,f.Player,current,"spread-portable-hatchet");Assert.False(f.Equipment(f.Player,item,out _));
            }
        }
        [TestCase("palette-override")][TestCase("mesh-copy")][TestCase("hidden-piece")][TestCase("foreign-bone")]
        public void StyleEvidenceProvesEveryCurrentPieceAndRefusesTampering(string mode)
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                f.CleanGear(f.Player);var item=f.Equip(f.Player,"LeatherBoots");f.Refresh();
                var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedEquipmentStyle(f.Player,item,out var proof),proof.Failure);
                Assert.AreSame(Expected("spread-worn-leatherboots"),proof.ExpectedMesh);Assert.AreSame(proof.ExpectedMesh,proof.SubmittedMesh);
                Assert.AreSame(SpreadPortable3DLibrary.Load().Material,proof.ExpectedMaterial);Assert.AreNotSame(proof.ExpectedMaterial,proof.SubmittedMaterial);
                var view=Exact(f,f.Player,item,"spread-worn-leatherboots",2);var piece=view.GetComponentsInChildren<SkinnedMeshRenderer>(true)[1];Mesh copy=null;
                try
                {
                    if(mode=="palette-override"){var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",Color.magenta);piece.SetPropertyBlock(block);}
                    if(mode=="mesh-copy"){copy=UnityEngine.Object.Instantiate(piece.sharedMesh);piece.sharedMesh=copy;}
                    if(mode=="hidden-piece")piece.enabled=false;
                    if(mode=="foreign-bone")piece.bones=new[]{f.Root.transform};
                    Assert.False(presenter.TryGetApprovedEquipmentStyle(f.Player,item,out var rejected),mode);Assert.IsNotEmpty(rejected.Failure);
                    Assert.AreSame(f.Player,item.GetPart<PhysicsPart>().Equipped);
                }
                finally{if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);}
            }
        }
        [TestCase("MarlbackBreacher")][TestCase("Farmer")]
        public void ActualAuthoredLoadoutRendersEveryEquippedItemOnItsOriginalRig(string name)
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                var actor=f.Add(name);var items=actor.GetPart<InventoryPart>().GetAllEquipped().ToArray();Assert.IsNotEmpty(items);f.Refresh();
                foreach(var item in items)
                {
                    string slot=item.GetPart<EquippablePart>().Slot;
                    Exact(f,actor,item,(slot=="Hand"?"spread-portable-":"spread-worn-")+item.BlueprintName.ToLowerInvariant(),slot=="Feet"||slot=="Handwear"?2:1);
                    Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedEquipmentStyle(actor,item,out var evidence),evidence.Failure);
                }
            }
        }
        [Test] public void RealArmLossKeepsNativeGlovesButOnlySurvivingSideIsDrawn()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                f.CleanGear(f.Player);var gloves=f.Equip(f.Player,"LeatherGloves");f.Refresh();Exact(f,f.Player,gloves,"spread-worn-leathergloves",2);
                var body=f.Player.GetPart<Body>();var arm=body.GetParts().Single(x=>x.Type=="Arm"&&(x.GetLaterality()&Laterality.RIGHT)!=0);
                Assert.True(body.Dismember(arm));f.Refresh();var view=Exact(f,f.Player,gloves,"spread-worn-leathergloves",1);
                Assert.True(view.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single().bones.Single().name.EndsWith(".L",StringComparison.Ordinal));
                Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedEquipmentStyle(f.Player,gloves,out var evidence),evidence.Failure);
                Assert.AreSame(f.Player,gloves.GetPart<PhysicsPart>().Equipped);
            }
        }
        [Test] public void PairedBootGeometryActuallyMovesWithImportedWalkClip()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                f.CleanGear(f.Player);var boots=f.Equip(f.Player,"LeatherBoots");f.Refresh();var view=Exact(f,f.Player,boots,"spread-worn-leatherboots",2);
                var actorRoot=f.View(f.Player);var animator=actorRoot.GetComponentInChildren<Animator>();
                var clip=animator.runtimeAnimatorController.animationClips.Single(x=>x.name.EndsWith("Walk",StringComparison.Ordinal)||x.name=="Walk");
                var skins=view.GetComponentsInChildren<SkinnedMeshRenderer>(true);Assert.AreEqual(2,skins.Length);
                var targets=skins.Select(x=>x.bones.Single()).ToArray();var legs=targets.Select(x=>x.parent).ToArray();
                Assert.True(legs.Any(x=>x.name=="Leg.L"));Assert.True(legs.Any(x=>x.name=="Leg.R"));
                var baked=new Mesh();
                try
                {
                    // Imported FBX curves are relative to its nested Animator,
                    // not the outer prefab wrapper returned by the presenter.
                    clip.SampleAnimation(animator.gameObject,0);
                    var before=skins.Select(x=>WorldVertices(x,baked)).ToArray();
                    var legBefore=legs.Select(x=>x.localRotation).ToArray();
                    clip.SampleAnimation(animator.gameObject,0);
                    for(int i=0;i<skins.Length;i++)CollectionAssert.AreEqual(before[i],WorldVertices(skins[i],baked),"Same-pose control must not manufacture motion.");
                    clip.SampleAnimation(animator.gameObject,clip.length*.25f);
                    for(int i=0;i<skins.Length;i++)
                    {
                        Assert.Greater(Quaternion.Angle(legBefore[i],legs[i].localRotation),.01f,"Actual imported leg must animate: "+legs[i].name);
                        var after=WorldVertices(skins[i],baked);Assert.AreEqual(before[i].Length,after.Length);
                        Assert.True(before[i].Where((v,j)=>(v-after[j]).sqrMagnitude>.000001f).Any(),"Both real bound boot vertex sets must move: "+targets[i].name);
                        Assert.AreSame(Expected("spread-worn-leatherboots"),skins[i].sharedMesh);
                        Assert.AreSame(targets[i],skins[i].bones.Single());
                    }
                    Assert.AreSame(skins[0].sharedMesh,skins[1].sharedMesh);
                }
                finally{UnityEngine.Object.DestroyImmediate(baked);}
            }
        }
        private static Vector3[] WorldVertices(SkinnedMeshRenderer skin,Mesh baked)
        {skin.BakeMesh(baked);return baked.vertices.Select(skin.transform.TransformPoint).ToArray();}
        [Test] public void ReceivingBiomeReversalNeverRetainsApprovedEquippedStyle()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                f.CleanGear(f.Player);var item=f.Equip(f.Player,"Hatchet");f.Refresh();var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedEquipmentStyle(f.Player,item,out _));
                f.Manager.WorldMap.Tiles[12,10]=BiomeType.Sodden;
                Assert.False(presenter.TryGetApprovedEquipmentStyle(f.Player,item,out _));f.Refresh();Assert.False(presenter.TryGetApprovedEquipmentStyle(f.Player,item,out _));
                f.Manager.WorldMap.Tiles[12,10]=BiomeType.Spread;f.Refresh();Assert.True(presenter.TryGetApprovedEquipmentStyle(f.Player,item,out var evidence),evidence.Failure);
            }
        }
        [Test] public void SameItemNativeHandRelocationRefusesStaleEvidenceUntilRefresh()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Spread))
            {
                f.CleanGear(f.Player);var item=f.Equip(f.Player,"Dagger");f.Refresh();var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedEquipmentStyle(f.Player,item,out _));var first=f.Player.GetPart<InventoryPart>().FindEquippedBodyPart(item);
                var other=f.Player.GetPart<Body>().GetParts().Single(x=>x.Type=="Hand"&&!ReferenceEquals(first,x));
                Assert.True(InventorySystem.UnequipItem(f.Player,item));Assert.True(InventorySystem.Equip(f.Player,item,other));
                Assert.False(presenter.TryGetApprovedEquipmentStyle(f.Player,item,out var stale));Assert.AreEqual("stale-equipped-form",stale.Failure);
                f.Refresh();Assert.True(presenter.TryGetApprovedEquipmentStyle(f.Player,item,out var repaired),repaired.Failure);
            }
        }
    }
}
