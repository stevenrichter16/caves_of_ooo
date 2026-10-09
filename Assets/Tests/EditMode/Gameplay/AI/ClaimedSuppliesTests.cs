// Native source, geometry and transaction contracts for claimed supplies.
// Proposed narrow contracts are discovered by reflection so the initial missing
// implementation is assertion RED, not a production stub or compile failure.
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    internal static class ClaimedSupplyContract
    {
        internal const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        internal static void Version(OverworldZoneManager manager,int version)
            => typeof(SpreadExplorationPlan).GetField("<Version>k__BackingField",All).SetValue(manager.Exploration,version);
        internal static string Place(Zone zone,Entity actor,SpreadGenerationReceipt people,SpreadGenerationReceipt stock,
            Func<bool> authority,Func<Entity[],Func<bool>,bool> commit)
        {
            var method=typeof(SpreadExplorationActorPlacement).GetMethod("TryClaimedSupplies",All,null,
                new[]{typeof(Zone),typeof(Entity),typeof(SpreadGenerationReceipt),typeof(SpreadGenerationReceipt),
                    typeof(Func<bool>),typeof(Func<Entity[],Func<bool>,bool>)},null);
            Assert.NotNull(method,"RED: missing stationary-cache source compositor.");
            try{return method.Invoke(null,new object[]{zone,actor,people,stock,authority,commit}).ToString();}
            catch(TargetInvocationException error){throw error.InnerException??error;}
        }
        internal static Func<bool> Graph(Zone zone,IEnumerable<Entity> owners) => (Func<bool>)typeof(SpreadGenerationReceipt)
            .GetMethod("CaptureFinalState",All).Invoke(null,new object[]{zone,owners});
    }

    public sealed class ClaimedSuppliesManifestTests
    {
        [TestCase(1,"Overworld.11.2.0|Overworld.17.12.0|Overworld.7.15.0|Overworld.7.6.0|Overworld.8.5.0")]
        [TestCase(64,"Overworld.11.2.0|Overworld.12.5.0|Overworld.4.14.0|Overworld.7.12.0|Overworld.7.6.0")]
        [TestCase(1729,"Overworld.10.7.0|Overworld.11.5.0|Overworld.16.12.0|Overworld.4.13.0|Overworld.8.10.0|Overworld.9.0.0")]
        public void FreshVersionOptsInWithoutAddingOrReallocatingAnyPrimaryBank(int seed,string frozenBanks)
        {
            using(var scope=new HaulingContentScope())
            {
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);
                Assert.AreEqual(16,manager.Exploration.Version,"RED: explicit new-world opt-in, with saved15 kept literal.");
                var banks=manager.Exploration.Entries.Where(e=>e.PlacementEligible&&e.Family==SpreadExplorationFamily.OccupiedBank)
                    .Select(e=>e.ZoneID).OrderBy(id=>id,StringComparer.Ordinal).ToArray();
                CollectionAssert.AreEqual(frozenBanks.Split('|'),banks,"Native v15 cohort frozen before the source probe: no additional allocation or second rank.");
                Assert.Zero(manager.CachedZoneCount);Assert.Zero(manager.Exploration.RetainedGraphCount);
                Assert.False(banks.Contains(ReferenceGladePlan.ZoneID));Assert.False(banks.Contains("Overworld.12.10.0"));
                var saved=SpreadExplorationPlan.BindForSave(manager,null).GetProperty(SpreadExplorationPlan.PropertyKey);
                Assert.AreEqual(saved,SpreadExplorationPlan.BindForSave(manager,null).GetProperty(SpreadExplorationPlan.PropertyKey));
            }
        }
        [Test] public void LiteralVersionFifteenStillRestoresWithoutRewritingItsManifest()
        {
            using(var scope=new HaulingContentScope())
            {
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,1,true);ClaimedSupplyContract.Version(manager,15);
                var world=SpreadExplorationPlan.BindForSave(manager,null);string saved=world.GetProperty(SpreadExplorationPlan.PropertyKey);
                var restore=typeof(SpreadExplorationPlan).GetMethod("Restore",ClaimedSupplyContract.All);
                var loaded=(SpreadExplorationPlan)restore.Invoke(null,new object[]{manager,world});
                typeof(OverworldZoneManager).GetProperty("Exploration",ClaimedSupplyContract.All).SetValue(manager,loaded);
                Assert.AreEqual(15,loaded.Version);Assert.AreEqual(saved,SpreadExplorationPlan.BindForSave(manager,null).GetProperty(SpreadExplorationPlan.PropertyKey));
                Assert.Zero(manager.CachedZoneCount);
            }
        }
        [TestCase(false)][TestCase(true)]
        public void FixedNativeSourceWitnessGainsTheCacheOnlyInAFreshVersion(bool saved15)
        {
            using(var scope=new HaulingContentScope())
            {
                const int seed=1;const string id="Overworld.17.12.0";
                scope.Seed(unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue)));
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);
                if(saved15)
                {
                    ClaimedSupplyContract.Version(manager,15);var world=SpreadExplorationPlan.BindForSave(manager,null);
                    var loaded=(SpreadExplorationPlan)typeof(SpreadExplorationPlan).GetMethod("Restore",ClaimedSupplyContract.All).Invoke(null,new object[]{manager,world});
                    typeof(OverworldZoneManager).GetProperty("Exploration",ClaimedSupplyContract.All).SetValue(manager,loaded);
                }
                var zone=manager.GetZone(id);Assert.NotNull(zone);
                var defenders=zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="MarlbackScrabbler"&&e.GetPart<SpreadTerritoryPart>()?.Post?.HasPart<ContainerPart>()==true).ToArray();
                if(saved15){Assert.IsEmpty(defenders,"Even cold, previously ungenerated v15 addresses retain the earlier territorial behavior.");return;}
                Assert.AreEqual(1,defenders.Length,"Known unmodified seed1/17.12 source: the actual existing Sack must become the post.");
                var post=defenders[0].GetPart<SpreadTerritoryPart>().Post;
                Assert.AreEqual("Sack",post.BlueprintName);Assert.AreEqual((74,19),zone.GetEntityPosition(post));Assert.IsNotEmpty(post.GetPart<ContainerPart>().Contents);
                var item=post.GetPart<ContainerPart>().Contents[0];Assert.True(post.GetPart<ContainerPart>().RemoveItem(item));
                Assert.AreSame(zone,manager.GetZone(id));Assert.False(post.GetPart<ContainerPart>().Contents.Contains(item),"Revisit cannot refill source stock.");
            }
        }
    }

    public sealed class ClaimedSuppliesPlacementTests
    {
        // These are controlled geometry/transaction fixtures, NOT ordinary-world
        // source evidence. Both receipts nevertheless come from their real producers.
        sealed class FirstRandom:Random
        {
            public override int Next(int maxValue)=>0;
            public override int Next(int minValue,int maxValue)=>minValue;
            public override double NextDouble()=>0;
        }
        sealed class Fixture:IDisposable
        {
            readonly HaulingContentScope scope=new HaulingContentScope();
            internal EntityFactory Factory=>scope.Factory;
            internal readonly Zone Zone=new Zone("Overworld.12.4.0");
            internal readonly PopulationBuilder Population;internal readonly ContainerBuilder Containers;
            internal Entity Actor,Cache;internal Entity[] Packet;internal Func<bool> Final;internal int Commits;
            internal readonly (int x,int y) OriginalActor;
            internal Fixture(int actorCount=1,int cacheX=12,int cacheY=10,int actorX=10,int actorY=10)
            {
                scope.Seed(64);
                // One controlled spawn cell (or two) gives a deterministic ordinary
                // producer receipt without creating or moving a receipt owner by hand.
                for(int y=0;y<CavesOfOoo.Core.Zone.Height;y++)for(int x=0;x<CavesOfOoo.Core.Zone.Width;x++)
                {
                    var ground=scope.Factory.CreateEntity("Grass");Assert.True(Zone.AddEntity(ground,x,y));
                    if(!(x==actorX&&y==actorY)&&!(actorCount==2&&x==actorX&&y==actorY+1))
                        Assert.True(Zone.AddEntity(scope.Factory.CreateEntity("StoneWall"),x,y));
                }
                Population=new PopulationBuilder(new PopulationTable{Name="SpreadTier1",Entries=new List<PopulationEntry>{
                    new PopulationEntry{BlueprintName="MarlbackScrabbler",EncounterGroup="SpreadTier1Encounter",MinCount=actorCount,MaxCount=actorCount}}}){CaptureSourceReceipts=true};
                Assert.True(Population.BuildZone(Zone,scope.Factory,new FirstRandom()));
                Assert.True(Population.SourceReceipt.IsCurrent);Actor=Population.SourceReceipt.Owners[0];OriginalActor=Zone.GetEntityPosition(Actor);
                foreach(var wall in Zone.GetCell(cacheX,cacheY).Objects.Where(e=>e.BlueprintName=="StoneWall").ToArray())Zone.RemoveEntity(wall);
                Containers=new ContainerBuilder(BiomeType.Spread,1,ContainerPlacementService.ZoneKind.Wilderness){CaptureSourceReceipts=true};
                Assert.True(Containers.BuildZone(Zone,scope.Factory,new FirstRandom()));
                Assert.True(Containers.SourceReceipt.IsCurrent);Assert.AreEqual(1,Containers.SourceReceipt.Owners.Count);
                Cache=Containers.SourceReceipt.Owners.Single();Assert.AreEqual("Crate",Cache.BlueprintName);
                Assert.IsNotEmpty(Cache.GetPart<ContainerPart>().Contents);Assert.AreEqual((cacheX,cacheY),Zone.GetEntityPosition(Cache));
                foreach(var wall in Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="StoneWall").ToArray())Zone.RemoveEntity(wall);
                Assert.True(Population.SourceReceipt.IsCurrent);Assert.True(Containers.SourceReceipt.IsCurrent);
            }
            internal string Place(Func<bool> authority=null,Func<Entity[],Func<bool>,bool> commit=null)
                => ClaimedSupplyContract.Place(Zone,Actor,Population.SourceReceipt,Containers.SourceReceipt,authority??(()=>true),commit??Commit);
            bool Commit(Entity[] packet,Func<bool> proof)
            {Commits++;Packet=packet;Final=proof;return proof!=null&&proof();}
            internal Func<bool> AllProof()=>ClaimedSupplyContract.Graph(Zone,Zone.GetReadOnlyEntities().ToArray());
            internal void Wall(int x,int y)=>Assert.True(Zone.AddEntity(scope.Factory.CreateEntity("StoneWall"),x,y));
            public void Dispose()=>scope.Dispose();
        }

        [Test] public void OneOriginalDefenderGuardsTheUnmovedCacheAndItsOriginalCompleteStock()
        {
            using(var f=new Fixture())
            {
                var roots=f.Zone.GetReadOnlyEntities().ToArray();var cacheProof=ClaimedSupplyContract.Graph(f.Zone,new[]{f.Cache});
                Assert.AreEqual("Committed",f.Place());Assert.AreEqual(1,f.Commits);Assert.True(f.Final());Assert.True(cacheProof());
                CollectionAssert.AreEquivalent(roots,f.Zone.GetReadOnlyEntities());CollectionAssert.AreEquivalent(new[]{f.Actor,f.Cache},f.Packet);
                var role=f.Actor.GetPart<SpreadTerritoryPart>();Assert.NotNull(role);Assert.AreSame(f.Cache,role.Post);Assert.True(role.Configured);
                Assert.AreEqual(2,role.GraceTurns);Assert.Null(role.WarningTarget);Assert.Zero(role.GraceRemaining);
                var at=f.Zone.GetEntityPosition(f.Cache);Assert.AreEqual(at.x-3,role.Left);Assert.AreEqual(at.x+3,role.Right);
                Assert.AreEqual(at.y-2,role.Top);Assert.AreEqual(at.y+2,role.Bottom);
                foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
                {Assert.Greater(at.x+d.Item1,role.Left);Assert.Less(at.x+d.Item1,role.Right);Assert.Greater(at.y+d.Item2,role.Top);Assert.Less(at.y+d.Item2,role.Bottom);}
            }
        }
        [Test] public void PureAuthorityObserverDoesNotPreventAValidCommit()
        {using(var f=new Fixture()){int calls=0;Assert.AreEqual("Committed",f.Place(()=>{calls++;return true;}));Assert.Greater(calls,1);Assert.True(f.Final());}}
        [Test] public void TwoSourceActorsDoNotGetCollapsedOrPromotedIntoThisSingleDefenderSite()
        {using(var f=new Fixture(2)){var proof=f.AllProof();Assert.AreEqual("Unavailable",f.Place());Assert.True(proof());Assert.Zero(f.Commits);Assert.True(f.Population.SourceReceipt.IsCurrent);}}
        [TestCase("locked")][TestCase("empty")][TestCase("foreign-owner")][TestCase("replaced-container-part")]
        public void AChangedOrUnsuitableActualCacheDoesNotCreateAPartialDuty(string fault)
        {
            using(var f=new Fixture())
            {
                var box=f.Cache.GetPart<ContainerPart>();
                if(fault=="locked")box.Locked=true;
                if(fault=="empty")foreach(var item in box.Contents.ToArray())box.RemoveItem(item);
                if(fault=="foreign-owner")f.Cache.GetPart<PhysicsPart>().InInventory=new Entity();
                if(fault=="replaced-container-part"){f.Cache.RemovePart(box);f.Cache.AddPart(new ContainerPart());}
                var at=f.Zone.GetEntityPosition(f.Actor);Assert.AreEqual("Unavailable",f.Place());
                Assert.AreEqual(at,f.Zone.GetEntityPosition(f.Actor));Assert.Null(f.Actor.GetPart<SpreadTerritoryPart>());Assert.Zero(f.Commits);
                Assert.True(f.Population.SourceReceipt.IsCurrent,"No source should be claimed at an unsuitable preflight.");
            }
        }
        [TestCase("default-arrival")][TestCase("reserved")][TestCase("only-corridor")]
        public void PlayerRoutesAndArrivalRemainOutsideTheClaimedGround(string fault)
        {
            using(var f=new Fixture(cacheX:fault=="default-arrival"?40:12,cacheY:fault=="default-arrival"?12:10,
                actorX:fault=="default-arrival"?38:10,actorY:fault=="default-arrival"?12:10))
            {
                if(fault=="reserved")f.Zone.GenReservedCells.Add((13,10));
                if(fault=="only-corridor")for(int y=0;y<CavesOfOoo.Core.Zone.Height;y++)for(int x=0;x<CavesOfOoo.Core.Zone.Width;x++)if(y<8||y>12)f.Wall(x,y);
                var proof=f.AllProof();Assert.AreEqual("Unavailable",f.Place());Assert.True(proof());Assert.Zero(f.Commits);
                Assert.True(f.Population.SourceReceipt.IsCurrent);Assert.True(f.Containers.SourceReceipt.IsCurrent);
            }
        }
        [Test] public void ARefusedOuterCommitRollsBackTheStillOwnedActorAndOnlyItsNewRole()
        {
            using(var f=new Fixture())
            {
                var original=f.AllProof();bool saw=false;
                Assert.AreEqual("Rejected",f.Place(commit:(packet,proof)=>{saw=true;Assert.True(proof());return false;}));
                Assert.True(saw);Assert.True(original());Assert.Null(f.Actor.GetPart<SpreadTerritoryPart>());
            }
        }
        [Test] public void AnIndependentActorRelocationDuringCommitIsNeverOverwrittenByRollback()
        {
            using(var f=new Fixture())
            {
                bool saw=false;var cacheProof=ClaimedSupplyContract.Graph(f.Zone,new[]{f.Cache});
                Assert.AreEqual("Rejected",f.Place(commit:(packet,proof)=>{saw=true;Assert.True(f.Zone.MoveEntity(f.Actor,50,18));return false;}));
                Assert.True(saw);Assert.AreEqual((50,18),f.Zone.GetEntityPosition(f.Actor));Assert.True(cacheProof());
            }
        }
        [Test] public void ChangedRealStockInsideCommitRefusesAndKeepsTheIndependentTransfer()
        {
            using(var f=new Fixture())
            {
                var box=f.Cache.GetPart<ContainerPart>();var first=box.Contents[0];bool saw=false;
                Assert.AreEqual("Rejected",f.Place(commit:(packet,proof)=>{saw=true;Assert.True(box.RemoveItem(first));Assert.False(proof());return true;}));
                Assert.True(saw);Assert.False(box.Contents.Contains(first));Assert.Null(first.GetPart<PhysicsPart>().InInventory);
            }
        }
        [TestCase("stock")][TestCase("post")][TestCase("role")]
        public void FinalGenerationProofRejectsChangedStockPostOrDutyAfterLocalPlacement(string fault)
        {
            using(var f=new Fixture())
            {
                Assert.AreEqual("Committed",f.Place());Assert.True(f.Final());
                if(fault=="stock"){var box=f.Cache.GetPart<ContainerPart>();Assert.True(box.RemoveItem(box.Contents[0]));}
                if(fault=="post")Assert.True(f.Zone.RemoveEntity(f.Cache));
                if(fault=="role")f.Actor.GetPart<SpreadTerritoryPart>().GraceTurns=1;
                Assert.False(f.Final());
            }
        }
        [TestCase(false)][TestCase(true)]
        public void ARealTakeAllUsesTheGraceWindowWhilePromptWithdrawalStaysPeaceful(bool take)
        {
            using(var globals=new SpreadExplorationActorTests.Scope())using(var f=new Fixture())
            {
                Assert.AreEqual("Committed",f.Place());var role=f.Actor.GetPart<SpreadTerritoryPart>();
                var player=f.Factory.CreateEntity("Player");Assert.True(f.Zone.AddEntity(player,12,7));
                var brain=f.Actor.GetPart<BrainPart>();brain.CurrentZone=f.Zone;
                var probe=new SpreadExplorationActorTests.AttackProbe();f.Actor.AddPart(probe);
                void Turn()=>f.Actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
                Turn();Assert.Null(role.WarningTarget);Assert.Zero(probe.Attacks);
                Assert.True(f.Zone.MoveEntity(player,12,8));Turn();Assert.AreSame(player,role.WarningTarget);Assert.AreEqual(2,role.GraceRemaining);
                if(!take)
                {
                    Assert.True(f.Zone.MoveEntity(player,12,7));Turn();Assert.Null(role.WarningTarget);Assert.Zero(probe.Attacks);
                    Assert.False(brain.IsPersonallyHostileTo(player));return;
                }
                Assert.True(f.Zone.MoveEntity(player,12,9));Turn();Assert.AreEqual(1,role.GraceRemaining);
                var box=f.Cache.GetPart<ContainerPart>();var stock=box.Contents.ToArray();Assert.IsNotEmpty(stock);
                int before=player.GetPart<InventoryPart>().Objects.Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
                int units=stock.Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
                foreach(var item in stock)Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(f.Cache,item),player,f.Zone).Success,item.BlueprintName);
                Assert.IsEmpty(box.Contents);Assert.AreEqual(before+units,player.GetPart<InventoryPart>().Objects.Sum(e=>e.GetPart<StackerPart>()?.StackCount??1));
                // The popup/take-all is one paid action, not one per child.
                // This controlled test exercises the actual transfer and role;
                // ordinary native InputHandler payment is a separate Play gate.
                Turn();Assert.Zero(role.GraceRemaining);Assert.Zero(probe.Attacks);
                Assert.True(f.Zone.MoveEntity(player,12,8));Turn();Assert.Greater(probe.Attacks,0);
                Assert.True(role.IsEnforcingAgainst(player,f.Zone));Assert.False(brain.IsPersonallyHostileTo(player));
                Assert.True(f.Zone.MoveEntity(player,12,7));Turn();Assert.Null(role.WarningTarget);
            }
        }
        [Test] public void WalkingAroundTheActualClaimRemainsPeaceful()
        {
            using(var globals=new SpreadExplorationActorTests.Scope())using(var f=new Fixture())
            {
                Assert.AreEqual("Committed",f.Place());var role=f.Actor.GetPart<SpreadTerritoryPart>();var player=f.Factory.CreateEntity("Player");
                Assert.True(f.Zone.AddEntity(player,8,7));f.Actor.GetPart<BrainPart>().CurrentZone=f.Zone;
                var probe=new SpreadExplorationActorTests.AttackProbe();f.Actor.AddPart(probe);
                for(int x=8;x<=16;x++){Assert.True(f.Zone.MoveEntity(player,x,7));f.Actor.FireEventAndRelease(GameEvent.New("TakeTurn"));Assert.Null(role.WarningTarget);}
                Assert.Zero(probe.Attacks);Assert.False(f.Actor.GetPart<BrainPart>().IsPersonallyHostileTo(player));Assert.IsNotEmpty(f.Cache.GetPart<ContainerPart>().Contents);
            }
        }
        [Test] public void ExamineNamesOnlyTheActualContainerAndNeverPromisesItsHiddenStock()
        {
            using(var f=new Fixture())
            {
                Assert.AreEqual("Committed",f.Place());string text=SpreadExplorationReadout.Describe(f.Actor);
                StringAssert.Contains("around that crate",text);StringAssert.Contains("withdraw",text);
                StringAssert.Contains(text,f.Actor.GetPart<ExaminablePart>().BuildExamineLine());
                var box=f.Cache.GetPart<ContainerPart>();foreach(var item in box.Contents.ToArray())Assert.True(box.RemoveItem(item));
                Assert.AreEqual(text,SpreadExplorationReadout.Describe(f.Actor),"The description identifies guarded ground, not a remaining-stock promise.");
                Assert.True(f.Zone.RemoveEntity(f.Cache));Assert.Null(SpreadExplorationReadout.Describe(f.Actor));
            }
        }
        [Test] public void AnOuterCommitExceptionRestoresOnlyTheLocallyOwnedMoveAndRole()
        {
            using(var f=new Fixture())
            {
                var original=f.AllProof();var error=Assert.Throws<InvalidOperationException>(()=>f.Place(commit:(owners,proof)=>{Assert.True(proof());throw new InvalidOperationException("commit observer");}));
                Assert.AreEqual("commit observer",error.Message);Assert.True(original());Assert.Null(f.Actor.GetPart<SpreadTerritoryPart>());
            }
        }
        [Test] public void ARealSameZoneContainerReceiptFromAnotherFactoryCannotBorrowTheActor()
        {
            using(var f=new Fixture())using(var other=new HaulingContentScope())
            {
                // Factories have independent numeric allocators starting at1.
                // Give this fixture producer a disjoint range before it creates
                // anything; duplicate IDs must not mask the factory authority check.
                var allocator=typeof(EntityFactory).GetField("_nextEntityID",ClaimedSupplyContract.All);
                Assert.NotNull(allocator);allocator.SetValue(other.Factory,allocator.GetValue(f.Factory));
                ContainerPlacementService.Factory=other.Factory;
                var producer=new ContainerBuilder(BiomeType.Spread,1,ContainerPlacementService.ZoneKind.Wilderness){CaptureSourceReceipts=true};
                Assert.True(producer.BuildZone(f.Zone,other.Factory,new FirstRandom()));Assert.True(producer.SourceReceipt.IsCurrent);
                Assert.AreSame(f.Zone,producer.SourceReceipt.Zone);Assert.AreNotSame(f.Population.SourceReceipt.Factory,producer.SourceReceipt.Factory);
                var proof=f.AllProof();Assert.AreEqual("Unavailable",ClaimedSupplyContract.Place(f.Zone,f.Actor,f.Population.SourceReceipt,producer.SourceReceipt,()=>true,(owners,final)=>true));
                Assert.True(proof());Assert.True(f.Population.SourceReceipt.IsCurrent);Assert.Null(f.Actor.GetPart<SpreadTerritoryPart>());
            }
        }
        [Test] public void AReplacementNativeContainerReceiptInvalidatesTheAcceptedPacket()
        {
            using(var f=new Fixture())
            {
                Assert.AreEqual("Committed",f.Place());Assert.True(f.Final());var original=f.Containers.SourceReceipt;
                Assert.True(f.Containers.BuildZone(f.Zone,f.Factory,new FirstRandom()));Assert.AreNotSame(original,f.Containers.SourceReceipt);
                Assert.True(f.Containers.SourceReceipt.IsCurrent);Assert.False(f.Final());
            }
        }

    }
    public sealed class ClaimedSuppliesSaveTests
    {
        sealed class ObservedManager:OverworldZoneManager
        {
            internal Action<Zone> After;internal int Seen;
            internal ObservedManager(EntityFactory factory):base(factory,1){}
            protected override void OnZoneGenerated(Zone zone,string id)
            {
                base.OnZoneGenerated(zone,id);
                if(!zone.GetReadOnlyEntities().Any(e=>e.GetPart<SpreadTerritoryPart>()?.Post?.HasPart<ContainerPart>()==true))return;
                Seen++;After?.Invoke(zone);
            }
        }
        [TestCase(false)][TestCase(true)]
        public void FinalAcceptanceUsesTheExactCurrentGraphAfterGenerationCallbacks(bool replace)
        {
            using(var scope=new HaulingContentScope())
            {
                const string id="Overworld.17.12.0";scope.Seed(unchecked(1^FormationSelector.StableIndex(id,int.MaxValue)));
                var manager=new ObservedManager(scope.Factory);Zone foreign=null;
                manager.After=zone=>{if(replace){foreign=new Zone(id);manager.CachedZones[id]=foreign;}};
                var result=manager.GetZone(id);Assert.AreEqual(1,manager.Seen,"Callback must actually observe the new cache duty.");
                if(replace){Assert.Null(result);Assert.AreSame(foreign,manager.CachedZones[id]);Assert.Zero(manager.Exploration.DispositionFor(id));}
                else{Assert.NotNull(result);Assert.AreSame(result,manager.CachedZones[id]);Assert.AreEqual(2,manager.Exploration.DispositionFor(id));}
            }
        }
        [TestCase(false)][TestCase(true)]
        public void RealTakenStockAndActualPostAftermathSurviveSaveWithoutRecomposition(bool removePost)
        {
            using(var globals=new SpreadExplorationActorTests.Scope())using(var scope=new HaulingContentScope())
            {
                const int seed=1;const string id="Overworld.17.12.0";scope.Seed(unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue)));
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);var zone=manager.GetZone(id);Assert.NotNull(zone);
                var actor=zone.GetReadOnlyEntities().Single(e=>e.GetPart<SpreadTerritoryPart>()?.Post?.BlueprintName=="Sack");
                var role=actor.GetPart<SpreadTerritoryPart>();var post=role.Post;var box=post.GetPart<ContainerPart>();Assert.IsNotEmpty(box.Contents);
                var player=scope.Factory.CreateEntity("Player");Assert.True(zone.AddEntity(player,75,19));manager.SetActiveZone(zone);
                actor.GetPart<BrainPart>().CurrentZone=zone;actor.FireEventAndRelease(GameEvent.New("TakeTurn"));Assert.AreSame(player,role.WarningTarget);Assert.AreEqual(2,role.GraceRemaining);
                foreach(var item in box.Contents.ToArray())Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(post,item),player,zone).Success,item.BlueprintName);
                Assert.IsEmpty(box.Contents);string actorID=actor.ID,postID=post.ID;
                string[] cargo=player.GetPart<InventoryPart>().Objects.Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)).ToArray();
                var actorAt=zone.GetEntityPosition(actor);var postAt=zone.GetEntityPosition(post);int roots=zone.EntityCount;
                if(removePost){Assert.True(zone.RemoveEntity(post));roots--;}
                var session=GameSessionState.Capture("claimed-supplies","aftermath",manager,new TurnManager(),player,0);GameSessionState restored;
                using(var stream=new MemoryStream()){session.Save(new SaveWriter(stream));stream.Position=0;restored=GameSessionState.Load(new SaveReader(stream,scope.Factory));}
                var loaded=(OverworldZoneManager)restored.ZoneManager;var returned=loaded.GetZone(id);Assert.NotNull(returned);Assert.AreNotSame(zone,returned);Assert.AreEqual(roots,returned.EntityCount);
                Assert.AreEqual(16,loaded.Exploration.Version);Assert.AreEqual(2,loaded.Exploration.DispositionFor(id));
                var defender=returned.GetReadOnlyEntities().Single(e=>e.ID==actorID);var saved=defender.GetPart<SpreadTerritoryPart>();Assert.NotNull(saved);Assert.AreEqual(actorAt,returned.GetEntityPosition(defender));
                var cache=returned.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==postID);Assert.AreEqual(!removePost,cache!=null);
                CollectionAssert.AreEqual(cargo,restored.Player.GetPart<InventoryPart>().Objects.Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)));
                if(!removePost)
                {
                    Assert.AreSame(cache,saved.Post);Assert.AreEqual(postAt,returned.GetEntityPosition(cache));Assert.IsEmpty(cache.GetPart<ContainerPart>().Contents);
                    Assert.AreSame(restored.Player,saved.WarningTarget);Assert.AreEqual(2,saved.GraceRemaining);StringAssert.Contains("around that sack",SpreadExplorationReadout.Describe(defender));
                }
                else
                {
                    defender.GetPart<BrainPart>().CurrentZone=returned;defender.FireEventAndRelease(GameEvent.New("TakeTurn"));Assert.Null(saved.WarningTarget);Assert.Null(SpreadExplorationReadout.Describe(defender));
                }
                Assert.AreSame(returned,loaded.GetZone(id));Assert.AreEqual(roots,returned.EntityCount);Assert.AreEqual(removePost?0:1,returned.GetReadOnlyEntities().Count(e=>e.ID==postID));
            }
        }
    }

}
