namespace CavesOfOoo.Core
{
    /// <summary>
    /// W2.3 (Docs/FELLING-W1-W2-PLAN.md §7.6) — the Beating's sun in the
    /// body. Stacking dehydration: −1 Strength and −1 Agility per stack,
    /// up to <see cref="MaxStacks"/>. Applied by
    /// <see cref="BeatingGlareSystem"/> during the Height band; cured by
    /// water — standing in it (checked in this effect's own turn tick),
    /// drawing it at a well (<see cref="WellPart"/>), or drinking a
    /// tonic (one stack per drink, <c>TonicPart</c>).
    ///
    /// <para><b>Persistent until cured:</b> Duration is −1, the shipped
    /// idiom — <c>Effect.OnTurnEnd</c> only decrements positive
    /// durations and <c>StatusEffectsPart</c> only removes at exactly 0,
    /// so a negative duration never expires. The sun does not get bored.</para>
    ///
    /// <para>Stat bookkeeping mirrors <see cref="HobbledEffect"/>:
    /// penalties applied in OnApply/OnStack, reversed in OnRemove.
    /// <see cref="Stacks"/> is public so it survives save/load.</para>
    /// </summary>
    public class ParchedEffect : Effect
    {
        public override string DisplayName =>
            Stacks >= MaxStacks ? "parched (badly)" : "parched";

        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        public const int MaxStacks = 3;
        public const int PenaltyPerStack = 1;

        /// <summary>Current dehydration depth, 1..MaxStacks. Public so
        /// the save round-trips it.</summary>
        public int Stacks = 1;

        public ParchedEffect()
        {
            Duration = -1;   // persists until cured
        }

        public override void OnApply(Entity target)
        {
            ShiftPenalties(target, +Stacks * PenaltyPerStack);
            MessageLog.Add(target.GetDisplayName() + " is parched by the glare.");
        }

        public override void OnRemove(Entity target)
        {
            ShiftPenalties(target, -Stacks * PenaltyPerStack);
            MessageLog.Add(target.GetDisplayName() + " is no longer parched.");
        }

        public override bool OnStack(Effect incoming)
        {
            if (!(incoming is ParchedEffect)) return false;
            if (Stacks < MaxStacks)
            {
                Stacks++;
                ShiftPenalties(Owner, +PenaltyPerStack);
                MessageLog.Add(Owner.GetDisplayName() + " is parched deeper.");
            }
            // At the cap the incoming stack is absorbed silently — the sun
            // cannot make it worse, and a message every exposure interval
            // would be noise.
            return true;
        }

        /// <summary>Water underfoot ends it — canon's own lexicon for the
        /// culture that lives here is "water, shade". Sets Duration to 0
        /// so the standard end-of-turn cleanup removes the effect (and
        /// OnRemove reverses the penalties) on this same tick.</summary>
        public override void OnTurnEnd(Entity target, GameEvent context)
        {
            var zone = context?.GetParameter<Zone>("Zone");
            if (zone == null) return;
            var pos = zone.GetEntityPosition(target);
            if (pos.x < 0) return;
            if (zone.TileState.HasCoating(pos.x, pos.y, "water"))
            {
                Duration = 0;
                MessageLog.Add("The water takes the parch out of "
                    + target.GetDisplayName() + ".");
            }
        }

        /// <summary>One stack of relief — a drink helps, a well cures.
        /// Static so <c>TonicPart</c> can call it without knowing the
        /// stack bookkeeping.</summary>
        public static void ReduceOneStack(Entity target)
        {
            var effects = target?.GetPart<StatusEffectsPart>();
            var parched = effects?.GetEffect<ParchedEffect>();
            if (parched == null) return;

            if (parched.Stacks > 1)
            {
                parched.Stacks--;
                ShiftPenalties(target, -PenaltyPerStack);
                MessageLog.Add("The drink pushes the parch back.");
            }
            else
            {
                effects.RemoveEffect<ParchedEffect>();
            }
        }

        private static void ShiftPenalties(Entity target, int delta)
        {
            if (target == null) return;
            var str = target.GetStat("Strength");
            if (str != null) str.Penalty += delta;
            var agi = target.GetStat("Agility");
            if (agi != null) agi.Penalty += delta;
        }
    }
}
