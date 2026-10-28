// Shared stone-body contract; runtime implementation follows native RED.
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
    public sealed class FlatPixelSharedStonePropsTests
    {
        const int X=10,Y=10;
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        const string PillarPath="Sprites/Environment/pillar", BasePath="Sprites/Live16x24/opening-base";
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone; FlatPixelGreyboxCatalog world;
        readonly List<Texture2D> borrowed=new List<Texture2D>();
        EntityFactory oldDestruction; EntityDamageVisualHandler oldDamage; EntityMovedVisualHandler oldMoved;
        Action<string> oldMessage; Action<WorldAudioCombatReceipt> oldAudio;
        Action<int,int,string> oldDirty; Action<string> oldFull;
        [SetUp] public void Setup()
        {
            oldDestruction=DestructionSystem.EntityFactoryRef;oldDamage=EntityVisualHooks.DamageCallback;oldMoved=EntityVisualHooks.MovedCallback;
            oldMessage=MessageLog.OnMessage;oldAudio=WorldAudioCombat.Callback;oldDirty=ZoneRenderHooks.CellDirtyCallback;oldFull=ZoneRenderHooks.FullDirtyCallback;
            EntityVisualHooks.DamageCallback=null;EntityVisualHooks.MovedCallback=null;MessageLog.OnMessage=null;WorldAudioCombat.Callback=null;ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;
            content=new DensityLootTestScope();DestructionSystem.EntityFactoryRef=content.Factory;
            manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);zone=Attach("Overworld.16.13.9");world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try{world?.Dispose();foreach(var t in borrowed)if(t!=null)Object.DestroyImmediate(t);content?.Dispose();}
            finally{DestructionSystem.EntityFactoryRef=oldDestruction;EntityVisualHooks.DamageCallback=oldDamage;EntityVisualHooks.MovedCallback=oldMoved;MessageLog.OnMessage=oldMessage;WorldAudioCombat.Callback=oldAudio;ZoneRenderHooks.CellDirtyCallback=oldDirty;ZoneRenderHooks.FullDirtyCallback=oldFull;}
        }
        Zone Attach(string id){var z=new Zone(id);manager.SetActiveZone(z);foreach(var c in z.Cells)c.IsVisible=c.Explored=true;return z;}
        Entity Add(string name,int x=X,int y=Y){var e=content.Factory.CreateEntity(name);Assert.NotNull(e);Assert.True(zone.AddEntity(e,x,y));return e;}
        FlatPixelGreyboxCell View(int x=X,int y=Y){Assert.True(world.TryResolvePresentation(zone,x,y,out var v));return v;}
        static object Read(object owner,string field){var f=owner.GetType().GetField(field,I);Assert.NotNull(f,field);return f.GetValue(owner);}
        static void Write(object owner,string field,object value){var f=owner.GetType().GetField(field,I);Assert.NotNull(f,field);f.SetValue(owner,value);}
        static Texture2D Resource(string name){var t=Resources.Load<Texture2D>(name=="Pillar"?PillarPath:BasePath);Assert.NotNull(t);return t;}
        static Rect BodyRect(string name)=>name=="Pillar"?new Rect(0,0,16,16):new Rect(96,0,16,24);
        static void Native(Tile tile,string name,Texture2D expected=null)
        {
            Assert.NotNull(tile);Assert.NotNull(tile.sprite);Assert.AreSame(expected??Resource(name),tile.sprite.texture);Assert.AreEqual(BodyRect(name),tile.sprite.rect);
            int h=name=="Pillar"?16:24;Assert.AreEqual(new Vector2(8,h/2f),tile.sprite.pivot);Assert.AreEqual(16,tile.sprite.pixelsPerUnit);
            Assert.AreEqual(new Vector2(1,h/16f),(Vector2)tile.sprite.bounds.size);Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);Assert.AreEqual(TileFlags.None,tile.flags);
        }
        void NativeOwner(Entity owner,Texture2D expected=null)
        {var c=zone.GetEntityCell(owner);Assert.NotNull(c);var v=View(c.X,c.Y);Assert.AreSame(owner,v.Owner);Assert.True(v.NativeForeground);Assert.AreEqual(owner.BlueprintName=="Pillar",v.BlocksMovement);Native(v.ForegroundTile,owner.BlueprintName,expected);}
        static Type ArtType(){var t=typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelSharedStonePropsCatalog");Assert.NotNull(t,"Missing shared stone-prop adapter");return t;}
        delegate bool Query(Entity owner,out Tile tile);
        static IDisposable Construct(Texture2D pillar,Texture2D opening)
        {var c=ArtType().GetConstructor(new[]{typeof(Texture2D),typeof(Texture2D)});Assert.NotNull(c);return (IDisposable)c.Invoke(new object[]{pillar,opening});}
        static Query Resolve(IDisposable art)=>(Query)Delegate.CreateDelegate(typeof(Query),art,ArtType().GetMethod("TryResolve",new[]{typeof(Entity),typeof(Tile).MakeByRefType()}));
        Texture2D Texture(int w,int h){var t=new Texture2D(w,h);borrowed.Add(t);return t;}
        AnnouncementDocument Document(Entity e,Entity viewer=null){var d=ExamineDocumentBuilder.BuildWorld(e,zone,zone.GetEntityCell(e),viewer);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(d));return d;}
        static ExaminePortraitRenderer Portraits()=>(ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer),I,null,new object[]{(Action<Camera,RenderTexture>)((c,t)=>{})},null);

        [TestCase("Pillar")] [TestCase("BrokenColumn")]
        public void ExactWorldAndAuthorizedReadersReuseBodiesAcrossRegions(string name)
        {
            foreach(var id in new[]{"Overworld.16.13.9","Overworld.12.11.0","StandaloneStoneProps"})
            {
                zone=Attach(id);var e=Add(name);var viewer=Add("Player",X-1,Y);NativeOwner(e);int version=zone.EntityVersion;
                using(var paint=new Paint(world,zone))using(var portraits=Portraits())
                {
                    paint.Step();Native(paint.At(X,Y),name);Assert.AreEqual(Color.white,paint.Tint(X,Y));Assert.AreEqual(Matrix4x4.identity,paint.Transform(X,Y));
                    var p=portraits.GetPortrait(Document(e,viewer),paint.Renderer);var t=Resource(name);var rect=BodyRect(name);
                    Assert.AreSame(t,p.Texture);Assert.False(p.IsModel);Assert.AreEqual(new Rect(rect.x/t.width,0,16f/t.width,rect.height/t.height),p.Uv);Assert.AreEqual(version,zone.EntityVersion);
                }
            }
        }
        [Test] public void StructuralAndTerrainContractsRemainDifferent()
        {
            var p=Add("Pillar");NativeOwner(p);var s=p.GetPart<DestructiblePart>();Assert.AreEqual(50,s.HP);Assert.AreEqual(50,s.MaxHP);Assert.AreEqual(4,s.Hardness);Assert.AreEqual("Rubble",s.WreckageBlueprint);
            Assert.AreEqual("Stone",p.GetPart<MaterialPart>().MaterialID);Assert.NotNull(p.GetPart<ThermalPart>());Assert.IsNull(p.GetStat("Hitpoints"));Assert.True(zone.GetCell(X,Y).BlocksMovement());
            Assert.AreEqual("I",p.GetPart<RenderPart>().RenderString);Assert.AreEqual("&w",p.GetPart<RenderPart>().ColorString);Assert.AreEqual(1,p.GetPart<RenderPart>().RenderLayer);
            foreach(int hp in new[]{50,49,1}){s.HP=hp;NativeOwner(p);}foreach(int hp in new[]{0,51}){s.HP=hp;using(var a=Construct(Resource("Pillar"),Resource("BrokenColumn")))Assert.False(Resolve(a)(p,out _));}s.HP=50;
            var b=Add("BrokenColumn",X+2,Y);NativeOwner(b);Assert.False(zone.GetCell(X+2,Y).BlocksMovement());Assert.True(b.HasTag("Terrain"));Assert.False(b.HasPart<MaterialPart>());Assert.False(b.HasPart<ThermalPart>());Assert.False(b.HasPart<DestructiblePart>());
            Assert.AreEqual(",",b.GetPart<RenderPart>().RenderString);Assert.AreEqual("&w",b.GetPart<RenderPart>().ColorString);Assert.AreEqual(0,b.GetPart<RenderPart>().RenderLayer);
        }
        [Test] public void RealPillarDamageAndDestructionProduceRubbleNotBrokenColumn()
        {
            var floor=Add("QuartziteFloor");var p=Add("Pillar");var actor=Add("Player",X-1,Y);NativeOwner(p);int hp=actor.GetStatValue("Hitpoints");
            using(var paint=new Paint(world,zone))
            {
                paint.Step();var body=paint.At(X,Y);Assert.AreEqual(DestroyVerdict.Damaged,DestructionSystem.Damage(p,5,actor,zone));Assert.AreEqual(49,p.GetPart<DestructiblePart>().HP);paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));NativeOwner(p);
                zone.TileState.AddHeat(X,Y,1);string state=zone.TileState.ToSaveString();Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(p,999,actor,zone));paint.Step();
                Assert.IsNull(zone.GetEntityCell(p));var rubble=zone.GetCell(X,Y).Objects.Single(e=>e.BlueprintName=="Rubble");Assert.NotNull(rubble);Assert.False(zone.GetCell(X,Y).Objects.Any(e=>e.BlueprintName=="BrokenColumn"));
                Assert.False(zone.GetCell(X,Y).BlocksMovement());Assert.Contains(floor,zone.GetCell(X,Y).Objects);Assert.AreEqual(state,zone.TileState.ToSaveString());Assert.AreEqual(hp,actor.GetStatValue("Hitpoints"));Assert.AreNotSame(body,paint.At(X,Y));
            }
        }
        [Test] public void BrokenColumnIsPassableNotBreakableAndDoesNotInventAnInteraction()
        {
            var b=Add("BrokenColumn");NativeOwner(b);var actor=Add("Player",X-1,Y);var probe=new EventProbe();b.AddPart(probe);
            Assert.AreEqual(DestroyVerdict.NotBreakable,DestructionSystem.Damage(b,999,actor,zone));NativeOwner(b);Assert.True(zone.MoveEntity(actor,X,Y));Assert.AreSame(actor,View().Owner);
            // An explicit current document can still examine a disclosed prop under an actor;
            // world selection and semantic portrait lookup are different contracts.
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {paint.Step();Assert.AreSame(Resource("BrokenColumn"),portraits.GetPortrait(Document(b,actor),paint.Renderer).Texture);}
            Assert.True(zone.MoveEntity(actor,X+1,Y));NativeOwner(b);Assert.AreEqual(0,probe.IdleQueries);Assert.AreEqual(0,probe.InventoryActions);
        }
        [TestCase("identity")] [TestCase("ownership")] [TestCase("custody")] [TestCase("footprint")]
        public void RefusedNeutralOwnersStayRefusedThroughPublicWorldAndReader(string group)
        {
            var faults=group=="identity"?new[]{"glyph","color","visual","variant","layer"}:group=="ownership"?new[]{"foreign-render","duplicate-physics","foreign-examinable"}:group=="custody"?new[]{"carried","equipped","takeable"}:new[]{"multicell"};
            foreach(var name in new[]{"Pillar","BrokenColumn"})foreach(var fault in faults)
            {
                zone=Attach("Overworld.12.11.0");var e=Add(name);NativeOwner(e);var r=e.GetPart<RenderPart>();var p=e.GetPart<PhysicsPart>();
                switch(fault)
                {
                    case "glyph":r.RenderString="!";break;case "color":r.ColorString="&R";break;case "visual":r.VisualID="unapproved";break;case "variant":r.VisualVariant="different";break;case "layer":r.RenderLayer=9;break;
                    case "foreign-render":r.ParentEntity=new Entity();break;case "duplicate-physics":e.AddPart(new PhysicsPart());break;case "foreign-examinable":e.GetPart<ExaminablePart>().ParentEntity=new Entity();break;
                    case "carried":p.InInventory=new Entity();break;case "equipped":p.Equipped=new Entity();break;case "takeable":p.Takeable=true;break;
                    case "multicell":e.AddPart(new SpatialFootprintPart{CellsRaw="0,0"});Assert.True(zone.TryChangeFootprint(e,"0,0;1,0"));break;
                }
                using(var a=Construct(Resource("Pillar"),Resource("BrokenColumn")))Assert.False(Resolve(a)(e,out _),name+fault);
                Assert.False(View().NativeForeground,name+fault);
                if(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var sprite,out _))Assert.AreNotSame(Resource(name),sprite.texture,name+fault);
            }
        }
        [Test] public void PillarAndBrokenColumnCannotBorrowEachOthersMechanicsOrFakeServices()
        {
            foreach(var fault in new[]{"solid","material","hardness","hp","thermal","container"})
            {
                zone=Attach("Overworld.16.13.9");var p=Add("Pillar");NativeOwner(p);
                if(fault=="solid")p.GetPart<PhysicsPart>().Solid=false;if(fault=="material")p.GetPart<MaterialPart>().MaterialID="Wood";if(fault=="hardness")p.GetPart<DestructiblePart>().Hardness=5;if(fault=="hp")p.GetPart<DestructiblePart>().MaxHP=55;if(fault=="thermal")p.GetPart<ThermalPart>().Temperature=float.NaN;if(fault=="container")p.AddPart(new ContainerPart());
                using(var a=Construct(Resource("Pillar"),Resource("BrokenColumn")))Assert.False(Resolve(a)(p,out _));Assert.False(View().NativeForeground);
            }
            foreach(var fault in new[]{"solid","material","structural","service"})
            {
                zone=Attach("Overworld.16.13.9");var b=Add("BrokenColumn");NativeOwner(b);if(fault=="solid")b.GetPart<PhysicsPart>().Solid=true;if(fault=="material")b.AddPart(new MaterialPart{MaterialID="Stone"});if(fault=="structural")b.AddPart(new DestructiblePart());if(fault=="service")b.AddPart(new HarvestablePart());
                using(var a=Construct(Resource("Pillar"),Resource("BrokenColumn")))Assert.False(Resolve(a)(b,out _));Assert.False(View().NativeForeground);
            }
        }
        [TestCase(0,false)] [TestCase(0,true)] [TestCase(1,false)] [TestCase(1,true)]
        public void OptionalInputGroupsFailIndependentlyWithoutImplicitReload(int missing,bool malformed)
        {
            var pillar=Texture(16,16);var opening=Texture(160,24);var inputs=new[]{pillar,opening};var bad=malformed?Texture(3,3):null;
            using(var positive=Construct(pillar,opening))foreach(var name in new[]{"Pillar","BrokenColumn"}){Assert.True(Resolve(positive)(content.Factory.CreateEntity(name),out var tile));Native(tile,name,name=="Pillar"?pillar:opening);}
            inputs[missing]=bad;world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);Write(world,"_sharedStonePropsArt",Construct(inputs[0],inputs[1]));
            foreach(var name in new[]{"Pillar","BrokenColumn"})
            {
                zone=Attach("Overworld.12.11.0");var e=Add(name);if((name=="Pillar"?0:1)==missing){Assert.False(View().NativeForeground);if(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var sprite,out _))Assert.AreNotSame(Resource(name),sprite.texture);}
                else NativeOwner(e,name=="Pillar"?pillar:opening);
            }
        }
        [Test] public void AcceptedExplicitGladeBrokenColumnRemainsOnOriginalFrameEvenWithoutSharedResources()
        {
            zone=Attach(ReferenceGladePlan.ZoneID);Assert.True(ReferenceGladePlan.IsActive(zone));var b=Add("BrokenColumn");b.GetPart<RenderPart>().VisualID="reference-glade-dark-ruin";
            NativeOwner(b);var before=View().ForegroundTile.sprite.rect;using(var a=Construct(Resource("Pillar"),Resource("BrokenColumn")))Assert.False(Resolve(a)(b,out _),"Neutral shared policy must not globally admit authored variants.");
            (Read(world,"_sharedStonePropsArt") as IDisposable)?.Dispose();Write(world,"_sharedStonePropsArt",Construct(null,null));NativeOwner(b);Assert.AreEqual(before,View().ForegroundTile.sprite.rect);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,b,X,Y,out var sprite,out _));Assert.AreSame(Resource("BrokenColumn"),sprite.texture);Assert.AreEqual(new Rect(96,0,16,24),sprite.rect);
            b.GetPart<RenderPart>().VisualID="unapproved-column";Assert.False(View().NativeForeground);
        }
        [Test] public void CurrentDisclosureAndCoverRetireReadersWithoutChangingUnderlyingProps()
        {
            var b=Add("BrokenColumn");NativeOwner(b);var d=Document(b);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();var body=paint.At(X,Y);zone.GetCell(X,Y).IsVisible=false;paint.Dirty(X,Y);paint.Step();Assert.False(View().NativeForeground);Assert.IsNull(portraits.GetPortrait(d,paint.Renderer).Texture);
                zone.GetCell(X,Y).Explored=false;Assert.False(world.TryResolvePresentation(zone,X,Y,out _));zone.GetCell(X,Y).Explored=zone.GetCell(X,Y).IsVisible=true;
                b.GetPart<RenderPart>().Visible=false;Assert.False(View().NativeForeground);b.GetPart<RenderPart>().Visible=true;NativeOwner(b);
                var actor=Add("Player");paint.Dirty(X,Y);paint.Step();Assert.AreSame(actor,View().Owner);Assert.AreNotSame(body,paint.At(X,Y));Assert.True(zone.RemoveEntity(actor));paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));
                Assert.True(zone.RemoveEntity(b));paint.Dirty(X,Y);paint.Step();Assert.IsNull(portraits.GetPortrait(d,paint.Renderer).Texture);Assert.AreNotSame(body,paint.At(X,Y));
            }
        }
        [TestCase(true)] [TestCase(false)]
        public void SameLayerFloorOrderRetainsRealSelectionAndDirtyActorCover(bool floorFirst)
        {
            Entity floor,broken;
            if(floorFirst){floor=Add("QuartziteFloor");broken=Add("BrokenColumn");}
            else{broken=Add("BrokenColumn");floor=Add("QuartziteFloor");}
            Assert.AreEqual(floor.GetPart<RenderPart>().RenderLayer,broken.GetPart<RenderPart>().RenderLayer);
            Assert.True(world.TryResolveFallback(zone,X,Y,out var selected));Assert.That(selected.Owner,Is.SameAs(floor).Or.SameAs(broken));
            var actualSelected=selected.Owner;Assert.AreSame(actualSelected,View().Owner);
            if(ReferenceEquals(actualSelected,broken))NativeOwner(broken);
            string state=zone.TileState.ToSaveString();
            using(var paint=new Paint(world,zone))
            {
                paint.Step();var before=paint.At(X,Y);var actor=Add("Player");paint.Dirty(X,Y);paint.Step();Assert.AreSame(actor,View().Owner);Assert.AreNotSame(before,paint.At(X,Y));
                Assert.True(zone.RemoveEntity(actor));paint.Dirty(X,Y);paint.Step();Assert.AreSame(actualSelected,View().Owner);Assert.AreSame(before,paint.At(X,Y));
                Assert.True(zone.RemoveEntity(floor));paint.Dirty(X,Y);paint.Step();NativeOwner(broken);Native(paint.At(X,Y),"BrokenColumn");
                Assert.AreEqual(state,zone.TileState.ToSaveString());Assert.NotNull(zone.GetEntityCell(broken));Assert.False(zone.GetCell(X,Y).BlocksMovement());
            }
        }
        [TestCase(1,"SandstoneFloor",FlatPixelBeatingMaterialsCatalog.ResourcePath,12)]
        [TestCase(3,"LimestoneFloor",FlatPixelLimestoneCavesCatalog.ResourcePath,0)]
        [TestCase(5,"ShaleFloor",FlatPixelShaleCavesCatalog.ResourcePath,0)]
        [TestCase(7,"SlateFloor",FlatPixelSlateCavesCatalog.ResourcePath,0)]
        [TestCase(9,"QuartziteFloor",FlatPixelQuartziteCavesCatalog.ResourcePath,0)]
        [TestCase(11,"ObsidianFloor",FlatPixelObsidianCavesCatalog.ResourcePath,0)]
        public void NeutralBrokenColumnPreservesActualCaveFloorWhileForeignMeaningAndCoatingStillRefuse(int depth,string floorName,string resource,int firstFrame)
        {
            zone=Attach(WorldMap.ToZoneID(0,9,depth));Assert.IsNull(manager.WorldMap.GetPOI(0,9));
            var floor=Add(floorName);var expected=Resources.Load<Texture2D>(resource);Assert.NotNull(expected,resource);
            void AssertFloor()
            {
                var v=View();Assert.True(v.LocalGround,"Real native floor must remain under a neutral fragment.");Assert.False(v.AmbientBackdrop);
                Assert.NotNull(v.GroundTile);Assert.AreSame(expected,v.GroundTile.sprite.texture);Assert.AreEqual(new Vector2(16,24),v.GroundTile.sprite.rect.size);
                Assert.That((int)v.GroundTile.sprite.rect.x/16,Is.InRange(firstFrame,firstFrame+3));Assert.AreEqual(Color.white,v.GroundColor);
                Assert.Contains(floor,zone.GetCell(X,Y).Objects);
            }
            AssertFloor();var broken=Add("BrokenColumn");NativeOwner(broken);AssertFloor();
            using(var paint=new Paint(world,zone))
            {paint.Step();Assert.AreSame(expected,paint.GroundAt(X,Y).sprite.texture);Native(paint.At(X,Y),"BrokenColumn");}
            var material=new MaterialPart{MaterialID="Stone"};broken.AddPart(material);Assert.False(View().LocalGround);Assert.False(View().AmbientBackdrop);
            Assert.True(broken.RemovePart(material));NativeOwner(broken);AssertFloor();
            var service=new HarvestablePart();broken.AddPart(service);Assert.False(View().LocalGround);Assert.False(View().AmbientBackdrop);
            Assert.True(broken.RemovePart(service));NativeOwner(broken);AssertFloor();
            zone.TileState.WriteCoating(X,Y,"water",ZoneTileState.Permanent);Assert.False(zone.TileState.Get(X,Y).IsEmpty);
            string state=zone.TileState.ToSaveString();Assert.False(View().LocalGround);Assert.False(View().AmbientBackdrop);Assert.AreEqual(state,zone.TileState.ToSaveString());
            Assert.Contains(floor,zone.GetCell(X,Y).Objects);Assert.AreSame(broken,View().Owner);
        }
        [Test] public void CurrentPhysicalMembershipAndCachedGraphStayAuthoritative()
        {
            var b=Add("BrokenColumn");NativeOwner(b);Assert.True(zone.MoveEntity(b,X+1,Y));zone.GetCell(X,Y).Objects.Add(b);Assert.IsNull(View().Owner);NativeOwner(b);
            manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);Assert.False(world.TryResolvePresentation(zone,X+1,Y,out _));Assert.False(world.TryGetPresentationSubjectSprite(zone,b,X+1,Y,out _,out _));
        }
        [TestCase("Pillar")] [TestCase("BrokenColumn")]
        public void ActualRenderCueIsDispatchedOnceAndReaderNeighborQueriesDoNotConsumeIt(string name)
        {
            var e=Add(name);NativeOwner(e);var probe=new EventProbe();e.AddPart(probe);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();var body=paint.At(X,Y);int count=probe.Renders;probe.OverrideColor="&R";paint.Dirty(X,Y);paint.Step();Assert.AreEqual(count+1,probe.Renders);Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(QudColorParser.Parse("&R"),paint.Tint(X,Y));
                portraits.GetPortrait(Document(e),paint.Renderer);NativeOwner(e);paint.Dirty(X+1,Y);paint.Step();Assert.AreEqual(count+1,probe.Renders);Assert.AreEqual(QudColorParser.Parse("&R"),paint.Tint(X,Y));
                probe.OverrideColor=null;paint.Dirty(X,Y);paint.Step();Assert.AreEqual(count+2,probe.Renders);Assert.AreEqual(Color.white,paint.Tint(X,Y));
            }
        }
        [Test] public void CachedViewsAndDisposalLeaveBothBorrowedSourcesAndPeerAlive()
        {
            var p=Texture(16,16);var b=Texture(160,24);var art=Construct(p,b);
            using(var peer=Construct(p,b))try
            {
                var owner=content.Factory.CreateEntity("Pillar");Assert.True(Resolve(art)(owner,out var tile));Assert.True(Resolve(art)(owner,out var again));Assert.AreSame(tile,again);var sprite=tile.sprite;
                art.Dispose();art.Dispose();Assert.True(tile==null);Assert.True(sprite==null);Assert.False(Resolve(art)(owner,out _));Assert.True(Resolve(peer)(owner,out var other));Native(other,"Pillar",p);
                Assert.True(p!=null&&b!=null);Assert.AreEqual(FilterMode.Bilinear,p.filterMode);Assert.AreEqual(TextureWrapMode.Repeat,b.wrapMode);
            }
            finally{art.Dispose();}
        }
        [Test] public void OtherStoneNamesAndSpoofedSourcePropertiesDoNotAcquireEitherBody()
        {
            using(var a=Construct(Resource("Pillar"),Resource("BrokenColumn")))
            {
                Assert.True(Resolve(a)(content.Factory.CreateEntity("Pillar"),out _));
                foreach(var name in new[]{"Rubble","Wall","StoneDome","CreatureCorpse"}){var e=content.Factory.CreateEntity(name);Assert.NotNull(e);e.Properties["SourceBlueprint"]="Pillar";Assert.False(Resolve(a)(e,out _));}
            }
        }
        sealed class EventProbe : Part
        {
            public int Renders, IdleQueries, InventoryActions;
            public string OverrideColor;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "Render") { Renders++; if (OverrideColor != null) e.SetParameter("ColorString", OverrideColor); }
                if (e.ID == IdleQueryEvent.ID) IdleQueries++;
                if (e.ID == "InventoryAction" || e.ID == "GetInventoryActions") InventoryActions++;
                return true;
            }
        }
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Shared stone props submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public Matrix4x4 Transform(int x, int y) { var at = Position(x, y); return overlay.HasTile(at) ? overlay.GetTransformMatrix(at) : main.GetTransformMatrix(at); }
            public Color Tint(int x, int y) { var at = Position(x, y); return overlay.HasTile(at) ? overlay.GetColor(at) : main.GetColor(at); }
            public Color Lit(Color color, int x, int y) { var light = (LightMap)Read(Renderer, "_lightMap"); return light == null ? color : light.ApplyToColor(color, x, y); }
            public Tile GroundAt(int x, int y) => ground.GetTile<Tile>(Position(x, y));
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Shared stone props fixture");
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
