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
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelNorthWorksTests
    {
        const string Resource = "Sprites/Live16x24/north-works";
        static readonly string[] Names = { "AlchemyStill", "AlchemyShelf", "StoneburrPatch", "FrostLichenPatch", "MendleafPlant", "Magpie", "Viper", "MarlbackPatchbearer" };
        EntityFactory factory;
        Texture2D works, vegetation, baseArt;
        FlatPixelOpeningArtCatalog art;
        FlatPixelGreyboxCatalog world;
        [OneTimeSetUp] public void Content() => factory = DensityReferenceGladeTests.Factory();
        [SetUp] public void Setup()
        { works=new Texture2D(144,24); vegetation=new Texture2D(64,24); baseArt=new Texture2D(160,24); }
        [TearDown] public void Cleanup()
        {
            art?.Dispose(); world?.Dispose(); art=null; world=null;
            foreach(var texture in new[]{works,vegetation,baseArt}) if(texture!=null)Object.DestroyImmediate(texture);
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)]
        public void ExactOwnersUseCachedNativeFramesWithoutChangingGameplay(int index)
        {
            Create();var owner=Owner(index);int frame=index==7?8:index;var physics=owner.GetPart<PhysicsPart>();
            bool solid=physics.Solid;var tile=Frame(owner,frame);Assert.AreSame(tile,Frame(owner,frame));
            Assert.AreEqual(new Vector2(8,12),tile.sprite.pivot);Assert.AreEqual(16,tile.sprite.pixelsPerUnit);
            Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);Assert.AreEqual(solid,physics.Solid);
            if(index==0)Assert.False(solid);if(index==1){Assert.True(solid);Assert.False(owner.HasTag("Furniture"));}
            owner.GetPart<RenderPart>().Visible=false;Assert.False(art.TryResolve(owner,0,0,out _));
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)]
        public void ForeignVisualRenderPhysicsAndIdentityCannotBorrowTheseFrames(int index)
        {
            Create();var owner=Owner(index);Assert.True(art.TryResolve(owner,0,0,out _));
            var render=owner.GetPart<RenderPart>();render.VisualID="foreign-reskin";Assert.False(art.TryResolve(owner,0,0,out _));render.VisualID="";
            render.VisualVariant="foreign-costume";Assert.False(art.TryResolve(owner,0,0,out _));render.VisualVariant="";
            render.GlyphVariants="other-glyphs";Assert.False(art.TryResolve(owner,0,0,out _));render.GlyphVariants="";
            render.ParentEntity=new Entity();Assert.False(art.TryResolve(owner,0,0,out _));render.ParentEntity=owner;
            var physics=owner.GetPart<PhysicsPart>();physics.ParentEntity=new Entity();Assert.False(art.TryResolve(owner,0,0,out _));physics.ParentEntity=owner;
            physics.Takeable=true;Assert.False(art.TryResolve(owner,0,0,out _));physics.Takeable=false;
            Assert.True(art.TryResolve(owner,0,0,out _));owner.BlueprintName="ForeignOwner";Assert.False(art.TryResolve(owner,0,0,out _));
        }

        [TestCase(0,"AlchemyStill")][TestCase(1,"Container")][TestCase(7,"FieldMedicine")]
        [TestCase(7,"Inventory")][TestCase(7,"Body")][TestCase(7,"Brain")]
        public void FunctionalPartsMustBelongToTheActualOwner(int index,string partName)
        {
            Create();var owner=Owner(index);Assert.True(art.TryResolve(owner,0,0,out _));var part=owner.GetPart(partName);Assert.NotNull(part);
            part.ParentEntity=new Entity();Assert.False(art.TryResolve(owner,0,0,out _));part.ParentEntity=owner;
            Assert.True(art.TryResolve(owner,0,0,out _));Assert.True(owner.RemovePart(part));Assert.False(art.TryResolve(owner,0,0,out _));
        }

        [TestCase(2)][TestCase(3)][TestCase(4)]
        public void HarvestArtworkRequiresUnspentExactFiniteYieldAndVegetation(int index)
        {
            Create();var owner=Owner(index);Frame(owner,index);var part=owner.GetPart<HarvestablePart>();
            string yield=part.YieldBlueprint;part.YieldBlueprint="WildBerries";Assert.False(art.TryResolve(owner,0,0,out _));part.YieldBlueprint=yield;
            part.Harvested=true;Assert.False(art.TryResolve(owner,0,0,out _));part.Harvested=false;
            part.YieldMin=0;Assert.False(art.TryResolve(owner,0,0,out _));part.YieldMin=3;part.YieldMax=2;Assert.False(art.TryResolve(owner,0,0,out _));
            part.YieldMin=1;part.YieldMax=HarvestablePart.MaximumYieldCount+1;Assert.False(art.TryResolve(owner,0,0,out _));part.YieldMax=2;
            part.YieldChance=0;Assert.False(art.TryResolve(owner,0,0,out _));part.YieldChance=101;Assert.False(art.TryResolve(owner,0,0,out _));part.YieldChance=50;
            Frame(owner,index);part.ParentEntity=new Entity();Assert.False(art.TryResolve(owner,0,0,out _));part.ParentEntity=owner;
            owner.Tags.Remove("Vegetation");Assert.False(art.TryResolve(owner,0,0,out _));
        }

        [TestCase(5)][TestCase(6)][TestCase(7)]
        public void StaticSpeciesRequireCreatureIdentityAndCannotBorrowCompactMotion(int index)
        {
            Create();var owner=Owner(index);Assert.True(art.TryResolve(owner,0,0,out _));
            var cast=new Texture2D(64,24);var poses=new Texture2D(256,24);
            try { using(var motion=new FlatPixelActorMotionCatalog(baseArt,cast,poses))
                { Assert.True(motion.TryGetAsset(factory.CreateEntity("MarlbackGleaner"),out _));Assert.False(motion.TryGetAsset(owner,out _)); } }
            finally { Object.DestroyImmediate(cast);Object.DestroyImmediate(poses); }
            owner.Tags.Remove("Creature");Assert.False(art.TryResolve(owner,0,0,out _));owner.SetTag("Creature");
            Assert.True(art.TryResolve(owner,0,0,out _));owner.SetTag("Item");Assert.False(art.TryResolve(owner,0,0,out _));
        }

        [Test] public void HealingSlotUsesActualSupplyNotHealthThresholdOrSeparateAntidote()
        {
            Create();var owner=Owner(7);Frame(owner,8);var inventory=owner.GetPart<InventoryPart>();
            Assert.True(inventory.AddObject(factory.CreateEntity("Antidote")));Frame(owner,8);
            var tonic=Supply(owner);var tile=Frame(owner,7);owner.GetStat("Hitpoints").BaseValue=1;
            owner.GetPart<FieldMedicinePart>().UseAtOrBelowPercent=0;owner.GetPart<BrainPart>().PushGoal(new NoFightGoal(50,false));
            Assert.AreSame(tile,Frame(owner,7));Assert.True(inventory.RemoveObject(tonic));Frame(owner,8);
            Assert.True(inventory.Objects.Any(e=>e.BlueprintName=="Antidote"));
            owner.GetPart<FieldMedicinePart>().TonicBlueprint="OtherTonic";Assert.False(art.TryResolve(owner,0,0,out _));
        }

        [TestCase("zero-stack")][TestCase("foreign-inventory")][TestCase("equipped")]
        [TestCase("foreign-physics")][TestCase("foreign-tonic")][TestCase("no-healing")][TestCase("on-ground")]
        public void InvalidMedicineReferencesNeverPaintAFullHealingSlot(string fault)
        {
            Create();var owner=Owner(7);var tonic=Supply(owner);Frame(owner,7);var physics=tonic.GetPart<PhysicsPart>();
            if(fault=="zero-stack")tonic.GetPart<StackerPart>().StackCount=0;
            if(fault=="foreign-inventory")physics.InInventory=new Entity();
            if(fault=="equipped")physics.Equipped=owner;
            if(fault=="foreign-physics")physics.ParentEntity=new Entity();
            if(fault=="foreign-tonic")tonic.GetPart<TonicPart>().ParentEntity=new Entity();
            if(fault=="no-healing")tonic.GetPart<TonicPart>().Healing="";
            if(fault=="on-ground"){var zone=new Zone("stock-counter");Assert.True(zone.AddEntity(tonic,1,1));}
            Assert.IsNull(owner.GetPart<FieldMedicinePart>().FindCarriedMedicine());Frame(owner,8);
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)]
        public void WorldAndExactSubjectAgreeWhileFogDoesNotRevealLiveOwners(int index)
        {
            world=NewWorld(baseArt,vegetation,works);var zone=new Zone(ReferenceGladePlan.ZoneID);var cell=zone.GetCell(10,10);
            cell.Explored=cell.IsVisible=true;var owner=Owner(index);Assert.True(zone.AddEntity(owner,10,10));
            Assert.True(world.TryResolve(zone,10,10,out var visible));Assert.AreSame(owner,visible.Owner);Assert.AreSame(works,visible.ForegroundTile.sprite.texture);
            Assert.True(world.TryGetSubjectSprite(owner,10,10,out var portrait,out var tint));Assert.AreSame(visible.ForegroundTile.sprite,portrait);Assert.AreEqual(Color.white,tint);
            cell.IsVisible=false;Assert.True(world.TryResolve(zone,10,10,out var hidden));Assert.IsNull(hidden.ForegroundTile);
            cell.IsVisible=true;Assert.True(zone.RemoveEntity(owner));Assert.True(world.TryResolve(zone,10,10,out var gone));Assert.IsNull(gone.ForegroundTile);
        }

        [TestCase(2,"StoneburrSeed")][TestCase(3,"FrostLichen")][TestCase(4,"MendleafSprig")]
        public void RealHarvestRemovesTheSourceAndProducesOnlyItsAuthoredYield(int index,string yield)
        {
            using(var globals=new DensityLootTestScope())
            {
                var old=HarvestablePart.Factory;HarvestablePart.Factory=factory;
                try
                {
                    world=NewWorld(baseArt,vegetation,works);var zone=new Zone(ReferenceGladePlan.ZoneID);var owner=Owner(index);var actor=factory.CreateEntity("Player");
                    Assert.True(zone.AddEntity(owner,10,10));Assert.True(zone.AddEntity(actor,9,10));zone.GetCell(10,10).Explored=zone.GetCell(10,10).IsVisible=true;
                    Assert.True(world.TryResolve(zone,10,10,out var before));Assert.AreSame(works,before.ForegroundTile.sprite.texture);
                    Assert.True(DensityHarvestSecurityTests.Act(owner,actor,zone));Assert.IsNull(zone.GetEntityCell(owner));
                    Assert.True(actor.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName==yield));
                    Assert.True(world.TryResolve(zone,10,10,out var after));Assert.IsNull(after.ForegroundTile);
                    Assert.False(DensityHarvestSecurityTests.Act(owner,actor,zone));
                }
                finally{HarvestablePart.Factory=old;}
            }
        }

        [Test] public void EighthAtlasIsIndependentAndEarlierInjectorsNeverLoadIt()
        {
            art=new FlatPixelOpeningArtCatalog(baseArt,null,null,null,null,null,vegetation);
            Assert.False(art.TryResolve(Owner(0),0,0,out _));Assert.True(art.TryResolve(factory.CreateEntity("Tree"),0,0,out var tree));Assert.AreSame(vegetation,tree.sprite.texture);art.Dispose();
            art=NewArt(null,null,works);Frame(Owner(0),0);Assert.False(art.TryResolve(factory.CreateEntity("Tree"),0,0,out _));
            Assert.False(art.TryResolve(factory.CreateEntity("Player"),0,0,out _));
        }
        [TestCase(143,24)][TestCase(144,23)] public void MalformedWorksKeepsPriorGroupsAndBorrowedTextureSettings(int width,int height)
        {
            Object.DestroyImmediate(works);works=new Texture2D(width,height);works.filterMode=FilterMode.Bilinear;works.wrapMode=TextureWrapMode.Repeat;Create();
            Assert.False(art.TryResolve(Owner(0),0,0,out _));Assert.True(art.TryResolve(factory.CreateEntity("Tree"),0,0,out var tree));Assert.AreSame(vegetation,tree.sprite.texture);
            Assert.True(art.TryResolve(factory.CreateEntity("Player"),0,0,out var player));Assert.AreSame(baseArt,player.sprite.texture);
            Assert.AreEqual(FilterMode.Bilinear,works.filterMode);Assert.AreEqual(TextureWrapMode.Repeat,works.wrapMode);
        }
        [Test] public void DisposalReleasesOnlyOwnGeneratedFrames()
        {
            Create();var tile=Frame(Owner(0),0);var sprite=tile.sprite;
            using(var other=NewArt(null,null,works))
            {
                Assert.True(other.TryResolve(Owner(0),0,0,out var separate));Assert.AreNotSame(tile,separate);
                art.Dispose();art.Dispose();Assert.True(tile==null);Assert.True(sprite==null);Assert.True(separate!=null);Assert.True(works!=null);
                Assert.False(art.TryResolve(Owner(0),0,0,out _));
            }
        }
        [Test] public void DefaultWorldAndCatalogBindAllNineActualImportedFrames()
        {
            var resource=Resources.Load<Texture2D>(Resource);Assert.NotNull(resource,"Synthetic routing does not prove native art is imported.");
            Assert.AreEqual(144,resource.width);Assert.AreEqual(24,resource.height);Assert.AreEqual(FilterMode.Point,resource.filterMode);
            Assert.AreEqual(TextureWrapMode.Clamp,resource.wrapMode);Assert.AreEqual(1,resource.mipmapCount);
            art=new FlatPixelOpeningArtCatalog();world=new FlatPixelGreyboxCatalog();
            for(int i=0;i<9;i++)
            {
                var owner=Owner(Math.Min(i,7));if(i==7)Supply(owner);
                Assert.True(art.TryResolve(owner,0,0,out var tile));Assert.AreSame(resource,tile.sprite.texture);Assert.AreEqual(new Rect(i*16,0,16,24),tile.sprite.rect);
                Assert.True(world.TryGetSubjectSprite(owner,0,0,out var portrait,out var tint));Assert.AreSame(resource,portrait.texture);Assert.AreEqual(tile.sprite.rect,portrait.rect);Assert.AreEqual(Color.white,tint);
            }
        }

        [Test] public void StationaryNativeSelfTreatmentConsumesTheVisibleBottle()
        {
            using(var f=new CadenceFixture())
            {
                f.Expect(7);var cell=f.Zone.GetEntityCell(f.Owner);f.Owner.GetStat("Hitpoints").BaseValue=1;
                f.Owner.GetPart<BrainPart>().CurrentZone=f.Zone;f.Owner.GetPart<BrainPart>().SetPersonallyHostile(f.Actor);
                Assert.True(f.Owner.GetPart<FieldMedicinePart>().TryUseMedicine(f.Actor,f.Zone,new FieldMedicineFixture.MedicineRandom()));
                Assert.False(f.FullDirty);f.Frame();f.Expect(8);Assert.AreSame(cell,f.Zone.GetEntityCell(f.Owner));f.Quiet();
            }
        }
        [Test] public void StationaryTransferRefillAndLastUnitConsumptionRepaintOnlyOnStateChange()
        {
            using(var f=new CadenceFixture())
            {
                f.Expect(7);var cell=f.Zone.GetEntityCell(f.Owner);var bag=f.Owner.GetPart<InventoryPart>();var other=f.Actor.GetPart<InventoryPart>();
                Assert.True(bag.RemoveObject(f.Tonic));Assert.True(other.AddObject(f.Tonic));f.Quiet();f.Frame();f.Expect(8);f.Quiet();
                Assert.True(other.RemoveObject(f.Tonic));Assert.True(bag.AddObject(f.Tonic));f.Quiet();f.Frame();f.Expect(7);f.Quiet();
                f.Tonic.GetPart<StackerPart>().StackCount=2;f.Frame();f.Expect(7);f.Quiet();
                Assert.True(bag.TryConsumeOne(f.Tonic));f.Frame();f.Expect(7);f.Quiet();
                Assert.True(bag.TryConsumeOne(f.Tonic));f.Frame();f.Expect(8);Assert.AreSame(cell,f.Zone.GetEntityCell(f.Owner));f.Quiet();
                long passes=EnvironmentSpriteRenderer.Perf.IncrementalPasses;f.Frame();Assert.AreEqual(passes,EnvironmentSpriteRenderer.Perf.IncrementalPasses,"No perpetual dirty work for unchanged supply.");
            }
        }
        [TestCase("medicine")][TestCase("inventory")][TestCase("render")][TestCase("tonic")]
        public void QuietInvalidOwnershipAndRecoveryRepaintSubmittedState(string fault)
        {
            using(var f=new CadenceFixture())
            {
                f.Expect(7);Part part=fault=="medicine"?f.Owner.GetPart<FieldMedicinePart>():fault=="inventory"?f.Owner.GetPart<InventoryPart>():fault=="render"?f.Owner.GetPart<RenderPart>():f.Tonic.GetPart<TonicPart>();
                var previous=part.ParentEntity;part.ParentEntity=new Entity();f.Quiet();f.Frame();
                if(fault=="tonic")f.Expect(8);else Assert.AreNotSame(f.Atlas,f.Submitted?.sprite.texture);
                f.Quiet();part.ParentEntity=previous;f.Frame();f.Expect(7);f.Quiet();
            }
        }
        [Test] public void CoveredOwnerIsNotRepaintedOrQueriedThroughAnotherTopOwner()
        {
            using(var f=new CadenceFixture())
            {
                var cover=f.Factory.CreateEntity("Chest");cover.GetPart<RenderPart>().RenderLayer=100;Assert.True(f.Zone.AddEntity(cover,10,10));f.Frame();f.Quiet();
                var drawn=f.Submitted;long passes=EnvironmentSpriteRenderer.Perf.IncrementalPasses;
                Assert.True(f.Owner.GetPart<InventoryPart>().RemoveObject(f.Tonic));f.Frame();Assert.AreSame(drawn,f.Submitted);Assert.AreEqual(passes,EnvironmentSpriteRenderer.Perf.IncrementalPasses);
                Assert.True(f.Zone.RemoveEntity(cover));f.Frame();f.Expect(8);
            }
        }
        [Test] public void HiddenChangesStayHiddenAndRevealCurrentSupply()
        {
            using(var f=new CadenceFixture())
            {
                f.Zone.GetCell(10,10).IsVisible=false;f.Frame();Assert.IsNull(f.Submitted);f.Quiet();
                Assert.True(f.Owner.GetPart<InventoryPart>().RemoveObject(f.Tonic));long passes=EnvironmentSpriteRenderer.Perf.IncrementalPasses;f.Frame();Assert.IsNull(f.Submitted);Assert.AreEqual(passes,EnvironmentSpriteRenderer.Perf.IncrementalPasses);
                f.Zone.GetCell(10,10).IsVisible=true;f.Renderer.MarkCellDirty(10,10,"normal FOV reveal");f.Frame();f.Expect(8);
            }
        }
        [Test] public void RemovedOwnerDoesNotRetainAWatcherOrPaintOverSameIdReplacement()
        {
            using(var f=new CadenceFixture())
            {
                Assert.True(f.Zone.RemoveEntity(f.Owner));var replacement=f.Factory.CreateEntity("Chest");replacement.ID=f.Owner.ID;Assert.True(f.Zone.AddEntity(replacement,10,10));f.Frame();f.Quiet();var drawn=f.Submitted;
                Assert.True(f.Owner.GetPart<InventoryPart>().RemoveObject(f.Tonic));long passes=EnvironmentSpriteRenderer.Perf.IncrementalPasses;f.Frame();Assert.AreSame(drawn,f.Submitted);Assert.AreEqual(passes,EnvironmentSpriteRenderer.Perf.IncrementalPasses);
            }
        }
        [TestCase("paused")][TestCase("mode")][TestCase("disabled")]
        public void InvisibleWorldDoesNotPollSupplyAndResumesFromActualInventory(string reason)
        {
            using(var f=new CadenceFixture())
            {
                if(reason=="paused")f.Renderer.Paused=true;if(reason=="mode")f.Renderer.FlatPixelModeEnabled=false;if(reason=="disabled")f.Env.RenderingEnabled=false;
                f.Frame();Assert.True(f.Owner.GetPart<InventoryPart>().RemoveObject(f.Tonic));long passes=EnvironmentSpriteRenderer.Perf.IncrementalPasses;f.Frame();Assert.AreEqual(passes,EnvironmentSpriteRenderer.Perf.IncrementalPasses);
                if(reason=="paused")f.Renderer.Paused=false;if(reason=="mode")f.Renderer.FlatPixelModeEnabled=true;if(reason=="disabled")f.Env.RenderingEnabled=true;
                f.Frame();f.Expect(8);f.Quiet();
            }
        }

        [Test] public void ReplacedCellCannotKeepTheOldIndexedActorsArtwork()
        {
            using(var f=new CadenceFixture())
            {
                f.Zone.Cells[10,10]=new Cell(10,10,f.Zone){Explored=true,IsVisible=true};f.Quiet();
                f.Frame();Assert.IsNull(f.Submitted);f.Quiet();
            }
        }
        [Test] public void RelocationWithRetainedOldCellReferencePaintsOnlyTheActualCurrentCell()
        {
            using(var f=new CadenceFixture())
            {
                var old=f.Zone.GetEntityCell(f.Owner);Assert.True(f.Zone.AddEntity(f.Owner,11,10));old.Objects.Add(f.Owner);f.Quiet();
                f.Frame();Assert.IsNull(f.Submitted);Assert.AreSame(f.Atlas,f.TileAt(11,10)?.sprite.texture);f.Quiet();
            }
        }
        [Test] public void ReplacedManagedGraphStopsPollingAndRestoredGraphReobservesCurrentSupply()
        {
            using(var f=new CadenceFixture(true))
            {
                var replacement=new Zone(f.Zone.ZoneID);f.Manager.CachedZones[f.Zone.ZoneID]=replacement;f.Frame();Assert.False(f.Renderer.FlatPixelModeActive);
                Assert.True(f.Owner.GetPart<InventoryPart>().RemoveObject(f.Tonic));long passes=EnvironmentSpriteRenderer.Perf.IncrementalPasses;f.Frame();Assert.AreEqual(passes,EnvironmentSpriteRenderer.Perf.IncrementalPasses);
                f.Manager.CachedZones[f.Zone.ZoneID]=f.Zone;f.Frame();Assert.True(f.Renderer.FlatPixelModeActive);f.Expect(8);f.Quiet();
            }
        }
        [Test] public void WarmQuietWatcherAllocatesNothingAndDoesNotDiscoverUnpaintedActors()
        {
            using(var f=new CadenceFixture())
            {
                var method=typeof(EnvironmentSpriteRenderer).GetMethod("ReconcileFlatOwnerStates");Assert.NotNull(method);
                var tick=(Action<Zone,ZoneRenderer>)Delegate.CreateDelegate(typeof(Action<Zone,ZoneRenderer>),f.Env,method);
                var other=f.Factory.CreateEntity("MarlbackPatchbearer");Assert.True(f.Zone.AddEntity(other,12,10));f.Zone.GetCell(12,10).Explored=f.Zone.GetCell(12,10).IsVisible=true;
                for(int i=0;i<10;i++)tick(f.Zone,f.Renderer);
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)tick(f.Zone,f.Renderer);long after=GC.GetAllocatedBytesForCurrentThread();
                Assert.AreEqual(before,after,"Warmed bounded revalidation must not allocate every frame.");f.Quiet();Assert.IsNull(f.TileAt(12,10));
            }
        }
        [Test] public void ForeignRendererCannotRedirectOrClearTheActualOwnersTracker()
        {
            using(var f=new CadenceFixture())
            {
                var method=typeof(EnvironmentSpriteRenderer).GetMethod("ReconcileFlatOwnerStates");Assert.NotNull(method);
                var foreignRoot=new GameObject("Foreign renderer");foreignRoot.SetActive(false);
                try
                {
                    var foreign=foreignRoot.AddComponent<ZoneRenderer>();foreign.SetZone(f.Zone);
                    method.Invoke(f.Env,new object[]{f.Zone,foreign});f.Quiet();
                    Assert.True(f.Owner.GetPart<InventoryPart>().RemoveObject(f.Tonic));f.Frame();f.Expect(8);
                }
                finally{Object.DestroyImmediate(foreignRoot);}
            }
        }

        Entity Owner(int index)
        {
            var owner=factory.CreateEntity(Names[index]);Assert.NotNull(owner);
            if(index==7)foreach(var item in owner.GetPart<InventoryPart>().Objects.ToArray())owner.GetPart<InventoryPart>().RemoveObject(item);
            return owner;
        }
        Entity Supply(Entity owner)
        {
            var item=factory.CreateEntity("HealingTonic");if(item.GetPart<StackerPart>()==null)item.AddPart(new StackerPart());
            item.GetPart<StackerPart>().StackCount=1;Assert.True(owner.GetPart<InventoryPart>().AddObject(item));return item;
        }
        void Create()=>art=NewArt(baseArt,vegetation,works);
        Tile Frame(Entity owner,int frame)
        {Assert.True(art.TryResolve(owner,0,0,out var tile),owner.BlueprintName);Assert.AreSame(works,tile.sprite.texture);Assert.AreEqual(new Rect(frame*16,0,16,24),tile.sprite.rect);return tile;}
        static Type[] Signature()=>Enumerable.Repeat(typeof(Texture2D),8).ToArray();
        static FlatPixelOpeningArtCatalog NewArt(Texture2D first,Texture2D seventh,Texture2D eighth)
        {
            var ctor=typeof(FlatPixelOpeningArtCatalog).GetConstructor(Signature());Assert.NotNull(ctor,"Eighth independent works atlas API is required.");
            return (FlatPixelOpeningArtCatalog)ctor.Invoke(new object[]{first,null,null,null,null,null,seventh,eighth});
        }
        static FlatPixelGreyboxCatalog NewWorld(Texture2D first,Texture2D seventh,Texture2D eighth)
        {
            var ctor=typeof(FlatPixelGreyboxCatalog).GetConstructor(Signature());Assert.NotNull(ctor,"World selector must forward eighth works atlas.");
            return (FlatPixelGreyboxCatalog)ctor.Invoke(new object[]{first,null,null,null,null,null,seventh,eighth});
        }

        sealed class CadenceFixture:IDisposable
        {
            const BindingFlags I=BindingFlags.Instance|BindingFlags.NonPublic,S=BindingFlags.Static|BindingFlags.NonPublic;
            readonly GameObject root;readonly HotbarSaveFixture globals;
            readonly Action<int,int,string> oldCell=ZoneRenderHooks.CellDirtyCallback;readonly Action<string> oldFull=ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound=typeof(ZoneTileStateSystem).GetField("_boundRenderZone",S);readonly object oldBound;
            readonly Zone previous;readonly Action<int,int> oldChanged;readonly Action oldSight;readonly Zone oldActive=SettlementRuntime.ActiveZone;
            readonly Dictionary<PropertyInfo,object> hooks=typeof(EntityVisualHooks).GetProperties(BindingFlags.Public|BindingFlags.Static).Where(p=>p.CanRead&&p.CanWrite).ToDictionary(p=>p,p=>p.GetValue(null));
            readonly Tilemap overlay;
            public readonly Texture2D Atlas;public readonly EntityFactory Factory;public readonly Zone Zone;public readonly Entity Owner,Actor,Tonic;
            public readonly OverworldZoneManager Manager;
            public readonly ZoneRenderer Renderer;public readonly EnvironmentSpriteRenderer Env;
            public bool FullDirty=>(bool)Field("_fullDirty").GetValue(Renderer);
            public Tile Submitted=>overlay.GetTile<Tile>(new Vector3Int(10,CavesOfOoo.Core.Zone.Height-1-10,0));
            public Tile TileAt(int x,int y)=>overlay.GetTile<Tile>(new Vector3Int(x,CavesOfOoo.Core.Zone.Height-1-y,0));
            public CadenceFixture(bool managed=false)
            {
                oldBound=bound.GetValue(null);if(oldBound is WeakReference<Zone> weak&&weak.TryGetTarget(out var old)){previous=old;oldChanged=old.TileState.OnCellChanged;oldSight=old.TileState.OnSightChanged;}
                try
                {
                    globals=new HotbarSaveFixture(true,false);EntityVisualHooks.Reset();Factory=DensityReferenceGladeTests.Factory();Zone=new Zone(managed?"Overworld.11.9.0":ReferenceGladePlan.ZoneID);
                    if(managed){Manager=OverworldZoneManager.CreateDetached(Factory,64,true);Manager.SetActiveZone(Zone);}
                    Actor=Factory.CreateEntity("Player");Owner=Factory.CreateEntity("MarlbackPatchbearer");var inventory=Owner.GetPart<InventoryPart>();
                    foreach(var item in inventory.Objects.ToArray())inventory.RemoveObject(item);
                    Tonic=Factory.CreateEntity("HealingTonic");if(Tonic.GetPart<StackerPart>()==null)Tonic.AddPart(new StackerPart());Tonic.GetPart<StackerPart>().StackCount=1;Assert.True(inventory.AddObject(Tonic));
                    Assert.True(Zone.AddEntity(Actor,9,10));Assert.True(Zone.AddEntity(Owner,10,10));
                    for(int y=9;y<=11;y++)for(int x=9;x<=11;x++)Zone.GetCell(x,y).Explored=Zone.GetCell(x,y).IsVisible=true;
                    SettlementRuntime.ActiveZone=Zone;root=new GameObject("Works state cadence",typeof(Grid));root.SetActive(false);
                    var main=new GameObject("Main",typeof(Tilemap),typeof(TilemapRenderer));main.transform.SetParent(root.transform,false);
                    var bg=new GameObject("Background",typeof(Tilemap));bg.transform.SetParent(root.transform,false);
                    var camera=new GameObject("Camera",typeof(Camera));camera.transform.SetParent(root.transform,false);camera.GetComponent<Camera>().orthographic=true;
                    Renderer=main.AddComponent<ZoneRenderer>();Field("_tilemap").SetValue(Renderer,main.GetComponent<Tilemap>());Field("_bgTilemap").SetValue(Renderer,bg.GetComponent<Tilemap>());Field("_mainCamera").SetValue(Renderer,camera.GetComponent<Camera>());
                    Env=root.AddComponent<EnvironmentSpriteRenderer>();Env.Init(root.transform,main.GetComponent<Tilemap>(),bg.GetComponent<Tilemap>());
                    Atlas=new Texture2D(144,24);typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox",I).SetValue(Env,NewWorld(null,null,Atlas));overlay=(Tilemap)typeof(EnvironmentSpriteRenderer).GetField("_overlayTilemap",I).GetValue(Env);
                    Field("_envSpriteRenderer").SetValue(Renderer,Env);Field("_flatPixelModeEnabled").SetValue(Renderer,true);
                    Renderer.SetZone(Zone);ZoneRenderHooks.CellDirtyCallback=Renderer.MarkCellDirty;ZoneRenderHooks.FullDirtyCallback=Renderer.MarkDirty;
                    Frame();Assert.True(Renderer.FlatPixelModeActive);Quiet();
                }
                catch{Dispose();throw;}
            }
            public void Frame(){try{typeof(ZoneRenderer).GetMethod("LateUpdate",I).Invoke(Renderer,null);}catch(TargetInvocationException e){throw e.InnerException??e;}}
            public void Quiet(){Assert.False(FullDirty);Assert.Zero(((HashSet<int>)Field("_dirtyCells").GetValue(Renderer)).Count);}
            public void Expect(int frame){Assert.NotNull(Submitted);Assert.AreSame(Atlas,Submitted.sprite.texture);Assert.AreEqual(new Rect(frame*16,0,16,24),Submitted.sprite.rect,"Already submitted healing-slot art is stale.");}
            static FieldInfo Field(string name)=>typeof(ZoneRenderer).GetField(name,I);
            public void Dispose()
            {
                try{if(root!=null)Object.DestroyImmediate(root);if(Atlas!=null)Object.DestroyImmediate(Atlas);}
                finally
                {
                    globals?.Dispose();ZoneRenderHooks.CellDirtyCallback=oldCell;ZoneRenderHooks.FullDirtyCallback=oldFull;SettlementRuntime.ActiveZone=oldActive;
                    foreach(var hook in hooks)hook.Key.SetValue(null,hook.Value);bound.SetValue(null,oldBound);if(previous!=null){previous.TileState.OnCellChanged=oldChanged;previous.TileState.OnSightChanged=oldSight;}
                }
            }
        }
    }
}
