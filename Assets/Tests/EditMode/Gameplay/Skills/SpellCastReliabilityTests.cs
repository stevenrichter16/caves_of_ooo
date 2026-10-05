using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    internal sealed class SpellCastFixture
    {
        public readonly Zone Zone = new Zone("Overworld.0.0.0");
        public readonly Entity Actor;
        public readonly TurnManager Turns;
        public SpellCastFixture(int speed = 100)
        {
            Actor = Creature("caster", 5, 5);
            Actor.Tags["Player"] = "";
            Actor.Statistics["Speed"] = new Stat { Owner = Actor, Name = "Speed", BaseValue = speed, Max = 1000 };
            Actor.AddPart(new SkillsPart()); Actor.AddPart(new ActivatedAbilitiesPart());
            Turns = new TurnManager(); Turns.AddEntity(Actor); Turns.ProcessUntilPlayerTurn();
        }
        public Entity Creature(string name, int x, int y)
        {
            var e = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 200, Min = 0, Max = 200 };
            foreach (string r in new[] { "HeatResistance", "ColdResistance", "ElectricResistance", "AcidResistance" })
                e.Statistics[r] = new Stat { Owner = e, Name = r, BaseValue = 0, Min = -100, Max = 100 };
            e.AddPart(new RenderPart { DisplayName = name }); e.AddPart(new StatusEffectsPart());
            Assert.True(Zone.AddEntity(e, x, y)); return e;
        }
        public T Learn<T>() where T : BaseSkillPart, new()
        { var skill = new T(); Assert.True(Actor.GetPart<SkillsPart>().AddSkill(skill)); return skill; }
        public BaseSkillPart Learn(Type type)
        { var skill = (BaseSkillPart)Activator.CreateInstance(type); Assert.True(Actor.GetPart<SkillsPart>().AddSkill(skill)); return skill; }
        public bool Cast(BaseSkillPart skill, int dx = 1, int dy = 0)
            => Cast(Actor, Zone, skill.DeclareActivatedAbility(Actor).Command, dx, dy);
        public static bool Cast(Entity actor, Zone zone, string command, int dx = 1, int dy = 0)
        {
            var ev = GameEvent.New(command);
            ev.SetParameter("Zone", (object)zone); ev.SetParameter("RNG", (object)new System.Random(11));
            ev.SetParameter("SourceCell", (object)zone.GetEntityCell(actor));
            var p = zone.GetEntityPosition(actor);
            ev.SetParameter("TargetCell", (object)zone.GetCell(p.x + dx, p.y + dy));
            ev.SetParameter("DirectionX", dx); ev.SetParameter("DirectionY", dy);
            actor.FireEvent(ev); bool result = ev.Handled;
            if (result) Assert.True(ev.GetParameter<bool>("BlocksTurnAdvance"));
            ev.Release(); return result;
        }
        public void Advance()
        { Turns.EndTurn(Actor, Zone); Assert.AreSame(Actor, Turns.ProcessUntilPlayerTurn()); }
        public Effect Buff(string type) => Actor.GetPart<StatusEffectsPart>().GetAllEffects().FirstOrDefault(e => e.GetType().Name == type);
        public GrimoireChargePart GiveInk()
        {
            var inventory = new InventoryPart { MaxWeight = 500 }; Actor.AddPart(inventory);
            var book = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "test-ink" };
            var ink = new GrimoireChargePart { Charges = 10, MaxCharges = 10 }; book.AddPart(ink);
            inventory.AddObject(book); return ink;
        }
        private static TurnManager _previousTurns;
        private static Entity _previousWorld;
        private static SettlementManager _previousSettlements;
        private static Func<int> _previousTickProvider;
        public static void RestoreRuntime()
        {
            typeof(TurnManager).GetProperty("Active").SetValue(null, _previousTurns);
            typeof(SettlementManager).GetProperty("Current").SetValue(null, _previousSettlements);
            TurnManager.World = _previousWorld; MessageLog.TickProvider = _previousTickProvider;
        }
        public static void Reset()
        {
            _previousTurns = TurnManager.Active; _previousWorld = TurnManager.World;
            _previousSettlements = SettlementManager.Current; _previousTickProvider = MessageLog.TickProvider;
            TurnManager.World = null;
            MessageLog.Clear(); SkillRegistry.ResetForTests(); AsciiFxBus.Clear();
            ResonanceSystem.ResetForTests();
            ResonanceSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Resonance/Resonance.json")));
        }
    }

    public class SpellCastReliabilityTests
    {
        [SetUp] public void Setup() => SpellCastFixture.Reset();
        [TearDown] public void TearDown() { ResonanceSystem.ResetForTests(); SpellCastFixture.RestoreRuntime(); }

        [TestCase(50, false)] [TestCase(100, false)] [TestCase(200, false)]
        [TestCase(50, true)] [TestCase(100, true)] [TestCase(200, true)]
        public void PaidHealthBuffSurvivesToTheNextOwnerActionAtEverySpeed(int speed, bool heart)
        {
            var f = new SpellCastFixture(speed); var victim = f.Creature("target", 6, 5);
            var buff = heart ? f.Learn(typeof(Pyromancy_HeartFlame)) : f.Learn(typeof(Spellcraft_LeyTap));
            var bolt = f.Learn<Pyromancy_Kindle>();
            int initialTick = f.Turns.TickCount;
            Assert.True(f.Cast(buff)); Assert.AreEqual(heart ? 100 : 170, f.Actor.GetStatValue("Hitpoints"));
            f.Advance(); Assert.Greater(f.Turns.TickCount, initialTick);
            int raw = DiceRoller.Roll("1d4", new System.Random(11));
            Assert.True(f.Cast(bolt));
            Assert.AreEqual(heart ? raw * 2 : raw + 60, 200 - victim.GetStatValue("Hitpoints"), "A paid action must leave the promised next-action benefit usable.");
        }

        [TestCase(false, 3)] [TestCase(true, 5)]
        public void LifetimeCountsOwnerActionsAndExpiresAfterTheAdvertisedWindow(bool heart, int duration)
        {
            var f = new SpellCastFixture(); var skill = heart ? f.Learn(typeof(Pyromancy_HeartFlame)) : f.Learn(typeof(Spellcraft_LeyTap));
            Assert.True(f.Cast(skill)); f.Advance();
            string name = heart ? "HeartFlameEffect" : "LeyTapEffect";
            var effect = f.Buff(name); Assert.NotNull(effect, "The buff must be an ordinary visible saved effect.");
            Assert.AreEqual(duration, effect.Duration);
            f.Turns.AdvanceClock(10000); Assert.AreEqual(duration, effect.Duration, "World ticks alone must not consume an owner-action window.");
            for (int remaining = duration - 1; remaining > 0; remaining--)
            { f.Advance(); Assert.AreEqual(remaining, f.Buff(name).Duration); }
            f.Advance(); Assert.Null(f.Buff(name));
            Assert.AreEqual(0, skill.OnGetSpellDamageModifier(f.Actor, f.Actor, "Heat", 4));
        }

        [Test]
        public void RepeatedModifierAndReadoutQueriesCannotSpendOrAgeEitherBuff()
        {
            var f = new SpellCastFixture(); var ley = f.Learn<Spellcraft_LeyTap>(); var heart = f.Learn<Pyromancy_HeartFlame>();
            Assert.True(f.Cast(ley)); Assert.True(f.Cast(heart));
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(60, ley.OnGetSpellDamageModifier(f.Actor, f.Actor, "Heat", 5));
                Assert.AreEqual(5, heart.OnGetSpellDamageModifier(f.Actor, f.Actor, "Heat", 5));
                Assert.AreEqual(60, ley.PendingBonus); Assert.AreEqual(3, heart.ChargesRemaining);
            }
            var effect = f.Buff("HeartFlameEffect"); Assert.NotNull(effect);
            StringAssert.Contains("3", EffectDescriber.Describe(effect));
            StringAssert.Contains("5", EffectDescriber.Describe(effect));
            Assert.AreEqual(0, effect.GetEffectType() & Effect.TYPE_NEGATIVE);
        }

        [Test]
        public void OneAreaCastBoostsEveryTargetAndConsumesOneChargeFromEachMatchingBuff()
        {
            var f = new SpellCastFixture(); var a = f.Creature("a", 6, 5); var b = f.Creature("b", 5, 6);
            var ley = f.Learn<Spellcraft_LeyTap>(); var heart = f.Learn<Pyromancy_HeartFlame>(); var nova = f.Learn<Pyromancy_Conflagration>();
            Assert.True(f.Cast(ley)); Assert.True(f.Cast(heart)); Assert.True(f.Cast(nova));
            var rng = new System.Random(11);
            Assert.AreEqual(DiceRoller.Roll("2d6", rng) * 2 + 60, 200 - a.GetStatValue("Hitpoints"));
            Assert.AreEqual(DiceRoller.Roll("2d6", rng) * 2 + 60, 200 - b.GetStatValue("Hitpoints"));
            Assert.AreEqual(0, ley.PendingBonus); Assert.AreEqual(2, heart.ChargesRemaining);
        }

        [Test]
        public void UtilityAndEmptyFiredBoltKeepChargesButAResistedDirectAttemptSpendsOne()
        {
            var f = new SpellCastFixture(); var ley = f.Learn<Spellcraft_LeyTap>(); var heart = f.Learn<Pyromancy_HeartFlame>();
            var dry = f.Learn<Hydromancy_DryingBreeze>(); var bolt = f.Learn<Pyromancy_Kindle>(); var jet = f.Learn<Pyromancy_FlameJet>();
            Assert.True(f.Cast(ley)); Assert.True(f.Cast(heart)); Assert.True(f.Cast(dry)); Assert.True(f.Cast(bolt));
            Assert.AreEqual(60, ley.PendingBonus); Assert.AreEqual(3, heart.ChargesRemaining);
            var victim = f.Creature("immune", 6, 5); victim.Statistics["HeatResistance"].BaseValue = 100;
            Assert.True(f.Cast(jet)); Assert.AreEqual(200, victim.GetStatValue("Hitpoints"));
            Assert.AreEqual(0, ley.PendingBonus); Assert.AreEqual(2, heart.ChargesRemaining);
        }

        [Test]
        public void NonmatchingColdSpellSpendsLeyTapButKeepsHeartFlame()
        {
            var f = new SpellCastFixture(); f.Creature("target", 6, 5);
            var ley = f.Learn<Spellcraft_LeyTap>(); var heart = f.Learn<Pyromancy_HeartFlame>(); var ice = f.Learn<Cryomancy_IceLance>();
            Assert.True(f.Cast(ley)); Assert.True(f.Cast(heart)); Assert.True(f.Cast(ice));
            Assert.AreEqual(0, ley.PendingBonus); Assert.AreEqual(3, heart.ChargesRemaining);
        }

        [Test]
        public void ActualReplacementGraphPreservesPartlySpentVisibleBuffsAndOwnerWindow()
        {
            var f = new SpellCastFixture(); var target = f.Creature("target", 6, 5);
            var heart = f.Learn<Pyromancy_HeartFlame>(); var ley = f.Learn<Spellcraft_LeyTap>(); var jet = f.Learn<Pyromancy_FlameJet>();
            f.Learn<Pyromancy_Kindle>(); Assert.True(f.Cast(heart)); f.Advance(); Assert.True(f.Cast(jet)); f.Advance();
            Assert.True(f.Cast(ley)); f.Advance();
            Assert.AreEqual(2, heart.ChargesRemaining);
            var before = f.Actor.GetPart<StatusEffectsPart>().GetAllEffects().Select(EffectDescriber.Describe).ToArray();
            var manager = new OverworldZoneManager(null, 333);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { f.Zone.ZoneID, f.Zone } }, f.Zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
            var restored = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("cast-buff", "fixture", manager, f.Turns, f.Actor));
            Assert.AreNotSame(f.Actor, restored.Player);
            var hp = restored.Player.GetPart<SkillsPart>().GetSkill(nameof(Pyromancy_HeartFlame)) as Pyromancy_HeartFlame;
            var lp = restored.Player.GetPart<SkillsPart>().GetSkill(nameof(Spellcraft_LeyTap)) as Spellcraft_LeyTap;
            Assert.NotNull(hp); Assert.NotNull(lp); Assert.AreEqual(2, hp.ChargesRemaining); Assert.AreEqual(30, lp.PendingBonus);
            CollectionAssert.AreEqual(before, restored.Player.GetPart<StatusEffectsPart>().GetAllEffects().Select(EffectDescriber.Describe));
            Assert.True(SpellCastFixture.Cast(restored.Player, restored.ZoneManager.ActiveZone, "CommandKindle"));
            Assert.AreEqual(1, hp.ChargesRemaining); Assert.AreEqual(0, lp.PendingBonus);
        }
    }
}
