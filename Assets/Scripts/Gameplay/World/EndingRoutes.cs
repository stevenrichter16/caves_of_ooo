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
    /// epilogue announced). <b>Let the Choir in</b> (Gathered, ER.3) needs a cutting
    /// of the Choir carried; <b>seal the Root</b> (Kept, ER.2) needs the three
    /// name-holding stones. Each consumes exactly what it needs and only once
    /// everything is present, and states its cost on the menu, in the giver's
    /// mouth and in the epilogue: no one remains; nothing heals. No clean-ledger
    /// gate; the act taken on toward the enactment is closed by it, taken on again
    /// first if it had been refused.
    /// </summary>
    public static class EndingRoutes
    {
        public const string SealCommand = "EndingSeal", GatherCommand = "EndingGather";
        public const string KeepQuestId = "KeepTheWorld", KeepTitle = "Keep the World";
        public const string GatherQuestId = "GatherTheWorld", GatherTitle = "Gather the World";
        public const string CuttingBlueprint = "ChoirCutting";
        public static readonly string[] Stones = { "Tepuibone", "MemoryMarble", "MuteStone" };
        private static readonly string[] StoneNames = { "tepuibone", "memory-marble", "mute-stone" };

        public const string KeptEpilogue =
            "You set tepuibone, memory-marble and mute-stone against the face, the three that hold their names hardest, and the Root is sealed. " +
            "The Thinning halts. The seal must be tended forever, and it will be; you have made a vigil, not a cure, because you cannot heal a tiring thing by stopping its clock. " +
            "The world is held still: nothing decays, and nothing heals. The estranged stay estranged. The tired stay tired, unable to die. " +
            "The festival-flowers last the afternoon and not a minute more, and never will again. " +
            "And the Six survive as themselves - the Reader still reading, the Rooted still reaching, every god you have come to know still there, exactly as they are, forever. That is not nothing. It is everything, kept.";
        public const string GatheredEpilogue =
            "You set the cutting against the face and it takes, the way water takes the way to the sea. The Choir reaches the Root through the substrate, and the Wedded ascends to sole divinity; the pantheon collapses to one. " +
            "The world becomes a single warm devouring memory - there is room in us for everyone - every self untied and its threads re-woven into the great weave. Nothing is forgotten; no one remains. " +
            "The bio-light goes out across every catacomb-village as the Rooted fades, then returns, changed, as the glow of the new Tree-Choir. The sari... sari... stops: a world with one name has no seams left for the pressure to work. " +
            "The flower-charms bloom, briefly, in the few unconsumed pockets, and then those bloom into the Choir too. " +
            "The kindest apocalypse and the most total: a world that remembers everything and is no one, and the only one, of the possible worlds, in which nothing is ever lost again.";

        public static bool IsWorldCommand(string command) => command == SealCommand || command == GatherCommand;

        /// <summary>Advertise both enactments on the face while no ending has been enacted.</summary>
        public static void AddActions(InventoryActionList actions, Entity player)
        {
            if (actions == null || EndingSpine.Enacted(player) != 0) return;
            actions.AddAction("EndingGather", "let the Choir in: gathered, no one left", GatherCommand, 'g', 30);
            actions.AddAction("EndingSeal", "seal the Root: kept, nothing heals", SealCommand, 'k', 29);
        }

        public static bool TryWorldAction(Entity target, Entity actor, Zone zone, string command, out int energyCost)
        {
            energyCost = 0;
            if (!IsWorldCommand(command)) return false;
            bool gather = command == GatherCommand;
            string reason = Refusal(target, actor, zone, gather, out var missing, out var consume);
            if (reason != null)
            {
                string tell = reason == "not_adjacent" ? "Stand beside the face." : reason == "already_enacted" ? "It is done; there is no second time."
                    : reason == "missing_stones" ? "The seal needs tepuibone, memory-marble and mute-stone; you lack " + string.Join(", ", missing) + "."
                    : reason == "no_cutting" ? "You carry no cutting of the Choir to set against it." : null;
                if (tell != null) MessageLog.Add(tell);
                Diag.Record("ending", "Rejected", actor: actor, target: target, payload: new { command, reason, missing = missing == null ? null : string.Join(",", missing) });
                return false;
            }
            // Everything needed was found before anything is consumed: no partial enactment.
            var inventory = actor.GetPart<InventoryPart>();
            foreach (var item in consume) if (!inventory.TryConsumeOne(item)) { Diag.Record("ending", "Rejected", actor: actor, target: target, payload: new { command, reason = "not_consumable" }); return false; }
            int path = gather ? EndingSpine.GatheredPath : EndingSpine.KeptPath;
            actor.SetIntProperty(EndingSpine.EndingProperty, path);
            NarrativeStatePart.Current?.SetFact(EndingSpine.EndingFact, path);
            var examine = target.GetPart<ExaminablePart>();
            if (examine != null) examine.Text = gather
                ? "The face is threaded now: the substrate has reached it, and the pale stone is veined green to the edges of the hollow. It does not dream alone. It sings, one syllable of the deeper sentence, and the sentence is everything."
                : "The face is under three stones now - tepuibone, memory-marble, mute-stone - set flush against it and already cold. It does not dream. It is kept, and the keeping must never stop.";
            var cell = zone.GetEntityCell(target); if (cell != null) ZoneRenderHooks.MarkCellDirty(cell.X, cell.Y, gather ? "EndingGathered" : "EndingKept");
            CloseTheAct(gather ? GatherQuestId : KeepQuestId, gather ? GatherTitle : KeepTitle, target, zone);
            // The reading follows the closing, so `open` in the record says what remains open after the world is ended.
            var reading = StoryletPart.Current.ReadLedger(actor);
            MessageLog.Add(gather ? "The cutting takes." : "The stones go against the face and stay.");
            NarrativeStatePart.Current?.LogEvent(gather ? "Let the Choir into the Root: the world gathered." : "Sealed the Root with the three name-holding stones: the world kept.");
            MessageLog.AddAnnouncement(gather ? GatheredEpilogue : KeptEpilogue);
            Diag.Record("ending", "Enacted", actor: actor, target: target, payload: new { path = gather ? "gathered" : "kept", closed = reading.Closed, refused = reading.Refused, open = reading.Open, consumed = consume.Count });
            energyCost = EndingSpine.EnactCost;
            return true;
        }

        /// <summary>The enactment closes the act taken on toward it. An act never taken
        /// on, or refused earlier, is taken on here (from the face, at the Root) and
        /// closed in one motion: a carried-through act is closed, whatever was said before.</summary>
        private static void CloseTheAct(string questId, string title, Entity face, Zone zone)
        {
            var sp = StoryletPart.Current;
            if (!sp.IsQuestActive(questId)) sp.UndertakeAct(questId, title, face, zone);
            sp.MarkQuestCompleted(questId);
        }

        private static string Refusal(Entity face, Entity actor, Zone zone, bool gather, out List<string> missing, out List<Entity> consume)
        {
            missing = null; consume = null;
            // ER.5 (adversarial sweep): the Root is the one authored face, by its fixed id — a second face
            // standing in the chamber is not it.
            if (face?.GetPart<RootFacePart>() == null || face.ID != RootSiteBuilder.FaceId || zone == null || zone.ZoneID != RootSiteBuilder.ChamberZoneID) return "not_the_root";
            if (actor == null || !actor.HasTag("Player") || actor.GetStatValue("Hitpoints", 0) <= 0 || CombatSystem.IsDeathHandled(actor)) return "no_actor";
            if (EndingSpine.Enacted(actor) != 0) return "already_enacted";
            var tc = zone.GetEntityCell(face); var ac = zone.GetEntityCell(actor);
            if (tc == null || ac == null || Math.Max(Math.Abs(tc.X - ac.X), Math.Abs(tc.Y - ac.Y)) != 1) return "not_adjacent";
            if (StoryletPart.Current == null) return "no_ledger";
            missing = new List<string>(); consume = new List<Entity>();
            var inventory = actor.GetPart<InventoryPart>();
            if (gather)
            {
                var cutting = Find(inventory, CuttingBlueprint);
                if (cutting == null) { missing.Add("a cutting of the Choir"); return "no_cutting"; }
                consume.Add(cutting); return null;
            }
            for (int i = 0; i < Stones.Length; i++)
            {
                var found = Find(inventory, Stones[i]);
                if (found == null) missing.Add(StoneNames[i]); else consume.Add(found);
            }
            return missing.Count > 0 ? "missing_stones" : null;
        }

        /// <summary>Top-level inventory only, one consumable unit - the same reach the
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
