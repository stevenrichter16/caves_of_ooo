using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class SpreadExplorationActorPlacementTests
    {
        static bool Call(string method,Zone zone,Entity actor,Func<bool> current)
        {var t=typeof(Entity).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationActorPlacement");Assert.NotNull(t,"Missing exact-owner placement helper");var m=t.GetMethod(method);Assert.NotNull(m);return (bool)m.Invoke(null,new object[]{zone,actor,current});}
        sealed class Fixture:IDisposable
        {
            internal readonly SpreadExplorationActorTests.Fixture F=new SpreadExplorationActorTests.Fixture();
            public Zone Z=>F.Zone;public Entity A=>F.Actor;
            public Fixture(bool grazer=false)
            {
                F.Zone.RemoveEntity(F.Player);F.Post.GetPart<PhysicsPart>().Solid=true;F.Post.AddPart(new RenderPart{DisplayName="hedge"});
                A.BlueprintName=grazer?"ReedbackGrazer":"MarlbackScrabbler";
                if(grazer){F.Brain.Passive=true;F.Add("SpreadGrazerPart");}
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++){var ground=new Entity{ID="ground"+x+","+y,BlueprintName="Grass"};ground.Tags["Terrain"]="true";Assert.True(Z.AddEntity(ground,x,y));}
            }
            public bool Run(bool grazer=false,Func<bool> guard=null)=>Call(grazer?"TryGleanings":"TryTerritory",Z,A,guard??(()=>true));
            public Part Territory=>A.GetPart("SpreadTerritory");
            public object Get(Part p,string key)=>p.GetType().GetField(key).GetValue(p);
            public void Wall(int x,int y){var wall=F.Prop("wall"+x+","+y,x,y);wall.BlueprintName="StoneWall";wall.GetPart<PhysicsPart>().Solid=true;}
            public void Dispose()=>F.Dispose();
        }
        [Test] public void TerritoryUsesExactExistingActorAndPostWithInteriorWarningBoundary()
        {using(var f=new Fixture()){var owners=f.Z.GetReadOnlyEntities().ToArray();Assert.True(f.Run());Assert.NotNull(f.Territory);Assert.AreSame(f.F.Post,f.Get(f.Territory,"Post"));CollectionAssert.AreEquivalent(owners,f.Z.GetReadOnlyEntities());Assert.Greater((int)f.Get(f.Territory,"Left"),0);Assert.Less((int)f.Get(f.Territory,"Right"),Zone.Width-1);Assert.Greater((int)f.Get(f.Territory,"Top"),0);Assert.Less((int)f.Get(f.Territory,"Bottom"),Zone.Height-1);}}
        [TestCase(false)][TestCase(true)] public void RefusedAuthorityDoesNotMoveOrConfigureAnyOwner(bool grazer)
        {using(var f=new Fixture(grazer)){var before=f.Z.GetEntityPosition(f.A);int v=f.Z.EntityVersion;Assert.False(f.Run(grazer,()=>false));Assert.AreEqual(before,f.Z.GetEntityPosition(f.A));Assert.AreEqual(v,f.Z.EntityVersion);if(grazer)Assert.False((bool)f.Get(f.F.Role,"Configured"));else Assert.IsNull(f.Territory);}}
        [Test] public void NoPostIsAnHonestTerritoryRefusal()
        {using(var f=new Fixture()){f.Z.RemoveEntity(f.F.Post);var before=f.Z.GetEntityPosition(f.A);Assert.False(f.Run());Assert.AreEqual(before,f.Z.GetEntityPosition(f.A));Assert.IsNull(f.Territory);}}
        [Test] public void ExistingConfiguredDutyCannotBeResetByPlacement()
        {using(var f=new Fixture()){Assert.True(f.Run());var role=f.Territory;var at=f.Z.GetEntityPosition(f.A);Assert.False(f.Run());Assert.AreSame(role,f.Territory);Assert.AreEqual(at,f.Z.GetEntityPosition(f.A));}}
        [Test] public void NoActualBorderApproachRejectsTerritoryInsteadOfClaimingBypass()
        {using(var f=new Fixture()){for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(x==0||y==0||x==Zone.Width-1||y==Zone.Height-1)f.Wall(x,y);Assert.False(f.Run());Assert.IsNull(f.Territory);}}
        [Test] public void SingleLaneThroughTheProposedTerritoryIsNotABypass()
        {using(var f=new Fixture()){for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(y<8||y>11)f.Wall(x,y);Assert.False(f.Run());Assert.IsNull(f.Territory);}}
        [Test] public void GleaningsConfiguresTheSameFactoryRoleAndTwoExistingFiniteRows()
        {using(var f=new Fixture(true)){var role=f.F.Role;var owners=f.Z.GetReadOnlyEntities().ToArray();Assert.True(f.Run(true));Assert.AreSame(role,f.A.GetPart("SpreadGrazer"));Assert.AreSame(f.F.Food,f.Get(role,"Food"));Assert.AreSame(f.F.Reserve,f.Get(role,"ReservedRow"));CollectionAssert.AreEquivalent(owners,f.Z.GetReadOnlyEntities());Assert.False(f.F.Food.GetPart<FieldHarvestPart>().Harvested);Assert.False(f.F.Reserve.GetPart<FieldHarvestPart>().Harvested);f.F.Turn();Assert.True(f.F.Food.GetPart<FieldHarvestPart>().Harvested);Assert.False(f.F.Reserve.GetPart<FieldHarvestPart>().Harvested);}}
        [Test] public void SingleRemainingRowRefusesWithoutFabricatingOrConfiguringFood()
        {using(var f=new Fixture(true)){f.Z.RemoveEntity(f.F.Reserve);var at=f.Z.GetEntityPosition(f.A);Assert.False(f.Run(true));Assert.AreEqual(at,f.Z.GetEntityPosition(f.A));Assert.False((bool)f.Get(f.F.Role,"Configured"));Assert.False(f.F.Food.GetPart<FieldHarvestPart>().Harvested);}}
        [Test] public void SealedReservedRowDoesNotCountAsPlayerAccessibleFood()
        {using(var f=new Fixture(true)){f.F.Move(f.F.Reserve,18,15);for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(dx!=0||dy!=0)f.Wall(18+dx,15+dy);Assert.False(f.Run(true));Assert.False((bool)f.Get(f.F.Role,"Configured"));}}
        [Test] public void ConsumedReserveCannotBeSelected()
        {using(var f=new Fixture(true)){f.F.Reserve.GetPart<FieldHarvestPart>().Harvested=true;Assert.False(f.Run(true));Assert.False(f.F.Food.GetPart<FieldHarvestPart>().Harvested);}}
        [TestCase(false)][TestCase(true)] public void ChangedSourceInsideAuthorityCallbackRejectsBeforeCommit(bool grazer)
        {using(var f=new Fixture(grazer)){int checks=0;var at=f.Z.GetEntityPosition(f.A);Assert.False(f.Run(grazer,()=>{checks++;if(checks==2){if(grazer)f.Z.RemoveEntity(f.F.Reserve);else f.Z.RemoveEntity(f.F.Post);}return true;}));Assert.GreaterOrEqual(checks,2);Assert.AreEqual(at,f.Z.GetEntityPosition(f.A));if(grazer)Assert.False((bool)f.Get(f.F.Role,"Configured"));else Assert.IsNull(f.Territory);}}
        [TestCase(false)][TestCase(true)] public void FinalAuthorityRefusalRestoresOnlyTheOwnedActorAndRole(bool grazer)
        {using(var f=new Fixture(grazer)){var at=f.Z.GetEntityPosition(f.A);var original=f.F.Role;bool saw=false;Assert.False(f.Run(grazer,()=>{var role=f.A.GetPart(grazer?"SpreadGrazer":"SpreadTerritory");if(role!=null&&(bool)f.Get(role,"Configured")){saw=true;return false;}return true;}));Assert.True(saw);Assert.AreEqual(at,f.Z.GetEntityPosition(f.A));if(grazer){Assert.AreSame(original,f.A.GetPart("SpreadGrazer"));Assert.False((bool)f.Get(original,"Configured"));Assert.IsNull(f.Get(original,"Food"));}else Assert.IsNull(f.Territory);}}
        [Test] public void CallbackOwnedRelocationIsNotOverwrittenByRollback()
        {using(var f=new Fixture()){bool moved=false;Assert.False(f.Run(false,()=>{if(f.Territory!=null&&(bool)f.Get(f.Territory,"Configured")){Assert.True(f.Z.MoveEntity(f.A,40,20));moved=true;return false;}return true;}));Assert.True(moved);Assert.AreEqual((40,20),f.Z.GetEntityPosition(f.A));}}
        [Test] public void RemovedActorAndSameIdReplacementAreNeverReclaimed()
        {using(var f=new Fixture()){Entity replacement=null;Assert.False(f.Run(false,()=>{if(f.Territory!=null&&(bool)f.Get(f.Territory,"Configured")){f.Z.RemoveEntity(f.A);replacement=new Entity{ID=f.A.ID,BlueprintName=f.A.BlueprintName};Assert.True(f.Z.AddEntity(replacement,40,20));return false;}return true;}));Assert.NotNull(replacement);Assert.AreEqual((40,20),f.Z.GetEntityPosition(replacement));Assert.IsNull(f.Z.GetEntityCell(f.A));}}
        [Test] public void PlacedAnimalCannotBlockTheOnlyApproachToItsReservedRow()
        {using(var f=new Fixture(true)){f.Z.RemoveEntity(f.F.Post);f.F.Move(f.A,5,10);f.F.Move(f.F.Food,12,10);f.F.Move(f.F.Reserve,14,10);for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(y!=10||x>15)f.Wall(x,y);Assert.True(f.Run(true));var path=FindPath.Search(f.Z,0,10,14,10,ignoreCreatures:false);Assert.True(path.Usable,"Exact current reserved row remains physically reachable after placement.");}}
        [Test] public void FinalCallbackCannotInvalidateBypassAfterTheGeometrySnapshot()
        {using(var f=new Fixture()){bool changed=false;Assert.False(f.Run(false,()=>{if(f.Territory!=null&&(bool)f.Get(f.Territory,"Configured")&&!changed){changed=true;for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(x==0||y==0||x==Zone.Width-1||y==Zone.Height-1)f.Wall(x,y);}return true;}));Assert.True(changed);Assert.IsNull(f.Territory);}}
        [Test] public void ReplacedHarvestPartCannotBorrowTheOldRowProof()
        {using(var f=new Fixture(true)){var original=f.F.Reserve.GetPart<FieldHarvestPart>();int calls=0;Assert.False(f.Run(true,()=>{if(++calls==2){f.F.Reserve.RemovePart(original);f.F.Reserve.AddPart(new FieldHarvestPart());}return true;}));Assert.AreNotSame(original,f.F.Reserve.GetPart<FieldHarvestPart>());Assert.False((bool)f.Get(f.F.Role,"Configured"));}}
        [Test] public void FinalCallbackCanOnlyLeaveTheReservedFoodActuallyReachable()
        {using(var f=new Fixture(true)){bool changed=false;Assert.False(f.Run(true,()=>{if((bool)f.Get(f.F.Role,"Configured")&&!changed){changed=true;var at=f.Z.GetEntityPosition(f.F.Reserve);for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(dx!=0||dy!=0)f.Wall(at.x+dx,at.y+dy);}return true;}));Assert.True(changed);Assert.False((bool)f.Get(f.F.Role,"Configured"));}}
        [Test] public void RollbackDoesNotRemoveTheCallbackOwnerClaimingTheOrigin()
        {using(var f=new Fixture()){var origin=f.Z.GetEntityPosition(f.A);Entity blocker=null;Assert.False(f.Run(false,()=>{if(f.Territory!=null&&(bool)f.Get(f.Territory,"Configured")){blocker=f.F.Prop("foreign-claim",origin.x,origin.y);blocker.GetPart<PhysicsPart>().Solid=true;return false;}return true;}));Assert.NotNull(blocker);Assert.AreEqual(origin,f.Z.GetEntityPosition(blocker));Assert.AreNotEqual(origin,f.Z.GetEntityPosition(f.A));Assert.IsNull(f.Territory);}}
        [Test] public void FactoryRoleReplacementSurvivesFailureWithoutBeingResetOrRemoved()
        {using(var f=new Fixture(true)){Part replacement=null;Assert.False(f.Run(true,()=>{if((bool)f.Get(f.F.Role,"Configured")){f.A.RemovePart(f.F.Role);replacement=(Part)Activator.CreateInstance(f.F.Role.GetType());f.A.AddPart(replacement);return false;}return true;}));Assert.NotNull(replacement);Assert.AreSame(replacement,f.A.GetPart("SpreadGrazer"));Assert.AreSame(f.A,replacement.ParentEntity);Assert.False((bool)f.Get(replacement,"Configured"));}}
        [TestCase(false,"unchanged")][TestCase(true,"unchanged")]
        [TestCase(false,"units")][TestCase(true,"units")]
        [TestCase(false,"carried")][TestCase(true,"carried")]
        [TestCase(false,"equipped")][TestCase(true,"equipped")]
        [TestCase(false,"body")][TestCase(true,"body")]
        [TestCase(false,"stat")][TestCase(true,"stat")]
        [TestCase(false,"property")][TestCase(true,"property")]
        [TestCase(false,"part-field")][TestCase(true,"part-field")]
        [TestCase(false,"part-added")][TestCase(true,"part-added")]
        [TestCase(false,"child-owner")][TestCase(true,"child-owner")]
        [TestCase(false,"personal-enemy")][TestCase(true,"personal-enemy")]
        [TestCase(false,"goal")][TestCase(true,"goal")]
        public void FinalCallbackCannotBorrowAnAlteredActorLoadoutOrState(bool grazer,string mutation)
        {
            using(var f=new Fixture(grazer))
            {
                var inventory=f.A.GetPart<InventoryPart>();
                var carried=new Entity{ID="carried",BlueprintName="Bone"};carried.AddPart(new PhysicsPart{InInventory=f.A});carried.AddPart(new StackerPart{StackCount=2});inventory.Objects.Add(carried);
                var equipped=new Entity{ID="equipped",BlueprintName="Cudgel"};equipped.AddPart(new PhysicsPart{Equipped=f.A});equipped.AddPart(new StackerPart());inventory.EquippedItems.Add("Hand",equipped);
                var body=new Body();f.A.AddPart(body);var hand=new CavesOfOoo.Core.Anatomy.BodyPart{Type="Hand",_Equipped=equipped};body.SetBody(hand);
                bool observed=false;
                bool result=f.Run(grazer,()=>
                {
                    var role=f.A.GetPart(grazer?"SpreadGrazer":"SpreadTerritory");
                    if(role!=null&&(bool)f.Get(role,"Configured")&&!observed)
                    {
                        observed=true;
                        switch(mutation)
                        {
                            case "units":carried.GetPart<StackerPart>().StackCount=1;break;
                            case "carried":inventory.Objects.Remove(carried);break;
                            case "equipped":inventory.EquippedItems.Clear();break;
                            case "body":hand._Equipped=null;break;
                            case "stat":f.A.Statistics["Strength"].Penalty=3;break;
                            case "property":f.A.Properties["callback-owned"]="retained";break;
                            case "part-field":f.F.Brain.Wanders=true;break;
                            case "part-added":f.A.AddPart(new ExaminablePart());break;
                            case "child-owner":equipped.GetPart<PhysicsPart>().Equipped=null;break;
                            case "personal-enemy":f.F.Brain.PersonalEnemies.Add(f.F.Player);break;
                            case "goal":f.F.Brain.PushGoal(new WaitGoal(5));break;
                        }
                    }
                    return true;
                });
                Assert.True(observed,"Callback must execute after the helper's own configuration.");
                Assert.AreEqual(mutation=="unchanged",result,"The exact prior source graph must survive the final callback.");
                // Refusal never restores unrelated callback-owned actor/item changes.
                if(mutation=="units")Assert.AreEqual(1,carried.GetPart<StackerPart>().StackCount);
                if(mutation=="carried")CollectionAssert.DoesNotContain(inventory.Objects,carried);
                if(mutation=="equipped")Assert.AreEqual(0,inventory.EquippedItems.Count);
                if(mutation=="body")Assert.IsNull(hand._Equipped);
                if(mutation=="stat")Assert.AreEqual(3,f.A.Statistics["Strength"].Penalty);
                if(mutation=="property")Assert.AreEqual("retained",f.A.Properties["callback-owned"]);
                if(mutation=="part-field")Assert.True(f.F.Brain.Wanders);
                if(mutation=="part-added")Assert.NotNull(f.A.GetPart<ExaminablePart>());
                if(mutation=="child-owner")Assert.IsNull(equipped.GetPart<PhysicsPart>().Equipped);
                if(mutation=="personal-enemy")Assert.True(f.F.Brain.PersonalEnemies.Contains(f.F.Player));
                if(mutation=="goal")Assert.IsInstanceOf<WaitGoal>(f.F.Brain.PeekGoal());
                if(mutation!="unchanged"){if(grazer)Assert.False((bool)f.Get(f.F.Role,"Configured"));else Assert.IsNull(f.Territory);}
            }
        }

        static Func<bool> FinalGeometry(Zone zone,Entity actor)
        {
            var type=typeof(Entity).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationActorPlacement");
            var method=type.GetMethod("CaptureFinalGeometry",BindingFlags.Static|BindingFlags.Public);
            Assert.NotNull(method,"Missing accepted-geometry proof for final generation callbacks.");
            return (Func<bool>)method.Invoke(null,new object[]{zone,actor});
        }
        [TestCase(false,"unchanged")][TestCase(true,"unchanged")]
        [TestCase(false,"off-route")][TestCase(true,"off-route")]
        [TestCase(false,"split")][TestCase(true,"split")]
        [TestCase(false,"border")][TestCase(true,"border")]
        [TestCase(false,"source")][TestCase(true,"source")]
        [TestCase(false,"actor")][TestCase(true,"actor")]
        public void AcceptedGeometrySurvivesOnlyChangesPreservingTheOriginalRoutes(bool grazer,string change)
        {
            using(var f=new Fixture(grazer))
            {
                Assert.True(f.Run(grazer));var proof=FinalGeometry(f.Z,f.A);Assert.NotNull(proof);Assert.True(proof());
                if(change=="off-route")f.Wall(50,15);
                if(change=="split")for(int y=0;y<Zone.Height;y++)f.Wall(40,y);
                if(change=="border")f.Wall(0,15);
                if(change=="source")
                {
                    if(grazer){var at=f.Z.GetEntityPosition(f.F.Reserve);for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(dx!=0||dy!=0)f.Wall(at.x+dx,at.y+dy);}
                    else Assert.True(f.Z.MoveEntity(f.F.Post,30,15));
                }
                if(change=="actor")Assert.True(f.Z.MoveEntity(f.A,40,20));
                int version=f.Z.EntityVersion;var position=f.Z.GetEntityPosition(f.A);var entities=f.Z.GetReadOnlyEntities().ToArray();
                bool expected=change=="unchanged"||change=="off-route";
                Assert.AreEqual(expected,proof());Assert.AreEqual(expected,proof(),"The query must not adapt its accepted baseline to a changed graph.");
                Assert.AreEqual(version,f.Z.EntityVersion);Assert.AreEqual(position,f.Z.GetEntityPosition(f.A));CollectionAssert.AreEqual(entities,f.Z.GetReadOnlyEntities());
            }
        }
        [TestCase(false)][TestCase(true)] public void UnconfiguredActorHasNoAcceptedFinalGeometry(bool grazer)
        {using(var f=new Fixture(grazer)){Assert.IsNull(FinalGeometry(f.Z,f.A));}}

        [TestCase(false)][TestCase(true)] public void NullGuardFailsClosedWithoutTouchingTheActor(bool grazer)
        {using(var f=new Fixture(grazer)){var at=f.Z.GetEntityPosition(f.A);Assert.False(Call(grazer?"TryGleanings":"TryTerritory",f.Z,f.A,null));Assert.AreEqual(at,f.Z.GetEntityPosition(f.A));}}
    }
}
