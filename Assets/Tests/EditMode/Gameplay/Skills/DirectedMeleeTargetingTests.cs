using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;
using F = CavesOfOoo.Tests.MultiCellAbilityConsumerTests;

namespace CavesOfOoo.Tests
{
    internal sealed class DirectedMeleeFixture
    {
        internal static readonly string[] SkillNames = {
            "Cudgel_Conk", "Cudgel_Slam", "Cudgel_Disarm", "Axe_RendArmor",
            "Axe_HookAndDrag", "ShortBlades_Shank", "ShortBlades_Flurry", "ShortBlades_Backstab"
        };
        internal readonly Zone Zone = new Zone("directed-melee");
        internal readonly Entity Actor, East, North;
        internal readonly BaseSkillPart Skill;
        internal readonly CountingRandom Rng = new CountingRandom();
        internal ActivatedAbility Ability => Actor.GetPart<ActivatedAbilitiesPart>().GetAbility(Skill.ActivatedAbilityID);

        internal DirectedMeleeFixture(string skill)
        {
            Actor = F.ArmedActor("Piercing Axe Cudgel");
            Actor.Tags["Player"] = "";
            Actor.AddPart(new ActivatedAbilitiesPart());
            Actor.AddPart(new SkillsPart());
            Skill = (BaseSkillPart)Activator.CreateInstance(typeof(BaseSkillPart).Assembly.GetType("CavesOfOoo.Skills." + skill, true));
            Assert.IsTrue(Actor.GetPart<SkillsPart>().AddSkill(Skill, "targeting-test"));
            East = F.ArmedActor("Piercing");
            North = F.ArmedActor("Piercing");
            F.Place(Zone, Actor, 10, 10);
            F.Place(Zone, East, 11, 10);
            F.Place(Zone, North, 10, 9);
        }

        internal bool Cast(Cell selected, bool includeTarget = true, int dx = 1, int dy = 0)
        {
            var command = GameEvent.New(Ability.Command);
            command.SetParameter("Zone", (object)Zone);
            command.SetParameter("RNG", (object)Rng);
            command.SetParameter("SourceCell", (object)Zone.GetEntityCell(Actor));
            command.SetParameter("DirectionX", dx);
            command.SetParameter("DirectionY", dy);
            command.SetParameter("Range", Ability.Range);
            if (includeTarget) command.SetParameter("TargetCell", (object)selected);
            try { Actor.FireEvent(command); return command.Handled; }
            finally { command.Release(); }
        }

        internal void AssertAffected(Entity target)
        {
            switch (Skill.Name)
            {
                case "Cudgel_Conk": case "Cudgel_Slam": Assert.IsTrue(target.HasEffect<StunnedEffect>(), Skill.Name); break;
                case "Cudgel_Disarm": Assert.IsNull(target.GetPart<InventoryPart>().GetEquippedWithPart<MeleeWeaponPart>(), Skill.Name); break;
                case "Axe_RendArmor": Assert.IsTrue(target.HasEffect<ShatterArmorEffect>(), Skill.Name); break;
                case "Axe_HookAndDrag": Assert.IsTrue(target.HasEffect<HookedEffect>(), Skill.Name); break;
                default: Assert.Less(target.GetStatValue("Hitpoints"), 1000, Skill.Name); break;
            }
            Assert.AreEqual(Ability.MaxCooldown, Ability.CooldownRemaining, "Only an actual selected-target cast spends cooldown.");
        }

        internal static void AssertUntouched(Entity target, Zone zone, int x, int y)
        {
            Assert.AreEqual(1000, target.GetStatValue("Hitpoints"), "Bystander must not take damage.");
            Assert.AreEqual(0, target.GetPart<StatusEffectsPart>().GetAllEffects().Count, "Bystander must not gain a control/debuff.");
            Assert.IsNotNull(target.GetPart<InventoryPart>().GetEquippedWithPart<MeleeWeaponPart>(), "Bystander keeps its weapon.");
            Assert.AreEqual((x, y), zone.GetEntityPosition(target), "Bystander must not move.");
            Assert.AreEqual(0, target.GetPart<MultiCellAbilityProbePart>().DamageCalls, "Bystander must not even receive a damage attempt.");
        }

        internal sealed class CountingRandom : Random
        {
            internal int Calls;
            internal CountingRandom() : base(17) { }
            public override int Next(int maxValue) { Calls++; return base.Next(maxValue); }
            public override int Next(int minValue, int maxValue) { Calls++; return base.Next(minValue, maxValue); }
        }
    }

    /// <summary>Real command dispatch must honor the cell chosen in the native
    /// direction prompt, including control, disarm, movement and damage skills.</summary>
    public sealed class DirectedMeleeTargetingTests
    {
        private static IEnumerable<TestCaseData> SelectedCases()
        {
            foreach (string skill in DirectedMeleeFixture.SkillNames)
                foreach (bool east in new[] { true, false }) yield return new TestCaseData(skill, east);
        }

        [TestCaseSource(nameof(SelectedCases))]
        public void ChosenAdjacentEnemyReceivesTheSkillAndOtherCreatureIsUntouched(string skill, bool east)
        {
            var f = new DirectedMeleeFixture(skill);
            Assert.IsTrue(f.Cast(f.Zone.GetCell(east ? 11 : 10, east ? 10 : 9)), "Selected legal cast must be handled.");
            f.AssertAffected(east ? f.East : f.North);
            DirectedMeleeFixture.AssertUntouched(east ? f.North : f.East, f.Zone, east ? 10 : 11, east ? 9 : 10);
        }

        [TestCaseSource(typeof(DirectedMeleeFixture), nameof(DirectedMeleeFixture.SkillNames))]
        public void AbsentTargetCellPreservesLegacyFirstAdjacentSelection(string skill)
        {
            var f = new DirectedMeleeFixture(skill);
            Assert.IsTrue(f.Cast(null, includeTarget: false));
            f.AssertAffected(f.North);
            DirectedMeleeFixture.AssertUntouched(f.East, f.Zone, 11, 10);
        }

        [TestCaseSource(typeof(DirectedMeleeFixture), nameof(DirectedMeleeFixture.SkillNames))]
        public void EmptyChosenCellRefusesWithoutDamagingBystandersOrSpendingActionCooldownOrRng(string skill)
        {
            var f = new DirectedMeleeFixture(skill);
            Assert.IsFalse(f.Cast(f.Zone.GetCell(9, 10)), "Unhandled refusal is the native input contract for a free failed cast.");
            Assert.AreEqual(0, f.Ability.CooldownRemaining);
            Assert.AreEqual(0, f.Rng.Calls);
            DirectedMeleeFixture.AssertUntouched(f.East, f.Zone, 11, 10);
            DirectedMeleeFixture.AssertUntouched(f.North, f.Zone, 10, 9);
        }

        [TestCaseSource(typeof(DirectedMeleeFixture), nameof(DirectedMeleeFixture.SkillNames))]
        public void SelectedPhysicalBodyCanBeAdjacentWhenItsAnchorIsDistant(string skill)
        {
            var f = new DirectedMeleeFixture(skill);
            Assert.IsTrue(f.Zone.RemoveEntity(f.East));
            f.East.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;-1,0" });
            F.Place(f.Zone, f.East, 12, 10);
            Assert.AreEqual(0, f.Zone.GetCell(11, 10).Objects.Count, "Selected cell is a real secondary body cell.");
            Assert.IsTrue(f.Cast(f.Zone.GetCell(11, 10)));
            f.AssertAffected(f.East);
            DirectedMeleeFixture.AssertUntouched(f.North, f.Zone, 10, 9);
        }

        [TestCaseSource(typeof(DirectedMeleeFixture), nameof(DirectedMeleeFixture.SkillNames))]
        public void ActorPhysicalEdgeCanReachSelectedCellBeyondAnchorAdjacency(string skill)
        {
            var f = new DirectedMeleeFixture(skill);
            Assert.IsTrue(f.Zone.RemoveEntity(f.Actor));
            f.Actor.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0;2,0" });
            F.Place(f.Zone, f.Actor, 8, 10);
            Assert.IsTrue(f.Cast(f.Zone.GetCell(11, 10)));
            f.AssertAffected(f.East);
            DirectedMeleeFixture.AssertUntouched(f.North, f.Zone, 10, 9);
        }

        [Test]
        public void SlamUsesChosenContactDirectionAndRealObstacleDamage()
        {
            var f = new DirectedMeleeFixture("Cudgel_Slam");
            var wall = new Entity(); wall.Tags["Solid"] = "";
            F.Place(f.Zone, wall, 13, 10);
            Assert.IsTrue(f.Cast(f.Zone.GetCell(11, 10)));
            Assert.AreEqual((12, 10), f.Zone.GetEntityPosition(f.East));
            Assert.Less(f.East.GetStatValue("Hitpoints"), 1000, "Chosen enemy is slammed east into its actual wall.");
            DirectedMeleeFixture.AssertUntouched(f.North, f.Zone, 10, 9);
        }

        [TestCase(true)] [TestCase(false)]
        public void BackstabComputesFlankBeyondChosenTargetInsteadOfBystander(bool flanked)
        {
            var f = new DirectedMeleeFixture("ShortBlades_Backstab");
            if (flanked) F.Place(f.Zone, F.Owner(creature: true), 12, 10);
            Assert.IsTrue(f.Cast(f.Zone.GetCell(11, 10)));
            Assert.AreEqual(flanked ? 2 : 1, f.East.GetPart<MultiCellAbilityProbePart>().DamageCalls,
                "One selected swing; the actual far-side ally permits the second bonus damage application.");
            DirectedMeleeFixture.AssertUntouched(f.North, f.Zone, 10, 9);
        }

        [TestCase(true)] [TestCase(false)]
        public void ShankAiSelectsActualEnemyContactWithoutHarmingEarlierAlly(bool wideTarget)
        {
            using (var f = new DensityCombatFixture())
            {
                var actor = f.Actor(skills: "ShortBlades_Shank");
                var dagger = f.Equip(actor, "Dagger").GetPart<MeleeWeaponPart>();
                dagger.HitBonus = 100;
                var ally = f.Actor(10, 9, "");
                var target = f.Target(wideTarget ? 12 : 11, 10);
                target.AddPart(new MultiCellAbilityProbePart());
                if (wideTarget)
                {
                    Assert.IsTrue(f.Zone.RemoveEntity(target));
                    target.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;-1,0" });
                    F.Place(f.Zone, target, 12, 10);
                }
                Assert.IsTrue(f.Cast(actor, target, new Random(17)));
                Assert.Greater(target.GetPart<MultiCellAbilityProbePart>().DamageCalls, 0,
                    "A successful Shank must actually reach the chosen target, not only charge cooldown.");
                Assert.AreEqual(500, ally.GetStatValue("Hitpoints"));
                Assert.Greater(DensityCombatFixture.Ability(actor).CooldownRemaining, 0);
                Assert.IsFalse(f.Cast(actor, target), "A successful exact-target cast retains ordinary cooldown refusal.");
            }
        }
    }
}
