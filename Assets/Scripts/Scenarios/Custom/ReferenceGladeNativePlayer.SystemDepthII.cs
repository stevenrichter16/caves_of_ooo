using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        private bool _systemDepthIIOnly, _systemDepthIIComplete;
        private Action _systemDepthIICleanup;
        private const string SystemDepthIICanVerify = "Isolated ordinary seed64 new game with explicitly arranged factory companion, supplies, chest, oil coating, injury and hostile ditch mate. Native keyboard and synthetic Gamepad menu navigation execute ordinary transfers, gear choices, step-aside, pith cleanup, eating, tonic use and container looting. The actual starting Ember Spit is cast with the ordinary scheduler active. Exact owners, quantities, purse, effects, cooldowns, goals and action costs are checked; screenshots record the UI. No direct service call performs an acceptance action.";
        private const string SystemDepthIICannotVerify = "Arranged fixtures do not prove organic recruitment/item acquisition, encounter frequency, overall balance, physical Steam Deck feel or build performance. The final combat uses a declared seeded NPC RNG, up to three actual wait commands so the newly scheduled opponent can act, and may advance additional turns while stunned. No explicit Play save/load round trip; normal new-game checkpoint writes are confined to the isolated save root and EditMode covers persistence. Raw screenshots require visual review.";
        public void ConfigureSystemDepthIIAudit() => _systemDepthIIOnly = true;

        private IEnumerator RunSystemDepthIIAudit()
        {
            var actor = _input.PlayerEntity; var zone = _input.CurrentZone; var pack = actor.GetPart<InventoryPart>();
            var companion = _context.Factory.CreateEntity("Villager");
            Require(zone.AddEntity(companion, 39, 12), "arranged companion on open adjacent tile");
            companion.GetPart<RenderPart>().DisplayName = "audit companion";
            var brain = companion.GetPart<BrainPart>(); brain.CurrentZone = zone; brain.Rng = new System.Random(64);
            Require(companion.ApplyEffect(new RecruitedEffect(actor), actor, zone), "arranged current recruitment");
            _input.TurnManager.AddEntity(companion);
            var cap = DepthIICarry("LeatherCap"); var pith = DepthIICarry("PrismreedPith");
            var food = DepthIICarry("DriedMeat"); var tonic = DepthIICarry("HealingTonic");
            var chest = _context.Factory.CreateEntity("Chest"); Require(zone.AddEntity(chest, 40, 13), "arranged adjacent chest");
            var coin = _context.Factory.CreateEntity("GoldCoin"); coin.GetPart<StackerPart>().StackCount = 3;
            Require(chest.GetPart<ContainerPart>().AddItem(coin), "arranged three ordinary coins");
            Entity foe = null;
            var previousPad = Gamepad.current; var pad = InputSystem.AddDevice<Gamepad>();
            _systemDepthIICleanup = () =>
            {
                if (pad.added) { InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.RemoveDevice(pad); }
                if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
                companion.GetEffect<RecruitedEffect>()?.Dismiss(actor);
                _input.TurnManager.RemoveEntity(companion); zone.RemoveEntity(companion);
                if (foe != null) { _input.TurnManager.RemoveEntity(foe); zone.RemoveEntity(foe); }
                zone.RemoveEntity(chest); ZoneRenderHooks.MarkFullDirty("Scenario.DepthIICleanup");
                Check("depth_ii_gamepad_and_previous_device_restored", !pad.added && (previousPad == null || !previousPad.added || Gamepad.current == previousPad));
            };
            ZoneRenderHooks.MarkFullDirty("Scenario.DepthIISetup");
            try
            {
                yield return ControllerHold(pad, new GamepadState(), .03f);
                yield return DepthIIWorldChoice(companion, CompanionOrders.StayCommand, pad);
                yield return BiomeCloseMenus(); Require(CompanionOrders.IsStaying(companion), "actual stay before management");
                var before = ControllerBefore();
                yield return DepthIIWorldChoice(companion, "CompanionPack", pad);
                yield return Capture("02-companion-pack-reader");
                Check("companion_pack_inspection_is_free", State() == "AnnouncementOpen" && ControllerCost(before, 0));
                yield return BiomeCloseMenus();

                before = ControllerBefore(); yield return DepthIIWorldChoice(companion, "CompanionGive|", pad, cap);
                yield return BiomeCloseMenus();
                Check("native_give_moves_exact_gear_and_pays_once", companion.GetPart<InventoryPart>().Objects.Contains(cap)
                    && !pack.Objects.Contains(cap) && ControllerCost(before, 1));
                before = ControllerBefore(); yield return DepthIIWorldChoice(companion, "CompanionEquip|", pad, cap);
                yield return BiomeCloseMenus();
                Check("native_companion_equipment_uses_actual_slot", InventorySystem.IsEquipped(companion, cap) && ControllerCost(before, 1));
                yield return DepthIIWorldChoice(companion, "CompanionPack", pad);
                yield return Capture("03-companion-equipped-pack"); yield return BiomeCloseMenus();
                before = ControllerBefore(); yield return DepthIIWorldChoice(companion, "CompanionUnequip|", pad, cap);
                yield return BiomeCloseMenus();
                Check("native_unequip_returns_owned_gear_to_companion_pack", !InventorySystem.IsEquipped(companion, cap)
                    && companion.GetPart<InventoryPart>().Objects.Contains(cap) && ControllerCost(before, 1));
                before = ControllerBefore(); yield return DepthIIWorldChoice(companion, "CompanionTake|", pad, cap);
                yield return BiomeCloseMenus();
                Check("native_retrieve_returns_exact_item_and_pays_once", pack.Objects.Contains(cap)
                    && !companion.GetPart<InventoryPart>().Objects.Contains(cap) && ControllerCost(before, 1));

                Require(actor.ApplyEffect(new LiquidCoveredEffect("oil", 15), actor, zone), "arranged removable oil coat");
                int pithBefore = DepthIIUnits(actor, pith);
                var wick = InventorySystem.GetActions(actor, pith).FirstOrDefault(a => a.Command.StartsWith("WickBody|", StringComparison.Ordinal));
                Require(wick != null, "actual carried pith offers body cleanup"); before = ControllerBefore();
                yield return ItemAction(pith, wick.Command); yield return BiomeCloseMenus();
                Check("native_pith_cleans_oil_spends_one_supply_and_one_action", actor.GetEffect<LiquidCoveredEffect>() == null
                    && DepthIIUnits(actor, pith) == pithBefore - 1 && ControllerCost(before, 1));
                yield return Capture("04-pith-cleanup-result");

                actor.GetStat("Hitpoints").BaseValue = Math.Max(1, actor.GetStat("Hitpoints").Max - 12);
                int hp = actor.GetStatValue("Hitpoints"), foodBefore = DepthIIUnits(actor, food); before = ControllerBefore();
                yield return ItemAction(food, "Eat"); yield return BiomeCloseMenus();
                Check("native_eating_consumes_and_advances_world", DepthIIUnits(actor, food) == foodBefore - 1 && ControllerCost(before, 1));
                int tonicBefore = DepthIIUnits(actor, tonic); before = ControllerBefore();
                yield return ItemAction(tonic, "ApplyTonic"); yield return BiomeCloseMenus();
                Check("native_tonic_heals_consumes_and_advances_world", actor.GetStatValue("Hitpoints") > hp
                    && DepthIIUnits(actor, tonic) == tonicBefore - 1 && ControllerCost(before, 1));
                yield return Capture("05-consumables-pay-time");

                int purse = actor.GetIntProperty(TradeSystem.CURRENCY_PROP, 0);
                yield return DepthIIWorldChoice(chest, "OpenContainer", pad);
                Require(State() == "PickupOpen", "real container pickup popup"); yield return Capture("06-container-coins");
                yield return Tap(Key.Tab); yield return BiomeCloseMenus();
                Check("native_container_coins_credit_exact_purse", actor.GetIntProperty(TradeSystem.CURRENCY_PROP, 0) == purse + 15
                    && !chest.GetPart<ContainerPart>().Contents.Contains(coin) && !pack.Objects.Contains(coin));

                var oldCell = zone.GetEntityPosition(companion); before = ControllerBefore();
                yield return DepthIIWorldChoice(companion, "CompanionStepAside", pad); yield return BiomeCloseMenus();
                Check("native_step_aside_moves_voluntarily_preserves_stay_and_pays_once", zone.GetEntityPosition(companion) != oldCell
                    && CompanionOrders.IsStaying(companion) && ControllerCost(before, 1));
                yield return Capture("07-companion-clears-the-way");
                yield return WalkTo(oldCell.x, oldCell.y);
                yield return DepthIIWorldChoice(companion, CompanionOrders.FollowCommand, pad); yield return BiomeCloseMenus();

                // Explicit factory opponent at melee reach, after the peaceful
                // inventory route. The authored chance is unchanged; seed1
                // makes the finite scheduler observation reproducible.
                foe = _context.Factory.CreateEntity("SpreadDitchMate");
                Require(zone.AddEntity(foe, Cell().X + 1, Cell().Y), "arranged authored ditch mate");
                var enemyBrain = foe.GetPart<BrainPart>(); enemyBrain.CurrentZone = zone; enemyBrain.Rng = new System.Random(1);
                enemyBrain.PushGoal(new KillGoal(actor)); _input.TurnManager.AddEntity(foe);
                ZoneRenderHooks.MarkFullDirty("Scenario.DepthIICombat");
                int enemyHp = foe.GetStatValue("Hitpoints");
                yield return Tap(Key.Digit1); Require(State() == "AwaitingDirection", "ordinary starting Ember Spit selected");
                yield return Tap(Key.D);
                for (int i = 0; i < 180 && State() != "Normal"; i++) yield return null;
                var spit = actor.GetPart<ActivatedAbilitiesPart>().AbilityList.First(a => a.Command == "CommandEmberSpit");
                Check("native_starting_spell_damages_and_companion_assists", foe.GetStatValue("Hitpoints") < enemyHp
                    && spit.CooldownRemaining > 0 && brain.FindGoal<KillGoal>()?.Target == foe);
                var slam = foe.GetPart<ActivatedAbilitiesPart>().AbilityList.First(a => a.Command == "CommandSlam");
                DepthIICombatObservation(foe, "after Ember Spit");
                // A newly appended actor starts at zero energy; the player can
                // win the first readiness tie. Frames alone never advance the
                // turn-based scheduler. Give the opponent a bounded real turn.
                for (int i = 0; i < 3 && slam.CooldownRemaining == 0 && foe.GetStatValue("Hitpoints") > 0; i++)
                {
                    yield return BiomeCloseMenus(); yield return Tap(Key.Period);
                    for (int j = 0; j < 180 && State() != "Normal"; j++) yield return null;
                    DepthIICombatObservation(foe, "after ordinary wait " + (i + 1));
                }
                Check("authored_ditch_mate_slam_runs_on_real_scheduler", slam.CooldownRemaining > 0);
                yield return Capture("08-direct-spell-party-combat");
                _systemDepthIIComplete = true;
            }
            finally { _systemDepthIICleanup?.Invoke(); _systemDepthIICleanup = null; }
        }
        private Entity DepthIICarry(string blueprint)
        {
            var item = _context.Factory.CreateEntity(blueprint); var pack = _input.PlayerEntity.GetPart<InventoryPart>();
            Require(item != null && pack.AddRetrievedObject(item), "arranged carried factory " + blueprint); return item;
        }
        private void DepthIICombatObservation(Entity foe, string stage)
        {
            var brain = foe.GetPart<BrainPart>(); var cell = _input.CurrentZone.GetEntityPosition(foe);
            _descriptions.Add(new Description { subject = "Scheduled ditch mate", source = stage,
                text = "id=" + foe.ID + "; HP=" + foe.GetStatValue("Hitpoints") + "; energy=" + _input.TurnManager.GetEnergy(foe)
                    + "; tick=" + _input.TurnManager.TickCount + "; cell=" + cell.x + "," + cell.y
                    + "; target=" + brain.FindGoal<KillGoal>()?.Target?.ID + "; state=" + State(), units = 0 });
        }
        private static int DepthIIUnits(Entity owner, Entity item) => owner.GetPart<InventoryPart>().Objects.Contains(item)
            ? item.GetPart<StackerPart>()?.StackCount ?? 1 : 0;
        private IEnumerator DepthIIWorldChoice(Entity target, string prefix, Gamepad pad, Entity item = null)
        {
            var from = Cell(); var to = SpatialQuery.ClosestCell(_input.CurrentZone, target, from.X, from.Y);
            Require(Math.Max(Math.Abs(from.X - to.X), Math.Abs(from.Y - to.Y)) <= 1, "native current management reach");
            yield return Tap(Key.L);
            if (to.X != from.X) yield return Tap(to.X > from.X ? Key.D : Key.A);
            if (to.Y != from.Y) yield return Tap(to.Y > from.Y ? Key.S : Key.W);
            yield return Tap(Key.Enter);
            Require(State() == "WorldActionMenuOpen" && _input.WorldActionMenuUI.SelectedTarget == target, "actual management target selected");
            var actions = (List<InventoryAction>)Field(_input.WorldActionMenuUI, "_actions");
            var chosen = actions.FirstOrDefault(a => a.Command.StartsWith(prefix, StringComparison.Ordinal)
                && (item == null || a.Command.Contains(Uri.EscapeDataString(item.ID))));
            Require(chosen != null, "actual management choice " + prefix);
            for (int i = 0; _input.WorldActionMenuUI.HighlightedAction?.Command != chosen.Command; i++)
            { Require(i < actions.Count + 1, "bounded native menu navigation"); yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.DpadDown)); }
            yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.South));
        }
    }
}
