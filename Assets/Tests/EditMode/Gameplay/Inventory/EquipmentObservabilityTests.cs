using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Observability-driven equipment-bonus tests. When the player
    /// equips an item with <c>EquippablePart.EquipBonuses</c>, those
    /// stat-bonus changes are now emitted under
    /// <c>category="equipment"</c>:
    ///
    /// <list type="bullet">
    ///   <item><c>StatBonusApplied</c> on equip — one per parsed stat:amount pair</item>
    ///   <item><c>StatBonusRemoved</c> on unequip — symmetric</item>
    ///   <item><c>SpeedPenaltyApplied</c>/<c>Removed</c> for armor speed penalties</item>
    /// </list>
    ///
    /// <para>Future "why did my Strength jump 4 points?" debug starts
    /// with <c>diag_query category=equipment kind=StatBonusApplied</c>,
    /// not a code-grep.</para>
    /// </summary>
    public class EquipmentObservabilityTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakeWearer(string id = "wearer", int strength = 12)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            e.Statistics["Strength"] = new Stat
            { Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat
            { Name = "Agility", BaseValue = 12, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat
            { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new InventoryPart());
            return e;
        }

        private static Entity MakeBonusItem(string id, string equipBonuses,
            string slot = "Hand")
        {
            var item = new Entity { ID = id, BlueprintName = id };
            item.Tags["Item"] = "";
            item.AddPart(new RenderPart { DisplayName = id.ToLower() });
            item.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            item.AddPart(new EquippablePart
            {
                Slot = slot,
                EquipBonuses = equipBonuses,
            });
            return item;
        }

        private static Entity MakeArmorWithSpeedPenalty(string id, int speedPenalty)
        {
            var item = new Entity { ID = id, BlueprintName = id };
            item.Tags["Item"] = "";
            item.AddPart(new RenderPart { DisplayName = id.ToLower() });
            item.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            item.AddPart(new EquippablePart { Slot = "Body" });
            item.AddPart(new ArmorPart { AV = 3, DV = 0, SpeedPenalty = speedPenalty });
            return item;
        }

        private static void DumpEquipmentRecords(string label)
        {
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "equipment",
                Limit = 20,
            }).Records;

            TestContext.WriteLine($"\n=== {label} ===");
            TestContext.WriteLine($"Records: {records.Count}");
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                TestContext.WriteLine(
                    $"  [{i}] {r.Kind,-22} actor={r.ActorId,-8} target={r.TargetId,-10} :: {r.PayloadJson}");
            }
        }

        // ── Tests ────────────────────────────────────────────────

        [Test]
        public void Equip_SingleStatBonus_EmitsStatBonusApplied()
        {
            var wearer = MakeWearer(strength: 12);
            var ring = MakeBonusItem("RingOfMight", "Strength:2");
            wearer.GetPart<InventoryPart>().AddObject(ring);
            Diag.ResetAll();  // ignore the AutoEquip path; focus on Equip

            InventorySystem.Equip(wearer, ring);

            DumpEquipmentRecords("equip ring with Strength:+2");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "equipment", Limit = 20,
            }).Records;
            // The equip command may auto-equip and run EquipBonuses twice;
            // assert AT LEAST one StatBonusApplied with Strength delta:2
            var strApplied = records.Where(r => r.Kind == "StatBonusApplied"
                && r.PayloadJson.Contains("\"statName\":\"Strength\"")).ToList();
            Assert.GreaterOrEqual(strApplied.Count, 1);
            StringAssert.Contains("\"delta\":2", strApplied[0].PayloadJson);
        }

        [Test]
        public void Equip_MultipleStatBonuses_EmitsOneRecordPerStat()
        {
            // EquipBonuses = "Strength:2,Agility:1" → two records.
            var wearer = MakeWearer();
            var amulet = MakeBonusItem("AmuletOfPower", "Strength:2,Agility:1");
            wearer.GetPart<InventoryPart>().AddObject(amulet);
            Diag.ResetAll();

            InventorySystem.Equip(wearer, amulet);

            DumpEquipmentRecords("equip amulet with two bonuses");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "equipment", Kind = "StatBonusApplied", Limit = 20,
            }).Records;
            Assert.AreEqual(2, records.Count);
            // Stat names should be exactly the two
            var statNames = records
                .Select(r => r.PayloadJson)
                .Where(p => p.Contains("\"statName\":"))
                .ToList();
            Assert.IsTrue(statNames.Any(p => p.Contains("\"statName\":\"Strength\"")));
            Assert.IsTrue(statNames.Any(p => p.Contains("\"statName\":\"Agility\"")));
        }

        [Test]
        public void Unequip_StatBonus_EmitsStatBonusRemoved()
        {
            var wearer = MakeWearer();
            var ring = MakeBonusItem("RingOfMight", "Strength:2");
            wearer.GetPart<InventoryPart>().AddObject(ring);
            InventorySystem.Equip(wearer, ring);
            Diag.ResetAll();  // discard equip records; focus on unequip

            InventorySystem.UnequipItem(wearer, ring);

            DumpEquipmentRecords("unequip ring (StatBonusRemoved)");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "equipment", Limit = 20,
            }).Records;
            var removed = records.Where(r => r.Kind == "StatBonusRemoved").ToList();
            Assert.AreEqual(1, removed.Count);
            StringAssert.Contains("\"statName\":\"Strength\"", removed[0].PayloadJson);
            StringAssert.Contains("\"delta\":-2", removed[0].PayloadJson);
        }

        [Test]
        public void EquipArmor_WithSpeedPenalty_EmitsSpeedPenaltyApplied()
        {
            var wearer = MakeWearer();
            var heavyArmor = MakeArmorWithSpeedPenalty("PlateMail", speedPenalty: 20);
            wearer.GetPart<InventoryPart>().AddObject(heavyArmor);
            Diag.ResetAll();

            InventorySystem.Equip(wearer, heavyArmor);

            DumpEquipmentRecords("equip plate mail with SpeedPenalty=20");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "equipment", Kind = "SpeedPenaltyApplied", Limit = 20,
            }).Records;
            Assert.GreaterOrEqual(records.Count, 1);
            StringAssert.Contains("\"delta\":20", records[0].PayloadJson);
        }

        [Test]
        public void Equip_StatNotPresentOnActor_DoesNotEmitForThatStat()
        {
            // Counter-check: items can claim "Ego:3" but if the wearer has
            // no Ego stat, EquipBonusUtility silently skips. The diag
            // emission must also skip — no false-positive record.
            var wearer = MakeWearer();
            // Note: MakeWearer doesn't give Ego stat
            var item = MakeBonusItem("EgoBoost", "Ego:5,Strength:1");
            wearer.GetPart<InventoryPart>().AddObject(item);
            Diag.ResetAll();

            InventorySystem.Equip(wearer, item);

            DumpEquipmentRecords("Ego claim but no Ego stat on wearer");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "equipment", Kind = "StatBonusApplied", Limit = 20,
            }).Records;
            // Strength should still emit; Ego should NOT
            Assert.IsTrue(records.Any(r =>
                r.PayloadJson.Contains("\"statName\":\"Strength\"")));
            Assert.IsFalse(records.Any(r =>
                r.PayloadJson.Contains("\"statName\":\"Ego\"")),
                "Stat that doesn't exist on the actor must not emit a record.");
        }

        [Test]
        public void Equip_MalformedBonusEntry_DoesNotEmitForThatEntry()
        {
            // Counter-check: an unparseable entry ("Strength:notANumber") is
            // silently skipped by EquipBonusUtility. The diag emission also
            // skips — only the parsed-valid entries emit.
            var wearer = MakeWearer();
            var item = MakeBonusItem("BrokenAmulet", "Strength:2,Agility:notANumber,Speed:1");
            wearer.GetPart<InventoryPart>().AddObject(item);
            Diag.ResetAll();

            InventorySystem.Equip(wearer, item);

            DumpEquipmentRecords("malformed bonus entry");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "equipment", Kind = "StatBonusApplied", Limit = 20,
            }).Records;
            // Strength + Speed should emit; Agility should NOT
            Assert.IsTrue(records.Any(r =>
                r.PayloadJson.Contains("\"statName\":\"Strength\"")));
            Assert.IsTrue(records.Any(r =>
                r.PayloadJson.Contains("\"statName\":\"Speed\"")));
            Assert.IsFalse(records.Any(r =>
                r.PayloadJson.Contains("\"statName\":\"Agility\"")),
                "Malformed entry must not emit a record.");
        }

        [Test]
        public void EquipThenUnequip_Symmetric_NetZeroBonus()
        {
            // Counter-check on Bug #2 (Remove-from-equipped permanent bonus
            // retention, fixed earlier). Equip + Unequip should produce
            // symmetric records with net-zero delta.
            var wearer = MakeWearer(strength: 12);
            var ring = MakeBonusItem("RingOfMight", "Strength:3");
            wearer.GetPart<InventoryPart>().AddObject(ring);

            int statBefore = wearer.GetStat("Strength").Bonus;
            InventorySystem.Equip(wearer, ring);
            int statAfterEquip = wearer.GetStat("Strength").Bonus;
            InventorySystem.UnequipItem(wearer, ring);
            int statAfterUnequip = wearer.GetStat("Strength").Bonus;

            DumpEquipmentRecords("equip+unequip symmetric round trip");

            Assert.AreEqual(statBefore + 3, statAfterEquip,
                "Equip should grant +3 Strength bonus.");
            Assert.AreEqual(statBefore, statAfterUnequip,
                "Unequip should fully reverse the bonus — net zero.");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "equipment", Limit = 20,
            }).Records;
            // At minimum: one StatBonusApplied + one StatBonusRemoved
            Assert.IsTrue(records.Any(r => r.Kind == "StatBonusApplied"));
            Assert.IsTrue(records.Any(r => r.Kind == "StatBonusRemoved"));
        }
    }
}
