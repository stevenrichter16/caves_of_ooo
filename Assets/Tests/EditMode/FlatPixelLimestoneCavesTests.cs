// Contract-first ordinary depth3/4 limestone scenery. Root runs native RED before production.
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
    public sealed class FlatPixelLimestoneCavesTests
    {
        const string Home = "Overworld.0.9.3", Floor = "LimestoneFloor", Wall = "LimestoneWall";
        const int X = 10, Y = 10;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[] Owners = { Wall, Floor, "Rubble", "Stalagmite", "StairsDown", "StairsUp" };
        static readonly string[] Paths = { "Sprites/Live16x24/limestone-caves", "Sprites/Live16x24/limestone-caves",
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
        {var t=typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelLimestoneCavesCatalog");Assert.NotNull(t,"Missing ordinary limestone-cave adapter.");return t;}
        static object Read(object owner,string field){var f=owner.GetType().GetField(field,I);Assert.NotNull(f,field);return f.GetValue(owner);}
        static void Write(object owner,string field,object value){var f=owner.GetType().GetField(field,I);Assert.NotNull(f,field);f.SetValue(owner,value);}
        static bool Call(object target,string method,object[] args)
        {var m=target.GetType().GetMethod(method,I);Assert.NotNull(m,method);return (bool)m.Invoke(target,args);}
        object Art()
        {ArtType();var a=Read(world,"_limestoneCavesArt");if(a==null){View();a=Read(world,"_limestoneCavesArt");}Assert.NotNull(a);return a;}
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
            if(name==Floor||name==Wall)Assert.AreEqual(new Vector2Int(96,24),new Vector2Int(texture.width,texture.height));
            int h=name=="Rubble"||name=="Stalagmite"?16:24;
            Assert.AreEqual(new Vector2(16,h),tile.sprite.rect.size);Assert.AreEqual(new Vector2(8,h/2f),tile.sprite.pivot);
            Assert.AreEqual(16,tile.sprite.pixelsPerUnit);Assert.AreEqual(new Vector2(1,h/16f),(Vector2)tile.sprite.bounds.size);
            Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);Assert.AreEqual(TileFlags.None,tile.flags);
            int frame=(int)tile.sprite.rect.x/16;
            if(name==Wall)Assert.That(frame,Is.InRange(4,5));
            else if(name==Floor)Assert.That(frame,Is.InRange(0,3));
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
            int[] widths={96,16,16,192,144},heights={24,16,16,24,24};var a=new Texture2D[5];
            for(int i=0;i<a.Length;i++){a[i]=new Texture2D(widths[i],heights[i]);textures.Add(a[i]);}return a;
        }
        void Inject(Texture2D[] inputs)
        {
            world.Dispose();injected?.Dispose();selector?.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);selector=new FlatPixelFallbackCatalog();
            var signature=new[]{typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(FlatPixelFallbackCatalog)};
            var ctor=ArtType().GetConstructor(signature);Assert.NotNull(ctor);
            injected=(IDisposable)ctor.Invoke(inputs.Cast<object>().Concat(new object[]{selector}).ToArray());Write(world,"_limestoneCavesArt",injected);
        }


        // Frozen limestone floor anchor112,112,100; ambient presentation remains
        // separate from real floor ownership and never introduces an interaction.
        bool Backdrop(out Tile tile,out Color tint,int x=X,int y=Y)
        {object[] args={zone,x,y,null,null};bool ok=Call(Art(),"TryResolveCellBackdrop",args);tile=(Tile)args[3];tint=args[4] is Color c?c:default;return ok;}
        Tile Ambient(Entity owner=null,int x=X,int y=Y)
        {
            Assert.False(Ground(out _,out _,x,y));Assert.True(Backdrop(out var tile,out var tint,x,y));
            Assert.NotNull(tile);Assert.NotNull(tile.sprite);Assert.AreEqual(new Vector2(16,24),tile.sprite.rect.size);
            Assert.AreEqual(16,tile.sprite.pixelsPerUnit);Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);
            Assert.AreEqual((Color)new Color32(112,112,100,255),tint);
            var v=View(x,y);Assert.True(v.AmbientBackdrop);Assert.False(v.LocalGround);Assert.AreSame(owner,v.Owner);
            Assert.AreSame(tile,v.GroundTile);Assert.AreEqual(tint,v.GroundColor);return tile;
        }
        static int TextureGroup(int ownerIndex)=>ownerIndex<2?0:ownerIndex-1;

        [TestCase(Floor)] [TestCase(Wall)] [TestCase("Rubble")] [TestCase("Stalagmite")] [TestCase("StairsDown")] [TestCase("StairsUp")]
        public void SixExactOwnersSubmitSameNativeWorldAndAuthorizedReaderWithoutChangingState(string name)
        {
            var e=Add(name);var observer=Add("Player",X-1,Y);int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();var tile=Native(e);
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone),"No opening admission expansion.");
            using(var paint=new Paint(world,zone))
            {
                paint.Step();Assert.AreSame(tile,name==Floor?paint.GroundAt(X,Y):paint.At(X,Y));
                var document=ExamineDocumentBuilder.BuildWorld(e,zone,zone.GetCell(X,Y),observer);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                Assert.True(paint.Renderer.TryGetFlatExaminePortrait(document,out var portrait));Assert.AreSame(tile.sprite.texture,portrait.Texture);
                Assert.AreEqual(new Rect(tile.sprite.rect.x/tile.sprite.texture.width,0,16f/tile.sprite.texture.width,1),portrait.Uv);Assert.AreEqual(Color.white,portrait.Tint);Assert.False(portrait.IsModel);
                Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
                e.GetPart<RenderPart>().VisualVariant="different meaning";Assert.False(Subject(e));Fallback();if(name==Floor)Assert.False(View().LocalGround);
            }
        }
        [TestCase(3)] [TestCase(4)]
        public void RealCurrentDepthGraphContainsExactLimestoneAndDoesNotWidenAdjacentStrata(int depth)
        {
            manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);zone=manager.GetZone(WorldMap.ToZoneID(0,9,depth));manager.SetActiveZone(zone);Reveal(zone);Assert.True(Supported(zone));
            foreach(string name in new[]{Floor,Wall})
            {
                var owner=zone.GetReadOnlyEntities().FirstOrDefault(e=>e.BlueprintName==name&&zone.GetEntityCell(e) is Cell c&&world.TryResolveFallback(zone,c.X,c.Y,out var f)&&ReferenceEquals(f.Owner,e));
                Assert.NotNull(owner,name);var c=zone.GetEntityCell(owner);Native(owner,c.X,c.Y);
            }
            Assert.False(Supported(Attach("Overworld.0.9.2")));Assert.False(Supported(Attach("Overworld.0.9.5")));
        }
        [TestCase("Overworld.0.9.2")] [TestCase("Overworld.0.9.5")] [TestCase("Overworld.00.9.3")]
        [TestCase("Overworld.11.10.3")] [TestCase("Overworld.3.7.4")] [TestCase("Overworld.2.7.3")] [TestCase("Overworld.3.3.4")]
        public void MaterialNamesDoNotAdmitWrongDepthAliasOrReservedColumn(string id)
        {
            Native(Add(Wall));var p=WorldMap.FromZoneID(id);manager.WorldMap.POIs[p.x,p.y]=null;zone=Attach(id);var e=Add(Wall);
            Assert.False(Supported(zone));Assert.False(Subject(e));Assert.False(Ground(out _,out _));Assert.False(Backdrop(out _,out _));
        }
        [Test] public void CurrentPoiMapShapeAndCachedGraphRevokeWarmAuthorityWithoutGenerating()
        {
            var e=Add(Wall);Native(e);var original=zone;var map=manager.WorldMap;var tiles=map.Tiles;var pois=map.POIs;int count=manager.CachedZoneCount;
            try
            {
                pois[0,9]=new PointOfInterest(POIType.MerchantCamp,"current authored column");Assert.False(Supported(zone));Assert.False(Subject(e));pois[0,9]=null;Native(e);
                map.Tiles=new BiomeType[1,1];Assert.False(Supported(zone));map.Tiles=tiles;
                map.POIs=new PointOfInterest[1,1];Assert.False(Supported(zone));map.POIs=pois;Native(e);
                Assert.AreEqual(count,manager.CachedZoneCount);Assert.False(HasLedger(manager));
                zone=new Zone(Home);Reveal(zone);Assert.IsNull(WorldLocationContext.For(zone));Assert.False(Supported(zone));
                zone=Attach(Home);Assert.False(Supported(original));Assert.False(world.TryResolvePresentation(original,X,Y,out _));
                manager.SetActiveZone(original);zone=original;Native(e);Assert.True(zone.RemoveEntity(e));Assert.False(Subject(e));
            }
            finally{map.Tiles=tiles;map.POIs=pois;pois[0,9]=null;}
        }
        [Test] public void SurfaceKeyedSavedLairRemainsExcludedAfterPoiRemovalWithoutCreatingNewLedger()
        {
            Ambient();int count=manager.CachedZoneCount;Assert.False(HasLedger(manager));for(int i=0;i<3;i++)Assert.True(Supported(zone));Assert.False(HasLedger(manager));Assert.AreEqual(count,manager.CachedZoneCount);
            manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);manager.WorldMap.SetPOI(0,9,new PointOfInterest(POIType.Lair,"retained column",null,1,"DesertProwler"));
            LairStacks.Restore(manager,null);Assert.NotNull(LairStacks.Inspect(manager,"Overworld.0.9.0"));manager.WorldMap.SetPOI(0,9,null);zone=Attach(Home);Add(Floor);
            Assert.False(Supported(zone));Assert.False(Ground(out _,out _));Assert.False(Backdrop(out _,out _));
        }
        [TestCase("hidden")] [TestCase("remembered")] [TestCase("unexplored")]
        public void NeitherMaterialNorAmbientLeaksUndisclosedOwners(string fault)
        {
            var floor=Add(Floor);var wall=Add(Wall);Native(wall);Assert.True(Ground(out _,out _));
            if(fault=="hidden"){wall.GetPart<RenderPart>().Visible=false;floor.GetPart<RenderPart>().Visible=false;}
            else{zone.GetCell(X,Y).IsVisible=false;zone.GetCell(X,Y).Explored=fault=="remembered";}
            Assert.False(Subject(wall));Assert.False(Ground(out _,out _));
            if(fault=="hidden")Ambient();
            else{Assert.False(Backdrop(out _,out _));if(fault=="unexplored")Assert.False(world.TryResolvePresentation(zone,X,Y,out _));else Assert.False(View().AmbientBackdrop);}
        }
        [Test] public void CurrentSelectionFootprintAndRemovalPreserveFloorAndReaderAuthority()
        {
            var floor=Add(Floor);var stair=Add("StairsDown");stair.AddPart(new SpatialFootprintPart{CellsRaw="0,0"});Native(stair);var reader=Add("Player",X-1,Y);
            var document=ExamineDocumentBuilder.BuildWorld(stair,zone,zone.GetCell(X,Y),reader);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            var actor=Add("Player");Assert.AreSame(actor,View().Owner);Assert.False(Subject(stair));Assert.True(Ground(out _,out var actual));Assert.AreSame(floor,actual);
            Assert.True(zone.MoveEntity(actor,X+2,Y));Native(stair);Assert.True(zone.TryChangeFootprint(stair,"0,0;1,0"));Assert.False(Subject(stair));Fallback();
            Assert.True(zone.TryChangeFootprint(stair,"0,0"));Native(stair);stair.GetPart<SpatialFootprintPart>().CellsRaw="0,0;0,1";Assert.False(Subject(stair));
            stair.GetPart<SpatialFootprintPart>().CellsRaw="0,0";Native(stair);Assert.True(zone.RemoveEntity(stair));Assert.False(Subject(stair));Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document));Native(floor);
        }
        [Test] public void OwnedPartsCustodyAndCustomIdentityAreRequiredForNeutralWallPixels()
        {
            foreach(string fault in new[]{"foreign-render","foreign-physics","duplicate-render","carried","equipped","takeable","custom-id"})
            {
                zone=Attach(Home);var e=Add(Wall);Native(e);var stranger=new Entity();
                if(fault=="foreign-render")e.GetPart<RenderPart>().ParentEntity=stranger;
                if(fault=="foreign-physics")e.GetPart<PhysicsPart>().ParentEntity=stranger;
                if(fault=="duplicate-render")e.AddPart(new RenderPart{RenderString="#",RenderLayer=0});
                if(fault=="carried")e.GetPart<PhysicsPart>().InInventory=stranger;
                if(fault=="equipped")e.GetPart<PhysicsPart>().Equipped=stranger;
                if(fault=="takeable")e.GetPart<PhysicsPart>().Takeable=true;
                if(fault=="custom-id")e.GetPart<RenderPart>().VisualID="different identity";
                Assert.False(Subject(e),fault);Assert.False(View().NativeForeground,fault);
            }
        }
        [Test] public void LimestoneIsNotAnAliasForSandstoneGenericWallVineOrCustomFloorMeaning()
        {
            foreach(string name in new[]{"SandstoneFloor","SandstoneWall","ShaleFloor","ShaleWall","Wall","VineWall"})
            {
                zone=Attach(Home);Assert.True(Supported(zone));var e=Add(name);Assert.False(Subject(e),name);Assert.False(Ground(out _,out _),name);Fallback();
            }
            zone=Attach(Home);var floor=Add(Floor);Assert.False(floor.HasPart<MaterialPart>());Assert.False(floor.HasPart<ThermalPart>());Assert.False(floor.HasPart<DestructiblePart>());
            Assert.AreEqual("&w",floor.GetPart<RenderPart>().ColorString);Assert.AreEqual("...,.'`",floor.GetPart<RenderPart>().GlyphVariants);Native(floor);
            floor.GetPart<RenderPart>().GlyphVariants="different";Assert.False(Ground(out _,out _));Assert.False(Subject(floor));
            floor.GetPart<RenderPart>().GlyphVariants="...,.'`";Native(floor);floor.AddPart(new MaterialPart{MaterialID="Wood"});Assert.False(Ground(out _,out _));Assert.False(Backdrop(out _,out _));
        }
        [Test] public void StructuralSurvivalUsesLimestoneThirtyFiveAndHardnessThreeNotCreatureHealth()
        {
            var wall=Add(Wall);Assert.IsNull(wall.GetStat("Hitpoints"));var s=wall.GetPart<DestructiblePart>();Assert.AreEqual(35,s.HP);Assert.AreEqual(35,s.MaxHP);Assert.AreEqual(3,s.Hardness);Assert.AreEqual("Rubble",s.WreckageBlueprint);
            foreach(int hp in new[]{35,17,1}){s.HP=hp;Native(wall);}
            foreach(int hp in new[]{0,-1,36}){s.HP=hp;Assert.False(Subject(wall));Fallback();}s.HP=35;
            s.MaxHP=30;Assert.False(Subject(wall));s.MaxHP=35;s.Hardness=2;Assert.False(Subject(wall));s.Hardness=3;
            s.WreckageBlueprint="StoneFloor";Assert.False(Subject(wall));s.WreckageBlueprint="Rubble";s.ParentEntity=new Entity();Assert.False(Subject(wall));s.ParentEntity=wall;Native(wall);
            var thermal=wall.GetPart<ThermalPart>();float temperature=thermal.Temperature;thermal.Temperature=float.NaN;Assert.False(Subject(wall));thermal.Temperature=temperature;Native(wall);
            wall.GetPart<MaterialPart>().MaterialID="Wood";Assert.False(Subject(wall));Fallback();
        }
        [Test] public void RealWallDamageAndDestructionSubmitActualRubbleAndPreserveUnrelatedHazardAndHealth()
        {
            var floor=Add(Floor);var wall=Add(Wall);var actor=Add("Player",X-1,Y);int hp=actor.GetStat("Hitpoints").Value;var structural=wall.GetPart<DestructiblePart>();Native(wall);
            using(var paint=new Paint(world,zone))
            {
                paint.Step();long full=EnvironmentSpriteRenderer.Perf.FullPasses;Assert.True(zone.GetCell(X,Y).BlocksMovement());
                Assert.AreEqual(DestroyVerdict.Damaged,DestructionSystem.Damage(wall,1,actor,zone));Assert.AreEqual(34,structural.HP);paint.Dirty(X,Y);paint.Step();Native(wall);
                zone.TileState.WriteCoating(X,Y,"water",ZoneTileState.Permanent);string state=zone.TileState.ToSaveString();
                Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(wall,999,actor,zone));paint.Step();Assert.True(structural.Gone);Assert.IsNull(zone.GetEntityCell(wall));Assert.False(Subject(wall));
                var rubble=zone.GetCell(X,Y).Objects.Single(e=>e.BlueprintName=="Rubble");Native(rubble);TileGeometry(paint.At(X,Y),Resource("Rubble"),"Rubble");
                Assert.False(zone.GetCell(X,Y).BlocksMovement());Assert.AreSame(floor,zone.GetCell(X,Y).Objects.Single(e=>e.BlueprintName==Floor));
                Assert.AreEqual(state,zone.TileState.ToSaveString());Assert.NotNull(paint.Mark);Assert.AreEqual(hp,actor.GetStat("Hitpoints").Value);Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);
                Assert.AreEqual(DestroyVerdict.NotBreakable,DestructionSystem.Damage(rubble,999,actor,zone));
            }
        }
        [Test] public void RealFloorUnderStairRubbleAndActorStaysDistinctFromOwnerFreeBackingInDirtyPaint()
        {
            var floor=Add(Floor);var stair=Add("StairsDown");Add(Floor,X+1,Y);var rubble=Add("Rubble",X+1,Y);Add(Floor,X+2,Y);var actor=Add("Player",X+2,Y);Native(stair);Native(rubble,X+1,Y);
            using(var paint=new Paint(world,zone))
            {
                paint.Step();for(int i=0;i<3;i++)TileGeometry(paint.GroundAt(X+i,Y),Resource(Floor),Floor);Assert.AreSame(actor,View(X+2,Y).Owner);
                long full=EnvironmentSpriteRenderer.Perf.FullPasses;Assert.True(zone.RemoveEntity(floor));paint.Dirty(X,Y);paint.Step();Assert.False(Ground(out _,out _));Assert.False(View().LocalGround);Native(stair);Ambient(stair);
                Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);Assert.AreSame(stair,View().Owner);
            }
        }
        [Test] public void AmbientBackingIsOwnerFreeAndUsesBothExistingUnattenuatedPaintPaths()
        {
            zone.AmbientLevel=.28f;zone.AmbientTint=Color.white;var backing=Ambient();Assert.IsEmpty(zone.GetCell(X,Y).Objects);
            Assert.False(world.TryGetPresentationSubjectSprite(zone,null,X,Y,out _,out _));var rubble=Add("Rubble");Native(rubble);Ambient(rubble);
            var control=Add("StoneFloor",X+3,Y);control.GetPart<RenderPart>().BackgroundColor="^r";
            using(var paint=new Paint(world,zone))
            {
                paint.Step();Assert.True(Backdrop(out _,out var tint));Assert.AreEqual(tint,paint.GroundTintAt(X,Y));
                var light=(LightMap)Read(paint.Renderer,"_lightMap");Assert.NotNull(light);Assert.AreEqual(.28f,light.AmbientLevel);Assert.Less(light.ApplyToColor(tint,X,Y).maxColorComponent,tint.maxColorComponent);
                var controlView=View(X+3,Y);Assert.False(controlView.AmbientBackdrop);var dim=light.ApplyToColor(controlView.GroundColor,X+3,Y);Assert.Less(dim.r,controlView.GroundColor.r);Assert.AreEqual(dim,paint.GroundTintAt(X+3,Y));
                var core=typeof(ZoneRenderer).GetMethod("RenderFlatCellCore",I);Assert.NotNull(core);core.Invoke(paint.Renderer,new object[]{X,Y,new Vector3Int(X,Zone.Height-1-Y,0),zone.GetCell(X,Y)});Assert.AreEqual(tint,paint.GroundTintAt(X,Y));
                var actor=Add("Player");paint.Dirty(X,Y);paint.Step();Ambient(actor);Assert.False(Subject(rubble));Assert.AreSame(backing,paint.GroundAt(X,Y));
                Assert.True(zone.MoveEntity(actor,X+1,Y));Assert.True(zone.RemoveEntity(rubble));paint.Dirty(X,Y);paint.Dirty(X+1,Y);paint.Step();Ambient();Ambient(actor,X+1,Y);Assert.IsNull(paint.At(X,Y));
                Assert.AreSame(backing,paint.GroundAt(X,Y));Assert.IsEmpty(zone.GetCell(X,Y).Objects);
            }
        }
        [Test] public void HiddenForeignAndCustodiedCompetitorsCannotSignalThroughTheBackdrop()
        {
            var backing=Ambient();var other=Add("StoneFloor");var render=other.GetPart<RenderPart>();var physics=other.GetPart<PhysicsPart>();
            render.Visible=false;Assert.AreSame(backing,Ambient());render.Visible=true;Assert.False(Backdrop(out _,out _));
            render.ParentEntity=new Entity();Assert.AreSame(backing,Ambient());render.ParentEntity=other;
            physics.ParentEntity=new Entity();Assert.AreSame(backing,Ambient());physics.ParentEntity=other;
            physics.InInventory=new Entity();Assert.AreSame(backing,Ambient());physics.InInventory=null;physics.Equipped=new Entity();Assert.AreSame(backing,Ambient());physics.Equipped=null;
            Assert.False(Backdrop(out _,out _));Assert.True(zone.RemoveEntity(other));var wall=Add(Wall);wall.GetPart<RenderPart>().Visible=false;Assert.True(zone.GetCell(X,Y).BlocksMovement());Assert.AreSame(backing,Ambient());
        }
        [TestCase("water")] [TestCase("heat")] [TestCase("charge")]
        public void GroundAndAmbientNeverReplaceLiveHazardOrItsSubmittedMark(string kind)
        {
            var floor=Add(Floor);var rubble=Add("Rubble");Native(rubble);Assert.True(Ground(out _,out _));
            using(var paint=new Paint(world,zone))
            {
                paint.Step();if(kind=="water")zone.TileState.WriteCoating(X,Y,"water",ZoneTileState.Permanent);if(kind=="heat")zone.TileState.AddHeat(X,Y,1);if(kind=="charge")zone.TileState.AddCharge(X,Y,1);
                string state=zone.TileState.ToSaveString();Assert.False(Ground(out _,out _));Assert.False(Backdrop(out _,out _));Native(rubble);paint.Dirty(X,Y);paint.Step();Assert.NotNull(paint.Mark);
                Assert.True(zone.RemoveEntity(floor));Assert.False(Backdrop(out _,out _));Assert.False(View().AmbientBackdrop);paint.Dirty(X,Y);paint.Step();Assert.NotNull(paint.Mark);Assert.AreEqual(state,zone.TileState.ToSaveString());
            }
        }
        [Test] public void VisibleCompetingMeaningAndExplicitBlackBackgroundOutrankAmbientWithoutHidingOwner()
        {
            foreach(string fault in new[]{"unknown-terrain","cultivation","source","trigger","rubble-variant","rubble-solid"})
            {
                zone=Attach(Home);Ambient();var owner=Add(fault=="unknown-terrain"?"StoneFloor":"Rubble");
                if(fault=="cultivation")owner.AddPart(new CultivatedSoilPart());if(fault=="source")owner.AddPart(new TileStateSourcePart());if(fault=="trigger")owner.AddPart(new RuneFlameTriggerPart());
                if(fault=="rubble-variant")owner.GetPart<RenderPart>().VisualVariant="other";if(fault=="rubble-solid")owner.GetPart<PhysicsPart>().Solid=true;
                Assert.False(Backdrop(out _,out _),fault);Assert.False(View().AmbientBackdrop,fault);Assert.AreSame(owner,View().Owner);
            }
            zone=Attach(Home);var actor=Add("Player");Ambient(actor);Assert.AreEqual(Color.black,QudColorParser.ParseBackground("^k"));actor.GetPart<RenderPart>().BackgroundColor="^k";
            Assert.False(Backdrop(out _,out _));Assert.False(View().AmbientBackdrop);Assert.AreSame(actor,View().Owner);
        }
        [Test] public void MissingOrMalformedAtlasDoesNotInventFloorAndBorrowedGroupsFailIndependently()
        {
            var inputs=MakeTextures();for(int i=0;i<Owners.Length;i++)Add(Owners[i],X+i,Y);
            for(int missing=0;missing<inputs.Length;missing++)for(int malformed=0;malformed<2;malformed++)
            {
                var set=(Texture2D[])inputs.Clone();if(malformed==0)set[missing]=null;else{set[missing]=new Texture2D(3,3);textures.Add(set[missing]);}Inject(set);
                for(int i=0;i<Owners.Length;i++)
                {
                    var v=View(X+i,Y);int group=TextureGroup(i);var owner=zone.GetCell(X+i,Y).Objects.Single();
                    if(group==missing){Assert.False(v.NativeForeground);if(i==1){Assert.False(v.LocalGround);Assert.False(v.AmbientBackdrop);Assert.False(Backdrop(out _,out _,X+i,Y));}Assert.False(Subject(owner,X+i,Y));}
                    else TileGeometry(i==1?v.GroundTile:v.ForegroundTile,inputs[group],Owners[i]);
                }
                Ambient(null,X+7,Y);
            }
        }
        [Test] public void DisposalDestroysOnlyOwnedViewsNotBorrowedTexturesOrSelectorBackdrop()
        {
            var inputs=MakeTextures();var wall=Add(Wall);Inject(inputs);var tile=View().ForegroundTile;TileGeometry(tile,inputs[0],Wall);var sprite=tile.sprite;
            var backing=Ambient(null,X+2,Y);var backgroundSprite=backing.sprite;injected.Dispose();Assert.True(tile==null);Assert.True(sprite==null);Assert.False(Subject(wall));Assert.False(Backdrop(out _,out _,X+2,Y));
            Assert.True(backing!=null);Assert.True(backgroundSprite!=null);foreach(var texture in inputs)Assert.True(texture!=null);
        }
        [TestCase(3)] [TestCase(4)]
        public void ActualVerticalCounterpartsSupportRoundTripButMissingReturnEndpointDoesNotGetRepaired(int lowerDepth)
        {
            zone=Attach(WorldMap.ToZoneID(0,9,lowerDepth-1));var upper=zone;var down=Add("StairsDown");
            Assert.True(world.TryGetPresentationSubjectSprite(zone,down,X,Y,out var downSprite,out _));Assert.AreSame(Resource("StairsDown"),downSprite.texture);
            var actor=Add("Player");int hp=actor.GetStat("Hitpoints").Value;zone=Attach(WorldMap.ToZoneID(0,9,lowerDepth));var lower=zone;var up=Add("StairsUp",X+3,Y+1);Native(up,X+3,Y+1);
            manager.SetActiveZone(upper);var result=ZoneTransitionSystem.TransitionPlayerVertical(actor,upper,true,X,Y,manager);Assert.True(result.Success,result.ErrorReason);Assert.AreSame(lower,result.NewZone);Assert.AreEqual((X+3,Y+1),lower.GetEntityPosition(actor));
            var back=ZoneTransitionSystem.TransitionPlayerVertical(actor,lower,false,X+3,Y+1,manager);Assert.True(back.Success,back.ErrorReason);Assert.AreSame(upper,back.NewZone);Assert.AreEqual((X,Y),upper.GetEntityPosition(actor));Assert.AreEqual(hp,actor.GetStat("Hitpoints").Value);
            Assert.True(lower.RemoveEntity(up));Assert.False(Subject(up,X+3,Y+1));int count=manager.CachedZoneCount;
            var refused=ZoneTransitionSystem.TransitionPlayerVertical(actor,upper,true,X,Y,manager);Assert.False(refused.Success);StringAssert.Contains("return stairs",refused.ErrorReason);Assert.AreSame(upper.GetCell(X,Y),upper.GetEntityCell(actor));Assert.AreEqual(count,manager.CachedZoneCount);
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
                    saved=new HotbarSaveFixture();root=new GameObject("Limestone cave submitted grid",typeof(Grid));saved.Root.transform.SetParent(root.transform,false);
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
            public void Dirty(int x,int y)=>Renderer.MarkCellDirty(x,y,"Limestone cave test state");
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
