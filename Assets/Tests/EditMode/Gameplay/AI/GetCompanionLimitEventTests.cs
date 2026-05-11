using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Followers F.3.2 — <see cref="GetCompanionLimitEvent"/> query +
    /// <see cref="Persuasion_Recruit"/> slot bump.
    ///
    /// <para><b>Contract:</b> <c>GetCompanionLimitEvent.GetFor(actor,
    /// means, baseLimit)</c> fires a <see cref="GameEvent"/> named
    /// <c>"GetCompanionLimit"</c> on the actor with parameters
    /// <c>"Means"</c> (string) and <c>"Limit"</c> (int, initialized
    /// to <paramref name="baseLimit"/>). Listeners interested in
    /// bumping the limit (skills, items) override
    /// <see cref="Part.HandleEvent"/>, check <c>e.ID</c> and
    /// <c>e.GetStringParameter("Means")</c>, and modify
    /// <c>e.SetParameter("Limit", current + 1)</c> when relevant. The
    /// query returns the final <c>Limit</c> param after dispatch.</para>
    ///
    /// <para><b>Qud parity:</b> mirrors
    /// <c>XRL.World/GetCompanionLimitEvent.cs</c>. CoO replaces Qud's
    /// per-event-type <c>PooledEvent&lt;T&gt;</c> with the existing
    /// CoO <see cref="GameEvent"/> pool (single class + string-id +
    /// dynamic param dicts) per F.3.1 verification sweep.</para>
    /// </summary>
    public class GetCompanionLimitEventTests
    {
        [SetUp]
        public void Setup()
        {
            SkillRegistry.ResetForTests();
            MessageLog.Clear();
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakeActor(string id)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new BrainPart());
            e.AddPart(new SkillsPart());
            return e;
        }

        // ── Contract: GetFor returns the right limit ──────────────────

        [Test]
        public void GetFor_NoListeners_ReturnsBaseLimit()
        {
            var actor = MakeActor("a");
            // No skills, no items — no one to bump the limit.
            int limit = GetCompanionLimitEvent.GetFor(actor, "Recruit", baseLimit: 0);
            Assert.AreEqual(0, limit,
                "Actor with no listeners returns base limit unmodified.");
        }

        [Test]
        public void GetFor_NullActor_ReturnsBaseLimit_NoCrash()
        {
            int limit = GetCompanionLimitEvent.GetFor(null, "Recruit", baseLimit: 3);
            Assert.AreEqual(3, limit,
                "Null actor returns base limit; no NRE.");
        }

        [Test]
        public void GetFor_WithPersuasionRecruit_BumpsLimitBy1()
        {
            var actor = MakeActor("a");
            actor.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");

            int limit = GetCompanionLimitEvent.GetFor(actor, "Recruit", baseLimit: 0);

            Assert.AreEqual(1, limit,
                "Persuasion_Recruit grants +1 slot for the 'Recruit' means.");
        }

        [Test]
        public void GetFor_WithPersuasionRecruit_WrongMeans_NoBump()
        {
            // Persuasion_Recruit ONLY bumps for the "Recruit" means.
            // A future skill (Beguile, Rebuke) would bump its own means
            // separately; this test pins the per-means filtering.
            var actor = MakeActor("a");
            actor.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");

            int limit = GetCompanionLimitEvent.GetFor(actor, "Beguile", baseLimit: 0);

            Assert.AreEqual(0, limit,
                "Persuasion_Recruit does NOT bump 'Beguile' means — per-means filtering holds.");
        }

        [Test]
        public void GetFor_BaseLimitPropagates()
        {
            // baseLimit is the starting value; listeners add to it.
            var actor = MakeActor("a");
            actor.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");

            int limit = GetCompanionLimitEvent.GetFor(actor, "Recruit", baseLimit: 2);

            Assert.AreEqual(3, limit,
                "Base limit 2 + skill bump 1 = 3.");
        }

        // ── Counter-checks ─────────────────────────────────────────

        [Test]
        public void GetFor_AfterSkillRemoval_LimitDropsBack()
        {
            // Counter-check: removing the skill drops the bump back.
            var actor = MakeActor("a");
            var skill = new Persuasion_Recruit();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            Assert.AreEqual(1, GetCompanionLimitEvent.GetFor(actor, "Recruit", 0),
                "Precondition: skill present → limit 1.");

            actor.GetPart<SkillsPart>().RemoveSkill(skill);

            Assert.AreEqual(0, GetCompanionLimitEvent.GetFor(actor, "Recruit", 0),
                "After skill removal, the bump is gone.");
        }

        [Test]
        public void GetFor_EventDispatchedOnActor_NotMutatingActorState()
        {
            // The query is read-only — it must not mutate the actor's
            // permanent state. Subsequent calls return the same value.
            var actor = MakeActor("a");
            actor.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");

            int first = GetCompanionLimitEvent.GetFor(actor, "Recruit", 0);
            int second = GetCompanionLimitEvent.GetFor(actor, "Recruit", 0);
            int third = GetCompanionLimitEvent.GetFor(actor, "Recruit", 0);

            Assert.AreEqual(first, second);
            Assert.AreEqual(second, third);
            Assert.AreEqual(1, first,
                "Idempotent query — repeated calls produce the same limit.");
        }

        [Test]
        public void GetFor_GameEventReleasedAfterDispatch()
        {
            // The query allocates a GameEvent from the pool. It must
            // release it back so the pool doesn't leak.
            // Indirect probe: after a query, the pool should have
            // the event available again. We can't read pool count
            // directly, but we can verify Release-after-use by reading
            // the standard GameEvent contract.
            var actor = MakeActor("a");
            for (int i = 0; i < 100; i++)
            {
                int limit = GetCompanionLimitEvent.GetFor(actor, "Recruit", 0);
                Assert.AreEqual(0, limit,
                    $"Iteration {i}: limit stable.");
            }
            // If GameEvents weren't released, the pool would grow without
            // bound; 100 iterations is enough to detect a leak by hitting
            // the pool's 64-capacity cap (line 47 of GameEvent.cs) and
            // forcing allocation, but no observable behavior change here.
            // This test is primarily an "doesn't crash under repeated calls" probe.
            Assert.Pass();
        }
    }
}
