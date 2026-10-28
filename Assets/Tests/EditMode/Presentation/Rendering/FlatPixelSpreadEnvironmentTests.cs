using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Real owners/selector/painter; reflection makes the missing adapter an assertion RED.
    public sealed class FlatPixelSpreadEnvironmentTests
    {
        const string Home="Overworld.10.11.0", Turf="Sprites/Live16x24/sill-environment", Road="Sprites/Live16x24/opening-ground",
            Hedge="Sprites/Live16x24/hedge-continuity", Trees="Sprites/Live16x24/north-vegetation", Bush="Sprites/Live16x24/opening-base";
        const int X=10,Y=10;
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        delegate bool Ground(Zone zone,int x,int y,out Tile tile,out Entity owner);
        delegate bool Foreground(Zone zone,int x,int y,Entity owner,out Tile tile);
        delegate bool Portrait(Zone zone,Entity owner,int x,int y,out Sprite sprite);
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone,oldSettlement;
        FlatPixelGreyboxCatalog world; FlatPixelFallbackCatalog selector; IDisposable adapter;
        Ground ground; Foreground foreground; Portrait portrait; Func<Zone,bool> supported;
        readonly List<Texture2D> textures=new List<Texture2D>();
        Action<int,int,string> oldDirty; Action<string> oldFull,oldMessage;
        EntityDamageVisualHandler oldDamage; Action<WorldAudioCombatReceipt> oldAudio;
        [SetUp] public void Setup()
        {
            oldSettlement=SettlementRuntime.ActiveZone;oldDirty=ZoneRenderHooks.CellDirtyCallback;oldFull=ZoneRenderHooks.FullDirtyCallback;
            oldMessage=MessageLog.OnMessage;oldDamage=EntityVisualHooks.DamageCallback;oldAudio=WorldAudioCombat.Callback;
            ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;MessageLog.OnMessage=null;
            EntityVisualHooks.DamageCallback=null;WorldAudioCombat.Callback=null;
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);
            zone=Attach(Home);Assert.IsNull(manager.WorldMap.GetPOI(10,11));world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try{world?.Dispose();adapter?.Dispose();selector?.Dispose();foreach(var t in textures)if(t!=null)Object.DestroyImmediate(t);textures.Clear();content?.Dispose();}
            finally{SettlementRuntime.ActiveZone=oldSettlement;ZoneRenderHooks.CellDirtyCallback=oldDirty;ZoneRenderHooks.FullDirtyCallback=oldFull;
                MessageLog.OnMessage=oldMessage;EntityVisualHooks.DamageCallback=oldDamage;WorldAudioCombat.Callback=oldAudio;}
        }
        Zone Attach(string id){var z=new Zone(id);manager.SetActiveZone(z);foreach(var c in z.Cells)c.IsVisible=c.Explored=true;return z;}
        Entity Add(string name,int x=X,int y=Y){var e=content.Factory.CreateEntity(name);Assert.NotNull(e,name);Assert.True(zone.AddEntity(e,x,y));return e;}
        static Texture2D Art(string path){var t=Resources.Load<Texture2D>(path);Assert.NotNull(t,path);return t;}
        static object Read(object o,string n){var f=o.GetType().GetField(n,I);Assert.NotNull(f,n);return f.GetValue(o);}
        static void Write(object o,string n,object v){var f=o.GetType().GetField(n,I);Assert.NotNull(f,n);f.SetValue(o,v);}
        FlatPixelGreyboxCell View(int x=X,int y=Y){Assert.True(world.TryResolvePresentation(zone,x,y,out var v));return v;}
        static void Frame(Tile t,string path,int low,int high){Assert.NotNull(t);Assert.AreSame(Art(path),t.sprite.texture);int f=(int)t.sprite.rect.x/16;
            Assert.That(f,Is.InRange(low,high));Assert.AreEqual(new Rect(f*16,0,16,24),t.sprite.rect);Assert.AreEqual(new Vector2(8,12),t.sprite.pivot);
            Assert.AreEqual(16,t.sprite.pixelsPerUnit);Assert.AreEqual(TileFlags.None,t.flags);Assert.AreEqual(Tile.ColliderType.None,t.colliderType);}
        void Native(string role,int x=X,int y=Y)
        {
            var v=View(x,y);if(role=="Grass"||role=="RoadStone")
            {Assert.True(v.LocalGround,"Ordinary Spread ground still uses the glyph field.");Frame(v.GroundTile,role=="Grass"?Turf:Road,role=="Grass"?0:16,role=="Grass"?3:17);}
            else{Assert.True(v.NativeForeground,"Ordinary Spread environmental owner still uses a glyph.");Frame(v.ForegroundTile,role=="Hedge"?Hedge:role=="Tree"?Trees:Bush,role=="Bush"?5:0,role=="Hedge"?15:role=="Bush"?5:0);}
        }
        void Create(Texture2D[] inputs=null,bool inject=false)
        {
            var type=typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelSpreadEnvironmentCatalog");
            Assert.NotNull(type,"The ordinary Spread material adapter is absent.");
            selector=new FlatPixelFallbackCatalog();inputs=inputs??new[]{Art(Turf),Art(Road),Art(Hedge),Art(Trees),Art(Bush)};
            adapter=(IDisposable)Activator.CreateInstance(type,new object[]{inputs[0],inputs[1],inputs[2],inputs[3],inputs[4],selector});
            ground=(Ground)Delegate.CreateDelegate(typeof(Ground),adapter,type.GetMethod("TryResolveGround",I));
            foreground=(Foreground)Delegate.CreateDelegate(typeof(Foreground),adapter,type.GetMethod("TryResolveForeground",I));
            portrait=(Portrait)Delegate.CreateDelegate(typeof(Portrait),adapter,type.GetMethod("TryGetSubjectSprite",I));
            supported=(Func<Zone,bool>)Delegate.CreateDelegate(typeof(Func<Zone,bool>),type.GetMethod("IsSupportedZone",S));
            if(inject){world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);Write(world,"_spreadEnvironmentArt",adapter);}
        }
        void ReleaseAdapter(){adapter?.Dispose();selector?.Dispose();adapter=null;selector=null;}
        bool ResolveOwner(Entity e,out Tile tile){var c=zone.GetEntityCell(e);if(e.BlueprintName=="Grass"||e.BlueprintName=="RoadStone")return ground(zone,c.X,c.Y,out tile,out _);return foreground(zone,c.X,c.Y,e,out tile);}

        [TestCase("Grass")][TestCase("RoadStone")][TestCase("Hedge")][TestCase("Tree")][TestCase("Bush")]
        public void ExactOrdinaryWorldAndReaderReuseApprovedMaterialsWithoutOpeningAdmission(string role)
        {
            if(role!="Grass"&&role!="RoadStone")Add("Grass");var e=Add(role);int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();Native(role);var v=View();Assert.AreSame(e,v.Owner);Assert.True(v.LocalGround,"The real grass beneath a shrub/tree/hedge must remain represented.");
            Assert.AreEqual(e.GetPart<PhysicsPart>().Solid,v.BlocksMovement);Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            Assert.True(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var sprite,out var tint));
            Assert.AreSame((role=="Grass"||role=="RoadStone"?v.GroundTile:v.ForegroundTile).sprite.texture,sprite.texture);
            Assert.AreEqual(Color.white,tint);Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
        }
        [Test] public void ScopeReservesOpeningAndSettlementAddressesEvenWhenTheirLabelsAreRemoved()
        {
            Create();Assert.True(supported(zone));
            foreach(string id in new[]{"Overworld.11.10.0","Overworld.11.9.0","Overworld.11.8.0","Overworld.12.10.0","Overworld.12.11.0","Overworld.10.10.0","Overworld.11.10.1","Overworld.18.8.0","Overworld.010.11.0","study"})
            {zone=Attach(id);var p=WorldMap.FromZoneID(id);if(WorldMapAuthoring.InBounds(p.x,p.y))manager.WorldMap.SetPOI(p.x,p.y,null);Assert.False(supported(zone),id);}
            zone=Attach(Home);Assert.True(supported(zone));var map=manager.WorldMap;var tiles=map.Tiles;var pois=map.POIs;
            foreach(string fault in new[]{"biome","poi","tiles","pois","replaced"})
            {
                if(fault=="biome")tiles[10,11]=BiomeType.Sodden;if(fault=="poi")map.SetPOI(10,11,new PointOfInterest(POIType.MerchantCamp,"changed site"));
                if(fault=="tiles")map.Tiles=new BiomeType[1,1];if(fault=="pois")map.POIs=new PointOfInterest[1,1];
                if(fault=="replaced")manager.CachedZones[Home]=new Zone(Home);Assert.False(supported(zone),fault);
                map.Tiles=tiles;map.POIs=pois;tiles[10,11]=BiomeType.Spread;map.SetPOI(10,11,null);manager.CachedZones[Home]=zone;Assert.True(supported(zone));
            }
        }
        [Test] public void SavedLairColumnCannotBecomeOrdinaryByErasingItsMapLabel()
        {
            Create();Assert.True(supported(zone));manager.CachedZones.Remove(Home);
            manager.WorldMap.SetPOI(10,11,new PointOfInterest(POIType.Lair,"authority probe",null,1,"MarlbackScrabbler"));
            LairStacks.Restore(manager,null);zone=manager.GetZone(Home);manager.SetActiveZone(zone);
            Assert.NotNull(LairStacks.Inspect(manager,Home));manager.WorldMap.SetPOI(10,11,null);
            Assert.False(supported(zone));Assert.NotNull(LairStacks.Inspect(manager,Home));
        }
        [Test] public void GroundPersistsBeneathActorsButNotCoatingsCultivationOrCompetingFloors()
        {
            Create();var grass=Add("Grass");Assert.True(ground(zone,X,Y,out var tile,out var floor));Frame(tile,Turf,0,3);
            var actor=Add("Player");Assert.True(ground(zone,X,Y,out var covered,out floor));Assert.AreSame(tile,covered);Assert.AreSame(grass,floor);
            foreach(string liquid in new[]{"water","oil"}){zone.TileState.WriteCoating(X,Y,liquid,3);Assert.False(ground(zone,X,Y,out _,out _));zone.TileState.RemoveCoating(X,Y,liquid);}
            foreach(string role in new[]{"StoneFloor","CropRow","WaterPuddle"}){var other=Add(role);Assert.False(ground(zone,X,Y,out _,out _),role);zone.RemoveEntity(other);}
            var soil=new CultivatedSoilPart();grass.AddPart(soil);Assert.False(ground(zone,X,Y,out _,out _));grass.RemovePart(soil);Assert.True(ground(zone,X,Y,out _,out _));
            var road=Add("RoadStone");Assert.True(ground(zone,X,Y,out tile,out floor));Assert.AreSame(road,floor);Frame(tile,Road,16,17);
            var duplicate=Add("RoadStone");Assert.False(ground(zone,X,Y,out _,out _));zone.RemoveEntity(duplicate);Assert.True(ground(zone,X,Y,out _,out _));
            Assert.AreSame(actor,View().Owner);Assert.True(View().LocalGround);Assert.AreSame(Art(Road),View().GroundTile.sprite.texture);
        }
        [Test] public void UndisclosedForeignOrCarriedCompetingOwnersDoNotRevealThemselvesThroughGround()
        {
            Create();var grass=Add("Grass");Assert.True(ground(zone,X,Y,out var tile,out _));var crop=Add("CropRow");
            crop.GetPart<RenderPart>().Visible=false;Assert.True(ground(zone,X,Y,out var same,out _));Assert.AreSame(tile,same);
            crop.GetPart<RenderPart>().Visible=true;Assert.False(ground(zone,X,Y,out _,out _));
            crop.GetPart<PhysicsPart>().InInventory=new Entity();Assert.True(ground(zone,X,Y,out _,out _));crop.GetPart<PhysicsPart>().InInventory=null;
            crop.GetPart<RenderPart>().ParentEntity=new Entity();Assert.True(ground(zone,X,Y,out _,out _));
            var c=zone.GetCell(X,Y);c.IsVisible=false;Assert.False(ground(zone,X,Y,out _,out _));Assert.False(View().LocalGround);Assert.False(portrait(zone,grass,X,Y,out _));
            c.IsVisible=true;c.Explored=false;Assert.False(ground(zone,X,Y,out _,out _));Assert.False(world.TryResolvePresentation(zone,X,Y,out _));
        }
        [Test] public void ExactCoreOwnershipAndCustomVisualsCannotEscapeThroughApprovedBodies()
        {
            Create();foreach(string role in new[]{"Grass","RoadStone","Hedge","Tree","Bush"})foreach(string fault in new[]{"visual","glyph","color","foreign-render","duplicate-render","foreign-physics","carried","equipped","footprint","harvest"})
            {
                zone=Attach(Home);var e=Add(role);Assert.True(ResolveOwner(e,out _),role);var r=e.GetPart<RenderPart>();var p=e.GetPart<PhysicsPart>();
                switch(fault){case "visual":r.VisualID="different";break;case "glyph":r.RenderString="?";break;case "color":r.ColorString="&R";break;
                    case "foreign-render":r.ParentEntity=new Entity();break;case "duplicate-render":e.Parts.Add(new RenderPart{ParentEntity=e});break;
                    case "foreign-physics":p.ParentEntity=new Entity();break;case "carried":p.InInventory=new Entity();break;case "equipped":p.Equipped=new Entity();break;
                    case "footprint":e.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});break;case "harvest":e.AddPart(new HarvestablePart());break;}
                Assert.False(ResolveOwner(e,out _),role+" "+fault);Assert.False(portrait(zone,e,X,Y,out _),role+" reader "+fault);
            }
        }
        [Test] public void HedgeMasksUseDisclosedSelectedCardinalsAndPortraitRemainsIsolated()
        {
            Create();for(int mask=0;mask<16;mask++)
            {
                zone=Attach(Home);var e=Add("Hedge");int[] dx={0,1,0,-1},dy={-1,0,1,0};
                for(int b=0;b<4;b++)if((mask&(1<<b))!=0)Add("Hedge",X+dx[b],Y+dy[b]);Add("Hedge",X+1,Y+1);
                Assert.True(foreground(zone,X,Y,e,out var tile));Frame(tile,Hedge,mask,mask);
                Assert.True(portrait(zone,e,X,Y,out var sprite));Assert.AreEqual(new Rect(0,0,16,24),sprite.rect);
                Assert.True(foreground(zone,X,Y,e,out var same));Assert.AreSame(tile,same);
            }
            zone=Attach(Home);var center=Add("Hedge");var east=Add("Hedge",X+1);Assert.True(foreground(zone,X,Y,center,out var connected));Frame(connected,Hedge,2,2);
            zone.GetCell(X+1,Y).IsVisible=false;Assert.True(foreground(zone,X,Y,center,out var cap));Frame(cap,Hedge,0,0);zone.GetCell(X+1,Y).IsVisible=true;
            var actor=Add("Player",X+1);Assert.True(foreground(zone,X,Y,center,out cap));Frame(cap,Hedge,0,0);zone.RemoveEntity(actor);
            east.GetPart<DestructiblePart>().Gone=true;Assert.True(foreground(zone,X,Y,center,out cap));Frame(cap,Hedge,0,0);
        }
        [Test] public void NativeGroundSelectionIsStableAndDoesNotAdvanceAnyWorldState()
        {
            Create();var seen=new HashSet<int>();for(int y=2;y<10;y++)for(int x=2;x<30;x++)Add("Grass",x,y);
            int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();
            for(int y=2;y<10;y++)for(int x=2;x<30;x++){Assert.True(ground(zone,x,y,out var tile,out _));Frame(tile,Turf,0,3);seen.Add((int)tile.sprite.rect.x/16);
                Assert.True(ground(zone,x,y,out var same,out _));Assert.AreSame(tile,same);}
            Assert.Greater(seen.Count,1);Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
        }
        [Test] public void OptionalGroupsAreIndependentAndInvalidInjectionNeverReloadsResources()
        {
            string[] roles={"Grass","RoadStone","Hedge","Tree","Bush"};var bad=new Texture2D(16,16);textures.Add(bad);
            for(int missing=0;missing<5;missing++)foreach(bool malformed in new[]{false,true})
            {
                ReleaseAdapter();var inputs=new[]{Art(Turf),Art(Road),Art(Hedge),Art(Trees),Art(Bush)};inputs[missing]=malformed?bad:null;Create(inputs);
                for(int n=0;n<5;n++){zone=Attach(Home);var owner=Add(roles[n]);Assert.AreEqual(n!=missing,ResolveOwner(owner,out _),roles[n]);}
            }
        }
        [Test] public void CachedViewsAreDisposedIndependentlyOfBorrowedArtAndSelector()
        {
            Create();var e=Add("Hedge");Assert.True(foreground(zone,X,Y,e,out var tile));var sprite=tile.sprite;adapter.Dispose();adapter.Dispose();
            Assert.True(tile==null);Assert.True(sprite==null);Assert.NotNull(Art(Hedge));Assert.True(selector.TryResolve(zone,X,Y,out _));Assert.False(foreground(zone,X,Y,e,out _));
            world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);Assert.False(View().NativeForeground);Assert.False(View().LocalGround);
        }
        [Test] public void RealHedgeDestructionRepaintsNeighborCapsWithoutAFullPass()
        {
            Add("Grass",X-1,Y);Add("Grass");Add("Grass",X+1,Y);Add("Hedge",X-1,Y);var center=Add("Hedge");Add("Hedge",X+1,Y);Native("Hedge");
            using(var paint=new Paint(world,zone))
            {
                paint.Step();Frame(paint.At(X,Y),Hedge,10,10);long full=EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(center,999,null,zone));paint.Step();
                Frame(paint.At(X-1,Y),Hedge,0,0);Frame(paint.At(X+1,Y),Hedge,0,0);Assert.False(zone.GetCell(X,Y).BlocksMovement());
                Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);long resolved=EnvironmentSpriteRenderer.Perf.CellsResolved;paint.Step();Assert.AreEqual(resolved,EnvironmentSpriteRenderer.Perf.CellsResolved);
            }
        }
        [Test] public void OneActualRenderEventRetainsWetColorWithoutChangingNeutralReader()
        {
            var e=Add("Bush");Native("Bush");var effects=new StatusEffectsPart();e.AddPart(effects);var probe=new Probe();e.AddPart(probe);
            using(var paint=new Paint(world,zone))
            {
                paint.Step();Assert.True(effects.ForceApplyEffect(new WetEffect(),null,zone));int count=probe.Renders;paint.Dirty(X,Y);paint.Step();
                Assert.AreEqual(count+1,probe.Renders);Assert.AreEqual(QudColorParser.Parse("&B"),paint.Tint(X,Y));Frame(paint.At(X,Y),Bush,5,5);
                Assert.True(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var sprite,out var tint));Assert.AreSame(Art(Bush),sprite.texture);Assert.AreEqual(Color.white,tint);
            }
        }
        [TestCase("Grass")][TestCase("RoadStone")]
        public void ExactUnderfootFloorReaderMatchesSubmittedGroundWithoutBorrowingActorSelection(string role)
        {
            var floor=Add(role);var player=Add("Player");string resource=role=="Grass"?Turf:Road;
            using(var paint=new Paint(world,zone))
            {
                paint.Step();var submitted=paint.GroundAt(X,Y);Frame(submitted,resource,role=="Grass"?0:16,role=="Grass"?3:17);
                Assert.AreSame(player,View().Owner,"The foreground remains the real actor, not an invented floor selection.");
                Assert.True(world.TryGetPresentationSubjectSprite(zone,floor,X,Y,out var sprite,out var tint));
                Assert.AreSame(submitted.sprite,sprite,"An authorized underfoot floor must use the same native material actually painted beneath the player.");
                Assert.AreEqual(Color.white,tint);
                var doc=ExamineDocumentBuilder.BuildWorld(floor,zone,zone.GetCell(X,Y),player);
                Assert.True(ExamineDocumentBuilder.IsCurrentSubject(doc));
                Assert.True(paint.Renderer.TryGetFlatExaminePortrait(doc,out var actual));Assert.AreSame(submitted.sprite.texture,actual.Texture);
                void Refused(string reason)
                {
                    bool handled=world.TryGetPresentationSubjectSprite(zone,floor,X,Y,out var rejected,out _);
                    Assert.True(!handled||rejected==null||rejected.texture!=Art(resource),reason);
                }
                var render=floor.GetPart<RenderPart>();render.Visible=false;Refused("Hidden floor");render.Visible=true;
                zone.GetCell(X,Y).IsVisible=false;Refused("Remembered cell");zone.GetCell(X,Y).IsVisible=true;
                floor.GetPart<PhysicsPart>().InInventory=player;Refused("Carried floor");floor.GetPart<PhysicsPart>().InInventory=null;
                Assert.True(zone.MoveEntity(floor,X+1,Y));Refused("Floor moved away from the authorized cell");Assert.True(zone.MoveEntity(floor,X,Y));
                zone.TileState.WriteCoating(X,Y,"water",3);Refused("Coating outranks dry material");zone.TileState.RemoveCoating(X,Y,"water");
                var conflict=Add("StoneFloor");Refused("Another actual floor takes priority");Assert.True(zone.RemoveEntity(conflict));
                var cover=Add("Hedge");
                Assert.True(world.TryGetPresentationSubjectSprite(zone,cover,X,Y,out var plant,out _));
                Assert.AreNotSame(Art(Hedge),plant.texture,"A covered hedge still requires foreground selection; this exception is for the exact lower floor only.");
                Assert.True(zone.RemoveEntity(cover));
                paint.Dirty(X,Y);paint.Step();Assert.True(world.TryGetPresentationSubjectSprite(zone,floor,X,Y,out sprite,out _));
                Assert.AreSame(paint.GroundAt(X,Y).sprite,sprite,"Valid restoration regains the same current floor material.");
                Assert.True(zone.RemoveEntity(floor));Refused("Removed floor");Assert.False(ExamineDocumentBuilder.IsCurrentSubject(doc));
                Assert.True(paint.Renderer.TryGetFlatExaminePortrait(doc,out actual));Assert.IsNull(actual.Texture);
            }
        }
        [TestCase("CopperPipe")][TestCase("PeatBog")][TestCase("SteamVent")]
        public void EnvironmentalFixtureHasNativeBodyRealGroundAndExactReader(string role)
        {
            var floor=Add("Grass");var owner=Add(role);var v=View();
            Assert.True(v.NativeForeground,"Fixture body is still ASCII");Assert.True(v.LocalGround,"Fixture has a black hole instead of its actual grass");
            Frame(v.GroundTile,Turf,0,3);Assert.AreEqual(FlatPixelGreyboxGround.Ground,v.GroundKind);Assert.False(v.RepresentedBrine||v.RepresentedMire);
            if(role!="SteamVent")Frame(v.ForegroundTile,"Sprites/Live16x24/nella-corner",role=="PeatBog"?16:0,role=="PeatBog"?16:0);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var sprite,out _));Assert.AreSame(v.ForegroundTile.sprite.texture,sprite.texture);
            var actor=Add("Player");Assert.AreSame(actor,View().Owner);Assert.True(View().LocalGround);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out sprite,out _));Assert.AreSame(v.ForegroundTile.sprite.texture,sprite.texture);
            zone.GetCell(X,Y).IsVisible=false;Assert.False(View().LocalGround);Assert.False(View().NativeForeground);
            zone.GetCell(X,Y).IsVisible=true;owner.GetPart<PhysicsPart>().InInventory=actor;
            Assert.True(!world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out sprite,out _)||sprite.texture!=v.ForegroundTile.sprite.texture);
        }
        [Test] public void OrdinaryPipeJoinsOnlyDisclosedSelectedNeighborsAndMatchesConnectedPortrait()
        {
            Add("Grass");var center=Add("CopperPipe");var neighbors=new List<Entity>();int[] dx={0,1,0,-1},dy={-1,0,1,0};
            for(int mask=0;mask<16;mask++)
            {
                foreach(var e in neighbors)zone.RemoveEntity(e);neighbors.Clear();
                for(int n=0;n<4;n++)if((mask&(1<<n))!=0)neighbors.Add(Add("CopperPipe",X+dx[n],Y+dy[n]));
                Frame(View().ForegroundTile,"Sprites/Live16x24/nella-corner",mask,mask);
                Assert.True(world.TryGetPresentationSubjectSprite(zone,center,X,Y,out var image,out _));Assert.AreEqual(mask*16,image.rect.x);
            }
            zone.GetCell(X+1,Y).IsVisible=false;Frame(View().ForegroundTile,"Sprites/Live16x24/nella-corner",13,13);
            zone.GetCell(X+1,Y).IsVisible=true;Add("Player",X+1,Y);Frame(View().ForegroundTile,"Sprites/Live16x24/nella-corner",13,13);
        }
        [TestCase("CopperPipe")][TestCase("PeatBog")][TestCase("SteamVent")]
        public void FixtureGroundKeepsActualHazardMarksAndSourceRemovalFallback(string role)
        {
            Add("Grass");var owner=Add(role);using(var paint=new Paint(world,zone))
            {
                foreach(string coating in new[]{"water","oil","ice"})
                {
                    zone.TileState.WriteCoating(X,Y,coating,4);paint.Dirty(X,Y);paint.Step();
                    Frame(paint.GroundAt(X,Y),Turf,0,3);Assert.NotNull(paint.Mark(X,Y),coating+" mark must remain above native floor");
                    zone.TileState.RemoveCoating(X,Y,coating);
                }
                foreach(string cue in new[]{"heat","cold","charge","cloud"})
                {
                    zone.TileState.Clear(X,Y);
                    if(cue=="heat")zone.TileState.AddHeat(X,Y,2);if(cue=="cold")zone.TileState.AddCold(X,Y,2);
                    if(cue=="charge")zone.TileState.AddCharge(X,Y,2);if(cue=="cloud")zone.TileState.WriteCloud(X,Y,"marsh-gas",4);
                    paint.Dirty(X,Y);paint.Step();Frame(paint.GroundAt(X,Y),Turf,0,3);Assert.NotNull(paint.Mark(X,Y),cue);
                }
                zone.TileState.Clear(X,Y);zone.TileState.WriteCoating(X,Y,"water",4);Assert.True(View().LocalGround);
                zone.RemoveEntity(owner);
                // Removing PeatBog clears its projected water. Write an independent
                // remaining film explicitly so this tests residual-state fallback.
                zone.TileState.WriteCoating(X,Y,"water",4);paint.Dirty(X,Y);paint.Step();Assert.False(View().LocalGround);Assert.NotNull(paint.Mark(X,Y));
            }
        }
        [TestCase("CopperPipe")][TestCase("PeatBog")][TestCase("SteamVent")]
        public void InvalidFixtureCannotAuthorizeDryGroundOverItsHazardState(string role)
        {
            Add("Grass");var owner=Add(role);zone.TileState.WriteCoating(X,Y,"water",3);Assert.True(View().LocalGround);
            var r=owner.GetPart<RenderPart>();r.VisualID="other";Assert.False(View().LocalGround);r.VisualID=null;Assert.True(View().LocalGround);
            r.Visible=false;Assert.False(View().LocalGround);r.Visible=true;
            var conflict=Add("WaterPuddle");Assert.False(View().LocalGround);zone.RemoveEntity(conflict);Assert.True(View().LocalGround);
            owner.GetPart<MaterialPart>().MaterialID="Wood";Assert.False(View().LocalGround);
        }
        [Test] public void MissingPipePeatAtlasLeavesGlyphsButDoesNotBorrowPixelsOrSuppressHazards()
        {
            Add("Grass");var owner=Add("CopperPipe");var selector=new FlatPixelFallbackCatalog();
            using(var empty=new FlatPixelNellaCornerCatalog(null,selector))
            {
                Write(world,"_nellaCornerArt",empty);Assert.False(View().NativeForeground);Assert.True(View().LocalGround);
                Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var sprite,out _));Assert.AreNotSame(Art("Sprites/Live16x24/nella-corner"),sprite.texture);
                zone.TileState.WriteCoating(X,Y,"oil",3);using(var paint=new Paint(world,zone)){paint.Step();Assert.NotNull(paint.Mark(X,Y));Frame(paint.GroundAt(X,Y),Turf,0,3);}
                Write(world,"_nellaCornerArt",null);
            }
            selector.Dispose();
        }
        sealed class Probe:Part{public int Renders;public override bool HandleEvent(GameEvent e){if(e.ID=="Render")Renders++;return true;}}
        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject root;
            readonly Tilemap overlay, main, ground;
            public ZoneRenderer Renderer { get; }
            public Transform RootTransform => root.transform;
            public Tilemap Main => main;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", S);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            public Paint(FlatPixelGreyboxCatalog catalog, Zone z)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous)) { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    saved = new HotbarSaveFixture(); root = new GameObject("Spread environment grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
                    main = saved.Root.GetComponent<Tilemap>(); var bg = new GameObject("Background", typeof(Tilemap)); bg.transform.SetParent(root.transform, false); ground = bg.GetComponent<Tilemap>();
                    var marks = new GameObject("Hazards", typeof(Tilemap)); marks.transform.SetParent(root.transform, false);
                    Renderer = saved.Renderer; Write(Renderer, "_tilemap", main); Write(Renderer, "_bgTilemap", ground); Write(Renderer, "_tileStateTilemap", marks.GetComponent<Tilemap>());
                    var camera = new GameObject("Camera", typeof(Camera)); camera.transform.SetParent(root.transform, false); camera.GetComponent<Camera>().orthographic = true; Write(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(root.transform, main, ground); env.RenderingEnabled = true; env.FlatPixelGreyboxEnabled = true;
                    Write(env, "_flatPixelGreybox", catalog); overlay = (Tilemap)Read(env, "_overlayTilemap"); Write(Renderer, "_envSpriteRenderer", env); Write(Renderer, "_flatPixelModeEnabled", true); Renderer.SetZone(z);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                }
                catch { Dispose(); throw; }
            }
            static Vector3Int Position(int x, int y) => new Vector3Int(x, Zone.Height - 1 - y, 0);
            public Tile At(int x, int y) { var at = Position(x, y); return overlay.GetTile<Tile>(at) ?? main.GetTile<Tile>(at); }
            public Tile GroundAt(int x,int y) => ground.GetTile<Tile>(Position(x,y));
            public Matrix4x4 Transform(int x, int y) { var at = Position(x, y); return overlay.HasTile(at) ? overlay.GetTransformMatrix(at) : main.GetTransformMatrix(at); }
            public Color Tint(int x, int y) { var at = Position(x, y); return overlay.HasTile(at) ? overlay.GetColor(at) : main.GetColor(at); }
            public Color Lit(Color color, int x, int y) { var light = (LightMap)Read(Renderer, "_lightMap"); return light == null ? color : light.ApplyToColor(color, x, y); }
            public Tile Mark(int x,int y) => ((Tilemap)Read(Renderer,"_tileStateTilemap")).GetTile<Tile>(Position(x,y));
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Spread environment fixture");
            public void Step() { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public void Dispose()
            {
                try
                {
                    // The fixture owns this catalog across regions/save checks.
                    // EnvironmentSpriteRenderer owns its field in production,
                    // so detach this borrowed test injection before teardown.
                    if (saved?.Root != null)
                    {
                        var environment = saved.Root.GetComponent<EnvironmentSpriteRenderer>();
                        if (environment != null) Write(environment, "_flatPixelGreybox", null);
                    }
                    saved?.Dispose(); if (root != null) Object.DestroyImmediate(root);
                }
                finally
                {
                    ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                    foreach (var entry in perf) entry.Key.SetValue(null, entry.Value);
                }
            }
        }
    }
}
