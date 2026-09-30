using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        private bool _curationOnly, _curationQuarantine;
        private static readonly string[] CurationPublicChecks = {
            "curation_generated_receiving", "curation_public_conversation", "curation_swapped_refusal",
            "curation_first_haul", "curation_second_haul", "curation_certification", "curation_repeat_refusal",
            "curation_finite_supply", "curation_read_record", "curation_equipped_rake", "curation_native_checkpoint",
            "curation_closed_quarantine", "curation_public_finish" };
        private static readonly string[] CurationCombatChecks = {
            "curation_voluntary_open", "curation_actual_player_attack", "curation_actual_player_damage",
            "curation_actual_hostile_attempt", "curation_actual_attack_pose", "curation_actual_defeat", "curation_optional_finish" };
        private bool CurationPublicComplete => CurationPublicChecks.All(n => _audit.Count(a => a == "PASS " + n) == 1)
            && _biomeShortcuts == 1 && _screenshots.Count >= 7;
        private bool CurationComplete => CurationPublicComplete && (!_curationQuarantine
            || CurationCombatChecks.All(n => _audit.Count(a => a == "PASS " + n) == 1));
        private const string CurationCanVerify = "Isolated seed64 ordinary player in real generated Marrowstye. Native local dialogue/reading, two exact bodies hauled to their bound bays, physical one-time certification, key/cabinet/finite supplies, earned rake equipment and F5/F6 graph replacement. Closed quarantine is observed during actual NPC turns. Optional mode separately opens the real gate and records exact player/target combat and animation.";
        private const string CurationCannotVerify = "One disclosed player travel shortcut; all local movement and actions use native keys with NPC scheduling active. No cargo placement, item grants, strength/HP/time/RNG changes or direct success calls. Public completion is reported separately from optional combat. After opening quarantine, at most eight ordinary wait inputs observe an actual hostile attempt before the player attacks. Optional fight may use at most two original starter tonics and one ready original Rime Grip after that observed hostile attempt. Scripted route does not prove natural discovery, universal balance or subjective readability; screenshots need inspection. Existing cached Marrowstye is not rebuilt.";
        private Entity CurationOwner(string blueprint)
        {
            var rows = _input.CurrentZone.GetReadOnlyEntities().Where(e => e.BlueprintName == blueprint).ToArray();
            Require(rows.Length == 1, "exact current Curation owner " + blueprint); return rows[0];
        }
        private Part CurationAuthority()
        {
            var authority = CurationOwner("CurationIntakeIndex").Parts.SingleOrDefault(p => p.Name == "CurationIntake");
            Require(authority != null && (bool)Field(authority, "Configured"), "configured actual receiving authority"); return authority;
        }
        private static Entity CurationBound(Part authority, string name) => (Entity)Field(authority, name);
        private IEnumerator CurationApproach(Entity owner)
        {
            var approach = SpecialistApproach(_input.CurrentZone, owner); Require(approach != null, "actual open Curation frontage " + owner.BlueprintName);
            yield return WalkTo(approach.X, approach.Y);
        }
        private IEnumerator CurationExamine(Entity owner, string filename)
        {
            yield return CurationApproach(owner); yield return ResidentWorldAction(owner, "Examine");
            Require(State() == "AnnouncementOpen" && _input.AnnouncementUI.IsOpen, "native exact-owner examine reader");
            string text = (string)Field(_input.AnnouncementUI, "_message"); Require(!string.IsNullOrWhiteSpace(text), "real authored description");
            _descriptions.Add(new Description { subject = owner.GetDisplayName(), source = owner.ID, text = text });
            yield return Capture(filename); yield return ResidentCloseMenus();
        }
        private IEnumerator RunCurationAudit()
        {
            var actor = _input.PlayerEntity;
            Require(BiomeManager != null && BiomeManager.WorldSeed == 64 && actor.GetStatValue("Strength") == 18,
                "ordinary seed64 actor can haul ninety-weight cargo");
            var yard = BiomeManager.GetZone("Overworld.12.12.0");
            var index = yard.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "CurationIntakeIndex");
            Require(index != null, "actual cold-generated Marrowstye receiving index");
            var arrival = SpecialistApproach(yard, index); Require(arrival != null, "unchanged safe receiving frontage");
            yield return BiomeTravel(yard, arrival, "real Marrowstye receiving yard; original staff, cargo, locks and stocks unchanged");
            var authority = CurationAuthority();
            var first = CurationBound(authority, "FirstBody"); var second = CurationBound(authority, "SecondBody");
            var firstBay = CurationBound(authority, "FirstBay"); var secondBay = CurationBound(authority, "SecondBay");
            var foil = CurationBound(authority, "Counterfoil");
            var filer = CurationBound(authority, "Filer");
            string foilID = foil.ID, actorID = actor.ID;
            int startingDrams = TradeSystem.GetDrams(actor), startingRep = PlayerReputation.Get("PaleCuration");
            var subjectIDs = new[] { first.ID, second.ID };
            Require(index.GetPart<InventoryPart>().Objects.Contains(foil) && foil.GetPart<PhysicsPart>().InInventory == index,
                "physical counterfoil begins in exact index inventory");
            Check("curation_generated_receiving", first != second && firstBay != secondBay
                && yard.GetReadOnlyEntities().Count(e => e.BlueprintName == "SaltCuredBody") == 2
                && new[] { first, second }.All(e => e.BlueprintName == "SaltCuredBody" && !e.HasTag("Creature")
                    && e.GetPart<HandlingPart>()?.Weight == 90 && DragRules.CanDrag(actor, e) == DragVerdict.Ok)
                && !(bool)Field(authority, "Certified") && BiomeDrawn(index));
            yield return Capture("curation-01-generated-receiving-hall");
            yield return CurationApproach(filer); yield return ResidentWorldAction(filer, "Chat");
            Require(State() == "DialogueOpen" && ConversationManager.Speaker == filer, "actual local filer dialogue");
            int branch = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target != "End" && c.Target != "Start"
                && !string.IsNullOrEmpty(c.Target) && (c.Actions == null || c.Actions.Count == 0));
            Require(branch >= 0, "public local explanation without quest acceptance"); yield return ResidentDialogueChoice(branch);
            if ((bool)Field(_input.DialogueUI, "_revealing")) yield return Tap(Key.Enter);
            Check("curation_public_conversation", State() == "DialogueOpen" && ConversationManager.CurrentText.Length > 50
                && TradeSystem.GetDrams(actor) == startingDrams && PlayerReputation.Get("PaleCuration") == startingRep);
            _descriptions.Add(new Description { subject = "Local filer conversation", source = filer.ID, text = ConversationManager.CurrentText });
            yield return Capture("curation-02-filer-explains-intake"); yield return ResidentCloseMenus();
            yield return CurationExamine(first, "curation-03-first-subject-label");
            yield return CurationExamine(second, "curation-04-second-subject-label");
            yield return CurationExamine(firstBay, "curation-05-first-receiving-bay");
            yield return CurationExamine(secondBay, "curation-06-second-receiving-bay");
            yield return CurationApproach(index); yield return ResidentWorldAction(index, "CertifyCurationIntake"); yield return ResidentCloseMenus();
            Check("curation_swapped_refusal", !(bool)Field(authority, "Certified")
                && index.GetPart<InventoryPart>().Objects.Contains(foil) && !actor.GetPart<InventoryPart>().Objects.Contains(foil));
            yield return CurationHaul(first, firstBay, "curation_first_haul");
            yield return CurationHaul(second, secondBay, "curation_second_haul");
            yield return Capture("curation-07-both-subjects-filed");
            yield return CurationApproach(index); yield return ResidentWorldAction(index, "CertifyCurationIntake"); yield return ResidentCloseMenus();
            Check("curation_certification", (bool)Field(authority, "Certified") && actor.GetPart<InventoryPart>().Objects.Contains(foil)
                && !index.GetPart<InventoryPart>().Objects.Contains(foil) && foil.GetPart<PhysicsPart>().InInventory == actor
                && TradeSystem.GetDrams(actor) == startingDrams && PlayerReputation.Get("PaleCuration") == startingRep);
            yield return ResidentWorldAction(index, "CertifyCurationIntake"); yield return ResidentCloseMenus();
            Check("curation_repeat_refusal", (bool)Field(authority, "Certified")
                && actor.GetPart<InventoryPart>().Objects.Count(e => e.BlueprintName == "CurationCounterfoil") == 1
                && actor.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "CurationCounterfoil").ID == foilID);
            yield return Capture("curation-08-physical-counterfoil-issued-once");
            var cabinet = CurationOwner("CurationToolCabinet"); yield return CurationApproach(cabinet);
            var stock = cabinet.GetPart<ContainerPart>().Contents.ToArray();
            Require(cabinet.GetPart<LockPart>().IsLocked && !cabinet.GetPart<ContainerPart>().Locked && stock.Length == 4,
                "real locked finite supply cabinet before native use");
            yield return ResidentWorldAction(cabinet, "Unlock"); yield return ResidentCloseMenus();
            Require(!cabinet.GetPart<LockPart>().IsLocked, "physical carried foil opens matching lock");
            yield return ResidentWorldAction(cabinet, "OpenContainer"); Require(State() == "PickupOpen", "actual cabinet inventory panel");
            yield return Capture("curation-09-open-supply-kit"); yield return Tap(Key.Tab); yield return ResidentCloseMenus();
            Check("curation_finite_supply", cabinet.GetPart<ContainerPart>().Contents.Count == 0
                && stock.All(e => actor.GetPart<InventoryPart>().Objects.Contains(e) && e.GetPart<PhysicsPart>().InInventory == actor)
                && actor.GetPart<InventoryPart>().Objects.Contains(foil));
            var document = stock.Single(e => e.BlueprintName == "CurationDiscrepancyReport");
            yield return ItemAction(document, "ReadDocument");
            Require(State() == "AnnouncementOpen" && _input.AnnouncementUI.IsOpen, "actual carried record reader");
            var entry = ReadableDocumentCatalog.Get(document.GetPart<ReadableDocumentPart>().DocumentId);
            Check("curation_read_record", entry != null && (string)Field(_input.AnnouncementUI, "_message") == entry.Title + "\n\n" + entry.Text);
            yield return Capture("curation-10-discrepancy-record-reader"); yield return ResidentCloseMenus();
            var rake = stock.Single(e => e.BlueprintName == "CurationSaltRake");
            yield return ItemAction(rake, "equip_auto"); yield return ResidentCloseMenus();
            Check("curation_equipped_rake", rake.GetPart<PhysicsPart>().Equipped == actor && rake.HasTag("Cudgel"));
            yield return CurationCheckpoint(foilID, subjectIDs, stock.Select(e => e.ID).ToArray(), rake.ID);
            actor = _input.PlayerEntity; authority = CurationAuthority();
            yield return CurationClosedQuarantine();
            Check("curation_public_finish", State() == "Normal" && actor.ID == actorID && actor.GetStatValue("Hitpoints") > 0
                && actor.GetStat("Hitpoints").Max == 40 && TradeSystem.GetDrams(actor) == startingDrams
                && PlayerReputation.Get("PaleCuration") == startingRep && !DevMode.Enabled && !actor.HasPart<BitLockerPart>());
            WriteReport();
            if (_curationQuarantine) { Require(CurationPublicComplete, "public route independently complete before voluntary release"); yield return RunCurationQuarantine(); }
        }
        private IEnumerator CurationHaul(Entity body, Entity bay, string check)
        {
            yield return CurationApproach(body); yield return ResidentWorldAction(body, HandlingPart.HaulCommand); yield return ResidentCloseMenus();
            Require(DragSystem.GetDragged(_input.PlayerEntity) == body, "native grip on exact original preserved subject");
            var destination = _input.CurrentZone.GetEntityCell(bay); Require(destination != null, "actual bound bay cell");
            yield return WalkTo(destination.X, destination.Y);
            Require(DragSystem.GetDragged(_input.PlayerEntity) == body && Cell() == destination, "player reaches bay while hauling");
            var beyond = new[] { (0,-1), (1,0), (-1,0), (0,1) }.Select(d => _input.CurrentZone.GetCell(destination.X + d.Item1, destination.Y + d.Item2))
                .FirstOrDefault(c => BiomeSafe(_input.CurrentZone, c, 0));
            Require(beyond != null, "real free step beyond bay for trailing cargo");
            yield return Tap(Direction(beyond.X - destination.X, beyond.Y - destination.Y));
            Require(Cell() == beyond && _input.CurrentZone.GetEntityCell(body) == destination, "real paid movement pulls body into vacated bay");
            yield return ResidentWorldAction(body, HandlingPart.ReleaseCommand); yield return ResidentCloseMenus();
            Check(check, _input.CurrentZone.GetEntityCell(body) == destination && !DragSystem.IsDragging(_input.PlayerEntity)
                && !body.HasPart<DraggedPart>() && body.GetPart<HandlingPart>().Weight == 90 && BiomeDrawn(body));
        }
        private IEnumerator CurationCheckpoint(string foilID, string[] subjects, string[] stock, string rakeID)
        {
            var before = _input.PlayerEntity; var at = Cell(); string gear = BiomeItemCollection(before);
            var poses = subjects.ToDictionary(id => id, id => _input.CurrentZone.GetEntityPosition(_input.CurrentZone.GetReadOnlyEntities().Single(e => e.ID == id)));
            var info = SaveGameService.GetSaveInfo("Quick"); Require(info != null, "isolated ordinary checkpoint");
            string path = Path.Combine(_ownedRoot, info.GameID, "Quick.sav.gz"), previous = CheckpointHash(path);
            int tick = _input.TurnManager.TickCount, energy = _input.TurnManager.GetEnergy(before), hp = before.GetStatValue("Hitpoints"), drams = TradeSystem.GetDrams(before);
            yield return Tap(Key.F5); string saved = CheckpointHash(path);
            Require(saved != previous && SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID == "Overworld.12.12.0", "native F5 saves receiving aftermath");
            var next = new[] { (1,0), (-1,0), (0,1), (0,-1) }.Select(d => _input.CurrentZone.GetCell(at.X + d.Item1, at.Y + d.Item2))
                .FirstOrDefault(c => BiomeSafe(_input.CurrentZone, c, 0)); Require(next != null, "actual safe post-save step");
            yield return Tap(Direction(next.X - at.X, next.Y - at.Y)); Require(Cell() == next && CheckpointHash(path) == saved, "real post-save mutation");
            yield return Tap(Key.F6); double began = Time.realtimeSinceStartupAsDouble;
            while (ReferenceEquals(before, _input.PlayerEntity)) { Require(Time.realtimeSinceStartupAsDouble - began < 8, "native receiving graph replacement"); yield return null; }
            var actor = _input.PlayerEntity; var authority = CurationAuthority(); var inventory = actor.GetPart<InventoryPart>();
            bool graph = actor.ID == before.ID && Cell().X == at.X && Cell().Y == at.Y && (bool)Field(authority, "Certified")
                && poses.All(p => _input.CurrentZone.GetEntityPosition(_input.CurrentZone.GetReadOnlyEntities().Single(e => e.ID == p.Key)) == p.Value)
                && CurationOwner("CurationToolCabinet").GetPart<ContainerPart>().Contents.Count == 0
                && !CurationOwner("CurationToolCabinet").GetPart<LockPart>().IsLocked
                && CurationOwner("CurationQuarantineGate").GetPart<LockPart>().IsLocked
                && inventory.Objects.Count(e => e.ID == foilID) == 1 && stock.All(id => BiomeOwnedItems(actor).Count(e => e.ID == id) == 1)
                && inventory.GetAllEquipped().Any(e => e.ID == rakeID) && BiomeItemCollection(actor) == gear
                && _input.TurnManager.TickCount == tick && _input.TurnManager.GetEnergy(actor) == energy && actor.GetStatValue("Hitpoints") == hp
                && TradeSystem.GetDrams(actor) == drams && CheckpointHash(path) == saved;
            yield return CurationApproach(CurationOwner("CurationIntakeIndex"));
            yield return ResidentWorldAction(CurationOwner("CurationIntakeIndex"), "CertifyCurationIntake"); yield return ResidentCloseMenus();
            Check("curation_native_checkpoint", graph && inventory.Objects.Count(e => e.BlueprintName == "CurationCounterfoil") == 1
                && CurationOwner("CurationToolCabinet").GetPart<ContainerPart>().Contents.Count == 0);
            _descriptions.Add(new Description { subject = "Native receiving save replacement", source = info.GameID,
                text = "F5, verified keyboard step, F6; unchanged saved bytes " + saved + "; same body, key, supply and equipment IDs; certification retained." });
            yield return Capture("curation-11-restored-filed-subjects");
        }
        private IEnumerator CurationClosedQuarantine()
        {
            var gate = CurationOwner("CurationQuarantineGate"); var enemy = CurationOwner("CurationHalfSet");
            yield return CurationExamine(gate, "curation-12-quarantine-warning");
            var cage = _input.CurrentZone.GetReadOnlyEntities().Where(e => e.BlueprintName == "CurationQuarantineRail" || e == gate)
                .Select(e => _input.CurrentZone.GetEntityCell(e)).ToArray();
            Require(cage.Length >= 4 && cage.All(c => c != null), "actual closed physical cage owners");
            int left = cage.Min(c => c.X), right = cage.Max(c => c.X), top = cage.Min(c => c.Y), bottom = cage.Max(c => c.Y);
            var staff = _input.CurrentZone.GetReadOnlyEntities().Where(e => e.BlueprintName == "FilerClerk"
                || e.BlueprintName == "CurationIntakeFiler" || e.BlueprintName == "CurationJuniorIndexer").ToArray();
            Require(staff.Length == 3 && _input.TurnManager.IsRegistered(enemy) && enemy.GetPart<BrainPart>().CurrentZone == _input.CurrentZone,
                "real scheduled half-set and original public staff");
            var health = staff.ToDictionary(e => e.ID, e => e.GetStatValue("Hitpoints"));
            bool oldVerbose = Diag.IsChannelEnabled("turn-verbose"); Diag.SetChannel("turn-verbose", true);
            int unsolicitedDoorRefusals = 0;
            Action<string> observeDoorRefusal = message => { if (message.Contains("That door cannot be used right now.")) unsolicitedDoorRefusals++; };
            MessageLog.OnMessage += observeDoorRefusal;
            try
            {
                Diag.Record("scenario", "CurationClosedWait", _input.PlayerEntity, enemy, new { runId = RunId });
                var marker = Diag.Snapshot(1).Single(); int before = _input.TurnManager.TickCount; bool contained = true;
                for (int i = 0; i < 12; i++)
                {
                    yield return Tap(Key.Period); yield return CombatWaitForFx();
                    var cell = _input.CurrentZone.GetEntityCell(enemy);
                    contained &= cell != null && cell.X > left && cell.X < right && cell.Y > top && cell.Y < bottom
                        && gate.GetPart<LockPart>().IsLocked && gate.GetPart<DoorPart>().IsClosed
                        && staff.All(e => _input.CurrentZone.GetEntityCell(e) != null && e.GetStatValue("Hitpoints") == health[e.ID]);
                }
                var records = Diag.Snapshot(Diag.BufferCapacity); bool markerRetained = records.Any(r => r.TraceId == marker.TraceId);
                var turns = records.SkipWhile(r => r.TraceId != marker.TraceId).Skip(1).Where(r => r.Category == "turn-verbose" && r.ActorId == enemy.ID && r.Kind == "End").ToArray();
                Check("curation_closed_quarantine", contained && markerRetained && turns.Length > 0 && _input.TurnManager.TickCount > before && unsolicitedDoorRefusals == 0);
                _descriptions.Add(new Description { subject = "Closed quarantine, actual NPC scheduling", source = enemy.ID,
                    text = "Twelve ordinary wait inputs; exact hostile End records=" + turns.Length + "; contained=" + contained + "; original three staff HP unchanged; unsolicited player-directed door refusals=" + unsolicitedDoorRefusals + "." });
            }
            finally { MessageLog.OnMessage -= observeDoorRefusal; Diag.SetChannel("turn-verbose", oldVerbose); }
            yield return Capture("curation-13-closed-optional-quarantine");
        }
        private IEnumerator RunCurationQuarantine()
        {
            var actor = _input.PlayerEntity; var gate = CurationOwner("CurationQuarantineGate");
            _stagedZone = _input.CurrentZone; _combatTarget = CurationOwner("CurationHalfSet"); _combatTargetID = _combatTarget.ID;
            Require(_combatTarget.GetStatValue("Hitpoints") > 0 && gate.GetPart<LockPart>().IsLocked, "same unopened quarantine before voluntary choice");
            foreach (var item in actor.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == "HealingTonic")) _combatStartingTonics.Add(item.ID, item);
            for (int slot = 0; slot < ActivatedAbilitiesPart.SlotCount; slot++)
            { var ability = actor.GetPart<ActivatedAbilitiesPart>()?.GetAbilityBySlot(slot); if (ability?.Command == "CommandRimeGrip") _combatStartingRime = ability; }
            Require(actor.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == "CurationInspectionKey"), "earned real inspection key retained");
            yield return CurationApproach(gate); yield return ResidentWorldAction(gate, "Unlock"); yield return ResidentCloseMenus();
            if (gate.GetPart<DoorPart>().IsClosed) { yield return ResidentWorldAction(gate, DoorPart.OpenCommand); yield return ResidentCloseMenus(); }
            var gateCell = _input.CurrentZone.GetEntityCell(gate);
            bool unobstructed = gateCell.Occupants.All(e => e == actor || !e.HasTag("Creature"));
            Check("curation_voluntary_open", !gate.GetPart<LockPart>().IsLocked && gate.GetPart<DoorPart>().IsOpen
                && !gate.GetPart<DoorPart>().IsClosed && (!unobstructed || !gateCell.BlocksMovement(actor)));
            yield return Capture("curation-14-voluntarily-opened-quarantine");
            int waits = 0;
            for (; waits < 8 && !_combatResponse; waits++)
            {
                int tick = _input.TurnManager.TickCount, energy = _input.TurnManager.GetEnergy(actor);
                int speed = actor.GetStatValue("Speed", TurnManager.DefaultSpeed);
                yield return CombatKey(Key.Period, false);
                Require(_input.TurnManager.TickCount >= tick && _input.TurnManager.GetEnergy(actor)
                    == energy - TurnManager.ActionThreshold + (_input.TurnManager.TickCount - tick) * speed,
                    "one paid ordinary wait while observing the released hostile");
            }
            _descriptions.Add(new Description { subject = "Released half-set before the player attacks", source = _combatTargetID,
                text = "Ordinary wait inputs=" + waits + "; exact hostile HitRoll witnessed=" + _combatResponse
                    + "; player attacks=" + _combatAttacks + "; player HP=" + actor.GetStatValue("Hitpoints")
                    + "; target HP=" + _combatTarget.GetStatValue("Hitpoints") + ". No combat outcomes or positions were set." });
            Require(_combatResponse && _combatAttacks == 0, "released half-set attempts actual melee within eight ordinary waits before player attack");
            for (int step = 0; !CombatSystem.IsDeathHandled(_combatTarget); step++)
            {
                Require(step < 40, "finite forty-input optional fight"); CombatRequireLiveTarget();
                yield return CombatUseStartingSupport();
                if (CombatSystem.IsDeathHandled(_combatTarget)) break;
                if (SpatialQuery.Distance(_stagedZone, actor, _combatTarget) > 1)
                {
                    var path = CombatRoute(); Require(path != null && path.Count > 0, "actual reachable half-set after voluntary opening");
                    var from = Cell(); yield return CombatKey(Direction(path[0].X - from.X, path[0].Y - from.Y), false); _combatMoves++;
                }
                else
                {
                    var from = Cell(); var to = SpatialQuery.ClosestCell(_stagedZone, _combatTarget, from.X, from.Y);
                    yield return CombatKey(Direction(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y)), true); _combatAttacks++;
                }
            }
            yield return CombatDismissEarnedAdvancement();
            Check("curation_actual_player_attack", _combatAttempt && _combatAttacks > 0);
            Check("curation_actual_player_damage", _combatDamage); Check("curation_actual_hostile_attempt", _combatResponse);
            Check("curation_actual_attack_pose", _combatPoseObserved && _combatPoseCaptured);
            Check("curation_actual_defeat", _combatLethal && CombatSystem.IsDeathHandled(_combatTarget) && _stagedZone.GetEntityCell(_combatTarget) == null);
            Check("curation_optional_finish", State() == "Normal" && actor.GetStatValue("Hitpoints") > 0
                && actor.GetStat("Hitpoints").Max == 40 && !DebugInvincibility.IsEnabled(actor) && !DevMode.Enabled && !actor.HasPart<BitLockerPart>());
            yield return Capture("curation-15-actual-quarantine-aftermath");
        }
    }
}
