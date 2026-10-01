namespace CavesOfOoo.Core
{
    /// <summary>
    /// Tracks continuous temperature for an entity.
    /// Handles heat application (radiant and direct modes from Qud)
    /// and triggers ignition when temperature crosses FlameTemperature.
    /// </summary>
    public class ThermalPart : Part
    {
        public override string Name => "Thermal";

        // Blueprint-configurable fields
        public float Temperature = 25f;
        public float FlameTemperature = 400f;
        public float VaporTemperature = 10000f;
        public float FreezeTemperature = 0f;
        public float BrittleTemperature = -100f;
        public float HeatCapacity = 1.0f;
        public float AmbientDecayRate = 0.02f;
        public float AmbientTemperature = 25f;

        public bool IsAflame => Temperature >= FlameTemperature;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "ApplyHeat")
                return HandleApplyHeat(e);

            if (e.ID == "EndTurn")
                return HandleEndTurn(e);

            return true;
        }

        private bool HandleApplyHeat(GameEvent e)
        {
            float joules = e.GetParameter<float>("Joules");
            bool radiant = e.GetParameter<bool>("Radiant");

            if (joules == 0f)
                return true;

            // Only opted-in workspots have a temperature-selected finite model.
            var cookingOwner = ParentEntity;
            var cookingStation = cookingOwner?.GetPart<CampfirePart>();
            var cookingZone = cookingStation?.FiniteCooking == true ? e.GetParameter<Zone>("Zone") : null;
            var cookingCell = CurrentFiniteCookingCell(cookingOwner, cookingStation, cookingZone);

            // Snapshot pre-change thresholds so we can detect crossings in both directions.
            float previous = Temperature;
            float effectiveFlame = GetEffectiveFlameTemperature();
            bool wasBelowFlame = previous < effectiveFlame;
            bool wasAboveFreeze = previous > FreezeTemperature;
            bool wasBelowVapor = previous < VaporTemperature;

            float delta;
            if (radiant)
            {
                // Qud-style radiant: asymptotic approach
                float factor = 0.035f / (HeatCapacity > 0f ? HeatCapacity : 1f);
                delta = (joules - Temperature) * factor;
            }
            else
            {
                // Direct heat: linear addition scaled by capacity
                delta = joules / (HeatCapacity > 0f ? HeatCapacity : 1f);
            }

            Temperature += delta;

            // Fire thaws (Docs/FREEZE-THAW.md): a heat dose melts ice in
            // proportion to the temperature it raises. Runs before the
            // crossing checks so a dose that thaws and then ignites reads
            // in that order. Cold doses (delta <= 0) never thaw.
            if (delta > 0f && ParentEntity != null)
                ParentEntity.GetEffect<FrozenEffect>()?.Thaw(delta * FrozenEffect.THAW_PER_DEGREE_OF_HEAT, "heat");

            // Check for ignition threshold crossing (volatility lowers the bar).
            if (wasBelowFlame && Temperature >= effectiveFlame)
                TryIgnite(e);

            // Check for extinguish: cooling dropped temperature below FlameTemperature
            if (!wasBelowFlame && Temperature < effectiveFlame)
                TryExtinguish();

            // Cold crossing: warm → at/below freezing. FrozenEffect.OnApply runs its own
            // shatter path against BrittleTemperature, so we skip the thermal-shock shatter
            // below when a freeze also fired to avoid double-damaging mid-brittleness targets.
            bool frozeThisTick = false;
            if (wasAboveFreeze && Temperature <= FreezeTemperature)
            {
                TryFreeze(e);
                frozeThisTick = true;
            }

            // Vapor crossing: sub-vapor → at/above VaporTemperature.
            if (wasBelowVapor && Temperature >= VaporTemperature)
                TryVaporize(e);

            // Thermal shock: brittle materials crack under a large single-tick delta,
            // but only if a freeze crossing didn't already drive a shatter this tick.
            if (!frozeThisTick && System.Math.Abs(delta) > 200f)
                TryShatter(e, "ThermalShock");

            InvalidateFiniteCookingBoundary(previous, cookingOwner, cookingStation, cookingZone, cookingCell);
            return true;
        }

        /// <summary>
        /// Returns the ignition threshold adjusted by MaterialPart.Volatility.
        /// Oil, alcohol, and other volatile materials ignite at a lower temperature
        /// and start with a hotter BurningEffect.
        /// </summary>
        private float GetEffectiveFlameTemperature()
        {
            if (ParentEntity == null)
                return FlameTemperature;
            var material = ParentEntity.GetPart<MaterialPart>();
            if (material == null || material.Volatility <= 0f)
                return FlameTemperature;
            return FlameTemperature - (material.Volatility * 100f);
        }

        private void TryIgnite(GameEvent sourceEvent)
        {
            if (ParentEntity == null)
                return;

            // Check WetEffect suppression
            var wet = ParentEntity.GetEffect<WetEffect>();
            if (wet != null && wet.Moisture > 0.35f)
            {
                // Wet suppresses ignition; evaporate some moisture instead
                wet.Moisture -= 0.1f;
                return;
            }

            // Fire TryIgnite event — MaterialPart can veto if Combustibility == 0
            var tryIgnite = GameEvent.New("TryIgnite");
            tryIgnite.SetParameter("Source", sourceEvent.GetParameter("Source"));
            bool allowed = ParentEntity.FireEvent(tryIgnite);
            bool cancelled = tryIgnite.GetParameter<bool>("Cancelled");
            tryIgnite.Release();

            if (!allowed || cancelled)
                return;

            // Apply BurningEffect if not already burning. Volatile materials start hotter.
            if (!ParentEntity.HasEffect<BurningEffect>())
            {
                Entity source = sourceEvent.GetParameter<Entity>("Source");
                var zone = sourceEvent.GetParameter<Zone>("Zone");
                float startIntensity = 1.0f;
                var material = ParentEntity.GetPart<MaterialPart>();
                if (material != null && material.Volatility > 0f)
                    startIntensity += material.Volatility;
                ParentEntity.ApplyEffect(new BurningEffect(intensity: startIntensity, source: source), source, zone);

                // W4.2 — fire is a crime in a grove. Charged HERE, at
                // the one seam every ignition passes through; GroveLaw
                // self-gates on player source + Grovelands surface, so
                // this is a no-op everywhere else and for fire spread
                // (whose Source is the propagating entity).
                GroveLaw.OnIgnite(source, ParentEntity, zone);
            }
        }

        private void TryFreeze(GameEvent sourceEvent)
        {
            if (ParentEntity == null)
                return;

            // Allow other parts to veto the freeze.
            var tryFreeze = GameEvent.New("TryFreeze");
            tryFreeze.SetParameter("Source", sourceEvent.GetParameter("Source"));
            bool allowed = ParentEntity.FireEvent(tryFreeze);
            bool cancelled = tryFreeze.GetParameter<bool>("Cancelled");
            tryFreeze.Release();

            if (!allowed || cancelled)
                return;

            if (!ParentEntity.HasEffect<FrozenEffect>())
            {
                Entity source = sourceEvent.GetParameter<Entity>("Source");
                var zone = sourceEvent.GetParameter<Zone>("Zone");
                // Through the matrix, not past it: creatures pass through
                // unchanged, but scenery is gated on the Freezable row —
                // the cooling path used to freeze stone walls the matrix
                // calls WrongMaterial, the freeze-side twin of the ignite
                // side's Combustibility veto (study §4 violator #3).
                // Refusals emit effect/ObjectEffectRefused.
                //
                // Magnitude follows how far the dose drove the body below
                // freezing (FrozenEffect.ColdForDepth), not a flat 1.0: a
                // Quench-sized chill is a short freeze, a deep chill a long one.
                ObjectStatusMatrix.TryApply(
                    new FrozenEffect(cold: FrozenEffect.ColdForDepth(FreezeTemperature - Temperature)),
                    ParentEntity, source, zone);
            }
        }

        private void TryVaporize(GameEvent sourceEvent)
        {
            if (ParentEntity == null)
                return;

            // Let parts veto vaporization (e.g. inert high-boil materials).
            var tryVaporize = GameEvent.New("TryVaporize");
            tryVaporize.SetParameter("Source", sourceEvent.GetParameter("Source"));
            bool allowed = ParentEntity.FireEvent(tryVaporize);
            bool cancelled = tryVaporize.GetParameter<bool>("Cancelled");
            tryVaporize.Release();

            if (!allowed || cancelled)
                return;

            // Strip moisture — the water has boiled off.
            if (ParentEntity.HasEffect<WetEffect>())
                ParentEntity.RemoveEffect<WetEffect>();

            // Mark the entity for steam handling upstream (material reactions pick this up).
            ParentEntity.FireEvent("Vaporized");
        }

        private void TryShatter(GameEvent sourceEvent, string cause)
        {
            if (ParentEntity == null)
                return;

            var tryShatter = GameEvent.New("TryShatter");
            tryShatter.SetParameter("Cause", cause);
            tryShatter.SetParameter("Source", sourceEvent.GetParameter("Source"));
            ParentEntity.FireEvent(tryShatter);
            tryShatter.Release();
        }

        private void TryExtinguish()
        {
            if (ParentEntity == null)
                return;

            if (ParentEntity.HasEffect<BurningEffect>())
            {
                ParentEntity.RemoveEffect<BurningEffect>();
                ParentEntity.FireEvent("Extinguished");
            }
        }

        private bool HandleEndTurn(GameEvent e)
        {
            float previous = Temperature;
            // Only opted-in workspots have a temperature-selected finite model.
            var cookingOwner = ParentEntity;
            var cookingStation = cookingOwner?.GetPart<CampfirePart>();
            var cookingZone = cookingStation?.FiniteCooking == true ? e.GetParameter<Zone>("Zone") : null;
            var cookingCell = CurrentFiniteCookingCell(cookingOwner, cookingStation, cookingZone);

            // Decay toward ambient temperature
            if (Temperature != AmbientTemperature)
            {
                float diff = Temperature - AmbientTemperature;
                Temperature -= diff * AmbientDecayRate;

                // Snap to ambient if close enough
                if (System.Math.Abs(Temperature - AmbientTemperature) < 0.5f)
                    Temperature = AmbientTemperature;
            }

            // Check if fire has gone out — remove the BurningEffect directly
            if (ParentEntity != null
                && Temperature < GetEffectiveFlameTemperature()
                && ParentEntity.HasEffect<BurningEffect>())
            {
                ParentEntity.RemoveEffect<BurningEffect>();
                ParentEntity.FireEvent("Extinguished");
            }

            InvalidateFiniteCookingBoundary(previous, cookingOwner, cookingStation, cookingZone, cookingCell);
            return true;
        }

        private Cell CurrentFiniteCookingCell(Entity owner, CampfirePart station, Zone zone)
        {
            if (zone == null || owner == null || station == null || !station.FiniteCooking
                || !ReferenceEquals(ParentEntity, owner) || !ReferenceEquals(owner.GetPart<ThermalPart>(), this)
                || !ReferenceEquals(station.ParentEntity, owner) || !ReferenceEquals(owner.GetPart<CampfirePart>(), station)
                || !ReferenceEquals(owner.SpatialZone, zone))
                return null;
            var physics = owner.GetPart<PhysicsPart>();
            if (physics == null || !ReferenceEquals(physics.ParentEntity, owner)
                || physics.InInventory != null || physics.Equipped != null)
                return null;
            var cell = zone.GetEntityCell(owner);
            return cell != null && ReferenceEquals(cell.ParentZone, zone)
                && ReferenceEquals(zone.GetCell(cell.X, cell.Y), cell) && cell.Objects.Contains(owner) ? cell : null;
        }

        private void InvalidateFiniteCookingBoundary(float previous, Entity owner, CampfirePart station, Zone zone, Cell cell)
        {
            // The current cell is redrawn once only when finite thermal appearance
            // changes. Fuel availability is a separate cooking/readout contract.
            if (cell == null || float.IsNaN(previous) || float.IsInfinity(previous)
                || float.IsNaN(Temperature) || float.IsInfinity(Temperature)
                || (previous >= CookingService.MinimumFiniteCookingTemperature)
                    == (Temperature >= CookingService.MinimumFiniteCookingTemperature)
                || !ReferenceEquals(cell, CurrentFiniteCookingCell(owner, station, zone)))
                return;
            ZoneRenderHooks.MarkCellDirty(cell, "FiniteCookingTemperature");
        }
    }
}
