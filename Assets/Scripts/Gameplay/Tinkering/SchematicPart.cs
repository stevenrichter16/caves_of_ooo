using System;
using CavesOfOoo.Core.Inventory;
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
                var actor = e.GetParameter<Entity>("Actor");
                if (actions != null && StudyRefusal(actor, out _) == null)
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

        /// <summary>Read-only item facts shared by world and inventory inspection.
        /// This explains the current release scope; it never grants tinkering access.</summary>
        public string DescribeAvailability()
        {
            string pattern = TinkerRecipeRegistry.TryGetRecipe(RecipeID, out var recipe)
                ? "Pattern: " + recipe.DisplayName + "." : "Pattern unavailable: no matching recipe exists.";
            return pattern + "\nTinkering is unavailable in this build. This schematic does not unlock it; it can still be kept or traded."
                + (ConsumeOnStudy ? " A successful study consumes one schematic." : " A successful study keeps the schematic.");
        }

        private string StudyRefusal(Entity actor, out TinkerRecipe recipe)
        {
            recipe = null;
            if (actor == null) return "NoReader";
            var inventory = actor.GetPart<InventoryPart>();
            var item = ParentEntity;
            var physics = item?.GetPart<PhysicsPart>();
            if (item == null || item.GetPart<SchematicPart>() != this || inventory?.ParentEntity != actor
                || !inventory.CanConsumeOne(item) || item.SpatialZone != null
                || inventory.EquippedItems.ContainsValue(item) || inventory.FindEquippedBodyPart(item) != null
                || (physics != null && (physics.ParentEntity != item || physics.InInventory != actor || physics.Equipped != null)))
                return "NotCarried";
            var access = actor.GetPart<BitLockerPart>();
            if (access?.ParentEntity != actor) return "NoBitLocker";
            if (!TinkerRecipeRegistry.TryGetRecipe(RecipeID, out recipe)) return "UnknownRecipe";
            return access.KnowsRecipe(recipe.ID) ? "AlreadyKnown" : null;
        }

        private bool DoStudy(Entity actor, GameEvent e)
        {
            string refusal = StudyRefusal(actor, out var recipe);
            if (refusal != null)
            {
                MessageLog.Add(refusal == "NoBitLocker" ? "You cannot study this pattern: tinkering is unavailable in this build."
                    : refusal == "AlreadyKnown" ? AlreadyKnownMessage : "This schematic cannot be studied now.");
                Diag.Record("event", "RecipeLearnRejected", actor: actor,
                    payload: new { recipeId = RecipeID, source = "schematic", reason = refusal });
                return true; // A rejected action must not report success to the inventory command.
            }

            var transaction = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            bool local = transaction == null;
            transaction = transaction ?? new InventoryTransaction();
            bool accepted = false;
            try
            {
                var item = ParentEntity;
                var inventory = actor.GetPart<InventoryPart>();
                var access = actor.GetPart<BitLockerPart>();
                var physics = item.GetPart<PhysicsPart>();
                var stack = item.GetPart<StackerPart>();
                int count = stack?.StackCount ?? 1;
                string selectedRecipe = RecipeID;
                string learnedRecipe = recipe.ID;
                bool consume = ConsumeOnStudy;
                if (!transaction.TryClaim(actor, actor, "StudySchematic")
                    || !transaction.TryClaim(item, actor, "StudySchematic")) return true;
                if (consume)
                {
                    var payment = InventoryTransferSnapshot.Capture(inventory, item);
                    transaction.Do(null, payment.Restore);
                    if (!payment.Apply(() => inventory.TryConsumeOne(item))
                        || !payment.ClaimChanges(transaction, actor, "StudySchematic")) return true;
                }
                bool Current()
                {
                    if (ParentEntity != item || item.GetPart<SchematicPart>() != this
                        || RecipeID != selectedRecipe || recipe.ID != learnedRecipe || ConsumeOnStudy != consume
                        || actor.GetPart<InventoryPart>() != inventory || actor.GetPart<BitLockerPart>() != access
                        || access.ParentEntity != actor || access.KnowsRecipe(recipe.ID)
                        || !TinkerRecipeRegistry.TryGetRecipe(selectedRecipe, out var current) || current != recipe
                        || item.GetPart<PhysicsPart>() != physics || item.GetPart<StackerPart>() != stack
                        || item.SpatialZone != null || inventory.EquippedItems.ContainsValue(item)
                        || inventory.FindEquippedBodyPart(item) != null || physics?.Equipped != null) return false;
                    if (consume && count == 1)
                        return !inventory.Objects.Contains(item) && physics?.InInventory == null;
                    return inventory.CanConsumeOne(item) && (stack?.StackCount ?? 1) == count - (consume ? 1 : 0)
                        && (physics == null || physics.InInventory == actor);
                }
                if (!Current()) return true;
                transaction.BeforeCommit(Current);
                // Knowledge is published only after the outer inventory command's
                // callbacks and payment commit; a refunded study teaches nothing.
                transaction.AfterCommit(() => access.LearnRecipe(learnedRecipe));
                transaction.AfterCommit(() => {
                    MessageLog.AddAnnouncement(!string.IsNullOrEmpty(LearnMessage) ? LearnMessage
                        : $"You study {item.GetDisplayName()} and learn: {recipe.DisplayName}.");
                    Diag.Record("event", "RecipeLearned", actor: actor,
                        payload: new { recipeId = learnedRecipe, source = "schematic", consumed = consume });
                });
                if (local) transaction.Commit();
                accepted = true;
                e.Handled = true;
                return false;
            }
            finally { if (local && !accepted) transaction.Rollback(); }
        }
    }
}
