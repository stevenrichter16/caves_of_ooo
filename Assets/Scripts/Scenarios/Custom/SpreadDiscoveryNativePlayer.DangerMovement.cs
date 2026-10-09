using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool _dangerMovement;
        string _dangerActorId, _dangerOilId, _dangerOilCommand;
        const string DangerMovementIntent = "Controlled native acceptance: ordinary duelist bootstrap followed by disclosed enclosed clear ground, a full-health factory marlback and one finite FrogOil. Real F5/waits show the clean direct first step; F6 restores the exact baseline; real inventory keys spread grease east and the same enemy's actual first approach takes a clean alternative. No simulation actions are invoked directly after setup.";
        const string DangerMovementLimits = "Controlled geometry and one supplied existing item, not ordinary item discovery, generated encounter frequency, difficulty or a performance proof. Setup removes owners/tile state within one rectangle and adds grass/walls, one original enemy and one FrogOil. No later actor placement, health, stock, target, goal, energy, RNG or visibility edits. Existing scene full reveal remains the user's chosen setting. Only voluntary approach is shown; forced/sole-route, immunity, retreat and firing-position controls are separate native tests. Screenshots need review.";
        static readonly string[] DangerMovementChecks = { "ordinary_start", "danger_controlled_setup", "danger_clean_approach", "danger_exact_branch_restore", "danger_native_oil_payment", "danger_oil_visible", "danger_safer_actual_step", "danger_finish" };
        Entity DangerActor => Owner(_dangerActorId);
        Entity DangerOil => Player.GetPart<InventoryPart>().Objects.SingleOrDefault(e => e.ID == _dangerOilId);

        public void InitializeDangerMovement(ScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Isolated launcher required before danger-movement bootstrap.");
            _dangerMovement = true; Initialize(context, connectedBuild: "duelist");
        }
        bool DangerContact()
        {
            var actor = DangerActor; var brain = actor?.GetPart<BrainPart>(); var goal = brain?.FindGoal<KillGoal>();
            return actor != null && brain.Target == Player && goal?.Target == Player
                && AIHelpers.TryGetVisibleTargetCell(actor, Player, Zone, brain.SightRadius, out _);
        }
        bool DangerOneMoveWindow(string marker)
        {
            var rows = Window(marker);
            return rows.Count(r => r.Kind == "Begin" && r.ActorId == _dangerActorId) == 1
                && rows.Count(r => r.Kind == "End" && r.ActorId == _dangerActorId) == 1
                && !rows.Any(r => (r.ActorId == _dangerActorId || r.TargetId == _dangerActorId)
                    && (r.Kind == "HitRoll" || r.Kind == "MeleeAttackVetoed" || r.Kind == "SpellDamage" || r.Kind == "DamageDealt"));
        }
        bool DangerOilRendered(int x, int y)
        {
            var presenter = _input.ZoneRenderer?.SpawnRing3D;
            return Zone.GetCell(x, y)?.IsVisible == true && presenter != null
                && ReferenceEquals(presenter.CurrentZone, Zone)
                && presenter.TryGetElementVolume(x, y, out var root, out var sample) && sample.Kind == "coating:oil"
                && root != null && root.activeInHierarchy && root.GetComponentsInChildren<Renderer>(true)
                    .Any(r => r.enabled && r.gameObject.activeInHierarchy && !r.forceRenderingOff);
        }
        IEnumerator DangerOilChoice(Entity oil, int x, int y)
        {
            Require(State == "Normal" && Owns(Player, oil), "native original finite oil is carried");
            yield return Tap(Key.I); Require(State == "InventoryOpen", "real native inventory opens");
            for (int n = 0; (int)Field(_input.InventoryUI, "_panel") != 1; n++)
            { Require(n < 6, "bounded native carried pane"); yield return Tap(Key.Tab); }
            var rows = (IList)Field(_input.InventoryUI, "_rows"); int row = -1;
            for (int i = 0; i < rows.Count; i++)
                if (ReferenceEquals(((InventoryScreenData.ItemDisplay)Field(rows[i], "Item"))?.Item, oil)) row = i;
            Require(row >= 0, "actual original oil row");
            for (int n = 0; (int)Field(_input.InventoryUI, "_cursorIndex") != row; n++)
            { Require(n < 80, "bounded native oil cursor"); yield return Tap((int)Field(_input.InventoryUI, "_cursorIndex") < row ? Key.DownArrow : Key.UpArrow); }
            yield return Tap(Key.Enter);
            var popup = Field(_input.InventoryUI, "_itemActionPopup"); Require(popup != null, "native oil action menu");
            var actions = ((IList)Field(popup, "Actions")).Cast<object>().ToArray();
            int index = Array.FindIndex(actions, a =>
            {
                string command = (string)Field(a, "Command");
                if (command == null || !command.StartsWith("SpreadGrease|", StringComparison.Ordinal)) return false;
                var parts = command.Split('|'); return parts.Length == 7 && parts[4] == x.ToString() && parts[5] == y.ToString();
            });
            Require(index >= 0, "real current spread-grease-east action is offered");
            _dangerOilCommand = (string)Field(actions[index], "Command");
            for (int n = 0; (int)Field(popup, "CursorIndex") != index; n++)
            { Require(n < 40, "bounded real oil action selection"); yield return Tap((int)Field(popup, "CursorIndex") < index ? Key.DownArrow : Key.UpArrow); }
            yield return Capture("danger-03-real-oil-menu");
            yield return Tap(Key.Enter);
        }
        IEnumerator DangerMovementJourney()
        {
            int hp = Player.GetStatValue("Hitpoints"), tick = Tick, energy = Energy, world = WorldClock.CurrentTick;
            int drams = TradeSystem.GetDrams(Player), initialInputs = _localInputs;
            string stats = Stats(Player), playerId = Player.ID;
            var originalPack = Player.GetPart<InventoryPart>().Objects.ToArray();
            var originalEquipment = Player.GetPart<InventoryPart>().EquippedItems.ToArray();
            Require(!originalPack.Any(e => e.BlueprintName == "FrogOil"), "one newly disclosed finite oil, without a merge recipient");
            var setup = new DangerAwareApproachScenario();
            setup.Apply(new ScenarioContext(Zone, _input.EntityFactory, Player, _input.TurnManager));
            _dangerActorId = setup.Actor.ID; _dangerOilId = setup.Oil.ID; int x = setup.X, y = setup.Y;
            yield return Settled();
            Check("danger_controlled_setup", At.X == x && At.Y == y && Stats(Player) == stats && Player.ID == playerId
                && Tick == tick && Energy == energy && WorldClock.CurrentTick == world && TradeSystem.GetDrams(Player) == drams
                && originalPack.All(e => Owns(Player, e)) && Player.GetPart<InventoryPart>().Objects.Count == originalPack.Length + 1
                && Player.GetPart<InventoryPart>().EquippedItems.ToArray().SequenceEqual(originalEquipment)
                && Owns(Player, DangerOil) && Units(DangerOil) == 1 && DangerActor.GetStatValue("Hitpoints") == 15
                && Zone.GetEntityPosition(DangerActor) == (x + 2, y) && DangerActor.GetPart<BrainPart>().Target == null
                && !Zone.TileState.HasCoating(x + 1, y, "oil") && TerrainNavigationWeight.ForStep(Zone, x + 1, y, DangerActor) == 0);
            _observations.Add(new { phase = "danger-disclosed-setup", setup.RemovedOwners, x, y,
                actor = _dangerActorId, oil = _dangerOilId, declaration = DangerMovementLimits });
            yield return Capture("danger-01-controlled-clear-ground");

            var oldPlayer = Player; var oldActor = DangerActor; var oldOil = DangerOil; var oldZone = Zone;
            string savedGear = Gear(Player), actorGear = Gear(DangerActor), notes = NoteSignature();
            int actorEnergy = _input.TurnManager.GetEnergy(DangerActor);
            yield return Tap(Key.F5); yield return Settled(); string file = SaveFile(); _checkpointHash = HashFile(file);
            Require(MessageLog.GetLast() == "Game saved." && !string.IsNullOrEmpty(_checkpointHash)
                && Tick == tick && Energy == energy && WorldClock.CurrentTick == world, "real unchanged clear-floor checkpoint");
            string marker = null;
            for (int n = 0; Zone.GetEntityPosition(DangerActor) == (x + 2, y) && n < 2; n++)
            {
                marker = Mark("danger-before-clean-first-step-" + n);
                yield return Paid(Tap(Key.Period), "local", "danger-native-clean-wait-" + n);
            }
            Check("danger_clean_approach", Zone.GetEntityPosition(DangerActor) == (x + 1, y)
                && DangerContact() && DangerOneMoveWindow(marker) && Player.GetStatValue("Hitpoints") == hp
                && DangerActor.GetStatValue("Hitpoints") == 15 && Gear(Player) == savedGear && Gear(DangerActor) == actorGear
                && !Zone.TileState.HasCoating(x + 1, y, "oil") && HashFile(file) == _checkpointHash);
            _observations.Add(new { phase = "danger-clean-first-step", actorAt = Zone.GetEntityPosition(DangerActor), rows = Window(marker) });
            yield return Capture("danger-02-clean-direct-approach");
            yield return Reload(oldPlayer);
            Check("danger_exact_branch_restore", Player != oldPlayer && Zone != oldZone && DangerActor != oldActor && DangerOil != oldOil
                && Player.ID == playerId && At.X == x && At.Y == y && Tick == tick && Energy == energy && WorldClock.CurrentTick == world
                && Stats(Player) == stats && Gear(Player) == savedGear && Gear(DangerActor) == actorGear && NoteSignature() == notes
                && TradeSystem.GetDrams(Player) == drams && _input.TurnManager.GetEnergy(DangerActor) == actorEnergy
                && Zone.GetEntityPosition(DangerActor) == (x + 2, y) && DangerActor.GetStatValue("Hitpoints") == 15
                && DangerActor.GetPart<BrainPart>().Target == null && DangerActor.GetPart<BrainPart>().FindGoal<KillGoal>() == null
                && Owns(Player, DangerOil) && Units(DangerOil) == 1 && !Zone.TileState.HasCoating(x + 1, y, "oil")
                && HashFile(file) == _checkpointHash);

            var oil = DangerOil; string oilMarker = Mark("danger-before-native-oil");
            yield return Paid(DangerOilChoice(oil, x + 1, y), "local", "danger-native-finite-oil");
            var oilRows = Window(oilMarker);
            var use = oilRows.Where(r => r.Kind == "CombatUtilityUsed" && r.ActorId == Player.ID && r.TargetId == _dangerOilId).ToArray();
            var payload = use.Length == 1 ? JObject.Parse(use[0].PayloadJson) : null;
            Check("danger_native_oil_payment", use.Length == 1 && payload.Value<string>("blueprint") == "FrogOil"
                && payload.Value<string>("layer") == "oil" && payload.Value<int>("spent") == 1
                && payload.Value<int>("x") == x + 1 && payload.Value<int>("y") == y && payload.Value<int>("turns") == 8
                && DangerOil == null && CountGraphId(_dangerOilId) == 0 && oil.GetPart<PhysicsPart>().InInventory == null
                && _lastClock.CompletedTurns == 1 && TradeSystem.GetDrams(Player) == drams
                && Zone.TileState.CoatingTurns(x + 1, y, "oil") > 0 && Zone.TileState.CoatingTurns(x + 1, y, "oil") <= 8
                && TerrainNavigationWeight.ForStep(Zone, x + 1, y, DangerActor) == 30
                && TerrainNavigationWeight.ForStep(Zone, x + 1, y - 1, DangerActor) == 0
                && TerrainNavigationWeight.ForStep(Zone, x + 1, y + 1, DangerActor) == 0);
            yield return Capture("danger-04-real-grease-film");
            Check("danger_oil_visible", DangerOilRendered(x + 1, y));
            marker = oilMarker;
            for (int n = 0; Zone.GetEntityPosition(DangerActor) == (x + 2, y) && n < 2; n++)
            {
                marker = Mark("danger-before-oily-first-step-" + n);
                yield return Paid(Tap(Key.Period), "local", "danger-native-oily-wait-" + n);
            }
            var chosen = Zone.GetEntityPosition(DangerActor);
            Check("danger_safer_actual_step", chosen.x == x + 1 && Math.Abs(chosen.y - y) == 1
                && TerrainNavigationWeight.ForStep(Zone, chosen.x, chosen.y, DangerActor) == 0
                && TerrainNavigationWeight.ForStep(Zone, x + 1, y, DangerActor) == 30
                && Zone.TileState.CoatingTurns(x + 1, y, "oil") > 0 && DangerContact() && DangerOneMoveWindow(marker)
                && At.X == x && At.Y == y && Player.GetStatValue("Hitpoints") == hp && DangerActor.GetStatValue("Hitpoints") == 15
                && Gear(DangerActor) == actorGear && HashFile(file) == _checkpointHash);
            _observations.Add(new { phase = "danger-native-first-detour", chosen, ideal = new { x = x + 1, y },
                idealCost = TerrainNavigationWeight.ForStep(Zone, x + 1, y, DangerActor),
                chosenCost = TerrainNavigationWeight.ForStep(Zone, chosen.x, chosen.y, DangerActor),
                actualCommand = _dangerOilCommand, remainingOilTurns = Zone.TileState.CoatingTurns(x + 1, y, "oil"), rows = Window(marker) });
            yield return Capture("danger-05-enemy-takes-clean-alternative");
            Check("danger_finish", State == "Normal" && _localInputs - initialInputs <= 6
                && DangerOil == null && Player.GetStatValue("Hitpoints") == hp && DangerActor.GetStatValue("Hitpoints") == 15
                && TradeSystem.GetDrams(Player) == drams && !DevMode.Enabled && !DebugInvincibility.IsEnabled(Player)
                && !Player.HasPart<BitLockerPart>());
        }
    }
}
