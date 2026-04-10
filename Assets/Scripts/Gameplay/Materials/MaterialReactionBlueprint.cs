using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Serializable data class for material reaction definitions.
    /// Loaded from JSON files in Resources/Content/Data/MaterialReactions/.
    /// </summary>
    [Serializable]
    public class MaterialReactionBlueprint
    {
        public string ID;
        public int Priority;
        public ReactionConditions Conditions;
        public List<ReactionEffect> Effects;
    }

    [Serializable]
    public class ReactionConditions
    {
        public string SourceState;
        public string TargetMaterialTag;
        public float MinTemperature;
        public float MaxTemperature = float.MaxValue;
        public float MaxMoisture = 1.0f;
        public float MinMoisture;
        public float MinBrittleness;
        public float MinConductivity;
        public float MinVolatility;
    }

    [Serializable]
    public class ReactionEffect
    {
        public string Type;
        public float FloatValue;
        public string StringValue;
    }

    [Serializable]
    public class MaterialReactionCollection
    {
        public List<MaterialReactionBlueprint> Reactions;
    }
}
