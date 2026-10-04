using System;
using System.Collections;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        private static readonly string[] CurationAnnexChecks = {
            "annex_commitment_and_sidestep", "annex_partial_recovery", "annex_wayfinding_and_materials", "annex_paid_repair", "annex_living_transfer",
            "annex_finite_recovery", "annex_closed_return", "annex_native_checkpoint" };
        private const string CurationAnnexCanVerify = "Real seed64 generated Marrowstye after the complete native public filing route. Keyboard examination, exact finite rack supplies, paid two-timber repair, ordinary door commands, a visible fixed lash, paid sidestep, scheduled recovery, hostile pursuit through the transfer aperture, living containment, split actual recovery stock and F5/F6 graph replacement. Models and actual closed boundaries are observed; screenshots record the native view.";
        private const string CurationAnnexCannotVerify = "The public route uses its one disclosed player travel shortcut; all annex movement and actions use ordinary keys. No item grants, actor/target relocation, damage calls, AI overrides or direct repairs/door changes. The lure and escape have finite bounds and may truthfully fail. No subjective readability, natural discovery, universal balance, offscreen simulation or old-save layout migration claim. The hostile remains a Bloom-occupied deceased subject; containment is not rescue or cure.";

        private IEnumerator RunCurationAnnex()
        {
            var zone = _input.CurrentZone; var actor = _input.PlayerEntity;
            var rack = CurationOwner("CurationMaintenanceRack"); var placard = CurationOwner("CurationAnnexPlacard");
            var service = CurationOwner("CurationServiceGate"); var transfer = CurationOwner("CurationTransferGate");
            var gallery = CurationOwner("CurationGalleryGate"); var enemy = CurationOwner("CurationHalfSet");
            var cabinet = CurationOwner("CurationRecoveryCabinet"); var conservation = CurationOwner("CurationConservationCase"); var inspection = CurationOwner("CurationQuarantineGate");
            var mainCell = zone.GetEntityCell(inspection); int x = mainCell.X - 9, y = mainCell.Y - 2;
            var serviceCell = zone.GetEntityCell(service); var transferCell = zone.GetEntityCell(transfer);
            int subjectHP = enemy.GetStatValue("Hitpoints"); string subjectID = enemy.ID;
            Require(subjectHP > 0 && !CombatSystem.IsDeathHandled(enemy) && _input.TurnManager.IsRegistered(enemy), "actual living scheduled annex threat");
            Require(inspection.GetPart<DoorPart>().IsClosed && transfer.GetPart<DoorPart>().IsClosed && gallery.GetPart<DoorPart>().IsClosed,
                "all gallery boundaries initially closed");
            yield return CurationExamine(placard, "annex-01-actual-layout-placard");
            yield return ResidentWorldAction(placard, "ReadDocument");
            var record = ReadableDocumentCatalog.Get(placard.GetPart<ReadableDocumentPart>().DocumentId);
            Require(State() == "AnnouncementOpen" && _input.AnnouncementUI.IsOpen
                && (string)Field(_input.AnnouncementUI, "_message") == record.Title + "\n\n" + record.Text,
                "native maintenance placard contains the actual route and incomplete intake record");
            yield return Capture("annex-01b-read-maintenance-instructions"); yield return ResidentCloseMenus();
            yield return CurationApproach(rack);
            int timberBefore = ResidentUnits(actor, "SalvagedTimber"); var timber = rack.GetPart<ContainerPart>().Contents.ToArray();
            Require(timber.Length == 1 && timber[0].BlueprintName == "SalvagedTimber" && timber[0].GetPart<StackerPart>().StackCount == 2,
                "actual finite two-length maintenance stock");
            yield return ResidentWorldAction(rack, "OpenContainer"); Require(State() == "PickupOpen", "native maintenance rack contents");
            yield return Tap(Key.Tab); yield return ResidentCloseMenus();
            Check("annex_wayfinding_and_materials", rack.GetPart<ContainerPart>().Contents.Count == 0
                && ResidentUnits(actor, "SalvagedTimber") == timberBefore + 2 && BiomeDrawn(placard) && BiomeDrawn(rack));

            // Explicit public vestibule and screened corridor waypoints avoid opening
            // the occupied gallery just because a generic route finds it shorter.
            yield return AnnexWalkTo(x + 7, y + 7); yield return AnnexWalkTo(serviceCell.X, serviceCell.Y + 1);
            var repair = service.GetPart<RepairablePart>(); var serviceDoor = service.GetPart<DoorPart>();
            Require(repair != null && repair.RecipeId == "timber-gate-frame" && !repair.Repaired && serviceDoor.IsOpen,
                "actual broken-open service frame before paid repair");
            yield return Capture("annex-02-broken-service-frame");
            int repairSupply = ResidentUnits(actor, "SalvagedTimber");
            yield return ResidentWorldAction(service, RepairablePart.RepairCommand); yield return ResidentCloseMenus();
            Require(repair.Repaired && ResidentUnits(actor, "SalvagedTimber") == repairSupply - 2, "native repair consumes exactly two acquired timber units");
            yield return ResidentWorldAction(service, DoorPart.CloseCommand); yield return ResidentCloseMenus();
            Require(serviceDoor.IsClosed, "repaired service gate closes from the public corridor");
            yield return Capture("annex-03-repaired-closed-service-frame");
            yield return ResidentWorldAction(service, DoorPart.OpenCommand); yield return ResidentCloseMenus();
            Check("annex_paid_repair", repair.Repaired && serviceDoor.IsOpen && !RepairablePart.BlocksFunction(service) && BiomeDrawn(service));

            yield return AnnexWalkTo(transferCell.X + 1, transferCell.Y);
            yield return ResidentWorldAction(transfer, DoorPart.OpenCommand); yield return ResidentCloseMenus();
            Require(!transfer.GetPart<DoorPart>().IsClosed, "ordinary transfer opening from holding side");
            // The subject may have wandered beyond sight while public filing took
            // place. Approach through the actual open aperture until it notices
            // the player; opening a distant door does not magically attract it.
            int approachSteps = 0;
            while (enemy.GetPart<BrainPart>().Target != actor && approachSteps++ < 12)
            {
                var from = Cell(); var target = zone.GetEntityCell(enemy);
                Require(target != null && !CombatSystem.IsDeathHandled(enemy), "same subject before visible approach");
                if (SpatialQuery.Distance(zone, actor, enemy) <= 2)
                { yield return Tap(Key.Period); yield return CombatWaitForFx(); continue; }
                var path = FindPath.Search(zone, from.X, from.Y, target.X, target.Y);
                Require(path.Usable && path.Steps.Count > 0, "ordinary approach toward the moving gallery subject");
                var step = path.Steps[0]; yield return AnnexWalkTo(from.X + step.dx, from.Y + step.dy);
            }
            Require(enemy.GetPart<BrainPart>().Target == actor, "subject notices actual visible player before retreat");
            yield return Capture("annex-03b-visible-approach-before-retreat");
            yield return AnnexObserveCommitment(enemy, transferCell.X, transferCell.Y);
            // The two-cell lash can reach from the aperture; lure one cell farther
            // so the subject must actually enter the holding chamber.
            yield return AnnexWalkTo(transferCell.X + 3, transferCell.Y);
            int waits = 0;
            while (zone.GetEntityCell(enemy)?.X <= transferCell.X && waits < 20)
            {
                Require(actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(enemy), "live ordinary lure participants");
                waits++; yield return Tap(Key.Period); yield return CombatWaitForFx();
            }
            var entered = zone.GetEntityCell(enemy);
            Require(entered != null && entered.X > transferCell.X && entered.Y > y && entered.Y < y + 6,
                "half-set actually pursues into holding within twenty ordinary waits; player=" + zone.GetEntityPosition(actor) + "; subject=" + zone.GetEntityPosition(enemy));
            var closingCell = new[] { 1, -1 }.Select(d => zone.GetCell(transferCell.X + 1, transferCell.Y + d))
                .FirstOrDefault(c => c != null && !c.BlocksMovement(actor) && !c.Objects.Any(e => e != actor && e.HasTag("Creature")));
            Require(closingCell != null, "actual clear standing place beside transfer gate");
            yield return AnnexWalkTo(closingCell.X, closingCell.Y);
            Require(zone.GetEntityCell(enemy).X > transferCell.X && !zone.GetEntityCell(transfer).Objects.Any(e => e.HasTag("Creature")),
                "subject clears the real transfer threshold before closure");
            yield return ResidentWorldAction(transfer, DoorPart.CloseCommand); yield return ResidentCloseMenus();
            Require(transfer.GetPart<DoorPart>().IsClosed, "native transfer closure succeeds behind living subject");
            yield return Capture("annex-04-living-subject-in-holding");
            // Close from the southern side and take the direct escape. Circling
            // the room gives the pursuing subject a diagonal shortcut to the exit.
            yield return AnnexWalkTo(serviceCell.X, serviceCell.Y + 1);
            Require(!zone.GetEntityCell(service).Objects.Any(e => e.HasTag("Creature")), "service threshold clear after actual escape");
            yield return ResidentWorldAction(service, DoorPart.CloseCommand); yield return ResidentCloseMenus();
            Check("annex_living_transfer", HoldingContains(enemy, transferCell.X, y) && enemy.ID == subjectID
                && enemy.GetStatValue("Hitpoints") == subjectHP && !CombatSystem.IsDeathHandled(enemy)
                && transfer.GetPart<DoorPart>().IsClosed && serviceDoor.IsClosed && actor.GetStatValue("Hitpoints") > 0);
            _descriptions.Add(new Description { subject = "Actual lure and escape", source = subjectID,
                text = "Ordinary wait inputs=" + waits + "; unchanged subject HP=" + enemy.GetStatValue("Hitpoints")
                    + "; subject cell=" + zone.GetEntityPosition(enemy) + "; player HP=" + actor.GetStatValue("Hitpoints")
                    + ". No subject movement, health or brain was set." });
            yield return Capture("annex-05-real-closed-holding-aftermath");

            var galleryCell = zone.GetEntityCell(gallery);
            yield return AnnexWalkTo(galleryCell.X, galleryCell.Y + 1);
            yield return ResidentWorldAction(gallery, DoorPart.OpenCommand); yield return ResidentCloseMenus();
            yield return CurationApproach(cabinet);
            var goods = cabinet.GetPart<ContainerPart>().Contents.ToArray(); Require(goods.Length == 3, "near cabinet contains only dressing, gloves and clay");
            var deepGoods = conservation.GetPart<ContainerPart>().Contents.ToArray();
            Require(deepGoods.Length == 2, "deeper ink stock still untouched");
            var quantities = goods.GroupBy(e => e.BlueprintName).ToDictionary(g => g.Key, g => g.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            var beforeGoods = quantities.Keys.ToDictionary(bp => bp, bp => ResidentUnits(actor, bp));
            yield return ResidentWorldAction(cabinet, "OpenContainer"); Require(State() == "PickupOpen", "native gallery recovery contents");
            yield return Capture("annex-06-actual-recovery-stock"); yield return Tap(Key.Tab); yield return ResidentCloseMenus();
            Check("annex_partial_recovery", cabinet.GetPart<ContainerPart>().Contents.Count == 0
                && conservation.GetPart<ContainerPart>().Contents.SequenceEqual(deepGoods)
                && quantities.All(q => ResidentUnits(actor, q.Key) == beforeGoods[q.Key] + q.Value));
            yield return CurationApproach(conservation);
            var deepQuantities = deepGoods.ToDictionary(e => e.BlueprintName, e => e.GetPart<StackerPart>()?.StackCount ?? 1);
            var deepBefore = deepQuantities.Keys.ToDictionary(bp => bp, bp => ResidentUnits(actor, bp));
            yield return ResidentWorldAction(conservation, "OpenContainer"); Require(State() == "PickupOpen", "native deeper conservation contents");
            yield return Capture("annex-06b-deeper-conservation-stock"); yield return Tap(Key.Tab); yield return ResidentCloseMenus();
            Check("annex_finite_recovery", conservation.GetPart<ContainerPart>().Contents.Count == 0
                && deepQuantities.All(q => ResidentUnits(actor, q.Key) == deepBefore[q.Key] + q.Value)
                && cabinet.GetPart<ContainerPart>().Contents.Count == 0
                && quantities.All(q => ResidentUnits(actor, q.Key) == beforeGoods[q.Key] + q.Value)
                && HoldingContains(enemy, transferCell.X, y) && enemy.GetStatValue("Hitpoints") == subjectHP);
            yield return AnnexWalkTo(galleryCell.X, galleryCell.Y + 1);
            yield return ResidentWorldAction(gallery, DoorPart.CloseCommand); yield return ResidentCloseMenus();
            bool retained = true;
            for (int i = 0; i < 12; i++)
            {
                yield return Tap(Key.Period);
                retained &= HoldingContains(enemy, transferCell.X, y) && serviceDoor.IsClosed && transfer.GetPart<DoorPart>().IsClosed
                    && gallery.GetPart<DoorPart>().IsClosed && inspection.GetPart<DoorPart>().IsClosed;
            }
            Check("annex_closed_return", retained && conservation.GetPart<ContainerPart>().Contents.Count == 0 && cabinet.GetPart<ContainerPart>().Contents.Count == 0
                && rack.GetPart<ContainerPart>().Contents.Count == 0 && enemy.GetStatValue("Hitpoints") == subjectHP && actor.GetStatValue("Hitpoints") > 0);
            yield return Capture("annex-07-depleted-stock-living-containment");
            yield return CurationAnnexCheckpoint(subjectID, subjectHP, transferCell.X, y);
        }

        // Real generated actor, ordinary scheduler, no relocation or attack calls.
        private IEnumerator AnnexObserveCommitment(Entity enemy, int exitX, int exitY)
        {
            var zone = _input.CurrentZone; var actor = _input.PlayerEntity;
            var commitment = enemy.GetPart<CommittedMeleePart>(); Require(commitment != null, "authored half-set commitment");
            for (int i = 0; !commitment.IsWindingUp && i < 18; i++)
            {
                Require(actor.GetStatValue("Hitpoints") > 0, "live player awaiting actual tell");
                var from = Cell(); var target = zone.GetEntityCell(enemy);
                if (SpatialQuery.Distance(zone, actor, enemy) <= 2)
                { yield return Tap(Key.Period); yield return CombatWaitForFx(); }
                else
                {
                    var path = FindPath.Search(zone, from.X, from.Y, target.X, target.Y);
                    Require(path.Usable && path.Steps.Count > 0, "actual approach to tendril reach");
                    var d = path.Steps[0]; yield return AnnexWalkTo(from.X + d.dx, from.Y + d.dy);
                }
            }
            var standing = Cell();
            Require(commitment.IsWindingUp && commitment.ThreatensCell(zone, standing.X, standing.Y), "player stands on actual signalled ray");
            Require(!string.IsNullOrEmpty(CombatIntentReadout.ActorLine(enemy, zone))
                && !string.IsNullOrEmpty(CombatIntentReadout.ThreatLine(zone, standing)), "normal visible actor and ground readouts exist");
            yield return Capture("annex-03c-visible-fixed-lash");
            int hp = actor.GetStatValue("Hitpoints"), tick = _input.TurnManager.TickCount;
            var side = (from dx in new[] { -1, 0, 1 } from dy in new[] { -1, 0, 1 }
                where dx != 0 || dy != 0 let c = zone.GetCell(standing.X + dx, standing.Y + dy)
                where c != null && !c.BlocksMovement(actor) && !c.Objects.Any(e => e != actor && e.HasTag("Creature"))
                    && !commitment.ThreatensCell(zone, c.X, c.Y)
                orderby Math.Abs(c.X - exitX) + Math.Abs(c.Y - exitY) select c).FirstOrDefault();
            Require(side != null, "real lateral escape exists before acting");
            yield return AnnexWalkTo(side.X, side.Y);
            for (int i = 0; commitment.IsWindingUp && i < 3; i++)
            { yield return Tap(Key.Period); yield return CombatWaitForFx(); }
            Check("annex_commitment_and_sidestep", commitment.IsRecovering
                && actor.GetStatValue("Hitpoints") == hp && _input.TurnManager.TickCount > tick
                && !commitment.ThreatensCell(zone, Cell().X, Cell().Y)
                && CombatIntentReadout.ActorLine(enemy, zone).Contains("Recovering"));
            _descriptions.Add(new Description { subject = "Actual fixed lash and sidestep", source = enemy.ID,
                text = "Player stepped from " + standing.X + "," + standing.Y + " to " + side.X + "," + side.Y
                    + "; HP stayed " + hp + "; subject now " + commitment.Describe()
                    + " No enemy, phase, HP, RNG or scheduler state was set." });
            yield return Capture("annex-03d-sidestep-recovery-opening");
        }

        private bool HoldingContains(Entity subject, int left, int top)
        {
            var cell = _input.CurrentZone.GetEntityCell(subject);
            return cell != null && cell.X > left && cell.X < left + 6 && cell.Y > top && cell.Y < top + 6;
        }

        // Every route step is paid native input, with refusal before bump-attacking
        // an actor or opening an unplanned door. Dynamic pursuit may stop the audit.
        private IEnumerator AnnexWalkTo(int x, int y)
        {
            for (int step = 0; step < 80; step++)
            {
                Require(State() == "Normal" && _input.PlayerEntity.GetStatValue("Hitpoints") > 0, "live annex walking actor");
                var at = Cell(); if (at.X == x && at.Y == y) yield break;
                var path = FindPath.Search(_input.CurrentZone, at.X, at.Y, x, y);
                Require(path.Usable && path.Steps.Count > 0, "actual annex route exists"); var d = path.Steps[0];
                var next = _input.CurrentZone.GetCell(at.X + d.dx, at.Y + d.dy);
                Require(next != null && !next.BlocksMovement(_input.PlayerEntity)
                    && !next.Objects.Any(e => e != _input.PlayerEntity && e.HasTag("Creature")), "annex next step avoids actors and closed apertures");
                yield return Tap(Direction(d.dx, d.dy)); yield return CombatWaitForFx();
                Require(Cell() == next, "paid native annex movement reaches the intended cell");
            }
            throw new InvalidOperationException("Annex movement exceeded eighty paid inputs.");
        }

        private IEnumerator CurationAnnexCheckpoint(string subjectID, int subjectHP, int holdingLeft, int top)
        {
            var oldActor = _input.PlayerEntity; var at = Cell(); string gear = BiomeItemCollection(oldActor);
            var enemy = CurationOwner("CurationHalfSet"); var enemyAt = _input.CurrentZone.GetEntityPosition(enemy);
            var info = SaveGameService.GetSaveInfo("Quick"); Require(info != null, "existing isolated native save slot");
            string path = Path.Combine(_ownedRoot, info.GameID, "Quick.sav.gz"), oldHash = CheckpointHash(path);
            int hp = oldActor.GetStatValue("Hitpoints"), tick = _input.TurnManager.TickCount;
            yield return Tap(Key.F5); string saved = CheckpointHash(path); Require(saved != oldHash, "native F5 persists annex aftermath");
            var next = new[] { (-1,0), (1,0), (0,1), (0,-1) }.Select(d => _input.CurrentZone.GetCell(at.X + d.Item1, at.Y + d.Item2))
                .FirstOrDefault(c => BiomeSafe(_input.CurrentZone, c, 0)); Require(next != null, "safe actual post-save mutation");
            yield return AnnexWalkTo(next.X, next.Y); Require(CheckpointHash(path) == saved, "post-save native walking does not rewrite checkpoint");
            yield return Tap(Key.F6); double began = Time.realtimeSinceStartupAsDouble;
            while (ReferenceEquals(oldActor, _input.PlayerEntity)) { Require(Time.realtimeSinceStartupAsDouble - began < 8, "native annex saved graph replacement"); yield return null; }
            var restored = CurationOwner("CurationHalfSet"); var service = CurationOwner("CurationServiceGate");
            Check("annex_native_checkpoint", _input.PlayerEntity.ID == oldActor.ID && Cell().X == at.X && Cell().Y == at.Y
                && !ReferenceEquals(enemy, restored) && restored.ID == subjectID && restored.GetStatValue("Hitpoints") == subjectHP
                && _input.CurrentZone.GetEntityPosition(restored) == enemyAt && HoldingContains(restored, holdingLeft, top)
                && new[] { "CurationQuarantineGate", "CurationTransferGate", "CurationGalleryGate", "CurationServiceGate" }.All(bp => CurationOwner(bp).GetPart<DoorPart>().IsClosed)
                && service.GetPart<RepairablePart>().Repaired && CurationOwner("CurationRecoveryCabinet").GetPart<ContainerPart>().Contents.Count == 0
                && CurationOwner("CurationMaintenanceRack").GetPart<ContainerPart>().Contents.Count == 0
                && CurationOwner("CurationConservationCase").GetPart<ContainerPart>().Contents.Count == 0
                && BiomeItemCollection(_input.PlayerEntity) == gear && _input.PlayerEntity.GetStatValue("Hitpoints") == hp
                && _input.TurnManager.TickCount == tick && CheckpointHash(path) == saved && State() == "Normal");
            yield return Capture("annex-08-restored-persistent-aftermath");
        }
    }
}
