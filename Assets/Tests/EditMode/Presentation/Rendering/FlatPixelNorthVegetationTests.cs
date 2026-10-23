using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelNorthVegetationTests
    {
        const string Resource = "Sprites/Live16x24/north-vegetation";
        static readonly string[] Names = { "Tree", "Hedge", "BerryBush", "DryBrush", "Bush", "Reeds" };
        EntityFactory factory;
        Texture2D vegetation, baseArt, cellar;
        FlatPixelOpeningArtCatalog art;
        FlatPixelGreyboxCatalog world;
        [OneTimeSetUp] public void Content() => factory = DensityReferenceGladeTests.Factory();
        [SetUp] public void Setup()
        { vegetation = new Texture2D(64,24); baseArt = new Texture2D(160,24); cellar = new Texture2D(144,24); }
        [TearDown] public void Cleanup()
        {
            art?.Dispose(); world?.Dispose();
            foreach (var texture in new[] { vegetation, baseArt, cellar }) if (texture != null) Object.DestroyImmediate(texture);
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void ExactVegetationUsesItsCachedNativeCellWithoutChangingPhysics(int frame)
        {
            Create(); var owner = Owner(frame); var physics = owner.GetPart<PhysicsPart>();
            bool solid = physics.Solid; var tile = Frame(owner,frame);
            Assert.AreEqual(new Vector2(8,12),tile.sprite.pivot); Assert.AreEqual(16,tile.sprite.pixelsPerUnit);
            Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);
            Assert.AreSame(tile,Frame(owner,frame)); Assert.AreEqual(solid,physics.Solid);
            Assert.False(physics.Takeable); owner.GetPart<RenderPart>().Visible=false;
            Assert.False(art.TryResolve(owner,0,0,out _));
        }

        [TestCase(4,5)][TestCase(5,3)]
        public void OrdinaryBushAndReedsReuseAcceptedBasePixelsWithoutInventingVisualIds(int index,int frame)
        {
            // Independent of the missing seventh constructor: this proves the
            // actual ordinary-owner routing gap in the pre-existing API.
            art = new FlatPixelOpeningArtCatalog(baseArt); var owner = Owner(index);
            Assert.That(owner.GetPart<RenderPart>().VisualID, Is.Null.Or.Empty);
            Assert.True(art.TryResolve(owner,0,0,out var tile)); Assert.AreSame(baseArt,tile.sprite.texture);
            Assert.AreEqual(new Rect(frame*16,0,16,24),tile.sprite.rect);
            Assert.True(art.TryResolve(owner,1,0,out var adjacent));
            Assert.AreEqual(new Rect((index==5?4:5)*16,0,16,24),adjacent.sprite.rect);
            Assert.That(owner.GetPart<RenderPart>().VisualID, Is.Null.Or.Empty);
            owner.GetPart<RenderPart>().VisualID="foreign-vegetation-art";
            Assert.False(art.TryResolve(owner,0,0,out _));
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void ReskinsForeignRendersAndUnknownIdentitiesKeepExplicitFallback(int index)
        {
            Create(); var owner=Owner(index); Assert.True(art.TryResolve(owner,0,0,out _));
            var render=owner.GetPart<RenderPart>(); render.VisualID="different-authored-plant";
            Assert.False(art.TryResolve(owner,0,0,out _)); render.VisualID="";
            Assert.True(art.TryResolve(owner,0,0,out _)); render.ParentEntity=new Entity();
            Assert.False(art.TryResolve(owner,0,0,out _)); render.ParentEntity=owner;
            owner.BlueprintName="ForeignPlant"; Assert.False(art.TryResolve(owner,0,0,out _));
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void OwnedNonportablePhysicsAndActualBlockingContractAreRequired(int index)
        {
            Create(); var owner=Owner(index); Assert.True(art.TryResolve(owner,0,0,out _));
            var physics=owner.GetPart<PhysicsPart>(); bool solid=physics.Solid;
            physics.Solid=!solid; Assert.False(art.TryResolve(owner,0,0,out _)); physics.Solid=solid;
            Assert.True(art.TryResolve(owner,0,0,out _)); physics.Takeable=true;
            Assert.False(art.TryResolve(owner,0,0,out _)); physics.Takeable=false;
            physics.ParentEntity=new Entity(); Assert.False(art.TryResolve(owner,0,0,out _)); physics.ParentEntity=owner;
            Assert.True(owner.RemovePart(physics)); Assert.False(art.TryResolve(owner,0,0,out _));
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void VegetationTagIsRequiredAndCreatureIdentityCannotBorrowPlantArt(int index)
        {
            Create(); var owner=Owner(index); Assert.True(art.TryResolve(owner,0,0,out _));
            owner.Tags.Remove("Vegetation"); Assert.False(art.TryResolve(owner,0,0,out _));
            owner.SetTag("Vegetation"); Assert.True(art.TryResolve(owner,0,0,out _));
            owner.SetTag("Creature"); Assert.False(art.TryResolve(owner,0,0,out _));
        }

        [TestCase("spent")][TestCase("wrong-yield")][TestCase("zero-min")][TestCase("reversed-range")]
        [TestCase("excess-count")][TestCase("zero-chance")][TestCase("excess-chance")][TestCase("foreign")][TestCase("missing")]
        public void BerryArtworkOnlyPromisesItsActualUnspentHarvest(string mutation)
        {
            Create(); var owner=Owner(2); Frame(owner,2); var harvest=owner.GetPart<HarvestablePart>();
            if(mutation=="spent")harvest.Harvested=true;
            if(mutation=="wrong-yield")harvest.YieldBlueprint="Mushroom";
            if(mutation=="zero-min")harvest.YieldMin=0;
            if(mutation=="reversed-range"){harvest.YieldMin=4;harvest.YieldMax=3;}
            if(mutation=="excess-count")harvest.YieldMax=HarvestablePart.MaximumYieldCount+1;
            if(mutation=="zero-chance")harvest.YieldChance=0;
            if(mutation=="excess-chance")harvest.YieldChance=101;
            if(mutation=="foreign")harvest.ParentEntity=new Entity();
            if(mutation=="missing")Assert.True(owner.RemovePart(harvest));
            Assert.False(art.TryResolve(owner,0,0,out _));
        }

        [Test] public void RealisticYieldAndDamageTuningDoNotCreateInventedPlantStates()
        {
            Create(); var berry=Owner(2); var tile=Frame(berry,2); var harvest=berry.GetPart<HarvestablePart>();
            harvest.YieldMin=2; harvest.YieldMax=4; harvest.YieldChance=50;
            Assert.AreSame(tile,Frame(berry,2)); Assert.False(harvest.Harvested);
            foreach(int index in new[]{0,1,3})
            {
                var owner=Owner(index); var same=Frame(owner,index);
                owner.GetPart<DestructiblePart>().HP=1; owner.GetPart<ThermalPart>().Temperature=200;
                Assert.AreSame(same,Frame(owner,index),"Damage/heat overlays retain their own authority.");
            }
        }

        [TestCase(0,false)][TestCase(1,false)][TestCase(2,false)][TestCase(3,true)]
        [TestCase(4,true)][TestCase(5,true)]
        public void WorldFogAndRemovalKeepExistingTerrainMemoryRules(int index,bool remembers)
        {
            world=NewWorld(); var zone=new Zone(ReferenceGladePlan.ZoneID); var cell=zone.GetCell(10,10);
            cell.Explored=cell.IsVisible=true; var owner=Owner(index); Assert.True(zone.AddEntity(owner,10,10));
            Assert.True(world.TryResolve(zone,10,10,out var visible)); Assert.AreSame(owner,visible.Owner);
            Assert.AreSame(index<4?vegetation:baseArt,visible.ForegroundTile.sprite.texture);
            Assert.AreEqual(index<2,visible.BlocksMovement); Assert.AreEqual(Color.white,visible.ForegroundColor);
            Assert.True(world.TryGetSubjectSprite(owner,10,10,out var portrait,out var tint));
            Assert.AreSame(visible.ForegroundTile.sprite,portrait); Assert.AreEqual(Color.white,tint);
            cell.IsVisible=false; Assert.True(world.TryResolve(zone,10,10,out var memory));
            Assert.True(memory.Remembered); Assert.False(memory.BlocksMovement);
            if(remembers){Assert.AreSame(visible.ForegroundTile,memory.ForegroundTile);Assert.AreEqual(new Color(.35f,.35f,.35f,1),memory.ForegroundColor);}
            else Assert.IsNull(memory.ForegroundTile);
            cell.Explored=false; Assert.False(world.TryResolve(zone,10,10,out _));
            cell.Explored=cell.IsVisible=true; Assert.True(zone.RemoveEntity(owner));
            Assert.True(world.TryResolve(zone,10,10,out var removed)); Assert.IsNull(removed.ForegroundTile);
        }

        [Test] public void BerryHarvestFlagCannotLeakThroughLowLayerMemory()
        {
            world=NewWorld(); var zone=new Zone(ReferenceGladePlan.ZoneID); var cell=zone.GetCell(10,10);
            var owner=Owner(2); owner.SetTag("Terrain"); owner.GetPart<RenderPart>().RenderLayer=1;
            Assert.True(zone.AddEntity(owner,10,10)); cell.Explored=cell.IsVisible=true;
            Assert.True(world.TryResolve(zone,10,10,out var visible)); Assert.AreSame(vegetation,visible.ForegroundTile.sprite.texture);
            cell.IsVisible=false; owner.GetPart<HarvestablePart>().Harvested=true;
            Assert.True(world.TryResolve(zone,10,10,out var hidden)); Assert.IsNull(hidden.ForegroundTile);
            cell.IsVisible=true; Assert.True(world.TryResolve(zone,10,10,out var spent));
            Assert.AreNotSame(vegetation,spent.ForegroundTile.sprite.texture,"No permanent spent berry-bush frame is invented.");
        }

        [Test] public void ActualBerryHarvestRemovesWorldSpriteAndCannotBeRepeatedThroughRetainedReference()
        {
            using(var isolated=new DensityLootTestScope())
            {
                var previousFactory=HarvestablePart.Factory;
                try
                {
                    HarvestablePart.Factory=factory; Create(); world=NewWorld();
                    var zone=new Zone(ReferenceGladePlan.ZoneID); var cell=zone.GetCell(10,10);
                    cell.Explored=cell.IsVisible=true; var berry=Owner(2); zone.AddEntity(berry,10,10);
                    var actor=new Entity { ID=Guid.NewGuid().ToString("N"), BlueprintName="HarvestTestActor" };
                    actor.AddPart(new PhysicsPart()); actor.AddPart(new InventoryPart()); zone.AddEntity(actor,9,10);
                    Assert.True(world.TryResolve(zone,10,10,out var before)); Assert.AreSame(vegetation,before.ForegroundTile.sprite.texture);
                    Assert.True(DensityHarvestSecurityTests.Act(berry,actor,zone));
                    Assert.True(berry.GetPart<HarvestablePart>().Harvested); Assert.IsNull(zone.GetEntityCell(berry));
                    int produced=actor.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="WildBerries")
                        .Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
                    Assert.That(produced,Is.InRange(1,3)); Assert.False(art.TryResolve(berry,0,0,out _));
                    Assert.True(world.TryResolve(zone,10,10,out var gone)); Assert.IsNull(gone.ForegroundTile);
                    Assert.False(DensityHarvestSecurityTests.Act(berry,actor,zone));
                    Assert.AreEqual(produced,actor.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="WildBerries")
                        .Sum(e=>e.GetPart<StackerPart>()?.StackCount??1));
                }
                finally { HarvestablePart.Factory=previousFactory; }
            }
        }

        [TestCase(0,true)][TestCase(1,false)]
        public void TreeAndHedgeArtDoesNotTurnMovementBarriersIntoPlayerSightWalls(int index,bool solidTag)
        {
            world=NewWorld(); var zone=new Zone(ReferenceGladePlan.ZoneID); var owner=Owner(index);
            Assert.True(zone.AddEntity(owner,10,10)); FieldOfView.Compute(zone,9,10,5);
            Assert.True(zone.GetCell(11,10).IsVisible); Assert.AreEqual(solidTag,owner.HasTag("Solid"));
            Assert.True(world.TryResolve(zone,10,10,out var visible)); Assert.True(visible.BlocksMovement);
            FieldOfView.Compute(zone,9,10,5); Assert.True(zone.GetCell(11,10).IsVisible);
            Assert.AreEqual(solidTag,owner.HasTag("Solid")); Assert.False(owner.HasTag("Wall"));
            owner.SetTag("Wall"); FieldOfView.Compute(zone,9,10,5);
            Assert.False(zone.GetCell(11,10).IsVisible,"The sight fixture must detect a real wall counterexample.");
        }

        [Test] public void SevenArgumentInjectionIsIndependentAndEarlierConstructorsNeverLoadVegetation()
        {
            art=new FlatPixelOpeningArtCatalog(baseArt,null,null,null,null,cellar);
            Assert.False(art.TryResolve(Owner(0),0,0,out _)); Assert.True(art.TryResolve(Owner(4),0,0,out var bush));
            Assert.AreSame(baseArt,bush.sprite.texture); art.Dispose();
            art=NewArt(null,null,vegetation); Frame(Owner(0),0);
            Assert.False(art.TryResolve(Owner(4),0,0,out _));
            Assert.False(art.TryResolve(factory.CreateEntity("StoneWall"),0,0,out _));
        }

        [TestCase(63,24)][TestCase(64,23)]
        public void MalformedVegetationDoesNotDisableExistingGroupsOrMutateBorrowedSettings(int width,int height)
        {
            Object.DestroyImmediate(vegetation); vegetation=new Texture2D(width,height);
            vegetation.filterMode=FilterMode.Bilinear; vegetation.wrapMode=TextureWrapMode.Repeat; Create();
            Assert.False(art.TryResolve(Owner(0),0,0,out _)); Assert.True(art.TryResolve(Owner(4),0,0,out var bush));
            Assert.AreSame(baseArt,bush.sprite.texture);
            Assert.True(art.TryResolve(factory.CreateEntity("StoneWall"),0,0,out var wall)); Assert.AreSame(cellar,wall.sprite.texture);
            Assert.AreEqual(FilterMode.Bilinear,vegetation.filterMode); Assert.AreEqual(TextureWrapMode.Repeat,vegetation.wrapMode);
        }

        [Test] public void DisposalOwnsOnlyGeneratedSpritesAndTilesAndEachCatalogIsIndependent()
        {
            Create(); var tile=Frame(Owner(0),0); var sprite=tile.sprite;
            using(var other=NewArt(baseArt,cellar,vegetation))
            {
                Assert.True(other.TryResolve(Owner(0),0,0,out var separate)); Assert.AreNotSame(tile,separate);
                art.Dispose(); art.Dispose(); Assert.True(tile==null); Assert.True(sprite==null);
                Assert.True(separate!=null); Assert.True(vegetation!=null);
                Assert.False(art.TryResolve(Owner(0),0,0,out _));
            }
        }

        [Test] public void DefaultCatalogAndWorldSelectorBindTheActualImportedVegetationResource()
        {
            var resource=Resources.Load<Texture2D>(Resource);
            Assert.NotNull(resource,"Actual vegetation atlas must be imported; synthetic routing is not visible integration.");
            Assert.AreEqual(64,resource.width); Assert.AreEqual(24,resource.height);
            Assert.AreEqual(FilterMode.Point,resource.filterMode); Assert.AreEqual(TextureWrapMode.Clamp,resource.wrapMode);
            Assert.AreEqual(1,resource.mipmapCount); art=new FlatPixelOpeningArtCatalog(); world=new FlatPixelGreyboxCatalog();
            for(int i=0;i<4;i++)
            {
                var owner=Owner(i); Assert.True(art.TryResolve(owner,0,0,out var tile));
                Assert.AreSame(resource,tile.sprite.texture); Assert.AreEqual(new Rect(i*16,0,16,24),tile.sprite.rect);
                Assert.True(world.TryGetSubjectSprite(owner,0,0,out var portrait,out var tint));
                Assert.AreSame(resource,portrait.texture); Assert.AreEqual(tile.sprite.rect,portrait.rect); Assert.AreEqual(Color.white,tint);
            }
        }

        Entity Owner(int index) { var owner=factory.CreateEntity(Names[index]); Assert.NotNull(owner); return owner; }
        void Create()=>art=NewArt(baseArt,cellar,vegetation);
        Tile Frame(Entity owner,int frame)
        { Assert.True(art.TryResolve(owner,0,0,out var tile),owner.BlueprintName); Assert.AreSame(vegetation,tile.sprite.texture); Assert.AreEqual(new Rect(frame*16,0,16,24),tile.sprite.rect); return tile; }
        static Type[] Signature()=>new[]{typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D)};
        static FlatPixelOpeningArtCatalog NewArt(Texture2D baseAtlas,Texture2D cellarAtlas,Texture2D vegetationAtlas)
        {
            var ctor=typeof(FlatPixelOpeningArtCatalog).GetConstructor(Signature()); Assert.NotNull(ctor,"Seventh independent vegetation atlas API is required.");
            return (FlatPixelOpeningArtCatalog)ctor.Invoke(new object[]{baseAtlas,null,null,null,null,cellarAtlas,vegetationAtlas});
        }
        FlatPixelGreyboxCatalog NewWorld()
        {
            var ctor=typeof(FlatPixelGreyboxCatalog).GetConstructor(Signature()); Assert.NotNull(ctor,"World selector must forward the seventh independent vegetation atlas.");
            return (FlatPixelGreyboxCatalog)ctor.Invoke(new object[]{baseArt,null,null,null,null,cellar,vegetation});
        }
    }
}
