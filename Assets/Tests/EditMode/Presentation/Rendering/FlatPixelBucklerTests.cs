using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Exact Buckler only. Existing incidental/sidearm/global fixtures remain
    // unchanged and supply the wider equipment and disclosure regressions.
    public sealed class FlatPixelBucklerTests
    {
        const int X=10,Y=10;
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        const string Resource="Sprites/Live16x24/buckler";
        static readonly string[] Peers={"LeatherArmor","Hatchet","Cudgel","Dagger","ShortSword","Mace"};
        readonly List<Action> restore=new List<Action>();
        readonly List<Texture2D> textures=new List<Texture2D>();
        HotbarSaveFixture globals;EntityFactory factory;OverworldZoneManager manager;Zone zone;
        FlatPixelGreyboxCatalog world;FlatPixelIncidentalEquipmentCatalog art;Texture2D[] inputs;
        [SetUp] public void Setup()
        {
            foreach(var type in new[]{typeof(MessageLog),typeof(EquipmentChangeBus),typeof(WorldAudioHooks),typeof(WorldAudioUtility),typeof(EntityVisualHooks)})
                foreach(var field in type.GetFields(S))if(!field.IsLiteral)Snapshot(field);
            Snapshot(typeof(AnatomyFactory).GetField("_types",S));Snapshot(typeof(BodyPart).GetField("_nextID",S));
            foreach(string name in new[]{"_buffer","_writeIndex","_filledCount","_droppedCount"})Snapshot(typeof(Diag).GetField(name,S));
            globals=new HotbarSaveFixture(false,false);MessageLog.OnMessage=null;MessageLog.Clear();WorldAudioHooks.Callback=null;WorldAudioUtility.Callback=null;EntityVisualHooks.Reset();
            ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;
            factory=DensityReferenceGladeTests.Factory();manager=OverworldZoneManager.CreateDetached(factory,65,true);
            Attach("Overworld.10.10.0");world=new FlatPixelGreyboxCatalog();
            inputs=Enumerable.Range(0,7).Select(i=>Texture(16,i>=3&&i<6?16:24)).ToArray();
        }
        [TearDown] public void Cleanup()
        {try{world?.Dispose();art?.Dispose();foreach(var t in textures)if(t!=null)Object.DestroyImmediate(t);textures.Clear();globals?.Dispose();}
         finally{for(int i=restore.Count-1;i>=0;i--)restore[i]();restore.Clear();}}
        void Attach(string id){zone=new Zone(id);manager.SetActiveZone(zone);foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;}
        Entity Owner(string name){var e=factory.CreateEntity(name);Assert.NotNull(e,name);return e;}
        Entity Add(string name,int x=10,int y=10){var e=Owner(name);Assert.True(zone.AddEntity(e,x,y));return e;}
        Texture2D Texture(int w,int h){var t=new Texture2D(w,h);textures.Add(t);return t;}
        FlatPixelGreyboxCell View(int x=10,int y=10){Assert.True(world.TryResolvePresentation(zone,x,y,out var v));return v;}
        static FlatPixelIncidentalEquipmentCatalog Construct(params Texture2D[] values)
        {
            var ctor=typeof(FlatPixelIncidentalEquipmentCatalog).GetConstructor(Enumerable.Repeat(typeof(Texture2D),values.Length).ToArray());
            Assert.NotNull(ctor,"Missing independent seven-input Buckler constructor.");
            return(FlatPixelIncidentalEquipmentCatalog)ctor.Invoke(values.Cast<object>().ToArray());
        }
        void Install(Texture2D optional=null,bool missing=false)
        {
            world.Dispose();art?.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);
            var values=(Texture2D[])inputs.Clone();values[6]=missing?optional:inputs[6];art=Construct(values);Write(world,"_incidentalEquipmentArt",art);
        }
        static void Frame(Tile tile,Texture2D texture,int height=24)
        {
            Assert.NotNull(texture);Assert.NotNull(tile);Assert.AreSame(texture,tile.sprite.texture);
            Assert.AreEqual(new Rect(0,0,16,height),tile.sprite.rect);Assert.AreEqual(new Vector2(8,height/2f),tile.sprite.pivot);
            Assert.AreEqual(16,tile.sprite.pixelsPerUnit);Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);Assert.AreEqual(TileFlags.None,tile.flags);
        }
        Tile Native(Entity owner,Texture2D texture=null)
        {
            var c=zone.GetEntityCell(owner);Assert.NotNull(c);var v=View(c.X,c.Y);Assert.AreSame(owner,v.Owner);
            Assert.True(v.NativeForeground,"Exact Buckler still uses a glyph fallback.");Frame(v.ForegroundTile,texture??inputs[6]);return v.ForegroundTile;
        }
        static void Write(object o,string name,object value){var f=o.GetType().GetField(name,I);Assert.NotNull(f,name);f.SetValue(o,value);}
        static object Read(object o,string name){var f=o.GetType().GetField(name,I);Assert.NotNull(f,name);return f.GetValue(o);}
        static void Portrait(Reader reader,AnnouncementDocument document,Texture2D texture)
        {
            Assert.NotNull(document);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document,out var image));Assert.AreSame(texture,image.Texture);
            Assert.AreEqual(new Rect(0,0,1,1),image.Uv);Assert.False(image.IsModel);Assert.AreEqual(Color.white,image.Tint);
        }
        static void Retired(Reader r,AnnouncementDocument d)
        {Assert.False(ExamineDocumentBuilder.IsCurrentSubject(d));Assert.True(r.Presentation.TryGetFlatExaminePortrait(d,out var image));Assert.IsNull(image.Texture);}
        void Refused(Entity owner)
        {
            Assert.False(art.TryResolve(owner,out _));Assert.False(View().NativeForeground);
            if(world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var sprite,out _))Assert.AreNotSame(inputs[6],sprite.texture);
            if(FlatPixelGreyboxCatalog.IsAdmitted(zone)){Assert.True(world.TryResolve(zone,X,Y,out var old));Assert.AreNotSame(inputs[6],old.ForegroundTile?.sprite?.texture);}
            if(world.TryGetSubjectSprite(owner,X,Y,out var legacy,out _))Assert.AreNotSame(inputs[6],legacy.texture);
        }

        [Test] public void DefaultBucklerUsesItsCompleteNativeBodyAndReaderAcrossRegions()
        {
            foreach(string id in new[]{"Overworld.10.10.0","Overworld.18.8.0",ReferenceGladePlan.ZoneID})
            {
                Attach(id);var shield=Add("Buckler");Assert.True(View().NativeForeground,"Buckler glyph fails before any resource assertion.");
                var texture=Resources.Load<Texture2D>(Resource);Assert.NotNull(texture);Assert.AreEqual(16,texture.width);Assert.AreEqual(24,texture.height);
                Assert.AreEqual(FilterMode.Point,texture.filterMode);Assert.AreEqual(TextureWrapMode.Clamp,texture.wrapMode);Assert.AreEqual(1,texture.mipmapCount);
                Native(shield,texture);using(var r=new Reader(world,zone,Owner("Player")))Portrait(r,ExamineDocumentBuilder.BuildWorld(shield,zone,zone.GetEntityCell(shield),r.Viewer),texture);
                if(FlatPixelGreyboxCatalog.IsAdmitted(zone)){Assert.True(world.TryResolve(zone,X,Y,out var old));Frame(old.ForegroundTile,texture);Assert.True(old.NativeForeground);}
                Assert.True(world.TryGetSubjectSprite(shield,X,Y,out var sprite,out _));Assert.AreSame(texture,sprite.texture);
            }
        }

        [Test] public void ActualHandUnequipDropPickupAndReequipRetireOnlyOldCustodyAndDirtyCell()
        {
            Install();using(var r=new Reader(world,zone,Owner("Player")))using(var paint=new Paint(world,zone))
            {
                var shield=Owner("Buckler");var pack=r.Viewer.GetPart<InventoryPart>();Assert.True(pack.AddObject(shield));
                var hand=r.Viewer.GetPart<Body>().GetParts().First(p=>p.Type=="Hand"&&p.Equipped==null);
                Assert.True(InventorySystem.Equip(r.Viewer,shield,hand));Assert.AreSame(shield,hand.Equipped);r.Presentation.Paused=true;
                var equipped=ExamineDocumentBuilder.BuildInventory(shield,r.Viewer);Portrait(r,equipped,inputs[6]);Assert.IsNull(zone.GetEntityCell(shield));
                Assert.True(InventorySystem.UnequipItem(r.Viewer,shield));Assert.IsNull(hand.Equipped);Assert.IsNull(shield.GetPart<PhysicsPart>().Equipped);
                Portrait(r,ExamineDocumentBuilder.BuildInventory(shield,r.Viewer),inputs[6]);paint.Step();long full=EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.True(InventorySystem.Drop(r.Viewer,shield,zone));Retired(r,equipped);var c=zone.GetEntityCell(shield);Assert.NotNull(c);
                Assert.True(zone.MoveEntity(r.Viewer,8,Y));var body=Native(shield);paint.Dirty(c.X,c.Y);paint.Step();Assert.AreSame(body,paint.At(c.X,c.Y));Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);
                var ground=ExamineDocumentBuilder.BuildWorld(shield,zone,c,r.Viewer);Portrait(r,ground,inputs[6]);
                Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(shield,false),r.Viewer,zone).Success);Retired(r,ground);
                paint.Dirty(c.X,c.Y);paint.Step();Assert.IsNull(paint.At(c.X,c.Y));Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);
                Assert.True(InventorySystem.Equip(r.Viewer,shield,hand));Assert.AreSame(shield,hand.Equipped);Portrait(r,ExamineDocumentBuilder.BuildInventory(shield,r.Viewer),inputs[6]);
            }
        }

        [Test] public void IndependentOptionalBucklerTexturePreservesEveryOlderConstructorAndPeer()
        {
            foreach(int count in new[]{2,3,6})using(var old=Construct(inputs.Take(count).ToArray()))
            {for(int i=0;i<count;i++){Assert.True(old.TryResolve(Owner(Peers[i]),out var tile));Frame(tile,inputs[i],i<3?24:16);}Assert.False(old.TryResolve(Owner("Buckler"),out _));}
            foreach(var bad in new[]{null,Texture(16,16)})
            {
                Install(bad,true);Attach(ReferenceGladePlan.ZoneID);var shield=Add("Buckler");Refused(shield);
                for(int i=0;i<6;i++){Assert.True(art.TryResolve(Owner(Peers[i]),out var tile));Frame(tile,inputs[i],i<3?24:16);}
            }
            Install();Attach(ReferenceGladePlan.ZoneID);Native(Add("Buckler"));
            world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);Assert.False(View().NativeForeground,"Explicit no-resource world cannot reload Buckler.");
        }

        [Test] public void EssentialAndPresentOptionalPartsMustBelongUniquelyToThisShield()
        {
            Install();
            foreach(var type in new[]{typeof(RenderPart),typeof(PhysicsPart),typeof(ExaminablePart),typeof(ArmorPart),typeof(EquippablePart),typeof(StackerPart),typeof(StatusEffectsPart),typeof(SpatialFootprintPart)})
            foreach(string fault in new[]{"missing","foreign","duplicate"})
            {
                Attach(ReferenceGladePlan.ZoneID);var shield=Add("Buckler");var part=shield.Parts.FirstOrDefault(p=>p.GetType()==type);
                if(part==null){part=(Part)Activator.CreateInstance(type);if(part is SpatialFootprintPart footprint)footprint.CellsRaw="0,0";shield.AddPart(part);}Native(shield);
                bool optional=type==typeof(StackerPart)||type==typeof(StatusEffectsPart)||type==typeof(SpatialFootprintPart);
                if(fault=="missing")Assert.True(shield.RemovePart(part));else if(fault=="foreign")part.ParentEntity=new Entity();else{var duplicate=(Part)Activator.CreateInstance(type);duplicate.ParentEntity=shield;shield.Parts.Add(duplicate);}
                if(optional&&fault=="missing")Native(shield);else Refused(shield);
            }
        }

        [Test] public void ForeignRolesReskinsWrongSlotAndOtherShieldIdentitiesNeverAcquireBucklerArt()
        {
            Install();
            foreach(string fault in new[]{"slot","visual","variant","glyphs","no-item","creature","corpse","solid","fixed","empty","overfull","container","weapon","service","speaker"})
            {
                Attach(ReferenceGladePlan.ZoneID);var shield=Add("Buckler");Native(shield);
                switch(fault){case"slot":shield.GetPart<EquippablePart>().Slot="Body";break;case"visual":shield.GetPart<RenderPart>().VisualID="other";break;case"variant":shield.GetPart<RenderPart>().VisualVariant="other";break;case"glyphs":shield.GetPart<RenderPart>().GlyphVariants="!?";break;case"no-item":shield.Tags.Remove("Item");break;case"creature":shield.SetTag("Creature");break;case"corpse":shield.SetTag("Corpse");break;case"solid":shield.GetPart<PhysicsPart>().Solid=true;break;case"fixed":shield.GetPart<PhysicsPart>().Takeable=false;break;case"empty":shield.GetPart<StackerPart>().StackCount=0;break;case"overfull":shield.GetPart<StackerPart>().StackCount=shield.GetPart<StackerPart>().MaxStack+1;break;case"container":shield.AddPart(new ContainerPart());break;case"weapon":shield.AddPart(new MeleeWeaponPart());break;case"service":shield.AddPart(new ForgePart());break;case"speaker":shield.AddPart(new ConversationPart());break;}
                Refused(shield);
            }
            var alias=Owner("IronBuckler");alias.Properties["SourceBlueprint"]="Buckler";Assert.False(art.TryResolve(alias,out _));
            var valid=Owner("Buckler");Assert.True(art.TryResolve(valid,out _));valid.BlueprintName="buckler";Assert.False(art.TryResolve(valid,out _));
        }

        [Test] public void MutableConditionStaysNeutralWhileOneRenderEventRetainsItsRealColorCue()
        {
            Install();var shield=Add("Buckler");var body=Native(shield);var probe=new Probe();shield.AddPart(probe);
            shield.GetPart<RenderPart>().DisplayName="an old named shield";shield.GetPart<RenderPart>().ColorString="&R";
            shield.GetPart<ArmorPart>().AV=7;shield.GetPart<ArmorPart>().DV=-2;shield.GetStat("Hitpoints").BaseValue=1;shield.GetPart<CommercePart>().Value=99;
            Assert.IsNull(shield.GetPart<ThermalPart>());Assert.IsNull(shield.GetPart<RepairablePart>());Assert.IsNull(shield.GetPart<CompositionPart>());
            int parts=shield.Parts.Count;for(int i=0;i<3;i++)Assert.AreSame(body,Native(shield));Assert.Zero(probe.Renders);Assert.AreEqual(parts,shield.Parts.Count);
            using(var paint=new Paint(world,zone))
            {
                paint.Step();Assert.AreEqual(Color.white,paint.Tint(X,Y));int before=probe.Renders;
                probe.Color="&B";paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(before+1,probe.Renders);
                Assert.AreEqual(QudColorParser.Parse("&B"),paint.Tint(X,Y));
                using(var r=new Reader(world,zone,Owner("Player")))Portrait(r,ExamineDocumentBuilder.BuildWorld(shield,zone,zone.GetEntityCell(shield),r.Viewer),inputs[6]);
                Assert.AreEqual(before+1,probe.Renders,"Portrait must not dispatch another Render.");
                probe.Color=null;paint.Dirty(X,Y);paint.Step();Assert.AreEqual(Color.white,paint.Tint(X,Y));
            }
            Assert.AreEqual(7,shield.GetPart<ArmorPart>().AV);Assert.AreEqual(-2,shield.GetPart<ArmorPart>().DV);Assert.AreEqual(1,shield.GetStatValue("Hitpoints"));
        }

        [Test] public void CoveredHiddenStaleFootprintAndReplacedGraphKeepTheirExistingAuthority()
        {
            Install();var shield=Add("Buckler");var footprint=new SpatialFootprintPart{CellsRaw="0,0"};shield.AddPart(footprint);var body=Native(shield);
            using(var r=new Reader(world,zone,Owner("Player")))
            {
                var document=ExamineDocumentBuilder.BuildWorld(shield,zone,zone.GetEntityCell(shield),r.Viewer);
                var cover=Add("GoldCoin");cover.GetPart<RenderPart>().RenderLayer=100;Assert.AreSame(cover,View().Owner);Portrait(r,document,inputs[6]);Assert.True(zone.RemoveEntity(cover));Assert.AreSame(body,Native(shield));
                zone.GetCell(X,Y).IsVisible=false;Assert.False(View().NativeForeground);Assert.True(r.Presentation.TryGetFlatExaminePortrait(document,out var hidden));Assert.IsNull(hidden.Texture);zone.GetCell(X,Y).IsVisible=true;
                Assert.True(zone.TryChangeFootprint(shield,"0,0;1,0"));Assert.False(art.TryResolve(shield,out _));footprint.CellsRaw="0,0";Assert.False(art.TryResolve(shield,out _));
                Assert.True(zone.TryChangeFootprint(shield,"0,0"));Assert.AreSame(body,Native(shield));Portrait(r,document,inputs[6]);
                var old=zone;manager.SetActiveZone(new Zone(zone.ZoneID));Assert.False(world.TryResolvePresentation(old,X,Y,out _));Assert.True(r.Presentation.TryGetFlatExaminePortrait(document,out var stale));Assert.IsNull(stale.Texture);
            }
        }

        [Test] public void CachedBucklerFramesAreOwnedAndBorrowedPixelsRemainUntouched()
        {
            Install();var owner=Owner("Buckler");Assert.True(art.TryResolve(owner,out var tile));var sprite=tile.sprite;
            using(var peer=Construct(inputs))
            {
                for(int i=0;i<3;i++){Assert.True(art.TryResolve(owner,out var same));Assert.AreSame(tile,same);}
                art.Dispose();art.Dispose();Assert.True(tile==null);Assert.True(sprite==null);Assert.False(art.TryResolve(owner,out _));
                Assert.True(peer.TryResolve(owner,out var independent));Frame(independent,inputs[6]);Assert.True(inputs[6]!=null);
                Assert.AreEqual(FilterMode.Bilinear,inputs[6].filterMode);Assert.AreEqual(TextureWrapMode.Repeat,inputs[6].wrapMode);
            }
        }
        sealed class Probe : Part
        {public int Renders;public string Color;public override bool HandleEvent(GameEvent e){if(e.ID=="Render"){Renders++;if(Color!=null)e.SetParameter("ColorString",Color);}return true;}}
        sealed class Reader : IDisposable
        {
            readonly GameObject root;
            readonly Zone ownedZone;
            public readonly Entity Viewer;
            public readonly ZoneRenderer Presentation;
            public Reader(FlatPixelGreyboxCatalog catalog,Zone z,Entity viewer)
            {
                Viewer=viewer;ownedZone=z;Assert.True(z.AddEntity(viewer,9,10));root=new GameObject("Buckler reader");root.SetActive(false);
                Presentation=root.AddComponent<ZoneRenderer>();var environment=root.AddComponent<EnvironmentSpriteRenderer>();
                Write(environment,"_flatPixelGreybox",catalog);environment.FlatPixelGreyboxEnabled=true;
                Write(Presentation,"_envSpriteRenderer",environment);Write(Presentation,"_flatPixelModeEnabled",true);
                typeof(ZoneRenderer).GetProperty("CurrentZone",I).SetValue(Presentation,z);Assert.True(Presentation.FlatPixelModeActive);
            }
            public void Dispose(){if(root!=null){Write(root.GetComponent<EnvironmentSpriteRenderer>(),"_flatPixelGreybox",null);Object.DestroyImmediate(root);}if(ownedZone.GetEntityCell(Viewer)!=null)ownedZone.RemoveEntity(Viewer);}
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Buckler submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Buckler fixture");
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
        void Snapshot(FieldInfo field)
        {
            Assert.NotNull(field); object value = field.GetValue(null);
            if (value is Array array) { var copy = (Array)array.Clone(); restore.Add(() => Array.Copy(copy, array, copy.Length)); }
            else if (value is IDictionary dictionary)
            {
                var copy = new List<DictionaryEntry>(); foreach (DictionaryEntry entry in dictionary) copy.Add(entry);
                restore.Add(() => { dictionary.Clear(); foreach (var entry in copy) dictionary.Add(entry.Key, entry.Value); });
            }
            else if (value is IList list) { var copy = list.Cast<object>().ToArray(); restore.Add(() => { list.Clear(); foreach (var item in copy) list.Add(item); }); }
            else if (value is IEnumerable enumerable && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(Queue<>))
            {
                var copy = enumerable.Cast<object>().ToArray(); restore.Add(() => { value.GetType().GetMethod("Clear").Invoke(value, null);
                    foreach (var item in copy) value.GetType().GetMethod("Enqueue").Invoke(value, new[] { item }); });
            }
            if (!field.IsInitOnly) restore.Add(() => field.SetValue(null, value));
        }
    }
}
