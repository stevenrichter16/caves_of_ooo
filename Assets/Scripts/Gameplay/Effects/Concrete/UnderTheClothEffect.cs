using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W2.5 (Docs/FELLING-W1-W2-PLAN.md §7.6) — the three-day oath, worn.
    /// "Anyone who reaches your tent and claims hospitality is protected
    /// for three days, absolutely... The bond is inviolable. Breaking it
    /// is the only unforgivable crime" (Lore/Factions/07_TentRight.md:27).
    ///
    /// <para><b>What it does:</b> while this effect holds, PEOPLE who
    /// would be hostile to the wearer are floored to non-hostile
    /// (<see cref="Protects"/>, consulted by
    /// <c>FactionManager.GetFeeling</c>). Beasts don't swear — the
    /// covenant is between people, and a scorpion cannot be an
    /// oathbreaker — so their hostility stands. The oath travels with
    /// the person, not the ground: it binds whoever might harm the
    /// guest, wherever the guest walks.</para>
    ///
    /// <para><b>Three days exactly:</b> expiry is measured against
    /// <see cref="WorldClock"/> ticks (<see cref="ExpiryTick"/>), not
    /// against effect-duration turns — a rest that jumps the clock 60
    /// ticks spends 60 ticks of the oath, as it should. Duration is −1
    /// (the persist idiom); the turn tick compares the clock and sets
    /// Duration to 0 when the third day ends.</para>
    ///
    /// <para><b>Breaking it:</b> the guest attacking a PERSON — anyone
    /// Tent-Right, or anyone likewise under the cloth — removes the
    /// effect and lands the single largest reputation loss in the game
    /// (canon :152). Attacking beasts is daily life, not oathbreak.
    /// Indirect harm (a shove into a hazard) is a documented 🧪
    /// deferral, per plan R4.</para>
    /// </summary>
    public class UnderTheClothEffect : Effect
    {
        public override string DisplayName => "under the cloth";

        public override int GetEffectType() => TYPE_GENERAL;

        /// <summary>Three days, in WorldClock ticks.</summary>
        public const int OathTicks = 3 * WorldClock.DayLengthTicks;

        /// <summary>The rep hammer. Largest single loss in the game —
        /// exile, in the wasteland, is death, slower.</summary>
        public const int OathbreakRepLoss = -100;

        /// <summary>The WorldClock tick at which the third day ends.
        /// Public so it survives save/load.</summary>
        public int ExpiryTick;

        /// <summary>Set by <see cref="Break"/> so OnRemove can tell an
        /// oathbreak from an ordinary expiry. Public for save-reach.</summary>
        public bool Broken;

        public UnderTheClothEffect()
        {
            Duration = -1;   // the clock, not the turn counter, ends this
            ExpiryTick = WorldClock.CurrentTick + OathTicks;
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName()
                + " is named guest. For three days, the cloth is over you.");
            if (Diag.IsChannelEnabled("effect"))
                Diag.Record("effect", "OathClaimed", actor: target,
                    payload: new { expiryTick = ExpiryTick });
        }

        public override void OnRemove(Entity target)
        {
            if (Broken)
            {
                MessageLog.Add("The cloth is withdrawn. Every tent will know.");
                if (Diag.IsChannelEnabled("effect"))
                    Diag.Record("effect", "OathBroken", actor: target);
            }
            else
            {
                MessageLog.Add("The third day ends. The world resumes.");
                if (Diag.IsChannelEnabled("effect"))
                    Diag.Record("effect", "OathExpired", actor: target);
            }
        }

        /// <summary>A fresh claim restarts the three days — the host
        /// offers again, the clock starts again.</summary>
        public override bool OnStack(Effect incoming)
        {
            if (!(incoming is UnderTheClothEffect fresh)) return false;
            ExpiryTick = fresh.ExpiryTick;
            MessageLog.Add("The cloth is offered again. Three days, from now.");
            return true;
        }

        public override void OnTurnEnd(Entity target, GameEvent context)
        {
            if (WorldClock.CurrentTick >= ExpiryTick)
                Duration = 0;   // the standard cleanup removes it this tick
        }

        // ════════════════════════════════════════════════════════
        // The two static seams the rest of the game consults
        // ════════════════════════════════════════════════════════

        /// <summary>Does the oath stand between these two? True when the
        /// TARGET is under the cloth and the would-be aggressor is a
        /// person (not Beasts). Consulted by FactionManager.GetFeeling
        /// ONLY on would-be-hostile results, so the effect scan stays off
        /// the friendly path.</summary>
        public static bool Protects(Entity source, Entity target)
        {
            if (source == null || target == null || source == target) return false;
            if (!target.HasEffect<UnderTheClothEffect>()) return false;
            return FactionManager.GetFaction(source) != "Beasts";
        }

        /// <summary>The guest swung at a person. Remove the cloth and
        /// land the hammer. Called from the two damage choke points
        /// (melee, spell) when the attacker wears the cloth and the
        /// victim is a person under its law — Tent-Right, or a fellow
        /// guest.</summary>
        public static void Break(Entity guest, Entity victim)
        {
            var effects = guest?.GetPart<StatusEffectsPart>();
            var oath = effects?.GetEffect<UnderTheClothEffect>();
            if (oath == null) return;
            if (victim != null
                && FactionManager.GetFaction(victim) != "TentRight"
                && !victim.HasEffect<UnderTheClothEffect>())
                return;   // beasts and strangers: not oathbreak

            oath.Broken = true;
            effects.RemoveEffect<UnderTheClothEffect>();
            if (guest.HasTag("Player"))
                PlayerReputation.Modify("TentRight", OathbreakRepLoss);
        }
    }
}
