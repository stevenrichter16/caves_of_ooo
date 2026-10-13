using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>One movement input cannot pay again after its scheduled NPC response.</summary>
    public sealed class ComposableMovementPaymentTests : ShortcutFixture
    {
        sealed class AttackProbe : Part
        {
            public int Attempts;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "BeforeMeleeAttack") Attempts++;
                return true;
            }
        }

        sealed class ScheduledStun : Part
        {
            public Entity Victim;
            public Zone Zone;
            public bool Enabled, Applied, AppliedOnNpcTurn;
            public int Turns;
            public StunnedEffect Stun;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "TakeTurn") return true;
                Turns++;
                if (!Enabled || Turns != 1) return true;
                AppliedOnNpcTurn = TurnManager.Active.CurrentActor == ParentEntity;
                Stun = new StunnedEffect(3, saveTarget: 0, rng: new Random(64));
                Applied = Victim.ApplyEffect(Stun, ParentEntity, Zone);
                return true;
            }
        }

        InputHandler Prepare(bool stun, out ScheduledStun response)
        {
            Player.GetStat("Speed").BaseValue = 100;
            var witness = new Entity { ID = "scheduled-stun-witness", BlueprintName = "ScheduledStunWitness" };
            response = new ScheduledStun { Victim = Player, Zone = Zone, Enabled = stun };
            witness.AddPart(response);
            Assert.True(Zone.AddEntity(witness, 14, 10));
            var turns = new TurnManager();
            turns.RestoreSavedState(17, true, Player, new List<TurnManager.SavedTurnEntry>
            {
                new TurnManager.SavedTurnEntry { Entity = Player, Energy = TurnManager.ActionThreshold },
                new TurnManager.SavedTurnEntry { Entity = witness, Energy = TurnManager.ActionThreshold }
            });
            var input = Component<InputHandler>();
            input.PlayerEntity = Player; input.CurrentZone = Zone; input.TurnManager = turns;
            Set(input, "_combatRng", new ComposableBladeTests.Rolls());
            Diag.SetChannel("turn", true);
            return input;
        }

        [TestCase("melee", false)] [TestCase("melee", true)]
        [TestCase("structure", false)] [TestCase("structure", true)]
        [TestCase("move", false)] [TestCase("move", true)]
        public void PaidMovementBranchCannotSpendAgainWhenScheduledNpcStunsPlayer(string branch, bool stun)
        {
            var probe = new AttackProbe(); Player.AddPart(probe);
            Entity target = null;
            if (branch == "melee")
            {
                target = Item("Creature"); target.GetPart<PhysicsPart>().Solid = true;
                target.GetStat("Hitpoints").BaseValue = target.GetStat("Hitpoints").Max = 1000;
                target.GetPart<BrainPart>().SetPersonallyHostile(Player, false);
                Assert.True(FactionManager.IsHostile(Player, target));
                Assert.True(Zone.AddEntity(target, 11, 10));
            }
            else if (branch == "structure")
            {
                target = new Entity { ID = "breakable-obstruction", BlueprintName = "BreakableObstruction" };
                target.AddPart(new PhysicsPart { Solid = true });
                target.AddPart(new DestructiblePart { HP = 1000, MaxHP = 1000, Hardness = 0 });
                Assert.True(Zone.AddEntity(target, 11, 10));
            }
            var input = Prepare(stun, out var response);
            Assert.False(Player.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true, "The chosen action starts legally.");
            int tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player);

            Call(input, "ExecuteMovementStep", 1, 0);

            Assert.AreEqual(branch == "move" ? (11, 10) : (10, 10), Zone.GetEntityPosition(Player));
            Assert.AreEqual(branch == "melee" ? 1 : 0, probe.Attempts, "Actual hostile bump entered melee exactly once.");
            if (branch == "structure") Assert.Less(target.GetPart<DestructiblePart>().HP, 1000, "Actual bump damaged the structure.");
            Assert.AreEqual(stun, response.Applied);
            if (stun)
            {
                Assert.True(response.AppliedOnNpcTurn, "Stun arrived inside the real NPC scheduling window.");
                Assert.AreSame(response.Stun, Player.GetEffect<StunnedEffect>());
                Assert.AreEqual(2, response.Stun.Duration, "Only the scheduler's one blocked Begin/End may thaw the new stun.");
            }
            int completions = stun ? 2 : 1;
            Assert.AreEqual(completions, response.Turns, "No additional NPC round from a second input payment.");
            Assert.AreEqual(completions * 10, input.TurnManager.TickCount - tick);
            Assert.AreEqual(energy - completions * TurnManager.ActionThreshold
                + (input.TurnManager.TickCount - tick) * 100, input.TurnManager.GetEnergy(Player));
            var boundaries = Diag.Snapshot(Diag.BufferCapacity)
                .Where(e => e.Category == "turn" && e.ActorId == Player.ID && (e.Kind == "Begin" || e.Kind == "End"))
                .Select(e => e.Kind).ToArray();
            CollectionAssert.AreEqual(stun ? new[] { "End", "Begin", "End" } : new[] { "End", "Begin" }, boundaries,
                "After the original action and a genuine blocked turn, no unbegun End is allowed.");
            Assert.True(input.TurnManager.WaitingForInput);
            Assert.AreSame(Player, input.TurnManager.CurrentActor);
        }

        [Test]
        public void UnbreakableObstructionWithoutStatusRemainsFree()
        {
            var wall = new Entity { ID = "unbreakable-obstruction" };
            wall.AddPart(new PhysicsPart { Solid = true }); Assert.True(Zone.AddEntity(wall, 11, 10));
            var input = Prepare(true, out var response);
            int tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player);
            Call(input, "ExecuteMovementStep", 1, 0);
            Assert.AreEqual((10, 10), Zone.GetEntityPosition(Player));
            Assert.AreEqual(tick, input.TurnManager.TickCount); Assert.AreEqual(energy, input.TurnManager.GetEnergy(Player));
            Assert.AreEqual(0, response.Turns); Assert.False(response.Applied);
        }

        [Test]
        public void AlreadyBlockedMovementStillPaysItsThawTurn()
        {
            var input = Prepare(false, out var response);
            var stun = new StunnedEffect(1, saveTarget: 0, rng: new Random(64));
            Assert.True(Player.ApplyEffect(stun, Player, Zone));
            stun.JustApplied = false; // Represents the existing effect at the yielded input boundary.
            int tick = input.TurnManager.TickCount;
            Call(input, "ExecuteMovementStep", 1, 0);
            Assert.AreEqual((10, 10), Zone.GetEntityPosition(Player));
            Assert.False(Player.HasEffect<StunnedEffect>(), "Refused movement must retain the existing paid thaw behavior.");
            Assert.AreEqual(1, response.Turns); Assert.AreEqual(10, input.TurnManager.TickCount - tick);
            Assert.AreEqual(TurnManager.ActionThreshold, input.TurnManager.GetEnergy(Player));
        }
    }
}
