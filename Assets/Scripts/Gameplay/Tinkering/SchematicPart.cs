using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Readable schematic item that teaches a tinkering recipe to the reader.
    /// Mirrors GrimoirePart (which teaches spells): declares a "Study"
    /// inventory action; on study, learns RecipeID into the reader's
    /// BitLockerPart. Not consumed by default (set ConsumeOnStudy in the
    /// blueprint for single-use schematics).
    /// Blueprint params: RecipeID, LearnMessage, AlreadyKnownMessage, ConsumeOnStudy.
    /// </summary>
    public class SchematicPart : Part
    {
        public override string Name => "Schematic";

        /// <summary>Tinker recipe ID taught when studied (must exist in TinkerRecipeRegistry).</summary>
        public string RecipeID = "";

        /// <summary>Announcement text shown when the reader learns the recipe.</summary>
        public string LearnMessage = "";

        /// <summary>Message shown if the reader already knows the recipe.</summary>
        public string AlreadyKnownMessage = "You already know this pattern.";

        /// <summary>If true, the schematic is consumed (one from its stack) on a successful study.</summary>
        public bool ConsumeOnStudy = false;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null)
                    actions.AddAction("Study", "study", "StudySchematic", 's', 20);
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command != "StudySchematic") return true;

                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null) return true;

                return DoStudy(actor, e);
            }

            return true;
        }

        private bool DoStudy(Entity actor, GameEvent e)
        {
            e.Handled = true;

            var bitLocker = actor.GetPart<BitLockerPart>();
            if (bitLocker == null)
            {
                MessageLog.Add("The notation means nothing to you.");
                Diag.Record("event", "RecipeLearnRejected", actor: actor,
                    payload: new { recipeId = RecipeID, source = "schematic", reason = "NoBitLocker" });
                return false;
            }

            if (string.IsNullOrWhiteSpace(RecipeID)
                || !TinkerRecipeRegistry.TryGetRecipe(RecipeID, out TinkerRecipe recipe))
            {
                MessageLog.Add("The diagrams reference tools and terms no one uses anymore.");
                Diag.Record("event", "RecipeLearnRejected", actor: actor,
                    payload: new { recipeId = RecipeID, source = "schematic", reason = "UnknownRecipe" });
                return false;
            }

            if (bitLocker.KnowsRecipe(recipe.ID))
            {
                MessageLog.Add(AlreadyKnownMessage);
                Diag.Record("event", "RecipeLearnRejected", actor: actor,
                    payload: new { recipeId = recipe.ID, source = "schematic", reason = "AlreadyKnown" });
                return false;
            }

            bitLocker.LearnRecipe(recipe.ID);

            if (!string.IsNullOrEmpty(LearnMessage))
                MessageLog.AddAnnouncement(LearnMessage);
            else
                MessageLog.AddAnnouncement($"You study {ParentEntity.GetDisplayName()} and learn: {recipe.DisplayName}.");

            Diag.Record("event", "RecipeLearned", actor: actor,
                payload: new { recipeId = recipe.ID, source = "schematic", consumed = ConsumeOnStudy });

            if (ConsumeOnStudy)
                ConsumeSelf(actor);

            return false;
        }

        private void ConsumeSelf(Entity actor)
        {
            var stacker = ParentEntity?.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
            {
                stacker.StackCount -= 1;
                return;
            }

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory != null && inventory.Contains(ParentEntity))
                inventory.RemoveObject(ParentEntity);
        }
    }
}
