using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;
using F = CavesOfOoo.Tests.MultiCellAbilityConsumerTests;

namespace CavesOfOoo.Tests
{
    internal sealed class SkillTargetReliabilityFixture
    {
        public static readonly string[] Names = { "Cryomancy_Frostbind", "Pyromancy_Pyroclasm",
            "Acrobatics_Tumble", "Persuasion_Recruit", "Persuasion_Dismiss" };
        internal readonly Zone Zone = new Zone("selected-utility");
        internal readonly Entity Actor, East, North;
        internal readonly BaseSkillPart Skill;
        internal readonly DirectedMeleeFixture.CountingRandom Rng = new DirectedMeleeFixture.CountingRandom();
        internal ActivatedAbility Ability => Actor.GetPart<ActivatedAbilitiesPart>().GetAbility(Skill.ActivatedAbilityID);

        internal SkillTargetReliabilityFixture(string name)
        {
            Actor = Creature(); East = Creature(); North = Creature();
            Actor.AddPart(new SkillsPart()); Actor.AddPart(new ActivatedAbilitiesPart());
            Actor.Statistics["Ego"] = new Stat { Owner = Actor, Name = "Ego", BaseValue = 50, Min = 0, Max = 100 };
            Actor.Statistics["Level"] = new Stat { Owner = Actor, Name = "Level", BaseValue = 10, Min = 0, Max = 100 };
            Skill = (BaseSkillPart)Activator.CreateInstance(typeof(BaseSkillPart).Assembly.GetType("CavesOfOoo.Skills." + name, true));
            Assert.True(Actor.GetPart<SkillsPart>().AddSkill(Skill, "selected-utility-test"));
            F.Place(Zone, Actor, 10, 10); F.Place(Zone, East, 11, 10); F.Place(Zone, North, 10, 9);
            foreach (var target in new[] { East, North })
            {
                if (name == "Pyromancy_Pyroclasm") Ignite(target);
                if (name == "Acrobatics_Tumble") target.GetPart<BrainPart>().PersonalEnemies.Add(Actor);
                // Two eligible followers deliberately isolate selection from the
                // current one-slot acquisition policy; no recruitment roll is faked.
                if (name == "Persuasion_Dismiss") Assert.True(target.ApplyEffect(new RecruitedEffect(Actor), Actor, Zone));
            }
        }
        internal static Entity Creature()
        { var e = F.Owner(creature: true); e.AddPart(new BrainPart()); return e; }
        internal void Ignite(Entity e)
        {
            Assert.True(e.ApplyEffect(new BurningEffect(rng: new Random(7)), Actor, Zone));
            e.GetEffect<BurningEffect>().Duration = 4;
        }
        internal bool Cast(Cell selected, bool includeTarget = true, int dx = 1, int dy = 0)
        {
            var e = GameEvent.New(Ability.Command);
            e.SetParameter("Zone", (object)Zone); e.SetParameter("RNG", (object)Rng);
            e.SetParameter("SourceCell", (object)Zone.GetEntityCell(Actor));
            e.SetParameter("DirectionX", dx); e.SetParameter("DirectionY", dy); e.SetParameter("Range", Ability.Range);
            if (includeTarget) e.SetParameter("TargetCell", (object)selected);
            try { Actor.FireEvent(e); return e.Handled; } finally { e.Release(); }
        }
        internal void AssertChosen(Entity chosen, Entity other, (int x, int y) originalChosen)
        {
            switch (Skill.Name)
            {
                case "Cryomancy_Frostbind": Assert.True(chosen.HasEffect<RootedEffect>()); Assert.False(other.HasEffect<RootedEffect>()); break;
                case "Pyromancy_Pyroclasm":
                    Assert.False(chosen.HasEffect<BurningEffect>()); Assert.True(other.HasEffect<BurningEffect>());
                    Assert.Less(chosen.GetStatValue("Hitpoints"), 1000); break;
                case "Acrobatics_Tumble":
                    Assert.AreEqual(originalChosen, Zone.GetEntityPosition(Actor));
                    Assert.AreEqual((10, 10), Zone.GetEntityPosition(chosen));
                    Assert.True(chosen.HasEffect<ConfusedEffect>()); Assert.False(other.HasEffect<ConfusedEffect>()); break;
                case "Persuasion_Recruit":
                    Assert.AreSame(Actor, chosen.GetPart<BrainPart>().PartyLeader);
                    Assert.True(chosen.HasEffect<RecruitedEffect>()); Assert.Null(other.GetPart<BrainPart>().PartyLeader); break;
                case "Persuasion_Dismiss":
                    Assert.Null(chosen.GetPart<BrainPart>().PartyLeader); Assert.False(chosen.HasEffect<RecruitedEffect>());
                    Assert.AreSame(Actor, other.GetPart<BrainPart>().PartyLeader); Assert.True(other.HasEffect<RecruitedEffect>()); break;
            }
            Assert.AreEqual(Ability.MaxCooldown, Ability.CooldownRemaining);
        }
        internal string Snapshot()
            => string.Join("|", new[] { Actor, East, North }.Select(e => e.ID + ":" + Zone.GetEntityPosition(e)
                + ":" + e.GetStatValue("Hitpoints") + ":" + e.GetPart<BrainPart>().PartyLeader?.ID
                + ":" + string.Join(",", e.GetPart<StatusEffectsPart>().GetAllEffects().Select(x => x.GetType().Name + "/" + x.Duration))));
        internal void AssertFreeRefusal(Cell selected)
        {
            string before = Snapshot(); int rolls = Rng.Calls;
            Assert.False(Cast(selected), "Invalid explicit target must not choose another neighbor.");
            Assert.AreEqual(before, Snapshot()); Assert.AreEqual(rolls, Rng.Calls);
            Assert.Zero(Ability.CooldownRemaining, "Unhandled command leaves native action and cooldown unpaid.");
        }
    }

    public sealed class SkillTargetReliabilityTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); Diag.ResetAll(); }
        static IEnumerable<TestCaseData> TwoNeighbors()
        { foreach (string name in SkillTargetReliabilityFixture.Names) foreach (bool east in new[] { true, false }) yield return new TestCaseData(name, east); }

        [TestCaseSource(nameof(TwoNeighbors))]
        public void ActualCommandUsesChosenCellWithTwoEligibleNeighbors(string name, bool east)
        {
            var f = new SkillTargetReliabilityFixture(name);
            Assert.True(f.Cast(f.Zone.GetCell(east ? 11 : 10, east ? 10 : 9), dx: east ? 0 : 1, dy: east ? -1 : 0),
                "TargetCell is authoritative even if direction fields disagree.");
            f.AssertChosen(east ? f.East : f.North, east ? f.North : f.East, east ? (11, 10) : (10, 9));
        }

        [TestCaseSource(typeof(SkillTargetReliabilityFixture), nameof(SkillTargetReliabilityFixture.Names))]
        public void EmptySelectedCellIsAFreeRefusal(string name)
        { var f = new SkillTargetReliabilityFixture(name); f.AssertFreeRefusal(f.Zone.GetCell(9, 10)); }

        [TestCaseSource(typeof(SkillTargetReliabilityFixture), nameof(SkillTargetReliabilityFixture.Names))]
        public void MissingTargetKeepsLegacyFirstNeighbor(string name)
        {
            var f = new SkillTargetReliabilityFixture(name);
            Assert.True(f.Cast(null, includeTarget: false)); f.AssertChosen(f.North, f.East, (10, 9));
        }

        [TestCaseSource(typeof(SkillTargetReliabilityFixture), nameof(SkillTargetReliabilityFixture.Names))]
        public void SelectedSecondaryBodyCellWorksWithDistantAnchor(string name)
        {
            var f = new SkillTargetReliabilityFixture(name);
            Assert.True(f.Zone.RemoveEntity(f.East)); f.East.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;-1,0" });
            F.Place(f.Zone, f.East, 12, 10);
            Assert.True(f.Cast(f.Zone.GetCell(11, 10))); f.AssertChosen(f.East, f.North, (12, 10));
        }

        [TestCaseSource(typeof(SkillTargetReliabilityFixture), nameof(SkillTargetReliabilityFixture.Names))]
        public void ActorSecondaryBodyEdgeReachesChosenCell(string name)
        {
            var f = new SkillTargetReliabilityFixture(name);
            Assert.True(f.Zone.RemoveEntity(f.Actor)); f.Actor.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0;2,0" });
            F.Place(f.Zone, f.Actor, 8, 10);
            Assert.True(f.Cast(f.Zone.GetCell(11, 10)));
            if (name == "Acrobatics_Tumble")
            {
                Assert.AreEqual((11, 10), f.Zone.GetEntityPosition(f.Actor));
                Assert.AreEqual((8, 10), f.Zone.GetEntityPosition(f.East));
                Assert.True(f.East.HasEffect<ConfusedEffect>()); Assert.False(f.North.HasEffect<ConfusedEffect>());
            }
            else f.AssertChosen(f.East, f.North, (11, 10));
        }

        [TestCase("neutral", false)] [TestCase("recruited", false)]
        [TestCase("hostile", true)] [TestCase("recruited-grudge", true)]
        public void TumbleConfusionUsesActualRelationship(string relation, bool confused)
        {
            var f = new SkillTargetReliabilityFixture("Acrobatics_Tumble");
            f.East.GetPart<BrainPart>().PersonalEnemies.Clear();
            if (relation.StartsWith("recruited")) Assert.True(f.East.ApplyEffect(new RecruitedEffect(f.Actor), f.Actor, f.Zone));
            if (relation == "hostile" || relation == "recruited-grudge") f.East.GetPart<BrainPart>().PersonalEnemies.Add(f.Actor);
            Assert.False(f.East.HasTag("Ally"));
            Assert.AreEqual(confused, FactionManager.IsHostile(f.East, f.Actor), "Fixture uses the real faction/party/grudge rules.");
            Assert.True(f.Cast(f.Zone.GetCell(11, 10)));
            Assert.AreEqual((11, 10), f.Zone.GetEntityPosition(f.Actor));
            Assert.AreEqual(confused, f.East.HasEffect<ConfusedEffect>());
            Assert.False(f.North.HasEffect<ConfusedEffect>());
        }

        [Test]
        public void PyroclasmDoesNotRetargetBurningNeighborWhenChosenCreatureIsUnburning()
        {
            var f = new SkillTargetReliabilityFixture("Pyromancy_Pyroclasm");
            f.East.RemoveEffect<BurningEffect>(); f.AssertFreeRefusal(f.Zone.GetCell(11, 10));
        }

        [TestCase(true)] [TestCase(false)]
        public void PyroclasmPreservesBurningObjectAndPhysicalContactBlast(bool wide)
        {
            var f = new SkillTargetReliabilityFixture("Pyromancy_Pyroclasm");
            Assert.True(f.Zone.RemoveEntity(f.East));
            var timber = F.Owner(wide ? "0,0;-1,0;-2,0" : null);
            timber.Tags["Flammable"] = "";
            timber.AddPart(new DestructiblePart { HP = 100, MaxHP = 100 });
            F.Place(f.Zone, timber, wide ? 13 : 11, 10); f.Ignite(timber);
            var inside = SkillTargetReliabilityFixture.Creature(); F.Place(f.Zone, inside, 12, 11);
            var outside = SkillTargetReliabilityFixture.Creature(); F.Place(f.Zone, outside, 14, 10);
            Assert.True(f.Cast(f.Zone.GetCell(11, 10)));
            Assert.False(timber.HasEffect<BurningEffect>()); Assert.True(f.North.HasEffect<BurningEffect>());
            Assert.AreEqual(88, timber.GetPart<DestructiblePart>().HP, "One pulse per physical owner, including structural HP.");
            Assert.AreEqual(988, inside.GetStatValue("Hitpoints"));
            Assert.AreEqual(1000, outside.GetStatValue("Hitpoints"), "Blast is centered on chosen contact, not remote anchor.");
        }

        [TestCase("Cryomancy_Frostbind")] [TestCase("Pyromancy_Pyroclasm")]
        [TestCase("Acrobatics_Tumble")] [TestCase("Persuasion_Recruit")]
        public void SuccessfulCastRetainsCooldownRefusal(string name)
        {
            var f = new SkillTargetReliabilityFixture(name); Assert.True(f.Cast(f.Zone.GetCell(11, 10)));
            string state = f.Snapshot(); int rolls = f.Rng.Calls;
            Assert.False(f.Cast(f.Zone.GetCell(10, 9))); Assert.AreEqual(state, f.Snapshot()); Assert.AreEqual(rolls, f.Rng.Calls);
        }
    }
}
