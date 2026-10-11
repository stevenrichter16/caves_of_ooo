using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class CompanionPackTests
    {
        protected Zone Zone; protected Entity Leader, Follower;
        protected InventoryPart Pack(Entity who) => who.GetPart<InventoryPart>();
        [SetUp] public void Setup()
        {
            FactionManager.Initialize(); MessageLog.Clear(); Zone = new Zone("companion-pack");
            Leader = Creature("leader", 10, 10); Leader.SetTag("Player"); Follower = Creature("follower", 11, 10);
            Assert.True(Follower.ApplyEffect(new RecruitedEffect(Leader), Leader, Zone));
        }
        protected Entity Creature(string id, int x, int y)
        {
            var e = new Entity { ID = id, BlueprintName = id }; e.SetTag("Creature"); e.SetTag("Faction", "Villagers");
            e.AddPart(new PhysicsPart { Solid = true }); e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new InventoryPart()); e.AddPart(new StatusEffectsPart()); e.AddPart(new BrainPart { CurrentZone = Zone, Rng = new Random(4) });
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Max = 20 };
            Assert.True(Zone.AddEntity(e, x, y)); Zone.GetCell(x,y).IsVisible=Zone.GetCell(x,y).Explored=true; return e;
        }
        protected Entity Item(Entity owner, string id = "supply", int count = 1)
        {
            var e = new Entity { ID = id, BlueprintName = id }; e.AddPart(new PhysicsPart { Weight = 2, Takeable = true });
            e.AddPart(new RenderPart { DisplayName = id }); if (count > 1) e.AddPart(new StackerPart { StackCount = count });
            Assert.True(Pack(owner).AddObject(e)); return e;
        }
        protected string Choice(string prefix) => WorldInteractionSystem.GatherActions(Follower, Leader).FirstOrDefault(a => a.Command.StartsWith(prefix, StringComparison.Ordinal))?.Command;
        protected bool Run(string command) => InventorySystem.PerformAction(Leader, Follower, command, Zone);
        [TestCase(false)] [TestCase(true)] public void RealMenuTransfersOneExactWholeStackInEitherDirection(bool retrieve)
        {
            Entity from = retrieve ? Follower : Leader, to = retrieve ? Leader : Follower;
            var item = Item(from, "supplies", 3); string command = Choice(retrieve ? "CompanionTake|" : "CompanionGive|"); Assert.NotNull(command);
            Assert.True(Run(command)); Assert.False(Pack(from).Objects.Contains(item)); Assert.Contains(item, Pack(to).Objects);
            Assert.AreSame(to, item.GetPart<PhysicsPart>().InInventory); Assert.AreEqual(3, item.GetPart<StackerPart>().StackCount);
            Assert.False(Run(command), "The same stale selection cannot transfer twice.");
        }
        [Test] public void FullPackInspectionIncludesLateItemsAndRealWeightWithoutChangingOwnership()
        {
            for (int i = 0; i < 40; i++) Item(Follower, "ration-" + i);
            string read = Choice("CompanionPack"); Assert.NotNull(read); Assert.True(Run(read));
            string text = MessageLog.ConsumeAnnouncement(); StringAssert.Contains("ration-39", text); StringAssert.Contains("80", text);
            Assert.AreEqual(40, Pack(Follower).Objects.Count); Assert.AreEqual(0, Pack(Leader).Objects.Count);
        }
        [TestCase("QuestItem")] [TestCase("NoTrade")] [TestCase("NoTake")] [TestCase("Owned")]
        public void BoundSuppliesAreNotOfferedOrAccepted(string tag)
        { var item = Item(Leader); string command = Choice("CompanionGive|"); Assert.NotNull(command); item.SetTag(tag); Assert.Null(Choice("CompanionGive|")); Assert.False(Run(command)); Assert.Contains(item, Pack(Leader).Objects); }
        [TestCase(false)] [TestCase(true)] public void HardCapacityRefusalLeavesSourceAndQuantityIntact(bool retrieve)
        { var from = retrieve ? Follower : Leader; var to = retrieve ? Leader : Follower; var item = Item(from,"heavy",3); string command = Choice(retrieve?"CompanionTake|":"CompanionGive|"); Assert.NotNull(command); Pack(to).MaxWeight = 5; Assert.False(Run(command)); Assert.Contains(item,Pack(from).Objects); Assert.AreEqual(3,item.GetPart<StackerPart>().StackCount); }
        [TestCase("far")] [TestCase("dismissed")] [TestCase("dead")] [TestCase("quantity")] [TestCase("foreign")]
        public void StaleSelectionRefusesChangedAuthorityOrItem(string change)
        {
            var item=Item(Leader,"supplies",3); string command=Choice("CompanionGive|"); Assert.NotNull(command);
            if(change=="far") Assert.True(Zone.MoveEntity(Follower,15,10));
            if(change=="dismissed") Follower.GetEffect<RecruitedEffect>().Dismiss(Leader);
            if(change=="dead") Follower.GetStat("Hitpoints").BaseValue=0;
            if(change=="quantity") item.GetPart<StackerPart>().StackCount=2;
            if(change=="foreign") item.GetPart<PhysicsPart>().InInventory=Follower;
            Assert.False(Run(command)); Assert.Contains(item,Pack(Leader).Objects); Assert.False(Pack(Follower).Objects.Contains(item));
        }
        public sealed class Callback : Part { public Action Change; public override string Name=>"CompanionPackCallback"; public override bool HandleEvent(GameEvent e) { if(e.ID=="AfterInventoryAction") Change(); return true; } }
        [TestCase(false)] [TestCase(true)] public void PostActionFailureOrDismissalRollsBackTransfer(bool dismiss)
        { var item=Item(Leader); string command=Choice("CompanionGive|"); Assert.NotNull(command); Leader.AddPart(new Callback{Change=()=>{if(dismiss) Follower.GetEffect<RecruitedEffect>().Dismiss(Leader);else throw new InvalidOperationException("rollback probe");}}); Assert.False(Run(command)); Assert.Contains(item,Pack(Leader).Objects); Assert.AreSame(Leader,item.GetPart<PhysicsPart>().InInventory); Assert.False(Pack(Follower).Objects.Contains(item)); }
        [Test] public void CompatibleResidentStackIsNotMergedOrConsumedByExplicitTransfer()
        {
            var resident=Item(Follower,"resident",2); var incoming=Item(Leader,"incoming",3);
            resident.BlueprintName=incoming.BlueprintName="Supply";
            string command=Choice("CompanionGive|"); Assert.NotNull(command); Assert.True(Run(command));
            Assert.Contains(resident,Pack(Follower).Objects); Assert.Contains(incoming,Pack(Follower).Objects);
            Assert.AreEqual(2,resident.GetPart<StackerPart>().StackCount); Assert.AreEqual(3,incoming.GetPart<StackerPart>().StackCount);
        }
        [TestCase("rental")] [TestCase("equipped")] [TestCase("duplicate-id")] [TestCase("duplicate-reference")] [TestCase("ground-alias")]
        public void AmbiguousOrRestrictedSourceCannotBeTransferred(string mutation)
        {
            var item=Item(Leader); string command=Choice("CompanionGive|"); Assert.NotNull(command);
            if(mutation=="rental") item.AddPart(new RentalPart());
            if(mutation=="equipped") { item.AddPart(new EquippablePart{Slot="Hand"}); Assert.True(InventorySystem.Equip(Leader,item)); }
            if(mutation=="duplicate-id") Item(Leader).ID=item.ID;
            if(mutation=="duplicate-reference") Pack(Leader).Objects.Add(item);
            if(mutation=="ground-alias") Assert.True(Zone.AddEntity(item,10,10));
            Assert.Null(Choice("CompanionGive|")); Assert.False(Run(command)); Assert.False(Pack(Follower).Objects.Contains(item));
        }
        [TestCase("brain")] [TestCase("pack")] [TestCase("move")] [TestCase("capacity")]
        public void CallbackInvalidationRestoresOnlyTheTransfer(string mutation)
        {
            var item=Item(Leader); string command=Choice("CompanionGive|"); var original=Pack(Follower); Assert.NotNull(command);
            Leader.AddPart(new Callback{Change=()=>{
                if(mutation=="brain") { Follower.RemovePart(Follower.GetPart<BrainPart>()); Follower.AddPart(new BrainPart{CurrentZone=Zone,PartyLeader=Leader}); }
                if(mutation=="pack") { Follower.RemovePart(original); Follower.AddPart(new InventoryPart()); }
                if(mutation=="move") Assert.True(Zone.MoveEntity(Follower,11,11));
                if(mutation=="capacity") original.MaxWeight=0;
            }});
            Assert.False(Run(command)); Assert.Contains(item,Pack(Leader).Objects); Assert.False(original.Objects.Contains(item));
            Assert.AreSame(Leader,item.GetPart<PhysicsPart>().InInventory);
        }
        [Test] public void NestedSameTransferRefusesAndIndependentItemSurvivesOuterRollback()
        {
            var first=Item(Leader,"first"); var independent=Item(Leader,"independent"); string command=Choice("CompanionGive|"); Assert.NotNull(command); bool nested=true;
            Leader.AddPart(new Callback{Change=()=>{nested=Run(command); Assert.True(Pack(Leader).RemoveObject(independent)); Assert.True(Pack(Follower).AddObject(independent)); throw new InvalidOperationException("outer rollback");}});
            Assert.False(Run(command)); Assert.False(nested); Assert.Contains(first,Pack(Leader).Objects); Assert.Contains(independent,Pack(Follower).Objects);
        }
        [TestCase(false)] [TestCase(true)] public void StrangerOrEngagedRecruitDoesNotOfferManagement(bool engaged)
        {
            Item(Leader); Assert.NotNull(Choice("CompanionGive|")); var other=Creature("other",10,11);
            if(engaged) Follower.GetPart<BrainPart>().Target=other; else Follower.GetEffect<RecruitedEffect>().Dismiss(Leader);
            Assert.Null(Choice("CompanionPack")); Assert.Null(Choice("CompanionGive|")); Assert.False(Run("CompanionPack")); Assert.False(MessageLog.HasPendingAnnouncement);
        }
        [TestCase(false)] [TestCase(true)] public void HiddenCompanionCannotRevealItsPackOrAcceptTransfer(bool renderHidden)
        {Item(Leader);Item(Follower,"private");string give=Choice("CompanionGive|");Assert.NotNull(give);if(renderHidden)Follower.GetPart<RenderPart>().Visible=false;else Zone.GetEntityCell(Follower).IsVisible=false;Assert.Null(Choice("CompanionPack"));Assert.False(Run("CompanionPack"));Assert.False(Run(give));Assert.False(MessageLog.HasPendingAnnouncement);}
        [Test] public void SoftStrengthAllowanceDoesNotBecomeAnInventedHardTransferCap()
        {Follower.Statistics["Strength"]=new Stat{Name="Strength",BaseValue=1,Max=100};var item=Item(Leader,"heavy",10);Assert.AreEqual(15,Pack(Follower).GetMaxCarryWeight());Assert.AreEqual(-1,Pack(Follower).MaxWeight);Assert.True(Run(Choice("CompanionGive|")));Assert.Contains(item,Pack(Follower).Objects);Assert.AreEqual(20,Pack(Follower).GetCarriedWeight());}
    }
}
