using System;
using System.Collections.Generic;
using UnityEngine;
using CavesOfOoo.Core;
namespace CavesOfOoo.Rendering
{
    /// <summary>Complete read-only preflight for the original biome botanical kit.
    /// All coordinates are in one native cell; assets add no collision or light.</summary>
    [Serializable]
    public sealed class BiomeCropSource
    {
        public int schemaVersion;
        public string id;
        public string[] palette;
        public Model[] models;
        public Species[] species;
        [Serializable] public sealed class Species { public string name,stem,biome,seedBlueprint,cropBlueprint,harvestBlueprint; }
        [Serializable] public sealed class Model { public string id,kind; public Box[] boxes; }
        [Serializable] public sealed class Box { public Vector3 center, size; public int color; }
        public static IReadOnlyList<string> ApprovedPalette { get; } = Array.AsReadOnly(new[] {
            "#082C28", "#103E36", "#1A4B40", "#26594A", "#A0A77C", "#CBC697",
            "#647353", "#235D25", "#40872C", "#65AE3D", "#435A53", "#62786C",
            "#819489", "#16883B", "#45CB4B", "#A0E772", "#207838", "#D2D3B4",
            "#B77B43", "#403D28", "#756C40", "#A39456", "#C4B877", "#243E39"
        });
        public static IReadOnlyList<string> ModelIds { get; } = Array.AsReadOnly(CreateIds());
        private static readonly HashSet<string> KnownIds = new HashSet<string>(ModelIds, StringComparer.Ordinal);
        private static string[] CreateIds()
        {
            var ids=new List<string>();
            foreach(var row in BiomeCropCatalog.All)
            {
                string prefix="biome-crop-"+row.ModelStem;
                for(int stage=0;stage<3;stage++){ids.Add(prefix+"-"+stage+"-dry");ids.Add(prefix+"-"+stage+"-wet");}
                ids.Add(prefix+"-seed");ids.Add(prefix+"-harvest");
            }
            return ids.ToArray();
        }
        public static bool IsModelId(string value)=>value!=null&&KnownIds.Contains(value);
        public static string KindForModel(string value){if(!IsModelId(value))throw new ArgumentException("Unknown cultivation model.");return "entity";}
        public void Validate()
        {
            if (schemaVersion != 1 || id!="biome-crops-original" || palette == null || palette.Length != ApprovedPalette.Count
                || models == null || models.Length != 280 || ModelIds.Count != 280 || species == null || species.Length != 35)
                throw new InvalidOperationException("Incomplete scenery source pack.");
            for (int i = 0; i < palette.Length; i++)
                if (palette[i] != ApprovedPalette[i]) throw new InvalidOperationException("Unreviewed scenery palette.");
            var names=new HashSet<string>(StringComparer.Ordinal);
            foreach(var plant in species)
            {
                var row=plant==null?null:BiomeCropCatalog.Find(plant.name);
                if(row==null||!names.Add(plant.name)||plant.stem!=row.ModelStem||plant.biome!=row.Biome.ToString()
                    ||plant.seedBlueprint!=row.SeedBlueprint||plant.cropBlueprint!=row.CropBlueprint||plant.harvestBlueprint!=row.YieldBlueprint)
                    throw new InvalidOperationException("Botanical art differs from the exact gameplay roster.");
            }
            var remaining = new HashSet<string>(ModelIds, StringComparer.Ordinal);
            foreach (var model in models)
            {
                if (model == null || model.id == null || !remaining.Remove(model.id)
                    || model.kind!=KindForModel(model.id)
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
