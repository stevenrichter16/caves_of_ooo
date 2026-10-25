using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Real scheduler/effect boundaries; no reflection into ResolveActorZone and
    // no direct visual-hook emissions. This fixture deliberately retains the
    // stale Player brain that ordinary InputHandler transitions currently leave.
    public sealed class PlayerTurnZoneContextTests
    {
        Fixture f;
        [SetUp] public void Setup() => f = new Fixture();
        [TearDown] public void Cleanup() => f?.Dispose();

        [TestCase(false)] [TestCase(true)]
        public void OrdinaryTransitionTicksPoisonInExactDestinationWithoutLeakingHiddenReadouts(bool hidden)
        {
            f.Transition();
            var origin = f.Destination.GetEntityCell(f.Player);
            origin.IsVisible = !hidden;
            Assert.AreSame(f.Source, f.Brain.CurrentZone, "Reproduce the stale brain; do not fix the fixture by rebinding it.");
            var poison = f.Dose(f.Player);
            Assert.AreSame(f.Player, f.Turns.ProcessUntilPlayerTurn());
            Assert.AreEqual(19, f.Player.GetStatValue("Hitpoints")); Assert.AreEqual(4, poison.Duration);
            Assert.AreEqual(1, f.Damage.Count); var hit = f.Damage[0];
            Assert.AreSame(f.Destination, hit.Zone); Assert.AreSame(f.Inflictor, hit.Source); Assert.AreSame(f.Player, hit.Target);
            Assert.AreEqual(1, hit.Amount); Assert.False(hit.Lethal);
            Assert.AreSame(f.Destination, f.Probe.BeginZone); Assert.AreEqual(1, f.Probe.Begins);
            var numbers = f.Numbers(); Assert.AreEqual(hidden ? 0 : 1, numbers.Count);
            if (!hidden) { Assert.AreSame(f.Destination, numbers[0].Zone); Assert.AreSame(origin, numbers[0].Origin); Assert.AreSame(f.Player, numbers[0].Owner); Assert.AreEqual(1, numbers[0].Amount); }
            Assert.AreSame(f.Source, f.Brain.CurrentZone, "Resolution is read-only; AI context need not be mutated.");
            Assert.AreEqual(0, f.Turns.TickCount, "A ready owner tick must not add arbitrary clock advancement.");
        }

        [Test] public void PlacedPlayerWithoutBrainUsesItsPhysicalZone()
        {
            Assert.True(f.Player.RemovePart(f.Brain));
            f.Dose(f.Player); Assert.AreSame(f.Player, f.Turns.ProcessUntilPlayerTurn());
            Assert.AreSame(f.Source, f.Probe.BeginZone); Assert.AreSame(f.Source, f.Damage[0].Zone);
            Assert.AreEqual(19, f.Player.GetStatValue("Hitpoints")); Assert.AreEqual(1, f.Numbers().Count);
        }

        [Test] public void DetachedPlayerDoesNotInventAZoneFromItsStaleBrain()
        {
            Assert.True(f.Source.RemoveEntity(f.Player));
            f.Dose(f.Player); Assert.AreSame(f.Player, f.Turns.ProcessUntilPlayerTurn());
            Assert.IsNull(f.Probe.BeginZone); Assert.IsNull(f.Damage[0].Zone);
            Assert.AreEqual(19, f.Player.GetStatValue("Hitpoints"), "Detached pure-simulation damage behavior remains unchanged.");
            Assert.IsEmpty(f.Numbers()); Assert.AreSame(f.Source, f.Brain.CurrentZone);
        }

        [TestCase("cell")] [TestCase("graph")]
        public void ReplacedCanonicalOwnerGraphCannotSupplyAFormerWorldContext(string replacement)
        {
            f.Transition(); var oldCell = f.Destination.GetEntityCell(f.Player);
            if (replacement == "cell") f.Destination.Cells[oldCell.X, oldCell.Y] = new Cell(oldCell.X, oldCell.Y, f.Destination);
            else f.Manager.CachedZones[f.Destination.ZoneID] = new Zone(f.Destination.ZoneID);
            f.Dose(f.Player); Assert.AreSame(f.Player, f.Turns.ProcessUntilPlayerTurn());
            Assert.IsNull(f.Probe.BeginZone); Assert.IsNull(f.Damage[0].Zone);
            Assert.AreEqual(19, f.Player.GetStatValue("Hitpoints")); Assert.IsEmpty(f.Numbers());
        }

        [Test] public void CurrentPhysicalPlayerZoneWinsOverAnUnrelatedActiveZone()
        {
            // The manager may temporarily have another active presentation zone;
            // its cache still authorizes the exact physical owner graph.
            f.Manager.SetActiveZone(f.Destination);
            f.Dose(f.Player); Assert.AreSame(f.Player, f.Turns.ProcessUntilPlayerTurn());
            Assert.AreSame(f.Source, f.Probe.BeginZone); Assert.AreSame(f.Source, f.Damage[0].Zone);
            Assert.AreSame(f.Destination, f.Manager.ActiveZone); Assert.AreEqual(1, f.Numbers().Count);
        }

        [Test] public void LethalPoisonAfterTransitionRemovesAndDropsOnlyInDestination()
        {
            f.Transition(); var origin = f.Destination.GetEntityCell(f.Player);
            f.Player.GetStat("Hitpoints").BaseValue = 1;
            var item = new Entity { ID = "poison-context-token", BlueprintName = "Token" };
            item.SetTag("Item"); item.AddPart(new PhysicsPart { Takeable = true }); item.AddPart(new RenderPart { DisplayName = "token" });
            Assert.True(f.Player.GetPart<InventoryPart>().AddObject(item));
            f.Dose(f.Player); f.Turns.ProcessUntilPlayerTurn();
            Assert.True(CombatSystem.IsDeathHandled(f.Player)); Assert.AreEqual(0, f.Player.GetStatValue("Hitpoints"));
            Assert.AreEqual(1, f.Deaths.Count); Assert.AreSame(f.Destination, f.Deaths[0]);
            Assert.AreSame(f.Destination, f.Damage[0].Zone); Assert.True(f.Damage[0].Lethal);
            Assert.IsNull(f.Destination.GetEntityCell(f.Player)); Assert.IsNull(f.Source.GetEntityCell(f.Player));
            Assert.False(f.Turns.IsRegistered(f.Player));
            Assert.AreSame(origin, f.Destination.GetEntityCell(item)); Assert.IsNull(f.Source.GetEntityCell(item));
            Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
            var numbers = f.Numbers(); Assert.AreEqual(1, numbers.Count); Assert.True(numbers[0].OriginOnly); Assert.AreSame(origin, numbers[0].Origin);
        }

        [TestCase(true)] [TestCase(false)]
        public void NonPlayerContextKeepsExistingBrainPolicyRatherThanUsingPlayerOrActiveZone(bool hasBrain)
        {
            var npc = MakeActor("npc", false); var probe = new ContextProbe { SuppressNpcAction = true }; npc.AddPart(probe);
            if (hasBrain) npc.AddPart(new BrainPart { CurrentZone = f.Source });
            Assert.True(f.Destination.AddEntity(npc, 20, 10));
            f.Manager.SetActiveZone(f.Destination);
            f.Turns.RestoreSavedState(0, false, null, new List<TurnManager.SavedTurnEntry> {
                new TurnManager.SavedTurnEntry { Entity = npc, Energy = 1000 },
                new TurnManager.SavedTurnEntry { Entity = f.Player, Energy = 1000 } });
            Assert.AreSame(f.Player, f.Turns.ProcessUntilPlayerTurn());
            Assert.AreEqual(1, probe.Begins); Assert.AreEqual(1, probe.Takes); Assert.AreEqual(1, probe.Ends);
            Assert.AreSame(hasBrain ? f.Source : null, probe.BeginZone);
            Assert.AreSame(probe.BeginZone, probe.TakeZone); Assert.AreSame(probe.BeginZone, probe.EndZone);
            Assert.AreEqual(20, npc.GetStatValue("Hitpoints")); Assert.IsEmpty(f.Damage);
        }

        [Test] public void UnpoisonedCurrentPlayerKeepsNormalContextWithoutManufacturingDamage()
        {
            Assert.AreSame(f.Player, f.Turns.ProcessUntilPlayerTurn());
            Assert.AreSame(f.Source, f.Probe.BeginZone); Assert.AreEqual(1, f.Probe.Begins);
            Assert.AreEqual(20, f.Player.GetStatValue("Hitpoints")); Assert.IsEmpty(f.Damage); Assert.IsEmpty(f.Numbers());
        }

        static Entity MakeActor(string id, bool player)
        {
            var e = new Entity { ID = id, BlueprintName = player ? "Player" : "ContextNpc" };
            e.SetTag("Creature"); if (player) e.SetTag("Player");
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", Owner = e, BaseValue = 20, Min = 0, Max = 20 };
            e.Statistics["Speed"] = new Stat { Name = "Speed", Owner = e, BaseValue = 100 };
            e.AddPart(new PhysicsPart { Solid = true }); e.AddPart(new RenderPart { DisplayName = player ? "you" : "context NPC" });
            e.AddPart(new InventoryPart()); e.AddPart(new StatusEffectsPart()); return e;
        }

        sealed class ContextProbe : Part
        {
            public int Begins, Takes, Ends; public Zone BeginZone, TakeZone, EndZone; public bool SuppressNpcAction;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "BeginTakeAction") { Begins++; BeginZone = e.GetParameter<Zone>("Zone"); }
                if (e.ID == "TakeTurn") { Takes++; TakeZone = e.GetParameter<Zone>("Zone"); return !SuppressNpcAction; }
                if (e.ID == "EndTurn") { Ends++; EndZone = e.GetParameter<Zone>("Zone"); }
                return true;
            }
        }
        sealed class DamageReceipt { public Entity Target, Source; public Zone Zone; public int Amount; public bool Lethal; }
        sealed class NumberReceipt { public Entity Owner; public Zone Zone; public Cell Origin; public int Amount; public bool OriginOnly; }
        sealed class Fixture : IDisposable
        {
            readonly HotbarSaveFixture isolation;
            readonly List<(FieldInfo field, object value)> hooks = new List<(FieldInfo, object)>();
            public readonly Zone Source = new Zone("Overworld.11.10.0"), Destination = new Zone("Overworld.11.9.0");
            public readonly Entity Player = MakeActor("poison-context-player", true), Inflictor = MakeActor("poison-context-snake", false);
            public readonly BrainPart Brain; public readonly ContextProbe Probe = new ContextProbe();
            public readonly OverworldZoneManager Manager; public readonly TurnManager Turns;
            public readonly List<DamageReceipt> Damage = new List<DamageReceipt>(); public readonly List<Zone> Deaths = new List<Zone>();
            public Fixture()
            {
                try
                {
                    isolation = new HotbarSaveFixture(true, false);
                    foreach (var type in new[] { typeof(EntityVisualHooks), typeof(ZoneRenderHooks) })
                        foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                            if (!field.IsInitOnly && typeof(Delegate).IsAssignableFrom(field.FieldType)) { hooks.Add((field, field.GetValue(null))); field.SetValue(null, null); }
                    TurnManager.World = null; LootDropSystem.Factory = null; CorpsePart.Factory = null;
                    Brain = new BrainPart { CurrentZone = Source }; Player.AddPart(Brain); Player.AddPart(Probe);
                    Assert.True(Source.AddEntity(Player, 30, 0)); Assert.True(Source.AddEntity(Inflictor, 31, 0));
                    foreach (var c in Source.Cells) c.Explored = c.IsVisible = true;
                    foreach (var c in Destination.Cells) c.Explored = c.IsVisible = true;
                    Manager = new OverworldZoneManager(null, 64);
                    Manager.ReplaceLoadedState(new Dictionary<string, Zone> { { Source.ZoneID, Source }, { Destination.ZoneID, Destination } },
                        Source.ZoneID, new Dictionary<string, List<ZoneConnection>>());
                    Turns = new TurnManager(); Ready();
                    isolation.BindOld(GameSessionState.Capture(Guid.NewGuid().ToString("N"), "poison-context-test", Manager, Turns, Player));
                    EntityVisualHooks.DamageCallback = (target, source, zone, amount, lethal) => Damage.Add(new DamageReceipt { Target = target, Source = source, Zone = zone, Amount = amount, Lethal = lethal });
                    EntityVisualHooks.DeathCallback = (target, source, zone, x, y) => Deaths.Add(zone);
                }
                catch { Dispose(); throw; }
            }
            void Ready() => Turns.RestoreSavedState(0, false, null, new List<TurnManager.SavedTurnEntry> {
                new TurnManager.SavedTurnEntry { Entity = Player, Energy = 1000 } });
            public void Transition()
            {
                var result = ZoneTransitionSystem.TransitionPlayer(Player, Source, TransitionDirection.North, 30, 0, Manager, null);
                Assert.True(result.Success, result.ErrorReason); Assert.AreSame(Destination, result.NewZone);
                typeof(InputHandler).GetMethod("HandleZoneTransition", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(isolation.Input, new object[] { result });
                Assert.AreSame(Destination, isolation.Input.CurrentZone); Assert.AreSame(Destination, Manager.ActiveZone);
                Assert.IsNull(Source.GetEntityCell(Player)); Assert.NotNull(Destination.GetEntityCell(Player)); Ready();
            }
            public PoisonedEffect Dose(Entity actor)
            {
                var effect = new PoisonedEffect(4, "1d1", new Random(1)) { DamageSource = Inflictor };
                Assert.True(actor.ApplyEffect(effect, Inflictor, Destination.GetEntityCell(actor) != null ? Destination : Source));
                AsciiFxBus.Clear(); return effect;
            }
            public List<NumberReceipt> Numbers()
            {
                var result = new List<NumberReceipt>();
                foreach (var r in AsciiFxBus.Drain())
                {
                    try { if (r.Type == AsciiFxRequestType.FloatingNumber && r.Number > 0) result.Add(new NumberReceipt { Owner = r.Anchor, Zone = r.Zone, Origin = r.OriginCell, Amount = r.Number, OriginOnly = r.AllowOriginOnly }); }
                    finally { AsciiFxBus.Release(r); }
                }
                return result;
            }
            public void Dispose()
            {
                for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].field.SetValue(null, hooks[i].value);
                isolation?.Dispose();
            }
        }
    }
}
