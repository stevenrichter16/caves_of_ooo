using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Rendering;
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
        private static readonly HashSet<string> AbstractRenderBlueprints = new HashSet<string>
        {
            "PhysicalObject",
            "Terrain",
            "Creature",
            "Item",
            "FoodItem",
            "TonicItem",
            "Container",
            "Corpse",
            "MeleeWeapon",
            "MissileWeapon",
            "Armor",
            "ArmorItem",
            "Shield",
            "Helmet",
            "Gloves",
            "Boots",
            "Cloak",
            "NaturalWeapon"
        };

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
        /// Validate baked blueprints against the world ASCII render contract.
        /// Issues are warnings for content authors; the runtime still falls back safely.
        /// </summary>
        public List<string> ValidateAsciiWorldBlueprints()
        {
            var issues = new List<string>();

            foreach (var blueprint in Blueprints.Values)
            {
                if (!blueprint.Parts.TryGetValue("Render", out var renderParameters))
                    continue;

                bool requireDisplayName = !AbstractRenderBlueprints.Contains(blueprint.Name);
                issues.AddRange(AsciiWorldRenderPolicy.ValidateBlueprintRenderParameters(
                    blueprint.Name,
                    renderParameters,
                    requireDisplayName));
            }

            return issues;
        }

        public List<string> ValidateAsciiWorldBlueprint(string blueprintName)
        {
            if (!Blueprints.TryGetValue(blueprintName, out var blueprint))
                return new List<string> { $"Unknown blueprint '{blueprintName}'" };

            if (!blueprint.Parts.TryGetValue("Render", out var renderParameters))
                return new List<string> { $"{blueprint.Name}: missing Render part" };

            bool requireDisplayName = !AbstractRenderBlueprints.Contains(blueprint.Name);
            return AsciiWorldRenderPolicy.ValidateBlueprintRenderParameters(
                blueprint.Name,
                renderParameters,
                requireDisplayName);
        }

        /// <summary>
        /// Validate item handling metadata such as GripType, carry/throw flags,
        /// and contradictions between Handling and explicit UsesSlots.
        /// </summary>
        public List<string> ValidateHandlingBlueprints()
        {
            var issues = new List<string>();

            foreach (var blueprint in Blueprints.Values)
                issues.AddRange(ValidateHandlingBlueprint(blueprint));

            return issues;
        }

        public List<string> ValidateHandlingBlueprint(string blueprintName)
        {
            if (!Blueprints.TryGetValue(blueprintName, out var blueprint))
                return new List<string> { $"Unknown blueprint '{blueprintName}'" };

            return ValidateHandlingBlueprint(blueprint);
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

            // Initialize body anatomy if Body part has an Anatomy parameter
            InitializeAnatomy(entity);

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
        /// Handles string, int, float, bool, double, and enum fields.
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
            if (targetType.IsEnum)
                return Enum.TryParse(targetType, value, true, out object enumValue) ? enumValue : null;
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

        private List<string> ValidateHandlingBlueprint(Blueprint blueprint)
        {
            var issues = new List<string>();
            if (blueprint == null || !blueprint.Parts.TryGetValue("Handling", out var handlingParameters))
                return issues;

            GripType gripType = GripType.OneHand;
            if (handlingParameters.TryGetValue("GripType", out string gripTypeRaw)
                && !Enum.TryParse(gripTypeRaw, true, out gripType))
            {
                issues.Add($"{blueprint.Name}: Handling.GripType '{gripTypeRaw}' is invalid. Expected OneHand or TwoHand.");
            }

            bool carryable = GetBoolParameter(
                blueprint.Name,
                "Handling",
                "Carryable",
                handlingParameters,
                true,
                issues);
            bool throwable = GetBoolParameter(
                blueprint.Name,
                "Handling",
                "Throwable",
                handlingParameters,
                true,
                issues);
            int weight = GetIntParameter(
                blueprint.Name,
                "Handling",
                "Weight",
                handlingParameters,
                0,
                issues);
            int minLiftStrength = GetIntParameter(
                blueprint.Name,
                "Handling",
                "MinLiftStrength",
                handlingParameters,
                0,
                issues);
            int minThrowStrength = GetIntParameter(
                blueprint.Name,
                "Handling",
                "MinThrowStrength",
                handlingParameters,
                0,
                issues);

            if (!carryable && throwable)
                issues.Add($"{blueprint.Name}: Handling marks the item throwable while Carryable is false.");

            if (weight < 0)
                issues.Add($"{blueprint.Name}: Handling.Weight cannot be negative.");
            if (minLiftStrength < 0)
                issues.Add($"{blueprint.Name}: Handling.MinLiftStrength cannot be negative.");
            if (minThrowStrength < 0)
                issues.Add($"{blueprint.Name}: Handling.MinThrowStrength cannot be negative.");

            if (blueprint.Parts.TryGetValue("Equippable", out var equippableParameters))
            {
                bool hasUsesSlots = equippableParameters.TryGetValue("UsesSlots", out string usesSlots)
                    && !string.IsNullOrWhiteSpace(usesSlots);

                if (hasUsesSlots)
                {
                    int handCount = CountSlots(usesSlots, "Hand");
                    if (gripType == GripType.TwoHand && handCount < 2)
                    {
                        issues.Add($"{blueprint.Name}: Handling.GripType=TwoHand conflicts with UsesSlots='{usesSlots}'.");
                    }
                    else if (gripType == GripType.OneHand && handCount >= 2)
                    {
                        issues.Add($"{blueprint.Name}: Handling.GripType=OneHand conflicts with UsesSlots='{usesSlots}'.");
                    }
                }
                else if (equippableParameters.TryGetValue("Slot", out string slot)
                    && !string.IsNullOrWhiteSpace(slot)
                    && !string.Equals(slot.Trim(), "Hand", StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add($"{blueprint.Name}: Handling.GripType will override Equippable.Slot='{slot}' when UsesSlots is not set; add explicit UsesSlots to preserve non-hand occupancy.");
                }
            }

            return issues;
        }

        private static bool GetBoolParameter(
            string blueprintName,
            string partName,
            string parameterName,
            Dictionary<string, string> parameters,
            bool defaultValue,
            List<string> issues)
        {
            if (!parameters.TryGetValue(parameterName, out string rawValue))
                return defaultValue;

            if (bool.TryParse(rawValue, out bool value))
                return value;

            issues.Add($"{blueprintName}: {partName}.{parameterName}='{rawValue}' is not a valid bool.");
            return defaultValue;
        }

        private static int GetIntParameter(
            string blueprintName,
            string partName,
            string parameterName,
            Dictionary<string, string> parameters,
            int defaultValue,
            List<string> issues)
        {
            if (!parameters.TryGetValue(parameterName, out string rawValue))
                return defaultValue;

            if (int.TryParse(rawValue, out int value))
                return value;

            issues.Add($"{blueprintName}: {partName}.{parameterName}='{rawValue}' is not a valid int.");
            return defaultValue;
        }

        private static int CountSlots(string slots, string slotName)
        {
            if (string.IsNullOrWhiteSpace(slots))
                return 0;

            string[] parts = slots.Split(',');
            int count = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.Equals(parts[i].Trim(), slotName, StringComparison.OrdinalIgnoreCase))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Get a blueprint by name.
        /// </summary>
        public Blueprint GetBlueprint(string name)
        {
            Blueprints.TryGetValue(name, out Blueprint bp);
            return bp;
        }

        /// <summary>
        /// If the entity has a Body part, initialize its anatomy from the
        /// Anatomy property (e.g. "Humanoid", "Quadruped", "Insectoid", "Simple").
        /// The Body part can declare this via blueprint parameter "Anatomy".
        /// </summary>
        private void InitializeAnatomy(Entity entity)
        {
            var body = entity.GetPart<Body>();
            if (body == null) return;
            if (body.GetBody() != null) return; // already initialized

            // Check for Anatomy property on the entity or infer from blueprint
            string anatomy = entity.GetProperty("Anatomy", null);

            // Default to Humanoid if no anatomy specified
            if (string.IsNullOrEmpty(anatomy))
                anatomy = "Humanoid";

            int category = BodyPartCategory.ANIMAL;
            string catStr = entity.GetProperty("BodyCategory", null);
            if (!string.IsNullOrEmpty(catStr))
            {
                int code = BodyPartCategory.GetCode(catStr);
                if (code > 0) category = code;
            }

            BodyPart root;
            switch (anatomy)
            {
                case "Quadruped":
                    root = AnatomyFactory.CreateQuadruped(category);
                    break;
                case "Insectoid":
                    root = AnatomyFactory.CreateInsectoid(category);
                    break;
                case "Simple":
                    root = AnatomyFactory.CreateSimple(category);
                    break;
                case "Humanoid":
                default:
                    root = AnatomyFactory.CreateHumanoid(category);
                    break;
            }

            body.SetBody(root);

            // Override natural weapon if specified (e.g. "DefaultClaw" for 1d4)
            string naturalWeapon = entity.GetProperty("NaturalWeapon", null);
            if (!string.IsNullOrEmpty(naturalWeapon))
            {
                var parts = root.GetParts();
                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i].Type == "Hand" && !string.IsNullOrEmpty(parts[i].DefaultBehaviorBlueprint))
                        parts[i].DefaultBehaviorBlueprint = naturalWeapon;
                }
            }
        }
    }
}
