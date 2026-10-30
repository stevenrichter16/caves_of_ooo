using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class TrinketSkillRollbackTests
    {
        Entity actor;
        [SetUp] public void SetUp()
        {
            SkillRegistry.ResetForTests(); SkillRegistry.EnsureInitialized();
            actor=new Entity { ID=Guid.NewGuid().ToString("N"), BlueprintName="SkillRollbackActor" };
            actor.SetTag("Player"); actor.SetTag("Creature");
            actor.AddPart(new RenderPart { DisplayName="wearer" });
            actor.AddPart(new PhysicsPart());
            foreach(var name in new[]{"Hitpoints","Strength","Agility","Speed","DV"})
                actor.Statistics[name]=new Stat { Name=name, Owner=actor, BaseValue=name=="Speed"?100:20, Max=100 };
            var body=new Body(); body.SetBody(AnatomyFactory.CreateHumanoid()); actor.AddPart(body);
            actor.AddPart(new InventoryPart()); actor.AddPart(new ActivatedAbilitiesPart()); actor.AddPart(new SkillsPart());
            TrinketSlots.InitializePlayer(actor);
        }
        [TearDown] public void TearDown() => SkillRegistry.ResetForTests();

        Entity Trinket(string skills)
        {
            var item=new Entity { ID=Guid.NewGuid().ToString("N"), BlueprintName="SkillRollbackTrinket" };
            item.SetTag("Item"); item.AddPart(new RenderPart { DisplayName="test charm" });
            item.AddPart(new PhysicsPart { Takeable=true, Weight=1 });
            item.AddPart(new EquippablePart { Slot="Trinket", EquipSkillBonuses=skills });
            Assert.True(actor.GetPart<InventoryPart>().AddObject(item));
            return item;
        }
        ActivatedAbility BindVault(Entity item)
        {
            Assert.True(InventorySystem.Equip(actor,item));
            var skill=actor.GetPart<SkillsPart>().GetSkill("Acrobatics_Vault");
            var abilities=actor.GetPart<ActivatedAbilitiesPart>();
            abilities.AssignAbilityToSlot(skill.ActivatedAbilityID,7);
            var ability=abilities.GetAbility(skill.ActivatedAbilityID); ability.CooldownRemaining=9;
            return ability;
        }
        void AssertRestored(Entity item,Guid abilityId)
        {
            Assert.True(InventorySystem.IsEquipped(actor,item));
            Assert.False(actor.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.AreSame(actor,item.GetPart<PhysicsPart>().Equipped);
            var skills=actor.GetPart<SkillsPart>();
            Assert.AreEqual(0,skills.GetLearnedRank("Acrobatics_Vault"));
            Assert.AreEqual(1,skills.GetRank("Acrobatics_Vault"));
            Assert.AreEqual(abilityId,skills.GetSkill("Acrobatics_Vault").ActivatedAbilityID);
            var abilities=actor.GetPart<ActivatedAbilitiesPart>();
            Assert.AreEqual(abilityId,abilities.GetAbilityBySlot(7)?.ID);
            Assert.AreEqual(9,abilities.GetAbility(abilityId)?.CooldownRemaining);
            Assert.AreEqual(1,abilities.AbilityList.Count(a=>a.SourcePowerClass=="Acrobatics_Vault"));
        }

        [Test] public void ThrowingAfterUnequipRestoresActiveIdentityBindingCooldownAndReceipt()
        {
            var item=Trinket("Acrobatics_Vault:1"); var ability=BindVault(item);
            string receipt=item.GetPart<EquippablePart>().AppliedEquipSkillBonuses;
            actor.AddPart(new TrinketSkillThrowProbe { EventId="AfterUnequip" });
            Assert.False(InventorySystem.UnequipItem(actor,item));
            AssertRestored(item,ability.ID);
            Assert.AreEqual(receipt,item.GetPart<EquippablePart>().AppliedEquipSkillBonuses);
        }

        [TestCase(false)] [TestCase(true)]
        public void FailedOuterActionRestoresActiveIdentityAndPassiveContributionExactlyOnce(bool sharedTransaction)
        {
            int dv=actor.GetStatValue("DV");
            var item=Trinket("Acrobatics_Vault:1,AcrobaticsDodgePower:1"); var ability=BindVault(item);
            Assert.AreEqual(dv+2,actor.GetStatValue("DV"));
            item.AddPart(new TrinketSkillNestedUnequipProbe { SharedTransaction=sharedTransaction });
            actor.AddPart(new TrinketSkillThrowProbe { EventId="AfterInventoryAction" });
            Assert.False(InventorySystem.PerformAction(actor,item,"TestUnequip"));
            AssertRestored(item,ability.ID);
            Assert.AreEqual(dv+2,actor.GetStatValue("DV"));
            Assert.AreEqual(1,actor.GetPart<SkillsPart>().GetRank("AcrobaticsDodgePower"));
            actor.RemovePart(actor.GetPart<TrinketSkillThrowProbe>());
            Assert.True(InventorySystem.UnequipItem(actor,item));
            Assert.AreEqual(dv,actor.GetStatValue("DV"));
            Assert.IsNull(actor.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(7));
        }

        [Test] public void FailedReplacementRestoresDisplacedActiveAndRemovesCandidateSkill()
        {
            var original=Trinket("Acrobatics_Vault:1"); var ability=BindVault(original);
            var slot=actor.GetPart<Body>().GetPartsByType("Trinket").Single(p=>p._Equipped==original);
            var candidate=Trinket("Acrobatics_Tumble:1");
            actor.AddPart(new TrinketSkillThrowProbe { EventId="AfterEquip" });
            Assert.False(InventorySystem.Equip(actor,candidate,slot));
            AssertRestored(original,ability.ID);
            Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(candidate));
            Assert.False(actor.GetPart<SkillsPart>().HasSkill("Acrobatics_Tumble"));
            Assert.AreEqual("",candidate.GetPart<EquippablePart>().AppliedEquipSkillBonuses);
        }
    }

    public sealed class TrinketSkillThrowProbe : Part
    {
        public string EventId;
        public override bool HandleEvent(GameEvent e)
        {
            if(e.ID==EventId) throw new InvalidOperationException("trinket skill rollback probe");
            return true;
        }
    }
    public sealed class TrinketSkillNestedUnequipProbe : Part
    {
        public bool SharedTransaction;
        public override bool HandleEvent(GameEvent e)
        {
            if(e.ID!="InventoryAction" || e.GetStringParameter("Command")!="TestUnequip") return true;
            var wearer=e.GetParameter<Entity>("Actor");
            bool removed=SharedTransaction
                ? new UnequipCommand(ParentEntity).Execute(new InventoryContext(wearer),e.GetParameter<InventoryTransaction>("InventoryTransaction")).Success
                : InventorySystem.UnequipItem(wearer,ParentEntity);
            if(!removed) return true;
            e.Handled=true; return false;
        }
    }
}
