using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Data
{
    /// <summary>
    /// Creates Entity instances from Blueprints.
    /// Mirrors Qud's GameObjectFactory: resolves part types by name, sets parameters,
    /// copies stats/tags/properties from the blueprint onto the new entity.
    /// </summary>
    public class EntityFactory
    {
        /// <summary>
        /// All loaded blueprints, keyed by name.
        /// </summary>
        public Dictionary<string, Blueprint> Blueprints = new Dictionary<string, Blueprint>();

        /// <summary>
        /// Registry of known Part types by name.
        /// Parts must be registered here before they can be instantiated from blueprints.
        /// </summary>
        private Dictionary<string, Type> _partTypes = new Dictionary<string, Type>();

        private int _nextEntityID = 1;

        public EntityFactory()
        {
            // Auto-register all Part subclasses in the current assembly
            RegisterPartsFromAssembly(typeof(Part).Assembly);
        }

        /// <summary>
        /// Scan an assembly for all concrete Part subclasses and register them.
        /// </summary>
        public void RegisterPartsFromAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAbstract && typeof(Part).IsAssignableFrom(type))
                {
                    // Register by both class name and Part.Name
                    _partTypes[type.Name] = type;
                }
            }
        }

        /// <summary>
        /// Register a specific part type by name.
        /// </summary>
        public void RegisterPartType<T>(string name = null) where T : Part
        {
            _partTypes[name ?? typeof(T).Name] = typeof(T);
        }

        /// <summary>
        /// Load blueprints from a JSON string.
        /// </summary>
        public void LoadBlueprints(string json)
        {
            var loaded = BlueprintLoader.LoadFromJson(json);
            foreach (var kvp in loaded)
            {
                Blueprints[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Create an entity from a blueprint name.
        /// </summary>
        public Entity CreateEntity(string blueprintName)
        {
            if (!Blueprints.TryGetValue(blueprintName, out Blueprint bp))
            {
                Debug.LogError($"EntityFactory: unknown blueprint '{blueprintName}'");
                return null;
            }
            return CreateEntity(bp);
        }

        /// <summary>
        /// Create an entity from a blueprint.
        /// </summary>
        public Entity CreateEntity(Blueprint blueprint)
        {
            var entity = new Entity();
            entity.ID = (_nextEntityID++).ToString();
            entity.BlueprintName = blueprint.Name;

            // Copy properties
            foreach (var kvp in blueprint.Props)
                entity.Properties[kvp.Key] = kvp.Value;
            foreach (var kvp in blueprint.IntProps)
                entity.IntProperties[kvp.Key] = kvp.Value;

            // Copy tags
            foreach (var kvp in blueprint.Tags)
                entity.Tags[kvp.Key] = kvp.Value;

            // Create stats
            foreach (var kvp in blueprint.Stats)
            {
                var stat = new Stat
                {
                    Owner = entity,
                    Name = kvp.Value.Name,
                    BaseValue = kvp.Value.Value,
                    Min = kvp.Value.Min,
                    Max = kvp.Value.Max,
                    Boost = kvp.Value.Boost,
                    sValue = kvp.Value.sValue
                };
                entity.Statistics[kvp.Key] = stat;
            }

            // Create and attach parts
            foreach (var kvp in blueprint.Parts)
            {
                string partTypeName = kvp.Key;
                var parameters = kvp.Value;

                Part part = CreatePart(partTypeName);
                if (part == null)
                {
                    Debug.LogWarning($"EntityFactory: unknown part type '{partTypeName}' on blueprint '{blueprint.Name}'");
                    continue;
                }

                // Set parameters on the part via reflection
                ApplyParameters(part, parameters);

                entity.AddPart(part);
            }

            // Fire creation event
            entity.FireEvent(GameEvent.New("ObjectCreated"));

            return entity;
        }

        private Part CreatePart(string typeName)
        {
            // Try direct name match
            if (_partTypes.TryGetValue(typeName, out Type type))
                return (Part)Activator.CreateInstance(type);

            // Try with "Part" suffix
            if (_partTypes.TryGetValue(typeName + "Part", out type))
                return (Part)Activator.CreateInstance(type);

            // Try matching Part.Name override
            foreach (var kvp in _partTypes)
            {
                var instance = (Part)Activator.CreateInstance(kvp.Value);
                if (instance.Name == typeName)
                    return instance;
            }

            return null;
        }

        /// <summary>
        /// Set public fields/properties on a Part from a string parameter dictionary.
        /// Handles string, int, float, and bool fields.
        /// </summary>
        private void ApplyParameters(Part part, Dictionary<string, string> parameters)
        {
            var type = part.GetType();
            foreach (var kvp in parameters)
            {
                // Try field first
                var field = type.GetField(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    object value = ConvertValue(kvp.Value, field.FieldType);
                    if (value != null)
                        field.SetValue(part, value);
                    continue;
                }

                // Try property
                var prop = type.GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                {
                    object value = ConvertValue(kvp.Value, prop.PropertyType);
                    if (value != null)
                        prop.SetValue(part, value);
                }
            }
        }

        private object ConvertValue(string value, Type targetType)
        {
            if (targetType == typeof(string))
                return value;
            if (targetType == typeof(int))
                return int.TryParse(value, out int i) ? (object)i : null;
            if (targetType == typeof(float))
                return float.TryParse(value, out float f) ? (object)f : null;
            if (targetType == typeof(bool))
                return bool.TryParse(value, out bool b) ? (object)b : null;
            if (targetType == typeof(double))
                return double.TryParse(value, out double d) ? (object)d : null;
            return null;
        }

        /// <summary>
        /// Get a blueprint by name.
        /// </summary>
        public Blueprint GetBlueprint(string name)
        {
            Blueprints.TryGetValue(name, out Blueprint bp);
            return bp;
        }
    }
}
