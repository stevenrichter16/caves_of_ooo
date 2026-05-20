using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LA — absurd-property coats. 5 liquids, each with a qualitatively
    /// NEW mechanic (not another stat dial): element immunity, reflect,
    /// HP rewind, knockback, undying+pacifist. Same RED→GREEN content +
    /// behavior pattern as LiquidBuffsTests.
    /// </summary>
    public class LiquidAbsurdTests
    {
        private const string DefDir = "Resources/Content/Data/LiquidDefinitions";

        [SetUp]
        public void Setup() { MessageLog.Clear(); Diag.ResetAll(); }

        [TearDown]
        public void TearDown() => LiquidRegistry.ResetForTests();

        private static LiquidDefinition LoadFromFile(string id)
        {
            string path = Path.Combine(UnityEngine.Application.dataPath, DefDir, id + ".json");
            Assert.IsTrue(File.Exists(path),
                $"Shipped absurd-liquid JSON missing: Assets/{DefDir}/{id}.json");
            LiquidRegistry.InitializeFromJsonSources(new[] { File.ReadAllText(path) });
            var def = LiquidRegistry.Get(id);
            Assert.IsNotNull(def, $"{id}.json failed to parse/register.");
            return def;
        }

        private static Entity MakeCreature(int hpMax = 400)
        {
            var e = new Entity { ID = "c", BlueprintName = "C" };
            e.Tags["Creature"] = "";
            void S(string n, int v, int max = 400) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", hpMax, hpMax); S("Toughness", 12);
            S("Agility", 14); S("DV", 6); S("AV", 0);
            S("HeatResistance", 0); S("ColdResistance", 0);
            S("ElectricResistance", 0); S("AcidResistance", 0);
            e.AddPart(new RenderPart { DisplayName = "c" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        // ════════════════ LA.2 — Veined Pulse Mycelium (ImmuneElement) ════════════════

        [Test]
        public void VeinedPulseMycelium_Json_GrantsElectricImmunity()
        {
            // Content RED: JSON file present, parses, declares ImmuneElement="Electric".
            // Lore anchor: §L5 Branchwork — fungal cognition routes electrical
            // discharge around obstacles (the mycelium is the obstacle the
            // discharge routes around).
            var d = LoadFromFile("veined-pulse-mycelium");
            Assert.AreEqual("Electric", d.ImmuneElement,
                "veined-pulse declares Electric as the immune element");
            Assert.AreEqual("pulse-veined", d.Adjective);
        }

        [Test]
        public void VeinedPulseCoat_NullifiesElectricDamage_Completely()
        {
            // Behavior RED before the OnBeforeTakeDamage early-out is added.
            // Pin: an Electric hit on a veined-pulse-coated creature deals 0,
            // not "reduced" — distinct from resistance, which scales the
            // amount. The mycelium routes the current around the host.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""veined-pulse-mycelium"", ""Adjective"":""pulse-veined"",
                ""Fluidity"":3, ""Evaporativity"":2,
                ""ImmuneElement"":""Electric"" } ] }");
            var c = MakeCreature(hpMax: 200);
            int hp0 = c.GetStatValue("Hitpoints");
            c.ApplyEffect(new LiquidCoveredEffect("veined-pulse-mycelium", 30));
            var d = new Damage(75); d.AddAttribute("Electric");
            CombatSystem.ApplyDamage(c, d, null, null);
            Assert.AreEqual(hp0, c.GetStatValue("Hitpoints"),
                "Electric damage must be fully nullified (Amount→0)");
        }

        [Test]
        public void VeinedPulseCoat_NonImmuneElement_StillDamages_Counter()
        {
            // Counter: only the named element is nullified. A Heat hit on a
            // veined-pulse (electric-immune) creature lands normally.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""veined-pulse-mycelium"", ""Adjective"":""pulse-veined"",
                ""Fluidity"":3, ""Evaporativity"":2,
                ""ImmuneElement"":""Electric"" } ] }");
            var c = MakeCreature(hpMax: 200);
            int hp0 = c.GetStatValue("Hitpoints");
            c.ApplyEffect(new LiquidCoveredEffect("veined-pulse-mycelium", 30));
            var d = new Damage(50); d.AddAttribute("Heat");
            CombatSystem.ApplyDamage(c, d, null, null);
            Assert.Less(c.GetStatValue("Hitpoints"), hp0,
                "non-immune element (Heat) must still damage");
        }

        [Test]
        public void NonImmuneCoat_ElectricHit_StillDamages_Counter()
        {
            // Counter: a coat without ImmuneElement does NOT confer immunity.
            // A water coat (Conductivity=100, no ImmuneElement) takes the
            // hit through the existing conductivity amplifier — not nullified.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""water"", ""Adjective"":""wet"", ""Conductivity"":100,
                ""Fluidity"":30, ""Evaporativity"":20 } ] }");
            var c = MakeCreature(hpMax: 200);
            int hp0 = c.GetStatValue("Hitpoints");
            c.ApplyEffect(new LiquidCoveredEffect("water", 30));
            var d = new Damage(20); d.AddAttribute("Electric");
            CombatSystem.ApplyDamage(c, d, null, null);
            Assert.Less(c.GetStatValue("Hitpoints"), hp0,
                "water coat has no ImmuneElement — Electric damage still lands");
        }

        [Test]
        public void VeinedPulseCoat_ImmunityFires_EmitsDiag()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""veined-pulse-mycelium"", ""Adjective"":""pulse-veined"",
                ""Fluidity"":3, ""Evaporativity"":2,
                ""ImmuneElement"":""Electric"" } ] }");
            var c = MakeCreature(hpMax: 200);
            c.ApplyEffect(new LiquidCoveredEffect("veined-pulse-mycelium", 30));
            var d = new Damage(50); d.AddAttribute("Electric");
            CombatSystem.ApplyDamage(c, d, null, null);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "ElementImmunity", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count, "one ElementImmunity record per nullified hit");
            StringAssert.Contains("\"liquidId\":\"veined-pulse-mycelium\"", recs[0].PayloadJson);
            StringAssert.Contains("\"element\":\"Electric\"", recs[0].PayloadJson);
        }

        [Test]
        public void VeinedPulseCoat_AliasCollapse_LightningTaggedHit_AlsoNullified()
        {
            // Adversarial: spells/weapons tag "Lightning", "Shock",
            // "Electricity" — all collapse to the Electric flag. The
            // immunity gate must match the FLAG, not the literal string,
            // mirroring the LQ.5/Fix-1 alias-collapse precedent.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""veined-pulse-mycelium"", ""Adjective"":""pulse-veined"",
                ""Fluidity"":3, ""Evaporativity"":2,
                ""ImmuneElement"":""Electric"" } ] }");
            var c = MakeCreature(hpMax: 200);
            int hp0 = c.GetStatValue("Hitpoints");
            c.ApplyEffect(new LiquidCoveredEffect("veined-pulse-mycelium", 30));
            // Lightning is a damage-attribute alias for Electric (Damage.cs:214).
            var d = new Damage(40); d.AddAttribute("Lightning");
            CombatSystem.ApplyDamage(c, d, null, null);
            Assert.AreEqual(hp0, c.GetStatValue("Hitpoints"),
                "Lightning alias should also be nullified by Electric immunity");
        }

        // ════════════════ LA.3 — Choir-Mirror Mucilage (ReflectPercent) ════════════════

        [Test]
        public void ChoirMirrorMucilage_Json_Reflects50Percent()
        {
            // §L4: the Choir's "external digestion" is bidirectional.
            // What is taken in is also given back. Mirror-mucilage
            // declares ReflectPercent=50.
            var d = LoadFromFile("choir-mirror-mucilage");
            Assert.AreEqual(50, d.ReflectPercent,
                "choir-mirror-mucilage reflects 50% of incoming damage");
            Assert.AreEqual("mirror-glazed", d.Adjective);
            Assert.IsTrue(d.Sticky, "mucilage is sticky (it is mucilage)");
        }

        [Test]
        public void ChoirMirrorCoat_AttackerTakesReflectedDamage()
        {
            // Behavior pin: when a melee/spell hit lands on a mirror-
            // coated target for N damage, the attacker takes N*pct/100
            // back via a separate ApplyDamage call.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""choir-mirror-mucilage"", ""Adjective"":""mirror-glazed"",
                ""Fluidity"":4, ""Evaporativity"":3,
                ""ReflectPercent"":50 } ] }");
            var defender = MakeCreature(hpMax: 200);
            var attacker = MakeCreature(hpMax: 200);
            defender.ApplyEffect(new LiquidCoveredEffect("choir-mirror-mucilage", 30));
            int attackerHpBefore = attacker.GetStatValue("Hitpoints");
            int defenderHpBefore = defender.GetStatValue("Hitpoints");
            // Attacker hits defender for 40.
            CombatSystem.ApplyDamage(defender, new Damage(40), attacker, null);
            int attackerLost = attackerHpBefore - attacker.GetStatValue("Hitpoints");
            Assert.AreEqual(20, attackerLost,
                "attacker takes 50% of 40 = 20 damage back");
            Assert.AreEqual(40, defenderHpBefore - defender.GetStatValue("Hitpoints"),
                "defender still takes the full 40 (reflect doesn't substitute)");
        }

        [Test]
        public void ChoirMirrorCoat_NullSource_NoReflect_Counter()
        {
            // Counter / cycle-breaker prep: environmental damage with
            // null source (a trap, a status-tick) must NOT reflect.
            // Without this gate, the second cycle of two-mirror-coated
            // entities would infinite-loop (the reflected hit's null
            // source is the very thing this guard tests).
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""choir-mirror-mucilage"", ""Adjective"":""mirror-glazed"",
                ""Fluidity"":4, ""Evaporativity"":3,
                ""ReflectPercent"":50 } ] }");
            var defender = MakeCreature(hpMax: 200);
            defender.ApplyEffect(new LiquidCoveredEffect("choir-mirror-mucilage", 30));
            int hp0 = defender.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(defender, new Damage(40), source: null, zone: null);
            Assert.AreEqual(hp0 - 40, defender.GetStatValue("Hitpoints"),
                "defender takes the hit (null source = environmental)");
            // No crash; no reflect emitted.
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "DamageReflected", Limit = 5 }).Records;
            Assert.AreEqual(0, recs.Count, "null-source hit does not reflect");
        }

        [Test]
        public void ChoirMirrorCoat_TwoMirrors_CycleBreakerHolds_Adversarial()
        {
            // Adversarial: two mirror-coated entities. Attacker hits
            // defender for 40. Defender reflects 20 back. Attacker's
            // OWN mirror coat would naively reflect 50% of 20 = 10 back,
            // and on and on — infinite loop. The cycle-breaker
            // (source=null on reflected damage) must prevent the second
            // bounce.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""choir-mirror-mucilage"", ""Adjective"":""mirror-glazed"",
                ""Fluidity"":4, ""Evaporativity"":3,
                ""ReflectPercent"":50 } ] }");
            var defender = MakeCreature(hpMax: 200);
            var attacker = MakeCreature(hpMax: 200);
            defender.ApplyEffect(new LiquidCoveredEffect("choir-mirror-mucilage", 30));
            attacker.ApplyEffect(new LiquidCoveredEffect("choir-mirror-mucilage", 30));
            int attackerHpBefore = attacker.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(defender, new Damage(40), attacker, null);
            int attackerLost = attackerHpBefore - attacker.GetStatValue("Hitpoints");
            // Exactly one reflect — defender→attacker for 20. Attacker's
            // mirror sees Source=null on the reflected hit, bails, no
            // further bounce.
            Assert.AreEqual(20, attackerLost,
                "exactly one reflect: defender→attacker for 20, no infinite bounce");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "DamageReflected", Limit = 10 }).Records;
            Assert.AreEqual(1, recs.Count,
                "exactly one DamageReflected record (the first hop)");
        }

        [Test]
        public void NonReflectCoat_TakesHit_NoReflect_Counter()
        {
            // Counter: a coat without ReflectPercent (water) does not
            // reflect — sanity check that the gate is conditional.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""water"", ""Adjective"":""wet"",
                ""Fluidity"":30, ""Evaporativity"":20 } ] }");
            var defender = MakeCreature(hpMax: 200);
            var attacker = MakeCreature(hpMax: 200);
            defender.ApplyEffect(new LiquidCoveredEffect("water", 30));
            int attackerHpBefore = attacker.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(defender, new Damage(40), attacker, null);
            Assert.AreEqual(attackerHpBefore, attacker.GetStatValue("Hitpoints"),
                "water coat does NOT reflect");
        }

        [Test]
        public void ChoirMirrorCoat_SelfDamage_DoesNotReflect_Counter()
        {
            // Counter: an entity damaging itself (Source==Target — e.g.
            // a poison tick that passes self as Source) must not reflect
            // onto itself. Self-reflect would be a confusing zero-effect
            // re-application via ApplyDamage of the reduced amount.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""choir-mirror-mucilage"", ""Adjective"":""mirror-glazed"",
                ""Fluidity"":4, ""Evaporativity"":3,
                ""ReflectPercent"":50 } ] }");
            var c = MakeCreature(hpMax: 200);
            c.ApplyEffect(new LiquidCoveredEffect("choir-mirror-mucilage", 30));
            int hp0 = c.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(c, new Damage(20), source: c, zone: null);
            Assert.AreEqual(hp0 - 20, c.GetStatValue("Hitpoints"),
                "self-damage lands once, no self-reflect");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "DamageReflected", Limit = 5 }).Records;
            Assert.AreEqual(0, recs.Count, "Source==Target does not reflect");
        }

        [Test]
        public void ChoirMirrorCoat_ReflectFires_EmitsDiag()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""choir-mirror-mucilage"", ""Adjective"":""mirror-glazed"",
                ""Fluidity"":4, ""Evaporativity"":3,
                ""ReflectPercent"":50 } ] }");
            var defender = MakeCreature(hpMax: 200);
            var attacker = MakeCreature(hpMax: 200);
            defender.ApplyEffect(new LiquidCoveredEffect("choir-mirror-mucilage", 30));
            CombatSystem.ApplyDamage(defender, new Damage(60), attacker, null);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "DamageReflected", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"liquidId\":\"choir-mirror-mucilage\"", recs[0].PayloadJson);
            StringAssert.Contains("\"originalAmount\":60", recs[0].PayloadJson);
            StringAssert.Contains("\"reflectedAmount\":30", recs[0].PayloadJson);
            StringAssert.Contains("\"percent\":50", recs[0].PayloadJson);
        }

        // ════════════════ LA.4 — Felling-Counter Resin (HpRewindOnTurnEnd) ════════════════

        [Test]
        public void FellingCounterResin_Json_DeclaresHpRewind()
        {
            // §L3: Felling-Counter Antikythera — pre-Felling time-tech.
            // The coat's mechanic is "this turn didn't happen for HP."
            var d = LoadFromFile("felling-counter-resin");
            Assert.IsTrue(d.HpRewindOnTurnEnd,
                "felling-counter declares HpRewindOnTurnEnd");
            Assert.AreEqual("time-locked", d.Adjective);
        }

        [Test]
        public void FellingCounterCoat_RewindsDamageTakenDuringTurn()
        {
            // Behavior pin: apply coat at full HP (snapshot taken). Take
            // damage. OnTurnEnd → HP restored to snapshot.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""felling-counter-resin"", ""Adjective"":""time-locked"",
                ""Fluidity"":5, ""Evaporativity"":3,
                ""HpRewindOnTurnEnd"":true } ] }");
            var c = MakeCreature(hpMax: 200);
            var coat = new LiquidCoveredEffect("felling-counter-resin", 30);
            c.ApplyEffect(coat);
            int snapshotHp = c.GetStatValue("Hitpoints");
            // Wound the wearer mid-turn.
            CombatSystem.ApplyDamage(c, new Damage(80), null, null);
            Assert.AreEqual(snapshotHp - 80, c.GetStatValue("Hitpoints"),
                "damage lands during the turn");
            // End of turn: rewind.
            coat.OnTurnEnd(c);
            Assert.AreEqual(snapshotHp, c.GetStatValue("Hitpoints"),
                "HP restored to pre-turn snapshot");
            Assert.AreEqual(snapshotHp, coat.RewindSnapshotHp,
                "snapshot value preserved (read for next-turn comparison)");
        }

        [Test]
        public void FellingCounterCoat_OnTurnStart_ResnapshotsHp()
        {
            // Multi-turn pin: snapshot refreshes each turn start, so
            // damage taken last turn is locked in (the OnTurnEnd from
            // last turn ran), and damage this turn gets rewound.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""felling-counter-resin"", ""Adjective"":""time-locked"",
                ""Fluidity"":5, ""Evaporativity"":3,
                ""HpRewindOnTurnEnd"":true } ] }");
            var c = MakeCreature(hpMax: 200);
            var coat = new LiquidCoveredEffect("felling-counter-resin", 30);
            c.ApplyEffect(coat);
            // Pretend we took permanent damage last turn:
            c.GetStat("Hitpoints").BaseValue = 150;
            // New turn starts:
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            Assert.AreEqual(150, coat.RewindSnapshotHp,
                "snapshot refreshes to current HP at OnTurnStart");
            // Take more damage this turn:
            CombatSystem.ApplyDamage(c, new Damage(40), null, null);
            Assert.AreEqual(110, c.GetStatValue("Hitpoints"));
            // End-of-turn rewind → back to 150, not 200:
            coat.OnTurnEnd(c);
            Assert.AreEqual(150, c.GetStatValue("Hitpoints"),
                "rewind targets the OnTurnStart snapshot, not the OnApply snapshot");
        }

        [Test]
        public void FellingCounterCoat_RewindCappedAtMax()
        {
            // Pin: rewind can't exceed Stat.Max. If somehow snapshot > Max
            // (e.g., Max was lowered between snapshot and OnTurnEnd), the
            // restore caps at Max — same pattern LB.3 uses for heals.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""felling-counter-resin"", ""Adjective"":""time-locked"",
                ""Fluidity"":5, ""Evaporativity"":3,
                ""HpRewindOnTurnEnd"":true } ] }");
            var c = MakeCreature(hpMax: 200);
            var coat = new LiquidCoveredEffect("felling-counter-resin", 30);
            c.ApplyEffect(coat);
            // Artificially set snapshot to 500 (impossible normally, but
            // tests the cap path).
            coat.RewindSnapshotHp = 500;
            c.GetStat("Hitpoints").BaseValue = 100;
            coat.OnTurnEnd(c);
            Assert.AreEqual(200, c.GetStatValue("Hitpoints"),
                "rewind capped at Stat.Max");
        }

        [Test]
        public void FellingCounterCoat_IntraTurnHeal_NotUndone_Counter()
        {
            // Counter: if the wearer ENDED the turn with MORE HP than
            // the snapshot (intra-turn healing), the rewind preserves
            // the heal — rewind only undoes NET damage. Otherwise
            // felling-counter would be a heal-eater, which it isn't.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""felling-counter-resin"", ""Adjective"":""time-locked"",
                ""Fluidity"":5, ""Evaporativity"":3,
                ""HpRewindOnTurnEnd"":true } ] }");
            var c = MakeCreature(hpMax: 400);
            c.GetStat("Hitpoints").BaseValue = 100;
            var coat = new LiquidCoveredEffect("felling-counter-resin", 30);
            c.ApplyEffect(coat); // snapshot = 100
            // Heal mid-turn (no damage taken).
            c.GetStat("Hitpoints").BaseValue = 180;
            coat.OnTurnEnd(c);
            Assert.AreEqual(180, c.GetStatValue("Hitpoints"),
                "intra-turn heal preserved — rewind only undoes net damage");
        }

        [Test]
        public void FellingCounterCoat_DeadAtTurnEnd_NoResurrect_Counter()
        {
            // Counter: don't resurrect. A wearer who died mid-turn
            // (HP ≤ 0 at OnTurnEnd) stays dead. The Memory-Bath
            // (LB.5) is the path for one-shot resurrection; felling-
            // counter only undoes damage on a still-alive wearer.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""felling-counter-resin"", ""Adjective"":""time-locked"",
                ""Fluidity"":5, ""Evaporativity"":3,
                ""HpRewindOnTurnEnd"":true } ] }");
            var c = MakeCreature(hpMax: 200);
            var coat = new LiquidCoveredEffect("felling-counter-resin", 30);
            c.ApplyEffect(coat);
            // Snapshot is 200. Kill the wearer (bypass ApplyDamage, set
            // HP directly to simulate post-death state).
            c.GetStat("Hitpoints").BaseValue = 0;
            coat.OnTurnEnd(c);
            Assert.AreEqual(0, c.GetStatValue("Hitpoints"),
                "rewind does not resurrect — dead stays dead");
        }

        [Test]
        public void NonRewindCoat_DamageStays_Counter()
        {
            // Counter: a coat without HpRewindOnTurnEnd does NOT undo
            // damage. Pinning the conditional.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""water"", ""Adjective"":""wet"",
                ""Fluidity"":30, ""Evaporativity"":20 } ] }");
            var c = MakeCreature(hpMax: 200);
            var coat = new LiquidCoveredEffect("water", 30);
            c.ApplyEffect(coat);
            CombatSystem.ApplyDamage(c, new Damage(50), null, null);
            int afterHit = c.GetStatValue("Hitpoints");
            coat.OnTurnEnd(c);
            Assert.AreEqual(afterHit, c.GetStatValue("Hitpoints"),
                "non-rewind coat (water) does not restore HP");
        }

        [Test]
        public void FellingCounterCoat_RewindFires_EmitsDiag()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""felling-counter-resin"", ""Adjective"":""time-locked"",
                ""Fluidity"":5, ""Evaporativity"":3,
                ""HpRewindOnTurnEnd"":true } ] }");
            var c = MakeCreature(hpMax: 200);
            var coat = new LiquidCoveredEffect("felling-counter-resin", 30);
            c.ApplyEffect(coat);
            CombatSystem.ApplyDamage(c, new Damage(60), null, null);
            coat.OnTurnEnd(c);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "HpRewound", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"liquidId\":\"felling-counter-resin\"", recs[0].PayloadJson);
            StringAssert.Contains("\"hpBefore\":140", recs[0].PayloadJson); // 200-60=140
            StringAssert.Contains("\"hpAfter\":200", recs[0].PayloadJson);
            StringAssert.Contains("\"gained\":60", recs[0].PayloadJson);
        }

        [Test]
        public void FellingCounterCoat_RewindRunsBeforeDryDown_Sequencing()
        {
            // Sequencing pin (pre-flagged 🟡 in §16.7): the rewind must
            // run BEFORE the dry-down. Otherwise a coat that exactly
            // dried-down to 0 this turn would have its rewind skipped
            // (Duration=0 might cause early exit). Verify both run.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""felling-counter-resin"", ""Adjective"":""time-locked"",
                ""Fluidity"":5, ""Evaporativity"":3,
                ""HpRewindOnTurnEnd"":true } ] }");
            var c = MakeCreature(hpMax: 200);
            var coat = new LiquidCoveredEffect("felling-counter-resin", 30);
            c.ApplyEffect(coat);
            int beforeAmount = coat.Amount;
            CombatSystem.ApplyDamage(c, new Damage(50), null, null);
            coat.OnTurnEnd(c);
            // Both happened: HP restored AND Amount decremented.
            Assert.AreEqual(c.GetStat("Hitpoints").Max, c.GetStatValue("Hitpoints"),
                "rewind happened");
            Assert.Less(coat.Amount, beforeAmount,
                "dry-down ran AFTER rewind (Amount decremented)");
        }
    }
}
