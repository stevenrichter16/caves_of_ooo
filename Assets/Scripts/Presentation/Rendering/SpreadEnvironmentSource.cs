using System;
using System.Collections.Generic;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>Complete read-only preflight for the original successful-environment kit.
    /// All coordinates are in one native cell; assets add no collision or light.</summary>
    [Serializable]
    public sealed class SpreadEnvironmentSource
    {
        public int schemaVersion;
        public string id;
        public string[] palette;
        public Model[] models;
        [Serializable] public sealed class Model { public string id,family,kind;public int variant; public Box[] boxes; }
        [Serializable] public sealed class Box { public Vector3 center, size; public int color; }
        public static IReadOnlyList<string> Families { get; } = Array.AsReadOnly(new[] {
            "paving","road","tree","hedge","vine-wall","stubble","grain","flowers","dry-brush","rock"
        });
        public static IReadOnlyList<string> ApprovedPalette { get; } = Array.AsReadOnly(new[] {
            "#082C28", "#103E36", "#1A4B40", "#26594A", "#A0A77C", "#CBC697",
            "#647353", "#235D25", "#40872C", "#65AE3D", "#435A53", "#62786C",
            "#819489", "#16883B", "#45CB4B", "#A0E772", "#207838", "#D2D3B4",
            "#B77B43", "#403D28", "#756C40", "#A39456", "#C4B877", "#243E39"
        });
        public static IReadOnlyList<string> ModelIds { get; } = Array.AsReadOnly(CreateIds());
        private static readonly HashSet<string> KnownIds = new HashSet<string>(ModelIds, StringComparer.Ordinal);
        private static string[] CreateIds()
        {var ids=new List<string>(40);foreach(string family in Families)for(int i=0;i<4;i++)ids.Add("spread-environment-"+family+"-"+i);return ids.ToArray();}
        public static bool IsModelId(string value)=>value!=null&&KnownIds.Contains(value);
        public static string ModelId(string family,int variant)
        {if(variant<0||variant>3)throw new ArgumentException("Unknown environment variant.");for(int i=0;i<Families.Count;i++)if(Families[i]==family)return ModelIds[i*4+variant];throw new ArgumentException("Unknown environment family.");}
        public static string KindForModel(string value)
        {if(!IsModelId(value))throw new ArgumentException("Unknown environment model.");return value.StartsWith("spread-environment-paving-",StringComparison.Ordinal)||value.StartsWith("spread-environment-road-",StringComparison.Ordinal)?"ground":"entity";}
        public void Validate()
        {
            if (schemaVersion != 1 || id!="spread-environment-original" || palette == null || palette.Length != ApprovedPalette.Count
                || models == null || models.Length != ModelIds.Count)
                throw new InvalidOperationException("Incomplete scenery source pack.");
            for (int i = 0; i < palette.Length; i++)
                if (palette[i] != ApprovedPalette[i]) throw new InvalidOperationException("Unreviewed scenery palette.");
            var remaining = new HashSet<string>(ModelIds, StringComparer.Ordinal);
            foreach (var model in models)
            {
                if (model == null || model.id == null || !remaining.Remove(model.id)
                    || model.id!=ModelId(model.family,model.variant)||model.kind!=KindForModel(model.id)
                    || model.boxes == null || model.boxes.Length < 3 || model.boxes.Length > 100)
                    throw new InvalidOperationException("Invalid scenery identity or box count.");
                foreach (var box in model.boxes)
                {
                    if (box == null || box.color < 0 || box.color >= palette.Length)
                        throw new InvalidOperationException("Invalid scenery geometry fields.");
                    for (int axis = 0; axis < 3; axis++)
                        if (!Finite(box.center[axis]) || !Finite(box.size[axis]) || box.size[axis] < .003f || box.size[axis]>1.001f)
                            throw new InvalidOperationException("Invalid scenery coordinate.");
                    if (Math.Abs(box.center[0]) + box.size[0] * .5f > .501f
                        || Math.Abs(box.center[2]) + box.size[2] * .5f > .501f
                        || box.center[1] - box.size[1] * .5f < -.0351f
                        || box.center[1] + box.size[1] * .5f > 1.8501f)
                        throw new InvalidOperationException("Scenery geometry leaves its native cell envelope.");
                }
            }
            if (remaining.Count != 0) throw new InvalidOperationException("Missing scenery model.");
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
