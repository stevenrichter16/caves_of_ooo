using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The Rites batch of the mutations→skills migration — all eleven
    /// consuming rites converged onto <see cref="ConsumingRiteSkillBase"/>
    /// (the mutation era had five hand-rolling the spine), plus the
    /// grimoire repoints and the Rites tree registration.
    ///
    /// <para>All casts dispatch through <c>FireEvent</c> with the
    /// InputHandler's exact parameter conventions. That is the point:
    /// the mutation-era rites read DirectionX with
    /// <c>GetParameter&lt;int&gt;</c> against the wrong dictionary, so
    /// every direction-shaped rite was DEAD from a keybind (audit F17).
    /// These tests cast the way the player does.</para>
    /// </summary>
    public class RitesPortTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            AsciiFxBus.Clear();
            ResonanceSystem.ResetForTests();
            ResonanceSystem.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Resonance/Resonance.json")));
        }

        [TearDown]
        public void TearDown() => ResonanceSystem.ResetForTests();

        // ── Fixtures ─────────────────────────────────────────────────

        private static Entity Creature(Zone zone, string name, int x, int y, int hp = 400)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            foreach (var r in new[] { "ElectricResistance", "HeatResistance", "ColdResistance", "AcidResistance" })
                e.Statistics[r] = new Stat { Owner = e, Name = r, BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["Toughness"] = new Stat { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 30 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new Body());
            zone.AddEntity(e, x, y);
            return e;
        }

        /// <summary>Caster with a SkillsPart, the rite learned, and an
        /// inked book in inventory. Returns the book's charge part so
        /// tests can pin the ink invariants.</summary>
        private static GrimoireChargePart Caster<T>(
            Zone zone, int x, int y, out Entity caster, int charges = 10)
            where T : ConsumingRiteSkillBase, new()
        {
            caster = Creature(zone, "caster", x, y);
            caster.AddPart(new ActivatedAbilitiesPart());
            caster.AddPart(new SkillsPart());
            var inv = new InventoryPart { MaxWeight = 500 };
            caster.AddPart(inv);

            var book = new Entity { ID = "book", BlueprintName = "AnyRite" };
            book.AddPart(new RenderPart { DisplayName = "a rite" });
            var charge = new GrimoireChargePart { Charges = charges, MaxCharges = 10 };
            book.AddPart(charge);
            inv.AddObject(book);

            Assert.IsTrue(caster.GetPart<SkillsPart>().AddSkill(new T()),
                $"fixture: {typeof(T).Name} must attach");
            return charge;
        }

        private static (bool handled, bool blocks) Cast(
            Entity caster, Zone zone, string command, int dx = 0, int dy = 0)
        {
            var src = zone.GetEntityCell(caster);
            var cmd = GameEvent.New(command);
            cmd.SetParameter("Zone", (object)zone);
            cmd.SetParameter("RNG", (object)new System.Random(11));
            cmd.SetParameter("SourceCell", (object)src);
            cmd.SetParameter("DirectionX", dx);
            cmd.SetParameter("DirectionY", dy);
            caster.FireEvent(cmd);
            bool handled = cmd.Handled;
            bool blocks = cmd.GetParameter<bool>("BlocksTurnAdvance");
            cmd.Release();
            return (handled, blocks);
        }

        private static ActivatedAbility Ability(Entity caster, string command)
        {
            var abilities = caster.GetPart<ActivatedAbilitiesPart>();
            for (int i = 0; i < abilities.AbilityList.Count; i++)
                if (abilities.AbilityList[i].Command == command)
                    return abilities.AbilityList[i];
            return null;
        }

        // ── The F17 pin: direction-shaped rites live from keybinds ───

        [Test]
        public void HangingBolt_FiresFromKeybindParams_AndPinsTheMarked()
        {
            // The mutation read DirectionX from the wrong dictionary and
            // refused every keybind cast. The skill ctx lifts ints
            // properly — this cast is the proof.
            var zone = new Zone("Z");
            var ink = Caster<Rites_HangingBolt>(zone, 5, 5, out var caster);
            var target = Creature(zone, "victim", 8, 5);
            target.ApplyEffect(new ElectrifiedEffect(charge: 1.0f), caster, zone);

            var (handled, blocks) = Cast(caster, zone, "CommandHangingBolt", dx: 1);

            Assert.IsTrue(handled, "a direction-shaped rite must fire from keybind params");
            Assert.IsTrue(blocks);
            Assert.AreEqual(9, ink.Charges, "one charge of ink spent");
            Assert.IsFalse(target.HasEffect<ElectrifiedEffect>(), "the mark was consumed");
            Assert.IsTrue(target.HasEffect<ParalyzedEffect>(),
                "one mark buys 2 turns of paralysis");
            int lost = 400 - target.GetStatValue("Hitpoints", 0);
            Assert.Greater(lost, 0);
            Assert.LessOrEqual(lost, 2, "damage is FLAT 2 — marks buy the pin, not the hit");
            Assert.Greater(Ability(caster, "CommandHangingBolt").CooldownRemaining, 0);
        }

        // ── The two ink invariants ───────────────────────────────────

        [Test]
        public void EmptyCast_NeverSpendsInk()
        {
            var zone = new Zone("Z");
            var ink = Caster<Rites_StormAnvil>(zone, 5, 5, out var caster);
            // No creatures in radius 2.

            var (handled, _) = Cast(caster, zone, "CommandStormAnvil");

            Assert.IsFalse(handled, "no mark, no cast");
            Assert.AreEqual(10, ink.Charges, "ink is NEVER spent on a cast that finds nothing");
            Assert.AreEqual(0, Ability(caster, "CommandStormAnvil").CooldownRemaining,
                "the refusal is free");
        }

        [Test]
        public void DryPages_RefuseFree()
        {
            var zone = new Zone("Z");
            var ink = Caster<Rites_StormAnvil>(zone, 5, 5, out var caster, charges: 0);
            Creature(zone, "victim", 6, 5);

            var (handled, _) = Cast(caster, zone, "CommandStormAnvil");

            Assert.IsFalse(handled);
            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains("pages are dry")));
            Assert.AreEqual(0, Ability(caster, "CommandStormAnvil").CooldownRemaining);
        }

        // ── Cast cold is deliberately weak ───────────────────────────

        [Test]
        public void SunderingWord_CastCold_DealsBaseAndSkipsPayoff()
        {
            var zone = new Zone("Z");
            var ink = Caster<Rites_SunderingWord>(zone, 5, 5, out var caster);
            var target = Creature(zone, "victim", 6, 5);

            var (handled, _) = Cast(caster, zone, "CommandSunderingWord");

            Assert.IsTrue(handled, "a cold cast still goes out — and still costs ink");
            Assert.AreEqual(9, ink.Charges);
            int lost = 400 - target.GetStatValue("Hitpoints", 0);
            Assert.Greater(lost, 0);
            Assert.LessOrEqual(lost, 8, "multiplier 1.0 — base numbers only");
            Assert.IsFalse(target.HasEffect<BrokenEffect>(),
                "counter-check: the unmade payoff needs at least one mark");
        }

        // ── A fed rite pays out ──────────────────────────────────────

        [Test]
        public void StormAnvil_FedATarget_ConsumesTheMarkAndOutdamagesCold()
        {
            var zone = new Zone("Z");
            Caster<Rites_StormAnvil>(zone, 5, 5, out var caster);
            var fed = Creature(zone, "fed", 6, 5);
            fed.ApplyEffect(new ElectrifiedEffect(charge: 1.0f), caster, zone);

            var (handled, _) = Cast(caster, zone, "CommandStormAnvil");

            Assert.IsTrue(handled);
            Assert.IsFalse(fed.HasEffect<ElectrifiedEffect>(), "the mark was spent");
            int lost = 400 - fed.GetStatValue("Hitpoints", 0);
            Assert.Greater(lost, 4, "consumed resonance multiplies past the cold base of 4");
        }

        [Test]
        public void BloodletterLedger_HealsTheCaster_EvenOverACorpse()
        {
            var zone = new Zone("Z");
            Caster<Rites_BloodletterLedger>(zone, 5, 5, out var caster);
            var casterHp = caster.GetStat("Hitpoints");
            casterHp.BaseValue = 300; // wounded, so the heal is visible
            var frail = Creature(zone, "frail", 8, 5, hp: 1);
            frail.ApplyEffect(new ElectrifiedEffect(charge: 1.0f), caster, zone);

            var (handled, _) = Cast(caster, zone, "CommandBloodletterLedger", dx: 1);

            Assert.IsTrue(handled);
            Assert.LessOrEqual(frail.GetStatValue("Hitpoints", 1), 0, "the hit was lethal");
            Assert.AreEqual(304, casterHp.BaseValue,
                "4 HP per mark, paid even when the rite killed the debtor — " +
                "the earnings hook is not survivor-gated");
        }

        // ── The Self shape (ScaldingVeil) ────────────────────────────

        [Test]
        public void ScaldingVeil_RefusesDry_BoilsWet()
        {
            var zone = new Zone("Z");
            var ink = Caster<Rites_ScaldingVeil>(zone, 5, 5, out var caster);

            var (dryCast, _) = Cast(caster, zone, "CommandScaldingVeil");
            Assert.IsFalse(dryCast, "a dry caster has nothing to boil");
            Assert.AreEqual(10, ink.Charges, "the dry refusal costs no ink");
            Assert.AreEqual(0, Ability(caster, "CommandScaldingVeil").CooldownRemaining);

            caster.ApplyEffect(new WetEffect(0.9f), caster, zone);
            var (wetCast, _) = Cast(caster, zone, "CommandScaldingVeil");
            Assert.IsTrue(wetCast, "wet skin boils into the veil");
            Assert.AreEqual(9, ink.Charges);
            Assert.IsTrue(caster.HasEffect<ScaldingVeilEffect>());
            Assert.Greater(Ability(caster, "CommandScaldingVeil").CooldownRemaining, 0);
        }

        // ── Grimoire + tree registration ─────────────────────────────

        [Test]
        public void ARiteGrimoire_TeachesTheSkill_AndItsOwnInkFuelsTheCast()
        {
            var zone = new Zone("Z");
            var reader = Creature(zone, "reader", 5, 5);
            reader.AddPart(new ActivatedAbilitiesPart());
            reader.AddPart(new SkillsPart());
            reader.AddPart(new InventoryPart { MaxWeight = 500 });

            var book = _factory.CreateEntity("StormAnvilGrimoire");
            Assert.IsNotNull(book, "StormAnvilGrimoire blueprint must exist");
            reader.GetPart<InventoryPart>().AddObject(book);

            var read = GameEvent.New("InventoryAction");
            read.SetParameter("Command", "ReadGrimoire");
            read.SetParameter("Actor", (object)reader);
            book.FireEvent(read);
            read.Release();

            Assert.IsTrue(reader.GetPart<SkillsPart>().HasSkill("Rites_StormAnvil"),
                "the repointed grimoire teaches the rite as a skill");
            Assert.IsNotNull(Ability(reader, "CommandStormAnvil"));

            // The same book's ink now fuels the cast — the full loop.
            var bookInk = book.GetPart<GrimoireChargePart>();
            if (bookInk != null && bookInk.HasCharge)
            {
                int before = bookInk.Charges;
                Creature(zone, "victim", 6, 5);
                var (handled, _) = Cast(reader, zone, "CommandStormAnvil");
                Assert.IsTrue(handled, "read the book, cast the rite");
                Assert.AreEqual(before - 1, bookInk.Charges);
            }
        }

        [Test]
        public void TheRitesTree_IsRegistered_WithAllElevenPowers()
        {
            SkillRegistry.ResetForTests();
            SkillRegistry.EnsureInitialized();

            string[] all =
            {
                "Rites_StormAnvil", "Rites_HangingBolt", "Rites_RenderedSteam",
                "Rites_ScaldingVeil", "Rites_Fulmination", "Rites_ShatteredRime",
                "Rites_StillHeart", "Rites_VerdigrisBloom", "Rites_HollowCoin",
                "Rites_SunderingWord", "Rites_BloodletterLedger",
            };
            foreach (var cls in all)
                Assert.IsTrue(SkillRegistry.TryGetPowerByClass(cls, out _),
                    cls + " must be an SP-buyable row in the Rites tree");
        }
    }
}
