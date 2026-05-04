using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// T2.4 — TripWireTriggerPart: multi-segment line trap.
    ///
    /// Pins the line-coordination contract: stepping on ANY segment of
    /// a wire (segments share a WireGroupId) detonates the WHOLE wire,
    /// damaging actors at every segment's cell and removing all segments.
    /// Pins the group-id filter: a 4th segment with a different
    /// WireGroupId is unaffected. Pins the inherited TriggerFaction
    /// filter at the stepped-on segment (the one whose OnTrigger fires).
    /// </summary>
    [TestFixture]
    public class TripWireTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            AsciiFxBus.Clear();
        }

        // ====================================================================
        // 1. Trip one segment → all 3 segments removed (group consumption)
        // ====================================================================

        [Test]
        public void Trip_Single_RemovesAllSegmentsInGroup()
        {
            var zone = new Zone("TestZone");
            var seg1 = MakeSegment(zone, 5, 5, "wire-A", damage: 10);
            var seg2 = MakeSegment(zone, 6, 5, "wire-A", damage: 10);
            var seg3 = MakeSegment(zone, 7, 5, "wire-A", damage: 10);

            var stepper = MakeStepper(zone, 4, 5, hp: 100);
            // Step onto seg1's cell.
            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            Assert.IsNull(zone.GetEntityCell(seg1),
                "Tripped segment must be removed.");
            Assert.IsNull(zone.GetEntityCell(seg2),
                "Sibling segment must also be removed (group consumption).");
            Assert.IsNull(zone.GetEntityCell(seg3),
                "Third sibling segment must also be removed.");
        }

        // ====================================================================
        // 2. Trip damages the actor at the tripped cell
        // ====================================================================

        [Test]
        public void Trip_DamagesActorAtTrippedCell()
        {
            var zone = new Zone("TestZone");
            MakeSegment(zone, 5, 5, "wire-A", damage: 10);
            MakeSegment(zone, 6, 5, "wire-A", damage: 10);

            var stepper = MakeStepper(zone, 4, 5, hp: 100);
            int hpBefore = stepper.GetStatValue("Hitpoints");
            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);
            int hpAfter = stepper.GetStatValue("Hitpoints");

            Assert.Less(hpAfter, hpBefore,
                $"Stepper at the tripped cell must take damage. " +
                $"HP {hpBefore} → {hpAfter}.");
        }

        // ====================================================================
        // 3. Multi-cell coverage: actor at non-tripped segment's cell
        //    also takes damage (the LINE-vs-AOE distinction)
        // ====================================================================

        [Test]
        public void Trip_DamagesActorsAtAllSegmentCells()
        {
            // Pin the LINE coverage. Place a "victim" creature at a
            // non-tripped segment's cell and trip the wire from a
            // different segment. The victim must take damage —
            // proving the wire isn't just an AOE around the tripped
            // cell, but a coordinated line strike across all segments.
            var zone = new Zone("TestZone");
            MakeSegment(zone, 5, 5, "wire-B", damage: 10);
            MakeSegment(zone, 7, 5, "wire-B", damage: 10);

            var victim = MakeStepper(zone, 7, 5, hp: 50);
            int victimHpBefore = victim.GetStatValue("Hitpoints");

            // A different actor trips the wire at (5,5).
            var tripper = MakeStepper(zone, 4, 5, hp: 100, id: "tripper");
            MovementSystem.TryMove(tripper, zone, dx: 1, dy: 0);

            int victimHpAfter = victim.GetStatValue("Hitpoints");
            Assert.Less(victimHpAfter, victimHpBefore,
                $"Victim at non-tripped segment cell must take damage too " +
                $"(line-strike contract). HP {victimHpBefore} → {victimHpAfter}. " +
                $"If equal: wire is acting like an AOE around the tripped cell, " +
                $"not a coordinated line.");
        }

        // ====================================================================
        // 4. Counter-check: different group id → unaffected
        // ====================================================================

        [Test]
        public void Trip_DifferentGroupIds_NotAffected()
        {
            var zone = new Zone("TestZone");
            MakeSegment(zone, 5, 5, "wire-A", damage: 10);
            MakeSegment(zone, 6, 5, "wire-A", damage: 10);
            // A different wire entirely — same zone, different group.
            var otherWireSeg = MakeSegment(zone, 8, 5, "wire-B", damage: 10);

            var stepper = MakeStepper(zone, 4, 5, hp: 100);
            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            // Wire-A's segments at (5,5) and (6,5) should have detonated +
            // removed themselves. Wire-B's segment at (8,5) must STILL be
            // in the zone — different group, unaffected.
            Assert.IsNotNull(zone.GetEntityCell(otherWireSeg),
                "Segment in a different WireGroupId must NOT detonate when " +
                "another wire trips. (A bug here would mean every wire in " +
                "the zone fires together.)");
        }

        // ====================================================================
        // 5. End-to-end: trip wire damages occupants at OTHER segment cells
        //    AND emits floating numbers for them too
        //    Cold-eye Finding 9: pin the LINE-strike + visual-feedback
        //    contract end-to-end. The user originally reported "tripwire
        //    doesn't work" because the snapjaw at the FAR segment took
        //    silent damage (no number, no message). After the
        //    damage-numbers-on-ApplyDamage fix, the snapjaw's hit MUST
        //    surface as a visible number. This test pins that.
        // ====================================================================

        [Test]
        public void Trip_EmitsFloatingDamageNumber_ForEveryHitOccupant()
        {
            // 2 segments, victim planted at the far segment. Trip from
            // the near segment. Both the tripper (player) at near cell
            // AND the victim at the far cell take damage; both should
            // emit floating numbers.
            var zone = new Zone("TestZone");
            MakeSegment(zone, 5, 5, "wire-A", damage: 10);
            MakeSegment(zone, 7, 5, "wire-A", damage: 10);

            var victim = MakeStepper(zone, 7, 5, hp: 50, id: "victim");
            var tripper = MakeStepper(zone, 4, 5, hp: 100, id: "tripper");

            AsciiFxBus.Clear();
            MovementSystem.TryMove(tripper, zone, dx: 1, dy: 0);

            var requests = AsciiFxBus.Drain();
            // Damage 10 = 2 digits per hit; 2 hits = 4 Particle requests
            // total (one per digit per damaged actor).
            int particleCount = 0;
            foreach (var r in requests)
                if (r.Type == AsciiFxRequestType.Particle) particleCount++;

            Assert.AreEqual(4, particleCount,
                $"Trip on a 2-segment wire with damage=10 (two digits) damaging " +
                $"two occupants (tripper + victim) must emit 4 Particles total " +
                $"(2 actors × 2 digits each). Got {particleCount}. " +
                $"If 2: only one actor got a number — collateral damage at the " +
                $"non-tripped segment is invisible (the original user complaint).");
        }

        // ====================================================================
        // 6. Faction filter (inherited): faction-mate stepping on the
        //    LAID segment doesn't trigger
        // ====================================================================

        [Test]
        public void FactionMate_DoesNotTrigger()
        {
            var zone = new Zone("TestZone");
            var seg1 = MakeSegment(zone, 5, 5, "wire-A", damage: 10,
                triggerFaction: "Cultists");
            var seg2 = MakeSegment(zone, 6, 5, "wire-A", damage: 10,
                triggerFaction: "Cultists");

            var stepper = MakeStepper(zone, 4, 5, hp: 100, faction: "Cultists");
            int hpBefore = stepper.GetStatValue("Hitpoints");
            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);
            int hpAfter = stepper.GetStatValue("Hitpoints");

            // Cultist stepping on a Cultist-laid segment should not
            // trigger. Both segments must STILL exist (no detonation).
            Assert.AreEqual(hpBefore, hpAfter,
                "Faction-mate stepper must not take damage from own-faction " +
                $"wire. HP {hpBefore} → {hpAfter}.");
            Assert.IsNotNull(zone.GetEntityCell(seg1),
                "Faction-mate stepping on a wire segment must NOT detonate " +
                "the wire (inherited TriggerFaction filter).");
            Assert.IsNotNull(zone.GetEntityCell(seg2),
                "Sibling segment must remain intact too.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity MakeSegment(Zone zone, int x, int y,
            string groupId, int damage = 10, string damageAttribute = "Piercing",
            string triggerFaction = null)
        {
            string id = $"wire-seg-{x}-{y}";
            var e = new Entity { BlueprintName = "TestWireSeg", ID = id };
            e.AddPart(new RenderPart { DisplayName = "tripwire" });
            e.AddPart(new PhysicsPart { Solid = false });
            var seg = new TripWireTriggerPart
            {
                Damage = damage,
                DamageAttribute = damageAttribute,
                WireGroupId = groupId,
                TriggerFaction = triggerFaction,
            };
            e.AddPart(seg);
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity MakeStepper(Zone zone, int x, int y,
            int hp = 100, string faction = null, string id = "stepper-1")
        {
            var e = new Entity { BlueprintName = "TestStepper", ID = id };
            e.AddPart(new RenderPart { DisplayName = "stepper" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", Owner = e, BaseValue = hp, Min = 0, Max = hp };
            if (faction != null) e.SetTag("Faction", faction);
            zone.AddEntity(e, x, y);
            return e;
        }
    }
}
