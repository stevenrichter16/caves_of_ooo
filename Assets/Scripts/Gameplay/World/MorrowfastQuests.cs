using System;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Core
{
    /// <summary>Local, optional errands. Evidence comes from owned native entities; persisted state uses normal properties and the journal.</summary>
    public static class MorrowfastQuests
    {
        public const string ReturnQuestId = "MorrowfastReturnNotch", BellQuestId = "MorrowfastBell", SupperQuestId = "MorrowfastSupper";
        public const string InspectCordCommand = "MorrowfastInspectCord", RepairBellCommand = "MorrowfastRepairBell", LoudBellCommand = "MorrowfastLoudBell", RingBellCommand = "MorrowfastRingBell", MoveStoolCommand = "MorrowfastMoveSupperStool", RestCommand = "MorrowfastRestGuestBed";
        public const string EddenConsent = "MorrowfastEddenConsent", BellDiagnosed = "MorrowfastBellDiagnosed", BellWorked = "MorrowfastBellWorked", SupperMoved = "MorrowfastSupperMoved";
        private const string ZoneId = "Overworld.3.6.0";

        public static void AttachProp(Entity owner, string componentId)
        {
            if (owner == null || (componentId != "rope-reserve-coil" && componentId != "north-oath-arch" && componentId != "guest-stool-south" && componentId != "guest-bed-west" && componentId != "guest-bed-east")) return;
            var part = owner.GetPart<MorrowfastQuestPropPart>();
            if (part == null) { part = new MorrowfastQuestPropPart(); owner.AddPart(part); }
            part.ComponentId = componentId;
        }
        public static bool IsWorldCommand(string command) => command == InspectCordCommand || command == RepairBellCommand || command == LoudBellCommand || command == RingBellCommand || command == MoveStoolCommand || command == RestCommand;

        private static bool Present(Entity target, Entity actor, Zone zone)
        {
            if (zone == null || zone.ZoneID != ZoneId || !MorrowfastSceneRuntime.IsActive(zone) || SettlementRuntime.ActiveZone != zone || actor == null || target == null
                || !actor.HasTag("Player") || actor.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(actor)
                || CombatSystem.IsDeathHandled(target) || (target.GetStat("Hitpoints") != null && target.GetStatValue("Hitpoints") <= 0)
                || (StoryletPart.LocalPlayer != null && StoryletPart.LocalPlayer != actor)) return false;
            var ac = zone.GetEntityCell(actor); var tc = zone.GetEntityCell(target);
            return ac != null && tc != null && ac.Objects.Contains(actor) && tc.Objects.Contains(target);
        }
        private static bool ResidentPresent(Entity speaker, Entity actor, out string resident)
        {
            resident = speaker?.GetPart<MorrowfastResidentPart>()?.ResidentId;
            var zone = SettlementRuntime.ActiveZone;
            if (string.IsNullOrEmpty(resident) || !Present(speaker, actor, zone) || speaker.ID != "morrowfast-owner:" + resident
                || MorrowfastSceneRuntime.FindOwner(zone, resident) != speaker
                || speaker.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(speaker)
                || FactionManager.IsHostile(speaker, actor)) return false;
            var sc = zone.GetEntityCell(speaker); var pc = zone.GetEntityCell(actor);
            return Math.Max(Math.Abs(sc.X - pc.X), Math.Abs(sc.Y - pc.Y)) <= 1;
        }
        private static bool Active(string quest) => StoryletPart.Current?.IsQuestActive(quest) == true;
        private static bool Available(string quest) => StoryletPart.Current != null && !Active(quest) && !StoryletPart.Current.IsQuestCompleted(quest);
        private static bool Flag(Entity actor, string key) => actor?.GetIntProperty(key) == 1;

        public static bool CanConversation(Entity speaker, Entity actor, string command)
        {
            if (!ResidentPresent(speaker, actor, out var resident)) return false;
            switch (command)
            {
                case "decline-register": return resident == "north-guard-east" || resident == "north-guard-west";
                case "accept-return": return resident == "north-guard-east" && Available(ReturnQuestId);
                case "eddens-account": return resident == "edden-brack" && Active(ReturnQuestId) && !Flag(actor, EddenConsent);
                case "report-return": return resident == "north-guard-east" && Active(ReturnQuestId) && Flag(actor, EddenConsent);
                case "refuse-return": return resident == "north-guard-east" && Active(ReturnQuestId);
                case "accept-bell": return resident == "north-guard-west" && Available(BellQuestId);
                case "report-bell": return resident == "north-guard-west" && Active(BellQuestId) && Flag(actor, BellWorked);
                case "refuse-bell": return resident == "north-guard-west" && Active(BellQuestId);
                case "accept-supper": return resident == "farra-sprig" && Available(SupperQuestId);
                case "report-supper": return resident == "farra-sprig" && Active(SupperQuestId) && Flag(actor, SupperMoved);
                case "refuse-supper": return resident == "farra-sprig" && Active(SupperQuestId);
                case "consent-record": return resident == "east-robed-resident";
                case "withdraw-record": return resident == "east-robed-resident" && Flag(actor, "MorrowfastRecordConsent");
                default: return false;
            }
        }
        public static bool TryConversation(Entity speaker, Entity actor, string command)
        {
            if (!CanConversation(speaker, actor, command)) return false;
            switch (command)
            {
                case "decline-register": MessageLog.Add("The watchkeeper steps aside. The northern arch is open; no name, payment, or favor is owed."); return true;
                case "accept-return": Start(ReturnQuestId); if (Flag(actor, EddenConsent)) Finish(ReturnQuestId, "account", actor); MessageLog.Add("Ask Edden, here in Morrowfast, whether he wants Hesta told he is safe. Bring back only what he permits."); return true;
                case "eddens-account":
                    actor.SetIntProperty(EddenConsent, 1); Finish(ReturnQuestId, "account", actor);
                    MessageLog.Add("Edden: 'Tell her I am safe. That is all I am asking you to carry.'"); return true;
                case "report-return": return Complete(ReturnQuestId, actor, "Hesta erases the open notch. 'Safe. I can let that be the whole sentence.'");
                case "refuse-return": return Refuse(ReturnQuestId, actor, "You release the promise. Hesta accepts that Edden's account is his to give.");
                case "accept-bell": Start(BellQuestId); if (Flag(actor, BellDiagnosed)) Finish(BellQuestId, "cord", actor); if (Flag(actor, BellWorked)) Finish(BellQuestId, "bell", actor); MessageLog.Add("Examine the reserve cord inside the southwest rope shop, then work at the northern arch. Fire clay makes a quiet sleeve; leaving the clapper bare costs no material."); return true;
                case "report-bell": return Complete(BellQuestId, actor, "Nemm checks the bell's new voice. The setting remains yours to change at the arch.");
                case "refuse-bell": return Refuse(BellQuestId, actor, "You leave the bell work to the watch. Any work already done remains in place.");
                case "accept-supper": Start(SupperQuestId); if (Flag(actor, SupperMoved)) Finish(SupperQuestId, "stool", actor); MessageLog.Add("Farra asks you to move the spare supper stool inside the western guesthouse, leaving the doorway clear, then return to her."); return true;
                case "report-supper": return Complete(SupperQuestId, actor, "Farra checks the space you left. 'A place can be offered without deciding who must fill it.'");
                case "refuse-supper": return Refuse(SupperQuestId, actor, "You release the supper errand. No place at the common table is conditional on helping.");
                case "consent-record": actor.SetIntProperty("MorrowfastRecordConsent", 1); MessageLog.Add("Vennit records only that you passed Morrowfast, with your permission. You may withdraw it here."); return true;
                case "withdraw-record": actor.SetIntProperty("MorrowfastRecordConsent", 0); MessageLog.Add("Vennit crosses out the entry at your request and leaves no account attributed to you."); return true;
            }
            return false;
        }
        private static void Start(string quest)
        { StoryletPart.Current.StartQuest(new QuestState { QuestId = quest, CurrentStageIndex = 0, EnteredStageAtTurn = TurnManager.Active?.TickCount ?? 0 }); }
        private static void Finish(string quest, string objective, Entity actor) { if (Active(quest)) StoryletPart.Current.FinishObjective(quest, objective, actor); }
        private static bool Complete(string quest, Entity actor, string text)
        {
            if (!StoryletPart.Current.CompleteQuest(quest, actor)) return false;
            MessageLog.Add(text); return true;
        }
        private static bool Refuse(string quest, Entity actor, string text)
        { StoryletPart.Current.RemoveActiveQuest(quest); actor.SetIntProperty(quest + "Refused", 1); if (quest == ReturnQuestId) actor.SetIntProperty(EddenConsent, 0); MessageLog.Add(text); return true; }

        public static bool TryWorldAction(Entity target, Entity actor, Zone zone, string command, out int energyCost)
        {
            energyCost = 0;
            var id = target?.GetPart<MorrowfastQuestPropPart>()?.ComponentId;
            if (!IsWorldCommand(command) || string.IsNullOrEmpty(id) || !Present(target, actor, zone)
                || target.ID != "morrowfast-owner:" + id || MorrowfastSceneRuntime.FindOwner(zone, id) != target
                || !MorrowfastSceneRuntime.WithinOwnerReach(actor, target, zone)) return false;
            var spec = MorrowfastSceneDefinition.Load()?.FindOwner(id);
            if (spec == null || (spec.kind != "stool" && zone.GetEntityPosition(target) != (spec.anchorX, spec.anchorY))) return false;
            if (command == RestCommand)
            {
                if (id != "guest-bed-west" && id != "guest-bed-east") return false;
                return RestSystem.TryRest(actor, zone, "Dry Hem guest bed", out _);
            }
            if (command == InspectCordCommand)
            {
                if (id != "rope-reserve-coil" || !Active(BellQuestId) || Flag(actor, BellDiagnosed)) return false;
                actor.SetIntProperty(BellDiagnosed, 1); Finish(BellQuestId, "cord", actor);
                MessageLog.Add("The spare cord is sound. The arch bell's clapper needs a sleeve of fire clay for a quiet call, or the bare setting for a call that carries across the square.");
            }
            else if (command == RepairBellCommand || command == LoudBellCommand)
            {
                if (id != "north-oath-arch" || !Flag(actor, BellDiagnosed)
                    || (!Active(BellQuestId) && StoryletPart.Current?.IsQuestCompleted(BellQuestId) != true)) return false;
                string mode = command == RepairBellCommand ? "quiet" : "loud";
                if (target.GetProperty("MorrowfastBellMode") == mode) return false;
                if (mode == "quiet")
                {
                    var inv = actor.GetPart<InventoryPart>(); var clay = inv?.FindConsumableByBlueprint("FireClay");
                    if (clay == null || !inv.TryConsumeOne(clay)) { MessageLog.Add("A quiet sleeve needs one measure of fire clay. Orrit stocks it; a bare clapper costs no material."); return false; }
                }
                target.Properties["MorrowfastBellMode"] = mode; actor.SetIntProperty(BellWorked, 1);
                Finish(BellQuestId, "bell", actor);
                var examine = target.GetPart<ExaminablePart>();
                if (examine != null) examine.Text = "An open stone arch with a watch bell. Passage is free. The clapper is set for a " + mode + " call.";
                MessageLog.Add(mode == "quiet" ? "You spend one fire clay to sleeve the clapper. Its low note will reach the two watchkeepers." : "You leave the clapper bare. Its clear note will carry across Morrowfast.");
            }
            else if (command == RingBellCommand)
            {
                if (id != "north-oath-arch") return false;
                bool quiet = target.GetProperty("MorrowfastBellMode") == "quiet";
                foreach (var person in zone.GetAllEntities())
                {
                    var part = person.GetPart<MorrowfastResidentPart>(); if (part == null || person.GetStatValue("Hitpoints") <= 0) continue;
                    if (quiet && part.ResidentId != "north-guard-west" && part.ResidentId != "north-guard-east") continue;
                    var ev = GameEvent.New("MorrowfastBellRung"); person.FireEventAndRelease(ev);
                    var cell = zone.GetEntityCell(person); AsciiFxBus.EmitParticle(zone, cell.X, cell.Y - 1, '!', "&y", 0.4f);
                }
                MessageLog.Add(quiet ? "A low bell note turns the two watchkeepers' heads." : "The bare bell rings across the square. Morrowfast's residents look toward the arch.");
            }
            else if (command == MoveStoolCommand)
            {
                if (id != "guest-stool-south" || !Active(SupperQuestId) || Flag(actor, SupperMoved)) return false;
                var from = zone.GetEntityPosition(target); bool moved = false;
                // Runtime validates same room, actual authored floor, occupancy and reach for each candidate.
                int[] dx = { -1, 1, 0, 0, -1, 1, -1, 1 }, dy = { 0, 0, -1, 1, -1, -1, 1, 1 };
                for (int i = 0; i < dx.Length && !moved; i++) moved = MorrowfastSceneRuntime.TryMoveFurniture(actor, zone, id, from.x + dx[i], from.y + dy[i]);
                if (!moved) { MessageLog.Add("There is no clear space beside the stool. Leave room inside the guesthouse and try again."); return false; }
                actor.SetIntProperty(SupperMoved, 1); Finish(SupperQuestId, "stool", actor);
                MessageLog.Add("You shift the spare supper stool onto clear floor. Its old place and the guesthouse doorway remain free.");
            }
            else return false;
            energyCost = 1000; return true;
        }
    }

    /// <summary>Only supplies native commands. The input boundary calls TryWorldAction and charges successful labor exactly once.</summary>
    public sealed class MorrowfastQuestPropPart : Part
    {
        public override string Name => "MorrowfastQuestProp";
        public string ComponentId = "";
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "GetInventoryActions") return true;
            var actions = e.GetParameter<InventoryActionList>("Actions"); if (actions == null) return true;
            if (ComponentId == "rope-reserve-coil") actions.AddAction("InspectCord", "inspect the watch cord", MorrowfastQuests.InspectCordCommand, 'i', 20);
            if (ComponentId == "north-oath-arch") {
                actions.AddAction("QuietBell", "sleeve the bell (1 fire clay)", MorrowfastQuests.RepairBellCommand, 'q', 21);
                actions.AddAction("LoudBell", "set a bare clapper (free)", MorrowfastQuests.LoudBellCommand, 'l', 20);
                actions.AddAction("RingBell", "ring the watch bell", MorrowfastQuests.RingBellCommand, 'b', 19);
            }
            if (ComponentId == "guest-stool-south") actions.AddAction("MoveSupperStool", "make room for supper", MorrowfastQuests.MoveStoolCommand, 'm', 20);
            if (ComponentId == "guest-bed-west" || ComponentId == "guest-bed-east") actions.AddAction("RestGuest", "rest in the guest bed (free)", MorrowfastQuests.RestCommand, 's', 20);
            return true;
        }
    }
}
