using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Static resolver that loads material reaction blueprints from JSON
    /// and evaluates them against entity state during simulation.
    /// Called from BurningEffect.OnTurnStart and other effect hooks.
    /// </summary>
    public static class MaterialReactionResolver
    {
        private static List<MaterialReactionBlueprint> _reactions = new List<MaterialReactionBlueprint>();
        private static bool _initialized;

        /// <summary>
        /// Optional entity factory used by SpawnEntity / SwapBlueprint reaction
        /// effect types. Set by GameBootstrap at initialization. Tests can leave
        /// it null — those effect types then no-op gracefully.
        /// </summary>
        public static Data.EntityFactory Factory;

        public static void Initialize(string json)
        {
            _reactions.Clear();
            AppendJson(json);
            _reactions.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            _initialized = true;
        }

        /// <summary>
        /// Load reactions from multiple JSON documents, merging them into a single
        /// priority-sorted list. Used by GameBootstrap when it scans the
        /// MaterialReactions folder for all reaction files at startup.
        /// </summary>
        public static void InitializeFromJsonSources(System.Collections.Generic.IEnumerable<string> jsonSources)
        {
            _reactions.Clear();
            if (jsonSources != null)
            {
                foreach (var json in jsonSources)
                    AppendJson(json);
            }
            _reactions.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            _initialized = true;
        }

        private static void AppendJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            var collection = JsonUtility.FromJson<MaterialReactionCollection>(json);
            if (collection?.Reactions != null)
                _reactions.AddRange(collection.Reactions);
        }

        public static bool IsInitialized => _initialized;
        public static int ReactionCount => _reactions.Count;

        /// <summary>
        /// Evaluate all matching reactions for an entity. When called from
        /// BurningEffect.OnTurnStart, the burning parameter carries the active
        /// effect so ModifyBurnIntensity / ModifyFuelConsumption can mutate it.
        /// When called from other contexts (ElectrifiedEffect, AcidicEffect,
        /// external systems), burning may be null and those effect types become
        /// no-ops while other types still fire.
        /// </summary>
        public static void EvaluateReactions(Entity entity, Zone zone, BurningEffect burning = null)
        {
            if (!_initialized || entity == null)
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

            // Check source state — recognised values map to per-entity effect queries.
            if (!string.IsNullOrEmpty(cond.SourceState))
            {
                if (!EntityHasSourceState(entity, cond.SourceState, burning))
                    return false;
            }

            // Check target material tag
            if (!string.IsNullOrEmpty(cond.TargetMaterialTag))
            {
                if (material == null || !material.HasMaterialTag(cond.TargetMaterialTag))
                    return false;
            }

            // Check temperature thresholds
            if (cond.MinTemperature > 0f)
            {
                if (thermal == null || thermal.Temperature < cond.MinTemperature)
                    return false;
            }
            if (cond.MaxTemperature < float.MaxValue)
            {
                if (thermal == null || thermal.Temperature > cond.MaxTemperature)
                    return false;
            }

            // Check moisture cap
            if (cond.MaxMoisture < 1.0f)
            {
                var wet = entity.GetEffect<WetEffect>();
                if (wet != null && wet.Moisture > cond.MaxMoisture)
                    return false;
            }

            // Check moisture floor — used by water_plus_fire to require a wet burning entity.
            if (cond.MinMoisture > 0f)
            {
                var wet = entity.GetEffect<WetEffect>();
                if (wet == null || wet.Moisture < cond.MinMoisture)
                    return false;
            }

            // Check material property thresholds
            if (cond.MinBrittleness > 0f)
            {
                if (material == null || material.Brittleness < cond.MinBrittleness)
                    return false;
            }
            if (cond.MinConductivity > 0f)
            {
                if (material == null || material.Conductivity < cond.MinConductivity)
                    return false;
            }
            if (cond.MinVolatility > 0f)
            {
                if (material == null || material.Volatility < cond.MinVolatility)
                    return false;
            }

            return true;
        }

        private static bool EntityHasSourceState(Entity entity, string state, BurningEffect burning)
        {
            switch (state)
            {
                case "Burning":
                    // Prefer the passed-in effect (authoritative when called from
                    // BurningEffect.OnTurnStart); fall back to a lookup.
                    return burning != null || entity.HasEffect<BurningEffect>();
                case "Wet":
                    return entity.HasEffect<WetEffect>();
                case "Frozen":
                    return entity.HasEffect<FrozenEffect>();
                case "Electrified":
                    return entity.HasEffect<ElectrifiedEffect>();
                case "Acidic":
                    return entity.HasEffect<AcidicEffect>();
                default:
                    return false;
            }
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
                        if (burning != null)
                            burning.Intensity = System.Math.Min(
                                burning.Intensity + fx.FloatValue, 5.0f);
                        break;

                    case "ModifyFuelConsumption":
                        if (burning != null)
                        {
                            // Apply as bonus consumption this tick, not a permanent BurnRate change
                            var fuel = entity.GetPart<FuelPart>();
                            if (fuel != null)
                            {
                                float bonusBurn = fuel.BurnRate * (fx.FloatValue - 1.0f) * burning.Intensity;
                                fuel.FuelMass -= bonusBurn;
                                if (fuel.FuelMass < 0f) fuel.FuelMass = 0f;
                            }
                        }
                        break;

                    case "EmitBonusHeat":
                        if (zone != null)
                            MaterialSimSystem.EmitHeatToAdjacent(entity, zone, fx.FloatValue);
                        break;

                    case "ApplyStatusEffect":
                        ApplyStatusEffectByName(entity, zone, fx.StringValue, fx.FloatValue);
                        break;

                    case "PropagateAlongTag":
                        PropagateAlongTag(entity, zone, fx.StringValue, fx.FloatValue);
                        break;

                    case "DealDamage":
                        if (entity.GetStatValue("Hitpoints", 0) > 0)
                            CombatSystem.ApplyDamage(entity, (int)fx.FloatValue, null, zone);
                        break;

                    case "SpawnEntity":
                        SpawnEntityInZone(entity, zone, fx.StringValue);
                        break;

                    case "SwapBlueprint":
                        SwapBlueprint(entity, zone, fx.StringValue);
                        break;

                    case "SpawnParticle":
                        // Future: spawn visual particle effect
                        break;
                }
            }
        }

        // ── Effect helpers ─────────────────────────────────────────────────

        private static void ApplyStatusEffectByName(Entity entity, Zone zone, string effectName, float value)
        {
            if (string.IsNullOrEmpty(effectName))
                return;

            Type effectType = FindEffectType(effectName);
            if (effectType == null)
            {
                Debug.LogWarning($"MaterialReactionResolver: unknown effect '{effectName}'");
                return;
            }

            Effect instance = InstantiateEffect(effectType, value);
            if (instance != null)
                entity.ApplyEffect(instance, null, zone);
        }

        private static Type FindEffectType(string name)
        {
            // Try fully qualified first, then short name within this assembly's Effects namespace.
            Type t = Type.GetType(name);
            if (t != null) return t;
            t = Type.GetType("CavesOfOoo.Core." + name);
            if (t != null) return t;
            return null;
        }

        private static Effect InstantiateEffect(Type type, float value)
        {
            // Try a (float) constructor first — matches our Frozen/Electrified/Acidic/Wet pattern.
            var floatCtor = type.GetConstructor(new[] { typeof(float) });
            if (floatCtor != null)
                return (Effect)floatCtor.Invoke(new object[] { value });

            // Fall back to parameterless.
            var empty = type.GetConstructor(Type.EmptyTypes);
            if (empty != null)
                return (Effect)empty.Invoke(null);

            return null;
        }

        private static void PropagateAlongTag(Entity source, Zone zone, string tag, float joules)
        {
            // Negative joules are allowed so future cold reactions can chill neighbors
            // along a tag (e.g., frost propagating down an ice rod).
            if (zone == null || string.IsNullOrEmpty(tag) || joules == 0f)
                return;

            var sourceCell = zone.GetEntityCell(source);
            if (sourceCell == null)
                return;

            for (int dir = 0; dir < 8; dir++)
            {
                var cell = zone.GetCellInDirection(sourceCell.X, sourceCell.Y, dir);
                if (cell == null)
                    continue;

                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var target = cell.Objects[i];
                    if (target == source)
                        continue;

                    var mat = target.GetPart<MaterialPart>();
                    if (mat == null || !mat.HasMaterialTag(tag))
                        continue;

                    var heat = GameEvent.New("ApplyHeat");
                    heat.SetParameter("Joules", (object)joules);
                    heat.SetParameter("Radiant", (object)true);
                    heat.SetParameter("Source", (object)source);
                    heat.SetParameter("Zone", (object)zone);
                    target.FireEvent(heat);
                    heat.Release();
                }
            }
        }

        private static void SpawnEntityInZone(Entity source, Zone zone, string blueprintName)
        {
            if (Factory == null || zone == null || string.IsNullOrEmpty(blueprintName))
                return;

            var sourceCell = zone.GetEntityCell(source);
            if (sourceCell == null)
                return;

            Entity spawned = Factory.CreateEntity(blueprintName);
            if (spawned == null)
                return;

            zone.AddEntity(spawned, sourceCell.X, sourceCell.Y);
        }

        private static void SwapBlueprint(Entity source, Zone zone, string blueprintName)
        {
            if (Factory == null || zone == null || string.IsNullOrEmpty(blueprintName))
                return;

            var sourceCell = zone.GetEntityCell(source);
            if (sourceCell == null)
                return;

            Entity replacement = Factory.CreateEntity(blueprintName);
            if (replacement == null)
                return;

            int x = sourceCell.X;
            int y = sourceCell.Y;
            zone.RemoveEntity(source);
            zone.AddEntity(replacement, x, y);
        }
    }
}
