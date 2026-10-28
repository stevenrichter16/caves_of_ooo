// Shared camp props; reflection fixture for genuine absent-adapter RED.
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
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelSharedCampPropsTests
    {
        const int X=10,Y=10;
        const string Sack="Sprites/Environment/cont_sack",Urn="Sprites/Environment/cont_urn",Well="Sprites/Environment/well";
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        static readonly string[] Names={"Sack","Urn","Well"};
        DensityLootTestScope content;OverworldZoneManager manager;Zone zone;FlatPixelGreyboxCatalog world;
        readonly List<Texture2D> borrowed=new List<Texture2D>();
        Action<string> oldMessage;Action<int,int,string> oldDirty;Action<string> oldFull;
        EntityDamageVisualHandler oldDamage;EntityMovedVisualHandler oldMoved;
        EntityFactory oldCorpse;Action<WorldAudioCombatReceipt> oldAudio;int oldEquipmentVersion;
        [SetUp] public void Setup()
        {
            oldEquipmentVersion=EquipmentChangeBus.GlobalVersion;oldMessage=MessageLog.OnMessage;oldDirty=ZoneRenderHooks.CellDirtyCallback;oldFull=ZoneRenderHooks.FullDirtyCallback;
            oldDamage=EntityVisualHooks.DamageCallback;oldMoved=EntityVisualHooks.MovedCallback;oldCorpse=CorpsePart.Factory;oldAudio=WorldAudioCombat.Callback;
            MessageLog.OnMessage=null;ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;
            EntityVisualHooks.DamageCallback=null;EntityVisualHooks.MovedCallback=null;WorldAudioCombat.Callback=null;
            content=new DensityLootTestScope();CorpsePart.Factory=content.Factory;
            manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);zone=Attach("Overworld.17.13.0");world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try{world?.Dispose();foreach(var t in borrowed)if(t!=null)Object.DestroyImmediate(t);borrowed.Clear();content?.Dispose();}
            finally{MessageLog.OnMessage=oldMessage;ZoneRenderHooks.CellDirtyCallback=oldDirty;ZoneRenderHooks.FullDirtyCallback=oldFull;
                EntityVisualHooks.DamageCallback=oldDamage;EntityVisualHooks.MovedCallback=oldMoved;CorpsePart.Factory=oldCorpse;WorldAudioCombat.Callback=oldAudio;typeof(EquipmentChangeBus).GetProperty("GlobalVersion").SetValue(null,oldEquipmentVersion);}
        }
        Zone Attach(string id){var z=new Zone(id);manager.SetActiveZone(z);foreach(var c in z.Cells)c.IsVisible=c.Explored=true;return z;}
        Entity Add(string name,int x=X,int y=Y){var e=content.Factory.CreateEntity(name);Assert.NotNull(e,name);Assert.True(zone.AddEntity(e,x,y));return e;}
        FlatPixelGreyboxCell View(int x=X,int y=Y){Assert.True(world.TryResolvePresentation(zone,x,y,out var v));return v;}
        static object Read(object o,string n){var f=o.GetType().GetField(n,I);Assert.NotNull(f,n);return f.GetValue(o);}
        static void Write(object o,string n,object value){var f=o.GetType().GetField(n,I);Assert.NotNull(f,n);f.SetValue(o,value);}
        static Texture2D Resource(string name){var t=Resources.Load<Texture2D>(name=="Sack"?Sack:name=="Urn"?Urn:Well);Assert.NotNull(t);return t;}
        static void Native(Tile tile,string name,Texture2D texture=null)
        {
            const int h=16;Assert.NotNull(tile);Assert.NotNull(tile.sprite);Assert.AreSame(texture??Resource(name),tile.sprite.texture);
            Assert.AreEqual(new Rect(0,0,16,h),tile.sprite.rect);Assert.AreEqual(new Vector2(8,h/2f),tile.sprite.pivot);Assert.AreEqual(16,tile.sprite.pixelsPerUnit);
            Assert.AreEqual(new Vector2(1,h/16f),(Vector2)tile.sprite.bounds.size);Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);Assert.AreEqual(TileFlags.None,tile.flags);
        }
        Tile NativeOwner(Entity e,Texture2D texture=null)
        {var c=zone.GetEntityCell(e);Assert.NotNull(c);var v=View(c.X,c.Y);Assert.AreSame(e,v.Owner);Assert.True(v.NativeForeground);Native(v.ForegroundTile,e.BlueprintName,texture);return v.ForegroundTile;}
        static Type ArtType(){var t=typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelSharedCampPropsCatalog");Assert.NotNull(t,"Missing shared camp props adapter");return t;}
        delegate bool Query(Entity owner,out Tile tile);
        static IDisposable Construct(Texture2D sack,Texture2D urn,Texture2D well){var c=ArtType().GetConstructor(new[]{typeof(Texture2D),typeof(Texture2D),typeof(Texture2D)});Assert.NotNull(c);return (IDisposable)c.Invoke(new object[]{sack,urn,well});}
        static Query Resolve(IDisposable art)=>(Query)Delegate.CreateDelegate(typeof(Query),art,ArtType().GetMethod("TryResolve",new[]{typeof(Entity),typeof(Tile).MakeByRefType()}));
        Texture2D Texture(int w,int h){var t=new Texture2D(w,h);borrowed.Add(t);return t;}
        AnnouncementDocument Document(Entity e,Entity viewer=null){var d=ExamineDocumentBuilder.BuildWorld(e,zone,zone.GetEntityCell(e),viewer);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(d));return d;}
        static ExaminePortraitRenderer Portraits()=>(ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer),I,null,new object[]{(Action<Camera,RenderTexture>)((c,t)=>{})},null);
        void Refused(Entity e,string why)
        {
            using(var a=Construct(Resource("Sack"),Resource("Urn"),Resource("Well"))){Assert.False(Resolve(a)(e,out var tile),why);Assert.IsNull(tile,why);}
            Assert.False(View().NativeForeground,why);
            if(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var sprite,out _))Assert.AreNotSame(Resource(e.BlueprintName),sprite.texture,why);
            if(FlatPixelGreyboxCatalog.IsAdmitted(zone))
            {
                Assert.True(world.TryResolve(zone,X,Y,out var old));Assert.AreNotSame(Resource(e.BlueprintName),old.ForegroundTile?.sprite?.texture,why);
                if(world.TryGetSubjectSprite(e,X,Y,out sprite,out _))Assert.AreNotSame(Resource(e.BlueprintName),sprite.texture,why);
            }
        }


        static void Act(Entity actor,Entity target,Zone z,string command)
        {var e=GameEvent.New("InventoryAction");e.SetParameter("Actor",(object)actor);e.SetParameter("Zone",(object)z);e.SetParameter("Command",command);target.FireEventAndRelease(e);}
        [TestCase("Sack")] [TestCase("Urn")] [TestCase("Well")]
        public void ExactBodiesMatchSubmittedWorldAndAuthorizedReadersAcrossContexts(string name)
        {
            foreach(var id in new[]{"Overworld.12.11.0","Overworld.17.13.0","Overworld.10.10.0","StandaloneCampProps"})
            {
                zone=Attach(id);var e=Add(name);var viewer=Add("Player",X-1,Y);var body=NativeOwner(e);var probe=new Probe();e.AddPart(probe);
                using(var paint=new Paint(world,zone))using(var portraits=Portraits())
                {
                    paint.Step();Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(Color.white,paint.Tint(X,Y));Assert.AreEqual(Matrix4x4.identity,paint.Transform(X,Y));
                    int rendered=probe.Renders;var p=portraits.GetPortrait(Document(e,viewer),paint.Renderer);Assert.AreSame(Resource(name),p.Texture);Assert.False(p.IsModel);Assert.AreEqual(new Rect(0,0,1,1),p.Uv);
                    NativeOwner(e);Assert.AreEqual(rendered,probe.Renders);Assert.Zero(probe.ServiceQueries);
                }
            }
        }
        [TestCase("missing")] [TestCase("foreign")] [TestCase("duplicate")]
        public void EssentialPartsMustBeUniqueAndOwnedBeforeLocalFallback(string fault)
        {
            foreach(var name in Names)foreach(var type in new[]{typeof(RenderPart),typeof(PhysicsPart),typeof(ExaminablePart),name=="Well"?typeof(WellPart):typeof(ContainerPart)})
            {
                zone=Attach("Overworld.12.11.0");var e=Add(name);NativeOwner(e);var part=e.Parts.First(p=>p.GetType()==type);
                if(fault=="missing")Assert.True(e.RemovePart(part));else if(fault=="foreign")part.ParentEntity=new Entity();else e.AddPart((Part)Activator.CreateInstance(type));Refused(e,name+type.Name+fault);
            }
        }
        [TestCase("visual")] [TestCase("custody")]
        public void CustomIdentityAndCustodyRefuseEveryWorldAndReaderRoute(string group)
        {
            var faults=group=="visual"?new[]{"glyph","color","layer","background","visual","variant","variants"}:new[]{"carried","equipped","takeable"};
            foreach(var name in Names)foreach(var fault in faults)
            {
                zone=Attach("Overworld.12.11.0");var e=Add(name);NativeOwner(e);var r=e.GetPart<RenderPart>();var p=e.GetPart<PhysicsPart>();
                switch(fault){case "glyph":r.RenderString="?";break;case "color":r.ColorString="&R";break;case "layer":r.RenderLayer=9;break;case "background":r.BackgroundColor="^R";break;case "visual":r.VisualID="custom-camp-prop";break;case "variant":r.VisualVariant="quest";break;case "variants":r.GlyphVariants="?!";break;case "carried":p.InInventory=new Entity();break;case "equipped":p.Equipped=new Entity();break;case "takeable":p.Takeable=true;break;}
                Refused(e,name+fault);
            }
        }
        [Test] public void OptionalStackAndFootprintRequireLiveOwnedSingleCellData()
        {
            foreach(var name in Names)foreach(var fault in new[]{"empty-stack","foreign-stack","duplicate-stack","multicell","foreign-footprint","duplicate-footprint","foreign-effects","duplicate-effects"})
            {
                zone=Attach("Overworld.12.11.0");var e=Add(name);NativeOwner(e);
                var stack=new StackerPart{StackCount=2,MaxStack=99};e.AddPart(stack);var footprint=new SpatialFootprintPart{CellsRaw="0,0"};e.AddPart(footprint);NativeOwner(e);
                switch(fault)
                {
                    case "empty-stack":stack.StackCount=0;break;case "foreign-stack":stack.ParentEntity=new Entity();break;case "duplicate-stack":e.AddPart(new StackerPart());break;
                    case "multicell":Assert.True(zone.TryChangeFootprint(e,"0,0;1,0"));break;case "foreign-footprint":footprint.ParentEntity=new Entity();break;case "duplicate-footprint":Assert.Throws<InvalidOperationException>(()=>e.AddPart(new SpatialFootprintPart{CellsRaw="0,0"}));NativeOwner(e);continue;
                    case "foreign-effects":var effects=new StatusEffectsPart();e.AddPart(effects);effects.ParentEntity=new Entity();break;case "duplicate-effects":e.AddPart(new StatusEffectsPart());e.AddPart(new StatusEffectsPart());break;
                }
                Refused(e,name+fault);
            }
        }
        [Test] public void ForeignRolesAndSimilarNamesCannotBorrowCampPropBodies()
        {
            foreach(var name in Names)foreach(var type in new[]{typeof(BrainPart),typeof(HarvestablePart),typeof(TraderPart),typeof(TileStateSourcePart),typeof(DoorPart),name=="Well"?typeof(ContainerPart):typeof(WellPart)})
            {zone=Attach("Overworld.12.11.0");var e=Add(name);NativeOwner(e);e.AddPart((Part)Activator.CreateInstance(type));Refused(e,name+type.Name);}
            using(var a=Construct(Resource("Sack"),Resource("Urn"),Resource("Well")))
            {
                Assert.False((bool)ArtType().GetMethod("HasKnownRole").Invoke(null,new object[]{null}));
                foreach(var name in new[]{"Chest","Crate","GroveSeep","TankBrocchinia","CreatureCorpse","Player"})
                {var e=content.Factory.CreateEntity(name);Assert.NotNull(e);e.Properties["SourceBlueprint"]="Well";Assert.False((bool)ArtType().GetMethod("HasKnownRole").Invoke(null,new object[]{e}));Assert.False(Resolve(a)(e,out _));}
            }
        }
        [Test] public void AuthoredBodiesNeedNoInventedMaterialHealthOrDefaultStock()
        {
            foreach(var name in Names)
            {
                zone=Attach("Overworld.17.13.0");var e=Add(name);Assert.IsNull(e.GetPart<MaterialPart>());Assert.IsNull(e.GetPart<ThermalPart>());Assert.IsNull(e.GetPart<DestructiblePart>());Assert.IsNull(e.GetStat("Hitpoints"));var body=NativeOwner(e);
                e.GetPart<RenderPart>().DisplayName="A traveller's fixture";
                if(name!="Well"){var c=e.GetPart<ContainerPart>();c.MaxItems=12;Assert.True(c.AddItem(content.Factory.CreateEntity("Dagger")));c.Locked=true;}
                Assert.AreSame(body,NativeOwner(e));
            }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void MissingOrMalformedInputsAreIndependentAndCannotReloadThroughLocalFallback(int group)
        {
            foreach(bool malformed in new[]{false,true})
            {
                var inputs=new[]{Texture(16,16),Texture(16,16),Texture(16,16)};var original=(Texture2D[])inputs.Clone();
                using(var a=Construct(inputs[0],inputs[1],inputs[2]))for(int i=0;i<3;i++){Assert.True(Resolve(a)(content.Factory.CreateEntity(Names[i]),out var tile));Native(tile,Names[i],original[i]);}
                inputs[group]=malformed?Texture(16,24):null;world.Dispose();world=new FlatPixelGreyboxCatalog();Write(world,"_sharedCampPropsArt",Construct(inputs[0],inputs[1],inputs[2]));
                for(int i=0;i<3;i++)
                {
                    zone=Attach("Overworld.12.11.0");var e=Add(Names[i]);if(i!=group){NativeOwner(e,original[i]);continue;}
                    Assert.False(View().NativeForeground);Assert.True(world.TryResolve(zone,X,Y,out var old));Assert.AreNotSame(Resource(Names[i]),old.ForegroundTile?.sprite?.texture);
                    using(var paint=new Paint(world,zone))using(var portraits=Portraits()){paint.Step();Assert.AreNotSame(Resource(Names[i]),paint.At(X,Y)?.sprite?.texture);Assert.AreNotSame(original[i],paint.At(X,Y)?.sprite?.texture);Assert.AreNotSame(Resource(Names[i]),portraits.GetPortrait(Document(e),paint.Renderer).Texture);}
                }
            }
        }
        [Test] public void ExplicitEmptyGreyboxAndCompanionInputsDoNotConsultResources()
        {
            // Positive control first: this is not a missing-content test.
            foreach(var name in Names){zone=Attach("Overworld.17.13.0");NativeOwner(Add(name));}
            world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);
            using(var a=Construct(null,null,null))foreach(var name in Names)
            {
                zone=Attach("Overworld.12.11.0");var e=Add(name);Assert.False(Resolve(a)(e,out _));Assert.False(View().NativeForeground);
                using(var paint=new Paint(world,zone))using(var portraits=Portraits()){paint.Step();Assert.AreNotSame(Resource(name),paint.At(X,Y)?.sprite?.texture);Assert.AreNotSame(Resource(name),portraits.GetPortrait(Document(e),paint.Renderer).Texture);}
            }
        }
        [TestCase("Sack")] [TestCase("Urn")]
        public void ActualContainerOpenTakePutAndLockedRefusalNeverInventBodyStates(string name)
        {
            var e=Add(name);var body=NativeOwner(e);var actor=Add("Player",X-1,Y);actor.GetPart<InventoryPart>().MaxWeight=-1;var c=e.GetPart<ContainerPart>();Assert.IsEmpty(c.Contents);
            var opened=new OpenProbe();actor.AddPart(opened);Act(actor,e,zone,"OpenContainer");Assert.AreEqual(1,opened.Opens);
            var item=content.Factory.CreateEntity("Dagger");Assert.True(c.AddItem(item));c.Locked=true;Assert.False(InventorySystem.TakeFromContainer(actor,e,item));Assert.AreSame(e,item.GetPart<PhysicsPart>().InInventory);Assert.AreSame(body,NativeOwner(e));
            c.Locked=false;Act(actor,e,zone,"OpenContainer");Assert.AreEqual(2,opened.Opens);Assert.True(InventorySystem.TakeFromContainer(actor,e,item));Assert.AreSame(actor,item.GetPart<PhysicsPart>().InInventory);
            Assert.True(InventorySystem.PutInContainer(actor,e,item));Assert.AreEqual(1,InventorySystem.TakeAllFromContainer(actor,e));Assert.IsEmpty(c.Contents);Assert.AreSame(body,NativeOwner(e));
        }
        [Test] public void OrdinaryWellDrawCuresActualParchedWithoutHealingRefillingOrQueryMutation()
        {
            var e=Add("Well");var body=NativeOwner(e);var actor=Add("Player",X-1,Y);var hp=actor.GetStat("Hitpoints");hp.BaseValue=12;int str=actor.GetStat("Strength").Penalty,agi=actor.GetStat("Agility").Penalty;
            Assert.IsNull(e.GetPart<LiquidPoolPart>());Assert.True(e.GetPart<WellPart>().IsUsable);Assert.True(actor.ForceApplyEffect(new ParchedEffect(),null,zone));Assert.True(actor.HasEffect<ParchedEffect>());
            Assert.AreEqual(str+1,actor.GetStat("Strength").Penalty);Assert.AreEqual(agi+1,actor.GetStat("Agility").Penalty);var probe=new Probe();e.AddPart(probe);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();for(int i=0;i<3;i++){NativeOwner(e);portraits.GetPortrait(Document(e,actor),paint.Renderer);}Assert.True(actor.HasEffect<ParchedEffect>());Assert.Zero(probe.ServiceQueries);Assert.AreEqual(12,hp.BaseValue);
                Assert.True(WorldInteractionSystem.GatherActions(e,actor).Any(a=>a.Command=="DrawWaterAtWell"));Act(actor,e,zone,"DrawWaterAtWell");Assert.False(actor.HasEffect<ParchedEffect>());
                Assert.AreEqual(str,actor.GetStat("Strength").Penalty);Assert.AreEqual(agi,actor.GetStat("Agility").Penalty);Assert.AreEqual(12,hp.BaseValue);Assert.IsNull(e.GetPart<LiquidPoolPart>());Assert.False(zone.TileState.HasCoating(X,Y,"water"));Assert.AreSame(body,NativeOwner(e));
                Assert.True(actor.ForceApplyEffect(new ParchedEffect(),null,zone));Assert.True(MovementSystem.TryMove(actor,zone,-1,0));Act(actor,e,zone,"DrawWaterAtWell");Assert.True(actor.HasEffect<ParchedEffect>(),"Out-of-range service must remain unavailable.");
            }
        }
        [TestCase("site")] [TestCase("broken")] [TestCase("repaired")] [TestCase("pool")]
        public void SettlementRepairAndFinitePoolWellsStayOutsideOrdinaryBodyContract(string fault)
        {
            var e=Add("Well");NativeOwner(e);var actor=Add("Player",X-1,Y);
            switch(fault)
            {
                case "site":e.AddPart(new WellSitePart());break;
                case "broken":e.AddPart(new RepairablePart());Assert.False(e.GetPart<WellPart>().IsUsable);Assert.False(WorldInteractionSystem.GatherActions(e,actor).Any(a=>a.Command=="DrawWaterAtWell"));break;
                case "repaired":e.AddPart(new RepairablePart{Repaired=true});Assert.True(e.GetPart<WellPart>().IsUsable);break;
                case "pool":e.AddPart(new LiquidPoolPart{LiquidId="water",Volume=0});break;
            }
            Refused(e,fault);
        }
        [Test] public void LiveRenderCueFiresOnceWhileReadersAndOtherCellsRemainPure()
        {
            foreach(var name in Names)
            {
                zone=Attach("Overworld.17.13.0");var e=Add(name);var body=NativeOwner(e);var probe=new Probe();e.AddPart(probe);
                using(var paint=new Paint(world,zone))using(var portraits=Portraits())
                {
                    paint.Step();Assert.AreEqual(Color.white,paint.Tint(X,Y));int renders=probe.Renders;probe.Cue="&B";paint.Dirty(X,Y);paint.Step();Assert.AreEqual(renders+1,probe.Renders);Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(QudColorParser.Parse("&B"),paint.Tint(X,Y));
                    NativeOwner(e);Assert.AreSame(Resource(name),portraits.GetPortrait(Document(e),paint.Renderer).Texture);Assert.AreEqual(renders+1,probe.Renders);Assert.Zero(probe.ServiceQueries);
                    paint.Dirty(X+1,Y);paint.Step();Assert.AreEqual(renders+1,probe.Renders);probe.Cue=null;paint.Dirty(X,Y);paint.Step();Assert.AreEqual(Color.white,paint.Tint(X,Y));
                }
            }
        }
        [Test] public void VisibilityCurrentGraphAndDirtyCoverControlWorldAndAuthorizedReader()
        {
            var e=Add("Sack");var body=NativeOwner(e);var doc=Document(e);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();zone.GetCell(X,Y).IsVisible=false;paint.Dirty(X,Y);paint.Step();Assert.False(View().NativeForeground);Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);
                zone.GetCell(X,Y).Explored=false;Assert.False(world.TryResolvePresentation(zone,X,Y,out _));zone.GetCell(X,Y).IsVisible=zone.GetCell(X,Y).Explored=true;
                e.GetPart<RenderPart>().Visible=false;Assert.False(View().NativeForeground);e.GetPart<RenderPart>().Visible=true;NativeOwner(e);
                var player=Add("Player");paint.Dirty(X,Y);paint.Step();Assert.AreSame(player,View().Owner);Assert.AreNotSame(body,paint.At(X,Y));Assert.True(zone.RemoveEntity(player));paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));
                var old=zone;Attach(old.ZoneID);Assert.False(world.TryResolvePresentation(old,X,Y,out _));Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);
            }
        }
        [Test] public void SackRemainsPassableAndUrnWellRemainBlockingWithoutSpriteCollision()
        {
            foreach(var name in Names)
            {
                zone=Attach("Overworld.17.13.0");var e=Add(name);NativeOwner(e);var actor=Add("Player",X-1,Y);Assert.AreEqual(name!="Sack",e.GetPart<PhysicsPart>().Solid);
                Assert.AreEqual(name=="Sack",MovementSystem.TryMove(actor,zone,1,0));Assert.AreEqual(name=="Sack"?(X,Y):(X-1,Y),zone.GetEntityPosition(actor));
                if(name=="Sack"){Assert.AreSame(actor,View().Owner);Assert.True(MovementSystem.TryMove(actor,zone,-1,0));NativeOwner(e);}else NativeOwner(e);
            }
        }
        [Test] public void CachedViewsDisposeWithoutDestroyingInputsOrPeer()
        {
            var inputs=new[]{Texture(16,16),Texture(16,16),Texture(16,16)};var a=Construct(inputs[0],inputs[1],inputs[2]);
            try{using(var peer=Construct(inputs[0],inputs[1],inputs[2]))
            {
                var tiles=new List<Tile>();var sprites=new List<Sprite>();for(int i=0;i<3;i++){var e=content.Factory.CreateEntity(Names[i]);Assert.True(Resolve(a)(e,out var t));Assert.True(Resolve(a)(e,out var same));Assert.AreSame(t,same);tiles.Add(t);sprites.Add(t.sprite);}
                a.Dispose();a.Dispose();foreach(var t in tiles)Assert.True(t==null);foreach(var s in sprites)Assert.True(s==null);for(int i=0;i<3;i++){var e=content.Factory.CreateEntity(Names[i]);Assert.False(Resolve(a)(e,out _));Assert.True(Resolve(peer)(e,out var t));Native(t,Names[i],inputs[i]);}
                foreach(var t in inputs){Assert.True(t!=null);Assert.AreEqual(FilterMode.Bilinear,t.filterMode);Assert.AreEqual(TextureWrapMode.Repeat,t.wrapMode);}
            }}finally{a.Dispose();}
        }
        sealed class OpenProbe:Part
        {public int Opens;public override bool HandleEvent(GameEvent e){if(e.ID=="OpenContainer")Opens++;return true;}}
        sealed class Probe:Part
        {
            public int Renders,ServiceQueries; public string Cue;
            public override bool HandleEvent(GameEvent e){if(e.ID=="Render"){Renders++;if(Cue!=null)e.SetParameter("ColorString",Cue);}if(e.ID=="GetInventoryActions"||e.ID=="InventoryAction"||e.ID=="ObjectCreated"||e.ID==IdleQueryEvent.ID)ServiceQueries++;return true;}
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Shared camp props submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public Tile Mark(int x,int y) => ((Tilemap)Read(Renderer,"_tileStateTilemap")).GetTile<Tile>(Position(x,y));
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Shared camp props fixture");
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
