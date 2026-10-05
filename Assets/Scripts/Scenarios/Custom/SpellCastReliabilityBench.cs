using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Controlled native-runtime command/scheduler/save audit. Plain
    /// actors, granted skills and fixed positions are fixtures, not ordinary
    /// discovery, input, combat balance or visual evidence.</summary>
    [Scenario(name: "Spell Cast Reliability Audit", category: "Combat",
        description: "Paid health buffs, multi-target charges and replacement-graph saving through real spell commands.")]
    public sealed class SpellCastReliabilityBench : IScenario
    {
        public string RunId { get; private set; }
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();

        public void Apply(ScenarioContext ctx)
        {
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            bool channel = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            try
            {
                using (var scope = new DetachedScope())
                {
                    var zone = new Zone("Overworld.0.0.0");
                    var actor = Creature(zone, "bench caster", 5, 5); actor.Tags["Player"] = "";
                    var a = Creature(zone, "first mark", 6, 5); var b = Creature(zone, "second mark", 7, 6);
                    actor.Statistics["Speed"] = new Stat { Owner = actor, Name = "Speed", BaseValue = 100, Max = 1000 };
                    actor.AddPart(new SkillsPart()); actor.AddPart(new ActivatedAbilitiesPart());
                    actor.GetPart<SkillsPart>().AddSkill(new Spellcraft_LeyTap());
                    actor.GetPart<SkillsPart>().AddSkill(new Pyromancy_HeartFlame());
                    actor.GetPart<SkillsPart>().AddSkill(new Pyromancy_FlameJet());
                    actor.GetPart<SkillsPart>().AddSkill(new Pyromancy_Kindle());
                    var turns = new TurnManager(); turns.AddEntity(actor); turns.ProcessUntilPlayerTurn();
                    MessageLog.TickProvider = () => turns.TickCount;
                    int tick = turns.TickCount;
                    bool ley = Cast(actor, zone, "CommandLeyTap"); Advance(turns, actor, zone);
                    Check("ley_paid_followup", ley && actor.GetStatValue("Hitpoints") == 170
                        && turns.TickCount == tick + 10 && actor.GetEffect<LeyTapEffect>()?.Duration == 3);
                    bool heart = Cast(actor, zone, "CommandHeartFlame"); Advance(turns, actor, zone);
                    Check("heart_paid_followup", heart && actor.GetStatValue("Hitpoints") == 85
                        && turns.TickCount == tick + 20 && actor.GetEffect<HeartFlameEffect>()?.Duration == 5
                        && actor.GetEffect<LeyTapEffect>()?.Duration == 2);
                    string leyText = EffectDescriber.Describe(actor.GetEffect<LeyTapEffect>());
                    string heartText = EffectDescriber.Describe(actor.GetEffect<HeartFlameEffect>());
                    int first = SkillEventDispatcher.GetSpellDamageModifier(actor, a, "Heat", 5);
                    int second = SkillEventDispatcher.GetSpellDamageModifier(actor, a, "Heat", 5);
                    Check("readouts_keep_charges", first == 65 && second == first
                        && actor.GetEffect<LeyTapEffect>()?.BonusDamage == 60
                        && actor.GetEffect<HeartFlameEffect>()?.ChargesRemaining == 3);

                    var manager = new OverworldZoneManager(null, 333);
                    manager.ReplaceLoadedState(new Dictionary<string, Zone> { { zone.ZoneID, zone } }, zone.ZoneID,
                        new Dictionary<string, List<ZoneConnection>>());
                    GameSessionState loaded;
                    using (var bytes = new MemoryStream())
                    {
                        GameSessionState.Capture(RunId, "spell bench", manager, turns, actor).Save(new SaveWriter(bytes));
                        bytes.Position = 0; loaded = GameSessionState.Load(new SaveReader(bytes, null));
                    }
                    var oldActor = actor; actor = loaded.Player; zone = loaded.ZoneManager.ActiveZone; turns = loaded.TurnManager;
                    Check("replacement_graph_preserves_both_buffs", actor != oldActor
                        && EffectDescriber.Describe(actor.GetEffect<LeyTapEffect>()) == leyText
                        && EffectDescriber.Describe(actor.GetEffect<HeartFlameEffect>()) == heartText
                        && turns.TickCount == tick + 20);
                    a = zone.GetCell(6, 5).Objects[0]; b = zone.GetCell(7, 6).Objects[0];
                    // Both fixtures lie in the actual east-facing cone.
                    bool area = Cast(actor, zone, "CommandFlameJet"); Advance(turns, actor, zone);
                    Check("area_spends_one_charge_for_both_targets", area
                        && a.GetStatValue("Hitpoints") == 130 && b.GetStatValue("Hitpoints") == 130
                        && actor.GetEffect<LeyTapEffect>() == null
                        && actor.GetEffect<HeartFlameEffect>()?.ChargesRemaining == 2
                        && turns.TickCount == tick + 30);
                    int before = a.GetStatValue("Hitpoints");
                    bool bolt = Cast(actor, zone, "CommandKindle"); Advance(turns, actor, zone);
                    Check("next_cast_uses_one_remaining_fire_charge", bolt
                        && before - a.GetStatValue("Hitpoints") == 2 * DiceRoller.Roll("1d4", new Random(11))
                        && actor.GetEffect<HeartFlameEffect>()?.ChargesRemaining == 1
                        && turns.TickCount == tick + 40);
                }
            }
            finally
            {
                Diag.Record("scenario", "SpellCastReliabilitySummary", payload: new
                    { runId = RunId, cases = Cases, failures = Failures, complete = Cases == 6 && Failures == 0 });
                Diag.SetChannel("scenario", channel);
            }
        }

        private static Entity Creature(Zone zone, string name, int x, int y)
        {
            var entity = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = name };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Owner = entity, Name = "Hitpoints", BaseValue = 200, Min = 0, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = name }); entity.AddPart(new StatusEffectsPart());
            if (!zone.AddEntity(entity, x, y)) throw new InvalidOperationException("Spell bench placement failed.");
            return entity;
        }
        private static bool Cast(Entity actor, Zone zone, string command, int dx = 1, int dy = 0)
        {
            var input = GameEvent.New(command);
            try
            {
                var cell = zone.GetEntityCell(actor);
                input.SetParameter("Zone", (object)zone); input.SetParameter("RNG", (object)new Random(11));
                input.SetParameter("SourceCell", (object)cell); input.SetParameter("TargetCell", (object)zone.GetCell(cell.X + dx, cell.Y + dy));
                input.SetParameter("DirectionX", dx); input.SetParameter("DirectionY", dy);
                actor.FireEvent(input); return input.Handled && input.GetParameter<bool>("BlocksTurnAdvance");
            }
            finally { input.Release(); }
        }
        private static void Advance(TurnManager turns, Entity actor, Zone zone)
        {
            turns.EndTurn(actor, zone);
            if (turns.ProcessUntilPlayerTurn() != actor) throw new InvalidOperationException("Spell bench lost its owner turn.");
        }
        private void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++;
            Audit.Add((passed ? "PASS " : "FAIL ") + name);
            Diag.Record("scenario", "SpellCastReliabilityCase", payload: new { runId = RunId, name, passed });
        }

        // Same synchronous isolation pattern as DensityCombatSchedulerBench;
        // full save/load also borrows the active settlement registry.
        private sealed class DetachedScope : IDisposable
        {
            readonly TurnManager active = TurnManager.Active;
            readonly Entity world = TurnManager.World;
            readonly SettlementManager settlement = SettlementManager.Current;
            readonly Action<string> onMessage = MessageLog.OnMessage;
            readonly Func<int> tickProvider = MessageLog.TickProvider;
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries();
            readonly List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue;
            readonly List<SpellFxSequence> spells = SpellFxBus.Drain();
            readonly List<AsciiFxRequest> ascii = AsciiFxBus.Drain();
            readonly Queue<AsciiFxRequest> queue;
            readonly FieldInfo cosmeticField;
            readonly int cosmeticSerial;
            public DetachedScope()
            {
                queue = (Queue<AsciiFxRequest>)typeof(AsciiFxBus).GetField("PendingRequests", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                cosmeticField = typeof(SpellFxCapture).GetField("_cosmeticSerial", BindingFlags.NonPublic | BindingFlags.Static);
                cosmeticSerial = (int)cosmeticField.GetValue(null);
                TurnManager.World = null; MessageLog.OnMessage = null;
            }
            public void Dispose()
            {
                foreach (var request in AsciiFxBus.Drain()) AsciiFxBus.Release(request);
                foreach (var request in ascii) queue.Enqueue(request);
                SpellFxBus.Clear(); foreach (var spell in spells) SpellFxBus.Emit(spell);
                cosmeticField.SetValue(null, cosmeticSerial);
                MessageLog.Restore(messages, announcements, flash, serial);
                MessageLog.OnMessage = onMessage; MessageLog.TickProvider = tickProvider; TurnManager.World = world;
                typeof(TurnManager).GetProperty("Active").SetValue(null, active);
                typeof(SettlementManager).GetProperty("Current").SetValue(null, settlement);
            }
        }
    }
}
