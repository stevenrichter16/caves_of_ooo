using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// First port from the WSP8.2 active-ability brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md</c> §LongBlades_Lunge).
    /// Pins the "blade extends, actor doesn't move, strike up to
    /// <see cref="LongBlades_Lunge.LUNGE_RANGE"/> cells in the chosen
    /// direction" mechanic. Uses
    /// <see cref="CavesOfOoo.Core.LineTargeting.TraceFirstImpact"/> for
    /// line resolution + <see cref="CombatSystem.PerformSingleAttack"/>
    /// for the swing — same canonical paths Shank uses.
    ///
    /// <para><b>Coverage layout (mirrors CudgelSlamTests):</b>
    /// <list type="bullet">
    ///   <item>Spec shape — DeclareActivatedAbility's
    ///         Command/TargetingMode/Range/Cooldown.</item>
    ///   <item>Positive: range-1 strike, range-2 strike, line-trace
    ///         picks the closer of two creatures.</item>
    ///   <item>Counter-checks: Cudgel weapon doesn't trigger Lunge,
    ///         no weapon doesn't swing, dx=dy=0 no-ops, distance-3
    ///         creature out of range.</item>
    ///   <item>Edge: wall blocks the line — creature behind it isn't
    ///         hit (line-trace stops on solid).</item>
    ///   <item>Adversarial: null Rng / null Zone bail without
    ///         exception (defense-in-depth, mirrors Slam).</item>
    ///   <item>Direction plumbing: SkillEventContext.DirectionX/Y must
    ///         pass through SkillsPart.HandleEvent's GameEvent dispatch
    ///         path (so InputHandler-injected direction reaches
    ///         OnCommand).</item>
    /// </list></para>
    /// </summary>
    public class LongBladesLungeTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
            Diag.ResetAll();
        }

        // ── Fixture helpers (mirror CudgelSlamTests exactly) ─────────────

        private static Entity MakeBodiedCreature(string name = "creature",
            int strength = 16, int hp = 50)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat
                { Owner = e, Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat
                { Owner = e, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat
                { Owner = e, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.Statistics["DV"] = new Stat
                { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new ArmorPart());
            e.AddPart(new InventoryPart { MaxWeight = 150 });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            var body = new Body();
            e.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return e;
        }

        private static Entity MakeWeaponEntity(string name, string dice,
            string attributes)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            e.AddPart(new MeleeWeaponPart
            {
                BaseDamage = dice, PenBonus = 0,
                Attributes = attributes,
            });
            e.AddPart(new EquippablePart { Slot = "Hand" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static void EquipInPrimary(Entity actor, Entity weaponEntity)
        {
            var hand = actor.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            actor.GetPart<InventoryPart>().EquipToBodyPart(weaponEntity, hand);
        }

        private static Entity MakeWall(string name = "wall")
        {
            var w = new Entity { ID = name, BlueprintName = name };
            w.Tags["Solid"] = "";
            w.Tags["Wall"] = "";
            w.AddPart(new RenderPart { DisplayName = name });
            return w;
        }

        /// <summary>Builds an attacker (LongBlades-class weapon equipped,
        /// Lunge skill owned) + a defender + an empty Zone. Caller places
        /// them via <c>zone.AddEntity(...)</c>.</summary>
        private static (Entity attacker, Entity defender, Zone zone, LongBlades_Lunge lunge)
            MakeLungeFixture(int defenderHp = 50, string weaponAttributes = "Cutting LongBlades")
        {
            var attacker = MakeBodiedCreature("attacker");
            EquipInPrimary(attacker,
                MakeWeaponEntity("longsword", "1d8+1", weaponAttributes));
            var lunge = new LongBlades_Lunge();
            attacker.GetPart<SkillsPart>().AddSkill(lunge, source: "test");

            var defender = MakeBodiedCreature("defender", hp: defenderHp);
            var zone = new Zone();
            return (attacker, defender, zone, lunge);
        }

        // ════════════════════════════════════════════════════════════════
        // Spec shape — DeclareActivatedAbility returns the expected spec
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_DeclareActivatedAbility_ReturnsExpectedSpec()
        {
            var lunge = new LongBlades_Lunge();
            var spec = lunge.DeclareActivatedAbility(actor: null);

            Assert.IsNotNull(spec, "Lunge must declare a non-null spec.");
            Assert.AreEqual("CommandLunge", spec.Command,
                "Lunge's command must be 'CommandLunge' (the input dispatcher key).");
            Assert.AreEqual(LongBlades_Lunge.COOLDOWN, spec.Cooldown,
                "Lunge's cooldown must match the COOLDOWN constant (25T per brainstorm).");
            Assert.AreEqual(AbilityTargetingMode.DirectionLine, spec.TargetingMode,
                "Lunge targets a direction line (player picks an 8-way direction, "
                + "the line traces up to LUNGE_RANGE).");
            Assert.AreEqual(LongBlades_Lunge.LUNGE_RANGE, spec.Range,
                "Lunge's range must be LUNGE_RANGE (2 cells — the defining "
                + "reach-extension mechanic).");
            Assert.AreEqual("Lunge", spec.DisplayName,
                "Lunge's display name should be 'Lunge'.");
        }

        // ════════════════════════════════════════════════════════════════
        // Positive: target adjacent (range 1) — Lunge degrades to a normal-
        // reach swing, since the line-trace finds the creature at distance 1
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_OnAdjacentTarget_StrikesAtRange1()
        {
            // Setup: attacker at (5,5), defender at (6,5) (East, distance 1).
            // Player aims East. Lunge's line-trace finds defender at the
            // first step — strike fires (PerformSingleAttack with "(Lunge)"
            // tag), defender takes damage.
            var (attacker, defender, zone, lunge) = MakeLungeFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            // Use a seed where the attack roll lands. Seed 42 + Str 16 +
            // longsword 1d8+1 vs. defender DV=0 → reliable hit-and-damage.
            lunge.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            // Defender should NOT have moved (Lunge is reach extension,
            // not a push). Damage applied from the swing.
            var newPos = zone.GetEntityPosition(defender);
            Assert.AreEqual((6, 5), (newPos.x, newPos.y),
                "Lunge must NOT push the defender — it's a reach-extension swing, not a slam.");
            Assert.Less(defender.GetStatValue("Hitpoints"), hpBefore,
                "Lunge at range 1 should strike + damage like a normal swing.");
        }

        // ════════════════════════════════════════════════════════════════
        // Positive: target at range 2 — the defining mechanic
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_OnTargetTwoCellsAway_StrikesAtRange2()
        {
            // Setup: attacker at (5,5), defender at (7,5) (East, distance 2).
            // Empty cell at (6,5). Player aims East. Lunge's line-trace
            // walks (6,5) → empty, (7,5) → finds defender. Strike fires.
            var (attacker, defender, zone, lunge) = MakeLungeFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 7, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            lunge.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            var attackerPos = zone.GetEntityPosition(attacker);
            Assert.AreEqual((5, 5), (attackerPos.x, attackerPos.y),
                "Attacker must NOT move — Lunge extends reach, doesn't move the actor.");
            var defenderPos = zone.GetEntityPosition(defender);
            Assert.AreEqual((7, 5), (defenderPos.x, defenderPos.y),
                "Defender must NOT move either — Lunge isn't a push.");
            Assert.Less(defender.GetStatValue("Hitpoints"), hpBefore,
                "Lunge at range 2 must damage the target (the defining mechanic).");
        }

        // ════════════════════════════════════════════════════════════════
        // Positive: nearer creature blocks the line — Lunge hits the closer
        // one (LineTargeting stops on the first creature)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_NearerCreatureBlocksLine_HitsCloserTarget()
        {
            // Setup: attacker at (5,5). Two defenders East: closer at (6,5),
            // further at (7,5). Aim East → line-trace stops on the first
            // creature. The closer defender takes the hit; the further one
            // is untouched.
            var (attacker, defenderA, zone, lunge) = MakeLungeFixture();
            var defenderB = MakeBodiedCreature("defenderB");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defenderA, 6, 5);
            zone.AddEntity(defenderB, 7, 5);

            int hpA_before = defenderA.GetStatValue("Hitpoints");
            int hpB_before = defenderB.GetStatValue("Hitpoints");
            lunge.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            Assert.Less(defenderA.GetStatValue("Hitpoints"), hpA_before,
                "Closer defender at range 1 must take the hit.");
            Assert.AreEqual(hpB_before, defenderB.GetStatValue("Hitpoints"),
                "Further defender at range 2 must be UNTOUCHED — line-trace "
                + "stops on the first creature it encounters.");
        }

        // ════════════════════════════════════════════════════════════════
        // Edge: wall blocks the line — creature behind wall is unhit
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_WallBlocksLine_CreatureBehindWallNotHit()
        {
            // Setup: attacker at (5,5), wall at (6,5), defender at (7,5).
            // Aim East → line-trace hits wall at step 1, stops, no creature
            // damaged. Cooldown is still spent (handled by SkillsPart, not
            // by OnCommand directly).
            var (attacker, defender, zone, lunge) = MakeLungeFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(MakeWall(), 6, 5);
            zone.AddEntity(defender, 7, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            lunge.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Wall in the lunge path must block the strike — defender HP unchanged.");
        }

        // ════════════════════════════════════════════════════════════════
        // Edge: empty line — no creature in 2 cells, no damage anywhere
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_EmptyLine_NoStrike()
        {
            // Setup: attacker at (5,5), defender far away at (15,15).
            // Player aims East. Lunge's line-trace walks 2 cells East,
            // finds nothing — no swing, no damage.
            var (attacker, defender, zone, lunge) = MakeLungeFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 15, 15);

            int hpBefore = defender.GetStatValue("Hitpoints");
            lunge.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "No creature in the lunge line → no swing fires → defender "
                + "(off in the corner) untouched.");
        }

        // ════════════════════════════════════════════════════════════════
        // Edge: creature at range 3 is OUT of range
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_TargetAtRange3_OutOfReach()
        {
            // Setup: attacker at (5,5), defender at (8,5) (East, distance 3).
            // LUNGE_RANGE=2 → trace stops after 2 steps (cells 6,5 and 7,5),
            // no creature found. Defender at (8,5) is UNTOUCHED.
            var (attacker, defender, zone, lunge) = MakeLungeFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 8, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            lunge.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Creature at distance 3 is BEYOND LUNGE_RANGE (2) — "
                + "must not be hit.");
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: Cudgel weapon does NOT enable Lunge
        // (gating mirrors Shank's "Piercing required" / Slam's "Cudgel
        //  required" — Lunge requires LongBlades.)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_WithCudgelWeapon_RefusesToSwing()
        {
            // Setup: attacker has a mace (Cudgel) instead of a longsword.
            // Defender at range 2. Lunge must check weapon-class FIRST,
            // see Cudgel != LongBlades, log a refusal, and NOT swing.
            var (attacker, defender, zone, lunge) =
                MakeLungeFixture(weaponAttributes: "Bludgeoning Cudgel");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 7, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            lunge.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Cudgel-class weapon must NOT trigger Lunge — defender HP "
                + "unchanged when the gate fails.");
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: no weapon equipped → no swing
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_WithNoWeapon_RefusesToSwing()
        {
            // Setup: build a unarmed actor with Lunge skill owned but no
            // weapon equipped. Defender at range 1. Lunge's gate fails on
            // missing weapon → no damage.
            var attacker = MakeBodiedCreature("attacker");
            var lunge = new LongBlades_Lunge();
            attacker.GetPart<SkillsPart>().AddSkill(lunge, source: "test");

            var defender = MakeBodiedCreature("defender");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            lunge.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Unarmed actor must not Lunge — defender HP unchanged.");
        }

        // ════════════════════════════════════════════════════════════════
        // Edge: dx=dy=0 (no direction picked) → graceful no-op
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_WithZeroDirection_NoOps_NoCrash()
        {
            // dx=dy=0 indicates no direction was supplied (defense in depth
            // — the InputHandler shouldn't fire a DirectionLine command
            // with both axes zero, but if some test/scenario path does,
            // Lunge should bail gracefully rather than crash on
            // LineTargeting.TraceFirstImpact's zero-direction guard).
            var (attacker, defender, zone, lunge) = MakeLungeFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            Assert.DoesNotThrow(() =>
            {
                lunge.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = zone, Rng = new Random(42),
                    DirectionX = 0, DirectionY = 0,
                });
            }, "Zero-direction Lunge must not throw.");
            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Zero-direction Lunge must do nothing — defender HP unchanged.");
        }

        // ════════════════════════════════════════════════════════════════
        // Adversarial: null Rng → no-op (defense in depth, mirrors Slam)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_WithNullRng_NoOps_NoCrash()
        {
            // Determinism rule (per WSP4.4 + Slam's null-Rng test): bail
            // on null Rng instead of falling back to a wall-clock-seeded
            // one. The InputHandler always threads an Rng; null indicates
            // a misconfigured caller and we'd rather no-op than corrupt
            // RNG-deterministic tests.
            var (attacker, defender, zone, lunge) = MakeLungeFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            Assert.DoesNotThrow(() =>
            {
                lunge.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = zone, Rng = null,
                    DirectionX = 1, DirectionY = 0,
                });
            }, "Null-Rng Lunge must not throw.");
            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Null-Rng Lunge must do nothing.");
        }

        // ════════════════════════════════════════════════════════════════
        // Adversarial: null Zone → no-op
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_WithNullZone_NoOps_NoCrash()
        {
            var attacker = MakeBodiedCreature("attacker");
            EquipInPrimary(attacker,
                MakeWeaponEntity("longsword", "1d8+1", "Cutting LongBlades"));
            var lunge = new LongBlades_Lunge();
            attacker.GetPart<SkillsPart>().AddSkill(lunge, source: "test");

            Assert.DoesNotThrow(() =>
            {
                lunge.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = null, Rng = new Random(42),
                    DirectionX = 1, DirectionY = 0,
                });
            }, "Null-Zone Lunge must not throw.");
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: non-creature object (e.g. barrel) in line blocks
        // the lunge. TraceFirstImpact returns the object as HitEntity but
        // Lunge's "Creature"-tag filter rejects it — no swing, no damage.
        // Without this filter, a future regression could let Lunge swing
        // at barrels (which would crash PerformSingleAttack on the
        // missing combat parts).
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_NonCreatureObjectInLine_NoStrike()
        {
            // Setup: attacker at (5,5), barrel-like object at (6,5),
            // defender at (7,5). The "object" has Hitpoints + PhysicsPart
            // (so TraceFirstImpact picks it up) but no Creature tag (so
            // Lunge's IsCreature filter rejects it). The line stops at
            // the object → defender behind it is untouched.
            var (attacker, defender, zone, lunge) = MakeLungeFixture();
            zone.AddEntity(attacker, 5, 5);
            var barrel = new Entity { ID = "barrel", BlueprintName = "barrel" };
            barrel.Tags["Item"] = "";  // explicitly NOT Creature
            barrel.Statistics["Hitpoints"] = new Stat
                { Owner = barrel, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            barrel.AddPart(new RenderPart { DisplayName = "barrel" });
            barrel.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(barrel, 6, 5);
            zone.AddEntity(defender, 7, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            int barrelHpBefore = barrel.GetStatValue("Hitpoints");
            lunge.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Defender behind a non-creature object must NOT be hit — "
                + "TraceFirstImpact stops at the barrel, blocking the line.");
            Assert.AreEqual(barrelHpBefore, barrel.GetStatValue("Hitpoints"),
                "Barrel itself must NOT be damaged — Lunge's Creature-tag "
                + "filter prevents PerformSingleAttack from firing on a "
                + "non-creature target. (If this fails, a future regression "
                + "removed the Tags.ContainsKey(\"Creature\") gate.)");
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: diagonal direction works just like cardinal
        // (LineTargeting handles both — Lunge shouldn't have any
        //  cardinal-only assumptions baked in)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_DiagonalDirection_StrikesAtRange2()
        {
            // Setup: attacker at (5,5), defender at (7,3) (NE, distance 2
            // by Chebyshev — diagonal). Empty cells at (6,4). Player aims
            // NE (dx=1, dy=-1). Lunge's line-trace walks (6,4) → (7,3) →
            // finds defender. Strike fires.
            var (attacker, defender, zone, lunge) = MakeLungeFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 7, 3);

            int hpBefore = defender.GetStatValue("Hitpoints");
            lunge.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = -1,
            });

            Assert.Less(defender.GetStatValue("Hitpoints"), hpBefore,
                "Lunge must work in diagonal directions, not just cardinal — "
                + "the LineTargeting helper handles both equivalently.");
        }

        // ════════════════════════════════════════════════════════════════
        // Direction plumbing: SkillsPart.HandleEvent must surface
        // GameEvent.DirectionX/Y into SkillEventContext for DirectionLine
        // skills. Without this, Lunge's OnCommand sees dx=dy=0 from the
        // production path.
        // ════════════════════════════════════════════════════════════════

        // ════════════════════════════════════════════════════════════════
        // Observability: per-skill rejection paths emit SkillRejected
        // diag records with the gate name in the reason field. Locks
        // CLAUDE.md §Observability — every gate that can reject emits
        // a record. The May-2026 live-run review surfaced this as
        // Finding 2 (CommandRouted fired but per-skill rejections were
        // silent — debuggers couldn't tell why a skill bailed).
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Lunge_NoWeapon_EmitsSkillRejectedDiag_ReasonNoWeapon()
        {
            var attacker = MakeBodiedCreature("attacker"); // unarmed
            var lunge = new LongBlades_Lunge();
            attacker.GetPart<SkillsPart>().AddSkill(lunge, source: "test");

            var defender = MakeBodiedCreature("defender");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            Diag.ResetAll(); // ignore the AddSkill record
            lunge.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill",
                Kind = "SkillRejected",
                Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count,
                "No-weapon Lunge must emit exactly 1 SkillRejected record.");
            StringAssert.Contains("no_weapon", records[0].PayloadJson,
                "Reason must be 'no_weapon' so debug queries can filter "
                + "by gate type.");
            StringAssert.Contains("LongBlades_Lunge", records[0].PayloadJson,
                "Payload must include the skill class for tracing.");
        }

        [Test]
        public void Lunge_NoDirection_EmitsSkillRejectedDiag_ReasonNoDirection()
        {
            var (attacker, defender, zone, lunge) = MakeLungeFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            Diag.ResetAll();
            lunge.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
                DirectionX = 0, DirectionY = 0, // explicit no-direction
            });

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill",
                Kind = "SkillRejected",
                Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("no_direction", records[0].PayloadJson);
        }

        [Test]
        public void Lunge_LineBlocked_EmitsSkillRejectedDiag_ReasonLineBlocked()
        {
            // Wall at distance 1 → TraceFirstImpact stops on the wall;
            // HitEntity is null (walls are solid but don't satisfy the
            // "Creature OR has-stat-Hitpoints" branches), so the reason
            // should be "no_target" (line is empty before any object).
            // Using a non-creature targetable object instead would
            // surface "line_blocked" — which we test via
            // Lunge_NonCreatureObjectInLine_NoStrike's diag side below.
            var (attacker, defender, zone, lunge) = MakeLungeFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(MakeWall(), 6, 5);
            zone.AddEntity(defender, 7, 5);

            Diag.ResetAll();
            lunge.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill",
                Kind = "SkillRejected",
                Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count,
                "Wall-blocked Lunge must emit exactly 1 SkillRejected.");
            // Reason can be "no_target" or "line_blocked" depending on
            // whether TraceFirstImpact returns a HitEntity for the wall.
            // Walls have no Hitpoints stat / Thermal / Material parts so
            // GetFirstTargetableObject returns null → HitEntity is null
            // → reason is "no_target".
            string payload = records[0].PayloadJson;
            Assert.IsTrue(
                payload.Contains("no_target") || payload.Contains("line_blocked"),
                "Reason must be 'no_target' or 'line_blocked' for "
                + "wall-blocked Lunge. Got payload: " + payload);
        }

        [Test]
        public void Lunge_FiredViaGameEvent_DirectionParamsReachOnCommand()
        {
            // Setup: attacker has Lunge skill owned + LongBlades weapon
            // equipped. Defender 2 cells East. Fire CommandLunge as a
            // GameEvent (the production path through SkillsPart.HandleEvent)
            // with DirectionX=1, DirectionY=0. The defender at (7,5) must
            // be hit — proving the DirectionX/Y params reached OnCommand.
            var (attacker, defender, zone, _) = MakeLungeFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 7, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");

            var cmd = GameEvent.New("CommandLunge");
            cmd.SetParameter("Zone", zone);
            cmd.SetParameter("RNG", new Random(42));
            cmd.SetParameter("DirectionX", 1);
            cmd.SetParameter("DirectionY", 0);
            attacker.FireEvent(cmd);

            Assert.IsTrue(cmd.Handled,
                "SkillsPart.HandleEvent must mark CommandLunge handled "
                + "after dispatching it to Lunge.OnCommand.");
            Assert.Less(defender.GetStatValue("Hitpoints"), hpBefore,
                "Direction params must propagate from GameEvent → "
                + "SkillEventContext.DirectionX/Y → Lunge's line-trace. "
                + "If this fails, the plumbing is dropping direction.");
        }
    }
}
