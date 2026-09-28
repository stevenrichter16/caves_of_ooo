using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class SpreadCollectorTests
    {
        public sealed class Callback : Part
        {
            public override string Name => "CollectorTestCallback";
            public string Event; public Action Action; public bool Refuse;
            public override bool HandleEvent(GameEvent e) { if(e.ID!=Event)return true; Action?.Invoke();return !Refuse; }
        }
        internal sealed class Fixture : IDisposable
        {
            internal readonly SpreadExplorationActorTests.Fixture Base=new SpreadExplorationActorTests.Fixture();
            internal Entity Home,Item; internal Part Role;
            internal Zone Zone=>Base.Zone; internal Entity Actor=>Base.Actor; internal BrainPart Brain=>Base.Brain;
            internal InventoryPart Inventory=>Actor.GetPart<InventoryPart>();
            internal ContainerPart Container=>Home.GetPart<ContainerPart>();
            internal Fixture(bool role=true)
            {
                Brain.Passive=true; Home=Base.Prop("collector-home",9,10);Home.BlueprintName="Sack";Home.AddPart(new ContainerPart());
                Item=Goods("collector-item",3);Assert.True(Zone.AddEntity(Item,11,10));
                if(role) {var t=typeof(BrainPart).Assembly.GetType("CavesOfOoo.Core.SpreadCollectorPart");Assert.NotNull(t,"Missing scoped collector role API");Role=(Part)Activator.CreateInstance(t);Actor.AddPart(Role);}
            }
            internal Entity Goods(string id,int count){var e=new Entity{ID=id,BlueprintName="Hatchet"};e.Tags["Item"]="true";e.AddPart(new PhysicsPart{Takeable=true,Weight=1});e.AddPart(new RenderPart{DisplayName="hatchet"});e.AddPart(new StackerPart{StackCount=count,MaxStack=99});e.AddPart(new EquippablePart{Slot="Hand"});return e;}
            internal bool Configure()=> (bool)Role.GetType().GetMethod("Configure").Invoke(Role,new object[]{Zone,Home,Item});
            internal object Field(string n)=>Role.GetType().GetField(n).GetValue(Role);
            internal string Phase=>Field("Phase").ToString();
            internal Entity Carried=>(Entity)Role.GetType().GetProperty("CurrentCarriedItem").GetValue(Role);
            internal void Turn()=>Base.Turn();
            internal void Pick(){Assert.True(Configure());Turn();Assert.AreSame(Item,Carried);}
            internal void RoundTrip(){Base.RoundTrip();Home=Zone.GetReadOnlyEntities().Single(e=>e.ID=="collector-home");Role=Actor.Parts.Single(p=>p.Name=="SpreadCollector");Item=(Entity)Field("Target");}
            public void Dispose()=>Base.Dispose();
        }
        [Test] public void LegacyOrdinaryOwnerDoesNotCollectTool(){using(var f=new Fixture(false)){f.Turn();Assert.NotNull(f.Zone.GetEntityCell(f.Item));Assert.IsEmpty(f.Inventory.Objects);}}
        [Test] public void OnePickupThenOneDepositKeepsExactWholeToolWithoutAutoEquip(){using(var f=new Fixture()){f.Pick();Assert.AreEqual("Carrying",f.Phase);Assert.IsEmpty(f.Inventory.EquippedItems);Assert.IsNull(f.Zone.GetEntityCell(f.Item));f.Turn();Assert.AreEqual("Deposited",f.Phase);Assert.IsNull(f.Carried);Assert.AreSame(f.Item,f.Container.Contents.Single());Assert.AreEqual(3,f.Item.GetPart<StackerPart>().StackCount);Assert.AreSame(f.Home,f.Item.GetPart<PhysicsPart>().InInventory);for(int i=0;i<3;i++)f.Turn();Assert.AreEqual(1,f.Container.Contents.Count);}}
        [TestCase("GoldCoin")] [TestCase("Starapple")] [TestCase("UnknownTool")]
        public void OnlyCurrentLooseSalvageAllowanceAccepted(string bp){using(var f=new Fixture()){f.Item.BlueprintName=bp;Assert.False(f.Configure());}}
        [TestCase("QuestItem")] [TestCase("Unique")] [TestCase("NoTrade")] [TestCase("Owned")] [TestCase("Essential")]
        public void AnnotatedSpecialGoodsRefuse(string tag){using(var f=new Fixture()){f.Item.Tags[tag]="true";Assert.False(f.Configure());}}
        [TestCase(false)] [TestCase(true)] public void RecruitedOrPlayerOwnerRefuses(bool player){using(var f=new Fixture()){if(player)f.Actor.Tags["Player"]="true";else f.Brain.SetPartyLeader(f.Base.Player);Assert.False(f.Configure());}}
        [Test] public void NonPassiveOwnerRefuses(){using(var f=new Fixture()){f.Brain.Passive=false;Assert.False(f.Configure());}}
        [Test] public void StolenTargetCannotBeRecreated(){using(var f=new Fixture()){Assert.True(f.Configure());Assert.True(f.Zone.RemoveEntity(f.Item));Assert.True(f.Base.Player.GetPart<InventoryPart>().AddObject(f.Item));f.Turn();Assert.IsNull(f.Carried);Assert.AreEqual("Stopped",f.Phase);Assert.AreSame(f.Base.Player,f.Item.GetPart<PhysicsPart>().InInventory);Assert.IsEmpty(f.Container.Contents);}}
        [Test] public void DestroyedHomeRetainsActualCarriedItem(){using(var f=new Fixture()){f.Pick();Assert.True(f.Zone.RemoveEntity(f.Home));f.Turn();Assert.AreSame(f.Item,f.Carried);Assert.AreEqual("Stopped",f.Phase);Assert.AreSame(f.Actor,f.Item.GetPart<PhysicsPart>().InInventory);}}
        [TestCase(false)] [TestCase(true)] public void HomeCapacityIsAtomicAndCanMerge(bool compatible){using(var f=new Fixture()){var resident=f.Goods("resident",2);if(!compatible)resident.BlueprintName="Cudgel";Assert.True(f.Container.AddItem(resident));f.Container.MaxItems=1;f.Pick();f.Turn();Assert.AreEqual(compatible?"Deposited":"Stopped",f.Phase);Assert.AreEqual(compatible?5:2,resident.GetPart<StackerPart>().StackCount);Assert.AreEqual(compatible?0:3,f.Item.GetPart<StackerPart>().StackCount);Assert.AreEqual(!compatible,f.Inventory.Objects.Contains(f.Item));Assert.AreEqual(1,f.Container.Contents.Count);}}
        [TestCase(false)] [TestCase(true)] public void SaveSeekingAndCarryingUsesReplacementExactReferences(bool carrying){using(var f=new Fixture()){Assert.True(f.Configure());if(carrying)f.Turn();var oldActor=f.Actor;var oldHome=f.Home;var oldItem=f.Item;f.RoundTrip();Assert.AreNotSame(oldActor,f.Actor);Assert.AreNotSame(oldHome,f.Home);Assert.AreNotSame(oldItem,f.Item);Assert.AreSame(f.Home,f.Field("Home"));if(!carrying)f.Turn();Assert.AreSame(f.Item,f.Carried);f.Turn();Assert.AreSame(f.Item,f.Container.Contents.Single());}}
        [Test] public void SaveDepositedCannotRestartTrip(){using(var f=new Fixture()){f.Pick();f.Turn();f.RoundTrip();Assert.AreEqual("Deposited",f.Phase);Assert.False(f.Configure());f.Turn();Assert.AreSame(f.Item,f.Container.Contents.Single());Assert.IsEmpty(f.Inventory.Objects);}}
        [Test] public void CalmAndCurrentWorkRemainAboveIdleCollection(){using(var f=new Fixture()){Assert.True(f.Configure());f.Brain.PushGoal(new NoFightGoal(10));f.Turn();Assert.NotNull(f.Zone.GetEntityCell(f.Item));Assert.IsNull(f.Carried);}}
        [Test] public void PaidPickupEndsExactlyOneActorTurnNoChildChain(){using(var f=new Fixture()){Assert.True(f.Configure());var turns=new TurnManager();turns.AddEntity(f.Actor);turns.AddEntity(f.Base.Player);Assert.AreSame(f.Base.Player,turns.ProcessUntilPlayerTurn());Assert.AreEqual(1,f.Base.Probe.Ends);Assert.AreEqual(0,turns.GetEnergy(f.Actor));Assert.AreSame(f.Item,f.Carried);Assert.IsEmpty(f.Container.Contents);Assert.AreEqual(1,f.Brain.GoalCount);}}
        [TestCase("veto")] [TestCase("quantity")] [TestCase("home")] [TestCase("actor")]
        public void BeforePickupCallbackRefusesChangedAuthority(string mutation){using(var f=new Fixture()){Assert.True(f.Configure());var p=new Callback{Event="BeforePickup",Refuse=mutation=="veto",Action=()=>{if(mutation=="quantity")f.Item.GetPart<StackerPart>().StackCount=4;if(mutation=="home")f.Zone.RemoveEntity(f.Home);if(mutation=="actor")f.Actor.GetPart<PhysicsPart>().InInventory=f.Base.Player;}};f.Actor.AddPart(p);f.Turn();Assert.IsNull(f.Carried);Assert.NotNull(f.Zone.GetEntityCell(f.Item));Assert.IsEmpty(f.Inventory.Objects);Assert.IsEmpty(f.Container.Contents);}}
        [Test] public void DepositMessageFailureRollsBackActualCommandAndPhase(){using(var f=new Fixture()){f.Pick();f.Actor.AddPart(new RenderPart{DisplayName="tatterjay"});f.Home.AddPart(new RenderPart{DisplayName="sack"});f.Zone.GetEntityCell(f.Actor).IsVisible=true;f.Zone.GetEntityCell(f.Home).IsVisible=true;int fired=0;MessageLog.OnMessage=s=>{if(s.StartsWith("tatterjay puts ")){fired++;throw new InvalidOperationException("receipt listener");}};f.Turn();Assert.AreEqual(1,fired);Assert.AreSame(f.Item,f.Carried);Assert.IsEmpty(f.Container.Contents);Assert.AreEqual(3,f.Item.GetPart<StackerPart>().StackCount);}}
        [Test] public void BeforePickupForeignTransferRemainsForeign(){using(var f=new Fixture()){Assert.True(f.Configure());f.Actor.AddPart(new Callback{Event="BeforePickup",Action=()=>{Assert.True(f.Zone.RemoveEntity(f.Item));Assert.True(f.Base.Player.GetPart<InventoryPart>().AddObject(f.Item));}});f.Turn();Assert.IsNull(f.Carried);Assert.AreSame(f.Base.Player,f.Item.GetPart<PhysicsPart>().InInventory);Assert.IsNull(f.Zone.GetEntityCell(f.Item));Assert.IsEmpty(f.Inventory.Objects);}}
    }
}
