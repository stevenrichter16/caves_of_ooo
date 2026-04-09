using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Static resolver that loads material reaction blueprints from JSON
    /// and evaluates them against entity state during simulation.
    /// Called from BurningEffect.OnTurnStart after fuel consumption.
    /// </summary>
    public static class MaterialReactionResolver
    {
        private static List<MaterialReactionBlueprint> _reactions = new List<MaterialReactionBlueprint>();
        private static bool _initialized;

        public static void Initialize(string json)
        {
            _reactions.Clear();
            if (string.IsNullOrEmpty(json))
            {
                _initialized = true;
                return;
            }

            var collection = JsonUtility.FromJson<MaterialReactionCollection>(json);
            if (collection?.Reactions != null)
            {
                _reactions.AddRange(collection.Reactions);
                _reactions.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
            _initialized = true;
        }

        public static bool IsInitialized => _initialized;
        public static int ReactionCount => _reactions.Count;

        /// <summary>
        /// Evaluate all matching reactions for a burning entity.
        /// Modifies the BurningEffect and entity state based on matched reaction effects.
        /// </summary>
        public static void EvaluateReactions(Entity entity, Zone zone, BurningEffect burning)
        {
            if (!_initialized || entity == null || burning == null)
                return;

            var material = entity.GetPart<MaterialPart>();
            var thermal = entity.GetPart<ThermalPart>();

            for (int i = 0; i < _reactions.Count; i++)
            {
                var reaction = _reactions[i];
                if (MatchesConditions(reaction.Conditions, entity, material, thermal, burning))
                    ApplyEffects(reaction.Effects, entity, zone, burning);
            }
        }

        private static bool MatchesConditions(
            ReactionConditions cond,
            Entity entity,
            MaterialPart material,
            ThermalPart thermal,
            BurningEffect burning)
        {
            if (cond == null)
                return false;

            // Check source state
            if (!string.IsNullOrEmpty(cond.SourceState))
            {
                if (cond.SourceState == "Burning" && burning == null)
                    return false;
            }

            // Check target material tag
            if (!string.IsNullOrEmpty(cond.TargetMaterialTag))
            {
                if (material == null || !material.HasMaterialTag(cond.TargetMaterialTag))
                    return false;
            }

            // Check temperature threshold
            if (cond.MinTemperature > 0f)
            {
                if (thermal == null || thermal.Temperature < cond.MinTemperature)
                    return false;
            }

            // Check moisture cap
            if (cond.MaxMoisture < 1.0f)
            {
                var wet = entity.GetEffect<WetEffect>();
                if (wet != null && wet.Moisture > cond.MaxMoisture)
                    return false;
            }

            return true;
        }

        private static void ApplyEffects(
            List<ReactionEffect> effects,
            Entity entity,
            Zone zone,
            BurningEffect burning)
        {
            if (effects == null)
                return;

            for (int i = 0; i < effects.Count; i++)
            {
                var fx = effects[i];
                switch (fx.Type)
                {
                    case "ModifyBurnIntensity":
                        burning.Intensity = System.Math.Min(
                            burning.Intensity + fx.FloatValue, 5.0f);
                        break;

                    case "ModifyFuelConsumption":
                        var fuel = entity.GetPart<FuelPart>();
                        if (fuel != null)
                            fuel.BurnRate *= fx.FloatValue;
                        break;

                    case "EmitBonusHeat":
                        if (zone != null)
                            MaterialSimSystem.EmitHeatToAdjacent(entity, zone, fx.FloatValue);
                        break;

                    case "SpawnParticle":
                        // Future: spawn visual particle effect
                        break;
                }
            }
        }
    }
}
