using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Pressing a reagent into a flask (Docs/MACHINE-PROOFING.md): the carried, temporary tier's bottle. A reagent offers one row per element it carries (its own heat, cold, conductive or corrosive
    /// property) and per eligible flask: empty, or already holding that element's proofing, with room for the coats it makes. The reagent's level for the property is the number of coats. One reagent
    /// and that room are spent whole or not at all. It needs no machine and no fire, so it can be done at the edge of the place you are about to cross.
    /// </summary>
    internal static class ProofingPressActions
    {
        const string Prefix = "PressProofing|";
        const int MaxFlasksPerElement = 3;

        internal static bool IsCommand(string command) => command != null && command.StartsWith(Prefix, StringComparison.Ordinal);

        /// <summary>A carried single flask whose contents are consistent; null otherwise (a waterskin, a stack of flasks, a malformed vessel).</summary>
        static LiquidVesselPart Flask(Entity actor, Entity item)
        {
            var vessel = item?.GetPart<LiquidVesselPart>(); var stack = item?.GetPart<StackerPart>();
            if (vessel == null || vessel.ParentEntity != item || item.HasPart<WaterskinPart>() || item.HasTag("Creature") || item.HasTag("Terrain")) return null;
            if (!WorldResourceActions.Carried(actor, item) || (stack != null && stack.StackCount != 1)) return null;
            if (vessel.Capacity <= 0 || vessel.Volume < 0 || vessel.Volume > vessel.Capacity) return null;
            if (vessel.Volume == 0 ? !string.IsNullOrEmpty(vessel.LiquidId) : string.IsNullOrEmpty(vessel.LiquidId)) return null;
            return vessel;
        }

        /// <summary>" You have seen heat nearby (steam vent)." when the viewer has seen that element's harm in this zone, else empty.</summary>
        internal static string SeenNote(Zone zone, Entity actor, ProofingElement element)
        {
            string seen = ElementalThreats.ForElement(zone, actor, element.Id); return seen.Length == 0 ? "" : " You have seen " + element.Label + " nearby (" + seen + ").";
        }

        static string CoatsText(int units) => units + (units == 1 ? " coat" : " coats");

        internal static void AddActions(Entity actor, Entity reagent, Zone zone, InventoryActionList actions)
        {
            if (actions == null || actor == null || reagent == null || !WorldResourceActions.Carried(actor, reagent, single: false) || !WorldResourceActions.ActorCurrent(actor, zone)) return;
            var elements = ProofingRegistry.ElementsOf(reagent); if (elements.Count == 0) return;
            var pack = actor.GetPart<InventoryPart>(); if (pack == null) return;
            foreach (var element in elements)
            {
                int units = ProofingRegistry.Units(reagent, element); if (units <= 0) continue;
                var liquid = LiquidRegistry.IsInitialized ? LiquidRegistry.Get(element.Liquid) : null; if (liquid == null) continue;
                var candidates = new List<(Entity flask, LiquidVesselPart vessel)>(); int shortOfRoom = 0, roomLeft = 0;
                foreach (var item in pack.Objects)
                {
                    var vessel = Flask(actor, item); if (vessel == null || (vessel.Volume > 0 && vessel.LiquidId != element.Liquid)) continue;
                    if (vessel.Capacity - vessel.Volume < units) { shortOfRoom++; roomLeft = Math.Max(roomLeft, vessel.Capacity - vessel.Volume); continue; }
                    candidates.Add((item, vessel));
                }
                string label = "press into a flask (" + liquid.DisplayName + ", " + CoatsText(units) + ")";
                if (candidates.Count == 0)
                {
                    if (shortOfRoom > 0) actions.AddDisabled("PressProofing", label, "no-room", "A flask needs room for " + units + " (" + CoatsText(units) + "); the best you carry has room for " + roomLeft + ".", '\0', 15);
                    else actions.AddDisabled("PressProofing", label, "no-flask", "You need a flask that is empty or already holds " + liquid.DisplayName + ".", '\0', 15);
                    continue;
                }
                candidates.Sort((a, b) => b.vessel.Volume.CompareTo(a.vessel.Volume));
                for (int i = 0; i < candidates.Count && i < MaxFlasksPerElement; i++)
                {
                    var (flask, vessel) = candidates[i];
                    actions.AddActionWithHint("PressProofing", "press into " + flask.GetDisplayName() + " (" + liquid.DisplayName + ", " + CoatsText(units) + "; " + vessel.Volume + "→" + (vessel.Volume + units) + "/" + vessel.Capacity + ")",
                        Prefix + element.Id + "|" + Uri.EscapeDataString(flask.ID),
                        "Spends one " + reagent.GetDisplayName() + ". Each coat later proofs one garment against " + element.Label + ": +" + ProofingRules.CoatAmount + " " + element.Label + " resistance for " + ProofingRules.CoatTurns + " worn turns, and it blunts " + element.Status + ". No fire or workbench needed." + SeenNote(zone, actor, element),
                        '\0', 15);
                }
            }
        }

        internal static bool TryAct(Entity actor, Entity reagent, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command)) return false;
            var fields = command.Split('|'); if (fields.Length != 3) return Reject(actor, reagent, "malformed-selection");
            var element = ProofingRegistry.ById(fields[1]); var pack = actor?.GetPart<InventoryPart>();
            if (element == null || pack == null || !WorldResourceActions.ActorCurrent(actor, zone) || !WorldResourceActions.Carried(actor, reagent, single: false)) return Reject(actor, reagent, "unavailable");
            var flask = WorldResourceActions.ExactCarried(actor, fields[2]); var vessel = Flask(actor, flask);
            int units = ProofingRegistry.Units(reagent, element); var liquid = LiquidRegistry.IsInitialized ? LiquidRegistry.Get(element.Liquid) : null;
            if (vessel == null || units <= 0 || liquid == null || (vessel.Volume > 0 && vessel.LiquidId != element.Liquid) || vessel.Capacity - vessel.Volume < units) return Reject(actor, reagent, "invalid-flask-or-room");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(reagent, actor, command) || !tx.TryClaim(flask, actor, command)) return Reject(actor, reagent, "in-progress");
                int volumeBefore = vessel.Volume; string liquidBefore = vessel.LiquidId; string reagentName = reagent.GetDisplayName();
                var receipt = InventoryTransferSnapshot.Capture(pack);
                tx.Do(null, receipt.Restore);
                if (!receipt.Apply(() => pack.TryConsumeOne(reagent)) || !receipt.ClaimChanges(tx, actor, command)) return Reject(actor, reagent, "payment-refused");
                tx.Do(() => { vessel.Volume = volumeBefore + units; vessel.LiquidId = element.Liquid; }, () => { vessel.Volume = volumeBefore; vessel.LiquidId = liquidBefore; });
                tx.AfterCommit(() =>
                {
                    MessageLog.Add("You press the " + reagentName + " into the flask: " + CoatsText(units) + " of " + liquid.DisplayName + ".");
                    Diag.Record("liquid", "ProofingPressed", actor, flask, new { element = element.Id, units, flaskVolume = volumeBefore + units, reagent = reagentName });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        static bool Reject(Entity actor, Entity target, string reason)
        {
            Diag.Record("liquid", "ProofingPressRefused", actor, target, new { reason });
            if (actor?.HasTag("Player") == true) MessageLog.Add("You cannot press that into a flask now; nothing is spent.");
            return false;
        }
    }
}
