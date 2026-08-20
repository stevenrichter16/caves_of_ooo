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
    /// <para><b>Breaking it:</b> the guest attacking any PERSON — the
    /// exact mirror of the floor: whoever the cloth silences, swinging
    /// at them forfeits it (plan D1: "guest attacks anyone") — removes
    /// the effect and lands the single largest reputation loss in the
    /// game (canon :152). Attacking beasts and the factionless wilds is
    /// daily life, not oathbreak. Indirect harm (a shove into a hazard)
    /// is a documented 🧪 deferral, per plan R4.</para>
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

        /// <summary>Who the covenant binds — and who it therefore also
        /// covers. A person is a FACTIONED entity that is not Beasts:
        /// the wilds never signed (a factionless critter can neither
        /// swear the oath nor be its victim), and "a scorpion cannot be
        /// an oathbreaker". A cloth-wearer counts regardless — a fellow
        /// guest is under the law by definition. This single test backs
        /// BOTH static seams so floor and break can never drift apart
        /// again (the W2 close-out's one-way-shield bug was exactly
        /// that drift).</summary>
        private static bool IsPerson(Entity e)
        {
            if (e == null) return false;
            if (e.HasEffect<UnderTheClothEffect>()) return true;
            string faction = FactionManager.GetFaction(e);
            return faction != null && faction != "Beasts";
        }

        /// <summary>Does the oath stand between these two? True when the
        /// TARGET is under the cloth and the would-be aggressor is a
        /// person (see <see cref="IsPerson"/>). Consulted by
        /// FactionManager.GetFeeling ONLY on would-be-hostile results,
        /// so the effect scan stays off the friendly path.</summary>
        public static bool Protects(Entity source, Entity target)
        {
            if (source == null || target == null || source == target) return false;
            if (!target.HasEffect<UnderTheClothEffect>()) return false;
            return IsPerson(source);
        }

        /// <summary>The guest swung at a person. Remove the cloth and
        /// land the hammer. Called unconditionally from the two damage
        /// choke points (melee, spell); this helper filters. The rule is
        /// the mirror of <see cref="Protects"/>: whoever's hostility the
        /// cloth floors, swinging at them forfeits the cloth (plan D1:
        /// "guest attacks anyone while under the cloth → effect
        /// removed"). Beasts and the factionless wilds are daily life.</summary>
        public static void Break(Entity guest, Entity victim)
        {
            var effects = guest?.GetPart<StatusEffectsPart>();
            var oath = effects?.GetEffect<UnderTheClothEffect>();
            if (oath == null) return;
            if (!IsPerson(victim)) return;   // no victim, or beast: not oathbreak

            oath.Broken = true;
            effects.RemoveEffect<UnderTheClothEffect>();
            if (guest.HasTag("Player"))
                PlayerReputation.Modify("TentRight", OathbreakRepLoss);
        }
    }
}
