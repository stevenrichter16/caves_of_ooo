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

        /// <summary>H1: the ID of whoever extended the cloth (an ID, not a
        /// reference: saving a reference would write the host's whole body
        /// into the save, a duplicate if the host lives in another zone).
        /// Null on cloths from before H1.</summary>
        public string HostId;

        /// <summary>H1: extended by a host sworn to the oath (the player's
        /// <c>Hospitality_ExtendCloth</c>). If that host's oath is withdrawn,
        /// this cloth comes off, wherever the guest is.</summary>
        public bool HostSworn;

        /// <summary>H1: removed because the host broke the oath, not because
        /// the guest did. Public for save-reach.</summary>
        public bool Withdrawn;

        public UnderTheClothEffect()
        {
            Duration = -1;   // the clock, not the turn counter, ends this
            ExpiryTick = WorldClock.CurrentTick + OathTicks;
        }

        public UnderTheClothEffect(Entity host, bool sworn = false) : this()
        {
            HostId = host?.ID;
            HostSworn = sworn;
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + (target.HasTag("Player")
                ? " is named guest. For three days, the cloth is over you."
                : " is named guest. For three days, the cloth is over them."));
            StandDown(target);
            if (Diag.IsChannelEnabled("effect"))
                Diag.Record("effect", "OathClaimed", actor: target,
                    payload: new { expiryTick = ExpiryTick, host = HostId, sworn = HostSworn });
        }

        // A guest who was fighting puts the fight down, and whoever was
        // hunting the guest has to wait. KillGoal also checks Stands each
        // turn; this makes the change take effect at once. OnApply runs
        // before the effect joins the guest's list, so this tests persons
        // directly rather than through Protects/KeepsPeace.
        static void StandDown(Entity guest)
        {
            bool npcGuest = !guest.HasTag("Player");
            var zone = guest.GetPart<BrainPart>()?.CurrentZone ?? guest.SpatialZone;
            if (zone == null) return;
            foreach (var e in zone.GetAllEntities())
            {
                var brain = e.GetPart<BrainPart>();
                if (brain?.Target == null) continue;
                if (ReferenceEquals(e, guest) ? npcGuest && IsPerson(brain.Target) : brain.Target == guest && IsPerson(e))
                    brain.Target = null;
            }
        }

        public override void OnRemove(Entity target)
        {
            if (Withdrawn)
            {
                MessageLog.Add("The cloth over " + target.GetDisplayName() + " is withdrawn by its host.");
                if (Diag.IsChannelEnabled("effect"))
                    Diag.Record("effect", "OathWithdrawn", actor: target, payload: new { host = HostId });
            }
            else if (Broken)
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
            else if (HostSworn && TentRightOath.IsOathbreaker())
            {
                // The host broke the oath while this guest was elsewhere.
                Withdrawn = true;
                Duration = 0;
            }
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
        internal static bool IsPerson(Entity e)
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

        /// <summary>H1: the guest's half. A guest who is not the player keeps
        /// the peace: its would-be hostility toward people is floored too, so
        /// a raider who takes the cloth stops fighting. The player is bound
        /// by <see cref="Break"/> instead, because the player chooses every
        /// swing. Standing is untouched (<c>GetFeelingUnfloored</c>), so the
        /// truce launders nothing.</summary>
        public static bool KeepsPeace(Entity source, Entity target)
        {
            if (source == null || target == null || source == target || source.HasTag("Player")) return false;
            if (!source.HasEffect<UnderTheClothEffect>()) return false;
            return IsPerson(target);
        }

        /// <summary>Does the cloth stand between these two in either
        /// direction? FactionManager.GetFeeling, KillGoal and territory
        /// enforcement consult this.</summary>
        public static bool Stands(Entity source, Entity target)
            => Protects(source, target) || KeepsPeace(source, target);

        /// <summary>The guest swung at a person. Remove the cloth and
        /// land the hammer. Called unconditionally from the two damage
        /// choke points (melee, spell); this helper filters. The rule is
        /// the mirror of <see cref="Protects"/>: whoever's hostility the
        /// cloth floors, swinging at them forfeits the cloth (plan D1:
        /// "guest attacks anyone while under the cloth → effect
        /// removed"). Beasts and the factionless wilds are daily life.</summary>
        public static void Break(Entity guest, Entity victim)
        {
            // A guest caught in their own blast has struck no one. Latent
            // since W2.5; surfaced by H1OathAdversarialTests.
            if (guest == null || guest == victim) return;
            BreakHost(guest, victim);
            var effects = guest?.GetPart<StatusEffectsPart>();
            var oath = effects?.GetEffect<UnderTheClothEffect>();
            if (oath == null) return;
            if (!IsPerson(victim)) return;   // no victim, or beast: not oathbreak

            oath.Broken = true;
            effects.RemoveEffect<UnderTheClothEffect>();
            if (guest.HasTag("Player"))
                TentRightOath.Oathbreak(guest, "guest");
        }

        /// <summary>H1: the host's half. Striking your own guest breaks the
        /// cloth from the other side: canon does not forgive "the guest who
        /// breaks it, and not the host". Called from <see cref="Break"/>, so
        /// both damage choke points carry it.</summary>
        static void BreakHost(Entity attacker, Entity victim)
        {
            if (attacker == null || victim == null || attacker == victim || string.IsNullOrEmpty(attacker.ID)) return;
            var cloth = victim.GetEffect<UnderTheClothEffect>();
            if (cloth == null || cloth.HostId != attacker.ID) return;
            if (Diag.IsChannelEnabled("effect"))
                Diag.Record("effect", "HostStruckGuest", actor: attacker, target: victim);
            if (attacker.HasTag("Player")) TentRightOath.Oathbreak(attacker, "host");
            else Withdraw(attacker.ID, victim.SpatialZone);
        }

        /// <summary>Take the cloth off every guest of this host in the zone.
        /// Guests elsewhere lose it on their next turn (<see cref="OnTurnEnd"/>,
        /// sworn hosts only).</summary>
        public static int Withdraw(string hostId, Zone zone)
        {
            if (string.IsNullOrEmpty(hostId) || zone == null) return 0;
            int n = 0;
            foreach (var e in zone.GetAllEntities())
            {
                var effects = e.GetPart<StatusEffectsPart>();
                var cloth = effects?.GetEffect<UnderTheClothEffect>();
                if (cloth == null || cloth.HostId != hostId) continue;
                cloth.Withdrawn = true;
                effects.RemoveEffect<UnderTheClothEffect>();
                n++;
            }
            return n;
        }
    }
}
