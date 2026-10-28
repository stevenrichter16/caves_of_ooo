// Villager/Merchant body-only proposal; reflection permits genuine absent-adapter RED.
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
    public sealed class FlatPixelSharedResidentsTests
    {
        const int X=10,Y=10;
        const string Cast="Sprites/Live16x24/opening-cast",Merchant="Sprites/Environment/merchant";
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        static readonly string[] Names={"Villager","Merchant"};
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
            manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);zone=Attach("Overworld.10.10.0");world=new FlatPixelGreyboxCatalog();
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
        static Texture2D Resource(string name){var t=Resources.Load<Texture2D>(name=="Villager"?Cast:Merchant);Assert.NotNull(t);return t;}
        static void Native(Tile tile,string name,Texture2D texture=null)
        {
            int h=name=="Villager"?24:16;Assert.NotNull(tile);Assert.NotNull(tile.sprite);Assert.AreSame(texture??Resource(name),tile.sprite.texture);
            Assert.AreEqual(new Rect(0,0,16,h),tile.sprite.rect);Assert.AreEqual(new Vector2(8,h/2f),tile.sprite.pivot);Assert.AreEqual(16,tile.sprite.pixelsPerUnit);
            Assert.AreEqual(new Vector2(1,h/16f),(Vector2)tile.sprite.bounds.size);Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);Assert.AreEqual(TileFlags.None,tile.flags);
        }
        Tile NativeOwner(Entity e,Texture2D texture=null)
        {var c=zone.GetEntityCell(e);Assert.NotNull(c);var v=View(c.X,c.Y);Assert.AreSame(e,v.Owner);Assert.True(v.NativeForeground);Native(v.ForegroundTile,e.BlueprintName,texture);return v.ForegroundTile;}
        static Type ArtType(){var t=typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelSharedResidentsCatalog");Assert.NotNull(t,"Missing shared residents adapter");return t;}
        delegate bool Query(Entity owner,out Tile tile);
        static IDisposable Construct(Texture2D cast,Texture2D merchant){var c=ArtType().GetConstructor(new[]{typeof(Texture2D),typeof(Texture2D)});Assert.NotNull(c);return (IDisposable)c.Invoke(new object[]{cast,merchant});}
        static Query Resolve(IDisposable art)=>(Query)Delegate.CreateDelegate(typeof(Query),art,ArtType().GetMethod("TryResolve",new[]{typeof(Entity),typeof(Tile).MakeByRefType()}));
        Texture2D Texture(int w,int h){var t=new Texture2D(w,h);borrowed.Add(t);return t;}
        AnnouncementDocument Document(Entity e,Entity viewer=null){var d=ExamineDocumentBuilder.BuildWorld(e,zone,zone.GetEntityCell(e),viewer);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(d));return d;}
        static ExaminePortraitRenderer Portraits()=>(ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer),I,null,new object[]{(Action<Camera,RenderTexture>)((c,t)=>{})},null);
        void Refused(Entity e,string why)
        {
            using(var a=Construct(Resource("Villager"),Resource("Merchant"))){Assert.False(Resolve(a)(e,out var tile),why);Assert.IsNull(tile,why);}
            Assert.False(View().NativeForeground,why);
            if(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var sprite,out _))Assert.AreNotSame(Resource(e.BlueprintName),sprite.texture,why);
            if(FlatPixelGreyboxCatalog.IsAdmitted(zone))
            {
                Assert.True(world.TryResolve(zone,X,Y,out var old));Assert.AreNotSame(Resource(e.BlueprintName),old.ForegroundTile?.sprite?.texture,why);
                if(world.TryGetSubjectSprite(e,X,Y,out sprite,out _))Assert.AreNotSame(Resource(e.BlueprintName),sprite.texture,why);
            }
        }
        [TestCase("Villager")] [TestCase("Merchant")]
        public void ExactBodiesMatchSubmittedWorldAndAuthorizedReaderAcrossContexts(string name)
        {
            foreach(var id in new[]{"Overworld.12.11.0","Overworld.10.10.0","Overworld.19.14.0","StandaloneResidents"})
            {
                zone=Attach(id);var e=Add(name);var viewer=Add("Player",X-1,Y);NativeOwner(e);
                using(var paint=new Paint(world,zone))using(var portraits=Portraits())
                {
                    paint.Step();Native(paint.At(X,Y),name);Assert.AreEqual(Color.white,paint.Tint(X,Y));Assert.AreEqual(Matrix4x4.identity,paint.Transform(X,Y));
                    var p=portraits.GetPortrait(Document(e,viewer),paint.Renderer);Assert.AreSame(Resource(name),p.Texture);Assert.False(p.IsModel);
                    Assert.AreEqual(new Rect(0,0,name=="Villager"?.25f:1,1),p.Uv);
                }
            }
        }
        [TestCase("Villager")] [TestCase("Merchant")]
        public void NamesDialoguePurseInventoryEquipmentAndAiRemainMutableWithoutPresentationQueries(string name)
        {
            var e=Add(name);var player=Add("Player",X-1,Y);var body=NativeOwner(e);var brain=e.GetPart<BrainPart>();
            e.GetPart<RenderPart>().DisplayName="Perret, a wayfaring resident";e.GetPart<ConversationPart>().ConversationID="DifferentQuestConversation";
            e.GetPart<TraderPart>().StockTable="DifferentRealStock";TradeSystem.SetDrams(e,0);e.GetStat("Hitpoints").BaseValue=1;
            brain.Passive=true;brain.Staying=false;brain.Wanders=true;brain.SetPersonallyHostile(player,false);
            var inv=e.GetPart<InventoryPart>();var sword=content.Factory.CreateEntity("LongSword");Assert.NotNull(sword);inv.AddObject(sword);Assert.True(inv.Equip(sword,"TestHand"));Assert.AreSame(e,sword.GetPart<PhysicsPart>().Equipped);
            var stock=inv.Objects.ToArray();int goals=brain.GoalCount;var probe=new Probe();e.AddPart(probe);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();int renders=probe.Renders;
                for(int i=0;i<3;i++){Assert.AreSame(body,NativeOwner(e));Assert.AreSame(Resource(name),portraits.GetPortrait(Document(e,player),paint.Renderer).Texture);}
                Assert.AreEqual(renders,probe.Renders);Assert.Zero(probe.ServiceQueries);Assert.AreEqual(goals,brain.GoalCount);Assert.Zero(TradeSystem.GetDrams(e));CollectionAssert.AreEqual(stock,inv.Objects);Assert.AreSame(e,sword.GetPart<PhysicsPart>().Equipped);
            }
        }
        [TestCase("missing")] [TestCase("foreign")] [TestCase("duplicate")]
        public void EveryEssentialPartMustBeUniqueAndOwnedBeforeAnyLocalFallback(string fault)
        {
            var types=new[]{typeof(RenderPart),typeof(PhysicsPart),typeof(Body),typeof(BrainPart),typeof(ExaminablePart),typeof(InventoryPart),typeof(TraderPart),typeof(ConversationPart)};
            foreach(var name in Names)foreach(var type in types)
            {
                zone=Attach("Overworld.12.11.0");var e=Add(name);NativeOwner(e);var part=e.Parts.First(p=>p.GetType()==type);
                if(fault=="missing")Assert.True(e.RemovePart(part));else if(fault=="foreign")part.ParentEntity=new Entity();else e.AddPart((Part)Activator.CreateInstance(type));
                Refused(e,name+"/"+type.Name+"/"+fault);
            }
        }
        [TestCase("identity")] [TestCase("custody")]
        public void ReskinsIncludingEllunAndCustodiedOrMulticellOwnersStayFallback(string group)
        {
            var faults=group=="identity"?new[]{"glyph","color","layer","visual","variant","ellipsis"}:new[]{"carried","equipped","takeable","multicell"};
            foreach(var name in Names)foreach(var fault in faults)
            {
                zone=Attach("Overworld.12.11.0");var e=Add(name);NativeOwner(e);var r=e.GetPart<RenderPart>();var p=e.GetPart<PhysicsPart>();
                switch(fault)
                {
                    case "glyph":r.RenderString="b";r.DisplayName="Ellun";break;case "color":r.ColorString="&c";break;case "layer":r.RenderLayer=1;break;
                    case "visual":r.VisualID="custom-resident";break;case "variant":r.VisualVariant="special";break;case "ellipsis":r.GlyphVariants="@b";break;
                    case "carried":p.InInventory=new Entity();break;case "equipped":p.Equipped=new Entity();break;case "takeable":p.Takeable=true;break;
                    case "multicell":e.AddPart(new SpatialFootprintPart{CellsRaw="0,0"});Assert.True(zone.TryChangeFootprint(e,"0,0;1,0"));break;
                }
                Refused(e,name+fault);
            }
        }
        [Test] public void ForeignServicesAndNonActorMeaningCannotBorrowResidents()
        {
            foreach(var name in Names)foreach(var type in new[]{typeof(WellPart),typeof(ContainerPart),typeof(HarvestablePart),typeof(TileStateSourcePart),typeof(DestructiblePart)})
            {zone=Attach("Overworld.12.11.0");var e=Add(name);NativeOwner(e);e.AddPart((Part)Activator.CreateInstance(type));Refused(e,name+type.Name);}
        }
        [Test] public void HitpointsMustBelongToThisLivingOwnerWithoutPinningInitialHealth()
        {
            foreach(var name in Names)foreach(var fault in new[]{"dead","foreign","missing"})
            {
                zone=Attach("Overworld.12.11.0");var e=Add(name);e.GetStat("Hitpoints").BaseValue=1;NativeOwner(e);
                if(fault=="dead")e.GetStat("Hitpoints").BaseValue=0;if(fault=="foreign")e.GetStat("Hitpoints").Owner=new Entity();if(fault=="missing")e.Statistics.Remove("Hitpoints");Refused(e,name+fault);
            }
        }
        [Test] public void ExistingExplicitLocalCastInjectionRetainsVillagerAndOtherCastOwners()
        {
            world.Dispose();var cast=Texture(64,24);world=new FlatPixelGreyboxCatalog(null,cast);zone=Attach("Overworld.12.11.0");
            var v=Add("Villager");NativeOwner(v,cast);var dog=Add("PetDog",X+2,Y);Assert.True(View(X+2,Y).NativeForeground);Assert.AreSame(cast,View(X+2,Y).ForegroundTile.sprite.texture);
            var m=Add("Merchant",X+4,Y);Assert.False(View(X+4,Y).NativeForeground,"Omitted merchant input must not reload Resources.");
        }
        [TestCase(0,false)] [TestCase(0,true)] [TestCase(1,false)] [TestCase(1,true)]
        public void MissingOrMalformedIndependentResourcesCannotBypassThroughValidOldLocalCast(int group,bool malformed)
        {
            var cast=Texture(64,24);var merchant=Texture(16,16);var inputs=new[]{cast,merchant};using(var a=Construct(cast,merchant))foreach(var name in Names){Assert.True(Resolve(a)(content.Factory.CreateEntity(name),out var t));Native(t,name,name=="Villager"?cast:merchant);}
            inputs[group]=malformed?Texture(17,17):null;world.Dispose();world=new FlatPixelGreyboxCatalog(null,cast);Write(world,"_sharedResidentsArt",Construct(inputs[0],inputs[1]));
            foreach(var name in Names)
            {
                zone=Attach("Overworld.12.11.0");var e=Add(name);var expected=name=="Villager"?cast:merchant;
                if((name=="Villager"?0:1)!=group){NativeOwner(e,expected);continue;}
                Assert.False(View().NativeForeground);Assert.True(world.TryResolve(zone,X,Y,out var old));Assert.AreNotSame(expected,old.ForegroundTile?.sprite?.texture);
                using(var paint=new Paint(world,zone))using(var portraits=Portraits()){paint.Step();Assert.AreNotSame(expected,paint.At(X,Y)?.sprite?.texture);Assert.AreNotSame(expected,portraits.GetPortrait(Document(e),paint.Renderer).Texture);}
            }
        }
        [TestCase("Villager")] [TestCase("Merchant")]
        public void RealWetCueUsesOneRenderEventAndDoesNotReplaceBodyOrTintPortrait(string name)
        {
            var e=Add(name);var body=NativeOwner(e);var effects=new StatusEffectsPart();e.AddPart(effects);var probe=new Probe();e.AddPart(probe);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();var wet=new WetEffect();Assert.True(effects.ForceApplyEffect(wet,null,zone));Assert.AreEqual("&B",effects.GetRenderColorOverride());int before=probe.Renders;
                paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(QudColorParser.Parse("&B"),paint.Tint(X,Y));Assert.AreEqual(before+1,probe.Renders);
                Assert.AreSame(Resource(name),portraits.GetPortrait(Document(e),paint.Renderer).Texture);NativeOwner(e);paint.Dirty(X+1,Y);paint.Step();Assert.AreEqual(before+1,probe.Renders);
                Assert.True(effects.RemoveEffect(wet));paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(Color.white,paint.Tint(X,Y));
            }
        }
        [Test] public void DisclosureSelectedOwnerDirtyCoverAndStaleGraphRetireWorldAndReaders()
        {
            var e=Add("Merchant");NativeOwner(e);var doc=Document(e);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();var body=paint.At(X,Y);zone.GetCell(X,Y).IsVisible=false;paint.Dirty(X,Y);paint.Step();Assert.False(View().NativeForeground);Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);
                zone.GetCell(X,Y).Explored=false;Assert.False(world.TryResolvePresentation(zone,X,Y,out _));zone.GetCell(X,Y).IsVisible=zone.GetCell(X,Y).Explored=true;
                e.GetPart<RenderPart>().Visible=false;Assert.False(View().NativeForeground);e.GetPart<RenderPart>().Visible=true;NativeOwner(e);
                var player=Add("Player");paint.Dirty(X,Y);paint.Step();Assert.AreSame(player,View().Owner);Assert.AreNotSame(body,paint.At(X,Y));Assert.True(zone.RemoveEntity(player));paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));
                var old=zone;Attach(old.ZoneID);Assert.False(world.TryResolvePresentation(old,X,Y,out _));Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);
            }
        }
        [TestCase("Villager")] [TestCase("Merchant")]
        public void RealDamageAndDeathKeepLiveBodyThenProduceSeparateCorpse(string name)
        {
            var e=Add(name);var spawner=e.GetPart<CorpsePart>();Assert.NotNull(spawner);Assert.AreEqual(100,spawner.CorpseChance);Assert.AreEqual("CreatureCorpse",spawner.CorpseBlueprint);spawner.TestRng=new System.Random(65);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();var body=paint.At(X,Y);Native(body,name);var doc=Document(e);int hp=e.GetStat("Hitpoints").BaseValue;
                CombatSystem.ApplyDamage(e,1,null,zone);Assert.AreEqual(hp-1,e.GetStat("Hitpoints").BaseValue);paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));
                CombatSystem.ApplyDamage(e,e.GetStat("Hitpoints").BaseValue,null,zone);paint.Step();Assert.IsNull(zone.GetEntityCell(e));var corpse=spawner.CreatedCorpse;Assert.NotNull(corpse);Assert.AreEqual("CreatureCorpse",corpse.BlueprintName);Assert.AreEqual(name,corpse.GetProperty("SourceBlueprint"));
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(doc));Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);Assert.AreNotSame(Resource(name),paint.At(X,Y)?.sprite?.texture);
                using(var a=Construct(Resource("Villager"),Resource("Merchant")))Assert.False(Resolve(a)(corpse,out _));
            }
        }
        [Test] public void OtherResidentsAndSourceBlueprintAliasesDoNotAcquireTheseBodiesAndFarmerStaysUnchanged()
        {
            using(var a=Construct(Resource("Villager"),Resource("Merchant")))
            {
                Assert.True(Resolve(a)(content.Factory.CreateEntity("Villager"),out _));Assert.False((bool)ArtType().GetMethod("HasKnownRole").Invoke(null,new object[]{null}));
                foreach(var name in new[]{"Farmer","Innkeeper","CreatureCorpse","Player"}){var e=content.Factory.CreateEntity(name);Assert.NotNull(e);e.Properties["SourceBlueprint"]="Villager";Assert.False((bool)ArtType().GetMethod("HasKnownRole").Invoke(null,new object[]{e}));Assert.False(Resolve(a)(e,out _));}
            }
            zone=Attach("Overworld.12.11.0");var farmer=Add("Farmer");Assert.True(View().NativeForeground);Assert.AreSame(Resources.Load<Texture2D>("Sprites/Live16x24/farmer"),View().ForegroundTile.sprite.texture);
        }
        [Test] public void CachedViewsDisposeWithoutDestroyingInputsOrIndependentPeer()
        {
            var cast=Texture(64,24);var merchant=Texture(16,16);var a=Construct(cast,merchant);
            try{using(var peer=Construct(cast,merchant))
            {
                var tiles=new List<Tile>();foreach(var name in Names){var e=content.Factory.CreateEntity(name);Assert.True(Resolve(a)(e,out var t));Assert.True(Resolve(a)(e,out var same));Assert.AreSame(t,same);tiles.Add(t);}
                a.Dispose();a.Dispose();foreach(var t in tiles)Assert.True(t==null);foreach(var name in Names){var e=content.Factory.CreateEntity(name);Assert.False(Resolve(a)(e,out _));Assert.True(Resolve(peer)(e,out var t));Native(t,name,name=="Villager"?cast:merchant);}
                Assert.True(cast!=null&&merchant!=null);Assert.AreEqual(FilterMode.Bilinear,cast.filterMode);Assert.AreEqual(TextureWrapMode.Repeat,merchant.wrapMode);
            }}finally{a.Dispose();}
        }
        [Test] public void RealChatOfferAndTradeRemainIndependentAndNativeQueriesNeverRestock()
        {
            var e=Add("Merchant");var player=Add("Player",X-1,Y);var body=NativeOwner(e);var inv=e.GetPart<InventoryPart>();var probe=new Probe();e.AddPart(probe);
            var actions=new InventoryActionList();var ev=GameEvent.New("GetInventoryActions");ev.SetParameter("Actor",(object)player);ev.SetParameter("Actions",(object)actions);e.FireEventAndRelease(ev);Assert.True(actions.Actions.Any(a=>a.Command=="Chat"));
            var item=TradeSystem.GetTraderStock(e).First();int price=TradeSystem.GetBuyPrice(item,TradeSystem.GetTradePerformance(player),e);TradeSystem.SetDrams(player,price+10);int purse=TradeSystem.GetDrams(e);
            Assert.True(TradeSystem.BuyFromTrader(player,e,item,out var reason),reason);Assert.AreEqual(purse+price,TradeSystem.GetDrams(e));Assert.AreEqual(10,TradeSystem.GetDrams(player));Assert.AreSame(body,NativeOwner(e));
            var stock=inv.Objects.ToArray();int serviceQueries=probe.ServiceQueries;int afterPurse=TradeSystem.GetDrams(e);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits()){paint.Step();for(int i=0;i<3;i++){NativeOwner(e);portraits.GetPortrait(Document(e,player),paint.Renderer);}}
            CollectionAssert.AreEqual(stock,inv.Objects);Assert.AreEqual(afterPurse,TradeSystem.GetDrams(e));Assert.AreEqual(serviceQueries,probe.ServiceQueries);
        }
        sealed class Probe:Part
        {
            public int Renders,ServiceQueries;
            public override bool HandleEvent(GameEvent e){if(e.ID=="Render")Renders++;if(e.ID=="GetInventoryActions"||e.ID=="InventoryAction"||e.ID=="ObjectCreated"||e.ID==IdleQueryEvent.ID)ServiceQueries++;return true;}
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Shared residents submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Shared residents fixture");
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
