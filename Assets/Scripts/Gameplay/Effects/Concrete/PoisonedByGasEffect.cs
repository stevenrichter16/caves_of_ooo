using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.5 — lingering poison effect applied by <see cref="GasPoisonPart"/>.
    /// Direct port of Qud <c>XRL.World.Effects.PoisonGasPoison</c>
    /// (PoisonGasPoison.cs:73-85).
    ///
    /// <para><b>Tick gating (Qud parity).</b> The effect deals damage
    /// each turn ONLY when the target is NOT currently in a cell with
    /// a matching <see cref="GasPoisonPart"/>. The gas itself ticks
    /// damage on creatures standing in its cell; this effect carries
    /// poison forward when the target LEAVES the cloud. While IN the
    /// cloud, the gas's own per-turn dose covers it.</para>
    ///
    /// <para>Distinct from the existing <see cref="PoisonedEffect"/>
    /// (tonic-applied / weapon-applied poison) so a future cure-tonic
    /// can target gas-poisoning specifically without confusion.</para>
    /// </summary>
    public class PoisonedByGasEffect : Effect
    {
        public override string DisplayName => "gas-poisoned";

        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        /// <summary>Damage dealt per tick when NOT in a matching gas cell.</summary>
        public int DamagePerTurn = 2;

        /// <summary>The <see cref="GasPoolPart.GasType"/> key of the gas
        /// that applied this effect. Used in <see cref="OnTurnStart"/>
        /// to check "am I still in this kind of gas" — the tick is
        /// suppressed when in a matching cloud (the cloud's own per-turn
        /// dose covers damage there).</summary>
        public string GasTypeKey = "Poison";

        /// <summary>The gas's <see cref="GasPoolPart.Creator"/>, for
        /// damage-credit attribution when the lingering tick kills.</summary>
        public Entity Owner;

        public override void OnApply(Entity target)
        {
            if (target != null)
                MessageLog.Add(target.GetDisplayName() + " is poisoned by the gas.");
        }

        public override void OnRemove(Entity target)
        {
            if (target != null)
                MessageLog.Add(target.GetDisplayName() + " recovers from the gas-poisoning.");
        }

        public override void OnTurnStart(Entity target, GameEvent context)
        {
            if (target == null) return;
            if (DamagePerTurn <= 0) return;
            // Skip tick if target is currently in a matching gas cloud
            // — the gas itself doses them per turn; this effect is the
            // "carry-after-exit" tick.
            var zone = context?.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone;
            if (zone != null && IsInMatchingGasCell(target, zone))
            {
                Diag.Record("gas", "PoisonTickSkipped", Owner, target,
                    new { reason = "InMatchingGasCell", gasTypeKey = GasTypeKey });
                return;
            }

            var dmg = new Damage(DamagePerTurn);
            dmg.AddAttribute("Poison");
            CombatSystem.ApplyDamage(target, dmg, Owner, zone);
            Diag.Record("gas", "PoisonTick", Owner, target,
                new { damage = DamagePerTurn, gasTypeKey = GasTypeKey });
        }

        public override bool OnStack(Effect incoming)
        {
            // Refresh-on-reapply (the GasPoisonPart already removes the
            // existing effect before applying a fresh one, so this is
            // only hit in odd code paths). Mirror the LiquidCoveredEffect
            // semantics: take the larger Duration + larger DamagePerTurn.
            if (incoming is PoisonedByGasEffect other)
            {
                if (other.Duration > Duration) Duration = other.Duration;
                if (other.DamagePerTurn > DamagePerTurn) DamagePerTurn = other.DamagePerTurn;
                if (Owner == null) Owner = other.Owner;
                return true;
            }
            return false;
        }

        private bool IsInMatchingGasCell(Entity target, Zone zone)
        {
            var pos = zone.GetEntityPosition(target);
            if (pos.x < 0) return false;
            var cell = zone.GetCell(pos.x, pos.y);
            if (cell == null) return false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var pool = cell.Objects[i].GetPart<GasPoolPart>();
                if (pool != null && pool.GasType == GasTypeKey)
                    return true;
            }
            return false;
        }
    }
}
