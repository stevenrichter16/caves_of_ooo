using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// LQ.4 — the on-creature liquid coating (closes plan gap (b): a
    /// creature stepping into a <see cref="LiquidPoolPart"/> cell now
    /// actually carries the liquid). Mirrors Qud's
    /// <c>XRL.World.Effects.LiquidCovered</c> reduced to CoO's phase-1
    /// single-liquid scalar model.
    ///
    /// <para><b>Divergence #1 (no parts-per-1000 mixing).</b> A second,
    /// different liquid merging via <see cref="OnStack"/> does NOT blend
    /// into a ratio. Instead the larger contact pool wins the surface id
    /// (the dominant liquid is what reacts downstream in LQ.5+); the
    /// scalar amounts add. Same-liquid re-entry just accumulates.</para>
    ///
    /// <para><b>Divergence #3 (water also keeps you wet).</b> A water
    /// coat additionally ensures a <see cref="WetEffect"/> so the
    /// pre-existing wet→electric coupling in
    /// <see cref="ElectrifiedEffect"/> — and the pinned
    /// ElectrifiedEffectDamageTests — keep working untouched. No other
    /// liquid applies WetEffect.</para>
    ///
    /// <para><b>Divergence #5 (once-on-enter + persistent dry-down).</b>
    /// The coat is applied once when the creature steps into the cell
    /// (the <c>EntityEnteredCell</c> dispatch in
    /// <see cref="LiquidPoolPart.HandleEvent"/>); standing still does not
    /// re-coat. It then dries down each end-of-turn by the liquid's
    /// Fluidity+Evaporativity (heat-accelerated, mirroring
    /// <see cref="WetEffect.OnTurnEnd"/>) and removes itself at zero.</para>
    ///
    /// <para><b>Flyweight contract (LQ.3 review finding F3).</b>
    /// <see cref="LiquidRegistry.Get"/> returns the shared mutable
    /// <see cref="LiquidDefinition"/>. This effect only ever reads
    /// scalars out of it — never holds-and-mutates.</para>
    ///
    /// <para>Fields are plain public so the effect round-trips through
    /// the reflection save path with no <c>FormatVersion</c> bump
    /// (plan §A5), and the optional-arg ctor doubles as the
    /// parameterless ctor reflection deserialization needs.</para>
    /// </summary>
    public class LiquidCoveredEffect : Effect
    {
        /// <summary>Registry id of the dominant liquid on the
        /// creature (e.g. "water", "oil", "acid").</summary>
        public string LiquidId;

        /// <summary>Scalar contact amount. Drives dry-down and the
        /// downstream consequence hooks (LQ.5+). Clamped to ≥ 0.</summary>
        public int Amount;

        /// <summary>
        /// LQ.6: flat record of EXACTLY the stat deltas currently pushed
        /// onto the wearer ("StatName:delta,StatName:delta") so removal
        /// reverses precisely — even after a stronger-wins id swap
        /// (<see cref="OnStack"/>) changes <see cref="LiquidId"/>, or the
        /// shared flyweight changes. A plain <c>string</c> rather than a
        /// <c>List</c> deliberately: it round-trips through the same
        /// reflection save path that already carries
        /// <see cref="LiquidId"/> (proven LQ.4) with no
        /// <c>FormatVersion</c> bump and no <c>List</c>-round-trip risk,
        /// mirroring the <c>EquipBonuses</c> flat-string convention.
        /// Empty = nothing applied. <see cref="OnApply"/> is never re-run
        /// on load, so a non-empty value loaded from a save means the
        /// deltas are already baked into the (separately round-tripped)
        /// <c>Stat.Bonus</c> — never double-applied.
        /// </summary>
        public string AppliedModsRaw = "";

        /// <summary>
        /// LB.4 attachment-tracking flag for the lantern-beetle ichor
        /// path. True iff <see cref="OnApply"/> *added* a fresh
        /// <see cref="LightSourcePart"/> to the wearer (because the
        /// liquid def declared <c>LightRadius &gt; 0</c> AND the wearer
        /// did not already have a LightSourcePart — held lanterns are
        /// respected). <see cref="OnRemove"/> only strips the
        /// LightSourcePart when this flag is set, so a player carrying
        /// their own lantern keeps it after the coat ends. Public for
        /// reflection save round-trip (mirrors AppliedModsRaw).
        /// </summary>
        public bool AddedLightSource;

        /// <summary>A coat at/above this <see cref="LiquidDefinition.Conductivity"/>
        /// amplifies incoming Lightning damage, and lets a conductive
        /// NON-water coat double an <see cref="ElectrifiedEffect"/>'s
        /// charge the same way <see cref="WetEffect"/> does. 0–100 scale.</summary>
        public const int CONDUCTIVITY_AMPLIFY_THRESHOLD = 50;

        /// <summary>A coat at/above this <see cref="LiquidDefinition.Combustibility"/>
        /// amplifies incoming Fire damage (oil, pitch). 0–100 scale.</summary>
        public const int COMBUSTIBLE_AMPLIFY_THRESHOLD = 50;

        public LiquidCoveredEffect(string liquidId = "", int amount = 0)
        {
            LiquidId = liquidId ?? "";
            Amount = amount < 0 ? 0 : amount;
            Duration = DURATION_INDEFINITE;
        }

        public override int GetEffectType() => TYPE_CONTACT | TYPE_REMOVABLE;

        /// <summary>
        /// Pulled live from the liquid definition's
        /// <see cref="LiquidDefinition.Adjective"/> so a new liquid only
        /// needs a JSON row. Falls back to a generic label when the
        /// registry is uninitialized or the id is unknown.
        /// </summary>
        public override string DisplayName
        {
            get
            {
                if (LiquidRegistry.IsInitialized)
                {
                    var def = LiquidRegistry.Get(LiquidId);
                    if (def != null && !string.IsNullOrEmpty(def.Adjective))
                        return def.Adjective;
                }
                return "liquid-covered";
            }
        }

        public override void OnApply(Entity target)
        {
            if (target != null)
                MessageLog.Add(target.GetDisplayName() + " is covered in " + DisplayName + ".");
            RefreshWaterCoupling(target);
            ApplyStatModifiers(target);
            ApplyLightSource(target);
        }

        public override void OnRemove(Entity target)
        {
            RemoveLightSource(target);
            ReverseStatModifiers(target);
            // Observability contract (every gate emits a record):
            // LQ.4 emits liquid/Coated on apply; this is the paired
            // terminal record so a query can confirm a coat's full
            // lifecycle (Coated → … → CoatExpired) without a grep.
            Diag.Record("liquid", "CoatExpired", target, null,
                new { liquidId = LiquidId, cause = LastRemovalCause });
            if (target != null)
                MessageLog.Add("The " + DisplayName + " coating wears off " + target.GetDisplayName() + ".");
        }

        /// <summary>
        /// Divergence #1: same-liquid re-entry accumulates; a
        /// different liquid with a larger contact amount takes over the
        /// surface id, and the amounts add either way. Always returns
        /// true so <see cref="StatusEffectsPart"/> never stacks a second
        /// instance (merge-not-stack — pinned by
        /// LiquidCoatingTests.ReEnterPool_Merges_NotStacks).
        /// </summary>
        public override bool OnStack(Effect incoming)
        {
            if (incoming is LiquidCoveredEffect other)
            {
                bool idChanges = other.LiquidId != LiquidId && other.Amount > Amount;
                if (idChanges)
                {
                    // The dominant liquid is changing. Reverse the
                    // OUTGOING liquid's stat deltas BEFORE the id swap
                    // (ReverseStatModifiers reads AppliedModsRaw, not the
                    // def, so it's exact regardless of registry state),
                    // then apply the INCOMING liquid's deltas after.
                    // Without this, a brine→pitch merge would leak
                    // brine's +HeatRes forever.
                    ReverseStatModifiers(Owner);
                    LiquidId = other.LiquidId;
                }
                Amount += other.Amount;
                if (idChanges)
                    ApplyStatModifiers(Owner);
                RefreshWaterCoupling(Owner);
                return true;
            }
            return false;
        }

        public override void OnTurnEnd(Entity target)
        {
            int dry = 1;
            if (LiquidRegistry.IsInitialized)
            {
                var def = LiquidRegistry.Get(LiquidId);
                if (def != null)
                {
                    dry = def.Fluidity + def.Evaporativity;
                    if (dry < 1) dry = 1; // never-zero so a coat always dries
                }
            }

            // Heat accelerates dry-down, mirroring WetEffect's
            // temperature coupling (WetEffect.OnTurnEnd:49-54).
            var thermal = target?.GetPart<ThermalPart>();
            if (thermal != null && thermal.Temperature > 50f)
                dry += (int)((thermal.Temperature - 50f) * 0.05f);

            Amount -= dry;
            if (Amount <= 0)
            {
                Amount = 0;
                Duration = 0; // StatusEffectsPart.HandleEndTurn cleans up
            }
        }

        /// <summary>
        /// LQ.5: a coat with a <see cref="LiquidDefinition.PerTurnDamage"/>
        /// (acid) deals that damage at the owner's turn start, attributed
        /// by Type so it routes through the matching resistance in
        /// <see cref="CombatSystem.ApplyResistances"/>. Mirrors
        /// <see cref="ElectrifiedEffect.OnTurnStart"/>'s tick shape.
        ///
        /// <para><b>FollowOnEffect deferred (⚪).</b> No shipped liquid
        /// sets it; "oil coat near fire → BurningEffect" needs
        /// reaction-system coupling (the existing untouched
        /// <c>oil_plus_fire.json</c>) that is bigger than LQ.5 — see
        /// §11 LQ.5 scope-prune.</para>
        /// </summary>
        public override void OnTurnStart(Entity target, GameEvent context)
        {
            if (target == null) return;
            if (!LiquidRegistry.IsInitialized) return;
            var def = LiquidRegistry.Get(LiquidId);
            if (def == null || def.PerTurnDamage == null) return;
            int amt = def.PerTurnDamage.Amount;
            if (amt == 0) return;
            if (target.GetStatValue("Hitpoints", 0) <= 0) return;

            if (amt > 0)
            {
                // Damage path (existing — acid/lava/ink/choir/bog).
                var dmg = new Damage(amt);
                if (!string.IsNullOrEmpty(def.PerTurnDamage.Type))
                    dmg.AddAttribute(def.PerTurnDamage.Type);
                var zone = context?.GetParameter<Zone>("Zone");
                CombatSystem.ApplyDamage(target, dmg, source: null, zone);
                return;
            }

            // LB.3 heal path: signed PerTurnDamage (convalessence). The
            // Damage.Amount setter clamps ≥ 0 so we can't route through
            // ApplyDamage — heal via direct HP add, capped to Max
            // (mirrors how Stat.Bonus modifications respect Max via
            // Stat.Value's compute). Emit liquid/HealTick for symmetry
            // with the damage tick's observability.
            int heal = -amt;
            var hp = target.GetStat("Hitpoints");
            if (hp == null) return;
            int before = hp.BaseValue;
            int after = System.Math.Min(before + heal, hp.Max);
            int gained = after - before;
            hp.BaseValue = after;
            Diag.Record("liquid", "HealTick", actor: target, target: null,
                payload: new { liquidId = LiquidId, requested = heal, gained,
                               hpBefore = before, hpAfter = after });
        }

        /// <summary>
        /// LQ.5: a coat changes how elemental damage lands, BEFORE
        /// resistance. Verified blocking-step-0: CombatSystem fires
        /// <c>BeforeTakeDamage</c> then re-reads <c>Damage.Amount</c>
        /// for the HP decrement (<see cref="CombatSystem"/>:751-836;
        /// dispatch <see cref="StatusEffectsPart"/>:491-497 fires this
        /// for every effect). The <c>Damage.Amount</c> setter clamps
        /// ≥ 0, so an over-dampen can never heal.
        ///
        /// <para><b>Divergence #6 (no double-amplify).</b> When an
        /// <see cref="ElectrifiedEffect"/> is present it OWNS electric
        /// amplification (doubles its own Charge on apply when
        /// wet/conductive, ticks its own Lightning damage).
        /// LiquidCovered only amplifies *direct* Lightning when no
        /// ElectrifiedEffect is on the target — prevents the 4× bug.</para>
        /// </summary>
        public override void OnBeforeTakeDamage(Entity target, GameEvent e)
        {
            if (target == null || e == null) return;
            var damage = e.GetParameter<Damage>("Damage");
            if (damage == null || damage.Amount <= 0) return;
            if (!LiquidRegistry.IsInitialized) return;
            var def = LiquidRegistry.Get(LiquidId);
            if (def == null) return;

            // Detect element by the alias-collapsing FLAG, not a literal
            // string: real spells/weapons tag "Electric"/"Heat"/"Ice"
            // (ArcBolt→Electric, Conflagration→Heat, IceSword→Ice), and
            // Damage.HasAttribute is a raw List.Contains. Mirror the
            // canonical detector CombatSystem.ApplyResistances:983-985
            // uses (IsHeatDamage/IsElectricDamage) so the coat layer and
            // the resistance layer agree on what "fire"/"electric" means.
            // ("Lightning"/"Fire" still collapse to these flags, so the
            // pre-fix LQ.5 suite stays green.)
            if (damage.IsElectricDamage())
            {
                // Divergence #6: yield to a present ElectrifiedEffect.
                if (target.GetEffect<ElectrifiedEffect>() != null) return;
                if (def.Conductivity >= CONDUCTIVITY_AMPLIFY_THRESHOLD)
                    damage.Amount = (int)System.Math.Round(
                        damage.Amount * (1.0 + def.Conductivity / 100.0));
                return; // a strike is one element; skip the Fire branch
            }

            if (damage.IsHeatDamage())
            {
                if (def.FireDampen > 0)
                    damage.Amount = (int)System.Math.Round(
                        damage.Amount * (1.0 - def.FireDampen / 100.0));
                if (def.Combustibility >= COMBUSTIBLE_AMPLIFY_THRESHOLD)
                    damage.Amount = (int)System.Math.Round(
                        damage.Amount * (1.0 + def.Combustibility / 200.0));
            }
        }

        /// <summary>
        /// LQ.6: push the coat's <see cref="LiquidDefinition.StatModifiers"/>
        /// + <see cref="LiquidDefinition.ResistanceModifiers"/> onto the
        /// wearer's stats using the symmetric <c>Stat.Bonus</c> pattern
        /// (mirrors <c>EquipBonusUtility.ApplyEquipBonuses</c>), recording
        /// EXACTLY what landed into <see cref="AppliedModsRaw"/> so
        /// <see cref="ReverseStatModifiers"/> nets it to zero on removal.
        /// Idempotent: a non-empty <see cref="AppliedModsRaw"/> means the
        /// deltas are already applied (re-coat / post-load), so this is a
        /// no-op — guarantees no double-apply.
        /// </summary>
        private void ApplyStatModifiers(Entity target)
        {
            if (target == null) return;
            if (!string.IsNullOrEmpty(AppliedModsRaw)) return; // already applied
            if (!LiquidRegistry.IsInitialized) return;
            var def = LiquidRegistry.Get(LiquidId);
            if (def == null) return;

            var applied = new StringBuilder();
            AccumulateMods(target, def.StatModifiers, applied);
            AccumulateMods(target, def.ResistanceModifiers, applied);
            AppliedModsRaw = applied.ToString();

            if (AppliedModsRaw.Length > 0)
                Diag.Record("liquid", "StatModApplied", target, null,
                    new { liquidId = LiquidId, mods = AppliedModsRaw });
        }

        private static void AccumulateMods(
            Entity target, List<LiquidStatMod> mods, StringBuilder applied)
        {
            if (mods == null) return;
            for (int i = 0; i < mods.Count; i++)
            {
                var m = mods[i];
                if (m == null || string.IsNullOrEmpty(m.Stat) || m.Delta == 0)
                    continue;
                var stat = target.GetStat(m.Stat);
                if (stat == null) continue; // stat absent on this entity — skip
                stat.Bonus += m.Delta;
                if (applied.Length > 0) applied.Append(',');
                applied.Append(m.Stat).Append(':').Append(m.Delta);
            }
        }

        /// <summary>
        /// LQ.6: undo exactly the deltas recorded in
        /// <see cref="AppliedModsRaw"/> (NOT re-derived from the def —
        /// exact even after an id swap or registry reset) and clear the
        /// record. Symmetric with <see cref="ApplyStatModifiers"/> so the
        /// wearer's stats net to zero when the coat ends (dry-down,
        /// cure, save→load→expire).
        /// </summary>
        private void ReverseStatModifiers(Entity target)
        {
            if (target == null || string.IsNullOrEmpty(AppliedModsRaw)) return;
            string[] pairs = AppliedModsRaw.Split(',');
            for (int i = 0; i < pairs.Length; i++)
            {
                string p = pairs[i];
                int colon = p.IndexOf(':');
                if (colon < 0) continue;
                string name = p.Substring(0, colon);
                if (!int.TryParse(p.Substring(colon + 1), out int delta)) continue;
                var stat = target.GetStat(name);
                if (stat != null) stat.Bonus -= delta;
            }
            string undone = AppliedModsRaw;
            AppliedModsRaw = "";
            Diag.Record("liquid", "StatModRemoved", target, null,
                new { liquidId = LiquidId, mods = undone });
        }

        /// <summary>
        /// Divergence #3: a water coat ALSO ensures a
        /// <see cref="WetEffect"/> (full moisture) so the existing
        /// wet→electric amplification in <see cref="ElectrifiedEffect"/>
        /// keeps working. Non-water liquids never apply WetEffect.
        /// Null-safe: a stack-merge on an orphaned effect (no Owner) is
        /// a no-op rather than a crash.
        /// </summary>
        /// <summary>
        /// LB.4: if the liquid def declares a <c>LightRadius &gt; 0</c>
        /// (lantern-beetle ichor) and the wearer doesn't already have a
        /// <see cref="LightSourcePart"/>, attach a fresh one. Sets
        /// <see cref="AddedLightSource"/> so <see cref="RemoveLightSource"/>
        /// only strips a LightSourcePart we added (don't pickpocket a
        /// player's held lantern). Idempotent: re-coat with non-empty
        /// AppliedModsRaw doesn't re-attach (OnApply guard upstream;
        /// here we additionally guard with the flag).
        /// </summary>
        private void ApplyLightSource(Entity target)
        {
            if (target == null) return;
            if (AddedLightSource) return;
            if (!LiquidRegistry.IsInitialized) return;
            var def = LiquidRegistry.Get(LiquidId);
            if (def == null || def.LightRadius <= 0) return;
            if (target.GetPart<LightSourcePart>() != null) return; // respect held lantern
            target.AddPart(new LightSourcePart
            {
                Radius = def.LightRadius,
                LightColor = string.IsNullOrEmpty(def.LightColor) ? "&Y" : def.LightColor,
            });
            AddedLightSource = true;
            Diag.Record("liquid", "LightApplied", target, null,
                new { liquidId = LiquidId, radius = def.LightRadius,
                      color = def.LightColor });
        }

        /// <summary>
        /// LB.4: paired terminal of <see cref="ApplyLightSource"/>.
        /// Only strips the LightSourcePart if we added it (AddedLightSource
        /// flag). Round-tripped via save reflection (the flag is public).
        /// </summary>
        private void RemoveLightSource(Entity target)
        {
            if (target == null || !AddedLightSource) return;
            var light = target.GetPart<LightSourcePart>();
            if (light != null)
            {
                target.RemovePart(light);
                Diag.Record("liquid", "LightRemoved", target, null,
                    new { liquidId = LiquidId });
            }
            AddedLightSource = false;
        }

        private void RefreshWaterCoupling(Entity target)
        {
            if (target == null) return;
            if (LiquidId != "water") return;
            target.ApplyEffect(new WetEffect(1.0f), null, null);
        }
    }
}
