using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using NUnit.Framework;

namespace CavesOfOoo.Tests.EditMode.Gameplay.Save
{
    /// <summary>
    /// Deep-dive adversarial pass on <c>SaveSystem.cs</c> per Methodology
    /// Template §3.9. Targets the round-trip identity surface NOT covered
    /// by earlier surgical passes (Spec / CoverageGap / Adversarial,
    /// 56 tests already shipped).
    ///
    /// Per §3.9.2: tests written WITHOUT reading SaveSystem.cs's
    /// implementation. Predictions stated in xml-doc with CONFIDENCE.
    /// LOW-confidence predictions are the gold.
    ///
    /// See <c>Docs/SAVESYSTEM-DEEP-DIVE-AUDIT.md</c> for the gap analysis
    /// + outcome classification.
    /// </summary>
    [TestFixture]
    public class SaveSystemDeepDiveTests
    {
        // ============================================================
        // Helpers — match the existing SaveSystemSpecTests pattern.
        // ============================================================

        private static Entity MakeCreature(string id, string blueprint = "TestCreature", bool isPlayer = false)
        {
            var e = new Entity { ID = id, BlueprintName = blueprint };
            e.AddPart(new RenderPart { DisplayName = blueprint, RenderString = "@", ColorString = "&W" });
            e.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 0, Max = 1000, Owner = e };
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10, Owner = e };
            if (isPlayer) e.SetTag("Player");
            return e;
        }

        private static OverworldZoneManager MakeManagerWithZone(Zone zone)
        {
            var m = new OverworldZoneManager(null, 12345);
            m.ReplaceLoadedState(
                new Dictionary<string, Zone> { { zone.ZoneID, zone } },
                zone.ZoneID,
                new Dictionary<string, List<ZoneConnection>>());
            return m;
        }

        private static GameSessionState RoundTrip(
            Entity player, Zone zone, OverworldZoneManager manager, TurnManager turns)
        {
            var state = GameSessionState.Capture("test-game", "test-version", manager, turns, player);
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            state.Save(writer);
            stream.Position = 0;
            var reader = new SaveReader(stream, null);
            return GameSessionState.Load(reader);
        }

        private static (Entity player, Zone zone, OverworldZoneManager mgr, TurnManager turns) MakeMinimalState(
            string playerID = "player-1", string zoneID = "Overworld.10.10.0")
        {
            var player = MakeCreature(playerID, "Player", isPlayer: true);
            var zone = new Zone(zoneID);
            zone.AddEntity(player, 1, 1);
            var mgr = MakeManagerWithZone(zone);
            var turns = new TurnManager();
            turns.RestoreSavedState(0, waitingForInput: true, currentActor: player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 }
                });
            return (player, zone, mgr, turns);
        }

        /// <summary>Find entity by ID across the active zone's cells.</summary>
        private static Entity FindEntityByID(Zone zone, string id)
        {
            foreach (var entity in zone.GetAllEntities())
                if (entity.ID == id) return entity;
            return null;
        }

        /// <summary>Walk a Body's part tree looking for a part with the given Manager string.</summary>
        private static BodyPart FindPartByManager(Body body, string managerID)
        {
            foreach (var part in body.GetParts())
                if (part.Manager == managerID) return part;
            return null;
        }

        // ============================================================
        // 1. MutationsPart manager IDs preserve dynamic body parts
        // ============================================================

        /// <summary>
        /// PRED: A creature with a dynamic body part added via
        /// <c>body.AddPartByManager(managerID, parent, newPart)</c> has that
        /// part's <c>Manager</c> string preserved across save/load. Walking
        /// the loaded body tree should still find a part keyed to the same
        /// manager ID.
        /// CONFIDENCE: LOW — mutations are dynamic add/remove and serialization
        /// of Manager-keyed dynamic state has no direct existing test.
        /// </summary>
        [Test]
        public void DeepDive_DynamicBodyPart_AddedViaManagerID_PreservesManagerString()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var body = new Body();
            body.SetBody(AnatomyFactory.CreateHumanoid());
            player.AddPart(body);

            var hand = body.GetPartByType("Hand");
            Assert.IsNotNull(hand, "Test setup: humanoid should have a Hand");

            // Add a dynamic part with a known manager ID
            var dynamicArm = new BodyPart { Type = "Arm", Name = "extra_arm" };
            body.AddPartByManager("ExtraArmMutation_42", hand, dynamicArm);

            int armsBeforeSave = body.CountParts("Arm");

            var loaded = RoundTrip(player, zone, mgr, turns);
            var loadedPlayer = FindEntityByID(loaded.ZoneManager.ActiveZone, "player-1");
            Assert.IsNotNull(loadedPlayer);
            var loadedBody = loadedPlayer.GetPart<Body>();
            Assert.IsNotNull(loadedBody, "Body part must round-trip");

            int armsAfterLoad = loadedBody.CountParts("Arm");
            Assert.AreEqual(armsBeforeSave, armsAfterLoad,
                $"Dynamic-arm count should be preserved (was {armsBeforeSave}, got {armsAfterLoad})");

            var resolved = FindPartByManager(loadedBody, "ExtraArmMutation_42");
            Assert.IsNotNull(resolved,
                "Walking loaded parts by Manager string should resolve the dynamic part");
            Assert.AreEqual("extra_arm", resolved.Name,
                "Manager-keyed part should have its original Name");
        }

        // ============================================================
        // 2. Entity.ID collision on load
        // ============================================================

        /// <summary>
        /// PRED: If a save file contains two entities with the same ID,
        /// the loader either (a) rejects with a clear error referencing
        /// "id"/"dup"/"collision", or (b) loads them as distinct instances.
        /// It does NOT silently reuse one instance for both — that would
        /// produce a corrupted entity graph.
        /// CONFIDENCE: LOW — ID-collision behavior unspecified.
        /// </summary>
        [Test]
        public void DeepDive_EntityIDCollision_OnLoad_DoesNotSilentlyMergeInstances()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var clone1 = MakeCreature("collision-id", "Snapjaw");
            var clone2 = MakeCreature("collision-id", "Snapjaw");
            zone.AddEntity(clone1, 5, 5);
            zone.AddEntity(clone2, 6, 5);

            try
            {
                var loaded = RoundTrip(player, zone, mgr, turns);
                var lZone = loaded.ZoneManager.ActiveZone;
                var snapjaws = lZone.GetAllEntities()
                    .Where(e => e.BlueprintName == "Snapjaw").ToList();
                Assert.AreEqual(2, snapjaws.Count,
                    $"Two pre-save Snapjaws with colliding IDs should yield TWO loaded entities. " +
                    $"Got {snapjaws.Count} — silent-merge bug suspected.");
                Assert.AreNotSame(snapjaws[0], snapjaws[1],
                    "Two loaded entities must be distinct instances even with same ID");
            }
            catch (System.Exception ex)
            {
                Assert.That(ex.Message.ToLower(),
                    Does.Contain("id").Or.Contain("dup").Or.Contain("collision").Or.Contain("entity"),
                    $"Loader threw on ID collision (acceptable) but message lacks " +
                    $"'id'/'dup'/'collision'/'entity' for diagnostics. Got: {ex.Message}");
            }
        }

        // ============================================================
        // 3. CorpsePart fields round-trip after cold load
        // ============================================================

        /// <summary>
        /// PRED: A creature with a CorpsePart has its
        /// <c>CorpseChance</c> and <c>CorpseBlueprint</c> fields
        /// preserved across save/load. Spec_StaticFactories_NotRequiredAtLoadTime
        /// covers the GENERAL case; this checks the SPECIFIC CorpsePart fields
        /// are preserved (the saga that motivated it).
        /// CONFIDENCE: LOW — static factory wiring after cold load is the
        /// classic "loaded entity tries to act and crashes" bug pattern.
        /// </summary>
        [Test]
        public void DeepDive_CorpsePart_FieldsRoundTrip_AfterColdLoad()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var snapjaw = MakeCreature("snapjaw-1", "Snapjaw");
            snapjaw.AddPart(new CorpsePart { CorpseChance = 75, CorpseBlueprint = "TestCorpse" });
            zone.AddEntity(snapjaw, 5, 5);

            var loaded = RoundTrip(player, zone, mgr, turns);
            var loadedSnapjaw = FindEntityByID(loaded.ZoneManager.ActiveZone, "snapjaw-1");
            Assert.IsNotNull(loadedSnapjaw, "Snapjaw must round-trip");

            var loadedCorpse = loadedSnapjaw.GetPart<CorpsePart>();
            Assert.IsNotNull(loadedCorpse, "CorpsePart must round-trip");
            Assert.AreEqual(75, loadedCorpse.CorpseChance);
            Assert.AreEqual("TestCorpse", loadedCorpse.CorpseBlueprint);
        }

        // ============================================================
        // 4. DisposeOfCorpseGoal.GoToCorpseTries field preserved
        // ============================================================

        /// <summary>
        /// PRED: A goal-internal counter (DisposeOfCorpseGoal.GoToCorpseTries
        /// from M5 saga) survives round-trip via reflective save.
        /// CONFIDENCE: medium — Spec test covers LayRuneGoal.MoveTries; this
        /// confirms the same pattern works for a different named goal.
        /// </summary>
        [Test]
        public void DeepDive_DisposeOfCorpseGoal_GoToCorpseTries_Preserved()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var undertaker = MakeCreature("undertaker-1", "Undertaker");
            var brain = new BrainPart();
            undertaker.AddPart(brain);
            zone.AddEntity(undertaker, 5, 5);
            brain.CurrentZone = zone;

            // Synthesize the corpse + container args for the goal ctor
            var corpse = MakeCreature("corpse-1", "Corpse");
            zone.AddEntity(corpse, 7, 5);
            var container = MakeCreature("grave-1", "Grave");
            zone.AddEntity(container, 8, 5);

            var goal = new DisposeOfCorpseGoal(corpse, container);
            goal.GoToCorpseTries = 7;
            brain.PushGoal(goal);

            var loaded = RoundTrip(player, zone, mgr, turns);
            var loadedUndertaker = FindEntityByID(loaded.ZoneManager.ActiveZone, "undertaker-1");
            var loadedBrain = loadedUndertaker.GetPart<BrainPart>();
            Assert.IsNotNull(loadedBrain);

            var snapshot = loadedBrain.GetGoalsSnapshot();
            DisposeOfCorpseGoal foundGoal = null;
            foreach (var g in snapshot)
                if (g is DisposeOfCorpseGoal dog) { foundGoal = dog; break; }
            Assert.IsNotNull(foundGoal, "DisposeOfCorpseGoal should survive round-trip");
            Assert.AreEqual(7, foundGoal.GoToCorpseTries,
                "GoToCorpseTries integer field should round-trip exactly");
        }

        // ============================================================
        // 5. MeleeWeaponPart.Attributes (Phase C field) round-trips
        // ============================================================

        /// <summary>
        /// PRED: Phase C's <c>MeleeWeaponPart.Attributes</c> string field
        /// survives save/load via reflective serialization.
        /// CONFIDENCE: medium — string field, standard reflective handling.
        /// </summary>
        [Test]
        public void DeepDive_MeleeWeaponPart_AttributesField_RoundTrips()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var sword = new Entity { ID = "sword-1", BlueprintName = "ShortSword" };
            sword.AddPart(new MeleeWeaponPart
            {
                BaseDamage = "1d6",
                PenBonus = 1,
                Attributes = "Cutting LongBlades"
            });
            zone.AddEntity(sword, 7, 7);

            var loaded = RoundTrip(player, zone, mgr, turns);
            var loadedSword = FindEntityByID(loaded.ZoneManager.ActiveZone, "sword-1");
            Assert.IsNotNull(loadedSword);
            var loadedWeapon = loadedSword.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(loadedWeapon);
            Assert.AreEqual("Cutting LongBlades", loadedWeapon.Attributes);
            Assert.AreEqual("1d6", loadedWeapon.BaseDamage);
            Assert.AreEqual(1, loadedWeapon.PenBonus);
        }

        // ============================================================
        // 6. MultiWeaponSkillBonus stat (Phase G) round-trips
        // ============================================================

        /// <summary>
        /// PRED: Phase G's <c>MultiWeaponSkillBonus</c> stat (added
        /// dynamically to entities — not part of any blueprint default)
        /// round-trips on the player.
        /// CONFIDENCE: medium — Statistics dict round-trip is well-tested.
        /// </summary>
        [Test]
        public void DeepDive_MultiWeaponSkillBonus_OnPlayer_RoundTrips()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            player.Statistics["MultiWeaponSkillBonus"] = new Stat
            {
                Owner = player, Name = "MultiWeaponSkillBonus",
                BaseValue = 5, Min = -10, Max = 10
            };

            var loaded = RoundTrip(player, zone, mgr, turns);
            Assert.IsNotNull(loaded.Player);
            var bonus = loaded.Player.GetStat("MultiWeaponSkillBonus");
            Assert.IsNotNull(bonus,
                "MultiWeaponSkillBonus stat should be present after round-trip");
            Assert.AreEqual(5, bonus.BaseValue);
            Assert.AreEqual(-10, bonus.Min);
            Assert.AreEqual(10, bonus.Max);
        }

        // ============================================================
        // 7. StoneskinEffect.Reduction (Phase T2.4) round-trips
        // ============================================================

        /// <summary>
        /// PRED: Phase T2.4's <c>StoneskinEffect.Reduction</c> int field
        /// round-trips through StatusEffectsPart's reflective Effect serializer.
        /// CONFIDENCE: medium.
        /// </summary>
        [Test]
        public void DeepDive_StoneskinEffect_ReductionField_RoundTrips()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var statusEffects = new StatusEffectsPart();
            player.AddPart(statusEffects);
            statusEffects.ApplyEffect(new StoneskinEffect(reduction: 3));

            var loaded = RoundTrip(player, zone, mgr, turns);
            var loadedEffects = loaded.Player.GetPart<StatusEffectsPart>();
            Assert.IsNotNull(loadedEffects);

            StoneskinEffect found = null;
            foreach (var eff in loadedEffects.GetAllEffects())
                if (eff is StoneskinEffect ss) { found = ss; break; }
            Assert.IsNotNull(found, "StoneskinEffect should round-trip");
            Assert.AreEqual(3, found.Reduction);
        }

        // ============================================================
        // 8. Phase E resistance stats round-trip
        // ============================================================

        /// <summary>
        /// PRED: Phase E elemental resistance stats round-trip on a
        /// non-player creature, including positive AND negative values
        /// (vulnerability).
        /// CONFIDENCE: medium.
        /// </summary>
        [Test]
        public void DeepDive_PhaseE_ResistanceStats_RoundTrip_BothPositiveAndNegative()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var glowmaw = MakeCreature("glowmaw-1", "Glowmaw");
            glowmaw.Statistics["HeatResistance"] = new Stat
            {
                Owner = glowmaw, Name = "HeatResistance",
                BaseValue = 50, Min = -100, Max = 200
            };
            glowmaw.Statistics["ColdResistance"] = new Stat
            {
                Owner = glowmaw, Name = "ColdResistance",
                BaseValue = -50, Min = -100, Max = 200
            };
            zone.AddEntity(glowmaw, 5, 5);

            var loaded = RoundTrip(player, zone, mgr, turns);
            var loadedGlowmaw = FindEntityByID(loaded.ZoneManager.ActiveZone, "glowmaw-1");
            Assert.IsNotNull(loadedGlowmaw);

            Assert.AreEqual(50, loadedGlowmaw.GetStatValue("HeatResistance", -999));
            Assert.AreEqual(-50, loadedGlowmaw.GetStatValue("ColdResistance", -999));
        }

        // ============================================================
        // 9. Save with entity at HP=0 (dying-but-not-removed)
        // ============================================================

        /// <summary>
        /// PRED: A save file captured at the exact moment when an entity has
        /// HP=0 but hasn't been removed from the zone round-trips and the
        /// loaded entity is still in the zone with HP=0. Load does NOT
        /// trigger HandleDeath again on the dying entity.
        /// CONFIDENCE: LOW — re-entrant state class.
        /// </summary>
        [Test]
        public void DeepDive_SaveWithEntityAtZeroHP_RoundTripsCleanly()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var dying = MakeCreature("dying-1", "Snapjaw");
            zone.AddEntity(dying, 5, 5);
            dying.GetStat("Hitpoints").BaseValue = 0;  // pre-killed

            GameSessionState loaded = null;
            Assert.DoesNotThrow(() => loaded = RoundTrip(player, zone, mgr, turns));

            var loadedDying = FindEntityByID(loaded.ZoneManager.ActiveZone, "dying-1");
            Assert.IsNotNull(loadedDying,
                "Entity at HP=0 should still be in the loaded zone (was not removed pre-save)");
            Assert.AreEqual(0, loadedDying.GetStat("Hitpoints").BaseValue,
                "HP=0 state preserved");
        }

        // ============================================================
        // 10. Resave-load 5-cycle stability
        // ============================================================

        /// <summary>
        /// PRED: 5 successive save-load cycles produce a stable state with
        /// no progressive corruption.
        /// CONFIDENCE: medium — extends the existing 1-cycle idempotency test.
        /// </summary>
        [Test]
        public void DeepDive_FiveCycle_SaveLoadStability_NoDriftAcrossIterations()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            player.Statistics["Strength"] = new Stat
                { Owner = player, Name = "Strength", BaseValue = 17, Min = 1, Max = 50 };
            player.SetTag("Visited.Town");

            var sword = new Entity { ID = "sword-cycle", BlueprintName = "ShortSword" };
            sword.AddPart(new MeleeWeaponPart { BaseDamage = "1d6", Attributes = "Cutting LongBlades" });
            zone.AddEntity(sword, 8, 8);

            // First cycle
            var state = RoundTrip(player, zone, mgr, turns);

            // 4 more cycles
            for (int i = 2; i <= 5; i++)
            {
                using var stream = new MemoryStream();
                var writer = new SaveWriter(stream);
                state.Save(writer);
                stream.Position = 0;
                var reader = new SaveReader(stream, null);
                state = GameSessionState.Load(reader);
            }

            Assert.IsNotNull(state.Player, "Player survives 5 cycles");
            Assert.AreEqual(17, state.Player.GetStatValue("Strength", -1),
                "Strength=17 stable across 5 cycles");
            Assert.IsTrue(state.Player.HasTag("Visited.Town"),
                "Tag stable across 5 cycles");

            var finalSword = FindEntityByID(state.ZoneManager.ActiveZone, "sword-cycle");
            Assert.IsNotNull(finalSword);
            Assert.AreEqual("Cutting LongBlades",
                finalSword.GetPart<MeleeWeaponPart>().Attributes,
                "Weapon Attributes stable across 5 cycles");
        }

        // ============================================================
        // 11. Stat-less entity round-trips
        // ============================================================

        /// <summary>
        /// PRED: An entity with NO stats (e.g., a stone statue prop) round-trips
        /// with an empty Statistics dict. The save system shouldn't assume
        /// every entity has Hitpoints/Speed.
        /// CONFIDENCE: medium — defensive case.
        /// </summary>
        [Test]
        public void DeepDive_StatlessEntity_RoundTrips_PreservesEmptyStatsDict()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var prop = new Entity { ID = "statue-1", BlueprintName = "StoneStatue" };
            prop.AddPart(new RenderPart { DisplayName = "stone statue", RenderString = "I" });
            zone.AddEntity(prop, 9, 9);

            GameSessionState loaded = null;
            Assert.DoesNotThrow(() => loaded = RoundTrip(player, zone, mgr, turns));

            var loadedProp = FindEntityByID(loaded.ZoneManager.ActiveZone, "statue-1");
            Assert.IsNotNull(loadedProp);
            Assert.AreEqual(0, loadedProp.Statistics.Count,
                "Stat-less entity should round-trip with empty Statistics dict");
            Assert.IsNotNull(loadedProp.GetPart<RenderPart>(),
                "Non-stat parts (RenderPart) still round-trip");
        }

        // ============================================================
        // 12. Two entities, same blueprint, distinct IDs
        // ============================================================

        /// <summary>
        /// PRED: Two entities sharing the same BlueprintName but with
        /// different IDs are loaded as two distinct instances. ID is the
        /// discriminator; BlueprintName is non-discriminating.
        /// CONFIDENCE: medium.
        /// </summary>
        [Test]
        public void DeepDive_TwoEntities_SameBlueprint_DifferentIDs_StayDistinct()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var s1 = MakeCreature("snap-A", "Snapjaw");
            var s2 = MakeCreature("snap-B", "Snapjaw");
            zone.AddEntity(s1, 4, 4);
            zone.AddEntity(s2, 5, 5);

            var loaded = RoundTrip(player, zone, mgr, turns);
            var loadedA = FindEntityByID(loaded.ZoneManager.ActiveZone, "snap-A");
            var loadedB = FindEntityByID(loaded.ZoneManager.ActiveZone, "snap-B");

            Assert.IsNotNull(loadedA);
            Assert.IsNotNull(loadedB);
            Assert.AreNotSame(loadedA, loadedB,
                "Two entities with same blueprint, distinct IDs must be distinct instances");
            Assert.AreEqual("Snapjaw", loadedA.BlueprintName);
            Assert.AreEqual("Snapjaw", loadedB.BlueprintName);
        }
    }
}
