using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>What a status effect means for a thing that is not alive.</summary>
    public enum ObjectStatusVerdict
    {
        /// <summary>It lands.</summary>
        Applies = 0,
        /// <summary>The effect is nonsense on an object of any material —
        /// you cannot make a barrel bleed.</summary>
        Meaningless,
        /// <summary>The effect is sensible on objects, but not on THIS one:
        /// fire on a stone wall, lightning through a wooden fence.</summary>
        WrongMaterial,
    }

    /// <summary>
    /// The authored table of which status effects mean anything on a
    /// non-living object, and on what it is made of
    /// (<c>Docs/OBJECT-INTERACTION-PLAN.md</c> §5.2).
    ///
    /// <para><b>Opt-in, and deliberately so.</b> The tempting rule is
    /// "effects apply to objects unless listed", but that fails OPEN: every
    /// effect added later would silently start working on furniture, and
    /// the first anyone would know is a bleed timer counting down on a
    /// barrel. Everything absent from <see cref="Table"/> is
    /// <see cref="ObjectStatusVerdict.Meaningless"/>.</para>
    ///
    /// <para><b>Why material TAGS rather than the numeric fields.</b>
    /// <c>MaterialPart.Combustibility</c> and <c>Conductivity</c> are
    /// authored on two different scales in the same content file — 84
    /// blueprints use 0-1 (steel is Conductivity 0.8) and a handful use
    /// 0-100 (copper pipe is 100), despite <c>MaterialPart</c>'s docstring
    /// declaring 0-100 canonical. Any threshold picked here would be right
    /// for one group and silently wrong for the other. The
    /// <c>MaterialTagsRaw</c> vocabulary ("Metal", "Conductor", "Organic",
    /// "Wood", "Stone") is authored consistently, so the gates read tags.
    /// See the note in <c>Docs/OBJECT-INTERACTION-PLAN.md</c> §5.2.</para>
    /// </summary>
    public static class ObjectStatusMatrix
    {
        /// <summary>Things that burn.</summary>
        private static readonly string[] Flammable =
            { "Flammable", "Organic", "Wood", "Plant", "Cloth", "Paper", "Fungal" };

        /// <summary>
        /// Things that carry a charge.
        ///
        /// <para><c>Water</c> is in the list because the status grammar
        /// this game teaches is "water douses flame, conducts shock", and
        /// a plain <c>WaterPuddle</c> is authored with only
        /// <c>Liquid,Water</c> — no <c>Conductor</c> tag. Without it the
        /// commonest possible instance of the rule (shock the puddle the
        /// player is standing in) would be refused, which reads as a bug
        /// rather than as a material distinction. <c>BrinePool</c> carries
        /// both tags and qualifies either way.</para>
        /// </summary>
        private static readonly string[] Conductive =
            { "Conductor", "Metal", "Water" };

        /// <summary>
        /// Things with water in or on them — plus the materials that
        /// freeze BRITTLE rather than solid: Metal, Crystal, Chitinous.
        /// The authority is shipped content: cold_plus_metal.json,
        /// cold_plus_crystal.json and cold_plus_chitinous.json each
        /// require SourceState Frozen on that material, and Ice Lance's
        /// shatter identity depends on frozen metal. Metal joined in step 1
        /// (study §4 violator #3); Crystal and Chitinous were found dead
        /// through the door in step 3 — a gated path could never freeze
        /// them, so two shipped reactions could never fire.
        /// </summary>
        private static readonly string[] Freezable =
            { "Wet", "Water", "Liquid", "Ice", "Organic", "Metal", "Crystal", "Chitinous" };

        /// <summary>
        /// THE conduction answer for entity-side gates, exposed so the
        /// electric chain (MaterialPart) asks the same question the
        /// Electrified row answers. Tag-based per the class-level note:
        /// the numeric Conductivity field is authored on two scales and
        /// cannot be trusted as a gate — it remains a STRENGTH input
        /// (how well charge passes), never a capability test.
        /// </summary>
        public static bool IsConductiveMaterial(MaterialPart mat)
        {
            if (mat == null) return false;
            for (int i = 0; i < Conductive.Length; i++)
                if (mat.HasMaterialTag(Conductive[i])) return true;
            return false;
        }

        /// <summary>
        /// The matrix. Key is the effect type; the value answers "is THIS
        /// object made of the right stuff?".
        ///
        /// <para>Anything not in here is refused as meaningless. Adding a
        /// row is the deliberate act of deciding what an effect means on
        /// scenery.</para>
        /// </summary>
        private static readonly Dictionary<Type, Func<Entity, bool>> Table =
            new Dictionary<Type, Func<Entity, bool>>
            {
                // Fire and its aftermath — only on things that burn. A
                // stone wall shrugs it off; a hedgerow does not.
                { typeof(BurningEffect),     e => HasAnyMaterialTag(e, Flammable) },
                { typeof(SmolderingEffect),  e => HasAnyMaterialTag(e, Flammable) },
                { typeof(CharredEffect),     e => HasAnyMaterialTag(e, Flammable) },

                // Electricity needs somewhere to go.
                { typeof(ElectrifiedEffect), e => HasAnyMaterialTag(e, Conductive) },

                // Freezing needs water — in the thing, or on it.
                { typeof(FrozenEffect),      e => HasAnyMaterialTag(e, Freezable) },

                // A liquid coat is prep for what the liquid does next —
                // oil on a hedge is a fire waiting to happen, oil on a
                // stone wall is a mess. Follows the fire rule
                // (Pyromancy_Oilmark was reaching past the matrix to coat
                // scenery — study §4 violator #1, closed by this row).
                { typeof(LiquidCoveredEffect), e => HasAnyMaterialTag(e, Flammable) },

                // Anything can get wet, and anything can be etched.
                { typeof(WetEffect),         e => true },
                { typeof(AcidicEffect),      e => true },

                // Broken is an object effect to begin with.
                { typeof(BrokenEffect),      e => true },
            };

        /// <summary>
        /// Does <paramref name="effect"/> mean anything on
        /// <paramref name="target"/>?
        ///
        /// <para>Creatures are always <see cref="ObjectStatusVerdict.Applies"/>
        /// — this table is about objects, and the creature path has its own
        /// gates (<c>Effect.CanBeAppliedTo</c>, resistances, saves).</para>
        /// </summary>
        public static ObjectStatusVerdict Evaluate(Effect effect, Entity target)
        {
            if (effect == null || target == null) return ObjectStatusVerdict.Meaningless;
            if (target.HasTag("Creature")) return ObjectStatusVerdict.Applies;

            if (!Table.TryGetValue(effect.GetType(), out var materialGate))
                return ObjectStatusVerdict.Meaningless;

            return materialGate(target)
                ? ObjectStatusVerdict.Applies
                : ObjectStatusVerdict.WrongMaterial;
        }

        /// <summary>
        /// The universal door, consulted by <see cref="Entity.ApplyEffect"/>
        /// on EVERY application (status study step 3). Behavior-preserving
        /// rule: creatures pass; objects with a matrix row get the row's
        /// verdict (WrongMaterial refused + diag'd); objects with NO row
        /// pass — today's ungated behavior, kept because twenty shipped
        /// effect types (Broken on equipment, Recruited on followers, gas
        /// statuses on statted props) legitimately land on non-creatures.
        /// </summary>
        public static bool PassesTheDoor(Effect effect, Entity target, Entity source)
            => Gate(effect, target, source, strict: false);

        /// <summary>
        /// The STRICT door — the original TryApply contract, unchanged:
        /// callers who name this entry point are saying "this target may
        /// be scenery; be fail-closed", so an effect with NO matrix row
        /// is refused as Meaningless (a barrel cannot bleed) and diag'd.
        /// Both doors read the SAME table, so they can never disagree on
        /// a WrongMaterial verdict — they differ only on the no-row case.
        /// </summary>
        /// <returns>True if the effect landed.</returns>
        public static bool TryApply(Effect effect, Entity target, Entity source, Zone zone)
        {
            if (!Gate(effect, target, source, strict: true))
            {
                SpellFxCapture.RecordEffect(zone, target, effect?.GetType().Name, applied: false);
                return false;
            }
            // ApplyEffect re-runs the lenient door; a strict pass implies a
            // lenient pass, so this is one table read, not two verdicts.
            return target.ApplyEffect(effect, source, zone);
        }

        private static bool Gate(Effect effect, Entity target, Entity source, bool strict)
        {
            if (effect == null || target == null)
                return !strict; // lenient: substrate null-guards downstream; strict: refuse
            if (target.HasTag("Creature")) return true;

            string reason;
            if (!Table.TryGetValue(effect.GetType(), out var materialGate))
            {
                if (!strict) return true;
                reason = "meaningless_on_objects";
            }
            else if (materialGate(target))
                return true;
            else
                reason = "wrong_material";

            if (Diag.IsChannelEnabled("effect"))
            {
                Diag.Record(
                    category: "effect",
                    kind: "ObjectEffectRefused",
                    actor: source,
                    target: target,
                    payload: new
                    {
                        effect = effect.GetType().Name,
                        reason,
                        blueprintName = target.BlueprintName,
                    });
            }
            return false;
        }

        private static bool HasAnyMaterialTag(Entity target, string[] tags)
        {
            var material = target?.GetPart<MaterialPart>();
            for (int i = 0; i < tags.Length; i++)
            {
                // MaterialPart.Initialize copies its tags onto the entity,
                // so an object can also declare "Flammable" directly in its
                // blueprint Tags without carrying a whole MaterialPart.
                if (material != null && material.HasMaterialTag(tags[i])) return true;
                if (target != null && target.HasTag(tags[i])) return true;
            }
            return false;
        }
    }
}
