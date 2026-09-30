using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        private static readonly string[] ResidentRequired = {
            "resident_generated_plot", "resident_remembered_nearby", "resident_read_travel_note", "resident_seed_trade", "resident_learn_rain", "resident_planted_seed",
            "resident_watered_crop", "resident_grown_crop", "resident_picked_produce", "resident_generated_kitchen",
            "resident_food_trade", "resident_cooked_food", "resident_ate_food", "resident_bed_action", "resident_ordinary_finish" };
        private bool ResidentComplete => _audit.Count == ResidentRequired.Length + 1
            && ResidentRequired.All(n => _audit.Count(a => a == "PASS " + n) == 1) && _biomeShortcuts == 2 && _screenshots.Count >= 6;

        private IEnumerator RunResidentAudit()
        {
            var actor = _input.PlayerEntity; string actorId = actor.ID;
            var inventory = actor.GetPart<InventoryPart>();
            Require(BiomeManager != null && BiomeManager.WorldSeed == 64 && TradeSystem.GetDrams(actor) == 50,
                "isolated original seed64 ordinary starting purse");
            var book = inventory.Objects.Single(e => e.BlueprintName == "WateringGrimoire");
            var plot = BiomeManager.GetZone("Overworld.11.8.0");
            var keeper = plot.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "SpreadSeedKeeper");
            Require(keeper != null && BiomeManager.Exploration.DispositionFor(plot.ZoneID) == 2, "real committed seed-keeper plot");
            var approach = SpecialistApproach(plot, keeper); Require(approach != null, "safe actual resident approach");
            yield return BiomeTravel(plot, approach, "generated seed keeper; actual opening stock and crops unchanged");
            var existing = plot.GetReadOnlyEntities().Where(e => e.HasPart<CropPart>()).ToArray();
            Check("resident_generated_plot", existing.Length == 4 && existing.All(e => e.GetPart<CropPart>().MoistureTicks == 0)
                && BiomeDrawn(keeper));
            yield return Capture("residents-01-seed-keeper-plot");
            yield return ResidentNearbyReport(keeper);
            yield return ResidentTrade(keeper, "CandyCarrotSeed", "resident_seed_trade");
            var seed = inventory.Objects.First(e => e.BlueprintName == "CandyCarrotSeed");
            yield return ItemAction(book, "ReadGrimoire"); yield return ResidentCloseMenus();
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            var rain = abilities.AbilityList.SingleOrDefault(a => a.Command == "CommandConjureRain");
            Check("resident_learn_rain", rain != null && inventory.Objects.Contains(book));
            Require(rain != null, "actual starter book teaches registered rain");
            var ground = Enumerable.Range(0, Zone.Width * Zone.Height).Select(i => plot.GetCell(i % Zone.Width, i / Zone.Width))
                .Where(c => c.Objects.Any(e => e.HasTag("Terrain") && e.HasTag("Plantable"))
                    && !c.Objects.Any(e => e.HasPart<CropPart>()) && BiomeSafe(plot, c, 2)
                    && existing.Any(e => SpatialQuery.DistanceToCell(plot, e, c.X, c.Y) <= 2))
                .OrderBy(c => Math.Max(Math.Abs(Cell().X - c.X), Math.Abs(Cell().Y - c.Y)))
                .FirstOrDefault(c => c == Cell() || FindPath.Search(plot, Cell().X, Cell().Y, c.X, c.Y, actor: actor).Usable);
            Require(ground != null, "reachable real planting ground beside actual garden");
            yield return WalkTo(ground.X, ground.Y);
            int seedsBefore = ResidentUnits(actor, "CandyCarrotSeed");
            yield return ItemAction(seed, "PlantSeed"); yield return ResidentCloseMenus();
            var planted = Cell().Objects.SingleOrDefault(e => e.BlueprintName == "CandyCarrotCrop");
            Check("resident_planted_seed", planted != null && !existing.Contains(planted)
                && ResidentUnits(actor, "CandyCarrotSeed") == seedsBefore - 1 && planted.GetPart<CropPart>().GrowthStage == 0);
            Require(planted != null, "exact naturally planted crop");
            yield return Capture("residents-02-dry-planted-seed");
            int slot = abilities.GetSlotForAbility(rain.ID); Require(slot >= 0 && slot < 10, "rain auto-bound to ordinary hotbar");
            yield return Tap((Key)Enum.Parse(typeof(Key), "Digit" + ((slot + 1) % 10)));
            yield return CombatWaitForFx(); yield return ResidentCloseMenus();
            var crop = planted.GetPart<CropPart>();
            Check("resident_watered_crop", crop.MoistureTicks > 0 && crop.MoistureTicks <= 40 && rain.CooldownRemaining > 0);
            yield return Capture("residents-03-wet-seed-soil");
            for (int waits = 0; crop.GrowthStage == 0 && plot.GetEntityCell(planted) != null && waits < 25; waits++)
            {
                Require(State() == "Normal" && actor.GetStatValue("Hitpoints") > 10, "ordinary live garden wait");
                yield return Tap(Key.Period);
            }
            Check("resident_grown_crop", plot.GetEntityCell(planted) != null && crop.GrowthStage == 1 && BiomeDrawn(planted));
            yield return Capture("residents-04-grown-gladroot");
            for (int waits = 0; plot.GetEntityCell(planted) != null && waits < 25; waits++)
            {
                Require(State() == "Normal" && actor.GetStatValue("Hitpoints") > 10, "ordinary live maturity wait");
                yield return Tap(Key.Period);
            }
            Require(plot.GetEntityCell(planted) == null, "actual crop became finite ground produce");
            var products = Cell().Objects.Where(e => e.BlueprintName == "CandyCarrot").ToArray();
            Require(products.Length == 2, "two actual mature roots at feet");
            int rootsBefore = ResidentUnits(actor, "CandyCarrot");
            yield return Tap(Key.G); if (State() == "PickupOpen") yield return Tap(Key.Tab); yield return ResidentCloseMenus();
            Check("resident_picked_produce", ResidentUnits(actor, "CandyCarrot") == rootsBefore + 2
                && products.All(e => plot.GetEntityCell(e) == null));
            yield return Capture("residents-05-earned-produce");

            var kitchen = BiomeManager.GetZone("Overworld.12.11.0");
            var cook = kitchen.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "SpreadWaysideCook");
            Require(cook != null && BiomeManager.Exploration.DispositionFor(kitchen.ZoneID) == 2, "real committed wayside kitchen");
            approach = SpecialistApproach(kitchen, cook); Require(approach != null, "safe native cook approach");
            yield return BiomeTravel(kitchen, approach, "generated wayside cook; actual food, oven and bed unchanged");
            var oven = kitchen.GetReadOnlyEntities().Single(e => e.BlueprintName == "Oven" && e.HasPart<CampfirePart>());
            var bed = kitchen.GetReadOnlyEntities().Single(e => e.HasPart<BedPart>() && string.IsNullOrEmpty(e.GetPart<BedPart>().Owner));
            Check("resident_generated_kitchen", BiomeDrawn(cook) && !oven.GetPart<CampfirePart>().AllowRest);
            yield return Capture("residents-06-wayside-kitchen");
            yield return ResidentTrade(cook, "RawMeat", "resident_food_trade");
            var raw = inventory.Objects.First(e => e.BlueprintName == "RawMeat");
            approach = SpecialistApproach(kitchen, oven); Require(approach != null, "actual reachable cooking approach");
            yield return WalkTo(approach.X, approach.Y);
            int rawBefore = ResidentUnits(actor, "RawMeat"), cookedBefore = ResidentUnits(actor, "CookedMeat");
            yield return ItemAction(raw, "Cook"); yield return ResidentCloseMenus();
            Check("resident_cooked_food", ResidentUnits(actor, "RawMeat") == 0 && ResidentUnits(actor, "CookedMeat") == cookedBefore + rawBefore);
            var cooked = inventory.Objects.First(e => e.BlueprintName == "CookedMeat");
            int foodBefore = ResidentUnits(actor, "CookedMeat");
            yield return ItemAction(cooked, "Eat"); yield return ResidentCloseMenus();
            Check("resident_ate_food", ResidentUnits(actor, "CookedMeat") == foodBefore - 1);
            var bedCell = kitchen.GetEntityCell(bed); Require(BiomeSafe(kitchen, bedCell, 0), "unoccupied native bed tile");
            yield return WalkTo(bedCell.X, bedCell.Y);
            int restTick = _input.TurnManager.TickCount, restDrams = TradeSystem.GetDrams(actor);
            bool oldFurniture = Diag.IsChannelEnabled("furniture");
            Diag.SetChannel("furniture", true);
            try
            {
                bool reservedBefore = bed.GetPart<BedPart>().Occupied, sittingBefore = actor.HasEffect<SittingEffect>();
                Diag.Record("scenario", "ResidentBedInput", actor, bed, new { runId = RunId, restTick, reservedBefore, sittingBefore });
                var marker = Diag.Snapshot(1).Single();
                yield return ResidentWorldAction(bed, PlayerBedService.RestCommand); yield return ResidentCloseMenus();
                var records = Diag.Snapshot(Diag.BufferCapacity);
                bool windowValid = records.Any(r => r.TraceId == marker.TraceId);
                var outcomes = records.SkipWhile(r => r.TraceId != marker.TraceId).Skip(1)
                    .Where(r => r.TimestampUnixMs >= marker.TimestampUnixMs && r.Category == "furniture" && r.ActorId == actor.ID).ToArray();
                var occupied = outcomes.Where(r => r.Kind == "PlayerBedRejected" && r.TargetId == bed.ID
                    && JObject.Parse(r.PayloadJson).Value<string>("reason") == "occupied").ToArray();
                var hostile = outcomes.Where(r => r.Kind == "RestBlocked"
                    && JObject.Parse(r.PayloadJson).Value<string>("site") == "bed"
                    && JObject.Parse(r.PayloadJson).Value<string>("reason") == "hostile_nearby").ToArray();
                var success = outcomes.Where(r => r.Kind == "Rested"
                    && JObject.Parse(r.PayloadJson).Value<string>("site") == "bed"
                    && JObject.Parse(r.PayloadJson).Value<int>("clockAdvanced") == RestSystem.RestClockTurns).ToArray();
                int elapsed = _input.TurnManager.TickCount - restTick;
                bool rested = elapsed == RestSystem.RestClockTurns && success.Length == 1 && occupied.Length == 0 && hostile.Length == 0;
                bool refused = elapsed == 0 && success.Length == 0 && occupied.Length + hostile.Length == 1;
                string result = rested ? "Native sleep advanced 60 ticks." : refused && occupied.Length == 1
                    ? "Native sleep refused: the bed was reserved/in use, or the actor was sitting (occupied)."
                    : refused ? "Native sleep refused: an actual hostile was nearby." : "Native bed outcome was not verified; observed tick change: " + elapsed + ".";
                _descriptions.Add(new Description { subject = "Generated unowned bed", source = bed.ID, text = result });
                Diag.Record("scenario", "ResidentBedOutcome", actor, bed, new { runId = RunId, marker = marker.TraceId,
                    windowValid, elapsed, reservedBefore, sittingBefore, result, outcomes });
                Check("resident_bed_action", windowValid && (rested || refused) && TradeSystem.GetDrams(actor) == restDrams);
            }
            finally { Diag.SetChannel("furniture", oldFurniture); }
            Check("resident_ordinary_finish", State() == "Normal" && actor.ID == actorId && actor.GetStatValue("Hitpoints") > 0
                && actor.GetStat("Hitpoints").Max == 40 && !DevMode.Enabled && !actor.HasPart<BitLockerPart>());
            yield return Capture("residents-07-kitchen-after-actions");
        }
        private static int ResidentUnits(Entity actor, string blueprint) => actor.GetPart<InventoryPart>().Objects
            .Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);

        private IEnumerator ResidentNearbyReport(Entity resident)
        {
            var actor = _input.PlayerEntity;
            var before = SpreadDiscoveryNotes.Read(actor).ToArray();
            int cached = BiomeManager.CachedZoneCount, tick = _input.TurnManager.TickCount;
            int drams = TradeSystem.GetDrams(actor);
            yield return ResidentWorldAction(resident, "Chat");
            Require(State() == "DialogueOpen" && ConversationManager.Speaker == resident, "actual resident directions dialogue");
            int nearby = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == "Nearby");
            Require(nearby >= 0, "authored nearby-places question");
            yield return ResidentDialogueChoice(nearby);
            Require(ConversationManager.CurrentNode?.ID == "Nearby", "native choice reaches nearby node");
            var reportChoices = ConversationManager.VisibleChoices.Select((c, i) => (choice: c, index: i))
                .Where(p => p.choice.Actions?.Any(a => a.Key == SpreadDiscoveryReports.ActionName) == true).ToArray();
            Require(reportChoices.Length > 0 && reportChoices.Length <= 2, "one or two bounded historical reports");
            Require(reportChoices.All(p => p.choice.Text.Contains("unconfirmed"))
                && SpreadDiscoveryNotes.Read(actor).SequenceEqual(before), "hearing reports alone does not grant notes");
            yield return ResidentDialogueChoice(reportChoices[0].index);
            var after = SpreadDiscoveryNotes.Read(actor).ToArray();
            var added = after.Except(before).ToArray();
            Check("resident_remembered_nearby", added.Length == 1 && after.Length == before.Length + 1
                && added[0].Contains("unconfirmed when heard") && added[0].Contains(resident.GetPart<RenderPart>().DisplayName)
                && BiomeManager.CachedZoneCount == cached && TradeSystem.GetDrams(actor) == drams
                && _input.TurnManager.TickCount == tick);
            Require(added.Length == 1, "exact note written by actual conversation action");
            string note = added[0];
            yield return ResidentCloseMenus();
            yield return Tap(Key.Q); Require(State() == "QuestLogOpen" && _input.QuestLogUI.IsOpen, "native Q opens journal");
            Require(!_input.QuestLogUI.NotesVisible, "journal starts on quest page");
            yield return Tap(Key.Tab);
            var lines = ((IList)Field(_input.QuestLogUI, "_noteLines")).Cast<string>().ToArray();
            string lead = note.Substring(0, note.IndexOf('.') + 1);
            int first = Array.FindIndex(lines, line => line.StartsWith(lead, StringComparison.Ordinal));
            Require(first >= 0, "journal projects exact newly heard place");
            int page = first / 34;
            for (int i = 0; _input.QuestLogUI.NotesPage < page; i++)
            { Require(i < 8, "bounded native notes navigation"); yield return Tap(Key.PageDown); }
            string joined = string.Join(" ", lines);
            Check("resident_read_travel_note", _input.QuestLogUI.NotesVisible && _input.QuestLogUI.NotesPage == page
                && joined.Contains(note) && SpreadDiscoveryNotes.Read(actor).SequenceEqual(after)
                && BiomeManager.CachedZoneCount == cached && _input.TurnManager.TickCount == tick);
            yield return Capture("residents-01b-remembered-field-directions");
            yield return ResidentCloseMenus();
        }
        private IEnumerator ResidentDialogueChoice(int index)
        {
            Require(State() == "DialogueOpen" && index >= 0 && index < ConversationManager.VisibleChoices.Count,
                "actual bounded dialogue choice");
            if ((bool)Field(_input.DialogueUI, "_revealing")) yield return Tap(Key.Enter);
            for (int i = 0; (int)Field(_input.DialogueUI, "_cursorIndex") != index; i++)
            { Require(i < 20, "bounded dialogue cursor"); yield return Tap((int)Field(_input.DialogueUI, "_cursorIndex") < index ? Key.DownArrow : Key.UpArrow); }
            yield return Tap(Key.Enter);
        }

        private IEnumerator ResidentTrade(Entity resident, string blueprint, string check)
        {
            yield return ResidentWorldAction(resident, "Chat");
            Require(State() == "DialogueOpen" && ConversationManager.Speaker == resident, "actual resident dialogue panel");
            if ((bool)Field(_input.DialogueUI, "_revealing")) yield return Tap(Key.Enter);
            int choice = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Actions?.Any(a => a.Key == "StartTrade") == true);
            Require(choice >= 0, "ordinary conversation offers trade");
            for (int i = 0; (int)Field(_input.DialogueUI, "_cursorIndex") != choice; i++)
            { Require(i < 20, "bounded dialogue navigation"); yield return Tap((int)Field(_input.DialogueUI, "_cursorIndex") < choice ? Key.DownArrow : Key.UpArrow); }
            yield return Tap(Key.Enter); Require(State() == "TradeOpen", "actual native trade panel");
            var rows = ((IList)Field(_input.TradeUI, "_leftRows")).Cast<object>().ToArray();
            int row = Array.FindIndex(rows, r => ((Entity)Field(r, "Item")).BlueprintName == blueprint); Require(row >= 0, "actual opening stock row " + blueprint);
            var item = (Entity)Field(rows[row], "Item"); int units = item.GetPart<StackerPart>()?.StackCount ?? 1;
            int price = (int)Field(rows[row], "Price"), drams = TradeSystem.GetDrams(_input.PlayerEntity), sellerDrams = TradeSystem.GetDrams(resident);
            int before = ResidentUnits(_input.PlayerEntity, blueprint), shelf = ResidentUnits(resident, blueprint);
            Require(price > 0 && drams >= price, "real existing purse affords selected stock");
            for (int i = 0; (int)Field(_input.TradeUI, "_leftCursor") != row; i++)
            { Require(i < 20, "bounded trade navigation"); yield return Tap((int)Field(_input.TradeUI, "_leftCursor") < row ? Key.DownArrow : Key.UpArrow); }
            yield return Tap(Key.Enter); Require((bool)Field(_input.TradeUI, "_confirmActive"), "real purchase confirmation");
            yield return Tap(Key.Enter); yield return ResidentCloseMenus();
            Check(check, ResidentUnits(_input.PlayerEntity, blueprint) == before + units && ResidentUnits(resident, blueprint) == shelf - units
                && TradeSystem.GetDrams(_input.PlayerEntity) == drams - price && TradeSystem.GetDrams(resident) == sellerDrams + price);
        }
        private IEnumerator ResidentWorldAction(Entity target, string command)
        {
            var from = Cell(); var to = SpatialQuery.ClosestCell(_input.CurrentZone, target, from.X, from.Y);
            Require(to != null && SpatialQuery.Distance(_input.CurrentZone, _input.PlayerEntity, target) <= 1, "actual resident world owner in reach");
            yield return Tap(Key.C); Require(State() == "AwaitingTalkDirection", "native interaction direction");
            yield return Tap(to.X == from.X && to.Y == from.Y ? Key.Period : Direction(to.X - from.X, to.Y - from.Y));
            Require(State() == "WorldActionMenuOpen", "native owner menu");
            if (_input.WorldActionMenuUI.SelectedCellIsPile) yield return BiomeMenuAction(WorldInteractionSystem.PickCellCommand);
            if (!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, target)
                || ((IList)Field(_input.WorldActionMenuUI, "_actions")).Cast<InventoryAction>().Any(a => a.Command == WorldInteractionSystem.PickTargetCommandPrefix + target.ID))
                yield return BiomeMenuAction(WorldInteractionSystem.PickTargetCommandPrefix + target.ID);
            Require(ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, target), "exact actual resident owner");
            yield return BiomeMenuAction(command);
        }
        private IEnumerator ResidentCloseMenus()
        {
            for (int i = 0; State() != "Normal" && i < 8; i++) yield return Tap(Key.Escape);
            Require(State() == "Normal", "resident menus close through ordinary input");
        }
    }
}
