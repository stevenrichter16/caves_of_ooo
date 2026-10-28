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
    // Reflection keeps the absent production adapter an assertion RED. Real
    // painter/movement/destruction are exercised; this is not ordinary Play.
    public sealed class FlatPixelGreatdewTests
    {
        const int X=10,Y=10;
        const string ResourcePath="Sprites/Live16x24/greatdew";
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone,oldSettlement;
        FlatPixelGreyboxCatalog world;
        readonly List<Texture2D> borrowed=new List<Texture2D>();
        Action<string> oldMessage,oldFull;
        Action<int,int,string> oldDirty;
        EntityDamageVisualHandler oldDamage;
        EntityMovedVisualHandler oldMoved;
        Action<WorldAudioCombatReceipt> oldAudio;
        System.Random oldRng;
        [SetUp] public void Setup()
        {
            oldSettlement=SettlementRuntime.ActiveZone;oldMessage=MessageLog.OnMessage;
            oldDirty=ZoneRenderHooks.CellDirtyCallback;oldFull=ZoneRenderHooks.FullDirtyCallback;
            oldDamage=EntityVisualHooks.DamageCallback;oldMoved=EntityVisualHooks.MovedCallback;
            oldAudio=WorldAudioCombat.Callback;oldRng=GreatdewSnarePart.TestRng;
            MessageLog.OnMessage=null;ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;
            EntityVisualHooks.DamageCallback=null;EntityVisualHooks.MovedCallback=null;WorldAudioCombat.Callback=null;
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);
            zone=Attach("Overworld.18.8.0");world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try{world?.Dispose();foreach(var t in borrowed)if(t!=null)Object.DestroyImmediate(t);borrowed.Clear();content?.Dispose();}
            finally{SettlementRuntime.ActiveZone=oldSettlement;MessageLog.OnMessage=oldMessage;
                ZoneRenderHooks.CellDirtyCallback=oldDirty;ZoneRenderHooks.FullDirtyCallback=oldFull;
                EntityVisualHooks.DamageCallback=oldDamage;EntityVisualHooks.MovedCallback=oldMoved;
                WorldAudioCombat.Callback=oldAudio;GreatdewSnarePart.TestRng=oldRng;}
        }
        Zone Attach(string id){var z=new Zone(id);manager.SetActiveZone(z);foreach(var c in z.Cells)c.IsVisible=c.Explored=true;return z;}
        Entity Add(string name="Greatdew",int x=X,int y=Y){var e=content.Factory.CreateEntity(name);Assert.NotNull(e,name);Assert.True(zone.AddEntity(e,x,y));return e;}
        static object Read(object o,string n){var f=o.GetType().GetField(n,I);Assert.NotNull(f,n);return f.GetValue(o);}
        static void Write(object o,string n,object value){var f=o.GetType().GetField(n,I);Assert.NotNull(f,n);f.SetValue(o,value);}
        static Texture2D Resource(){var t=Resources.Load<Texture2D>(ResourcePath);Assert.NotNull(t,ResourcePath);return t;}
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,X,Y,out var v));return v;}
        static void Frame(Tile t,Texture2D texture=null)
        {
            Assert.NotNull(t);Assert.NotNull(t.sprite);Assert.AreSame(texture??Resource(),t.sprite.texture);
            Assert.AreEqual(new Rect(0,0,16,24),t.sprite.rect);Assert.AreEqual(new Vector2(8,12),t.sprite.pivot);
            Assert.AreEqual(16,t.sprite.pixelsPerUnit);Assert.AreEqual(new Vector2(1,1.5f),(Vector2)t.sprite.bounds.size);
            Assert.AreEqual(TileFlags.None,t.flags);Assert.AreEqual(Tile.ColliderType.None,t.colliderType);
        }
        Tile Native(Entity e,Texture2D texture=null)
        {var v=View();Assert.AreSame(e,v.Owner);Assert.True(v.NativeForeground,"The disclosed Greatdew still uses glyph fallback.");Frame(v.ForegroundTile,texture);Assert.AreEqual(Color.white,v.ForegroundColor);return v.ForegroundTile;}
        static Type ArtType(){var t=typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelGreatdewCatalog");Assert.NotNull(t,"The exact Greatdew adapter is absent.");return t;}
        delegate bool Query(Entity owner,out Tile tile);
        static IDisposable Construct(Texture2D texture){var c=ArtType().GetConstructor(new[]{typeof(Texture2D)});Assert.NotNull(c);return(IDisposable)c.Invoke(new object[]{texture});}
        static Query Resolve(IDisposable art)=>(Query)Delegate.CreateDelegate(typeof(Query),art,ArtType().GetMethod("TryResolve",new[]{typeof(Entity),typeof(Tile).MakeByRefType()}));
        Texture2D Texture(int w=16,int h=24){var t=new Texture2D(w,h);borrowed.Add(t);return t;}
        AnnouncementDocument Document(Entity e,Entity viewer=null){var d=ExamineDocumentBuilder.BuildWorld(e,zone,zone.GetEntityCell(e),viewer);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(d));return d;}
        static ExaminePortraitRenderer Portraits()=>(ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer),I,null,new object[]{(Action<Camera,RenderTexture>)((c,t)=>{})},null);
        void Refused(Entity e,string why)
        {
            using(var a=Construct(Resource())){Assert.False(Resolve(a)(e,out var t),why);Assert.IsNull(t,why);}
            Assert.True(world.TryResolveFallback(zone,X,Y,out var fallback));var v=View();Assert.False(v.NativeForeground,why);
            Assert.AreSame(fallback.Owner,v.Owner);Assert.AreSame(fallback.ForegroundTile,v.ForegroundTile);
            if(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var sprite,out _))Assert.AreNotSame(Resource(),sprite.texture,why);
            if(FlatPixelGreyboxCatalog.IsAdmitted(zone))
            {
                Assert.True(world.TryResolve(zone,X,Y,out var old));Assert.AreNotSame(Resource(),old.ForegroundTile?.sprite?.texture,why);
                if(world.TryGetSubjectSprite(e,X,Y,out sprite,out _))Assert.AreNotSame(Resource(),sprite.texture,why);
            }
        }

        [TestCase("Overworld.18.8.0")][TestCase("Overworld.12.11.0")]
        [TestCase("Overworld.16.13.8")][TestCase("GreatdewCurrentGraph")]
        public void ExactBodyMatchesSubmittedWorldAndAuthorizedReader(string id)
        {
            zone=Attach(id);var e=Add();var viewer=Add("Player",X-1,Y);var body=Native(e);
            Assert.False(zone.GetCell(X,Y).BlocksMovement());Assert.IsNull(e.GetPart<ThermalPart>());Assert.IsNull(e.GetStat("Hitpoints"));
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(Matrix4x4.identity,paint.Transform(X,Y));
                Assert.AreEqual(Color.white,paint.Tint(X,Y));var portrait=portraits.GetPortrait(Document(e,viewer),paint.Renderer);
                Assert.AreSame(Resource(),portrait.Texture);Assert.False(portrait.IsModel);Assert.AreEqual(new Rect(0,0,1,1),portrait.Uv);
            }
            if(FlatPixelGreyboxCatalog.IsAdmitted(zone))
            {Assert.True(world.TryResolve(zone,X,Y,out var legacy));Frame(legacy.ForegroundTile);Assert.True(world.TryGetSubjectSprite(e,X,Y,out var sprite,out var tint));Assert.AreSame(Resource(),sprite.texture);Assert.AreEqual(Color.white,tint);}
        }
        [Test] public void NamedDamagedAndValidRetunedPlantRetainsNeutralBodyWithoutSimulationQueries()
        {
            var e=Add();var body=Native(e);var snare=e.GetPart<GreatdewSnarePart>();var hp=e.GetPart<DestructiblePart>();
            Assert.AreEqual(55,snare.GrabChance);Assert.AreEqual(4,snare.HoldTurns);Assert.AreEqual(1f,snare.Corrosion);
            e.GetPart<RenderPart>().DisplayName="An old greatdew";hp.HP=1;snare.GrabChance=35;snare.HoldTurns=6;snare.Corrosion=.5f;
            var rng=new FixedRng(0);GreatdewSnarePart.TestRng=rng;var probe=new Probe();e.AddPart(probe);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();int renders=probe.Renders;for(int i=0;i<3;i++){Assert.AreSame(body,Native(e));Assert.AreSame(Resource(),portraits.GetPortrait(Document(e),paint.Renderer).Texture);}
                Assert.AreEqual(renders,probe.Renders);Assert.Zero(probe.Actions);Assert.Zero(rng.Calls);Assert.AreEqual(1,hp.HP);Assert.AreSame(snare,e.GetPart<TriggerOnStepPart>());
            }
        }
        [Test] public void MissingForeignOrDuplicateEssentialPartsCannotBorrowBody()
        {
            foreach(string part in new[]{"Render","Physics","Material","Destructible","Examinable","GreatdewSnare"})
            foreach(string fault in new[]{"missing","foreign","duplicate"})
            {
                zone=Attach("Overworld.12.11.0");var e=Add();Native(e);Part chosen=null;
                foreach(var p in e.Parts)if(p.GetType().Name==part+"Part")chosen=p;Assert.NotNull(chosen,part);
                if(fault=="missing")e.RemovePart(chosen);else if(fault=="foreign")chosen.ParentEntity=new Entity();
                else{var duplicate=(Part)Activator.CreateInstance(chosen.GetType());duplicate.ParentEntity=e;e.Parts.Add(duplicate);}
                Refused(e,part+" "+fault);
            }
        }
        [Test] public void ReskinsCustodyAndContradictoryOwnersRemainTruthfulFallback()
        {
            foreach(string fault in new[]{"glyph","color","layer","background","visual","variant","glyphVariants","solid","takeable","carried","equipped","material","vegetation","creature","player","item","terrain","corpse"})
            {
                zone=Attach("Overworld.12.11.0");var e=Add();Native(e);var r=e.GetPart<RenderPart>();var p=e.GetPart<PhysicsPart>();
                switch(fault)
                {
                    case "glyph":r.RenderString="?";break;case "color":r.ColorString="&c";break;case "layer":r.RenderLayer=3;break;case "background":r.BackgroundColor="^G";break;
                    case "visual":r.VisualID="other-greatdew";break;case "variant":r.VisualVariant="open";break;case "glyphVariants":r.GlyphVariants="()?";break;
                    case "solid":p.Solid=true;break;case "takeable":p.Takeable=true;break;case "carried":p.InInventory=new Entity();break;case "equipped":p.Equipped=new Entity();break;
                    case "material":e.GetPart<MaterialPart>().MaterialID="Stone";break;case "vegetation":e.Tags.Remove("Vegetation");break;
                    default:e.SetTag(char.ToUpperInvariant(fault[0])+fault.Substring(1));break;
                }
                Refused(e,fault);
            }
        }
        [Test] public void AddedServicesHarvestThermalOrForeignEffectsNeverMasqueradeAsNeutralDew()
        {
            foreach(var type in new[]{typeof(Body),typeof(BrainPart),typeof(ContainerPart),typeof(CropPart),typeof(FieldHarvestPart),typeof(HarvestablePart),typeof(CultivatedSoilPart),typeof(LiquidPoolPart),typeof(TileStateSourcePart),typeof(WellPart),typeof(ForgePart),typeof(ThermalPart)})
            {zone=Attach("Overworld.12.11.0");var e=Add();Native(e);e.AddPart((Part)Activator.CreateInstance(type));Refused(e,type.Name);}
            foreach(bool duplicate in new[]{false,true})
            {zone=Attach("Overworld.12.11.0");var e=Add();Native(e);var effects=new StatusEffectsPart();e.AddPart(effects);if(duplicate)e.AddPart(new StatusEffectsPart());else effects.ParentEntity=new Entity();Refused(e,"effects authority");}
        }
        [Test] public void MalformedOrOneShotSnareCannotClaimThisRepeatablePlant()
        {
            foreach(string fault in new[]{"chance-negative","chance-high","hold-negative","corrosion-negative","corrosion-nan","corrosion-infinite","consume","faction","other-trigger"})
            {
                zone=Attach("Overworld.12.11.0");var e=Add();Native(e);var s=e.GetPart<GreatdewSnarePart>();
                switch(fault){case "chance-negative":s.GrabChance=-1;break;case "chance-high":s.GrabChance=101;break;case "hold-negative":s.HoldTurns=-1;break;
                    case "corrosion-negative":s.Corrosion=-1;break;case "corrosion-nan":s.Corrosion=float.NaN;break;case "corrosion-infinite":s.Corrosion=float.PositiveInfinity;break;
                    case "consume":s.ConsumeOnTrigger=true;break;case "faction":s.TriggerFaction="Player";break;case "other-trigger":e.AddPart(new RuneFlameTriggerPart());break;}
                Refused(e,fault);
            }
        }
        [Test] public void DepletedGoneOrEmptyStackDoesNotReturnAsHealthyPlant()
        {
            foreach(string fault in new[]{"zero","gone","zero-stack","foreign-stack"})
            {zone=Attach("Overworld.12.11.0");var e=Add();Native(e);if(fault=="zero")e.GetPart<DestructiblePart>().HP=0;else if(fault=="gone")e.GetPart<DestructiblePart>().Gone=true;
                else{var stack=new StackerPart{StackCount=fault=="zero-stack"?0:1};e.AddPart(stack);if(fault=="foreign-stack")stack.ParentEntity=new Entity();}Refused(e,fault);}
        }
        [TestCase(false)][TestCase(true)]
        public void OptionalMissingOrMalformedTextureDoesNotReloadThroughWorldOrReader(bool malformed)
        {
            var good=Texture();using(var valid=Construct(good)){Assert.True(Resolve(valid)(content.Factory.CreateEntity("Greatdew"),out var tile));Frame(tile,good);}
            world.Dispose();world=new FlatPixelGreyboxCatalog();Write(world,"_greatdewArt",Construct(malformed?Texture(16,16):null));var e=Add();
            Assert.False(View().NativeForeground);using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {paint.Step();Assert.AreNotSame(Resource(),paint.At(X,Y)?.sprite?.texture);Assert.AreNotSame(Resource(),portraits.GetPortrait(Document(e),paint.Renderer).Texture);}
        }
        [Test] public void CachedViewsDisposeButDoNotDestroyBorrowedPixelsOrReloadLegacyInjection()
        {
            var input=Texture();var e=Add();var a=Construct(input);
            try{using(var peer=Construct(input)){Assert.True(Resolve(a)(e,out var tile));var sprite=tile.sprite;Assert.True(Resolve(a)(e,out var same));Assert.AreSame(tile,same);
                a.Dispose();a.Dispose();Assert.True(tile==null);Assert.True(sprite==null);Assert.True(input!=null);Assert.False(Resolve(a)(e,out _));Assert.True(Resolve(peer)(e,out var other));Frame(other,input);
                Assert.AreEqual(FilterMode.Bilinear,input.filterMode);Assert.AreEqual(TextureWrapMode.Repeat,input.wrapMode);}}
            finally{a.Dispose();}
            world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);Assert.False(View().NativeForeground);
        }
        [Test] public void DisclosureCoverAndReplacementGraphControlSubmittedBodyAndStaleReader()
        {
            var e=Add();Native(e);var doc=Document(e);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();var body=paint.At(X,Y);zone.GetCell(X,Y).IsVisible=false;paint.Dirty(X,Y);paint.Step();Assert.False(View().NativeForeground);Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);
                zone.GetCell(X,Y).Explored=false;Assert.False(world.TryResolvePresentation(zone,X,Y,out _));zone.GetCell(X,Y).Explored=zone.GetCell(X,Y).IsVisible=true;
                e.GetPart<RenderPart>().Visible=false;Assert.False(View().NativeForeground);e.GetPart<RenderPart>().Visible=true;Native(e);
                var cover=Add("Player");paint.Dirty(X,Y);paint.Step();Assert.AreSame(cover,View().Owner);Assert.AreNotSame(body,paint.At(X,Y));Assert.True(zone.RemoveEntity(cover));paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));
                var old=zone;Attach(old.ZoneID);Assert.False(world.TryResolvePresentation(old,X,Y,out _));Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);
            }
        }
        [Test] public void MultiCellForeignAndStaleFootprintsRefuseUntilCanonicalRepair()
        {
            var e=Add();var footprint=new SpatialFootprintPart{CellsRaw="0,0"};e.AddPart(footprint);var body=Native(e);
            footprint.ParentEntity=new Entity();Refused(e,"foreign");footprint.ParentEntity=e;Assert.AreSame(body,Native(e));
            Assert.True(zone.TryChangeFootprint(e,"0,0;1,0"));Refused(e,"multi");footprint.CellsRaw="0,0";Refused(e,"stale");
            Assert.True(zone.TryChangeFootprint(e,"0,0"));Assert.AreSame(body,Native(e));
        }
        [Test] public void ActualWetCueUsesOneRenderEventAndLeavesNeutralPortrait()
        {
            var e=Add();var body=Native(e);var effects=new StatusEffectsPart();e.AddPart(effects);var probe=new Probe();e.AddPart(probe);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();var wet=new WetEffect();Assert.True(effects.ForceApplyEffect(wet,null,zone));int before=probe.Renders;
                paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(QudColorParser.Parse("&B"),paint.Tint(X,Y));Assert.AreEqual(before+1,probe.Renders);
                Assert.AreSame(Resource(),portraits.GetPortrait(Document(e),paint.Renderer).Texture);paint.Dirty(X+1,Y);paint.Step();Assert.AreEqual(before+1,probe.Renders);
                Assert.True(effects.RemoveEffect(wet));paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(Color.white,paint.Tint(X,Y));
            }
        }
        [TestCase(0,true)][TestCase(99,false)]
        public void OnlyRealStepRollsSnareAndTheSameNeutralPlantSurvivesBothOutcomes(int roll,bool caught)
        {
            var e=Add();var body=Native(e);var walker=Add("Player",X-1,Y);var rng=new FixedRng(roll);GreatdewSnarePart.TestRng=rng;
            var snare=e.GetPart<GreatdewSnarePart>();using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();for(int i=0;i<3;i++){Native(e);Assert.AreSame(Resource(),portraits.GetPortrait(Document(e,walker),paint.Renderer).Texture);paint.Step();}
                Assert.Zero(rng.Calls);Assert.False(walker.HasEffect<RootedEffect>());Assert.False(walker.HasEffect<AcidicEffect>());
                Assert.True(MovementSystem.TryMove(walker,zone,1,0));Assert.AreEqual(1,rng.Calls);Assert.AreEqual(caught,walker.HasEffect<RootedEffect>());Assert.AreEqual(caught,walker.HasEffect<AcidicEffect>());
                Assert.NotNull(zone.GetEntityCell(e));Assert.False(snare.ConsumeOnTrigger);Assert.AreEqual(55,snare.GrabChance);Assert.AreEqual(4,snare.HoldTurns);Assert.AreEqual(1f,snare.Corrosion);
                Assert.AreSame(walker,View().Owner);Assert.True(zone.RemoveEntity(walker));paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));
                Assert.AreSame(Resource(),portraits.GetPortrait(Document(e),paint.Renderer).Texture);Assert.AreEqual(1,rng.Calls);
            }
        }
        [Test] public void RealStructuralDamageAndDestructionRetireBodyAndReaderWithoutFullRepaint()
        {
            var e=Add();var body=Native(e);var doc=Document(e);using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();long full=EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.AreEqual(DestroyVerdict.Damaged,DestructionSystem.Damage(e,1,null,zone));Assert.AreEqual(9,e.GetPart<DestructiblePart>().HP);Assert.AreSame(body,Native(e));
                Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(e,9,null,zone));paint.Step();Assert.IsNull(zone.GetEntityCell(e));Assert.True(e.GetPart<DestructiblePart>().Gone);
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(doc));Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);Assert.AreNotSame(Resource(),paint.At(X,Y)?.sprite?.texture);
                Assert.False(zone.GetCell(X,Y).BlocksMovement());Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);
                long cells=EnvironmentSpriteRenderer.Perf.CellsResolved;paint.Step();Assert.AreEqual(cells,EnvironmentSpriteRenderer.Perf.CellsResolved);
            }
        }
        [Test] public void SimilarSnarePlantsAndSourceAliasesNeverAcquireGreatdewBody()
        {
            using(var a=Construct(Texture()))
            {Assert.True(Resolve(a)(content.Factory.CreateEntity("Greatdew"),out _));Assert.False((bool)ArtType().GetMethod("HasKnownRole").Invoke(null,new object[]{null}));
                foreach(string name in new[]{"WineLeafSundew","PebbleSundewThreshold","Reeds","CreatureCorpse","Player"})
                {var e=content.Factory.CreateEntity(name);Assert.NotNull(e,name);e.Properties["SourceBlueprint"]="Greatdew";Assert.False((bool)ArtType().GetMethod("HasKnownRole").Invoke(null,new object[]{e}));Assert.False(Resolve(a)(e,out _));}}
        }
        [Test] public void MechanicalJammingDoesNotInventADisabledGreatdewState()
        {
            var plant=Add();Native(plant);var player=Add("Player",X-1,Y);
            Assert.IsNull(plant.GetPart<TrapJammingPart>());Assert.False(TrapJammingPart.IsSupported(plant));
            foreach(var action in WorldInteractionSystem.GatherActions(plant,player))Assert.AreNotEqual(TrapJammingPart.JamCommand,action.Command);
            var trap=content.Factory.CreateEntity("SpikeTrap");Assert.True(TrapJammingPart.IsSupported(trap));
            trap.GetPart<TrapJammingPart>().Jammed=true;Assert.True(TrapJammingPart.IsJammed(trap));Assert.False(TrapJammingPart.IsJammed(plant));
            plant.AddPart(new TrapJammingPart{Jammed=true});Assert.False(TrapJammingPart.IsJammed(plant),"The real shared trigger gate refuses a forged plant jam flag.");
            Refused(plant,"Foreign mechanical intervention is not an approved disabled-plant visual.");
        }
        sealed class FixedRng:System.Random{readonly int value;public int Calls;public FixedRng(int value){this.value=value;}public override int Next(int maxValue){Calls++;return value;}}
        sealed class Probe:Part{public int Renders,Actions;public override bool HandleEvent(GameEvent e){if(e.ID=="Render")Renders++;if(e.ID=="EntityEnteredCell"||e.ID=="GetInventoryActions"||e.ID=="InventoryAction")Actions++;return true;}}
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Greatdew submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Greatdew fixture");
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
