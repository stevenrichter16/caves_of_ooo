using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W3.4 (Docs/FELLING-W3-PLAN.md §3) — skin that answers contact.
    /// The Bandfrog's defense: strike it at arm's reach and its caustic
    /// slime bites back. A PART, not an effect, because it is anatomy —
    /// permanent, unremovable, uncurable (a panacea cures ailments, not
    /// what an animal is made of).
    ///
    /// <para><b>Contact means contact:</b> the reflect fires only when
    /// the attacker stands adjacent (Chebyshev ≤ 1) — all melee
    /// qualifies, a spell hurled from across the marsh does not. Zone
    /// resolved via <c>SettlementRuntime.ActiveZone</c> (the TakeDamage
    /// event carries no zone; same fallback as BurnOffGasPart).</para>
    ///
    /// <para><b>No reflect-on-reflect:</b> the reflected damage carries
    /// the <see cref="ReflectAttribute"/> and the part ignores incoming
    /// damage that carries it — two caustic-skinned creatures trading a
    /// blow must not ping-pong to mutual death. (ScaldingVeilEffect has
    /// this exposure between two veiled entities; flagged separately —
    /// this part is born with the guard.)</para>
    /// </summary>
    public class CausticSkinPart : Part
    {
        public override string Name => "CausticSkin";

        /// <summary>Acid damage returned to a touching attacker.</summary>
        public int ReflectDamage = 2;

        /// <summary>Percent-in-100 chance the contact also poisons.</summary>
        public int PoisonChance = 25;

        /// <summary>Turns of PoisonedEffect on a poisoning contact.</summary>
        public int PoisonDuration = 3;

        /// <summary>Marker attribute on reflected damage; incoming damage
        /// carrying it is never answered (the recursion guard).</summary>
        public const string ReflectAttribute = "SkinContact";

        /// <summary>Damage that arrives as element, not as touch — the
        /// skin answers hands and teeth, never the fire somebody lit.
        /// W3 re-review: this was six exact strings, but the damage
        /// model treats Lightning/Shock/Electricity, Ice/Freeze, and
        /// Light/Laser as aliases (DamageAttributeFlags) — an
        /// ElectrifiedEffect tick carries only "Lightning" and would
        /// have reopened the arsonist bug for electricity the moment
        /// its source-threading lands. The flag helpers collapse the
        /// aliases; Poison stays a string check (no flag exists).</summary>
        private static bool IsElemental(Damage damage)
            => damage.IsHeatDamage() || damage.IsColdDamage()
            || damage.IsElectricDamage() || damage.IsAcidDamage()
            || damage.IsLightDamage() || damage.IsDisintegrationDamage()
            || damage.HasAttribute("Poison");

        public static System.Random TestRng;
        private static readonly System.Random _defaultRng = new System.Random();

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "TakeDamage") return true;

            var damage = e.GetParameter<Damage>("Damage");
            if (damage == null || damage.HasAttribute(ReflectAttribute)) return true;

            var attacker = e.GetParameter<Entity>("Source");
            // Environmental damage (burning, poison ticks) has no one to
            // answer; self-damage must not loop. Wx opt review §3c: these
            // early-outs sit ABOVE the elemental check so a source-less
            // burn tick doesn't emit a SkinContactRejected with a null
            // target every turn — a rejection record means an attacker
            // actually existed to reject. See DiagChannelSplitTests.
            if (attacker == null || attacker == ParentEntity) return true;

            // W3.7 audit (H4): fire is not touch. A burning frog's tick
            // damage carries Source = whoever lit it; an adjacent
            // arsonist must not be "in contact" every tick. Elemental
            // damage never triggers the skin — only physical contact —
            // and like the adjacency gate below, the rejection names
            // its reason (the observability rule).
            if (IsElemental(damage))
            {
                if (Diag.IsChannelEnabled("damage"))
                    Diag.Record("damage", "SkinContactRejected", ParentEntity,
                        attacker,
                        new { reason = "elemental_not_contact" });
                return true;
            }

            if (attacker.GetStatValue("Hitpoints", 0) <= 0) return true;

            var zone = SettlementRuntime.ActiveZone;
            if (zone == null) return true;
            var mine = zone.GetEntityPosition(ParentEntity);
            var theirs = zone.GetEntityPosition(attacker);
            if (mine.x < 0 || theirs.x < 0) return true;
            int dist = System.Math.Max(System.Math.Abs(mine.x - theirs.x),
                                       System.Math.Abs(mine.y - theirs.y));
            if (dist > 1)
            {
                // A gate that rejects emits its reason (CLAUDE.md).
                if (Diag.IsChannelEnabled("damage"))
                    Diag.Record("damage", "SkinContactRejected", ParentEntity, attacker,
                        new { reason = "not_adjacent", dist });
                return true;
            }

            var reflect = new Damage(ReflectDamage);
            reflect.AddAttribute("Acid");
            reflect.AddAttribute(ReflectAttribute);
            CombatSystem.ApplyDamage(attacker, reflect, ParentEntity, zone);
            MessageLog.Add(ParentEntity.GetDisplayName() + "'s skin sears "
                + attacker.GetDisplayName() + "!");

            var rng = TestRng ?? _defaultRng;
            if (PoisonChance > 0 && attacker.GetStatValue("Hitpoints", 0) > 0
                && rng.Next(100) < PoisonChance)
                attacker.ApplyEffect(new PoisonedEffect(PoisonDuration), ParentEntity, zone);

            if (Diag.IsChannelEnabled("damage"))
                Diag.Record("damage", "SkinContactReflect", ParentEntity, attacker,
                    new { amount = ReflectDamage, poisonChance = PoisonChance });
            return true;
        }
    }
}
