using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadSceneryIntegrationTests
    {
        private static SpawnRing3DPresenter Presenter(SpawnRing3DIntegrationFixture f)=>(SpawnRing3DPresenter)f.Presenter;
        private static Entity Add(SpawnRing3DIntegrationFixture f,string name,int x=20,int y=10)
        {
            if(name!="OldStump")return f.Add(name,x,y);
            var owner=new Entity{ID="SceneryProbeOldStump",BlueprintName="OldStump"};
            owner.AddPart(new RenderPart{RenderString="o",ColorString="&y"});owner.AddPart(new PhysicsPart{Solid=false});
            owner.AddPart(new QuestMarkerTriggerPart{Fact="bmo_stump_reached",Value=1});Assert.True(f.Zone.AddEntity(owner,x,y));return owner;
        }
        private static SpawnRing3DRecipe Native(SpawnRing3DIntegrationFixture f,Entity owner)=>Native(f.Zone,f.Library.Definition,owner);
        private static SpawnRing3DRecipe Native(Zone zone,SpawnRing3DCatalog catalog,Entity owner)
        {
            var method=typeof(SpawnRing3DRecipes).GetMethod("ResolveNative",BindingFlags.Static|BindingFlags.NonPublic);
            Assert.NotNull(method);return (SpawnRing3DRecipe)method.Invoke(null,new object[]{zone,owner,catalog,null});
        }
        private static SpawnRing3DRecipe Refine(Zone zone,Entity owner,SpawnRing3DRecipe prior)
        {
            var type=typeof(SpawnRing3DRecipes).Assembly.GetType("CavesOfOoo.Rendering.SpreadSceneryWorldRecipes");
            Assert.NotNull(type,"Missing narrow scenery refinement is intentional RED before integration.");
            var method=type.GetMethod("Refine",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);Assert.NotNull(method);
            try{return (SpawnRing3DRecipe)method.Invoke(null,new object[]{zone,owner,prior});}
            catch(TargetInvocationException error){throw error.InnerException??error;}
        }
        private static SpawnRing3DRecipe Exact(SpawnRing3DIntegrationFixture f,Entity owner)
        {
            var recipe=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);Assert.IsNull(recipe.Failure);
            Assert.That(recipe.ModelId,Does.StartWith("spread-scenery-"));Assert.AreSame(owner,recipe.Owner);Assert.True(recipe.Batched);Assert.False(recipe.Transient);
            Assert.True(Presenter(f).TryGetApprovedStyle(owner,out var proof),proof.Failure);Assert.True(proof.Batched);
            var entry=SpreadScenery3DLibrary.Load().Find(recipe.ModelId);Assert.NotNull(entry);Assert.AreSame(entry.Mesh,proof.ExpectedMesh);
            Assert.AreSame(SpreadScenery3DLibrary.Load().Material,proof.ExpectedMaterial);Assert.Greater(proof.SubmittedMesh.vertexCount,0);
            return recipe;
        }
        [TestCase("BerryBush")]
        [TestCase("Signpost")]
        [TestCase("HollowStump")]
        [TestCase("Beehive")]
        [TestCase("RiverShrine")]
        [TestCase("StoneFloor")]
        [TestCase("StoneWall")]
        [TestCase("Chair")]
        [TestCase("Bed")]
        [TestCase("Well")]
        [TestCase("Oven")]
        [TestCase("WatchLantern")]
        [TestCase("CampfireGroundMarker")]
        [TestCase("WellGroundMarker")]
        [TestCase("OvenGroundMarker")]
        [TestCase("LanternGroundMarker")]
        [TestCase("Shrine")]
        [TestCase("AlchemyShelf")]
        [TestCase("AlchemyStill")]
        [TestCase("TinkersForge")]
        [TestCase("PressurePlate")]
        [TestCase("BearTrap")]
        [TestCase("FireTrap")]
        [TestCase("SpikeTrap")]
        [TestCase("WeaponRack")]
        [TestCase("OldStump")]
        public void MissingNativeOwnerReceivesExactApprovedModelWithoutGameplayMutation(string name)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner=Add(f,name);var native=Native(f,owner);Assert.AreEqual("unmodeled-native-blueprint",native.Failure);
                var parts=owner.Parts.ToArray();string id=owner.ID,tiles=f.Zone.TileState.ToSaveString();int version=f.Zone.EntityVersion;
                var position=f.Zone.GetEntityPosition(owner);bool solid=owner.GetPart<PhysicsPart>().Solid;
                f.Refresh();var recipe=Exact(f,owner);f.Refresh();Assert.AreEqual(recipe.ModelId,Exact(f,owner).ModelId);
                Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(tiles,f.Zone.TileState.ToSaveString());Assert.AreEqual(id,owner.ID);
                Assert.AreEqual(position,f.Zone.GetEntityPosition(owner));Assert.AreEqual(solid,owner.GetPart<PhysicsPart>().Solid);CollectionAssert.AreEqual(parts,owner.Parts);
            }
        }
        [Test] public void AlreadyModeledNativeFlowerFieldPreservesItsSuccessfulRecipe()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner=Add(f,"FlowerField");var before=Native(f,owner);Assert.IsNull(before.Failure);Assert.NotNull(before.ModelId);
                Assert.That(before.ModelId,Does.StartWith("spread-flowers-"));
                var scenery=Refine(f.Zone,owner,before);Assert.AreEqual(before,scenery,"Missing-only scenery must preserve every field of this successful native recipe.");
                int version=f.Zone.EntityVersion;var parts=owner.Parts.ToArray();string tiles=f.Zone.TileState.ToSaveString();
                f.Refresh();var actual=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);
                Assert.AreSame(before.Owner,actual.Owner);Assert.AreEqual(before.ModelId.Replace("spread-flowers-","spread-environment-flowers-"),actual.ModelId);Assert.AreEqual(before.Failure,actual.Failure);
                Assert.AreEqual(before.Position,actual.Position);Assert.AreEqual(before.Batched,actual.Batched);Assert.AreEqual(before.Transient,actual.Transient);
                Assert.AreEqual(before.ComponentId,actual.ComponentId);Assert.AreEqual(before.QuarterTurns,actual.QuarterTurns);
                Assert.AreEqual(version,f.Zone.EntityVersion);CollectionAssert.AreEqual(parts,owner.Parts);Assert.AreEqual(tiles,f.Zone.TileState.ToSaveString());
                Assert.False(actual.ModelId.StartsWith("spread-scenery-",StringComparison.Ordinal),"Successful native FlowerField remains separately owned environment-style work.");
                var entry=SpreadEnvironment3DLibrary.Load().Find(actual.ModelId);Assert.NotNull(entry);
                Assert.True(Presenter(f).TryGetApprovedStyle(owner,out var proof),proof.Failure);
                Assert.AreSame(entry.Mesh,proof.ExpectedMesh);Assert.AreSame(SpreadEnvironment3DLibrary.Load().Material,proof.ExpectedMaterial);
                Assert.True(proof.Batched);Assert.Greater(proof.SubmittedMesh.vertexCount,0);
                Assert.AreEqual(before,Native(f,owner),"Scoped adoption must not rewrite the current native source selection.");
                owner.GetPart<RenderPart>().VisualID="unrelated-custom-owner";f.Refresh();
                Assert.AreEqual(before,Native(f,owner));Assert.AreEqual(before,SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition),"Custom identity preserves native source instead of acquiring the new flower form.");
                owner.GetPart<RenderPart>().VisualID=null;f.Refresh();Assert.AreEqual(actual,SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition));
            }
        }
        [TestCase("hidden")][TestCase("removed")][TestCase("same-id-clone")][TestCase("foreign-biome")][TestCase("foreign-render")]
        public void CurrentOwnerAuthorityLossCannotBorrowItsPriorModel(string change)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner=Add(f,"BerryBush");f.Refresh();Exact(f,owner);
                if(change=="hidden")owner.GetPart<RenderPart>().Visible=false;
                else if(change=="removed")Assert.True(f.Zone.RemoveEntity(owner));
                else if(change=="same-id-clone"){var clone=f.Factory.CreateEntity("BerryBush");clone.ID=owner.ID;owner=clone;}
                else if(change=="foreign-render")owner.GetPart<RenderPart>().ParentEntity=new Entity();
                else f.Manager.WorldMap.Tiles[12,10]=BiomeType.Beating;
                f.Refresh();Assert.False(Presenter(f).TryGetApprovedStyle(owner,out _));
                Assert.False(SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition).ModelId?.StartsWith("spread-scenery-",StringComparison.Ordinal)==true);
            }
        }
        [TestCase("existing-model")][TestCase("native-render-hidden")][TestCase("missing-native-quest-markers")]
        [TestCase("runtime-place-authority")][TestCase("reskinned-native-actor")]
        public void PriorSuccessOrNamedRefusalIsReturnedUnchangedInAnActiveSpreadZone(string status)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner=Add(f,"WatchLantern");Assert.True(SpreadPresentationScope.IsActive(f.Zone));
                var prior=new SpawnRing3DRecipe(owner,status=="existing-model"?"wellmeet-lantern-2":null,"authored-component",new Vector3(7,0,8),false,true,status=="existing-model"?null:status,3);
                var actual=Refine(f.Zone,owner,prior);
                Assert.AreSame(prior.Owner,actual.Owner);Assert.AreEqual(prior.ModelId,actual.ModelId);Assert.AreEqual(prior.Failure,actual.Failure);
                Assert.AreEqual(prior.ComponentId,actual.ComponentId);Assert.AreEqual(prior.Position,actual.Position);
                Assert.AreEqual(prior.Batched,actual.Batched);Assert.AreEqual(prior.Transient,actual.Transient);Assert.AreEqual(prior.QuarterTurns,actual.QuarterTurns);
            }
        }
        [Test] public void LanternSwitchAndSavedDisabledStateRetainTheActualNativeOwner()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner=Add(f,"WatchLantern");string id=owner.ID;var light=owner.GetPart<LightSourcePart>();Assert.True(light.Enabled);
                f.Refresh();string lit=Exact(f,owner).ModelId;Assert.False(lit.Contains("-unlit-"));
                light.Enabled=false;f.Refresh();string off=Exact(f,owner).ModelId;Assert.That(off,Does.Contain("-unlit-"));Assert.AreNotEqual(lit,off);
                f.BindLoaded(f.RoundTrip());owner=f.Zone.GetReadOnlyEntities().Single(e=>e.ID==id);
                Assert.False(owner.GetPart<LightSourcePart>().Enabled);Assert.AreEqual(off,Exact(f,owner).ModelId);
                owner.GetPart<LightSourcePart>().Enabled=true;f.Refresh();Assert.AreEqual(lit,Exact(f,owner).ModelId);
            }
        }
        [Test] public void RealNativeHarvestConsumesSourceAndRelinquishesItsExactView()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner=Add(f,"BerryBush");f.Approach(owner);f.Refresh();Exact(f,owner);
                var e=GameEvent.New("InventoryAction");try{e.SetParameter("Actor",f.Player);e.SetParameter("Zone",f.Zone);e.SetParameter("Command","Harvest");owner.FireEvent(e);Assert.True(e.Handled);}finally{e.Release();}
                Assert.True(owner.GetPart<HarvestablePart>().Harvested);Assert.Null(f.Zone.GetEntityCell(owner));
                Assert.True(f.Player.GetPart<InventoryPart>().Objects.Any(item=>item.BlueprintName=="WildBerries"));
                f.Refresh();Assert.False(Presenter(f).TryGetApprovedStyle(owner,out _));Assert.False(f.Authored(owner));
            }
        }
        [Test] public void RealNativeStepConsumesTrapAndRelinquishesItsExactView()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                foreach(var existing in f.Zone.GetReadOnlyEntities().ToArray())if(existing!=f.Player)f.Zone.RemoveEntity(existing);
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)f.Zone.TileState.Clear(x,y);
                Assert.True(f.Zone.MoveEntity(f.Player,19,10));var trap=Add(f,"SpikeTrap");int hp=f.Player.GetStatValue("Hitpoints");
                f.Refresh();Exact(f,trap);Assert.True(MovementSystem.TryMove(f.Player,f.Zone,1,0));
                Assert.Null(f.Zone.GetEntityCell(trap));Assert.Less(f.Player.GetStatValue("Hitpoints"),hp);
                f.Refresh();Assert.False(Presenter(f).TryGetApprovedStyle(trap,out _));Assert.False(f.Authored(trap));
            }
        }
        [TestCase("StoneFloor",true)][TestCase("CampfireGroundMarker",false)]
        public void OnlyRealFloorSuppressesOneCellsFallbackGround(string name,bool floor)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                foreach(var existing in f.Zone.GetReadOnlyEntities().ToArray())if(existing!=f.Player)f.Zone.RemoveEntity(existing);
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)f.Zone.TileState.Clear(x,y);
                f.Refresh();int before=PatchVertices(f);Assert.Greater(before,0);Assert.Zero(before%50);
                var owner=Add(f,name);f.Refresh();var recipe=Exact(f,owner);var entry=SpreadScenery3DLibrary.Load().Find(recipe.ModelId);
                Assert.AreEqual(floor?"ground":"entity",entry.Spec.kind);
                Assert.AreEqual(before+(floor?-before/50:0)+entry.Mesh.vertexCount,PatchVertices(f));
            }
        }
        private static int PatchVertices(SpawnRing3DIntegrationFixture f)
        {
            var ground=typeof(SpawnRing3DPresenter).GetField("ground",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(f.Presenter);
            var root=(GameObject)ground.GetType().GetMethod("RootFor").Invoke(ground,new object[]{20,10});
            return root.GetComponentsInChildren<MeshFilter>(true).Where(filter=>filter.gameObject.activeInHierarchy&&filter.GetComponent<Renderer>()?.enabled==true)
                .Sum(filter=>filter.sharedMesh.vertexCount);
        }
        [TestCase("visual-id")][TestCase("visual-variant")][TestCase("glyph")][TestCase("portable")]
        public void LiveCustomAppearanceOrPortableMutationRefusesSceneryClaim(string change)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner=Add(f,"BerryBush");f.Refresh();Exact(f,owner);
                if(change=="visual-id")owner.GetPart<RenderPart>().VisualID="authored-custom-owner";
                else if(change=="visual-variant")owner.GetPart<RenderPart>().VisualVariant="winter";
                else if(change=="glyph")owner.GetPart<RenderPart>().RenderString="?";
                else owner.GetPart<PhysicsPart>().Takeable=true;
                f.Refresh();Assert.False(Presenter(f).TryGetApprovedStyle(owner,out _));
                Assert.False(SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition).ModelId?.StartsWith("spread-scenery-",StringComparison.Ordinal)==true);
            }
        }
        [TestCase(false)][TestCase(true)]
        public void UntouchedGeneratedWildernessAndVillageOwnersUseTheSameExactContract(bool village)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                if(village)
                {
                    Zone selected=null;var inspected=new System.Collections.Generic.List<string>();
                    for(int y=0;y<WorldMap.Height&&selected==null;y++)for(int x=0;x<WorldMap.Width&&selected==null;x++)
                    {
                        if(f.Manager.WorldMap.GetBiome(x,y)!=BiomeType.Spread||f.Manager.WorldMap.GetPOI(x,y)?.Type!=POIType.Village)continue;
                        string id=WorldMap.ToZoneID(x,y,0);var candidate=f.Manager.GetZone(id);
                        int count=candidate?.GetReadOnlyEntities().Count(owner=>SpreadSceneryRecipes.TryModel(owner,0,out _)
                            &&Native(candidate,f.Library.Definition,owner).Failure=="unmodeled-native-blueprint")??0;
                        inspected.Add(id+":"+count);if(count>0)selected=candidate;
                    }
                    TestContext.WriteLine("Inspected actual Spread villages: "+string.Join(",",inspected));
                    Assert.NotNull(selected,"Actual current-map Spread village with native-missing scenery required: "+string.Join(",",inspected));
                    f.Zone.RemoveEntity(f.Player);f.Zone=selected;var at=f.FreeCell();Assert.True(f.Zone.AddEntity(f.Player,at.x,at.y));
                    f.Manager.SetActiveZone(selected);f.Reveal();f.Bind(selected);
                }
                var candidates=f.Zone.GetReadOnlyEntities().Where(owner=>SpreadSceneryRecipes.TryModel(owner,0,out _)
                    &&Native(f,owner).Failure=="unmodeled-native-blueprint").ToArray();
                Assert.Greater(candidates.Length,0,"No factory-created scenery substitutes enter this generated-source test.");
                var owners=f.Zone.GetReadOnlyEntities().ToArray();string tiles=f.Zone.TileState.ToSaveString();int version=f.Zone.EntityVersion;
                f.Refresh();foreach(var owner in candidates)Exact(f,owner);
                CollectionAssert.AreEqual(owners,f.Zone.GetReadOnlyEntities());Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(tiles,f.Zone.TileState.ToSaveString());
            }
        }
        [Test] public void ActualCommittedSpreadLairFloorUsesSameCurrentOwnerContract()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                LairStackRecord plan=null;
                for(int y=0;y<WorldMap.Height&&plan==null;y++)for(int x=0;x<WorldMap.Width&&plan==null;x++)
                {
                    if(f.Manager.WorldMap.GetBiome(x,y)!=BiomeType.Spread||f.Manager.WorldMap.GetPOI(x,y)?.Type!=POIType.Lair)continue;
                    string id=WorldMap.ToZoneID(x,y,0);Assert.NotNull(f.Manager.GetZone(id));var candidate=LairStacks.Inspect(f.Manager,id);
                    if(candidate!=null&&!candidate.Legacy)plan=candidate;
                }
                Assert.NotNull(plan,"Real current-map lair source required.");var floor=f.Manager.GetZone(plan.ZoneAt(1));Assert.NotNull(floor);
                Assert.True(SpreadPresentationScope.IsActive(floor),"Actual committed lair binding must be active.");
                var up=floor.GetReadOnlyEntities().Single(e=>e.HasPart<StairsUpPart>());var at=floor.GetEntityCell(up);
                Assert.True(f.Zone.TryTransferEntityTo(f.Player,floor,at.X,at.Y));f.Zone=floor;f.Manager.SetActiveZone(floor);f.Reveal();f.Bind(floor);
                var place=f.FreeCell();var owner=Add(f,"BerryBush",place.x,place.y);f.Refresh();Exact(f,owner);
            }
        }
    }
}
