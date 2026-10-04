using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Synthetic combat bodies isolate the decision; the supply is the real authored tonic.
    internal sealed class FieldMedicineFixture : IDisposable
    {
        public readonly EntityFactory Factory = new EntityFactory();
        public readonly Zone Zone = new Zone("field-medicine-" + Guid.NewGuid().ToString("N"));
        public readonly MedicineRandom Rng = new MedicineRandom();
        readonly TurnManager previousTurns = TurnManager.Active;
        readonly Entity previousWorld = TurnManager.World;
        readonly System.Collections.Generic.List<MessageLog.Entry> messages = MessageLog.GetAllEntries();
        readonly System.Collections.Generic.List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
        readonly int flash = MessageLog.FlashStamp;
        readonly int serial = (int)typeof(MessageLog).GetField("NextSerial", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        readonly Action<string> callback = MessageLog.OnMessage;
        readonly Action<int, int, string> cellDirty = ZoneRenderHooks.CellDirtyCallback;
        readonly Action<string> fullDirty = ZoneRenderHooks.FullDirtyCallback;
        readonly System.Collections.Generic.Dictionary<PropertyInfo, object> hooks =
            typeof(EntityVisualHooks).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.CanRead && p.CanWrite).ToDictionary(p => p, p => p.GetValue(null));

        public FieldMedicineFixture()
        {
            Factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            MessageLog.OnMessage = null;
            EntityVisualHooks.Reset();
            ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
        }
        public Entity Actor(int hp = 7, bool medicine = true, int x = 10, int y = 10)
        {
            var actor = new Entity { ID = "medicine-" + Guid.NewGuid().ToString("N"), BlueprintName = "Creature" };
            actor.SetTag("Creature"); actor.AddPart(new RenderPart { DisplayName = "test patchbearer" });
            actor.AddPart(new PhysicsPart { Solid = true }); actor.AddPart(new InventoryPart());
            actor.AddPart(new StatusEffectsPart());
            actor.AddPart(new MeleeWeaponPart { BaseDamage = "1d1", Attributes = "" });
            actor.AddPart(new BrainPart { FleeThreshold = .4f, CurrentZone = Zone, Rng = Rng, Wanders = false });
            foreach (var name in new[] { "Hitpoints", "Speed", "Strength", "Agility", "Toughness", "DV" })
                actor.Statistics[name] = new Stat { Owner = actor, Name = name, Min = 0,
                    Max = name == "Hitpoints" ? 20 : 1000,
                    BaseValue = name == "Hitpoints" ? hp : name == "Speed" ? 100 : name == "DV" ? 0 : 16 };
            if (medicine) AttachMedicine(actor);
            Assert.IsTrue(Zone.AddEntity(actor, x, y)); return actor;
        }
        public Entity Threat(Entity actor, int x = 11, int y = 10)
        {
            var target = Actor(20, false, x, y); target.SetTag("Player");
            actor.GetPart<BrainPart>().SetPersonallyHostile(target);
            return target;
        }
        public Entity Supply(Entity actor, string blueprint = "HealingTonic", int count = 1)
        {
            var item = Factory.CreateEntity(blueprint); Assert.NotNull(item);
            item.ID = "medicine-supply-" + Guid.NewGuid().ToString("N");
            if (item.GetPart<StackerPart>() == null) item.AddPart(new StackerPart());
            item.GetPart<StackerPart>().StackCount = count;
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(item)); return item;
        }
        public static Part AttachMedicine(Entity actor)
        {
            // Reflection lets native RED compile before the production part exists.
            var type = typeof(BrainPart).Assembly.GetType("CavesOfOoo.Core.FieldMedicinePart");
            Assert.NotNull(type, "The opt-in field medicine decision has not been implemented.");
            var part = (Part)Activator.CreateInstance(type); actor.AddPart(part); return part;
        }
        public bool Use(Entity actor, Entity target, Zone zone = null, Random rng = null)
            => Invoke(actor, target, zone ?? Zone, rng ?? Rng);
        public static bool Invoke(Entity actor, Entity target, Zone zone, Random rng)
        {
            var part = actor.GetPart("FieldMedicine"); Assert.NotNull(part);
            var method = part.GetType().GetMethod("TryUseMedicine"); Assert.NotNull(method);
            return (bool)method.Invoke(part, new object[] { target, zone, rng });
        }
        public static Entity Available(Entity actor)
        {
            var part = actor.GetPart("FieldMedicine"); Assert.NotNull(part);
            var method = part.GetType().GetMethod("FindCarriedMedicine"); Assert.NotNull(method);
            return (Entity)method.Invoke(part, null);
        }
        public void Act(Entity actor, Entity target, string goal)
        {
            var brain = actor.GetPart<BrainPart>(); brain.Target = target;
            if (goal == "kill") brain.PushGoal(new KillGoal(target));
            else if (goal == "flee") brain.PushGoal(new FleeGoal(target));
            // "bored" exercises ordinary hostile acquisition and its immediate child.
            actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
        }
        public TurnManager ScheduledAction(Entity actor, Entity player)
        {
            TurnManager.World = null;
            var turns = new TurnManager(); turns.AddEntity(actor); turns.AddEntity(player);
            Assert.AreSame(player, turns.ProcessUntilPlayerTurn()); return turns;
        }
        public void Dispose()
        {
            typeof(TurnManager).GetProperty("Active").SetValue(null, previousTurns);
            TurnManager.World = previousWorld;
            MessageLog.Restore(messages, announcements, flash, serial); MessageLog.OnMessage = callback;
            foreach (var pair in hooks) pair.Key.SetValue(null, pair.Value);
            ZoneRenderHooks.CellDirtyCallback = cellDirty; ZoneRenderHooks.FullDirtyCallback = fullDirty;
        }
        public sealed class MedicineRandom : Random
        {
            public int Calls;
            public override int Next(int minValue, int maxValue) { Calls++; return minValue; }
            public override int Next(int maxValue) { Calls++; return 0; }
        }
    }

    public sealed class FieldMedicineTests
    {
        FieldMedicineFixture f;
        [SetUp] public void Setup() => f = new FieldMedicineFixture();
        [TearDown] public void Teardown() => f.Dispose();

        [TestCase("kill", 7)] [TestCase("kill", 8)] [TestCase("flee", 7)] [TestCase("bored", 7)]
        public void ActualTonicReplacesOneFightOrRetreatAction(string goal, int hp)
        {
            var actor = f.Actor(hp); var target = f.Threat(actor); var tonic = f.Supply(actor);
            Assert.AreSame(tonic, FieldMedicineFixture.Available(actor));
            f.Act(actor, target, goal);
            Assert.AreEqual(hp + 8, actor.GetStatValue("Hitpoints"), "Real 4d6+4 with minimum rolls.");
            Assert.AreEqual((10, 10), f.Zone.GetEntityPosition(actor), "Medicine costs the movement action.");
            Assert.AreEqual(20, target.GetStatValue("Hitpoints"), "Medicine costs the attack action.");
            Assert.IsFalse(actor.GetPart<InventoryPart>().Objects.Contains(tonic));
            Assert.IsNull(tonic.GetPart<PhysicsPart>().InInventory);
            Assert.IsNull(FieldMedicineFixture.Available(actor)); Assert.AreEqual(4, f.Rng.Calls);
        }

        [Test] public void SchedulerChargesOneActionAndDoesNotRepeatPreActionEvents()
        {
            var actor = f.Actor(); var target = f.Threat(actor); f.Supply(actor);
            var probe = new FieldMedicineTurnProbe(); actor.AddPart(probe);
            actor.GetPart<BrainPart>().PushGoal(new KillGoal(target));
            var turns = f.ScheduledAction(actor, target);
            Assert.AreEqual(15, actor.GetStatValue("Hitpoints"));
            Assert.AreEqual(1, probe.Begins); Assert.AreEqual(1, probe.Ends);
            Assert.AreEqual(0, turns.GetEnergy(actor)); Assert.AreEqual(20, target.GetStatValue("Hitpoints"));
        }

        [TestCase("stun")] [TestCase("sleep")] [TestCase("custom-veto")]
        public void ScheduledActionBlockPreservesBottleAndDoesNotHeal(string blocker)
        {
            var actor = f.Actor(); var target = f.Threat(actor); var tonic = f.Supply(actor);
            actor.GetPart<BrainPart>().PushGoal(new KillGoal(target));
            if (blocker == "stun") actor.ApplyEffect(new StunnedEffect(5));
            else if (blocker == "sleep") actor.ApplyEffect(new AsleepByGasEffect(5));
            else actor.AddPart(new FieldMedicineTurnProbe { Block = true });
            f.ScheduledAction(actor, target);
            Assert.AreEqual(7, actor.GetStatValue("Hitpoints")); Assert.AreEqual((10, 10), f.Zone.GetEntityPosition(actor));
            Assert.AreSame(tonic, FieldMedicineFixture.Available(actor)); Assert.AreEqual(0, f.Rng.Calls);
        }

        [TestCase(9)] [TestCase(20)]
        public void HealthyCreatureKeepsMedicineAndPerformsOrdinaryMovement(int hp)
        {
            var actor = f.Actor(hp); var target = f.Threat(actor, 13); var tonic = f.Supply(actor);
            f.Act(actor, target, "kill");
            Assert.AreEqual(hp, actor.GetStatValue("Hitpoints")); Assert.AreEqual((11, 10), f.Zone.GetEntityPosition(actor));
            Assert.AreSame(tonic, FieldMedicineFixture.Available(actor));
        }

        [TestCase(false)] [TestCase(true)]
        public void NoMedicineOrNoOptInKeepsOrdinaryRetreat(bool optIn)
        {
            var actor = f.Actor(medicine: optIn); var target = f.Threat(actor);
            var tonic = optIn ? null : f.Supply(actor);
            f.Act(actor, target, "flee");
            Assert.AreEqual(7, actor.GetStatValue("Hitpoints")); Assert.AreNotEqual((10, 10), f.Zone.GetEntityPosition(actor));
            if (tonic != null) Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(tonic));
        }

        [Test] public void OneUnitFromStackIsConsumedAndInjuryAgainUsesOnlyActualRemainingSupply()
        {
            var actor = f.Actor(); var target = f.Threat(actor); var tonic = f.Supply(actor, count: 2);
            Assert.IsTrue(f.Use(actor, target)); Assert.AreEqual(1, tonic.GetPart<StackerPart>().StackCount);
            Assert.IsFalse(f.Use(actor, target), "No tonic should be spent above the threshold.");
            actor.GetStat("Hitpoints").BaseValue = 7;
            Assert.IsTrue(f.Use(actor, target)); Assert.IsEmpty(actor.GetPart<InventoryPart>().Objects);
            actor.GetStat("Hitpoints").BaseValue = 7;
            Assert.IsFalse(f.Use(actor, target)); Assert.AreEqual(7, actor.GetStatValue("Hitpoints"));
        }

        [TestCase(false)] [TestCase(true)]
        public void DeathDropsOnlyUnusedActualTonic(bool used)
        {
            var actor = f.Actor(); var target = f.Threat(actor); var tonic = f.Supply(actor);
            if (used) Assert.IsTrue(f.Use(actor, target));
            CombatSystem.ApplyDamage(actor, new Damage(100), target, f.Zone);
            Assert.IsNull(f.Zone.GetEntityCell(actor));
            Assert.AreEqual(!used, f.Zone.GetAllEntities().Contains(tonic));
            Assert.IsNull(tonic.GetPart<PhysicsPart>().InInventory);
        }

        [Test] public void CalmKeepsBottleUntilItsOverrideIsRemoved()
        {
            var actor = f.Actor(); var target = f.Threat(actor); var tonic = f.Supply(actor);
            var brain = actor.GetPart<BrainPart>(); brain.PushGoal(new KillGoal(target));
            var calm = new NoFightGoal(50, false); brain.PushGoal(calm);
            actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Assert.AreEqual(7, actor.GetStatValue("Hitpoints")); Assert.AreSame(tonic, FieldMedicineFixture.Available(actor));
            brain.RemoveGoal(calm); actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Assert.AreEqual(15, actor.GetStatValue("Hitpoints")); Assert.IsNull(FieldMedicineFixture.Available(actor));
        }

        [TestCase(false)] [TestCase(true)]
        public void SaveRestoresActualAvailabilityWithoutManufacturingAnotherBottle(bool used)
        {
            var actor = f.Actor(); var target = f.Threat(actor); var tonic = f.Supply(actor);
            if (used) Assert.IsTrue(f.Use(actor, target));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var available = FieldMedicineFixture.Available(loaded);
            Assert.AreEqual(!used, available != null);
            if (!used) { Assert.AreEqual(tonic.ID, available.ID); Assert.AreSame(loaded, available.GetPart<PhysicsPart>().InInventory); }
            f.Zone.RemoveEntity(actor); Assert.IsTrue(f.Zone.AddEntity(loaded, 10, 10));
            loaded.GetPart<BrainPart>().CurrentZone = f.Zone; loaded.GetPart<BrainPart>().SetPersonallyHostile(target);
            loaded.GetStat("Hitpoints").BaseValue = 7;
            Assert.AreEqual(!used, f.Use(loaded, target));
            Assert.IsNull(FieldMedicineFixture.Available(loaded));
        }
    }

    public sealed class FieldMedicineTurnProbe : Part
    {
        public override string Name => "FieldMedicineTurnProbe";
        public int Begins, Ends;
        public bool Block;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeginTakeAction") { Begins++; if (Block) return false; }
            if (e.ID == "EndTurn") Ends++;
            return true;
        }
    }
}
