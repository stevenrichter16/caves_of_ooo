using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class SpawnRing3DRecipeTests
    {
        EntityEquipmentContentFixture scope;
        SpawnRing3DCatalog catalog;
        MultiCellPilot3DCatalog pilot;
        Type recipes;
        static readonly string[] Ring={"Overworld.2.5.0","Overworld.3.5.0","Overworld.4.5.0","Overworld.2.6.0","Overworld.4.6.0","Overworld.2.7.0","Overworld.3.7.0","Overworld.4.7.0"};
        [SetUp] public void Setup()
        {
            recipes=typeof(Village3DPresenter).Assembly.GetType("CavesOfOoo.Rendering.SpawnRing3DRecipes");
            Assert.NotNull(recipes,"Record recipe missing-type RED before implementation.");
            scope=new EntityEquipmentContentFixture();
            var pilotLibrary=Resources.Load<MultiCellPilot3DLibrary>(MultiCellPilot3DLibrary.ResourcePath);
            Assert.NotNull(pilotLibrary);pilotLibrary.Validate();pilot=pilotLibrary.Definition;
            catalog=SpawnRing3DCatalog.Parse(File.ReadAllText(Path.Combine(Application.dataPath,
                "Tests/EditMode/Presentation/Rendering/Fixtures/spawn-ring-catalog-unit-fixture.json")));
        }
        [TearDown] public void Teardown()=>scope?.Dispose();
        object Call(string method,params object[] args)
        {
            var m=recipes.GetMethod(method,BindingFlags.Public|BindingFlags.Static);Assert.NotNull(m,method);
            try{return m.Invoke(null,args);}catch(TargetInvocationException e){throw e.InnerException??e;}
        }
        object Resolve(Zone z,Entity e)=>Call("Resolve",z,e,catalog,pilot);
        static T Value<T>(object recipe,string field)
        {var f=recipe.GetType().GetField(field);Assert.NotNull(f,field);return (T)f.GetValue(recipe);}
        static bool Supported(object r)=>Value<string>(r,"ModelId")!=null;
        static void Refused(object r,string reason=null)
        {Assert.IsFalse(Supported(r));Assert.IsNotEmpty(Value<string>(r,"Failure"));if(reason!=null)Assert.AreEqual(reason,Value<string>(r,"Failure"));}
        static IEnumerable<TestCaseData> Worlds()
        {foreach(int seed in new[]{64,1729,729490642})foreach(string id in Ring)yield return new TestCaseData(seed,id);}
        [TestCaseSource(nameof(Worlds))] public void CurrentNativeGroundGraphResolvesWithoutMutation(int seed,string id)
        {
            var manager=new OverworldZoneManager(scope.Factory,seed);var zone=manager.GetZone(id);
            var members=zone.GetReadOnlyEntities().ToArray();var positions=members.Select(zone.GetEntityPosition).ToArray();
            var ids=members.Select(e=>e.ID).ToArray();int version=zone.EntityVersion;string tiles=zone.TileState.ToSaveString();
            foreach(var entity in members)
            {
                var recipe=Resolve(zone,entity);var render=entity.GetPart<RenderPart>();
                if(render==null||!render.Visible) {Refused(recipe);continue;}
                Assert.IsTrue(Supported(recipe),id+" "+entity.BlueprintName+": "+Value<string>(recipe,"Failure"));
                Assert.AreSame(entity,Value<Entity>(recipe,"Owner"));
                var at=zone.GetEntityPosition(entity);
                Assert.AreEqual(Village3DProjection.CellCentre(at.x,at.y),Value<Vector3>(recipe,"Position"));
                if(entity.HasPart<MultiCellPilotPropPart>())Assert.NotNull(pilot.FindModel(Value<string>(recipe,"ModelId")));
                else Assert.NotNull(catalog.FindModel(Value<string>(recipe,"ModelId")));
                Assert.AreEqual(Value<string>(recipe,"ModelId"),Value<string>(Resolve(zone,entity),"ModelId"));
            }
            CollectionAssert.AreEquivalent(members,zone.GetReadOnlyEntities());CollectionAssert.AreEqual(positions,members.Select(zone.GetEntityPosition));
            CollectionAssert.AreEqual(ids,members.Select(e=>e.ID));Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(tiles,zone.TileState.ToSaveString());
        }
        [Test] public void VariantsFollowCurrentCoordinatesAndRemainStableAcrossFreshEquivalentGraphs()
        {
            var a=new Zone(Ring[0]);var b=new Zone(Ring[0]);var seen=new HashSet<string>();
            for(int x=0;x<32;x++)
            {
                var e=scope.Factory.CreateEntity("Rock");e.ID=null;Assert.IsTrue(a.AddEntity(e,x,12));
                var f=scope.Factory.CreateEntity("Rock");f.ID=null;Assert.IsTrue(b.AddEntity(f,x,12));
                string model=Value<string>(Resolve(a,e),"ModelId");seen.Add(model);
                Assert.AreEqual(model,Value<string>(Resolve(b,f),"ModelId"));
                Assert.AreEqual(Village3DProjection.CellCentre(x,12),Value<Vector3>(Resolve(a,e),"Position"));
            }
            Assert.GreaterOrEqual(seen.Count,3,"Repeated native rocks need actual variant selection, not always choice zero.");
        }
        [Test] public void PlayerAndConditionalSpeciesResolveWithoutNeedingTheReferenceSpawn()
        {
            var zone=new Zone(Ring[0]);
            foreach(string name in new[]{"Player","SariSnake","SkySari","Snapjaw","SnapjawWarlord","ChoirIronVein"})
            {
                var entity=scope.Factory.CreateEntity(name);Assert.NotNull(entity,name);zone.AddEntity(entity,15,12);
                var recipe=Resolve(zone,entity);Assert.IsTrue(Supported(recipe),name);
                Assert.AreEqual(name!="ChoirIronVein",Value<bool>(recipe,"Transient"));zone.RemoveEntity(entity);
            }
        }
        [TestCase("unknown")] [TestCase("detached")] [TestCase("wrong-zone")] [TestCase("invisible")] [TestCase("missing-render")]
        public void UnrepresentedNativeEntityHasAnExplicitFallback(string kind)
        {
            var zone=new Zone(Ring[0]);var entity=scope.Factory.CreateEntity("Rock");zone.AddEntity(entity,12,7);
            Assert.IsTrue(Supported(Resolve(zone,entity)));
            if(kind=="unknown")entity.BlueprintName="UnmodeledNativeObject";
            if(kind=="detached")zone.RemoveEntity(entity);
            if(kind=="invisible")entity.GetPart<RenderPart>().Visible=false;
            if(kind=="missing-render")entity.RemovePart(entity.GetPart<RenderPart>());
            Refused(Resolve(kind=="wrong-zone"?new Zone(Ring[0]):zone,entity));
        }
        [TestCase("Overworld.3.6.0")] [TestCase("Overworld.4.6.3")] [TestCase("Overworld.18.18.1")] [TestCase("WorldMap")]
        public void UnrelatedZonesAreNeverClaimedByRingRecipes(string id)
        {var z=new Zone(id);var e=scope.Factory.CreateEntity("Rock");z.AddEntity(e,1,1);Refused(Resolve(z,e));}
        [Test] public void FellingNeedsItsRealSceneContractAndExactCurrentOwner()
        {
            var m=new OverworldZoneManager(scope.Factory,64);var z=m.GetZone(FellingSiteBuilder.ZoneID);
            var layer=FellingSceneDefinition.Load().layers.First(l=>l.mutable);var e=FellingSceneRuntime.FindOwner(z,layer.id);
            var recipe=Resolve(z,e);Assert.IsTrue(Supported(recipe));Assert.IsFalse(Value<bool>(recipe,"Batched"));
            Assert.AreEqual(layer.id,Value<string>(recipe,"ComponentId"));
            var spoof=new Entity{ID=e.ID,BlueprintName=e.BlueprintName};spoof.AddPart(new RenderPart());
            spoof.AddPart(new FellingScenePropPart{ComponentId=layer.id,Mutable=layer.mutable});z.AddEntity(spoof,layer.anchorX,layer.anchorY);
            Refused(Resolve(z,spoof));Assert.IsTrue(Supported(Resolve(z,e)));
            z.RemoveEntity(e);Refused(Resolve(z,e));Refused(Resolve(z,spoof));
            var incomplete=new Zone(FellingSiteBuilder.ZoneID);var stone=scope.Factory.CreateEntity("TepuiStone");incomplete.AddEntity(stone,1,1);
            Refused(Resolve(incomplete,stone));
        }
        [Test] public void NativeMovementAndMembershipSetRecipePositionWithoutInventedEntityIds()
        {
            var z=new Zone(Ring[0]);var e=scope.Factory.CreateEntity("GlasspaneFrog");e.ID=null;z.AddEntity(e,12,7);
            var a=Resolve(z,e);Assert.IsTrue(Value<bool>(a,"Transient"));Assert.IsFalse(Value<bool>(a,"Batched"));
            Assert.IsTrue(z.MoveEntity(e,13,8));var b=Resolve(z,e);
            Assert.AreEqual(Village3DProjection.CellCentre(13,8),Value<Vector3>(b,"Position"));Assert.IsNull(e.ID);
            z.RemoveEntity(e);Refused(Resolve(z,e));
        }
        [Test] public void TakeableNativePropsStayIndependentWhileRepeatedSceneryCanBatch()
        {
            var z=new Zone(Ring[0]);var rock=scope.Factory.CreateEntity("Rock");var bone=scope.Factory.CreateEntity("Tepuibone");
            z.AddEntity(rock,12,7);z.AddEntity(bone,13,7);
            Assert.IsTrue(Value<bool>(Resolve(z,rock),"Batched"));Assert.IsTrue(bone.GetPart<PhysicsPart>().Takeable);
            Assert.IsFalse(Value<bool>(Resolve(z,bone),"Batched"));
        }
        [Test] public void FellingSacredScarUsesCleanGroundVariantAndLeavesPeripheralVariation()
        {
            var z=new OverworldZoneManager(scope.Factory,64).GetZone(FellingSiteBuilder.ZoneID);
            string clean=catalog.FindBlueprint("TepuiStone").models[0];var peripheral=new HashSet<string>();
            foreach(var e in z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="TepuiStone"))
            {
                var at=z.GetEntityPosition(e);string id=Value<string>(Resolve(z,e),"ModelId");
                if(at.x>=31&&at.x<=48&&at.y>=6&&at.y<=18)Assert.AreEqual(clean,id,"Felling scar must stay free of decorative grass/flowers.");
                else peripheral.Add(id);
            }
            Assert.GreaterOrEqual(peripheral.Count,3,"The clean scar rule must not remove outer stone variation.");
        }
        [Test] public void ReskinnedActorsKeepTheirNativeIdentityInsteadOfBorrowingASpeciesModel()
        {
            var z=new Zone(Ring[0]);
            foreach(var binding in catalog.blueprints.Where(b=>b.role=="actor"))
            {
                var e=scope.Factory.CreateEntity(binding.blueprint);Assert.NotNull(e,binding.blueprint);z.AddEntity(e,12,7);
                var render=e.GetPart<RenderPart>();string original=render.RenderString;
                Assert.IsTrue(Supported(Resolve(z,e)),binding.blueprint+" canonical control");
                render.RenderString="?";Refused(Resolve(z,e),"reskinned-native-actor");
                render.RenderString=original;Assert.IsTrue(Supported(Resolve(z,e)),binding.blueprint+" recovery");z.RemoveEntity(e);
            }
        }
        [Test] public void TakeableItemUsesVisibleOnlyFogWhileSceneryCanRemainRemembered()
        {
            var z=new Zone(Ring[0]);var e=scope.Factory.CreateEntity("Tepuibone");z.AddEntity(e,12,7);
            Assert.IsTrue(e.GetPart<PhysicsPart>().Takeable);
            Assert.IsTrue(Value<bool>(Resolve(z,e),"Transient"),"Takeable loot must not be exposed in remembered cells.");
            e.GetPart<PhysicsPart>().Takeable=false;
            Assert.IsFalse(Value<bool>(Resolve(z,e),"Transient"));
        }
        [Test] public void PermanentFenWaterUsesCurrentTileStateAndSavedErasureNotPoolPresence()
        {
            var z=SpawnRing3DIntegrationFixture.CreateLegacyFen(scope.Factory);
            var keys=new List<int>();z.TileState.CollectWrittenKeys(keys);
            int key=keys.First(k=>z.TileState.Get(k%80,k/80).Coatings.Any(l=>l.Id=="water"&&l.Turns==ZoneTileState.Permanent)
                &&!z.GetCell(k%80,k/80).Objects.Any(e=>e.HasPart<LiquidPoolPart>()));int x=key%80,y=key/80;
            Assert.IsTrue((bool)Call("HasPermanentWater",z,x,y));z.TileState.RemoveCoating(x,y,"water");
            Assert.IsFalse((bool)Call("HasPermanentWater",z,x,y));z.TileState.WriteCoating(x,y,"water",7);
            Assert.IsFalse((bool)Call("HasPermanentWater",z,x,y));z.TileState.RemoveCoating(x,y,"water");
            z.TileState.WriteCoating(x,y,"water",ZoneTileState.Permanent);Assert.IsTrue((bool)Call("HasPermanentWater",z,x,y));
        }
        [Test] public void SprayPoolPresentationReadsItsNativeWaterWithoutChangingTheProjection()
        {
            var m=new OverworldZoneManager(scope.Factory,729490642);var z=m.GetZone(Ring[0]);
            var e=z.GetReadOnlyEntities().First(a=>a.BlueprintName=="SprayPool");var p=z.GetEntityPosition(e);string before=z.TileState.ToSaveString();
            var pool=e.GetPart<LiquidPoolPart>();Assert.NotNull(pool);Assert.AreEqual("water",pool.LiquidId);Assert.AreEqual(40,pool.Volume);
            Assert.IsNull(e.GetPart<TileStateSourcePart>(),"Native pool ownership supplies the coating; rendering must not invent a second source.");
            Assert.IsTrue(Supported(Resolve(z,e)));
            Assert.IsTrue((bool)Call("HasPermanentWater",z,p.x,p.y));Assert.AreEqual(before,z.TileState.ToSaveString());
        }
    }
}
