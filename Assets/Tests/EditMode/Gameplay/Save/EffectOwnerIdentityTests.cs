using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.8.3 — Effect.Owner cross-Part identity. See
    /// <c>Docs/SAVE-LOAD-AUDIT.md §SL.8</c>.
    ///
    /// <para><see cref="Effect.Owner"/> is excluded from
    /// <c>SaveEffect</c>'s field filter (SaveSystem.cs:1183) and never
    /// restored by <c>LoadEffect</c> directly. Instead,
    /// <c>StatusEffectsPart</c>'s load-side hook re-binds each effect's
    /// Owner to the host Entity. SL.6.1 pinned that Owner is non-null
    /// post-load; SL.8.3 pins the stronger contract: <b>Owner is the
    /// SAME loaded Entity instance</b>, not just any non-null Entity.</para>
    ///
    /// <para>It also probes the cross-actor case: a defender's
    /// <c>BurningEffect</c> with <c>IgnitionSource = attacker</c> must
    /// preserve TWO distinct entity refs — Owner=defender,
    /// IgnitionSource=attacker — without crossing the wires.</para>
    /// </summary>
    public class EffectOwnerIdentityTests
    {
        [Test]
        public void Effect_Owner_IsSame_AsLoadedHostEntity()
        {
            var actor = new Entity { ID = "host", BlueprintName = "Host" };
            actor.ForceApplyEffect(new RootedEffect(duration: 4));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var rooted = loaded.GetEffect<RootedEffect>();
            Assert.IsNotNull(rooted);
            Assert.AreSame(loaded, rooted.Owner,
                "Effect.Owner must be the SAME instance as the loaded "
                + "host Entity. AreSame, not just AreEqual — a future "
                + "regression that creates a fresh Entity with the same "
                + "ID would pass AreEqual but break the live wiring.");
        }

        [Test]
        public void MultipleEffects_OnSameActor_AllShareOwnerIdentity()
        {
            // Three effects on one actor. Pin all three Owner pointers
            // resolve to the SAME loaded instance.
            var actor = new Entity { ID = "host", BlueprintName = "Host" };
            actor.ForceApplyEffect(new RootedEffect(duration: 4));
            actor.ForceApplyEffect(new StunnedEffect(duration: 2));
            actor.ForceApplyEffect(new ConfusedEffect(duration: 3));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreSame(loaded, loaded.GetEffect<RootedEffect>().Owner,
                "RootedEffect.Owner == loaded actor.");
            Assert.AreSame(loaded, loaded.GetEffect<StunnedEffect>().Owner,
                "StunnedEffect.Owner == loaded actor.");
            Assert.AreSame(loaded, loaded.GetEffect<ConfusedEffect>().Owner,
                "ConfusedEffect.Owner == loaded actor.");
        }

        [Test]
        public void Effect_Owner_AndIgnitionSource_DistinctInstances()
        {
            // Adversarial: BurningEffect carries TWO entity refs:
            //   Owner          → the defender (host)
            //   IgnitionSource → the attacker
            // These must round-trip as two DIFFERENT loaded instances.
            // A buggy load that crosses the wires (Owner ← attacker)
            // would still pass HasEffect / IsNotNull but would break
            // burn-attribution.
            var attacker = new Entity { ID = "atk", BlueprintName = "Attacker" };
            var defender = new Entity { ID = "def", BlueprintName = "Defender" };
            defender.ForceApplyEffect(new BurningEffect(intensity: 1f, source: attacker));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(defender);
            var burn = loaded.GetEffect<BurningEffect>();
            Assert.IsNotNull(burn);
            Assert.AreSame(loaded, burn.Owner,
                "Owner == loaded defender (host).");
            Assert.IsNotNull(burn.IgnitionSource,
                "IgnitionSource is non-null — survives via WriteEntityReference.");
            Assert.AreNotSame(burn.Owner, burn.IgnitionSource,
                "Owner and IgnitionSource must be DISTINCT entities. "
                + "Catches a bug where load crosses the wires and both "
                + "fields point at the same loaded instance.");
            Assert.AreEqual("def", burn.Owner.ID);
            Assert.AreEqual("atk", burn.IgnitionSource.ID,
                "IgnitionSource still has the attacker's ID, not the "
                + "defender's — proves the two ref slots are independent.");
        }

        [Test]
        public void Effect_OnHost_WithoutEntityRefPayload_OwnerOnlySet()
        {
            // Counter-check: a Tier-A effect (no entity ref payload —
            // RootedEffect has no IgnitionSource etc.) round-trips with
            // Owner correctly bound, AND any other-name fields stay
            // null. Catches a bug that pre-populates "future" entity
            // ref fields with the host (which would be wrong for
            // most Tier-A effects).
            var actor = new Entity { ID = "host", BlueprintName = "Host" };
            actor.ForceApplyEffect(new RootedEffect(duration: 4));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var rooted = loaded.GetEffect<RootedEffect>();
            Assert.AreSame(loaded, rooted.Owner,
                "Owner is bound by StatusEffectsPart's load hook.");
            // RootedEffect has no other entity-ref fields; this assertion
            // is more of a sanity probe than a hard invariant. It exists
            // to fail-fast if someone adds a public Entity field to
            // RootedEffect without thinking through round-trip.
            int declaredEntityFields = 0;
            foreach (var f in typeof(RootedEffect).GetFields(
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.DeclaredOnly))
            {
                if (f.FieldType == typeof(Entity)) declaredEntityFields++;
            }
            Assert.AreEqual(0, declaredEntityFields,
                "Sanity probe: RootedEffect declares no public Entity "
                + "fields of its own (Owner is inherited from Effect, "
                + "DeclaredOnly skips it). If this fails, someone added "
                + "an Entity ref to RootedEffect without thinking through "
                + "the round-trip — review per SL.6 / SL.8.3 contracts.");
        }
    }
}
