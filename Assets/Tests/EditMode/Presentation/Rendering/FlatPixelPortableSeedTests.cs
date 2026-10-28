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
    // Only the two portable identities are new. Existing PlotProduct/Essentials
    // fixtures retain the broader art, fog, paint and transaction counter-checks.
    public sealed class FlatPixelPortableSeedTests
    {
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        const string ProductsPath="Sprites/Live16x24/plot-products";
        readonly List<Action> restore=new List<Action>();
        readonly List<Texture2D> textures=new List<Texture2D>();
        HotbarSaveFixture globals;
        EntityFactory factory;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        Texture2D products;
        [SetUp] public void Setup()
        {
            foreach(var type in new[]{typeof(MessageLog),typeof(EquipmentChangeBus),typeof(WorldAudioHooks),typeof(WorldAudioUtility),typeof(EntityVisualHooks)})
                foreach(var field in type.GetFields(S))if(!field.IsLiteral)Snapshot(field);
            Snapshot(typeof(AnatomyFactory).GetField("_types",S));Snapshot(typeof(BodyPart).GetField("_nextID",S));
            foreach(string name in new[]{"_buffer","_writeIndex","_filledCount","_droppedCount"})Snapshot(typeof(Diag).GetField(name,S));
            globals=new HotbarSaveFixture(false,false);MessageLog.OnMessage=null;MessageLog.Clear();WorldAudioHooks.Callback=null;WorldAudioUtility.Callback=null;EntityVisualHooks.Reset();
            ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;
            factory=DensityReferenceGladeTests.Factory();manager=OverworldZoneManager.CreateDetached(factory,65,true);
            Attach("Overworld.18.8.0");world=new FlatPixelGreyboxCatalog();products=Resources.Load<Texture2D>(ProductsPath);
        }
        [TearDown] public void Cleanup()
        {try{world?.Dispose();foreach(var t in textures)if(t!=null)Object.DestroyImmediate(t);textures.Clear();globals?.Dispose();}
         finally{for(int i=restore.Count-1;i>=0;i--)restore[i]();restore.Clear();}}
        void Attach(string id){zone=new Zone(id);manager.SetActiveZone(zone);foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;}
        Entity Owner(string name){var e=factory.CreateEntity(name);Assert.NotNull(e,name);return e;}
        Entity Add(string name,int x=10,int y=10){var e=Owner(name);Assert.True(zone.AddEntity(e,x,y));return e;}
        Texture2D Texture(int w,int h){var t=new Texture2D(w,h);textures.Add(t);return t;}
        FlatPixelGreyboxCell View(int x=10,int y=10){Assert.True(world.TryResolvePresentation(zone,x,y,out var v));return v;}
        Tile Native(Entity owner,int frame)
        {
            var c=zone.GetEntityCell(owner);Assert.NotNull(c);var v=View(c.X,c.Y);Assert.AreSame(owner,v.Owner);
            Assert.True(v.NativeForeground,"Portable seed still uses glyph fallback outside the opening.");
            Frame(v.ForegroundTile,products,frame);Assert.AreEqual(Color.white,v.ForegroundColor);return v.ForegroundTile;
        }
        static void Frame(Tile tile,Texture2D texture,int frame)
        {
            Assert.NotNull(texture);Assert.NotNull(tile);Assert.AreSame(texture,tile.sprite.texture);
            Assert.AreEqual(new Rect(frame*16,0,16,24),tile.sprite.rect);Assert.AreEqual(new Vector2(8,12),tile.sprite.pivot);
            Assert.AreEqual(16,tile.sprite.pixelsPerUnit);Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);
        }
        static void Write(object o,string name,object value){var f=o.GetType().GetField(name,I);Assert.NotNull(f,name);f.SetValue(o,value);}
        FlatPixelExpeditionEssentialsCatalog Expanded(Texture2D items,Texture2D book,Texture2D plot)
        {
            var c=typeof(FlatPixelExpeditionEssentialsCatalog).GetConstructor(new[]{typeof(Texture2D),typeof(Texture2D),typeof(Texture2D)});
            Assert.NotNull(c,"The independently optional plot-products constructor is missing.");
            return(FlatPixelExpeditionEssentialsCatalog)c.Invoke(new object[]{items,book,plot});
        }
        FlatPixelGreyboxCatalog Eleven(Texture2D plot)
            =>new FlatPixelGreyboxCatalog(null,null,null,null,null,null,null,null,null,null,plot);
        void Portrait(Reader reader,AnnouncementDocument document,int frame)
        {
            Assert.NotNull(document);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document,out var image));
            Assert.AreSame(products,image.Texture,"Authorized seed reader still uses glyph fallback.");
            Assert.AreEqual(new Rect(frame/8f,0,1f/8f,1),image.Uv);Assert.False(image.IsModel);Assert.AreEqual(Color.white,image.Tint);
        }
        static void Retired(Reader r,AnnouncementDocument d)
        {Assert.False(ExamineDocumentBuilder.IsCurrentSubject(d));Assert.True(r.Presentation.TryGetFlatExaminePortrait(d,out var image));Assert.IsNull(image.Texture);}

        [TestCase("CandyCarrotSeed",1)] [TestCase("EmberwheatSeed",2)]
        public void ExactGroundAndCarriedSeedUseTheApprovedFrameOutsideAndInsideOpening(string name,int frame)
        {
            foreach(string id in new[]{"Overworld.18.8.0",ReferenceGladePlan.ZoneID})
            {
                Attach(id);var seed=Add(name);var body=Native(seed,frame);
                using(var reader=new Reader(world,zone,Owner("Player")))
                {
                    var ground=ExamineDocumentBuilder.BuildWorld(seed,zone,zone.GetEntityCell(seed),reader.Viewer);Portrait(reader,ground,frame);
                    Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(seed,false),reader.Viewer,zone).Success);
                    Retired(reader,ground);reader.Presentation.Paused=true;
                    var carried=ExamineDocumentBuilder.BuildInventory(seed,reader.Viewer);Portrait(reader,carried,frame);
                    Assert.AreEqual(1,seed.GetPart<StackerPart>().StackCount);Assert.False(zone.GetCell(9,10).HasObjectWithPart<CropPart>());
                    reader.Viewer.GetPart<InventoryPart>().RemoveObject(seed);Retired(reader,carried);
                }
                Assert.AreSame(products,body.sprite.texture);
            }
        }

        [Test] public void OptionalThirdAtlasPreservesOldConstructorsWithoutImplicitResourceReload()
        {
            var items=Texture(192,24);var book=Texture(16,16);var atlas=Texture(128,24);var bad=Texture(127,24);
            var seed=Owner("CandyCarrotSeed");
            using(var old=new FlatPixelExpeditionEssentialsCatalog(items,book))
            {Assert.True(old.TryResolve(Owner("HealingTonic"),out _));Assert.True(old.TryResolve(Owner("DriedMeat"),out _));Assert.True(old.TryResolve(Owner("WateringGrimoire"),out _));Assert.False(old.TryResolve(seed,out _));}
            foreach(var optional in new[]{atlas,null,bad})using(var art=Expanded(items,book,optional))
            {
                Assert.AreEqual(optional==atlas,art.TryResolve(seed,out var tile));if(tile!=null)Frame(tile,atlas,1);
                foreach(string name in new[]{"HealingTonic","DriedMeat","WateringGrimoire"})Assert.True(art.TryResolve(Owner(name),out _),name);
            }
            foreach(string id in new[]{ReferenceGladePlan.ZoneID,"Overworld.18.8.0"})foreach(var optional in new[]{atlas,null,bad})
            {
                Attach(id);world.Dispose();world=Eleven(optional);var owner=Add("EmberwheatSeed");
                var value=View();Assert.AreSame(owner,value.Owner);Assert.AreEqual(optional==atlas,value.NativeForeground,id);
                if(value.NativeForeground)Frame(value.ForegroundTile,atlas,2);
                if(FlatPixelGreyboxCatalog.IsAdmitted(zone)){Assert.True(world.TryResolve(zone,10,10,out var legacy));Assert.AreEqual(optional==atlas,legacy.NativeForeground);}
            }
            Assert.AreEqual(FilterMode.Bilinear,atlas.filterMode,"Borrowed texture settings are untouched.");
        }

        [Test] public void ChangedSeedFunctionCannotRegainArtThroughAnyOlderOpeningRoute()
        {
            Attach(ReferenceGladePlan.ZoneID);
            foreach(string name in new[]{"CandyCarrotSeed","EmberwheatSeed"})
            {
                var seed=Add(name);int frame=name=="CandyCarrotSeed"?1:2;Native(seed,frame);
                using(var art=Expanded(null,null,products))
                {
                    var part=seed.GetPart<SeedPart>();string crop=part.CropBlueprint;
                    part.CropBlueprint="MarlrootCrop";Refused(seed,art);part.CropBlueprint=crop;
                    part.RequireCultivatedSoil=true;Refused(seed,art);part.RequireCultivatedSoil=false;
                    part.ParentEntity=new Entity();Refused(seed,art);part.ParentEntity=seed;
                    Assert.True(seed.RemovePart(part));Refused(seed,art);seed.AddPart(part);
                    seed.GetPart<RenderPart>().VisualID="custom-seed";Refused(seed,art);seed.GetPart<RenderPart>().VisualID="";
                    Assert.True(art.TryResolve(seed,out var restored));Frame(restored,products,frame);Native(seed,frame);
                    Assert.False(art.TryResolve(Owner("MarlrootSeed"),out _));Assert.False(art.TryResolve(Owner(crop),out _));
                }
                Assert.True(zone.RemoveEntity(seed));
            }
            var reserve=Add("MarlrootSeed");Assert.True(world.TryResolve(zone,10,10,out var other));Frame(other.ForegroundTile,products,4);
            Assert.AreSame(reserve,other.Owner);
        }
        void Refused(Entity owner,FlatPixelExpeditionEssentialsCatalog art)
        {
            Assert.False(art.TryResolve(owner,out _));Assert.False(View().NativeForeground);
            Assert.True(world.TryResolve(zone,10,10,out var legacy));Assert.AreNotSame(products,legacy.ForegroundTile?.sprite?.texture);
            if(world.TryGetSubjectSprite(owner,10,10,out var oldSprite,out _))Assert.AreNotSame(products,oldSprite.texture);
            if(world.TryGetPresentationSubjectSprite(zone,owner,10,10,out var sprite,out _))Assert.AreNotSame(products,sprite.texture);
        }

        [Test] public void RealDropPickupAndPlantKeepOnlyTheRemainingOwnedSeedBody()
        {
            typeof(TurnManager).GetProperty("Active",S).SetValue(null,null);SeedPart.Factory=factory;SettlementRuntime.ActiveZone=zone;
            using(var reader=new Reader(world,zone,Owner("Player")))
            {
                reader.Presentation.Paused=true;var actor=reader.Viewer;var pack=actor.GetPart<InventoryPart>();
                var seed=Owner("CandyCarrotSeed");seed.GetPart<StackerPart>().StackCount=2;Assert.True(pack.AddObject(seed));
                var carried=ExamineDocumentBuilder.BuildInventory(seed,actor);Portrait(reader,carried,1);
                Assert.AreEqual(2,seed.GetPart<StackerPart>().StackCount);Assert.False(zone.GetCell(9,10).HasObjectWithPart<CropPart>());
                Assert.True(InventorySystem.Drop(actor,seed,zone));Retired(reader,carried);Assert.True(zone.MoveEntity(actor,9,11));Native(seed,1);
                var ground=ExamineDocumentBuilder.BuildWorld(seed,zone,zone.GetEntityCell(seed),actor);Portrait(reader,ground,1);
                Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(seed,false),actor,zone).Success);Retired(reader,ground);
                Assert.True(zone.MoveEntity(actor,9,10));Add("Grass",9,10);
                carried=ExamineDocumentBuilder.BuildInventory(seed,actor);Portrait(reader,carried,1);
                Assert.True(InventorySystem.PerformAction(actor,seed,"PlantSeed",zone));Assert.AreEqual(1,seed.GetPart<StackerPart>().StackCount);
                var crop=zone.GetCell(9,10).Objects.Single(e=>e.BlueprintName=="CandyCarrotCrop");Assert.AreEqual(0,crop.GetPart<CropPart>().GrowthStage);
                Portrait(reader,ExamineDocumentBuilder.BuildInventory(seed,actor),1);
                Assert.False(InventorySystem.PerformAction(actor,seed,"PlantSeed",zone));Assert.AreEqual(1,seed.GetPart<StackerPart>().StackCount);
                Assert.True(zone.MoveEntity(actor,9,11));Add("Grass",9,11);carried=ExamineDocumentBuilder.BuildInventory(seed,actor);
                Assert.True(InventorySystem.PerformAction(actor,seed,"PlantSeed",zone));Assert.False(pack.Objects.Contains(seed));Retired(reader,carried);
                Assert.IsNull(zone.GetEntityCell(seed));Assert.AreNotSame(seed,View(9,10).Owner);Assert.AreNotSame(seed,View(9,11).Owner);
            }
        }

        sealed class Reader : IDisposable
        {
            readonly GameObject root;
            readonly Zone ownedZone;
            public readonly Entity Viewer;
            public readonly ZoneRenderer Presentation;
            public Reader(FlatPixelGreyboxCatalog catalog,Zone z,Entity viewer)
            {
                Viewer=viewer;ownedZone=z;Assert.True(z.AddEntity(viewer,9,10));root=new GameObject("Portable seed reader");root.SetActive(false);
                Presentation=root.AddComponent<ZoneRenderer>();var environment=root.AddComponent<EnvironmentSpriteRenderer>();
                Write(environment,"_flatPixelGreybox",catalog);environment.FlatPixelGreyboxEnabled=true;
                Write(Presentation,"_envSpriteRenderer",environment);Write(Presentation,"_flatPixelModeEnabled",true);
                typeof(ZoneRenderer).GetProperty("CurrentZone",I).SetValue(Presentation,z);Assert.True(Presentation.FlatPixelModeActive);
            }
            public void Dispose(){if(root!=null){Write(root.GetComponent<EnvironmentSpriteRenderer>(),"_flatPixelGreybox",null);Object.DestroyImmediate(root);}if(ownedZone.GetEntityCell(Viewer)!=null)ownedZone.RemoveEntity(Viewer);}
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
