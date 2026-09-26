using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Finite scheduler audit on detached arenas and ordinary factory
    /// stats. Hostility, ready energy, cooldown and one injury are explicit
    /// fixtures; this does not establish natural discovery, input, FX or balance.</summary>
    [Scenario(name: "Density Combat Scheduler Audit", category: "Combat",
        description: "Ordinary-stat spells, melee, local assistance and retreat through the real scheduler.")]
    public sealed class DensityCombatSchedulerBench : IScenario
    {
        public string RunId { get; private set; }
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();

        public void Apply(ScenarioContext ctx)
        {
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            bool oldChannel = Diag.IsChannelEnabled("scenario");
            Diag.SetChannel("scenario", true);
            try
            {
                Require(ctx != null, "scenario context");
                foreach (string name in new[] { "Player", "IceWight", "MarlbackTunnelguard", "MarlbackGleaner", "GlassScorpion" })
                    Require(ctx.Factory.Blueprints.ContainsKey(name), "missing blueprint " + name);
                using (var scope = new DetachedScope(ctx.Factory))
                {
                    Projectile(ctx.Factory, false, false);
                    Projectile(ctx.Factory, true, false);
                    Projectile(ctx.Factory, false, true);
                    Hunter(ctx.Factory, false);
                    Hunter(ctx.Factory, true);
                    Assistance(ctx.Factory);
                    HealthyPursuit(ctx.Factory);
                }
            }
            finally
            {
                Diag.Record("scenario", "DensityCombatSchedulerSummary", payload: new
                    { runId = RunId, cases = Cases, failures = Failures, complete = Cases == 7 && Failures == 0 });
                Diag.SetChannel("scenario", oldChannel);
            }
        }

        private void Projectile(EntityFactory factory, bool wall, bool cooldown)
        {
            var a = Arena(factory, "IceWight", 13);
            var ability = a.Actor.GetPart<ActivatedAbilitiesPart>().AbilityList.Single();
            if (wall)
            {
                var obstacle = new Entity(); obstacle.SetTag("Solid"); obstacle.AddPart(new PhysicsPart { Solid = true });
                a.Zone.AddEntity(obstacle, 11, 10);
                // Keep pursuit explicit even when the wall prevents acquisition.
                a.Actor.GetPart<BrainPart>().PushGoal(new KillGoal(a.Player));
            }
            if (cooldown) ability.CooldownRemaining = 5;
            int hp = a.Player.GetStatValue("Hitpoints");
            bool schedule = RunOne(a);
            bool result = wall ? ability.CooldownRemaining == 0 && a.Player.GetStatValue("Hitpoints") == hp
                : cooldown ? ability.CooldownRemaining == 4 && a.Player.GetStatValue("Hitpoints") == hp
                    && a.Zone.GetEntityPosition(a.Actor) != (10, 10)
                : ability.CooldownRemaining == ability.MaxCooldown - 1 && a.Player.GetStatValue("Hitpoints") < hp;
            Measure(wall ? "wall_control" : cooldown ? "cooldown_movement_control" : "ice_lance", a, schedule && result);
        }

        private void Hunter(EntityFactory factory, bool wounded)
        {
            var a = Arena(factory, "MarlbackTunnelguard", 11);
            var ability = a.Actor.GetPart<ActivatedAbilitiesPart>().AbilityList.Single();
            if (wounded)
                CombatSystem.ApplyDamage(a.Actor, a.Actor.GetStatValue("Hitpoints") - 5, null, a.Zone);
            int playerHp = a.Player.GetStatValue("Hitpoints");
            bool schedule = RunOne(a);
            bool result = wounded ? SpatialQuery.Distance(a.Zone, a.Actor, a.Player) > 1
                    && ability.CooldownRemaining == 0 && a.Player.GetStatValue("Hitpoints") == playerHp
                : ability.CooldownRemaining == ability.MaxCooldown - 1;
            Measure(wounded ? "injured_retreat" : "equipped_hunter_shank", a, schedule && result);
        }

        private void HealthyPursuit(EntityFactory factory)
        {
            var a = Arena(factory, "MarlbackTunnelguard", 13);
            bool schedule = RunOne(a);
            Measure("healthy_pursuit_control", a, schedule && SpatialQuery.Distance(a.Zone, a.Actor, a.Player) < 3
                && a.Actor.GetStatValue("Hitpoints") == a.InitialHp);
        }

        private void Assistance(EntityFactory factory)
        {
            var a = Arena(factory, "MarlbackTunnelguard", 13);
            var ally = factory.CreateEntity("MarlbackGleaner");
            var neutral = factory.CreateEntity("GlassScorpion");
            a.Zone.AddEntity(ally, 10, 12); a.Zone.AddEntity(neutral, 9, 11);
            ally.GetPart<BrainPart>().CurrentZone = a.Zone;
            neutral.GetPart<BrainPart>().CurrentZone = a.Zone;
            // Acquisition alerts are the subject; neither receiver receives a turn here.
            a.Actor.GetPart<BrainPart>().Target = null;
            bool schedule = RunOne(a);
            Measure("local_assist_neutral_control", a, schedule
                && ally.GetPart<BrainPart>().IsPersonallyHostileTo(a.Player)
                && !neutral.GetPart<BrainPart>().IsPersonallyHostileTo(a.Player));
        }

        private ArenaState Arena(EntityFactory factory, string blueprint, int playerX)
        {
            var a = new ArenaState { Zone = new Zone("density-scheduler-" + RunId + "-" + Cases) };
            a.Actor = factory.CreateEntity(blueprint); a.Player = factory.CreateEntity("Player");
            Require(a.Actor != null && a.Player != null, "factory actors");
            a.Actor.ID = Guid.NewGuid().ToString("N"); a.Player.ID = Guid.NewGuid().ToString("N");
            a.Zone.AddEntity(a.Actor, 10, 10); a.Zone.AddEntity(a.Player, playerX, 10);
            a.InitialHp = a.Actor.GetStatValue("Hitpoints"); a.MaxHp = a.Actor.GetStat("Hitpoints").Max;
            a.Strength = a.Actor.GetStatValue("Strength"); a.Agility = a.Actor.GetStatValue("Agility");
            Require(!DebugInvincibility.IsEnabled(a.Player) && a.InitialHp > 0 && a.InitialHp == a.MaxHp,
                "ordinary full-health actors without invincibility");
            var brain = a.Actor.GetPart<BrainPart>(); Require(brain != null, "real brain");
            brain.CurrentZone = a.Zone; brain.Rng = new Random(1);
            brain.SetPersonallyHostile(a.Player, alertAllies: false);
            a.Observer = new TurnObserver(); a.Actor.AddPart(a.Observer);
            a.PlayerObserver = new TurnObserver(); a.Player.AddPart(a.PlayerObserver);
            return a;
        }

        private bool RunOne(ArenaState a)
        {
            var turns = new TurnManager();
            MessageLog.TickProvider = () => turns.TickCount;
            turns.RestoreSavedState(0, false, null, new List<TurnManager.SavedTurnEntry>
            {
                new TurnManager.SavedTurnEntry { Entity = a.Actor, Energy = 1100 },
                new TurnManager.SavedTurnEntry { Entity = a.Player, Energy = 1000 }
            });
            var next = turns.ProcessUntilPlayerTurn();
            a.ElapsedTicks = turns.TickCount; a.RemainingEnergy = turns.GetEnergy(a.Actor);
            // Ice Lance can freeze the ordinary player. The real scheduler then
            // spends one blocked player turn and advances to the next input yield.
            return next == a.Player && turns.WaitingForInput && turns.TickCount <= 10
                && turns.GetEnergy(a.Actor) == 100 + turns.TickCount * a.Actor.GetStatValue("Speed", 100)
                && turns.GetEnergy(a.Player) == 1000 + turns.TickCount * a.Player.GetStatValue("Speed", 100)
                    - 1000 * a.PlayerObserver.Ends && a.PlayerObserver.Ends <= 1
                && a.Observer.Actions == 1 && a.Observer.Ends == 1
                && a.Actor.GetStat("Hitpoints").Max == a.MaxHp
                && a.Actor.GetStatValue("Strength") == a.Strength && a.Actor.GetStatValue("Agility") == a.Agility;
        }

        private void Measure(string name, ArenaState a, bool passed)
        {
            Cases++; if (!passed) Failures++;
            Audit.Add((passed ? "PASS " : "FAIL ") + name + " blueprint=" + a.Actor.BlueprintName
                + " hp=" + a.Actor.GetStatValue("Hitpoints") + "/" + a.MaxHp
                + " strength=" + a.Strength + " agility=" + a.Agility + " scheduledActions=" + a.Observer.Actions + " ticks=" + a.ElapsedTicks + " remainingEnergy=" + a.RemainingEnergy);
            Diag.Record("scenario", "DensityCombatSchedulerCase", actor: a.Actor, target: a.Player,
                payload: new { runId = RunId, name, passed, hp = a.Actor.GetStatValue("Hitpoints"),
                    maxHp = a.MaxHp, strength = a.Strength, agility = a.Agility, actions = a.Observer.Actions, endTurns = a.Observer.Ends, elapsedTicks = a.ElapsedTicks, remainingEnergy = a.RemainingEnergy, playerBlockedTurns = a.PlayerObserver.Ends });
        }
        private void Require(bool condition, string reason)
        {
            if (condition) return;
            Failures++;
            Diag.Record("scenario", "DensityCombatSchedulerSkipped", payload: new { runId = RunId, reason, passed = false });
            throw new InvalidOperationException("Density combat audit precondition failed: " + reason);
        }
        private sealed class ArenaState
        {
            public Zone Zone; public Entity Actor, Player; public TurnObserver Observer, PlayerObserver;
            public int InitialHp, MaxHp, Strength, Agility, ElapsedTicks, RemainingEnergy;
        }
        private sealed class TurnObserver : Part
        {
            public int Actions, Ends;
            public override bool HandleEvent(GameEvent e)
            { if (e.ID == "TakeTurn") Actions++; if (e.ID == "EndTurn") Ends++; return true; }
        }

        // Synchronous audit only: borrow the global scheduler/loadout/message/FX
        // seams and restore their live values even if a case throws. Reflection
        // is confined to debug isolation because these APIs have no push-back.
        private sealed class DetachedScope : IDisposable
        {
            readonly TurnManager active = TurnManager.Active;
            readonly Entity world = TurnManager.World;
            readonly EntityFactory factory = LoadoutPart.Factory;
            readonly Random rng = LoadoutPart.Rng;
            readonly Action<string> onMessage = MessageLog.OnMessage;
            readonly Func<int> tickProvider = MessageLog.TickProvider;
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries();
            readonly List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue;
            readonly List<SpellFxSequence> spells;
            readonly List<AsciiFxRequest> ascii;
            readonly Queue<AsciiFxRequest> queue;
            readonly FieldInfo cosmeticField;
            readonly int cosmeticSerial;
            public DetachedScope(EntityFactory value)
            {
                queue = (Queue<AsciiFxRequest>)typeof(AsciiFxBus).GetField("PendingRequests", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                cosmeticField = typeof(SpellFxCapture).GetField("_cosmeticSerial", BindingFlags.NonPublic | BindingFlags.Static);
                cosmeticSerial = (int)cosmeticField.GetValue(null);
                spells = SpellFxBus.Drain(); ascii = AsciiFxBus.Drain();
                TurnManager.World = null; LoadoutPart.Factory = value; LoadoutPart.Rng = new Random(812);
                MessageLog.OnMessage = null;
            }
            public void Dispose()
            {
                foreach (var request in AsciiFxBus.Drain()) AsciiFxBus.Release(request);
                foreach (var request in ascii) queue.Enqueue(request);
                SpellFxBus.Clear(); foreach (var spell in spells) SpellFxBus.Emit(spell);
                cosmeticField.SetValue(null, cosmeticSerial);
                MessageLog.Restore(messages, announcements, flash, serial);
                MessageLog.OnMessage = onMessage; MessageLog.TickProvider = tickProvider;
                LoadoutPart.Factory = factory; LoadoutPart.Rng = rng; TurnManager.World = world;
                typeof(TurnManager).GetProperty("Active").SetValue(null, active);
            }
        }
    }
}
