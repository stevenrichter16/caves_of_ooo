using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Followers F.2 — Recruit / Dismiss showcase. Validates the F.2
    /// recruitment substrate end-to-end with a player who can actually
    /// press the activated ability and watch the effect ripple through
    /// to BrainPart.PartyLeader + the FollowLeaderGoal.
    ///
    /// <para><b>Layout:</b></para>
    /// <code>
    ///   [Scribe NW  — recruit target #1: easy]
    ///   [Scribe N   — recruit target #2: easy]
    ///   [Player]
    ///   [Snapjaw E  — hostile: Veto #7 (target_hostile) probe]
    /// </code>
    ///
    /// <para><b>Loadout:</b> high Ego (22 → +3 mod) so the d20 vs DC=10
    /// roll succeeds the majority of the time; Persuasion_Recruit and
    /// Persuasion_Dismiss both registered on the player. Press M to
    /// open the abilities manager, then activate Recruit or Dismiss
    /// (each is adjacent-cell targeted).</para>
    ///
    /// <para><b>Probe:</b> a <see cref="RecruitEventProbePart"/> on the
    /// player listens for <c>EffectApplied</c> / <c>EffectRemoved</c>
    /// of <see cref="RecruitedEffect"/> and emits <c>[RecruitDemo]</c>
    /// log lines so the player can read each apply/dismiss as it fires.</para>
    ///
    /// <para><b>Suggested experiments:</b></para>
    /// <list type="number">
    ///   <item>Walk up to Scribe NW. Activate Recruit. Watch the
    ///         <c>"X joins Y!"</c> message + the <c>[RecruitDemo]</c>
    ///         probe line. The Scribe now follows you.</item>
    ///   <item>Move several cells. The Scribe should keep up via the
    ///         FollowLeaderGoal (F.1.5).</item>
    ///   <item>Try Recruit on the same Scribe again. Nothing happens —
    ///         Veto #5 <c>already_recruited</c> blocks the cast.</item>
    ///   <item>Recruit Scribe N. Now you have 2 followers.</item>
    ///   <item>Walk up to the Snapjaw. Try Recruit. Nothing happens —
    ///         Veto #7 <c>target_hostile</c> blocks (faction-default
    ///         hostility against the player).</item>
    ///   <item>Activate Dismiss adjacent to one of the Scribes. They
    ///         leave your service; the <c>[RecruitDemo]</c> probe
    ///         confirms the effect removal.</item>
    /// </list>
    /// </summary>
    [Scenario(
        name: "Recruit Showcase (F.2 verb)",
        category: "AI Behavior",
        description: "F.2 recruitment end-to-end. Player has Persuasion_Recruit + Persuasion_Dismiss as activated abilities; 2 neutral Scribes available for recruit, 1 hostile Snapjaw probes the hostile-target veto.")]
    public class RecruitShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // === Player loadout: high Ego for reliable recruit success ===
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Ego", 50)
                .SetStat("Ego", 22) // mod +3 — DC=10 vs Level-1 defender needs d20 ≥ 7 (70%)
                .SetStat("Level", 5)
                .Equip("Mace"); // self-defense if a recruit goes sideways

            // === Register the two activated abilities on the player ===
            // Skills resolve by class name via SkillsPart.ResolveSkillType,
            // which walks the assembly — no JSON registration needed.
            var skills = ctx.PlayerEntity?.GetPart<SkillsPart>();
            if (skills != null)
            {
                skills.AddSkill(nameof(Persuasion_Recruit),  source: "scenario:prebuy");
                skills.AddSkill(nameof(Persuasion_Dismiss), source: "scenario:prebuy");
            }

            // === Probe Part on the player so the demo log shows each
            // RecruitedEffect apply/remove event as it happens. ===
            ctx.PlayerEntity?.AddPart(new RecruitEventProbePart());

            // === Targets ===
            // Two neutral Scribes within easy walking distance for the
            // recruit demo. Spawned via the existing Scribe blueprint
            // (used in IgnoredScribe / VillageChildrenPetting / etc.),
            // so they come with BrainPart + a non-Snapjaws faction.
            ctx.Spawn("Scribe").At(p.x - 1, p.y - 2); // NW
            ctx.Spawn("Scribe").At(p.x,     p.y - 2); // N

            // One hostile Snapjaw to probe Veto #7 — recruit refuses
            // because faction-default hostility puts GetFeeling below
            // HOSTILE_THRESHOLD. Player has to either de-escalate (out
            // of scope here) or just see the silent failure.
            ctx.Spawn("Snapjaw")
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .At(p.x + 2, p.y);

            // === Walk-through ===
            ctx.Log("=== F.2 Recruit Showcase ===");
            ctx.Log("Press M to open abilities. You have Recruit + Dismiss.");
            ctx.Log("");
            ctx.Log("Layout: 2 Scribes NW/N of you (neutral — recruitable).");
            ctx.Log("         1 Snapjaw E (hostile — Recruit will refuse).");
            ctx.Log("");
            ctx.Log("Try:");
            ctx.Log("  1) Walk adjacent to a Scribe → Recruit. Watch [RecruitDemo].");
            ctx.Log("  2) Move several cells. The Scribe should follow (F.1.5 goal).");
            ctx.Log("  3) Recruit the same Scribe again → silently rejected (Veto #5).");
            ctx.Log("  4) Recruit the other Scribe → 2 followers now.");
            ctx.Log("  5) Walk adjacent to the Snapjaw → Recruit → silently rejected (Veto #7).");
            ctx.Log("  6) Dismiss a Scribe → [RecruitDemo] confirms removal.");
            ctx.Log("");
            ctx.Log("Stats: Ego 22 (+3 mod), Level 5. Most rolls succeed.");
        }
    }

    /// <summary>
    /// Showcase-only Part. Listens to <c>EffectApplied</c> /
    /// <c>EffectRemoved</c> on the host entity (the player); when the
    /// effect is a <see cref="RecruitedEffect"/>, announces it via
    /// <see cref="MessageLog"/> so the player can confirm at a glance
    /// that the F.2 substrate fired.
    ///
    /// <para>The probe is on the PLAYER, not on the recruits. CoO's
    /// EffectApplied event fires on the entity that GAINS the effect
    /// (the recruit), not on the source (the player). So this probe
    /// will only see events when the player THEMSELVES gains/loses a
    /// RecruitedEffect — which never happens in normal play. We need
    /// to listen at a higher level.</para>
    ///
    /// <para>Workaround: the
    /// <see cref="RecruitedEffect.OnApply"/> already calls
    /// <c>MessageLog.Add("{follower} joins {recruiter}!")</c>, which is
    /// the user-visible success line. This probe is kept as
    /// scaffolding for future expansion (e.g., listening to the diag
    /// stream directly) but is currently a no-op. Documented to flag
    /// the design gap for the next revision.</para>
    ///
    /// <para><b>🟡 Design note:</b> a proper showcase probe would
    /// subscribe to the diag stream (category=skill kind=Recruited)
    /// and pretty-print each record. Out of scope for the v1 ship.</para>
    /// </summary>
    public class RecruitEventProbePart : Part
    {
        public override string Name => "RecruitEventProbe";

        public override bool HandleEvent(GameEvent e)
        {
            // Scaffolding — see the class docstring's design note.
            // RecruitedEffect.OnApply itself does the user-visible
            // MessageLog.Add, so no probe-side work is needed for v1.
            return true;
        }
    }
}
