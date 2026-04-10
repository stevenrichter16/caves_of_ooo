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

        // Temperature bounds are symmetric: the resolver treats the
        // sentinel defaults (float.MinValue / float.MaxValue) as "no check"
        // and enforces any explicit value — including zero and negative
        // temperatures — so authors can target subzero windows without the
        // resolver silently skipping their MinTemperature.
        public float MinTemperature = float.MinValue;
        public float MaxTemperature = float.MaxValue;

        // Moisture/material scalars are in [0,1], so 0 is the natural
        // "no minimum" sentinel and is checked via > 0.
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
