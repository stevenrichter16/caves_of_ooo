using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelOpeningItemsTests
    {
        static readonly string[] Names = { "Emberwheat", "ToastedEmberwheat", "DrawgourdShell", "DrawgourdSeed",
            "Mushroom", "RoastedMushroom", "GlowQuartz", "HealingTonic", "DriedMeat", "FireClay", "SalvagedTimber", "MarlbackCorpse" };
        EntityFactory factory;
        Texture2D items, baseArt, cast, anchors, states;
        FlatPixelGreyboxCatalog catalog;
        Zone zone;
        const int X = 10, Y = 10;
        [OneTimeSetUp] public void Content() => factory = DensityReferenceGladeTests.Factory();
        [SetUp] public void Setup()
        {
            items = new Texture2D(192,24); baseArt = new Texture2D(160,24); cast = new Texture2D(64,24);
            anchors = new Texture2D(192,24); states = new Texture2D(192,24);
            zone = new Zone(ReferenceGladePlan.ZoneID); zone.GetCell(X,Y).Explored = zone.GetCell(X,Y).IsVisible = true;
        }
        [TearDown] public void Cleanup()
        {
            catalog?.Dispose();
            foreach(var t in new[]{items,baseArt,cast,anchors,states}) if(t != null) Object.DestroyImmediate(t);
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        [TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)][TestCase(11)]
        public void CurrentLooseOwnerUsesOneCachedNativeFrame(int frame)
        {
            var owner = Add(frame); Create(); var first = Resolve(); AssertFrame(first, frame);
            Assert.AreSame(owner, first.Owner); Assert.AreEqual(Color.white, first.ForegroundColor);
            Assert.AreEqual(new Vector2(8,12), first.ForegroundTile.sprite.pivot);
            Assert.AreEqual(16, first.ForegroundTile.sprite.pixelsPerUnit);
            Assert.AreSame(first.ForegroundTile, Resolve().ForegroundTile);
            Assert.True(zone.RemoveEntity(owner)); Assert.IsNull(Resolve().ForegroundTile);
        }
        [TestCase(0,"Food")][TestCase(0,"Cookable")][TestCase(1,"Food")]
        [TestCase(2,"Waterskin")][TestCase(3,"Seed")][TestCase(4,"Food")][TestCase(4,"Cookable")]
        [TestCase(5,"Food")][TestCase(6,"MineralPreparation")][TestCase(7,"Tonic")][TestCase(8,"Food")]
        [TestCase(9,"Physics")][TestCase(10,"Physics")]
        public void MissingOrForeignFunctionCannotAdvertiseTheKnownItem(int frame,string name)
        {
            var owner=Add(frame);Create();AssertFrame(Resolve(),frame);
            var part=owner.GetPart(name);Assert.NotNull(part);part.ParentEntity=new Entity();NotItems();
            part.ParentEntity=owner;AssertFrame(Resolve(),frame);Assert.True(owner.RemovePart(part));NotItems();
        }
        [TestCase("inventory")][TestCase("equipped")][TestCase("hidden")]
        [TestCase("foreign-render")][TestCase("foreign-physics")][TestCase("zero-stack")][TestCase("foreign-stack")]
        public void InvalidGroundOwnershipLeavesNoNativeOrProceduralPhantom(string mutation)
        {
            var owner=Add(0);
            // Exercise the existing overload so RED demonstrates a real phantom,
            // independently of the missing new atlas API.
            catalog=new FlatPixelGreyboxCatalog(baseArt,cast,anchors,states);
            Assert.AreSame(owner,Resolve().Owner);
            var lower=Add(4);lower.GetPart<RenderPart>().RenderLayer=2;
            if(mutation=="inventory")owner.GetPart<PhysicsPart>().InInventory=new Entity();
            if(mutation=="equipped")owner.GetPart<PhysicsPart>().Equipped=new Entity();
            if(mutation=="hidden")owner.GetPart<RenderPart>().Visible=false;
            if(mutation=="foreign-render")owner.GetPart<RenderPart>().ParentEntity=new Entity();
            if(mutation=="foreign-physics")owner.GetPart<PhysicsPart>().ParentEntity=new Entity();
            if(mutation=="zero-stack")owner.GetPart<StackerPart>().StackCount=0;
            if(mutation=="foreign-stack")owner.GetPart<StackerPart>().ParentEntity=new Entity();
            Assert.AreSame(lower,Resolve().Owner,"The valid lower owner must remain visible.");
            zone.RemoveEntity(lower);Assert.IsNull(Resolve().Owner);Assert.IsNull(Resolve().ForegroundTile);
        }
        [Test] public void UntakeableItemKeepsTruthfulFallbackButNotItsPortableArt()
        {
            var owner=Add(0);Create();AssertFrame(Resolve(),0);
            owner.GetPart<PhysicsPart>().Takeable=false;NotItems();Assert.AreSame(owner,Resolve().Owner);
        }
        [TestCase("unknown-blueprint")][TestCase("explicit-reskin")]
        public void UnrecognizedItemIdentityDoesNotAliasKnownArt(string mutation)
        {
            var owner=Add(0);Create();AssertFrame(Resolve(),0);
            if(mutation=="unknown-blueprint")owner.BlueprintName="UnrelatedGrain";
            else owner.GetPart<RenderPart>().VisualID="unrelated-art";
            NotItems();Assert.AreSame(owner,Resolve().Owner);
        }
        [Test] public void SemanticCarriedArtworkResolvesWithoutWorldInsertionOrPortraitAuthorization()
        {
            var owner=Add(3);zone.RemoveEntity(owner);owner.GetPart<PhysicsPart>().InInventory=new Entity();
            var ctor=typeof(FlatPixelOpeningArtCatalog).GetConstructor(new[]{typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D)});
            Assert.NotNull(ctor);
            using(var art=(FlatPixelOpeningArtCatalog)ctor.Invoke(new object[]{baseArt,cast,anchors,states,items}))
            {
                Assert.True(art.TryResolve(owner,0,0,out var tile));Assert.AreSame(items,tile.sprite.texture);
                Assert.IsNull(zone.GetEntityCell(owner));
            }
        }
        [Test] public void StaleOldCellCannotDuplicateAMovedItem()
        {
            var owner=Add(0);Create();var old=zone.GetCell(X,Y);
            AssertFrame(Resolve(),0);Assert.True(zone.MoveEntity(owner,X+1,Y));
            old.Objects.Add(owner);var lower=Add(4);lower.GetPart<RenderPart>().RenderLayer=2;
            Assert.AreSame(lower,Resolve().Owner);
            zone.RemoveEntity(lower);Assert.IsNull(Resolve().ForegroundTile);
            zone.GetCell(X+1,Y).Explored=zone.GetCell(X+1,Y).IsVisible=true;
            Assert.True(catalog.TryResolve(zone,X+1,Y,out var moved));AssertFrame(moved,0);
        }
        [Test] public void NativeTimberRequiresItsApprovedSingleCellWhileLargerBodyKeepsTruthfulFallback()
        {
            var owner=factory.CreateEntity("SalvagedTimber");
            owner.AddPart(new SpatialFootprintPart { CellsRaw="0,0" });
            Assert.True(zone.AddEntity(owner,X,Y));Create();AssertFrame(Resolve(),10);
            Assert.True(zone.TryChangeFootprint(owner,"0,0;1,0"));
            Assert.AreEqual(2,zone.GetOccupiedCells(owner).Count,"Art refusal must not shrink the real body.");
            var value=Resolve();Assert.AreSame(owner,value.Owner);
            Assert.True(catalog.TryGetFallbackSubjectSprite(owner,X,Y,out var glyph,out var tint));
            Assert.AreSame(glyph.texture,value.ForegroundTile.sprite.texture);
            Assert.AreEqual(glyph.rect,value.ForegroundTile.sprite.rect);
            Assert.AreNotSame(items,value.ForegroundTile.sprite.texture);
            zone.GetCell(X+1,Y).Explored=zone.GetCell(X+1,Y).IsVisible=true;
            Assert.True(catalog.TryResolve(zone,X+1,Y,out var covered));Assert.AreSame(owner,covered.Owner);
            Assert.AreSame(glyph.texture,covered.ForegroundTile.sprite.texture);
            Assert.True(zone.TryChangeFootprint(owner,"0,0"));AssertFrame(Resolve(),10);
            Assert.AreEqual(1,zone.GetOccupiedCells(owner).Count);
            Assert.True(zone.RemoveEntity(owner));Assert.IsNull(Resolve().ForegroundTile);
        }
        [Test] public void RememberedAndUnexploredCellsCannotExposeLooseItem()
        {
            Add(7);Create();AssertFrame(Resolve(),7);
            zone.GetCell(X,Y).IsVisible=false;NotItems();
            zone.GetCell(X,Y).Explored=false;Assert.False(catalog.TryResolve(zone,X,Y,out _));
        }
        [Test] public void LaterEqualLayerSeedCoversShellUntilActuallyRemoved()
        {
            var shell=Add(2);var seed=Add(3);Create();AssertFrame(Resolve(),3);
            Assert.True(zone.RemoveEntity(seed));AssertFrame(Resolve(),2);Assert.AreSame(shell,Resolve().Owner);
            Assert.True(zone.RemoveEntity(shell));Assert.IsNull(Resolve().ForegroundTile);
        }
        [Test] public void FoodAndSeedRequireTheirAuthoredTransformationTargets()
        {
            var grain=Add(0);Create();AssertFrame(Resolve(),0);
            grain.GetPart<CookablePart>().Into="RoastedMushroom";NotItems();zone.RemoveEntity(grain);
            var seed=Add(3);AssertFrame(Resolve(),3);seed.GetPart<SeedPart>().CropBlueprint="MarlrootCrop";NotItems();
        }
        [Test] public void CorpseDoesNotPretendAnotherSpeciesOrLiveCreatureIsMarlbackRemains()
        {
            var corpse=Add(11);Create();AssertFrame(Resolve(),11);
            corpse.Properties["SourceBlueprint"]="PetDog";NotItems();
            corpse.Properties["SourceBlueprint"]="MarlbackScrabbler";AssertFrame(Resolve(),11);
            corpse.Properties["SourceID"]=" ";NotItems();
            corpse.Properties["SourceID"]="native-test-deceased";corpse.Tags.Remove("Corpse");NotItems();
            corpse.Tags["Corpse"]="";corpse.Tags["Creature"]="";NotItems();
        }
        [Test] public void RawQuartzAndClayDoNotBecomeSourcesOrFlames()
        {
            var quartz=Add(6);Create();AssertFrame(Resolve(),6);Assert.IsNull(quartz.GetPart<LightSourcePart>());
            zone.RemoveEntity(quartz);var clay=Add(9);AssertFrame(Resolve(),9);Assert.False(clay.HasEffect<BurningEffect>());
        }
        [Test] public void MissingItemsPreservePriorArtAndEarlierOverloadDoesNotLoadImplicitly()
        {
            Add(0);catalog=NewCatalog(baseArt,cast,anchors,states,null);NotItems();catalog.Dispose();
            catalog=new FlatPixelGreyboxCatalog(baseArt,cast,anchors,states);NotItems();
            zone.RemoveEntity(Resolve().Owner);var player=factory.CreateEntity("Player");Assert.True(zone.AddEntity(player,X,Y));
            Assert.AreSame(baseArt,Resolve().ForegroundTile.sprite.texture);
        }
        [Test] public void MalformedAtlasFallsBackAndDisposalNeverOwnsBorrowedTextures()
        {
            Object.DestroyImmediate(items);items=new Texture2D(191,24);Add(0);Create();NotItems();
            catalog.Dispose();Assert.NotNull(items);Assert.AreEqual(191,items.width);
            Assert.False(catalog.TryResolve(zone,X,Y,out _));
        }
        [Test] public void DefaultCatalogBindsAllActualImportedItems()
        {
            var resource=Resources.Load<Texture2D>("Sprites/Live16x24/opening-items");
            Assert.NotNull(resource,"Actual item atlas missing: synthetic art is not integration.");
            Assert.AreEqual(192,resource.width);Assert.AreEqual(24,resource.height);
            Assert.AreEqual(FilterMode.Point,resource.filterMode);Assert.AreEqual(TextureWrapMode.Clamp,resource.wrapMode);Assert.AreEqual(1,resource.mipmapCount);
            catalog=new FlatPixelGreyboxCatalog();
            for(int i=0;i<Names.Length;i++)
            {var owner=Add(i);var value=Resolve();Assert.AreSame(resource,value.ForegroundTile.sprite.texture);Assert.AreEqual(new Rect(i*16,0,16,24),value.ForegroundTile.sprite.rect);zone.RemoveEntity(owner);}
        }
        Entity Add(int i)
        {
            var owner=factory.CreateEntity(Names[i]);Assert.NotNull(owner);
            if(i==11){owner.Properties["SourceBlueprint"]="MarlbackScrabbler";owner.Properties["SourceID"]="native-test-deceased";}
            Assert.True(zone.AddEntity(owner,X,Y));return owner;
        }
        void Create()=>catalog=NewCatalog(baseArt,cast,anchors,states,items);
        static FlatPixelGreyboxCatalog NewCatalog(Texture2D a,Texture2D b,Texture2D c,Texture2D d,Texture2D e)
        {
            var ctor=typeof(FlatPixelGreyboxCatalog).GetConstructor(new[]{typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D)});
            Assert.NotNull(ctor,"A fifth independently optional borrowed item atlas is required.");
            return (FlatPixelGreyboxCatalog)ctor.Invoke(new object[]{a,b,c,d,e});
        }
        FlatPixelGreyboxCell Resolve(){Assert.True(catalog.TryResolve(zone,X,Y,out var v));return v;}
        void AssertFrame(FlatPixelGreyboxCell value,int i)
        {Assert.NotNull(value.ForegroundTile);Assert.AreSame(items,value.ForegroundTile.sprite.texture);Assert.AreEqual(new Rect(i*16,0,16,24),value.ForegroundTile.sprite.rect);}
        void NotItems(){var v=Resolve();if(v.ForegroundTile!=null)Assert.AreNotSame(items,v.ForegroundTile.sprite.texture);}
    }
}
