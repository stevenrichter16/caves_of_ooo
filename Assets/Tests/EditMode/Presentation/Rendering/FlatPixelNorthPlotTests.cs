using System;
using System.IO;
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
    public sealed class FlatPixelNorthPlotTests
    {
        const string PlotResource="Sprites/Live16x24/north-plot", KeeperResource="Sprites/Live16x24/seed-keeper-3";
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static readonly string[] Names={"CandyCarrotCrop","EmberwheatCrop","MarlrootCrop","PitchpodCrop","ConnectedReserveTray","SpreadSeedKeeper"};
        static readonly string[] Produce={"CandyCarrot","Emberwheat","MarlrootClod","PitchpodResin"};
        static readonly string[] Seeds={"","","MarlrootSeed","PitchpodSeed"};
        EntityFactory factory;Texture2D plot,keeper,states,works;
        FlatPixelOpeningArtCatalog art;FlatPixelGreyboxCatalog world;
        [OneTimeSetUp] public void Content()=>factory=DensityReferenceGladeTests.Factory();
        [SetUp] public void Setup(){plot=new Texture2D(128,24);keeper=new Texture2D(16,24);states=new Texture2D(192,24);works=new Texture2D(144,24);}
        [TearDown] public void Cleanup()
        {
            art?.Dispose();world?.Dispose();art=null;world=null;
            foreach(var texture in new[]{plot,keeper,states,works})if(texture!=null)Object.DestroyImmediate(texture);
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void PlantedStageReusesAcceptedMarkWithoutLoadingTheNewPlotAtlas(int index)
        {
            art=NewArt(states,null,null,null);var owner=Owner(index,0);var tile=Frame(owner,states,6);
            Assert.AreSame(tile,Frame(owner,states,6));Assert.AreEqual(0,owner.GetPart<CropPart>().GrowthStage);
            owner.GetPart<CropPart>().GrowthStage=1;Assert.False(art.TryResolve(owner,0,0,out _));
        }
        [TestCase(0,1,0)][TestCase(1,1,1)][TestCase(2,1,2)][TestCase(2,2,3)][TestCase(3,1,4)][TestCase(3,2,5)][TestCase(4,0,6)]
        public void ExactGrowthAndNeutralTraySelectCachedFullCellFrames(int index,int stage,int frame)
        {
            Create();var owner=Owner(index,stage);var tile=Frame(owner,plot,frame);
            Assert.AreSame(tile,Frame(owner,plot,frame));Assert.AreEqual(new Vector2(8,12),tile.sprite.pivot);
            Assert.AreEqual(16,tile.sprite.pixelsPerUnit);Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);
            Assert.False(owner.GetPart<PhysicsPart>().Solid);Assert.False(owner.GetPart<PhysicsPart>().Takeable);
            owner.GetPart<RenderPart>().Visible=false;Assert.False(art.TryResolve(owner,0,0,out _));
        }
        [Test] public void KeeperUsesAllTwentyFourRowsAndDoesNotRequireAReserveClaim()
        {
            Create();var owner=Owner(5);Assert.False(owner.HasPart<LocalGatheringClaimPart>());var tile=Frame(owner,keeper,0);
            Assert.AreEqual(new Rect(0,0,16,24),tile.sprite.rect);Assert.AreEqual(new Vector2(8,12),tile.sprite.pivot);Assert.AreEqual(16,tile.sprite.pixelsPerUnit);
            var claim=new LocalGatheringClaimPart{Configured=false};owner.AddPart(claim);Assert.AreSame(tile,Frame(owner,keeper,0));
            claim.ParentEntity=new Entity();Assert.AreSame(tile,Frame(owner,keeper,0));Assert.True(owner.RemovePart(claim));
            owner.GetPart<BrainPart>().Passive=false;owner.GetStat("Hitpoints").BaseValue=1;
            owner.GetPart<TraderPart>().StockTable="";Assert.AreSame(tile,Frame(owner,keeper,0));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void ExplicitReskinsForeignRenderAndUnknownIdentityKeepFallback(int index)
        {
            Create();var owner=Owner(index,1);Assert.True(art.TryResolve(owner,0,0,out _));var render=owner.GetPart<RenderPart>();
            render.VisualID="custom-plot-art";Assert.False(art.TryResolve(owner,0,0,out _));render.VisualID="";
            render.VisualVariant="custom-costume";Assert.False(art.TryResolve(owner,0,0,out _));render.VisualVariant="";
            render.GlyphVariants="custom-glyphs";Assert.False(art.TryResolve(owner,0,0,out _));render.GlyphVariants="";
            render.ParentEntity=new Entity();Assert.False(art.TryResolve(owner,0,0,out _));render.ParentEntity=owner;
            Assert.True(art.TryResolve(owner,0,0,out _));owner.BlueprintName="DifferentPlotOwner";Assert.False(art.TryResolve(owner,0,0,out _));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void PhysicalAndRoleIdentityMustBelongToTheActualOwner(int index)
        {
            Create();var owner=Owner(index,1);Assert.True(art.TryResolve(owner,0,0,out _));var physical=owner.GetPart<PhysicsPart>();
            physical.ParentEntity=new Entity();Assert.False(art.TryResolve(owner,0,0,out _));physical.ParentEntity=owner;
            physical.Takeable=true;Assert.False(art.TryResolve(owner,0,0,out _));physical.Takeable=false;
            physical.InInventory=new Entity();Assert.False(art.TryResolve(owner,0,0,out _));physical.InInventory=null;
            if(index<5){physical.Solid=true;Assert.False(art.TryResolve(owner,0,0,out _));physical.Solid=false;owner.SetTag("Creature");Assert.False(art.TryResolve(owner,0,0,out _));owner.Tags.Remove("Creature");}
            else{owner.Tags.Remove("Creature");Assert.False(art.TryResolve(owner,0,0,out _));owner.SetTag("Creature");}
            Assert.True(art.TryResolve(owner,0,0,out _));owner.SetTag("Item");Assert.False(art.TryResolve(owner,0,0,out _));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void CropArtworkRequiresItsOwnedStageAndExactFunctionalOutput(int index)
        {
            Create();var owner=Owner(index,1);Assert.True(art.TryResolve(owner,0,0,out _));var crop=owner.GetPart<CropPart>();
            crop.ParentEntity=new Entity();Assert.False(art.TryResolve(owner,0,0,out _));crop.ParentEntity=owner;
            crop.GrowthStage=-1;Assert.False(art.TryResolve(owner,0,0,out _));crop.GrowthStage=index<2?2:3;Assert.False(art.TryResolve(owner,0,0,out _));crop.GrowthStage=1;
            crop.YieldBlueprint="UnrelatedProduce";Assert.False(art.TryResolve(owner,0,0,out _));crop.YieldBlueprint=Produce[index];
            crop.YieldCount=0;Assert.False(art.TryResolve(owner,0,0,out _));crop.YieldCount=33;Assert.False(art.TryResolve(owner,0,0,out _));crop.YieldCount=2;
            crop.HarvestAtMaturity=!crop.HarvestAtMaturity;Assert.False(art.TryResolve(owner,0,0,out _));crop.HarvestAtMaturity=!crop.HarvestAtMaturity;
            Assert.True(art.TryResolve(owner,0,0,out _));owner.Tags.Remove("Crop");Assert.False(art.TryResolve(owner,0,0,out _));owner.SetTag("Crop");
            Assert.True(owner.RemovePart(crop));Assert.False(art.TryResolve(owner,0,0,out _));
        }
        [TestCase(2)][TestCase(3)]
        public void ReserveCropsNeedNoVegetationTagButKeepTheirRealSavedSeedContract(int index)
        {
            Create();var owner=Owner(index,2);Assert.False(owner.HasTag("Vegetation"));Assert.True(art.TryResolve(owner,0,0,out _));
            var crop=owner.GetPart<CropPart>();crop.SeedYieldBlueprint="OtherSeed";Assert.False(art.TryResolve(owner,0,0,out _));crop.SeedYieldBlueprint=Seeds[index];
            crop.SeedYieldCount=0;Assert.False(art.TryResolve(owner,0,0,out _));crop.SeedYieldCount=31;Assert.False(art.TryResolve(owner,0,0,out _));crop.SeedYieldCount=1;
            Assert.True(art.TryResolve(owner,0,0,out _));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void MoistureTimingCompostAndValidQuantityTuningDoNotInventIdentityStates(int index)
        {
            Create();var owner=Owner(index,index<2?1:2);Assert.True(art.TryResolve(owner,0,0,out var tile));var crop=owner.GetPart<CropPart>();
            crop.MoistureTicks=0;crop.TicksPerStage=1;crop.TicksInStage=0;crop.Composted=true;crop.YieldCount=3;
            if(index<2){crop.SeedYieldBlueprint="IgnoredOnAutomaticYield";crop.SeedYieldCount=999;}
            Assert.True(art.TryResolve(owner,0,0,out var dry));Assert.AreSame(tile,dry);
            crop.MoistureTicks=100;crop.TicksPerStage=200;crop.TicksInStage=90;Assert.True(art.TryResolve(owner,0,0,out var wet));Assert.AreSame(tile,wet);
        }
        [Test] public void TrayArtworkIsNeutralWhenContentsChangeAndDoesNotRequireClaimBinding()
        {
            Create();var owner=Owner(4);var tile=Frame(owner,plot,6);var container=owner.GetPart<ContainerPart>();
            container.Contents.Add(factory.CreateEntity("MarlrootSeed"));Assert.AreSame(tile,Frame(owner,plot,6));container.Contents.Clear();Assert.AreSame(tile,Frame(owner,plot,6));
            container.ParentEntity=new Entity();Assert.False(art.TryResolve(owner,0,0,out _));container.ParentEntity=owner;
            Assert.True(owner.RemovePart(container));Assert.False(art.TryResolve(owner,0,0,out _));
        }
        [Test] public void CordIsOnlyAnExplicitBorrowedGroundDecorationNeverAForegroundOwner()
        {
            Create();Assert.True(Cord(art,out var tile));Assert.AreSame(plot,tile.sprite.texture);Assert.AreEqual(new Rect(112,0,16,24),tile.sprite.rect);
            Assert.True(Cord(art,out var again));Assert.AreSame(tile,again);
            for(int i=0;i<Names.Length;i++){var owner=Owner(i,1);Assert.True(art.TryResolve(owner,0,0,out var body));Assert.AreNotSame(tile,body);}
            art.Dispose();Assert.False(Cord(art,out _));
        }
        [Test] public void NinthAndTenthGroupsRemainIndependentAndEarlierInjectionsOmitBoth()
        {
            art=new FlatPixelOpeningArtCatalog(null,null,null,states,null,null,null,works);
            Assert.False(art.TryResolve(Owner(0,1),0,0,out _));Assert.False(art.TryResolve(Owner(5),0,0,out _));Assert.False(Cord(art,out _));art.Dispose();
            art=NewArt(null,works,plot,null,9);Frame(Owner(0,1),plot,0);Assert.False(art.TryResolve(Owner(5),0,0,out _));Assert.True(art.TryResolve(factory.CreateEntity("AlchemyStill"),0,0,out var still));Assert.AreSame(works,still.sprite.texture);art.Dispose();
            art=NewArt(null,null,null,keeper);Frame(Owner(5),keeper,0);Assert.False(art.TryResolve(Owner(0,1),0,0,out _));Assert.False(Cord(art,out _));
        }
        [TestCase(true,127,24)][TestCase(true,128,23)][TestCase(false,15,24)][TestCase(false,16,23)]
        public void MalformedOptionalGroupDoesNotDisablePeersOrMutateBorrowedImportSettings(bool malformedPlot,int width,int height)
        {
            if(malformedPlot){Object.DestroyImmediate(plot);plot=new Texture2D(width,height);}else{Object.DestroyImmediate(keeper);keeper=new Texture2D(width,height);}
            var bad=malformedPlot?plot:keeper;bad.filterMode=FilterMode.Bilinear;bad.wrapMode=TextureWrapMode.Repeat;Create();
            Assert.AreEqual(!malformedPlot,art.TryResolve(Owner(0,1),0,0,out _));Assert.AreEqual(malformedPlot,art.TryResolve(Owner(5),0,0,out _));
            Frame(Owner(0,0),states,6);Assert.True(art.TryResolve(factory.CreateEntity("AlchemyStill"),0,0,out _));
            Assert.AreEqual(FilterMode.Bilinear,bad.filterMode);Assert.AreEqual(TextureWrapMode.Repeat,bad.wrapMode);
        }
        [Test] public void DisposalOwnsGeneratedFramesButNeverApprovedKeeperOrOtherBorrowedTextures()
        {
            Create();var crop=Frame(Owner(0,1),plot,0);var person=Frame(Owner(5),keeper,0);var cropSprite=crop.sprite;var personSprite=person.sprite;
            using(var peer=NewArt(states,works,plot,keeper))
            {
                Assert.True(peer.TryResolve(Owner(5),0,0,out var separate));Assert.AreNotSame(person,separate);
                art.Dispose();art.Dispose();Assert.True(crop==null&&person==null&&cropSprite==null&&personSprite==null);Assert.True(plot!=null&&keeper!=null&&states!=null&&works!=null);Assert.True(separate!=null);
            }
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void AuthorizedExactSubjectPortraitUsesSameNativeFrameAndEndsWithVisibility(int index)
        {
            using(var f=new PortraitFixture(NewWorld(states,works,plot,keeper),factory))
            using(var portrait=PortraitProbe())
            {
                var owner=Owner(index,1);Assert.True(f.Zone.AddEntity(owner,10,10));var doc=ExamineDocumentBuilder.BuildWorld(owner,f.Zone,f.Zone.GetCell(10,10),f.Viewer);
                Assert.True(ExamineDocumentBuilder.IsCurrentSubject(doc));Assert.True(f.Catalog.TryResolve(f.Zone,10,10,out var body));
                var image=portrait.GetPortrait(doc,f.Presentation);Assert.AreSame(body.ForegroundTile.sprite.texture,image.Texture);Assert.AreEqual(Color.white,image.Tint);Assert.False(image.IsModel);
                Assert.AreEqual(new Rect(body.ForegroundTile.sprite.rect.x/image.Texture.width,0,16f/image.Texture.width,1),image.Uv);Assert.IsNull(portrait.CurrentModelId);
                f.Zone.GetCell(10,10).IsVisible=false;Assert.IsNull(portrait.GetPortrait(doc,f.Presentation).Texture);Assert.True(f.Catalog.TryResolve(f.Zone,10,10,out var hidden));Assert.IsNull(hidden.ForegroundTile);
            }
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void RealElapsedGrowthAndFiniteHarvestKeepTheAuthoredLifecycle(int index)
        {
            using(var globals=new DensityLootTestScope())
            {
                var previousFactory=CropSystem.Factory;var previousZone=SettlementRuntime.ActiveZone;var previousTurns=TurnManager.Active;
                var activeTurns=typeof(TurnManager).GetProperty("Active",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
                try
                {
                    activeTurns.SetValue(null,null); // This fixture advances its explicit detached crop clock.
                    CropSystem.Factory=factory;var zone=new Zone(ReferenceGladePlan.ZoneID);SettlementRuntime.ActiveZone=zone;
                    var actor=factory.CreateEntity("Player");var owner=Owner(index,0);var crop=owner.GetPart<CropPart>();crop.TicksPerStage=1;crop.MoistureTicks=4;
                    Assert.True(zone.AddEntity(actor,9,10));Assert.True(zone.AddEntity(owner,10,10));zone.GetCell(10,10).IsVisible=zone.GetCell(10,10).Explored=true;
                    world=NewWorld(states,works,plot,keeper);Assert.True(world.TryResolve(zone,10,10,out var planted));Assert.AreSame(states,planted.ForegroundTile.sprite.texture);
                    Assert.True(Reconcile(crop,zone,0));Assert.True(Reconcile(crop,zone,10));Assert.AreEqual(1,crop.GrowthStage);
                    Assert.True(world.TryResolve(zone,10,10,out var sprout));Assert.AreSame(plot,sprout.ForegroundTile.sprite.texture);
                    Assert.True(Reconcile(crop,zone,20));
                    if(index>=2)
                    {
                        Assert.AreEqual(2,crop.GrowthStage);Assert.AreSame(zone.GetCell(10,10),zone.GetEntityCell(owner));
                        Assert.True(world.TryResolve(zone,10,10,out var ripe));Assert.AreEqual(new Rect((index==2?3:5)*16,0,16,24),ripe.ForegroundTile.sprite.rect);
                        Assert.True(InventorySystem.PerformAction(actor,owner,"HarvestCultivatedCrop",zone));
                        Assert.False(InventorySystem.PerformAction(actor,owner,"HarvestCultivatedCrop",zone));
                        Assert.AreEqual(1,zone.GetReadOnlyEntities().Count(e=>e.BlueprintName==Seeds[index]));
                    }
                    Assert.IsNull(zone.GetEntityCell(owner));Assert.AreEqual(2,zone.GetReadOnlyEntities().Count(e=>e.BlueprintName==Produce[index]));
                    Assert.True(world.TryResolve(zone,10,10,out var harvested));Assert.AreNotSame(owner,harvested.Owner);Assert.AreNotSame(plot,harvested.ForegroundTile?.sprite.texture);
                }
                finally{activeTurns.SetValue(null,previousTurns);CropSystem.Factory=previousFactory;SettlementRuntime.ActiveZone=previousZone;}
            }
        }
        [Test] public void DefaultPlotResourceBindsAllSixGrowthFramesNeutralTrayAndGroundCord()
        {
            var actual=Resources.Load<Texture2D>(PlotResource);AssertImported(actual,128);art=new FlatPixelOpeningArtCatalog();world=new FlatPixelGreyboxCatalog();
            var frames=new[]{(0,1,0),(1,1,1),(2,1,2),(2,2,3),(3,1,4),(3,2,5),(4,0,6)};
            foreach(var item in frames)
            {
                var owner=Owner(item.Item1,item.Item2);Frame(owner,actual,item.Item3);
                Assert.True(world.TryGetSubjectSprite(owner,10,10,out var sprite,out var tint));Assert.AreSame(actual,sprite.texture);Assert.AreEqual(new Rect(item.Item3*16,0,16,24),sprite.rect);Assert.AreEqual(Color.white,tint);
            }
            Assert.True(Cord(art,out var cord));Assert.AreSame(actual,cord.sprite.texture);Assert.AreEqual(new Rect(112,0,16,24),cord.sprite.rect);
        }
        [Test] public void DefaultKeeperResourceIsByteIdenticalAndBindsAuthorizedWorldPortrait()
        {
            var actual=Resources.Load<Texture2D>(KeeperResource);AssertImported(actual,16);
            CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(Application.dataPath,"Art/SeedKeeper3/seed-keeper-3.png")),
                File.ReadAllBytes(Path.Combine(Application.dataPath,"Resources/Sprites/Live16x24/seed-keeper-3.png")),"Approved native source must be copied byte-for-byte, including row23.");
            art=new FlatPixelOpeningArtCatalog();Frame(Owner(5),actual,0);
            using(var f=new PortraitFixture(new FlatPixelGreyboxCatalog(),factory))
            using(var portrait=PortraitProbe())
            {
                var owner=Owner(5);Assert.True(f.Zone.AddEntity(owner,10,10));var doc=ExamineDocumentBuilder.BuildWorld(owner,f.Zone,f.Zone.GetCell(10,10),f.Viewer);
                var image=portrait.GetPortrait(doc,f.Presentation);Assert.AreSame(actual,image.Texture);Assert.AreEqual(new Rect(0,0,1,1),image.Uv);Assert.IsNull(portrait.CurrentModelId);
            }
        }

        Entity Owner(int index,int stage=0){var owner=factory.CreateEntity(Names[index]);Assert.NotNull(owner);if(index<4)owner.GetPart<CropPart>().GrowthStage=stage;return owner;}
        void Create()=>art=NewArt(states,works,plot,keeper);
        Tile Frame(Entity owner,Texture2D texture,int frame)
        {Assert.True(art.TryResolve(owner,0,0,out var tile),owner.BlueprintName);Assert.AreSame(texture,tile.sprite.texture);Assert.AreEqual(new Rect(frame*16,0,16,24),tile.sprite.rect);return tile;}
        static Type[] Signature(int count)=>Enumerable.Repeat(typeof(Texture2D),count).ToArray();
        static object[] Arguments(Texture2D states,Texture2D works,Texture2D plot,Texture2D keeper,int count)
            =>new object[]{null,null,null,states,null,null,null,works,plot,keeper}.Take(count).ToArray();
        static FlatPixelOpeningArtCatalog NewArt(Texture2D states,Texture2D works,Texture2D plot,Texture2D keeper,int count=10)
        {
            var ctor=typeof(FlatPixelOpeningArtCatalog).GetConstructor(Signature(count));Assert.NotNull(ctor,"Independent ninth plot and tenth keeper atlas APIs are required.");
            return (FlatPixelOpeningArtCatalog)ctor.Invoke(Arguments(states,works,plot,keeper,count));
        }
        static FlatPixelGreyboxCatalog NewWorld(Texture2D states,Texture2D works,Texture2D plot,Texture2D keeper)
        {
            var ctor=typeof(FlatPixelGreyboxCatalog).GetConstructor(Signature(10));Assert.NotNull(ctor,"World selector must forward plot and approved keeper resources.");
            return (FlatPixelGreyboxCatalog)ctor.Invoke(Arguments(states,works,plot,keeper,10));
        }
        static bool Cord(FlatPixelOpeningArtCatalog catalog,out Tile tile)
        {
            var method=typeof(FlatPixelOpeningArtCatalog).GetMethod("TryGetReserveCordTile");Assert.NotNull(method,"Ground-only cached cord access is required.");
            var args=new object[]{null};bool result=(bool)method.Invoke(catalog,args);tile=(Tile)args[0];return result;
        }
        static bool Reconcile(CropPart crop,Zone zone,int tick)
        {
            var type=typeof(CropPart).Assembly.GetType("CavesOfOoo.Core.CropTime");
            return (bool)type.GetMethod("Reconcile",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{crop,zone,tick});
        }
        static void AssertImported(Texture2D actual,int width)
        {Assert.NotNull(actual,"Real missing-resource RED is separate from synthetic selectors.");Assert.AreEqual(width,actual.width);Assert.AreEqual(24,actual.height);Assert.AreEqual(FilterMode.Point,actual.filterMode);Assert.AreEqual(TextureWrapMode.Clamp,actual.wrapMode);Assert.AreEqual(1,actual.mipmapCount);}
        static ExaminePortraitRenderer PortraitProbe()=> (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer),I,null,new object[]{(Action<Camera,RenderTexture>)((camera,target)=>{})},null);
        sealed class PortraitFixture:IDisposable
        {
            readonly GameObject root;readonly Action<int,int,string> oldCell=ZoneRenderHooks.CellDirtyCallback;readonly Action<string> oldFull=ZoneRenderHooks.FullDirtyCallback;
            public readonly Zone Zone=new Zone(ReferenceGladePlan.ZoneID);public readonly Entity Viewer;public readonly ZoneRenderer Presentation;public readonly FlatPixelGreyboxCatalog Catalog;
            public PortraitFixture(FlatPixelGreyboxCatalog catalog,EntityFactory factory)
            {
                Catalog=catalog;
                try
                {
                    ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;
                    Viewer=factory.CreateEntity("Player");Assert.True(Zone.AddEntity(Viewer,9,10));
                    for(int y=9;y<=11;y++)for(int x=9;x<=11;x++)Zone.GetCell(x,y).IsVisible=Zone.GetCell(x,y).Explored=true;
                    root=new GameObject("Plot exact-subject fixture");root.SetActive(false);Presentation=root.AddComponent<ZoneRenderer>();var env=root.AddComponent<EnvironmentSpriteRenderer>();
                    typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox",I).SetValue(env,Catalog);env.FlatPixelGreyboxEnabled=true;
                    typeof(ZoneRenderer).GetField("_envSpriteRenderer",I).SetValue(Presentation,env);typeof(ZoneRenderer).GetField("_flatPixelModeEnabled",I).SetValue(Presentation,true);
                    typeof(ZoneRenderer).GetProperty("CurrentZone",I).SetValue(Presentation,Zone);Assert.True(Presentation.FlatPixelModeActive);
                }
                catch{Dispose();throw;}
            }
            public void Dispose(){try{if(root!=null)Object.DestroyImmediate(root);Catalog?.Dispose();}finally{ZoneRenderHooks.CellDirtyCallback=oldCell;ZoneRenderHooks.FullDirtyCallback=oldFull;}}
        }
    }
}
