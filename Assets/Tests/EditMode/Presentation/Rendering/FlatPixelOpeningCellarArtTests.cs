using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelOpeningCellarArtTests
    {
        static readonly string[] Names = { "StairsUp", "Crate", "GleanersTimberPallet", "SootrootCrop",
            "SootrootCrop", "SootrootCrop", "SootrootPulp", "SootrootSeed", "StoneWall" };
        EntityFactory factory;
        Texture2D cellar, baseArt, cast, anchors, states, items;
        FlatPixelOpeningArtCatalog art;
        FlatPixelGreyboxCatalog world;
        [OneTimeSetUp] public void Content() => factory = DensityReferenceGladeTests.Factory();
        [SetUp] public void Setup()
        {
            cellar = new Texture2D(144,24); baseArt = new Texture2D(160,24); cast = new Texture2D(64,24);
            anchors = new Texture2D(192,24); states = new Texture2D(192,24); items = new Texture2D(192,24);
        }
        [TearDown] public void Cleanup()
        {
            art?.Dispose(); world?.Dispose();
            foreach (var texture in new[]{cellar,baseArt,cast,anchors,states,items}) if(texture != null) Object.DestroyImmediate(texture);
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)]
        public void CurrentOwnedSemanticsUseOneCachedNativeCell(int frame)
        {
            var owner = Owner(frame); Create(); var tile = Frame(owner,frame);
            Assert.AreEqual(new Vector2(8,12),tile.sprite.pivot); Assert.AreEqual(16,tile.sprite.pixelsPerUnit);
            Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);
            Assert.AreSame(tile,Frame(owner,frame)); Assert.AreSame(tile.sprite,Frame(owner,frame).sprite);
            owner.GetPart<RenderPart>().Visible=false; Assert.False(art.TryResolve(owner,0,0,out _));
        }
        [TestCase(0,"StairsUp")][TestCase(0,"Physics")][TestCase(1,"Container")]
        [TestCase(2,"Physics")][TestCase(2,"Handling")][TestCase(2,"Harvestable")]
        [TestCase(3,"Crop")][TestCase(6,"Physics")][TestCase(6,"Tonic")][TestCase(6,"CureTonic")]
        [TestCase(7,"Physics")][TestCase(7,"Seed")][TestCase(8,"Physics")]
        public void ForeignOrMissingFunctionalPartCannotClaimNativeIdentity(int frame,string name)
        {
            var owner=Owner(frame); Create(); Frame(owner,frame);
            var part=owner.GetPart(name); Assert.NotNull(part); part.ParentEntity=new Entity();
            Assert.False(art.TryResolve(owner,0,0,out _)); part.ParentEntity=owner; Frame(owner,frame);
            Assert.True(owner.RemovePart(part)); Assert.False(art.TryResolve(owner,0,0,out _));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)]
        public void ExplicitReskinAndForeignRenderDoNotBorrowCellarArtwork(int frame)
        {
            var owner=Owner(frame); Create(); Frame(owner,frame);
            var render=owner.GetPart<RenderPart>(); render.VisualID="other-site-reskin";
            Assert.False(art.TryResolve(owner,0,0,out _)); render.VisualID=""; Frame(owner,frame);
            render.ParentEntity=new Entity(); Assert.False(art.TryResolve(owner,0,0,out _));
            render.ParentEntity=owner; owner.BlueprintName="UnrelatedOwner"; Assert.False(art.TryResolve(owner,0,0,out _));
        }
        [Test] public void CrateContentsDoNotInventAnOpenLidState()
        {
            var owner=Owner(1); Create(); var tile=Frame(owner,1); var container=owner.GetPart<ContainerPart>();
            var clay=Owner(6); container.Contents.Add(clay); clay.GetPart<PhysicsPart>().InInventory=owner;
            Assert.AreSame(tile,Frame(owner,1)); container.Contents.Clear(); Assert.AreSame(tile,Frame(owner,1));
        }
        [Test] public void PalletOnlyPromisesAnUnspentSolidHaulableTimberSource()
        {
            var owner=Owner(2); Create(); Frame(owner,2); var h=owner.GetPart<HarvestablePart>();
            h.Harvested=true; Rejected(owner); h.Harvested=false; Frame(owner,2);
            h.YieldBlueprint="FireClay"; Rejected(owner); h.YieldBlueprint="SalvagedTimber";
            h.YieldMin=0; Rejected(owner); h.YieldMin=2;
            h.YieldMax=1; Rejected(owner); h.YieldMax=2;
            h.YieldChance=0; Rejected(owner); h.YieldChance=100;
            var physics=owner.GetPart<PhysicsPart>(); physics.Solid=false; Rejected(owner); physics.Solid=true;
            Frame(owner,2); // Hauling capability depends on actor strength; no fixed readiness cue.
        }
        [Test] public void CropFollowsStageWithoutPinningMoistureOrTiming()
        {
            var owner=Owner(3); Create(); var crop=owner.GetPart<CropPart>();
            for(int stage=0;stage<3;stage++)
            {
                crop.GrowthStage=stage; var tile=Frame(owner,3+stage);
                crop.MoistureTicks=100; crop.TicksPerStage=7; crop.TicksInStage=3; crop.Composted=true;
                Assert.AreSame(tile,Frame(owner,3+stage)); crop.MoistureTicks=0;
            }
            crop.GrowthStage=-1; Rejected(owner); crop.GrowthStage=3; Rejected(owner);
        }
        [TestCase("yield")][TestCase("yield-count")][TestCase("seed")][TestCase("seed-count")][TestCase("maturity")]
        public void CropRequiresItsRealHarvestContract(string mutation)
        {
            var owner=Owner(5); Create(); Frame(owner,5); var crop=owner.GetPart<CropPart>();
            if(mutation=="yield")crop.YieldBlueprint="Emberwheat";
            if(mutation=="yield-count")crop.YieldCount=0;
            if(mutation=="seed")crop.SeedYieldBlueprint="DrawgourdSeed";
            if(mutation=="seed-count")crop.SeedYieldCount=0;
            if(mutation=="maturity")crop.HarvestAtMaturity=false;
            Rejected(owner);
        }
        [TestCase(6)][TestCase(7)] public void LooseProductsRequirePortableValidStacksButNotGroundPlacement(int frame)
        {
            var owner=Owner(frame); Create(); var tile=Frame(owner,frame); var physics=owner.GetPart<PhysicsPart>();
            physics.InInventory=new Entity(); Assert.AreSame(tile,Frame(owner,frame));
            physics.Takeable=false; Rejected(owner); physics.Takeable=true;
            var stack=owner.GetPart<StackerPart>(); Assert.NotNull(stack); stack.StackCount=0; Rejected(owner);
            stack.StackCount=2; Assert.AreSame(tile,Frame(owner,frame)); stack.ParentEntity=new Entity(); Rejected(owner);
        }
        [Test] public void PulpIsTopicalBurnCureAndSeedRequiresPreparedSootrootBed()
        {
            Create(); var pulp=Owner(6); Frame(pulp,6);
            pulp.GetPart<TonicPart>().Drink=true; Rejected(pulp); pulp.GetPart<TonicPart>().Drink=false;
            pulp.GetPart<CureTonicPart>().CureEffect="PoisonedEffect"; Rejected(pulp);
            var seed=Owner(7); Frame(seed,7); seed.GetPart<SeedPart>().CropBlueprint="DrawgourdCrop"; Rejected(seed);
            seed.GetPart<SeedPart>().CropBlueprint="SootrootCrop"; seed.GetPart<SeedPart>().RequireCultivatedSoil=false; Rejected(seed);
        }
        [Test] public void CurrentCellAndFogAuthorityRemainWithWorldSelector()
        {
            world=NewWorld(); var zone=new Zone(ReferenceGladePlan.ZoneID); var cell=zone.GetCell(10,10);
            cell.IsVisible=cell.Explored=true; var crop=Owner(5); Assert.True(zone.AddEntity(crop,10,10));
            Assert.True(world.TryResolve(zone,10,10,out var visible)); Assert.AreSame(cellar,visible.ForegroundTile.sprite.texture);
            cell.IsVisible=false; Assert.True(world.TryResolve(zone,10,10,out var memory)); Assert.IsNull(memory.ForegroundTile);
            cell.Explored=false; Assert.False(world.TryResolve(zone,10,10,out _));
            cell.IsVisible=cell.Explored=true; Assert.True(zone.RemoveEntity(crop));
            Assert.True(world.TryResolve(zone,10,10,out var gone)); Assert.IsNull(gone.ForegroundTile);
            var seed=Owner(7); Assert.True(zone.AddEntity(seed,10,10)); Assert.True(zone.MoveEntity(seed,11,10)); cell.Objects.Add(seed);
            Assert.True(world.TryResolve(zone,10,10,out var stale)); Assert.IsNull(stale.ForegroundTile);
            Assert.True(world.TryGetSubjectSprite(seed,11,10,out var portrait,out var tint)); Assert.AreSame(cellar,portrait.texture); Assert.AreEqual(Color.white,tint);
        }
        [Test] public void MissingCellarAndLegacyConstructorPreserveEarlierGroups()
        {
            art=new FlatPixelOpeningArtCatalog(baseArt,cast,anchors,states,items);
            Assert.False(art.TryResolve(Owner(0),0,0,out _));
            Assert.False(art.TryResolve(Owner(8),0,0,out _),"A cellar wall must not alias the stepped surface ruin pillar.");
            Assert.True(art.TryResolve(factory.CreateEntity("Player"),0,0,out var player)); Assert.AreSame(baseArt,player.sprite.texture);
            art.Dispose(); art=NewArt(null,null,null,null,null,cellar); Frame(Owner(0),0);
            Assert.False(art.TryResolve(factory.CreateEntity("Player"),0,0,out _));
        }
        [Test] public void MalformedAtlasDoesNotDisableExistingArtOrChangeBorrowedSettings()
        {
            Object.DestroyImmediate(cellar); cellar=new Texture2D(143,24); cellar.filterMode=FilterMode.Bilinear; cellar.wrapMode=TextureWrapMode.Repeat;
            Create(); Assert.False(art.TryResolve(Owner(0),0,0,out _));
            Assert.True(art.TryResolve(factory.CreateEntity("Player"),0,0,out var player)); Assert.AreSame(baseArt,player.sprite.texture);
            Assert.AreEqual(FilterMode.Bilinear,cellar.filterMode); Assert.AreEqual(TextureWrapMode.Repeat,cellar.wrapMode);
        }
        [Test] public void DisposalReleasesOnlyGeneratedTilesAndSpritesAndRejectsReuse()
        {
            Create(); var tile=Frame(Owner(0),0); var sprite=tile.sprite; art.Dispose(); art.Dispose();
            Assert.True(tile==null); Assert.True(sprite==null); Assert.True(cellar!=null); Assert.AreEqual(144,cellar.width);
            Assert.False(art.TryResolve(Owner(0),0,0,out _));
        }
        [Test] public void DefaultCatalogUsesActualImportedCellarAtlasForAllNineFrames()
        {
            var resource=Resources.Load<Texture2D>("Sprites/Live16x24/opening-cellar");
            Assert.NotNull(resource,"Missing actual cellar resource; synthetic tests are not art integration.");
            Assert.AreEqual(144,resource.width); Assert.AreEqual(24,resource.height); Assert.AreEqual(FilterMode.Point,resource.filterMode);
            Assert.AreEqual(TextureWrapMode.Clamp,resource.wrapMode); Assert.AreEqual(1,resource.mipmapCount);
            art=new FlatPixelOpeningArtCatalog();
            for(int i=0;i<9;i++){Assert.True(art.TryResolve(Owner(i),0,0,out var tile)); Assert.AreSame(resource,tile.sprite.texture); Assert.AreEqual(new Rect(i*16,0,16,24),tile.sprite.rect);}
        }
        [TestCase("reskin")][TestCase("not-wall")][TestCase("not-solid")][TestCase("takeable")]
        [TestCase("foreign-physics")][TestCase("hidden")][TestCase("foreign-render")]
        public void OrdinaryStoneWallUsesSolidCellarMasonryOnlyWithItsCurrentWallContract(string mutation)
        {
            var owner=factory.CreateEntity("StoneWall");
            Create();
            Assert.True(art.TryResolve(owner,0,0,out var tile),"Ordinary cellar masonry needs a solid dedicated frame.");
            Assert.AreSame(cellar,tile.sprite.texture); Assert.AreEqual(new Rect(128,0,16,24),tile.sprite.rect);
            if(mutation=="reskin")owner.GetPart<RenderPart>().VisualID="other-wall-art";
            if(mutation=="not-wall")owner.Tags.Remove("Wall");
            if(mutation=="not-solid")owner.GetPart<PhysicsPart>().Solid=false;
            if(mutation=="takeable")owner.GetPart<PhysicsPart>().Takeable=true;
            if(mutation=="foreign-physics")owner.GetPart<PhysicsPart>().ParentEntity=new Entity();
            if(mutation=="hidden")owner.GetPart<RenderPart>().Visible=false;
            if(mutation=="foreign-render")owner.GetPart<RenderPart>().ParentEntity=new Entity();
            Assert.False(art.TryResolve(owner,0,0,out _));
        }
        Entity Owner(int frame)
        {
            var owner=factory.CreateEntity(Names[frame]); Assert.NotNull(owner);
            if(frame>=3&&frame<=5)owner.GetPart<CropPart>().GrowthStage=frame-3;
            return owner;
        }
        void Create()=>art=NewArt(baseArt,cast,anchors,states,items,cellar);
        Tile Frame(Entity owner,int frame)
        { Assert.True(art.TryResolve(owner,0,0,out var tile),owner.BlueprintName); Assert.AreSame(cellar,tile.sprite.texture); Assert.AreEqual(new Rect(frame*16,0,16,24),tile.sprite.rect); return tile; }
        void Rejected(Entity owner)=>Assert.False(art.TryResolve(owner,0,0,out _));
        static Type[] Signature()=>new[]{typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D)};
        static FlatPixelOpeningArtCatalog NewArt(Texture2D a,Texture2D b,Texture2D c,Texture2D d,Texture2D e,Texture2D f)
        {
            var ctor=typeof(FlatPixelOpeningArtCatalog).GetConstructor(Signature()); Assert.NotNull(ctor,"Sixth optional cellar atlas API is required.");
            return (FlatPixelOpeningArtCatalog)ctor.Invoke(new object[]{a,b,c,d,e,f});
        }
        FlatPixelGreyboxCatalog NewWorld()
        {
            var ctor=typeof(FlatPixelGreyboxCatalog).GetConstructor(Signature()); Assert.NotNull(ctor,"World selector must forward the sixth optional cellar atlas.");
            return (FlatPixelGreyboxCatalog)ctor.Invoke(new object[]{baseArt,cast,anchors,states,items,cellar});
        }
    }
}
