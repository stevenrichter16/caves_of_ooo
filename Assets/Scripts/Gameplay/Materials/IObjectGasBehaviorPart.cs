using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.5 — abstract dispatch base for gas behaviors that target
    /// creatures/objects in the same cell. Direct mirror of Qud's
    /// <c>XRL.World.Parts.IObjectGasBehavior</c>
    /// (IObjectGasBehavior.cs:1-85), reduced to CoO's event vocabulary.
    ///
    /// <para><b>Two dispatch paths</b> (Qud parity):
    /// <list type="number">
    ///   <item><b>On entry</b> — when a creature steps into the gas's
    ///         cell, <c>EntityEnteredCell</c> fires on this entity
    ///         (mirror of Qud <c>ObjectEnteredCellEvent</c>). We call
    ///         <see cref="ApplyGas"/> on the entrant.</item>
    ///   <item><b>Per-turn pass</b> — <see cref="GasSystem.OnTickEnd"/>
    ///         iterates each gas pool after dispersal and calls
    ///         <see cref="ApplyToCell"/> to dose every object currently
    ///         in the cell (creatures standing still still get hit).</item>
    /// </list></para>
    ///
    /// <para><b>The filter pipeline</b> is implemented by each subclass
    /// in their override of <see cref="ApplyGas(Entity, Zone)"/>. Gates
    /// follow the Qud 7-layer pattern (GasPoison.cs:77-124) reduced to
    /// what CoO has today:
    /// <list type="number">
    ///   <item>entity != self (gas can't gas itself)</item>
    ///   <item>entity has "Creature" tag</item>
    ///   <item><c>CheckGasCanAffect</c> event veto
    ///         (G.6 <c>GasImmunityPart</c> listens)</item>
    ///   <item><c>GetRespiratoryPerformance</c> intake modifier
    ///         (G.6 <c>GasMaskPart</c> listens)</item>
    ///   <item>subclass-specific apply (effect + damage)</item>
    /// </list>
    /// </para>
    /// </summary>
    public abstract class IObjectGasBehaviorPart : IGasBehaviorPart
    {
        /// <summary>The gas's behavior dispatch on <c>EntityEnteredCell</c>.
        /// Mirrors Qud IObjectGasBehavior's
        /// <c>HandleEvent(ObjectEnteredCellEvent)</c>:34-41.</summary>
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "EntityEnteredCell") return true;
            var mover = e.GetParameter<Entity>("Actor");
            var zone = e.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone;
            if (mover != null)
                ApplyGas(mover, zone);
            return true;
        }

        /// <summary>Per-turn dispatch (called by <see cref="GasSystem"/>
        /// after dispersal). Iterates a snapshot of the cell's objects and
        /// calls <see cref="ApplyGas"/> on each. Mirrors Qud
        /// <c>ApplyGas(Cell)</c>:53-79.</summary>
        public void ApplyToCell(Cell cell, Zone zone)
        {
            if (cell == null) return;
            // Snapshot the object list: ApplyGas may apply effects that
            // re-enter this code path (e.g. burning gas + acid coat
            // interaction in a future phase), and we don't want
            // mid-iteration mutation to corrupt the loop.
            var snapshot = new System.Collections.Generic.List<Entity>(cell.Objects);
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i] == ParentEntity) continue; // gas can't gas itself
                ApplyGas(snapshot[i], zone);
            }
        }

        /// <summary>Per-creature apply hook. Subclasses implement the
        /// filter chain + specific effect application. Returns true if
        /// the gas actually affected the target (Qud parity: useful
        /// for "did anything happen this tick" diagnostics).</summary>
        public abstract bool ApplyGas(Entity target, Zone zone);

        // ──────────── Shared filter helpers (used by subclasses) ────────────

        /// <summary>Filter step: target must have the "Creature" tag.
        /// Emits a <c>gas/ApplyVetoed</c> diag with reason "NotACreature"
        /// on failure.</summary>
        protected bool CheckIsCreature(Entity target)
        {
            if (target == null || !target.Tags.ContainsKey("Creature"))
            {
                Diag.Record("gas", "ApplyVetoed", BaseGas?.Creator, target,
                    new { gasId = BaseGas?.GasId, gasType = BaseGas?.GasType,
                          reason = "NotACreature" });
                return false;
            }
            return true;
        }

        /// <summary>Filter step: fire the <c>CheckGasCanAffect</c> event
        /// on the target so per-type immunity Parts (G.6 GasImmunityPart)
        /// can veto. Listener returns false from HandleEvent → vetoed.
        /// Emits <c>gas/ApplyVetoed</c> with reason "GasImmunity" on
        /// rejection.</summary>
        protected bool CheckCanAffect(Entity target)
        {
            if (BaseGas == null) return true;
            var e = GameEvent.New("CheckGasCanAffect");
            e.SetParameter("GasEntity", (object)ParentEntity);
            e.SetParameter("GasType", (object)BaseGas.GasType);
            e.SetParameter("GasPool", (object)BaseGas);
            bool canAffect = target.FireEventAndRelease(e);
            if (!canAffect)
            {
                Diag.Record("gas", "ApplyVetoed", BaseGas.Creator, target,
                    new { gasId = BaseGas.GasId, gasType = BaseGas.GasType,
                          reason = "GasImmunity" });
                return false;
            }
            return true;
        }

        /// <summary>Filter step: compute respiratory intake. Fires the
        /// <c>GetRespiratoryPerformance</c> event with "BaseIntake" /
        /// "Intake" params; listeners (G.6 GasMaskPart) reduce "Intake".
        /// Returns the final Intake value (0..BaseIntake). Mirrors Qud
        /// <c>GetRespiratoryAgentPerformanceEvent</c>.</summary>
        protected int GetRespiratoryPerformance(Entity target, int baseIntake = 100)
        {
            if (BaseGas == null) return baseIntake;
            var e = GameEvent.New("GetRespiratoryPerformance");
            e.SetParameter("GasEntity", (object)ParentEntity);
            e.SetParameter("GasType", (object)BaseGas.GasType);
            e.SetParameter("BaseIntake", (object)baseIntake);
            e.SetParameter("Intake", (object)baseIntake);
            target.FireEvent(e);
            int intake = e.GetParameter<int>("Intake");
            e.Release();
            if (intake < 0) intake = 0;
            if (intake > baseIntake) intake = baseIntake;
            return intake;
        }

        /// <summary>G.8a refactor — run the full filter chain (null-safe
        /// → self-guard → Creature → CheckGasCanAffect → respiratory
        /// intake). Returns the computed Intake (0..baseIntake) on
        /// success, -1 if any gate vetoed (with the corresponding
        /// <c>gas/ApplyVetoed</c> diag already emitted by the failed
        /// gate). Subclasses call this then post-process (apply
        /// effect, deal immediate damage, etc.).
        ///
        /// <para>Extracted from <see cref="GasPoisonPart.ApplyGas"/>
        /// for reuse by G.8 GasStunPart/GasConfusionPart/etc. — keeps
        /// the filter chain in ONE place so a future gate change (e.g.
        /// the deferred Respires tag) only touches this method.</para>
        /// </summary>
        protected int RunFilterChain(Entity target, int baseIntake = 100)
        {
            if (BaseGas == null) return -1;
            if (target == null || target == ParentEntity) return -1;
            if (!CheckIsCreature(target)) return -1;
            if (!CheckCanAffect(target)) return -1;
            int intake = GetRespiratoryPerformance(target, baseIntake);
            if (intake <= 0)
            {
                Diag.Record("gas", "ApplyVetoed", BaseGas.Creator, target,
                    new { gasId = BaseGas.GasId, gasType = BaseGas.GasType,
                          reason = "ZeroIntake" });
                return -1;
            }
            return intake;
        }
    }
}
