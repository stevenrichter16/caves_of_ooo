// Contract-first ordinary depth1/2 scenery. Root runs native RED before production.
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
    public sealed class FlatPixelShallowCavesTests
    {
        const string Home = "Overworld.0.9.1", Floor = "SandstoneFloor", Wall = "SandstoneWall";
        const int X = 10, Y = 10;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[] Owners = { Wall, Floor, "Rubble", "Stalagmite", "StairsDown", "StairsUp" };
        static readonly string[] Paths = { "Sprites/Live16x24/shallow-caves", "Sprites/Live16x24/beating-materials",
            "Sprites/Environment/rubble", "Sprites/Environment/stalagmite", "Sprites/Live16x24/opening-anchors", "Sprites/Live16x24/opening-cellar" };
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        FlatPixelFallbackCatalog selector;
        IDisposable injected;
        readonly List<Texture2D> textures = new List<Texture2D>();
        Dictionary<string,LiquidDefinition> liquids, oldLiquids;
        bool oldLiquidInitialized;
        Action<string> oldMessage;
        Action<WorldAudioUtilityReceipt> oldUtility;
        Action<WorldAudioWorkReceipt> oldWork;
        Action<WorldAudioCombatReceipt> oldCombat;
        Action<WorldAudioCue,Entity,Zone,int,int> oldAudio;
        EntityDamageVisualHandler oldDamage;
        EntityMovedVisualHandler oldMoved;
        EntityFactory oldDestructionFactory;
        Action<int,int,string> oldCellDirty;
        Action<string> oldFullDirty;

        [SetUp] public void Setup()
        {
            oldMessage=MessageLog.OnMessage; oldUtility=WorldAudioUtility.Callback; oldWork=WorldAudioWork.Callback;
            oldCombat=WorldAudioCombat.Callback; oldAudio=WorldAudioHooks.Callback;
            oldDamage=EntityVisualHooks.DamageCallback; oldMoved=EntityVisualHooks.MovedCallback;
            oldDestructionFactory=DestructionSystem.EntityFactoryRef;
            oldCellDirty=ZoneRenderHooks.CellDirtyCallback; oldFullDirty=ZoneRenderHooks.FullDirtyCallback;
            ZoneRenderHooks.CellDirtyCallback=null; ZoneRenderHooks.FullDirtyCallback=null;
            MessageLog.OnMessage=null; WorldAudioUtility.Callback=null; WorldAudioWork.Callback=null;
            WorldAudioCombat.Callback=null; WorldAudioHooks.Callback=null; EntityVisualHooks.DamageCallback=null; EntityVisualHooks.MovedCallback=null;
            liquids=(Dictionary<string,LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId",S).GetValue(null);
            oldLiquids=new Dictionary<string,LiquidDefinition>(liquids); oldLiquidInitialized=LiquidRegistry.IsInitialized;
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t=>t.text));
            content=new DensityLootTestScope(); DestructionSystem.EntityFactoryRef=content.Factory;
            manager=OverworldZoneManager.CreateDetached(content.Factory,65,true); Assert.IsNull(manager.WorldMap.GetPOI(0,9));
            zone=Attach(Home); world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try
            {
                world?.Dispose(); injected?.Dispose(); selector?.Dispose();
                foreach(var t in textures)if(t!=null)Object.DestroyImmediate(t); textures.Clear(); content?.Dispose();
            }
            finally
            {
                MessageLog.OnMessage=oldMessage; WorldAudioUtility.Callback=oldUtility; WorldAudioWork.Callback=oldWork;
                WorldAudioCombat.Callback=oldCombat; WorldAudioHooks.Callback=oldAudio; EntityVisualHooks.DamageCallback=oldDamage; EntityVisualHooks.MovedCallback=oldMoved;
                DestructionSystem.EntityFactoryRef=oldDestructionFactory;
                ZoneRenderHooks.CellDirtyCallback=oldCellDirty; ZoneRenderHooks.FullDirtyCallback=oldFullDirty;
                if(liquids!=null&&oldLiquids!=null){liquids.Clear();foreach(var pair in oldLiquids)liquids[pair.Key]=pair.Value;typeof(LiquidRegistry).GetField("_initialized",S).SetValue(null,oldLiquidInitialized);}
            }
        }
        Zone Attach(string id)
        {var graph=new Zone(id);manager.SetActiveZone(graph);Reveal(graph);return graph;}
        static void Reveal(Zone graph){foreach(var c in graph.Cells)c.IsVisible=c.Explored=true;}
        Entity Add(string name,int x=X,int y=Y)
        {var e=content.Factory.CreateEntity(name);Assert.NotNull(e,name);Assert.True(zone.AddEntity(e,x,y));return e;}
        FlatPixelGreyboxCell View(int x=X,int y=Y)
        {Assert.True(world.TryResolvePresentation(zone,x,y,out var v));return v;}
        static Type ArtType()
        {var t=typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelShallowCavesCatalog");Assert.NotNull(t,"Missing ordinary shallow-cave adapter.");return t;}
        static object Read(object owner,string field){var f=owner.GetType().GetField(field,I);Assert.NotNull(f,field);return f.GetValue(owner);}
        static void Write(object owner,string field,object value){var f=owner.GetType().GetField(field,I);Assert.NotNull(f,field);f.SetValue(owner,value);}
        static bool Call(object target,string method,object[] args)
        {var m=target.GetType().GetMethod(method,I);Assert.NotNull(m,method);return (bool)m.Invoke(target,args);}
        object Art()
        {ArtType();var a=Read(world,"_shallowCavesArt");if(a==null){View();a=Read(world,"_shallowCavesArt");}Assert.NotNull(a);return a;}
        bool Supported(Zone z)
        {var m=ArtType().GetMethod("IsSupportedZone",S);Assert.NotNull(m);return (bool)m.Invoke(null,new object[]{z});}
        bool Ground(out Tile tile,out Entity owner,int x=X,int y=Y)
        {object[] args={zone,x,y,null,null};bool ok=Call(Art(),"TryResolveGround",args);tile=(Tile)args[3];owner=(Entity)args[4];return ok;}
        bool Subject(Entity e,int x=X,int y=Y)
        {object[] args={zone,e,x,y,null};return Call(Art(),"TryGetSubjectSprite",args);}
        static Texture2D Resource(string name)
        {int i=Array.IndexOf(Owners,name);Assert.GreaterOrEqual(i,0);var t=Resources.Load<Texture2D>(Paths[i]);Assert.NotNull(t,Paths[i]);return t;}
        static void TileGeometry(Tile tile,Texture2D texture,string name)
        {
            Assert.NotNull(tile);Assert.NotNull(tile.sprite);Assert.AreSame(texture,tile.sprite.texture);
            int h=name=="Rubble"||name=="Stalagmite"?16:24;
            Assert.AreEqual(new Vector2(16,h),tile.sprite.rect.size);Assert.AreEqual(new Vector2(8,h/2f),tile.sprite.pivot);
            Assert.AreEqual(16,tile.sprite.pixelsPerUnit);Assert.AreEqual(new Vector2(1,h/16f),(Vector2)tile.sprite.bounds.size);
            Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);Assert.AreEqual(TileFlags.None,tile.flags);
            int frame=(int)tile.sprite.rect.x/16;
            if(name==Wall)Assert.That(frame,Is.InRange(0,1));
            else if(name==Floor)Assert.That(frame,Is.InRange(12,15));
            else Assert.AreEqual(name=="StairsDown"?4:0,frame);
        }
        Tile Native(Entity owner,int x=X,int y=Y)
        {
            Assert.True(Supported(zone));var v=View(x,y);Assert.AreSame(owner,v.Owner);
            var tile=owner.BlueprintName==Floor?v.GroundTile:v.ForegroundTile;
            TileGeometry(tile,Resource(owner.BlueprintName),owner.BlueprintName);
            if(owner.BlueprintName==Floor){Assert.True(v.LocalGround);Assert.AreEqual(Color.white,v.GroundColor);Assert.IsNull(v.ForegroundTile);}
            else{Assert.True(v.NativeForeground);Assert.AreEqual(Color.white,v.ForegroundColor);}
            Assert.AreEqual(zone.GetCell(x,y).BlocksMovement(),v.BlocksMovement);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,x,y,out var sprite,out var tint));
            Assert.AreSame(tile.sprite.texture,sprite.texture);Assert.AreEqual(tile.sprite.rect,sprite.rect);Assert.AreEqual(Color.white,tint);
            return tile;
        }
        void Fallback(int x=X,int y=Y)
        {Assert.True(world.TryResolveFallback(zone,x,y,out var f));var v=View(x,y);Assert.AreSame(f.Owner,v.Owner);Assert.False(v.NativeForeground);Assert.AreSame(f.ForegroundTile,v.ForegroundTile);}
        static bool HasLedger(OverworldZoneManager m)
        {return ((System.Runtime.CompilerServices.ConditionalWeakTable<OverworldZoneManager,LairStackLedgerPart>)typeof(LairStacks).GetField("Ledgers",S).GetValue(null)).TryGetValue(m,out _);}
        Texture2D[] MakeTextures()
        {
            int[] widths={32,544,16,16,192,144},heights={24,24,16,16,24,24};var a=new Texture2D[6];
            for(int i=0;i<a.Length;i++){a[i]=new Texture2D(widths[i],heights[i]);textures.Add(a[i]);}return a;
        }
        void Inject(Texture2D[] inputs)
        {
            world.Dispose();injected?.Dispose();selector?.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);selector=new FlatPixelFallbackCatalog();
            var signature=new[]{typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(FlatPixelFallbackCatalog)};
            var ctor=ArtType().GetConstructor(signature);Assert.NotNull(ctor);
            injected=(IDisposable)ctor.Invoke(inputs.Cast<object>().Concat(new object[]{selector}).ToArray());Write(world,"_shallowCavesArt",injected);
        }

        [TestCase(Floor)] [TestCase(Wall)] [TestCase("Rubble")] [TestCase("Stalagmite")] [TestCase("StairsDown")] [TestCase("StairsUp")]
        public void SixExactOwnersSubmitNativeGeometryAndAuthorizedReaderWithoutChangingState(string name)
        {
            var e=Add(name);var observer=Add("Player",X-1,Y);int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();var tile=Native(e);
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone),"No opening admission expansion.");
            using(var paint=new Paint(world,zone))
            {
                paint.Step();Assert.AreSame(tile,name==Floor?paint.GroundAt(X,Y):paint.At(X,Y));
                var document=ExamineDocumentBuilder.BuildWorld(e,zone,zone.GetCell(X,Y),observer);Assert.NotNull(document);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                Assert.True(paint.Renderer.TryGetFlatExaminePortrait(document,out var portrait));Assert.AreSame(tile.sprite.texture,portrait.Texture);
                Assert.AreEqual(new Rect(tile.sprite.rect.x/tile.sprite.texture.width,0,16f/tile.sprite.texture.width,1),portrait.Uv);Assert.AreEqual(Color.white,portrait.Tint);Assert.False(portrait.IsModel);
                Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
                e.GetPart<RenderPart>().VisualVariant="changed meaning";Assert.False(Subject(e));Fallback();if(name==Floor)Assert.False(View().LocalGround);
            }
        }
        [TestCase(1)] [TestCase(2)]
        public void RealNaturalDepthGraphsSupplyActualSandstoneAndKeepDeeperBoundaryDistinct(int depth)
        {
            manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);zone=manager.GetZone(WorldMap.ToZoneID(0,9,depth));manager.SetActiveZone(zone);Reveal(zone);Assert.True(Supported(zone));
            foreach(string name in new[]{Floor,Wall})
            {
                var e=zone.GetReadOnlyEntities().FirstOrDefault(v=>v.BlueprintName==name&&zone.GetEntityCell(v) is Cell c&&world.TryResolveFallback(zone,c.X,c.Y,out var f)&&ReferenceEquals(f.Owner,v));
                Assert.NotNull(e,name);var at=zone.GetEntityCell(e);Native(e,at.X,at.Y);
            }
            var deep=Attach("Overworld.0.9.3");Assert.False(Supported(deep));
        }
        [Test] public void CurrentPoiAndMapShapeMutationsRevokeWarmScopeWithoutGenerating()
        {
            var e=Add(Wall);Native(e);var map=manager.WorldMap;var tiles=map.Tiles;var pois=map.POIs;int count=manager.CachedZoneCount;
            try
            {
                pois[0,9]=new PointOfInterest(POIType.MerchantCamp,"current authored column");Assert.False(Supported(zone));Fallback();Assert.False(Subject(e));pois[0,9]=null;Native(e);
                map.Tiles=new BiomeType[1,1];Assert.False(Supported(zone));map.Tiles=tiles;Native(e);
                map.POIs=new PointOfInterest[1,1];Assert.False(Supported(zone));map.POIs=pois;Native(e);
            }
            finally{map.Tiles=tiles;map.POIs=pois;pois[0,9]=null;}
            Assert.AreEqual(count,manager.CachedZoneCount);
        }
        [TestCase("Overworld.0.9.0")] [TestCase("Overworld.0.9.3")] [TestCase("Overworld.00.9.1")]
        [TestCase("Overworld.11.10.1")] [TestCase("Overworld.3.7.1")] [TestCase("Overworld.2.7.1")] [TestCase("Overworld.3.3.1")]
        public void MaterialNamesCannotAdmitDepthAliasesOrReservedColumnsEvenAfterPoiRemoval(string id)
        {
            Native(Add(Wall));var p=WorldMap.FromZoneID(id);manager.WorldMap.POIs[p.x,p.y]=null;zone=Attach(id);var e=Add(Wall);Assert.False(Supported(zone));Assert.False(Subject(e));
        }
        [Test] public void SurfaceKeyedSavedLairOutlivesMapLabelAndOrdinaryQueryCreatesNoLedger()
        {
            Native(Add(Wall));Assert.False(HasLedger(manager));int count=manager.CachedZoneCount;for(int n=0;n<3;n++)Assert.True(Supported(zone));Assert.False(HasLedger(manager));Assert.AreEqual(count,manager.CachedZoneCount);
            manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);manager.WorldMap.SetPOI(0,9,new PointOfInterest(POIType.Lair,"retained lair",null,1,"DesertProwler"));
            LairStacks.Restore(manager,null);Assert.NotNull(LairStacks.Inspect(manager,"Overworld.0.9.0"));manager.WorldMap.SetPOI(0,9,null);zone=Attach(Home);Add(Floor);Assert.False(Supported(zone));Assert.False(View().LocalGround);
        }
        [Test] public void ExactCachedGraphAuthorityRejectsDetachedReplacedAndStaleOwners()
        {
            var e=Add(Wall);Native(e);var old=zone;zone=new Zone(Home);Reveal(zone);Assert.IsNull(WorldLocationContext.For(zone));Assert.False(Supported(zone));
            zone=Attach(Home);Assert.False(Supported(old));Assert.False(world.TryResolvePresentation(old,X,Y,out _));
            manager.SetActiveZone(old);zone=old;Native(e);Assert.True(zone.RemoveEntity(e));Assert.False(Subject(e));
        }
        [TestCase("hidden")] [TestCase("remembered")] [TestCase("unexplored")]
        public void GroundAndBodyDoNotRevealUndisclosedOwners(string fault)
        {
            var floor=Add(Floor);var e=Add(Wall);Native(e);Assert.True(Ground(out _,out _));
            if(fault=="hidden"){e.GetPart<RenderPart>().Visible=false;floor.GetPart<RenderPart>().Visible=false;}
            else{zone.GetCell(X,Y).IsVisible=false;zone.GetCell(X,Y).Explored=fault=="remembered";}
            Assert.False(Subject(e));Assert.False(Ground(out _,out _));
            if(fault=="unexplored")Assert.False(world.TryResolvePresentation(zone,X,Y,out _));else Assert.False(View().NativeForeground);
        }
        [Test] public void SelectedOwnerAndCurrentFootprintAreRequiredButUnderlyingFloorIsIndependent()
        {
            var floor=Add(Floor);var e=Add("StairsDown");e.AddPart(new SpatialFootprintPart{CellsRaw="0,0"});Native(e);
            var player=Add("Player");Assert.AreSame(player,View().Owner);Assert.False(Subject(e));Assert.True(Ground(out _,out var actual));Assert.AreSame(floor,actual);
            Assert.True(zone.MoveEntity(player,X-2,Y));Native(e);Assert.True(zone.TryChangeFootprint(e,"0,0;1,0"));Assert.False(Subject(e));Fallback();
            Assert.True(zone.TryChangeFootprint(e,"0,0"));Native(e);e.GetPart<SpatialFootprintPart>().CellsRaw="0,0;0,1";Assert.False(Subject(e));
            e.GetPart<SpatialFootprintPart>().CellsRaw="0,0";Native(e);Assert.True(zone.RemoveEntity(e));Assert.False(Subject(e));Native(floor);
        }
        [TestCase("foreign-render")] [TestCase("foreign-physics")] [TestCase("duplicate-render")]
        [TestCase("carried")] [TestCase("equipped")] [TestCase("takeable")]
        public void MalformedOrCustodiedOwnersCannotBorrowWallIdentity(string fault)
        {
            var e=Add(Wall);Native(e);var stranger=new Entity();
            switch(fault)
            {
                case "foreign-render":e.GetPart<RenderPart>().ParentEntity=stranger;break;
                case "foreign-physics":e.GetPart<PhysicsPart>().ParentEntity=stranger;break;
                case "duplicate-render":e.AddPart(new RenderPart{RenderString="#",RenderLayer=0});break;
                case "carried":e.GetPart<PhysicsPart>().InInventory=stranger;break;
                case "equipped":e.GetPart<PhysicsPart>().Equipped=stranger;break;
                case "takeable":e.GetPart<PhysicsPart>().Takeable=true;break;
            }
            Assert.False(Subject(e));Assert.False(View().NativeForeground);
        }
        [Test] public void ChangedGameplayMaterialMarkerSolidityAndThermalMeaningUseTruthfulFallback()
        {
            foreach(string fault in new[]{"wall-material","floor-material","thermal","stair-marker","rubble-solid","indestructible"})
            {
                zone=Attach(Home);string name=fault=="floor-material"?Floor:fault=="stair-marker"?"StairsUp":fault=="rubble-solid"?"Rubble":Wall;var e=Add(name);Native(e);
                if(fault=="wall-material")e.GetPart<MaterialPart>().MaterialID="Wood";
                if(fault=="floor-material")e.AddPart(new MaterialPart{MaterialID="Wood"});
                if(fault=="thermal")e.GetPart<ThermalPart>().Temperature=float.NaN;
                if(fault=="stair-marker")e.GetPart<StairsUpPart>().ParentEntity=new Entity();
                if(fault=="rubble-solid")e.GetPart<PhysicsPart>().Solid=true;
                if(fault=="indestructible")e.GetPart<DestructiblePart>().Indestructible=true;
                Assert.False(Subject(e));Fallback();if(name==Floor)Assert.False(View().LocalGround);
            }
        }
        [Test] public void ActualFloorRemainsUnderDryStairsRubbleAndCoveringActorInSubmittedPaint()
        {
            var floor=Add(Floor);var down=Add("StairsDown");var rubble=Add("Rubble",X+1,Y);Add(Floor,X+1,Y);
            // Same-layer selection remains literal: put Rubble last over its real floor.
            Assert.True(zone.RemoveEntity(rubble));Assert.True(zone.AddEntity(rubble,X+1,Y));
            var actor=Add("Player",X+2,Y);Add(Floor,X+2,Y);Native(down);Native(rubble,X+1,Y);
            using(var paint=new Paint(world,zone))
            {
                paint.Step();for(int i=0;i<3;i++)TileGeometry(paint.GroundAt(X+i,Y),Resource(Floor),Floor);
                TileGeometry(paint.At(X,Y),Resource("StairsDown"),"StairsDown");TileGeometry(paint.At(X+1,Y),Resource("Rubble"),"Rubble");Assert.AreSame(actor,View(X+2,Y).Owner);
                Assert.True(zone.RemoveEntity(floor));paint.Dirty(X,Y);paint.Step();Assert.False(Ground(out _,out _));Assert.False(View().LocalGround);Native(down);
            }
        }
        [Test] public void OwnerlessAndRubbleOnlyCellsNeverAcquireAnInventedSandstoneFloor()
        {
            Native(Add(Floor,X-1,Y),X-1,Y);Assert.False(Ground(out _,out _));Assert.False(View().LocalGround);Assert.IsNull(View().Owner);
            var rubble=Add("Rubble");Native(rubble);Assert.False(Ground(out _,out _));Assert.False(View().LocalGround);
            var floor=Add(Floor);Assert.True(Ground(out _,out var actual));Assert.AreSame(floor,actual);Assert.True(zone.RemoveEntity(floor));Assert.False(Ground(out _,out _));Native(rubble);
        }
        [TestCase("water")] [TestCase("heat")] [TestCase("charge")] [TestCase("cultivation")] [TestCase("competing-floor")]
        public void ActualHazardsCultivationAndCompetingMaterialVetoPlainGroundWithoutHidingCover(string fault)
        {
            var floor=Add(Floor);var stair=Add("StairsDown");Native(stair);Assert.True(Ground(out _,out _));
            if(fault=="water")zone.TileState.WriteCoating(X,Y,"water",ZoneTileState.Permanent);
            if(fault=="heat")zone.TileState.AddHeat(X,Y,1);
            if(fault=="charge")zone.TileState.AddCharge(X,Y,1);
            if(fault=="cultivation")floor.AddPart(new CultivatedSoilPart());
            if(fault=="competing-floor"){Add("StoneFloor");Assert.True(zone.RemoveEntity(stair));Assert.True(zone.AddEntity(stair,X,Y));}
            string state=zone.TileState.ToSaveString();Assert.False(Ground(out _,out _));Assert.False(View().LocalGround);Native(stair);Assert.AreEqual(state,zone.TileState.ToSaveString());
        }
        [Test] public void HiddenForeignAndCarriedFloorCompetitorDoesNotSuppressVisibleGround()
        {
            var floor=Add(Floor);Native(floor);var other=Add("StoneFloor");other.GetPart<RenderPart>().Visible=false;Assert.True(Ground(out _,out var actual));Assert.AreSame(floor,actual);
            other.GetPart<RenderPart>().Visible=true;Assert.False(Ground(out _,out _));other.GetPart<RenderPart>().ParentEntity=new Entity();Assert.True(Ground(out _,out _));
            other.GetPart<RenderPart>().ParentEntity=other;other.GetPart<PhysicsPart>().InInventory=new Entity();Assert.True(Ground(out _,out _));other.GetPart<PhysicsPart>().InInventory=null;Assert.False(Ground(out _,out _));
        }
        [Test] public void StructuralHealthIsOwnedFiniteSurvivalNotMissingCreatureHitpoints()
        {
            var wall=Add(Wall);Assert.IsNull(wall.GetStat("Hitpoints"));var structural=wall.GetPart<DestructiblePart>();Assert.AreEqual(30,structural.HP);Assert.AreEqual(30,structural.MaxHP);Assert.AreEqual(2,structural.Hardness);
            foreach(int hp in new[]{30,17,1}){structural.HP=hp;Native(wall);}
            foreach(int hp in new[]{0,-1,31}){structural.HP=hp;Assert.False(Subject(wall));Fallback();}
            structural.HP=30;structural.ParentEntity=new Entity();Assert.False(Subject(wall));structural.ParentEntity=wall;Native(wall);
        }
        [Test] public void RealDamageThenDestructionSubmitsRubbleAndPreservesIndependentHazardAndCreatureState()
        {
            var floor=Add(Floor);var wall=Add(Wall);var actor=Add("Player",X-1,Y);int hp=actor.GetStat("Hitpoints").Value;
            var structural=wall.GetPart<DestructiblePart>();Native(wall);Assert.True(zone.GetCell(X,Y).BlocksMovement());
            using(var paint=new Paint(world,zone))
            {
                paint.Step();TileGeometry(paint.At(X,Y),Resource(Wall),Wall);long full=EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.AreEqual(DestroyVerdict.Damaged,DestructionSystem.Damage(wall,1,actor,zone));Assert.AreEqual(29,structural.HP);paint.Dirty(X,Y);paint.Step();Native(wall);
                zone.TileState.WriteCoating(X,Y,"water",ZoneTileState.Permanent);string state=zone.TileState.ToSaveString();paint.Step();Assert.NotNull(paint.Mark);
                Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(wall,999,actor,zone));paint.Step();Assert.True(structural.Gone);Assert.IsNull(zone.GetEntityCell(wall));Assert.False(Subject(wall));
                var rubble=zone.GetCell(X,Y).Objects.Single(e=>e.BlueprintName=="Rubble");Native(rubble);TileGeometry(paint.At(X,Y),Resource("Rubble"),"Rubble");Assert.False(zone.GetCell(X,Y).BlocksMovement());
                Assert.AreEqual(state,zone.TileState.ToSaveString());Assert.NotNull(paint.Mark);Assert.AreEqual(hp,actor.GetStat("Hitpoints").Value);Assert.False(zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="CreatureCorpse"));
                Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);Assert.AreEqual(DestroyVerdict.NotBreakable,DestructionSystem.Damage(rubble,999,actor,zone));
                Assert.AreSame(floor,zone.GetCell(X,Y).Objects.Single(e=>e.BlueprintName==Floor));
            }
        }
        [Test] public void RealVerticalRoundTripUsesCurrentPhysicalCounterpartsAndKeepsBodyIdentity()
        {
            var upper=zone;var down=Add("StairsDown");Native(down);var actor=Add("Player");int hp=actor.GetStat("Hitpoints").Value;
            zone=Attach("Overworld.0.9.2");var lower=zone;var up=Add("StairsUp",X+3,Y+1);Native(up,X+3,Y+1);
            manager.SetActiveZone(upper);var result=ZoneTransitionSystem.TransitionPlayerVertical(actor,upper,true,X,Y,manager);
            Assert.True(result.Success,result.ErrorReason);Assert.AreSame(lower,result.NewZone);Assert.AreEqual((X+3,Y+1),lower.GetEntityPosition(actor));Assert.IsNull(upper.GetEntityCell(actor));
            var back=ZoneTransitionSystem.TransitionPlayerVertical(actor,lower,false,X+3,Y+1,manager);Assert.True(back.Success,back.ErrorReason);Assert.AreSame(upper,back.NewZone);Assert.AreEqual((X,Y),upper.GetEntityPosition(actor));
            Assert.True(upper.MoveEntity(actor,X-1,Y));zone=upper;Native(down);zone=lower;Native(up,X+3,Y+1);Assert.AreEqual(hp,actor.GetStat("Hitpoints").Value);
        }
        [Test] public void RemovedReturnEndpointRefusesTransferWithoutInventingOrRepairingAStair()
        {
            var upper=zone;var down=Add("StairsDown");Native(down);var actor=Add("Player");zone=Attach("Overworld.0.9.2");var lower=zone;var up=Add("StairsUp");Native(up);
            Assert.True(lower.RemoveEntity(up));Assert.False(Subject(up));int count=manager.CachedZoneCount;manager.SetActiveZone(upper);
            var result=ZoneTransitionSystem.TransitionPlayerVertical(actor,upper,true,X,Y,manager);Assert.False(result.Success);StringAssert.Contains("return stairs",result.ErrorReason);
            Assert.AreSame(upper.GetCell(X,Y),upper.GetEntityCell(actor));Assert.IsNull(lower.GetEntityCell(actor));Assert.AreEqual(count,manager.CachedZoneCount);Assert.False(lower.GetReadOnlyEntities().Any(e=>e.HasPart<StairsUpPart>()));
        }
        [Test] public void OptionalTextureGroupsFailIndependentlyAndExplicitInjectionNeverReloadsResources()
        {
            var inputs=MakeTextures();for(int i=0;i<Owners.Length;i++)Add(Owners[i],X+i,Y);
            for(int missing=0;missing<6;missing++)for(int malformed=0;malformed<2;malformed++)
            {
                var set=(Texture2D[])inputs.Clone();if(malformed==0)set[missing]=null;else{set[missing]=new Texture2D(3,3);textures.Add(set[missing]);}Inject(set);
                for(int i=0;i<6;i++)
                {
                    var v=View(X+i,Y);var tile=i==1?v.GroundTile:v.ForegroundTile;
                    if(i==missing){Assert.False(v.LocalGround&&i==1);Assert.False(v.NativeForeground);Assert.False(Subject(zone.GetCell(X+i,Y).Objects.Single(),X+i,Y));}
                    else TileGeometry(tile,inputs[i],Owners[i]);
                }
            }
        }
        [Test] public void DisposingOwnedPairsLeavesAllBorrowedInputsAndOtherGroupsAlive()
        {
            var inputs=MakeTextures();var wall=Add(Wall);Inject(inputs);var tile=View().ForegroundTile;TileGeometry(tile,inputs[0],Wall);var sprite=tile.sprite;
            injected.Dispose();Assert.True(tile==null);Assert.True(sprite==null);Assert.False(Subject(wall));foreach(var t in inputs)Assert.True(t!=null);
        }
        [Test] public void AuthoredFloorGlyphVariantsStayNativeButUnknownVariantMeaningDoesNot()
        {
            var floor=Add(Floor);Assert.AreEqual("...,.'`",floor.GetPart<RenderPart>().GlyphVariants);Native(floor);
            floor.GetPart<RenderPart>().GlyphVariants="xyz";Assert.False(Ground(out _,out _));Assert.False(Subject(floor));Fallback();
            floor.GetPart<RenderPart>().GlyphVariants="...,.'`";Native(floor);
        }

        // A disclosed cell backdrop is presentation, never an invented floor owner.
        bool Backdrop(out Tile tile,out Color tint,int x=X,int y=Y)
        {object[] args={zone,x,y,null,null};bool ok=Call(Art(),"TryResolveCellBackdrop",args);tile=(Tile)args[3];tint=args[4] is Color c?c:default;return ok;}
        static bool IsAmbient(FlatPixelGreyboxCell cell)
        {var field=typeof(FlatPixelGreyboxCell).GetField("AmbientBackdrop",I);Assert.NotNull(field,"Ambient backing must be distinct from LocalGround.");return (bool)field.GetValue(cell);}
        Tile Ambient(Entity owner=null,int x=X,int y=Y)
        {
            Assert.False(Ground(out _,out _,x,y),"Ambient presentation must not invent a floor.");
            Assert.True(Backdrop(out var tile,out var tint,x,y));Assert.NotNull(tile);Assert.NotNull(tile.sprite);
            Assert.AreEqual(new Vector2(16,24),tile.sprite.rect.size);Assert.AreEqual(16,tile.sprite.pixelsPerUnit);
            Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);Assert.AreEqual((Color)new Color32(128,106,76,255),tint);
            var view=View(x,y);Assert.True(IsAmbient(view));Assert.False(view.LocalGround);Assert.AreSame(owner,view.Owner);
            Assert.AreSame(tile,view.GroundTile);Assert.AreEqual(tint,view.GroundColor);Assert.AreEqual(FlatPixelGreyboxGround.Ground,view.GroundKind);
            return tile;
        }
        [TestCase(1)] [TestCase(2)]
        public void AmbientBackdropDisclosesNoOwnerMaterialOrInteractionInAnEligibleEmptyCell(int depth)
        {
            zone=Attach(WorldMap.ToZoneID(0,9,depth));var cell=zone.GetCell(X,Y);int version=zone.EntityVersion;
            string state=zone.TileState.ToSaveString();Assert.IsEmpty(cell.Objects);Assert.False(cell.BlocksMovement());
            var tile=Ambient();Assert.False(View().NativeForeground);Assert.IsNull(View().ForegroundTile);
            Assert.IsEmpty(cell.Objects);Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
            Assert.False(world.TryGetPresentationSubjectSprite(zone,null,X,Y,out _,out _));
            for(int i=0;i<3;i++)Assert.AreSame(tile,Ambient());
        }
        [Test] public void AmbientBackingStaysSubmittedAcrossRubbleActorCoverAndRemovalWithoutCameraOrFullRepaintChanges()
        {
            zone.AmbientLevel=.28f;zone.AmbientTint=Color.white;
            var control=Add("StoneFloor",X+3,Y);control.GetPart<RenderPart>().BackgroundColor="^r";
            var rubble=Add("Rubble");var native=Native(rubble);var backing=Ambient(rubble);
            using(var paint=new Paint(world,zone))
            {
                paint.Step();Assert.AreSame(native,paint.At(X,Y));Assert.AreSame(backing,paint.GroundAt(X,Y));
                Color expected=(Color)new Color32(128,106,76,255);Assert.AreEqual(expected,paint.GroundTintAt(X,Y));
                var light=(LightMap)Read(paint.Renderer,"_lightMap");Assert.NotNull(light);Assert.AreEqual(.28f,light.AmbientLevel);
                Assert.Less(light.ApplyToColor(expected,X,Y).r,expected.r,"The native backdrop must be tested under real attenuation.");
                var controlView=View(X+3,Y);Assert.False(controlView.LocalGround);Assert.False(IsAmbient(controlView));Assert.False(controlView.NativeForeground);
                Color dimControl=light.ApplyToColor(controlView.GroundColor,X+3,Y);Assert.Greater(controlView.GroundColor.r,0);Assert.Less(dimControl.r,controlView.GroundColor.r);
                Assert.AreEqual(dimControl,paint.GroundTintAt(X+3,Y),"The existing nonnative background still receives lighting.");
                var core=typeof(ZoneRenderer).GetMethod("RenderFlatCellCore",I);Assert.NotNull(core);
                core.Invoke(paint.Renderer,new object[]{X,Y,new Vector3Int(X,Zone.Height-1-Y,0),zone.GetCell(X,Y)});
                Assert.AreEqual(expected,paint.GroundTintAt(X,Y),"The main painter must honor ambient backing before the environment pass can overwrite it.");
                core.Invoke(paint.Renderer,new object[]{X+3,Y,new Vector3Int(X+3,Zone.Height-1-Y,0),zone.GetCell(X+3,Y)});
                Assert.AreEqual(dimControl,paint.GroundTintAt(X+3,Y));
                var camera=(Camera)Read(paint.Renderer,"_mainCamera");var position=camera.transform.position;float size=camera.orthographicSize;var rect=camera.rect;
                long full=EnvironmentSpriteRenderer.Perf.FullPasses;
                var actor=Add("Player");Ambient(actor);Assert.False(Subject(rubble));paint.Dirty(X,Y);paint.Step();
                Assert.AreSame(backing,paint.GroundAt(X,Y));Assert.AreEqual((Color)new Color32(128,106,76,255),paint.GroundTintAt(X,Y));
                Assert.True(zone.MoveEntity(actor,X+1,Y));paint.Dirty(X,Y);paint.Dirty(X+1,Y);paint.Step();Native(rubble);Ambient(rubble);Ambient(actor,X+1,Y);
                Assert.True(zone.RemoveEntity(rubble));paint.Dirty(X,Y);paint.Step();Ambient();Assert.IsNull(paint.At(X,Y));Assert.AreSame(backing,paint.GroundAt(X,Y));
                Assert.True(zone.AddEntity(rubble,X,Y));paint.Dirty(X,Y);paint.Step();Native(rubble);Ambient(rubble);Assert.AreSame(native,paint.At(X,Y));
                Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);Assert.AreEqual(position,camera.transform.position);Assert.AreEqual(size,camera.orthographicSize);Assert.AreEqual(rect,camera.rect);
            }
        }
        [Test] public void HiddenForeignAndCustodiedOccupantsDoNotSignalTheirPresenceThroughAmbientBacking()
        {
            var backing=Ambient();var other=Add("StoneFloor");var render=other.GetPart<RenderPart>();var physics=other.GetPart<PhysicsPart>();
            render.Visible=false;Assert.AreSame(backing,Ambient());render.Visible=true;Assert.False(Backdrop(out _,out _));Assert.False(IsAmbient(View()));
            render.ParentEntity=new Entity();Assert.AreSame(backing,Ambient());render.ParentEntity=other;
            physics.ParentEntity=new Entity();Assert.AreSame(backing,Ambient());physics.ParentEntity=other;
            physics.InInventory=new Entity();Assert.AreSame(backing,Ambient());physics.InInventory=null;
            physics.Equipped=new Entity();Assert.AreSame(backing,Ambient());physics.Equipped=null;
            Assert.False(Backdrop(out _,out _));Assert.True(zone.RemoveEntity(other));
            var wall=Add(Wall);wall.GetPart<RenderPart>().Visible=false;Assert.True(zone.GetCell(X,Y).BlocksMovement(),"A hidden solid must not affect backdrop eligibility.");
            Assert.AreSame(backing,Ambient());Assert.True(zone.RemoveEntity(wall));Assert.AreSame(backing,Ambient());
        }
        [Test] public void AmbientBackingCannotRevealUnexploredRememberedDetachedOrReplacedGraphs()
        {
            Ambient();var cell=zone.GetCell(X,Y);cell.IsVisible=false;Assert.False(Backdrop(out _,out _));Assert.False(IsAmbient(View()));
            cell.Explored=false;Assert.False(Backdrop(out _,out _));Assert.False(world.TryResolvePresentation(zone,X,Y,out _));
            cell.IsVisible=cell.Explored=true;Ambient();var original=zone;
            zone=new Zone(Home);Reveal(zone);Assert.False(Backdrop(out _,out _));
            zone=Attach(Home);Ambient();Assert.False(world.TryResolvePresentation(original,X,Y,out _));
            object[] stale={original,X,Y,null,null};Assert.False(Call(Art(),"TryResolveCellBackdrop",stale));
        }
        [Test] public void AmbientBackingHonorsCurrentPoiSavedColumnAndDepthScopeWithoutGenerating()
        {
            Ambient();int count=manager.CachedZoneCount;Assert.False(HasLedger(manager));manager.WorldMap.SetPOI(0,9,new PointOfInterest(POIType.MerchantCamp,"authored"));
            Assert.False(Backdrop(out _,out _));Assert.False(IsAmbient(View()));manager.WorldMap.SetPOI(0,9,null);Ambient();Assert.False(HasLedger(manager));Assert.AreEqual(count,manager.CachedZoneCount);
            foreach(string id in new[]{"Overworld.0.9.0","Overworld.0.9.3","Overworld.00.9.1","Overworld.11.10.1"})
            {zone=Attach(id);Assert.False(Backdrop(out _,out _));Assert.False(IsAmbient(View()));}
            manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);manager.WorldMap.SetPOI(0,9,new PointOfInterest(POIType.Lair,"retained lair",null,1,"DesertProwler"));
            LairStacks.Restore(manager,null);Assert.NotNull(LairStacks.Inspect(manager,"Overworld.0.9.0"));manager.WorldMap.SetPOI(0,9,null);zone=Attach(Home);
            Assert.False(Backdrop(out _,out _));Assert.False(IsAmbient(View()));
        }
        [TestCase("water")] [TestCase("heat")] [TestCase("charge")]
        public void AmbientBackingCannotReplaceLiveHazardOrSuppressItsSubmittedMark(string stateKind)
        {
            var rubble=Add("Rubble");Ambient(rubble);
            using(var paint=new Paint(world,zone))
            {
                paint.Step();
                if(stateKind=="water")zone.TileState.WriteCoating(X,Y,"water",ZoneTileState.Permanent);
                if(stateKind=="heat")zone.TileState.AddHeat(X,Y,1);
                if(stateKind=="charge")zone.TileState.AddCharge(X,Y,1);
                string state=zone.TileState.ToSaveString();Assert.False(Backdrop(out _,out _));Assert.False(IsAmbient(View()));Assert.False(Ground(out _,out _));Native(rubble);
                paint.Dirty(X,Y);paint.Step();Assert.NotNull(paint.Mark);Assert.AreEqual(state,zone.TileState.ToSaveString());
            }
        }
        [Test] public void VisibleCompetingTerrainAndRepurposedRubbleKeepTheirOwnMeaning()
        {
            foreach(string fault in new[]{"unknown-terrain","cultivation","source","trigger","rubble-variant","rubble-solid"})
            {
                zone=Attach(Home);Ambient();var owner=Add(fault=="unknown-terrain"?"StoneFloor":"Rubble");
                if(fault=="cultivation")owner.AddPart(new CultivatedSoilPart());
                if(fault=="source")owner.AddPart(new TileStateSourcePart());
                if(fault=="trigger")owner.AddPart(new RuneFlameTriggerPart());
                if(fault=="rubble-variant")owner.GetPart<RenderPart>().VisualVariant="other meaning";
                if(fault=="rubble-solid")owner.GetPart<PhysicsPart>().Solid=true;
                Assert.False(Backdrop(out _,out _),fault);Assert.False(IsAmbient(View()),fault);Assert.AreSame(owner,View().Owner);
            }
        }
        [Test] public void AuthoredBackgroundIncludingExplicitBlackOutranksAmbientWithoutHidingItsOwner()
        {
            var actor=Add("Player");Ambient(actor);var render=actor.GetPart<RenderPart>();
            Assert.AreEqual(Color.black,QudColorParser.ParseBackground("^k"),"Lowercase k is explicit black; uppercase K is dark gray.");
            foreach(string background in new[]{"^k","^K","^r"})
            {
                render.BackgroundColor=background;Assert.False(Backdrop(out _,out _));var view=View();Assert.False(IsAmbient(view));Assert.AreSame(actor,view.Owner);
                Assert.True(world.TryResolveFallback(zone,X,Y,out var fallback));Assert.AreEqual(fallback.GroundColor,view.GroundColor);
            }
            render.BackgroundColor=null;Ambient(actor);
        }
        [Test] public void AmbientBackingIsIndependentOfOptionalNativeGroupsButNeverReplacesARealFloorFailure()
        {
            var inputs=MakeTextures();var missing=new Texture2D[6];Inject(missing);var backing=Ambient();
            var floor=Add(Floor);Assert.False(Ground(out _,out _));Assert.False(Backdrop(out _,out _));Assert.False(IsAmbient(View()));Assert.AreSame(floor,View().Owner);Fallback();
            Assert.True(zone.RemoveEntity(floor));Assert.AreSame(backing,Ambient());
            for(int i=0;i<missing.Length;i++){missing[i]=new Texture2D(3,3);textures.Add(missing[i]);}Inject(missing);Ambient();
            Inject(inputs);floor=Add(Floor);var floorView=View();Assert.AreSame(floor,floorView.Owner);Assert.True(floorView.LocalGround);Assert.AreEqual(Color.white,floorView.GroundColor);Assert.IsNull(floorView.ForegroundTile);
            TileGeometry(floorView.GroundTile,inputs[1],Floor);Assert.False(Backdrop(out _,out _));Assert.False(IsAmbient(floorView));Assert.True(Ground(out var floorTile,out var actual));Assert.AreSame(floor,actual);Assert.AreSame(floorTile,floorView.GroundTile);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,floor,X,Y,out var floorSprite,out var floorTint));Assert.AreSame(inputs[1],floorSprite.texture);Assert.AreEqual(floorTile.sprite.rect,floorSprite.rect);Assert.AreEqual(Color.white,floorTint);
            Assert.True(zone.RemoveEntity(floor));backing=Ambient();var sprite=backing.sprite;injected.Dispose();Assert.False(Backdrop(out _,out _));Assert.True(backing!=null);Assert.True(sprite!=null);foreach(var texture in inputs)Assert.True(texture!=null);
        }

        // Same submitted painter and restoration contract as the accepted regional fixtures.
        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject root;
            readonly Tilemap overlay,marks,ground;
            public ZoneRenderer Renderer{get;}
            readonly Action<int,int,string> oldCell=ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull=ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound=typeof(ZoneTileStateSystem).GetField("_boundRenderZone",S);
            readonly object oldBound;readonly Zone previous;readonly Action<int,int> oldChanged;readonly Action oldSight;
            readonly Dictionary<FieldInfo,object> perf=new Dictionary<FieldInfo,object>();
            public Paint(FlatPixelGreyboxCatalog catalog,Zone z)
            {
                oldBound=bound.GetValue(null);if(oldBound is WeakReference<Zone> weak&&weak.TryGetTarget(out previous)){oldChanged=previous.TileState.OnCellChanged;oldSight=previous.TileState.OnSightChanged;}
                try
                {
                    foreach(var f in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public|BindingFlags.Static))perf[f]=f.GetValue(null);
                    saved=new HotbarSaveFixture();root=new GameObject("Shallow cave submitted grid",typeof(Grid));saved.Root.transform.SetParent(root.transform,false);
                    var main=saved.Root.GetComponent<Tilemap>();var bg=new GameObject("Background",typeof(Tilemap));bg.transform.SetParent(root.transform,false);ground=bg.GetComponent<Tilemap>();
                    var mark=new GameObject("Hazards",typeof(Tilemap));mark.transform.SetParent(root.transform,false);marks=mark.GetComponent<Tilemap>();
                    Renderer=saved.Renderer;Write(Renderer,"_tilemap",main);Write(Renderer,"_bgTilemap",ground);Write(Renderer,"_tileStateTilemap",marks);
                    var camera=new GameObject("Camera",typeof(Camera));camera.transform.SetParent(root.transform,false);camera.GetComponent<Camera>().orthographic=true;Write(Renderer,"_mainCamera",camera.GetComponent<Camera>());
                    var env=saved.Root.AddComponent<EnvironmentSpriteRenderer>();env.Init(root.transform,main,ground);env.RenderingEnabled=true;env.FlatPixelGreyboxEnabled=true;
                    Write(env,"_flatPixelGreybox",catalog);overlay=(Tilemap)Read(env,"_overlayTilemap");Write(Renderer,"_envSpriteRenderer",env);Write(Renderer,"_flatPixelModeEnabled",true);Renderer.SetZone(z);
                    ZoneRenderHooks.CellDirtyCallback=Renderer.MarkCellDirty;ZoneRenderHooks.FullDirtyCallback=Renderer.MarkDirty;
                }
                catch{Dispose();throw;}
            }
            public Tile At(int x,int y)=>overlay.GetTile<Tile>(new Vector3Int(x,Zone.Height-1-y,0));
            public Tile GroundAt(int x,int y)=>ground.GetTile<Tile>(new Vector3Int(x,Zone.Height-1-y,0));
            public Color GroundTintAt(int x,int y)=>ground.GetColor(new Vector3Int(x,Zone.Height-1-y,0));
            public TileBase Mark=>marks.GetTile(new Vector3Int(X,Zone.Height-1-Y,0));
            public void Dirty(int x,int y)=>Renderer.MarkCellDirty(x,y,"Shallow cave test state");
            public void Step(){try{typeof(ZoneRenderer).GetMethod("LateUpdate",I).Invoke(Renderer,null);}catch(TargetInvocationException e){throw e.InnerException??e;}}
            public void Dispose()
            {
                try{saved?.Dispose();if(root!=null)Object.DestroyImmediate(root);}
                finally
                {
                    ZoneRenderHooks.CellDirtyCallback=oldCell;ZoneRenderHooks.FullDirtyCallback=oldFull;bound.SetValue(null,oldBound);
                    if(previous!=null){previous.TileState.OnCellChanged=oldChanged;previous.TileState.OnSightChanged=oldSight;}
                    foreach(var p in perf)p.Key.SetValue(null,p.Value);
                }
            }
        }
    }
}
