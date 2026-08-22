using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W4.4 (Docs/FELLING-W4-PLAN.md §6) — the Driving Bloom, on a body.
    ///
    /// <para>The Bloom is TERRAIN, not a faction: no dialogue, no rep
    /// track, never funny. A Bloomed creature is being WORN — each turn
    /// the effect pushes a <see cref="BloomGoal"/> onto the bearer's
    /// brain (guarded by HasGoal, the WitnessedEffect.cs:60-93 shape):
    /// attack whatever stands adjacent regardless of faction, otherwise
    /// leave the others and walk toward open ground. The push lives in
    /// OnTurnStart rather than OnApply so a loaded save self-heals — the
    /// goal is rebuilt on the bearer's first turn (goal stacks do not
    /// round-trip; the effect's public fields do).</para>
    ///
    /// <para><b>R4 — why no TYPE_NEGATIVE (the cure taxonomy).</b> The
    /// cure-all tonic strips by the TYPE_NEGATIVE bit
    /// (CureTonicPart.cs:34), and passive omission is the codebase's ONE
    /// shipped exemption mechanism — UnderTheClothEffect.cs:39,
    /// ScaldingVeil, Hibernating all omit bits deliberately, so in this
    /// codebase TYPE_NEGATIVE already reads "tonic-curable", not "bad
    /// for you". The Bloom is not an ailment a tonic understands: it is
    /// cured ONLY by the Choir's early-stage ritual (the conversation
    /// CureEffect action matches by NAME and ignores type flags, so the
    /// ritual works regardless). Pinned both ways in BloomedEffectTests.</para>
    ///
    /// <para><b>Duration −1</b> — the Choir or death ends this, not the
    /// clock (the UnderTheClothEffect.cs:58 convention).</para>
    ///
    /// <para><b>Eruption (SM-C)</b> hooks OnRemove gated on
    /// LastRemovalCause == CAUSE_OWNER_DIED — effects never receive the
    /// Died event; StatusEffectsPart translates death into removal with
    /// that cause, and a Choir cure arrives as CAUSE_EXTERNAL, so the
    /// same seam separates erupting from being healed.</para>
    /// </summary>
    public class BloomedEffect : Effect
    {
        public override string DisplayName => "Bloomed";

        // R4: TYPE_GENERAL only — see the class docstring.
        public override int GetEffectType() => TYPE_GENERAL;

        // The Choir's color family — the bearer reads as the Bloom's.
        public override string GetRenderColorOverride() => "&m";

        // The goal this effect pushed; OnRemove surgically removes
        // exactly this instance (WitnessedEffect shape). Private on
        // purpose: not serialized, rebuilt by OnTurnStart after load.
        private BloomGoal _pushedGoal;

        public BloomedEffect()
        {
            Duration = -1;
        }

        public override void OnApply(Entity target)
        {
            if (target == null) return;
            // Voice gate: terrain-grade, never funny — and R9d-backed:
            // the goal below actually walks the bearer there.
            MessageLog.Add(target.GetDisplayName() + " turns toward the open ground.");
        }

        public override void OnTurnStart(Entity target)
        {
            var brain = target?.GetPart<BrainPart>();
            if (brain == null) return; // the player path is SM-F; NPC-only here
            if (brain.HasGoal<BloomGoal>()) return;

            _pushedGoal = new BloomGoal();
            brain.PushGoal(_pushedGoal);
            if (Diag.IsChannelEnabled("effect"))
                Diag.Record("effect", "BloomCompelled", target, null,
                    new { blueprintName = target.BlueprintName,
                          hp = target.GetStatValue("Hitpoints", -1) });
        }

        public override void OnRemove(Entity target)
        {
            // SM-C — eruption. Effects never receive the Died event;
            // StatusEffectsPart translates death into
            // RemoveAllEffects(CAUSE_OWNER_DIED), so the cause gate IS
            // the death gate — a Choir cure arrives as CAUSE_EXTERNAL
            // and must not open the body (pinned both ways). The zone
            // resolve mirrors BurnOffGasPart (TakeDamage carries no
            // Zone either); the cell is still resolvable because
            // StatusEffectsPart sits at Parts[0] — removal runs before
            // CorpsePart's corpse AND before zone.RemoveEntity, so the
            // corpse spawns INTO the spores.
            if (LastRemovalCause == CAUSE_OWNER_DIED)
                Erupt(target);

            if (_pushedGoal == null) return;
            target?.GetPart<BrainPart>()?.RemoveGoal(_pushedGoal);
            _pushedGoal = null;
        }

        private static void Erupt(Entity target)
        {
            if (target == null) return;
            var zone = SettlementRuntime.ActiveZone;
            if (zone == null) return;
            var pos = zone.GetEntityPosition(target);
            if (pos.x < 0) return;

            var gas = GasFactory.SpawnGas(zone, pos.x, pos.y, "bloom-spores",
                creator: target);
            // Voice gate: terrain-grade. The line only speaks when the
            // spores are real (R9d).
            if (gas != null)
                MessageLog.Add(target.GetDisplayName() + " opens.");
            if (Diag.IsChannelEnabled("effect"))
                Diag.Record("effect", "BloomErupted", target, gas,
                    new { blueprintName = target.BlueprintName,
                          x = pos.x, y = pos.y, spawned = gas != null });
        }

        public override bool OnStack(Effect incoming)
        {
            // Already taken. A second Bloom changes nothing — there is
            // no "more worn".
            return true;
        }
    }
}
