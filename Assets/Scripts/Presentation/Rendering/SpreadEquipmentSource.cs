using System;
using System.Collections.Generic;
namespace CavesOfOoo.Rendering
{
    /// <summary>Bounded original fitted-wearable source. Coordinates are local to
    /// the declared equipment socket, not the flat dropped-item coordinate frame.</summary>
    [Serializable] public sealed class SpreadEquipmentSource
    {
        public int schemaVersion, paletteCells;
        public Model[] models;
        [Serializable] public sealed class Model
        { public string id, blueprint, slot; public Box[] boxes; }
        [Serializable] public sealed class Box
        { public float[] center, size; public int paint; }
        public static readonly IReadOnlyList<string> Blueprints = Array.AsReadOnly(new[] { "LeatherArmor", "ChainMail", "PlateArmor", "FineRingMail", "RivetedPlate", "LeatherCap", "IronHelmet", "LeatherBoots", "IronshodBoots", "LeatherGloves", "Cloak", "WardedCloak" });
        public void Validate()
        {
            if (schemaVersion != 1 || paletteCells != 42 || models == null || models.Length != Blueprints.Count)
                throw new InvalidOperationException("Incomplete fitted equipment source.");
            var expected = new HashSet<string>(Blueprints, StringComparer.Ordinal);
            foreach (var model in models)
            {
                string slot = model?.blueprint == "LeatherCap" || model?.blueprint == "IronHelmet" ? "Head"
                    : model?.blueprint == "LeatherBoots" || model?.blueprint == "IronshodBoots" ? "Feet"
                    : model?.blueprint == "LeatherGloves" ? "Handwear"
                    : model?.blueprint == "Cloak" || model?.blueprint == "WardedCloak" ? "Back" : "Body";
                if (model == null || model.blueprint == null || !expected.Remove(model.blueprint)
                    || model.id != "spread-worn-" + model.blueprint.ToLowerInvariant() || model.slot != slot
                    || model.boxes == null || model.boxes.Length < 1 || model.boxes.Length > 256)
                    throw new InvalidOperationException("Invalid fitted equipment identity.");
                foreach (var box in model.boxes)
                {
                    if (box == null || box.center == null || box.size == null || box.center.Length != 3
                        || box.size.Length != 3 || box.paint < 0 || box.paint >= paletteCells)
                        throw new InvalidOperationException("Invalid fitted equipment box.");
                    for (int i = 0; i < 3; i++)
                        if (!Finite(box.center[i]) || !Finite(box.size[i]) || box.size[i] < .01f || box.size[i] > 1
                            || Math.Abs(box.center[i]) + box.size[i] * .5f > 1)
                            throw new InvalidOperationException("Unbounded fitted equipment geometry.");
                }
            }
            if (expected.Count != 0) throw new InvalidOperationException("Missing fitted equipment form.");
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
