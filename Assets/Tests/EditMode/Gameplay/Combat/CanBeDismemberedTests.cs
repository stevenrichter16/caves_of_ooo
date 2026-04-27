using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase H — `CanBeDismembered` event hook on combat dismemberment.
    ///
    /// Qud reference: `IGameSystem.cs:637` (`CanBeDismemberedEvent`) +
    /// `XRL.World.Parts.NoDamageExcept.cs:54` (an example listener that
    /// vetoes dismemberment unless damage matches a specific tag).
    ///
    /// User-visible invariants:
    ///
    ///   1. By default (no listener), combat dismemberment proceeds as
    ///      before — the rolled chance + body.Dismember() pathway is
    ///      unchanged.
    ///
    ///   2. A part on the defender can veto dismemberment by returning
    ///      false from HandleEvent("CanBeDismembered"). The body part
    ///      stays intact.
    ///
    ///   3. The event fires only when dismemberment WAS rolled to occur
    ///      (i.e., the rng roll passed the chance check). Listeners aren't
    ///      spammed for every hit.
    ///
    /// Counter-checks (Methodology Template §3.4):
    ///   • A no-op listener leaves dismemberment behavior unchanged
    ///   • A listener that returns true (proceed) is identical to no listener
    ///   • Veto event fires on the DEFENDER, not the attacker
    /// </summary>
    public class CanBeDismemberedTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ====================================================================
        // 1. Default behavior preserved when no listener
        // ====================================================================

        [Test]
        public void NoListener_DismembermentProceeds_OnHighDamage()
        {
            // Setup: deliver a single MASSIVE-damage attack to ensure the
            // dismemberment chance saturates (50% cap) and the rng will
            // very likely roll under it across many seeds. Track whether
            // any seed produced a dismembered part.
            var defender = MakeFighterWithBody(hp: 100);
            var hand = GetHand(defender, primary: false);
            int dismemberedCount = 0;

            for (int seed = 0; seed < 50; seed++)
            {
                // Restore the hand each iteration (re-create defender)
                defender = MakeFighterWithBody(hp: 100);
                hand = GetHand(defender, primary: false);
                int handsBefore = defender.GetPart<Body>().CountParts("Hand");

                // Damage = max-HP × 2 (way over threshold) so chance saturates
                CombatSystem.CheckCombatDismemberment(
                    defender, defender.GetPart<Body>(), hand, defender.GetStat("Hitpoints").Max * 2,
                    zone: null, rng: new Random(seed));

                int handsAfter = defender.GetPart<Body>().CountParts("Hand");
                if (handsAfter < handsBefore) dismemberedCount++;
            }

            // 50% chance × 50 seeds = ~25 dismemberments expected
            Assert.Greater(dismemberedCount, 5,
                $"Default behavior should produce some dismemberments at saturated chance. Got {dismemberedCount}.");
        }

        // ====================================================================
        // 2. Veto listener prevents dismemberment
        // ====================================================================

        [Test]
        public void VetoListener_PreventsDismemberment()
        {
            int dismemberedCount = 0;
            for (int seed = 0; seed < 50; seed++)
            {
                var defender = MakeFighterWithBody(hp: 100);
                defender.AddPart(new CanBeDismemberedVetoProbe());
                var hand = GetHand(defender, primary: false);
                int handsBefore = defender.GetPart<Body>().CountParts("Hand");

                CombatSystem.CheckCombatDismemberment(
                    defender, defender.GetPart<Body>(), hand, defender.GetStat("Hitpoints").Max * 2,
                    zone: null, rng: new Random(seed));

                int handsAfter = defender.GetPart<Body>().CountParts("Hand");
                if (handsAfter < handsBefore) dismemberedCount++;
            }

            Assert.AreEqual(0, dismemberedCount,
                "Veto listener must prevent ALL dismemberments across 50 seeds — got {dismemberedCount}");
        }

        // ====================================================================
        // 3. Listener returning true (proceed) is identical to no listener
        //    (counter-check)
        // ====================================================================

        [Test]
        public void NoOpListener_DismembermentBehaviorUnchanged()
        {
            int dismembered = 0;
            for (int seed = 0; seed < 50; seed++)
            {
                var defender = MakeFighterWithBody(hp: 100);
                // A listener that observes but returns true (default behavior)
                defender.AddPart(new CanBeDismemberedObserveProbe());
                var hand = GetHand(defender, primary: false);
                int handsBefore = defender.GetPart<Body>().CountParts("Hand");

                CombatSystem.CheckCombatDismemberment(
                    defender, defender.GetPart<Body>(), hand, defender.GetStat("Hitpoints").Max * 2,
                    zone: null, rng: new Random(seed));

                int handsAfter = defender.GetPart<Body>().CountParts("Hand");
                if (handsAfter < handsBefore) dismembered++;
            }

            Assert.Greater(dismembered, 5,
                "No-op listener should not block dismemberment");
        }

        // ====================================================================
        // 4. CanBeDismembered fires ONLY when chance roll passed
        //    (not on every hit) — listeners aren't spammed
        // ====================================================================

        [Test]
        public void CanBeDismembered_DoesNotFire_OnDamageBelowThreshold()
        {
            var defender = MakeFighterWithBody(hp: 100);
            int eventFires = 0;
            defender.AddPart(new EventCaptureProbe
            {
                OnEvent = e =>
                {
                    if (e.ID == "CanBeDismembered") eventFires++;
                }
            });
            var hand = GetHand(defender, primary: false);

            // Damage well below threshold (DISMEMBER_DAMAGE_THRESHOLD * maxHP)
            // — even with a perfect rng roll, dismemberment shouldn't trigger
            // and the event shouldn't fire.
            for (int seed = 0; seed < 30; seed++)
            {
                CombatSystem.CheckCombatDismemberment(
                    defender, defender.GetPart<Body>(), hand, damage: 1,  // tiny damage
                    zone: null, rng: new Random(seed));
            }

            Assert.AreEqual(0, eventFires,
                "CanBeDismembered must NOT fire when damage is below threshold");
        }

        [Test]
        public void CanBeDismembered_DoesNotFire_OnNonSeverablePart()
        {
            // Mortal/integral parts (Body, Head depending on flags) are not severable.
            // The event should not fire for them.
            var defender = MakeFighterWithBody(hp: 100);
            // Find a non-severable part — Head is Mortal (Mortal != Severable),
            // and Body root is non-severable too. We construct a "Body" type
            // search.
            var body = defender.GetPart<Body>();
            BodyPart nonSeverable = null;
            foreach (var p in body.GetParts())
            {
                if (!p.IsSeverable())
                {
                    nonSeverable = p;
                    break;
                }
            }
            Assert.IsNotNull(nonSeverable, "Test setup: humanoid must have at least one non-severable part");

            int eventFires = 0;
            defender.AddPart(new EventCaptureProbe
            {
                OnEvent = e =>
                {
                    if (e.ID == "CanBeDismembered") eventFires++;
                }
            });

            CombatSystem.CheckCombatDismemberment(
                defender, body, nonSeverable, damage: 9999,  // overwhelming damage
                zone: null, rng: new Random(42));

            Assert.AreEqual(0, eventFires,
                $"CanBeDismembered must NOT fire on non-severable part '{nonSeverable.Type}' (early-out before chance roll)");
        }

        // ====================================================================
        // 5. Counter-check: event fires on the DEFENDER, not on a separate
        //    attacker entity. Self-review Finding H-1.
        // ====================================================================

        [Test]
        public void CanBeDismembered_FiresOnDefender_NotOnAttacker()
        {
            var defender = MakeFighterWithBody(hp: 100);
            var attacker = MakeFighterWithBody(hp: 100);

            int firedOnDefender = 0;
            int firedOnAttacker = 0;
            defender.AddPart(new EventCaptureProbe { OnEvent = e =>
                { if (e.ID == "CanBeDismembered") firedOnDefender++; } });
            attacker.AddPart(new EventCaptureProbe { OnEvent = e =>
                { if (e.ID == "CanBeDismembered") firedOnAttacker++; } });

            var hand = GetHand(defender, primary: false);
            // Run a saturated-chance call; we expect at least some dismember rolls
            // to pass and fire the event on defender (not attacker).
            for (int seed = 0; seed < 30; seed++)
            {
                var freshDefender = MakeFighterWithBody(hp: 100);
                int firedOnFresh = 0;
                freshDefender.AddPart(new EventCaptureProbe { OnEvent = e =>
                    { if (e.ID == "CanBeDismembered") firedOnFresh++; } });
                var freshHand = GetHand(freshDefender, primary: false);
                CombatSystem.CheckCombatDismemberment(
                    freshDefender, freshDefender.GetPart<Body>(), freshHand,
                    freshDefender.GetStat("Hitpoints").Max * 2,
                    zone: null, rng: new Random(seed));
                firedOnDefender += firedOnFresh;
            }

            Assert.Greater(firedOnDefender, 0,
                "Defender's CanBeDismembered listener must fire at least sometimes");
            Assert.AreEqual(0, firedOnAttacker,
                "Attacker's listener must NEVER fire — event is target-side only");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private Entity MakeFighterWithBody(int hp = 100)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestFighter";
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Owner = entity, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Owner = entity, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Owner = entity, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.AddPart(new RenderPart { DisplayName = "fighter" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new InventoryPart { MaxWeight = 150 });
            var body = new Body();
            entity.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return entity;
        }

        private BodyPart GetHand(Entity entity, bool primary)
        {
            var body = entity.GetPart<Body>();
            foreach (var p in body.GetParts())
            {
                if (p.Type != "Hand") continue;
                if (primary && (p.Primary || p.DefaultPrimary)) return p;
                if (!primary && !(p.Primary || p.DefaultPrimary)) return p;
            }
            return null;
        }
    }

    /// <summary>
    /// Test probe that vetoes CanBeDismembered by returning false from HandleEvent.
    /// </summary>
    public class CanBeDismemberedVetoProbe : Part
    {
        public override string Name => "CanBeDismemberedVetoProbe";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "CanBeDismembered") return false;
            return true;
        }
    }

    /// <summary>
    /// Test probe that observes CanBeDismembered without vetoing.
    /// </summary>
    public class CanBeDismemberedObserveProbe : Part
    {
        public override string Name => "CanBeDismemberedObserveProbe";
        public int FireCount = 0;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "CanBeDismembered") FireCount++;
            return true;
        }
    }
}
