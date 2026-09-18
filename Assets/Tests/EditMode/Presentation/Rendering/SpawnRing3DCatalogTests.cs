using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Metadata-only unit fixture: the agreed native coverage and art names are real;
    // its unit-cube bounds/12-triangle numbers are deliberately synthetic controls.
    // Actual FBX/skin/clip/bounds tests are a separate post-export gate.
    public sealed class SpawnRing3DCatalogTests
    {
        const string CatalogName="CavesOfOoo.Rendering.SpawnRing3DCatalog";
        const string LibraryName="CavesOfOoo.Rendering.SpawnRing3DLibrary";
        const string FixtureRelative="Tests/EditMode/Presentation/Rendering/Fixtures/spawn-ring-catalog-unit-fixture.json";
        [Serializable] public sealed class Doc
        {
            public int schemaVersion,zoneWidth,zoneHeight;
            public string id,coordinates,paletteTexture;
            public Model[] models;public Blueprint[] blueprints;public Owner[] fellingOwners;
            public Water tileState;public string[] zones,materialSlots;public Gear externalEquipment;
        }
        [Serializable] public sealed class Model
        {
            public string id,path,kind,rigFamily,materialFamily,sourceBlueprint;
            public bool rigged;public Vector3 boundsCenter,boundsSize;public int triangles;
            public string[] clips,sockets;
        }
        [Serializable] public sealed class Blueprint {public string blueprint,role,resolver;public string[] models;}
        [Serializable] public sealed class Owner {public string componentId,modelId;public bool mutable;}
        [Serializable] public sealed class Water {public string waterModel;public float height;}
        [Serializable] public sealed class Gear {public string library;public string[] models;}
        static Type RequiredType(string name)
        {
            var t=typeof(Village3DPresenter).Assembly.GetType(name);
            Assert.NotNull(t,"Record missing-type RED before implementing "+name);return t;
        }
        static string Text()
        {
            string path=Path.Combine(Application.dataPath,FixtureRelative);
            Assert.IsTrue(File.Exists(path),"Adopt the unit fixture alongside these tests: "+path);
            return File.ReadAllText(path);
        }
        static Doc Fresh()=>JsonUtility.FromJson<Doc>(Text());
        static object Invoke(object target,string name,params object[] args)
        {
            var m=target.GetType().GetMethod(name,BindingFlags.Public|BindingFlags.Instance);
            Assert.NotNull(m,"Required catalog/library operation "+name);
            try{return m.Invoke(target,args);}catch(TargetInvocationException e){throw e.InnerException??e;}
        }
        static object Parse(string json)
        {
            var t=RequiredType(CatalogName);
            var m=t.GetMethod("Parse",new[]{typeof(string),typeof(FellingSceneDefinition)});
            Assert.NotNull(m,"Parse validates metadata against the actual native Felling definition.");
            var native=FellingSceneDefinition.Load();Assert.NotNull(native);
            try{return m.Invoke(null,new object[]{json,native});}catch(TargetInvocationException e){throw e.InnerException??e;}
        }
        static object Parse(Doc d)=>Parse(JsonUtility.ToJson(d));
        static object Get(object target,string field)
        {
            var f=target.GetType().GetField(field);Assert.NotNull(f,"Required serialized field "+field);return f.GetValue(target);
        }
        static void Set(object target,string field,object value)
        {
            var f=target.GetType().GetField(field);Assert.NotNull(f,"Required serialized field "+field);f.SetValue(target,value);
        }
        static object Definition(object target)
        {
            var p=target.GetType().GetProperty("Definition");Assert.NotNull(p);
            try{return p.GetValue(target);}catch(TargetInvocationException e){throw e.InnerException??e;}
        }
        static Model ModelFor(Doc d,string blueprint)=>d.models.Single(m=>m.id==d.blueprints.Single(b=>b.blueprint==blueprint).models[0]);
        static void Reject(Doc d)=>Assert.Throws<ArgumentException>(()=>Parse(d));

        [Test] public void CompleteCoverageResolvesExactNativeFellingOwnersAndEightZones()
        {
            var d=Fresh();var parsed=Parse(d);
            Assert.AreEqual(73,d.blueprints.Length);Assert.AreEqual(55,d.fellingOwners.Length);
            Assert.AreEqual(39,d.fellingOwners.Count(o=>o.mutable));Assert.AreEqual(8,d.zones.Length);
            Assert.AreEqual(4,d.blueprints.Single(b=>b.blueprint=="GlowQuartzVein").models.Length,"Actual alternate-seed vein needs its four authored variants.");
            foreach(var b in d.blueprints)Assert.NotNull(Invoke(parsed,"FindBlueprint",b.blueprint),b.blueprint);
            foreach(var o in d.fellingOwners)Assert.NotNull(Invoke(parsed,"FindFellingOwner",o.componentId),o.componentId);
            foreach(string z in d.zones)Assert.AreEqual(true,Invoke(parsed,"SupportsZone",z),z);
            Assert.AreEqual(false,Invoke(parsed,"SupportsZone","Overworld.3.6.0"));
            Assert.AreEqual(false,Invoke(parsed,"SupportsZone","Overworld.3.5.1"));
        }
        [Test] public void UnknownRuntimeNamesAreHonestFallbackWhileDeclaredReferencesAreStrict()
        {
            var p=Parse(Fresh());
            foreach(string unknown in new[]{null,"","DroppedForeignItem","morrowfast-door-west"})
            {
                Assert.IsNull(Invoke(p,"FindBlueprint",new object[]{unknown}));
                Assert.IsNull(Invoke(p,"FindFellingOwner",new object[]{unknown}));
                Assert.IsNull(Invoke(p,"FindModel",new object[]{unknown}));
                Assert.AreEqual(false,Invoke(p,"SupportsZone",new object[]{unknown}));
            }
            Assert.NotNull(Invoke(p,"FindBlueprint","Player"));Assert.NotNull(Invoke(p,"FindModel","ring-player"));
        }
        [Test] public void PositiveRigAndMaterialFamiliesIncludeSpeciesWithoutInventedEquipmentHands()
        {
            var d=Fresh();var p=Parse(d);
            foreach(var pair in new[]{new[]{"Player","humanoid"},new[]{"CascadeFather","frog"},new[]{"YellowfootWayfarer","tortoise"},
                new[]{"Wardline","serpent"},new[]{"SkySari","avian"},new[]{"Mosshulk","fungal"},new[]{"ChoirTendril","rooted"}})
            {
                var model=ModelFor(d,pair[0]);Assert.AreEqual(pair[1],model.rigFamily);
                Assert.AreEqual(5,model.clips.Length);Assert.AreEqual(pair[1]=="humanoid"?4:0,model.sockets.Length);
                Assert.NotNull(Invoke(p,"FindModel",model.id));
            }
            Assert.AreEqual("ring-water",d.models.Single(m=>m.id==d.tileState.waterModel).materialFamily);
        }
        [TestCase("header")] [TestCase("coordinates")] [TestCase("palette")]
        [TestCase("missing-or-duplicate-blueprint")] [TestCase("role")] [TestCase("empty-model-list")]
        [TestCase("model-reference")] [TestCase("model-role")] [TestCase("actor-rig")]
        [TestCase("sockets")] [TestCase("clips")] [TestCase("duplicate-model")]
        [TestCase("model-path")] [TestCase("geometry")] [TestCase("material-family")]
        [TestCase("felling-identity")] [TestCase("felling-mutation")] [TestCase("felling-reference")]
        [TestCase("zones")] [TestCase("water")] [TestCase("equipment")] [TestCase("missing-collections")]
        public void MalformedAuthoredCatalogIsRejectedBeforeBinding(string mutation)
        {
            var d=Fresh();Assert.DoesNotThrow(()=>Parse(d));
            switch(mutation)
            {
                case "header":d.schemaVersion=2;Reject(d);d=Fresh();d.id="village";Reject(d);d=Fresh();d.zoneWidth=79;break;
                case "coordinates":d.coordinates="Blender axes without conversion";break;
                case "palette":d.paletteTexture="../other.png";break;
                case "missing-or-duplicate-blueprint":d.blueprints=d.blueprints.Skip(1).ToArray();Reject(d);d=Fresh();d.blueprints[1]=d.blueprints[0];break;
                case "role":d.blueprints[0].role="decoration-everything";break;
                case "empty-model-list":d.blueprints[0].models=Array.Empty<string>();Reject(d);d=Fresh();d.blueprints.Single(b=>b.blueprint=="FellingSceneProp").resolver="Unknown";break;
                case "model-reference":d.blueprints[0].models=new[]{"unknown-model"};break;
                case "model-role":ModelFor(d,"Player").kind="ground";break;
                case "actor-rig":ModelFor(d,"YellowfootWayfarer").rigFamily="humanoid";ModelFor(d,"YellowfootWayfarer").sockets=ModelFor(d,"Player").sockets;break;
                case "sockets":ModelFor(d,"Wardline").sockets=new[]{"Equipment.Hand.L"};Reject(d);d=Fresh();ModelFor(d,"Player").sockets=new[]{"Equipment.Head"};break;
                case "clips":ModelFor(d,"SkySari").clips=new[]{"Idle","Walk","Interact","Attack","Attack"};break;
                case "duplicate-model":d.models[1]=d.models[0];break;
                case "model-path":d.models[0].path="models/../"+d.models[0].id+".fbx";Reject(d);d=Fresh();d.models[0].id="../unsafe";break;
                case "geometry":d.models[0].triangles=0;Reject(d);d=Fresh();d.models[0].boundsSize=Vector3.zero;Reject(d);d=Fresh();d.models[0].boundsCenter=new Vector3(float.PositiveInfinity,0,0);break;
                case "material-family":d.models[0].materialFamily="village-palette";Reject(d);d=Fresh();d.materialSlots=new[]{"SpawnRingPalette","UnrecognizedWater"};break;
                case "felling-identity":d.fellingOwners[0].componentId="invented-owner";Reject(d);d=Fresh();d.fellingOwners=d.fellingOwners.Skip(1).ToArray();break;
                case "felling-mutation":d.fellingOwners[0].mutable=!d.fellingOwners[0].mutable;break;
                case "felling-reference":d.fellingOwners[0].modelId=ModelFor(d,"Player").id;break;
                case "zones":d.zones[0]="Overworld.3.6.0";Reject(d);d=Fresh();d.zones[1]=d.zones[0];break;
                case "water":d.tileState.waterModel=ModelFor(d,"Player").id;Reject(d);d=Fresh();d.tileState.height=-1;break;
                case "equipment":d.externalEquipment.models=new[]{"fake-sword"};Reject(d);d=Fresh();d.externalEquipment.library="OtherLibrary";break;
                case "missing-collections":d.models=null;Reject(d);d=Fresh();d.blueprints=null;Reject(d);d=Fresh();d.fellingOwners=null;Reject(d);d=Fresh();d.tileState=null;break;
                default:Assert.Fail("Unhandled negative control "+mutation);break;
            }
            Reject(d);
        }
        sealed class LibraryFixture:IDisposable
        {
            public readonly ScriptableObject Library;public readonly Village3DLibrary Village;
            readonly List<Object> owned=new List<Object>();
            public LibraryFixture()
            {
                try
                {
                    Library=ScriptableObject.CreateInstance(RequiredType(LibraryName));owned.Add(Library);
                    Village=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);Assert.NotNull(Village);Village.Validate();
                    var catalog=new TextAsset(Text());owned.Add(catalog);Set(Library,"Catalog",catalog);
                    Set(Library,"WorldMaterial",Village.WorldMaterial);Set(Library,"WaterMaterial",Village.WaterMaterial);
                    Set(Library,"CompositeMaterial",Village.CompositeMaterial);Set(Library,"Renderer",Village.Renderer);Set(Library,"RendererIndex",Village.RendererIndex);
                    Set(Library,"EquipmentLibrary",Village);
                    var field=Library.GetType().GetField("Models");Assert.NotNull(field);var element=field.FieldType.GetElementType();Assert.NotNull(element);
                    var models=Fresh().models;var array=Array.CreateInstance(element,models.Length);
                    var usable=Village.Models.First(x=>x!=null&&x.Prefab!=null).Prefab;
                    for(int i=0;i<models.Length;i++)
                    {var b=Activator.CreateInstance(element);Set(b,"Id",models[i].id);Set(b,"Prefab",usable);array.SetValue(b,i);}
                    field.SetValue(Library,array);
                }
                catch{Dispose();throw;}
            }
            public void Dispose(){for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)Object.DestroyImmediate(owned[i]);}
            public TextAsset OwnText(string text){var value=new TextAsset(text);owned.Add(value);return value;}
        }
        [Test] public void LibraryUsesExplicitResourcesAndReturnsNullForUnknownRuntimeModel()
        {
            using(var f=new LibraryFixture())
            {
                Assert.DoesNotThrow(()=>Invoke(f.Library,"Validate"));
                Assert.NotNull(Invoke(f.Library,"FindModel","ring-player"));Assert.IsNull(Invoke(f.Library,"FindModel","foreign-drop"));
                foreach(string id in Fresh().externalEquipment.models)
                {
                    var expected=f.Village.FindModel(id);Assert.NotNull(expected,id+" shared equipment positive control");
                    Assert.AreSame(expected,Invoke(f.Library,"FindEquipmentModel",id));
                }
                Assert.IsNull(Invoke(f.Library,"FindEquipmentModel","character-teal"));
                Assert.IsNull(Invoke(f.Library,"FindEquipmentModel",new object[]{null}));
            }
        }
        [TestCase("catalog")] [TestCase("world")] [TestCase("water")] [TestCase("composite")]
        [TestCase("renderer")] [TestCase("equipment")] [TestCase("missing-binding")] [TestCase("duplicate-binding")]
        public void IncompleteLibraryFailsBeforeAnyWorldBinding(string mutation)
        {
            using(var f=new LibraryFixture())
            {
                Assert.DoesNotThrow(()=>Invoke(f.Library,"Validate"));
                switch(mutation)
                {
                    case "catalog":Set(f.Library,"Catalog",null);break;
                    case "world":Set(f.Library,"WorldMaterial",null);break;
                    case "water":Set(f.Library,"WaterMaterial",null);break;
                    case "composite":Set(f.Library,"CompositeMaterial",null);break;
                    case "renderer":Set(f.Library,"Renderer",null);break;
                    case "equipment":Set(f.Library,"EquipmentLibrary",null);break;
                    case "missing-binding":var a=(Array)Get(f.Library,"Models");Set(a.GetValue(0),"Prefab",null);break;
                    case "duplicate-binding":var b=(Array)Get(f.Library,"Models");b.SetValue(b.GetValue(0),1);break;
                }
                Invoke(f.Library,"InvalidateCaches");
                Assert.Throws<InvalidOperationException>(()=>Invoke(f.Library,"Validate"));
            }
        }
        [Test] public void ExplicitCacheInvalidationRejectsChangedCatalogAndCanRecover()
        {
            using(var f=new LibraryFixture())
            {
                Invoke(f.Library,"Validate");var original=Definition(f.Library);Assert.NotNull(original);
                Set(f.Library,"Catalog",f.OwnText("{}"));Invoke(f.Library,"InvalidateCaches");
                Assert.Throws<ArgumentException>(()=>Definition(f.Library));
                Set(f.Library,"Catalog",f.OwnText(Text()));Invoke(f.Library,"InvalidateCaches");
                Assert.DoesNotThrow(()=>Invoke(f.Library,"Validate"));Assert.AreNotSame(original,Definition(f.Library));
            }
        }
    }
}
