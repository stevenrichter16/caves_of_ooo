using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;
using F = CavesOfOoo.Tests.MultiCellAbilityConsumerTests;

namespace CavesOfOoo.Tests
{
    public sealed class ChargingStrikePaymentTests
    {
        sealed class Fixture
        {
            internal readonly Zone Zone = new Zone("Overworld.10.10.0");
            internal readonly Entity Actor = F.ArmedActor("Bludgeoning Cudgel");
            internal readonly Cudgel_ChargingStrike Skill = new Cudgel_ChargingStrike();
            internal ActivatedAbility Ability => Actor.GetPart<ActivatedAbilitiesPart>().GetAbility(Skill.ActivatedAbilityID);
            internal Fixture()
            {
                Actor.Tags["Player"] = ""; Actor.AddPart(new SkillsPart()); Actor.AddPart(new ActivatedAbilitiesPart());
                Assert.True(Actor.GetPart<SkillsPart>().AddSkill(Skill, "charge-payment-test")); F.Place(Zone, Actor, 10, 10);
            }
            internal void Wall(int x)
            { var wall = new Entity(); wall.Tags["Solid"] = ""; F.Place(Zone, wall, x, 10); }
            internal bool Cast(int dx = 1)
            {
                var e = GameEvent.New("CommandChargingStrike"); e.SetParameter("Zone", (object)Zone);
                e.SetParameter("RNG", (object)new Random(17)); e.SetParameter("DirectionX", dx);
                e.SetParameter("SourceCell", (object)Zone.GetEntityCell(Actor));
                try { Actor.FireEvent(e); return e.Handled; } finally { e.Release(); }
            }
        }

        [TestCase(0, 13, true)] [TestCase(12, 11, true)] [TestCase(11, 10, false)]
        public void EmptyLanePaysOnlyWhenMovementCommitted(int wallX, int finalX, bool paid)
        {
            var f = new Fixture(); if (wallX != 0) f.Wall(wallX);
            bool handled = f.Cast();
            Assert.AreEqual((finalX, 10), f.Zone.GetEntityPosition(f.Actor), "The existing movement role is preserved.");
            Assert.AreEqual(paid, handled, "Committed travel is a real command; blocked zero-travel is a refusal.");
            Assert.AreEqual(paid ? Cudgel_ChargingStrike.COOLDOWN : 0, f.Ability.CooldownRemaining);
        }

        [Test]
        public void AdjacentAttackPaysDespiteNoMovement()
        {
            var f = new Fixture(); var target = F.Owner(creature: true); F.Place(f.Zone, target, 11, 10);
            Assert.True(f.Cast()); Assert.AreEqual((10, 10), f.Zone.GetEntityPosition(f.Actor));
            Assert.Less(target.GetStatValue("Hitpoints"), 1000); Assert.AreEqual(Cudgel_ChargingStrike.COOLDOWN, f.Ability.CooldownRemaining);
        }

        [Test]
        public void ZeroDirectionIsAFreeNoOp()
        {
            var f = new Fixture(); Assert.False(f.Cast(0)); Assert.Zero(f.Ability.CooldownRemaining);
            Assert.AreEqual((10, 10), f.Zone.GetEntityPosition(f.Actor));
        }

        [Test]
        public void RealSpikeTrapStillTriggersAndConsumedTravelPays()
        {
            var f = new Fixture(); var trap = F.Owner(); trap.AddPart(new SpikeTrapTriggerPart { Damage = 12 });
            F.Place(f.Zone, trap, 11, 10);
            bool handled = f.Cast();
            Assert.Null(f.Zone.GetEntityCell(trap), "Ordinary movement consumes the actual one-shot trigger.");
            Assert.AreEqual(988, f.Actor.GetStatValue("Hitpoints"));
            Assert.AreEqual((13, 10), f.Zone.GetEntityPosition(f.Actor));
            Assert.True(handled); Assert.AreEqual(Cudgel_ChargingStrike.COOLDOWN, f.Ability.CooldownRemaining);
        }

        [TestCase("return")] [TestCase("remove")] [TestCase("relocate")]
        public void EntryReactionCannotRefundAlreadyCommittedMovement(string reaction)
        {
            var f = new Fixture(); var surface = F.Owner(); F.Place(f.Zone, surface, 11, 10);
            surface.GetPart<MultiCellAbilityProbePart>().OnEntry = () =>
            {
                if (reaction == "remove") Assert.True(f.Zone.RemoveEntity(f.Actor));
                else Assert.True(f.Zone.MoveEntity(f.Actor, reaction == "return" ? 10 : 20, 10));
            };
            Assert.True(f.Cast());
            Assert.AreEqual(1, surface.GetPart<MultiCellAbilityProbePart>().EntryCalls);
            if (reaction == "remove") Assert.Null(f.Zone.GetEntityCell(f.Actor));
            else Assert.AreEqual((reaction == "return" ? 10 : 20, 10), f.Zone.GetEntityPosition(f.Actor));
            Assert.AreEqual(Cudgel_ChargingStrike.COOLDOWN, f.Ability.CooldownRemaining);
        }

        [Test]
        public void LethalSpikeEntryIsStillACommittedCommand()
        {
            var f = new Fixture(); var trap = F.Owner(); trap.AddPart(new SpikeTrapTriggerPart { Damage = 2000 });
            F.Place(f.Zone, trap, 11, 10);
            bool handled = f.Cast();
            Assert.LessOrEqual(f.Actor.GetStatValue("Hitpoints"), 0); Assert.Null(f.Zone.GetEntityCell(trap));
            Assert.True(handled, "Death after entering terrain does not turn the committed move into a free refusal.");
            Assert.AreEqual(Cudgel_ChargingStrike.COOLDOWN, f.Ability.CooldownRemaining);
        }

        [TestCase(0, 10)] [TestCase(12, 10)] [TestCase(11, 0)]
        public void ActualInputCompletionChargesSchedulerForCommittedTravelOnly(int wallX, int expectedTicks)
        {
            // Existing global guard and actual InputHandler completion path.
            // Controlled quiet scene; this does not claim keyboard delivery.
            using (var ui = new HotbarSaveFixture(true, false))
            {
                var f = new Fixture(); if (wallX != 0) f.Wall(wallX);
                var turns = new TurnManager(); turns.RestoreSavedState(0, true, f.Actor,
                    new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = f.Actor, Energy = 1000 } });
                TurnManager.World = null;
                ui.Input.PlayerEntity = f.Actor; ui.Input.CurrentZone = f.Zone; ui.Input.TurnManager = turns;
                var method = typeof(InputHandler).GetMethod("ResolveAbilityCommand", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                try { method.Invoke(ui.Input, new object[] { f.Ability, f.Zone.GetEntityCell(f.Actor), 1, 0, null }); }
                catch (TargetInvocationException e) { throw e.InnerException ?? e; }
                Assert.AreEqual(expectedTicks, turns.TickCount, "Speed100 pays 1000 energy, requiring ten actual ticks to act again.");
                Assert.AreEqual(1000, turns.GetEnergy(f.Actor)); Assert.True(turns.WaitingForInput);
                Assert.AreSame(f.Actor, turns.CurrentActor);
                Assert.AreEqual(expectedTicks == 0 ? 0 : Cudgel_ChargingStrike.COOLDOWN - 1, f.Ability.CooldownRemaining);
            }
        }
    }
}
