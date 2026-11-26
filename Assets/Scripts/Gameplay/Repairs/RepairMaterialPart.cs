using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Explicit suitability offered by a prepared repair commodity.</summary>
    public sealed class RepairMaterialPart : Part
    {
        public override string Name => "RepairMaterial";
        internal const string LiningClay = "lining-clay";
        public string Grade = "";

        // The authored grade and the live item must agree. Canonical payload
        // checking rejects extra contents, effects and unsupported alterations.
        internal static MaterialProcessItemState Read(EntityFactory factory, Entity item, string grade)
        {
            if (grade != LiningClay || item?.GetPart<RepairMaterialPart>()?.Grade != grade
                || !MaterialProcessRegistry.Portable(factory, item.BlueprintName)) return null;
            return MaterialProcessTemplate.Capture(factory, item.BlueprintName)?.Snapshot(item);
        }

        internal static bool ReadPayment(EntityFactory factory, Entity item, RepairRecipe recipe, out MaterialProcessItemState alternate)
        {
            alternate = null;
            if (item == null || recipe == null) return false;
            if (item.BlueprintName == recipe.MaterialBlueprint) return true;
            if (string.IsNullOrEmpty(recipe.SubstituteBlueprint))
                alternate = Read(factory, item, recipe.MaterialGrade);
            else if (item.BlueprintName == recipe.SubstituteBlueprint
                && MaterialProcessRegistry.Portable(factory, item.BlueprintName)
                && (item.GetPart<StackerPart>() is not StackerPart stack
                    || stack.StackCount >= 1 && stack.StackCount <= stack.MaxStack))
                alternate = MaterialProcessTemplate.Capture(factory, item.BlueprintName)?.Snapshot(item);
            return alternate != null;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "ObjectCreated") MaterialProcessRegistry.RememberPayload(ParentEntity?.BlueprintName);
            return true;
        }

        public override void OnAfterLoad(SaveReader reader)
            => MaterialProcessRegistry.RememberPayload(ParentEntity?.BlueprintName);
    }
}
