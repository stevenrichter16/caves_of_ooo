using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Detached native-runtime meal commands, owner turns and save replacement.
    /// Factory-supplied controlled food/station fixtures do not prove ordinary discovery.</summary>
    [Scenario(name: "Meal Preparation Audit", category: "Items",
        description: "Five cooked preparations through real inventory commands, turn payment and saving.")]
    public sealed class MealPreparationBench : IScenario
    {
        public string RunId { get; private set; }
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            var oldFactory = MaterialReactionResolver.Factory;
            try
            {
                using (var scope = new DetachedScope())
                {
                    MaterialReactionResolver.Factory = ctx.Factory;
                    string[] raw = { "Emberwheat", "Hearthbulb", "Mushroom", "RawMeat", "Starapple" };
                    string[] cooked = { "ToastedEmberwheat", "RoastedHearthbulb", "RoastedMushroom", "CookedMeat", "RoastedStarapple" };
                    string[] stats = { "HeatResistance", "ColdResistance", "AcidResistance", "Toughness", "DV" };
                    int[] bonuses = { 20, 20, 20, 2, 1 };
                    for (int i = 0; i < raw.Length; i++)
                    {
                        var zone = new Zone("Overworld.0.0.0");
                        var actor = ctx.Factory.CreateEntity("Player");
                        var brain = actor.GetPart<BrainPart>(); if (brain != null) actor.RemovePart(brain);
                        Require(zone.AddEntity(actor, 10, 10), "player fixture placement");
                        var station = ctx.Factory.CreateEntity("Campfire");
                        Require(station != null && zone.AddEntity(station, 11, 10), "authored campfire fixture");
                        var food = ctx.Factory.CreateEntity(raw[i]);
                        Require(actor.GetPart<InventoryPart>().AddObject(food), "finite raw food fixture");
                        var turns = new TurnManager(); turns.AddEntity(actor); Require(turns.ProcessUntilPlayerTurn() == actor, "owner turn");
                        int tick = turns.TickCount, before = actor.GetStatValue(stats[i]);
                        bool prepared = InventorySystem.PerformAction(actor, food, "Cook", zone);
                        if (prepared) Advance(turns, actor, zone);
                        var dish = actor.GetPart<InventoryPart>().Objects.Find(x => x.BlueprintName == cooked[i]);
                        Check(cooked[i] + "_real_cook", prepared && dish != null && turns.TickCount == tick + 10);
                        Require(dish != null, "cooked product");
                        bool ate = InventorySystem.PerformAction(actor, dish, "Eat", zone);
                        if (ate) Advance(turns, actor, zone);
                        var meal = actor.GetEffect<PreparedMealEffect>();
                        Check(cooked[i] + "_paid_protection", ate && meal?.Duration == 100
                            && actor.GetStatValue(stats[i]) == before + bonuses[i] && turns.TickCount == tick + 20);
                        Advance(turns, actor, zone);
                        Check(cooked[i] + "_owner_clock", meal?.Duration == 99 && turns.TickCount == tick + 30);
                        var manager = new OverworldZoneManager(null, 64);
                        manager.ReplaceLoadedState(new Dictionary<string, Zone> { { zone.ZoneID, zone } }, zone.ZoneID,
                            new Dictionary<string, List<ZoneConnection>>());
                        GameSessionState loaded;
                        using (var bytes = new MemoryStream())
                        {
                            GameSessionState.Capture(RunId, "meal bench", manager, turns, actor).Save(new SaveWriter(bytes));
                            bytes.Position = 0; loaded = GameSessionState.Load(new SaveReader(bytes, null));
                        }
                        var restored = loaded.Player;
                        bool retained = restored != actor && restored.GetEffect<PreparedMealEffect>()?.Duration == 99
                            && restored.GetStatValue(stats[i]) == before + bonuses[i];
                        restored.RemoveEffect<PreparedMealEffect>();
                        Check(cooked[i] + "_replacement_save_and_remove", retained && restored.GetStatValue(stats[i]) == before);
                    }
                }
            }
            finally { MaterialReactionResolver.Factory = oldFactory; }
        }
        private static void Advance(TurnManager turns, Entity actor, Zone zone)
        { turns.EndTurn(actor, zone); Require(turns.ProcessUntilPlayerTurn() == actor, "owner turn retained"); }
        private static void Require(bool value, string name)
        { if (!value) throw new InvalidOperationException("Meal audit fixture: " + name); }
        private void Check(string name, bool passed)
        { Cases++; if (!passed) Failures++; Audit.Add((passed ? "PASS " : "FAIL ") + name); }
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
