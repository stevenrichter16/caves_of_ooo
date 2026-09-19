using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The other advertised routes (Docs/ENDING-ROUTES.md): the enactments offered
    /// at the Root's face, through the spine's own state (<see cref="EndingSpine"/>:
    /// one enactment ever, persisted on the player and as a narrative fact, the
    /// epilogue announced). ER.2 — <b>seal the Root</b> (Kept): needs the three
    /// name-holding stones carried, consumes exactly one of each, and states its
    /// cost on the menu, in the giver's mouth and in the epilogue: nothing heals;
    /// the Six survive as themselves. No clean-ledger gate; the act taken on toward
    /// it is closed by the enactment, taken on again first if it had been refused.
    /// </summary>
    public static class EndingRoutes
    {
        public const string SealCommand = "EndingSeal";
        public const string KeepQuestId = "KeepTheWorld", KeepTitle = "Keep the World";
        public static readonly string[] Stones = { "Tepuibone", "MemoryMarble", "MuteStone" };
        private static readonly string[] StoneNames = { "tepuibone", "memory-marble", "mute-stone" };

        public const string KeptEpilogue =
            "You set tepuibone, memory-marble and mute-stone against the face, the three that hold their names hardest, and the Root is sealed. " +
            "The Thinning halts. The seal must be tended forever, and it will be; you have made a vigil, not a cure, because you cannot heal a tiring thing by stopping its clock. " +
            "The world is held still: nothing decays, and nothing heals. The estranged stay estranged. The tired stay tired, unable to die. " +
            "The festival-flowers last the afternoon and not a minute more, and never will again. " +
            "And the Six survive as themselves - the Reader still reading, the Rooted still reaching, every god you have come to know still there, exactly as they are, forever. That is not nothing. It is everything, kept.";

        public static bool IsWorldCommand(string command) => command == SealCommand;

        /// <summary>Advertise the seal on the face while no ending has been enacted.</summary>
        public static void AddActions(InventoryActionList actions, Entity player)
        {
            if (actions == null || EndingSpine.Enacted(player) != 0) return;
            actions.AddAction("EndingSeal", "seal the Root: kept, nothing heals", SealCommand, 'k', 30);
        }

        public static bool TryWorldAction(Entity target, Entity actor, Zone zone, string command, out int energyCost)
        {
            energyCost = 0;
            if (!IsWorldCommand(command)) return false;
            string reason = Refusal(target, actor, zone, out var missing, out var carried);
            if (reason != null)
            {
                string tell = reason == "not_adjacent" ? "Stand beside the face." : reason == "already_enacted" ? "It is done; there is no second time."
                    : reason == "missing_stones" ? "The seal needs tepuibone, memory-marble and mute-stone; you lack " + string.Join(", ", missing) + "." : null;
                if (tell != null) MessageLog.Add(tell);
                Diag.Record("ending", "Rejected", actor: actor, target: target, payload: new { command, reason, missing = missing == null ? null : string.Join(",", missing) });
                return false;
            }
            // Every stone was found before any is consumed: no partial sealing.
            var inventory = actor.GetPart<InventoryPart>();
            foreach (var stone in carried) if (!inventory.TryConsumeOne(stone)) { Diag.Record("ending", "Rejected", actor: actor, target: target, payload: new { command, reason = "stone_not_consumable" }); return false; }
            var reading = StoryletPart.Current.ReadLedger(actor);
            actor.SetIntProperty(EndingSpine.EndingProperty, EndingSpine.KeptPath);
            NarrativeStatePart.Current?.SetFact(EndingSpine.EndingFact, EndingSpine.KeptPath);
            var examine = target.GetPart<ExaminablePart>();
            if (examine != null) examine.Text = "The face is under three stones now - tepuibone, memory-marble, mute-stone - set flush against it and already cold. It does not dream. It is kept, and the keeping must never stop.";
            var cell = zone.GetEntityCell(target); if (cell != null) ZoneRenderHooks.MarkCellDirty(cell.X, cell.Y, "EndingKept");
            CloseTheAct(target, zone);
            MessageLog.Add("The stones go against the face and stay.");
            NarrativeStatePart.Current?.LogEvent("Sealed the Root with the three name-holding stones: the world kept.");
            MessageLog.AddAnnouncement(KeptEpilogue);
            Diag.Record("ending", "Enacted", actor: actor, target: target, payload: new { path = "kept", closed = reading.Closed, refused = reading.Refused, open = reading.Open, stones = Stones.Length });
            energyCost = EndingSpine.EnactCost;
            return true;
        }

        /// <summary>The enactment closes the act taken on toward it. An act never taken
        /// on, or refused earlier, is taken on here (from the face, at the Root) and
        /// closed in one motion: a carried-through act is closed, whatever was said before.</summary>
        private static void CloseTheAct(Entity face, Zone zone)
        {
            var sp = StoryletPart.Current;
            if (!sp.IsQuestActive(KeepQuestId)) sp.UndertakeAct(KeepQuestId, KeepTitle, face, zone);
            sp.MarkQuestCompleted(KeepQuestId);
        }

        private static string Refusal(Entity face, Entity actor, Zone zone, out List<string> missing, out List<Entity> carried)
        {
            missing = null; carried = null;
            if (face?.GetPart<RootFacePart>() == null || zone == null || zone.ZoneID != RootSiteBuilder.ChamberZoneID) return "not_the_root";
            if (actor == null || !actor.HasTag("Player") || actor.GetStatValue("Hitpoints", 0) <= 0 || CombatSystem.IsDeathHandled(actor)) return "no_actor";
            if (EndingSpine.Enacted(actor) != 0) return "already_enacted";
            var tc = zone.GetEntityCell(face); var ac = zone.GetEntityCell(actor);
            if (tc == null || ac == null || Math.Max(Math.Abs(tc.X - ac.X), Math.Abs(tc.Y - ac.Y)) != 1) return "not_adjacent";
            if (StoryletPart.Current == null) return "no_ledger";
            missing = new List<string>(); carried = new List<Entity>();
            var inventory = actor.GetPart<InventoryPart>();
            for (int i = 0; i < Stones.Length; i++)
            {
                var found = Find(inventory, Stones[i]);
                if (found == null) missing.Add(StoneNames[i]); else carried.Add(found);
            }
            return missing.Count > 0 ? "missing_stones" : null;
        }

        /// <summary>Top-level inventory only, one consumable unit — the same reach the
        /// settlement repairs and the material guidance use.</summary>
        private static Entity Find(InventoryPart inventory, string blueprint)
        {
            if (inventory == null) return null;
            for (int i = 0; i < inventory.Objects.Count; i++)
                if (inventory.Objects[i].BlueprintName == blueprint && inventory.CanConsumeOne(inventory.Objects[i])) return inventory.Objects[i];
            return null;
        }
    }
}
