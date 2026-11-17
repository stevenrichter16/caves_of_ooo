using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MachineContainedLiquidAdversarialTests : FiftyWorldFixture
    {
        Entity Tray()
        {
            var tray = Factory.CreateEntity("PhysicalObject"); tray.GetPart<PhysicsPart>().Solid = true; tray.GetPart<PhysicsPart>().Takeable = false;
            tray.AddPart(new ContainerPart()); tray.AddPart(new ContainedLiquidSourcePart());
            Assert.True(tray.GetPart<ContainerPart>().AddItem(Factory.CreateEntity("CinderhookAxeHeadComponent")));
            Assert.True(Zone.AddEntity(tray,11,10)); Zone.GetCell(11,10).IsVisible = Zone.GetCell(11,10).Explored = true; return tray;
        }
        string Fill(Entity tray) => "FillLiquidVessel|" + Id(tray) + "|oil";
        string Reach(Entity tray) => "RecoverThroughLiquid|" + Id(tray.GetPart<ContainerPart>().Contents.Single()) + "|" + Id(tray) + "|oil|12";
        public sealed class Probe : Part
        {
            public Action Callback;
            public string Event = "AfterInventoryAction";
            public override bool HandleEvent(GameEvent e) { if(e.ID==Event)Callback?.Invoke(); return true; }
        }
        public sealed class VetoCoating : Part
        {
            public override bool HandleEvent(GameEvent e) => e.ID != "BeforeApplyEffect" || !(e.GetParameter<Effect>("Effect") is LiquidCoveredEffect);
        }
        [TestCase("actor-move")] [TestCase("source-move")] [TestCase("source-part")] [TestCase("source-id")]
        [TestCase("source-capacity")] [TestCase("source-volume")] [TestCase("receiver-capacity")] [TestCase("receiver-part")]
        [TestCase("receiver-custody")] [TestCase("actor-inventory")]
        public void Adversarial_FillCallbackInvalidationCannotCommitLiquid(string mutation)
        {
            var tray = Tray(); var source = tray.GetPart<ContainedLiquidSourcePart>(); var flask = Flask(0); var receiver = flask.GetPart<LiquidVesselPart>();
            Actor.AddPart(new Probe { Callback = () =>
            {
                switch(mutation)
                {
                    case "actor-move": Zone.RemoveEntity(Actor); Zone.AddEntity(Actor,15,10); break;
                    case "source-move": Zone.RemoveEntity(tray); Zone.AddEntity(tray,12,10); break;
                    case "source-part": tray.RemovePart(source); tray.AddPart(new ContainedLiquidSourcePart { Volume=0 }); break;
                    case "source-id": tray.ID="changed"; break;
                    case "source-capacity": source.Capacity=20; break;
                    case "source-volume": source.Volume=2; break;
                    case "receiver-capacity": receiver.Capacity=20; break;
                    case "receiver-part": flask.RemovePart(receiver); flask.AddPart(new LiquidVesselPart()); break;
                    case "receiver-custody": Pack.RemoveObject(flask); Zone.AddEntity(flask,10,10); break;
                    case "actor-inventory": Actor.RemovePart(Pack); Actor.AddPart(new InventoryPart()); break;
                }
            }});
            Assert.False(Act(flask,Fill(tray))); Assert.AreEqual(12,source.Volume); Assert.AreEqual(0,receiver.Volume);
            Assert.AreEqual("",receiver.LiquidId);
            if(mutation=="receiver-custody") { Assert.Null(Zone.GetEntityCell(flask),"Rollback must not leave a carried/world alias."); Assert.True(Pack.Objects.Contains(flask)); }
        }
        [TestCase("taken")] [TestCase("outer")]
        public void Adversarial_RecoveryMovedIntoAnotherContainerRollsBackExactCustody(string phase)
        {
            var tray=Tray(); var head=tray.GetPart<ContainerPart>().Contents.Single(); var foreign=Factory.CreateEntity("Chest");
            var probe=new Probe { Event=phase=="taken"?"Taken":"AfterInventoryAction", Callback=()=>{Pack.RemoveObject(head);foreign.GetPart<ContainerPart>().AddItem(head);} };
            (phase=="taken"?head:Actor).AddPart(probe);
            Assert.False(Act(tray,Reach(tray))); Assert.AreSame(head,tray.GetPart<ContainerPart>().Contents.Single());
            Assert.False(foreign.GetPart<ContainerPart>().Contents.Contains(head)); Assert.False(Pack.Objects.Contains(head));
            Assert.AreSame(tray,head.GetPart<PhysicsPart>().InInventory); Assert.Null(Actor.GetEffect<LiquidCoveredEffect>());
        }
        [TestCase(false)] [TestCase(true)] public void Adversarial_RecoveryMergesOneUnitAndRollbackPreservesOlderStack(bool fail)
        {
            var tray=Tray(); var head=tray.GetPart<ContainerPart>().Contents.Single(); var older=Carry("CinderhookAxeHeadComponent");older.GetPart<StackerPart>().StackCount=3;
            if(fail)FailAfter();Assert.AreEqual(!fail,Act(tray,Reach(tray)));Assert.AreEqual(fail?3:4,older.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(fail?1:0,tray.GetPart<ContainerPart>().Contents.Count); Assert.AreEqual(fail?1:0,head.GetPart<StackerPart>().StackCount);
        }
        [TestCase("oil")] [TestCase("water")] public void Adversarial_ExistingStrongerCoatingUsesNormalMerge(string liquid)
        {
            var tray=Tray(); Actor.ApplyEffect(new LiquidCoveredEffect(liquid,30));Assert.True(Act(tray,Reach(tray)));
            Assert.AreEqual(liquid,Actor.GetEffect<LiquidCoveredEffect>().LiquidId);Assert.AreEqual(42,Actor.GetEffect<LiquidCoveredEffect>().Amount);
        }
        [TestCase(true)] [TestCase(false)] public void Adversarial_ChangedRecoveryQuoteCannotApplyUnadvertisedExposure(bool liquid)
        {
            var tray=Tray();string command=Reach(tray);var source=tray.GetPart<ContainedLiquidSourcePart>();
            if(liquid)source.LiquidId="acid";else source.Volume=6;
            Assert.False(Act(tray,command));Assert.AreEqual(1,tray.GetPart<ContainerPart>().Contents.Count);
            Assert.Null(Actor.GetEffect<LiquidCoveredEffect>());Assert.AreEqual(liquid?12:6,source.Volume);
        }
        [TestCase(false)] [TestCase(true)] public void Adversarial_MergeRecipientMovedToForeignContainerCannotDuplicateOnRollback(bool prefilled)
        {
            var tray=Tray();var head=tray.GetPart<ContainerPart>().Contents.Single();var older=Carry("CinderhookAxeHeadComponent");older.GetPart<StackerPart>().StackCount=2;
            var foreign=Factory.CreateEntity("Chest");var resident=Factory.CreateEntity("CinderhookAxeHeadComponent");
            if(prefilled)Assert.True(foreign.GetPart<ContainerPart>().AddItem(resident));
            Actor.AddPart(new Probe { Callback=()=>{Pack.RemoveObject(older);foreign.GetPart<ContainerPart>().AddItem(older);} });
            Assert.False(Act(tray,Reach(tray)));Assert.AreEqual(2,older.GetPart<StackerPart>().StackCount);
            Assert.True(Pack.Objects.Contains(older));Assert.AreEqual(prefilled?1:0,foreign.GetPart<ContainerPart>().Contents.Count);
            if(prefilled)Assert.AreEqual(1,resident.GetPart<StackerPart>().StackCount);
            Assert.AreSame(Actor,older.GetPart<PhysicsPart>().InInventory);Assert.AreSame(head,tray.GetPart<ContainerPart>().Contents.Single());
            Assert.AreEqual(1,head.GetPart<StackerPart>().StackCount);Assert.Null(Actor.GetEffect<LiquidCoveredEffect>());
        }
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
        public void Adversarial_RecoveryRefusesCallbackSplitsAndReleasesGuard(bool removeOne, bool rollback)
        {
            var tray=Tray();var older=Carry("CinderhookAxeHeadComponent");var stack=older.GetPart<StackerPart>();stack.StackCount=3;
            var foreign=Factory.CreateEntity("Chest");Entity clone=null;bool called=false;
            Actor.AddPart(new Probe { Callback=()=>{called=true;clone=removeOne?stack.RemoveOne():stack.SplitStack(1);if(clone!=null)foreign.GetPart<ContainerPart>().AddItem(clone);} });
            if(rollback)FailAfter();
            Assert.AreEqual(!rollback,Act(tray,Reach(tray)));Assert.True(called);Assert.Null(clone,"Protected custody must not emit an untracked clone.");
            Assert.IsEmpty(foreign.GetPart<ContainerPart>().Contents);Assert.AreEqual(rollback?3:4,stack.StackCount);
            var after=removeOne?stack.RemoveOne():stack.SplitStack(1);Assert.NotNull(after,"The guard must end with the transaction.");
            Assert.AreEqual(rollback?2:3,stack.StackCount);Assert.AreEqual(1,after.GetPart<StackerPart>().StackCount);
        }
        [Test] public void Adversarial_ExposureVetoDoesNotUndoSuccessfulRecovery()
        {
            var tray=Tray();Actor.AddPart(new VetoCoating());Assert.True(Act(tray,Reach(tray)));
            Assert.Null(Actor.GetEffect<LiquidCoveredEffect>());Assert.IsEmpty(tray.GetPart<ContainerPart>().Contents);
        }
        [TestCase("fill")] [TestCase("recovery")] public void Adversarial_ReentryCannotTakeOtherParticipantsDuringAnOpenReceipt(string action)
        {
            var tray=Tray();var a=Flask(0);var b=Flask(0);bool nested=true;
            var probe=new Probe();probe.Callback=()=>{probe.Callback=null;nested=Act(b,Fill(tray));};Actor.AddPart(probe);
            Assert.True(action=="fill"?Act(a,Fill(tray)):Act(tray,Reach(tray)));Assert.False(nested);Assert.AreEqual(0,Units(b));
        }
        [TestCase("far")] [TestCase("hidden")] [TestCase("locked")] [TestCase("alias")] [TestCase("removed")]
        public void Adversarial_UnavailableSourceCannotBeUsedByDirectCommands(string defect)
        {
            var tray=Tray();var head=tray.GetPart<ContainerPart>().Contents.Single();string reach=Reach(tray);var flask=Flask(0);
            if(defect=="far"){Zone.RemoveEntity(Actor);Zone.AddEntity(Actor,15,10);}
            if(defect=="hidden")Zone.GetCell(11,10).IsVisible=false;
            if(defect=="locked")tray.GetPart<ContainerPart>().Locked=true;
            if(defect=="alias"){var other=Place("Chair",12,10);other.ID=tray.ID;}
            if(defect=="removed")Zone.RemoveEntity(tray);
            Assert.False(Act(flask,Fill(tray)));Assert.False(Act(tray,reach));Assert.AreEqual(0,Units(flask));Assert.False(Pack.Objects.Contains(head));
        }
    }
}
