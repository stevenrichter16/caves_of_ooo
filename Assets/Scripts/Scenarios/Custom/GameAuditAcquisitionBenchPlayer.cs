using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Temporary native keyboard driver. Reflection observes UI state; it never selects actions.</summary>
    public sealed class GameAuditAcquisitionBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditAcquisitionBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;
        private bool _oldEvents, _oldTrade;

        public void Initialize(ScenarioContext ctx, GameAuditAcquisitionBench bench)
        {
            _ctx = ctx; _bench = bench; _started = Time.realtimeSinceStartupAsDouble;
            _oldEvents = Diag.IsChannelEnabled("event"); Diag.SetChannel("event", true);
            _oldTrade = Diag.IsChannelEnabled("trade"); Diag.SetChannel("trade", true);
            _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings);
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _settings;
            _oldBackground = Application.runInBackground; Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            StartCoroutine(RunSafely(AuditInputs()));
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            // Flatten nested audit helpers so an assertion in any helper writes a failed report immediately.
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception failure = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception e) { failure = e; }
                if (failure != null) { Failures++; Debug.LogError(failure); break; }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            Finish();
        }
        private IEnumerator AuditInputs()
        {
            yield return new WaitForSecondsRealtime(0.6f);
            var input = FindFirstObjectByType<InputHandler>();
            if (input == null) throw new InvalidOperationException("Native input handler missing.");
            var boot = Read(input, "_bootMenuController") as BootMenuController;
            if (boot == null) throw new InvalidOperationException("Native boot controller missing.");
            if (boot.IsActive) yield return Tap(Key.N);
            yield return new WaitForSecondsRealtime(0.3f);
            _bench.Check("native_boot_keeps_arena", !boot.IsActive && _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity) == (20, 12));
            var inv = _ctx.PlayerEntity.GetPart<InventoryPart>();
            var contents = _bench.Sack.GetPart<ContainerPart>(); var shelf = _bench.Merchant.GetPart<InventoryPart>();
            _bench.Check("real_capacity_source_preconditions", inv.MaxWeight == 150 && inv.GetCarriedWeight() == 52
                && inv.Objects.Single() == _bench.StartApples && shelf.MaxWeight == 150 && SourcePair(contents.Contents, _bench.SackLarge, _bench.SackSmall)
                && SourcePair(shelf.Objects, _bench.StockLarge, _bench.StockSmall) && _ctx.Zone.GetEntityPosition(_bench.Sack) == (20, 13));
            int purse = TradeSystem.GetDrams(_ctx.PlayerEntity);
            int credits = Outcomes("CurrencyCreditApplied", _ctx.PlayerEntity.ID, "\"amount\":15");
            yield return Tap(Key.G);
            _bench.Check("native_gold_pays_once", TradeSystem.GetDrams(_ctx.PlayerEntity) == purse + 15
                && _bench.Gold.GetPart<StackerPart>().StackCount == 0 && !inv.Contains(_bench.Gold)
                && _ctx.Zone.GetEntityCell(_bench.Gold) == null
                && Outcomes("CurrencyCreditApplied", _ctx.PlayerEntity.ID, "\"amount\":15") == credits + 1);
            yield return Tap(Key.G);
            _bench.Check("native_second_pickup_does_not_repay", TradeSystem.GetDrams(_ctx.PlayerEntity) == purse + 15
                && State(input) == "Normal" && Outcomes("CurrencyCreditApplied", _ctx.PlayerEntity.ID, "\"amount\":15") == credits + 1);

            int refuses = Outcomes("ItemAcquisitionRejected", _bench.SackLarge.ID, "TakeFromContainer");
            yield return OpenSack(input); yield return Loot(input, _bench.SackLarge);
            _bench.Check("native_container_capacity_refusal", Outcomes("ItemAcquisitionRejected", _bench.SackLarge.ID, "TakeFromContainer") == refuses + 1
                && !string.IsNullOrEmpty(Read(input.PickupUI, "_statusMessage") as string)
                && SourcePair(contents.Contents, _bench.SackLarge, _bench.SackSmall)
                && _bench.SackLarge.GetPart<PhysicsPart>().InInventory == _bench.Sack
                && inv.GetCarriedWeight() == 52 && _bench.StartApples.GetPart<StackerPart>().StackCount == 52);
            yield return CloseLoot(input); yield return EatOne(input, _bench.StartApples);
            _bench.Check("native_first_eat_reaches_boundary", inv.GetCarriedWeight() == 51);
            yield return OpenSack(input); yield return Loot(input, _bench.SackLarge);
            _bench.Check("native_container_retry_preserves_partial_merge", inv.GetCarriedWeight() == 150
                && inv.Objects.Contains(_bench.StartApples) && _bench.StartApples.GetPart<StackerPart>().StackCount == 99
                && inv.Objects.Contains(_bench.SackLarge) && _bench.SackLarge.GetPart<StackerPart>().StackCount == 51
                && _bench.SackLarge.GetPart<PhysicsPart>().InInventory == _ctx.PlayerEntity
                && contents.Contents.Count == 1 && contents.Contents[0] == _bench.SackSmall);
            yield return CloseLoot(input);
            yield return CarriedAction(input, _bench.StartApples, "drop");
            _bench.Check("native_drop_whole_stack_frees_capacity", _ctx.Zone.GetEntityPosition(_bench.StartApples) == (20, 12)
                && _bench.StartApples.GetPart<StackerPart>().StackCount == 99 && inv.GetCarriedWeight() == 51);
            yield return OpenSack(input); yield return Loot(input, _bench.SackSmall); yield return CloseLoot(input);
            _bench.Check("native_last_loot_merges_and_closes", contents.Contents.Count == 0 && inv.GetCarriedWeight() == 52
                && _bench.SackLarge.GetPart<StackerPart>().StackCount == 52 && _bench.SackSmall.GetPart<StackerPart>().StackCount == 0
                && !inv.Objects.Contains(_bench.SackSmall) && State(input) == "Normal");

            yield return OpenTrade(input);
            var trade = input.TradeUI;
            int playerBefore = TradeSystem.GetDrams(_ctx.PlayerEntity), traderBefore = TradeSystem.GetDrams(_bench.Merchant);
            int buyRefusals = Outcomes("BuyRejected", _bench.Merchant.ID, _bench.StockLarge.ID);
            yield return TradeItem(input, _bench.StockLarge, sell: false);
            _bench.Check("native_buy_refusal_preserves_source_wallets", Outcomes("BuyRejected", _bench.Merchant.ID, _bench.StockLarge.ID) == buyRefusals + 1
                && (string)Read(trade, "_statusMessage") == "You cannot carry that much weight."
                && SourcePair(shelf.Objects, _bench.StockLarge, _bench.StockSmall)
                && _bench.StockLarge.GetPart<PhysicsPart>().InInventory == _bench.Merchant && inv.GetCarriedWeight() == 52
                && TradeSystem.GetDrams(_ctx.PlayerEntity) == playerBefore && TradeSystem.GetDrams(_bench.Merchant) == traderBefore);
            yield return Tap(Key.Escape);
            _bench.Check("native_refused_trade_exit", State(input) == "Normal");
            yield return EatOne(input, _bench.SackLarge);
            _bench.Check("native_second_eat_reaches_boundary", inv.GetCarriedWeight() == 51);
            yield return OpenTrade(input);
            int price = TradeSystem.GetBuyPrice(_bench.StockLarge, TradeSystem.GetTradePerformance(_ctx.PlayerEntity), _bench.Merchant);
            int purchases = Outcomes("Bought", _bench.Merchant.ID, _bench.StockLarge.ID);
            yield return TradeItem(input, _bench.StockLarge, sell: false);
            _bench.Check("native_purchase_delivers_exact_remainder_and_pays", inv.GetCarriedWeight() == 150
                && inv.Objects.Contains(_bench.SackLarge) && _bench.SackLarge.GetPart<StackerPart>().StackCount == 99
                && inv.Objects.Contains(_bench.StockLarge) && _bench.StockLarge.GetPart<StackerPart>().StackCount == 51
                && _bench.StockLarge.GetPart<PhysicsPart>().InInventory == _ctx.PlayerEntity
                && shelf.Objects.Count == 1 && shelf.Objects[0] == _bench.StockSmall && _bench.StockSmall.GetPart<StackerPart>().StackCount == 1
                && TradeSystem.GetDrams(_ctx.PlayerEntity) == playerBefore - price && TradeSystem.GetDrams(_bench.Merchant) == traderBefore + price
                && Outcomes("Bought", _bench.Merchant.ID, _bench.StockLarge.ID) == purchases + 1 && Read(trade, "_statusMessage") == null
                && _ctx.Zone.GetEntityCell(_bench.StartApples) != null && _bench.StartApples.GetPart<StackerPart>().StackCount == 99);
            yield return Tap(Key.Escape);
            _bench.Check("native_trade_exit", State(input) == "Normal" && !trade.IsOpen);
        }
        private static bool SourcePair(List<Entity> items, Entity first, Entity second) => items.Count == 2
            && items[0] == first && items[1] == second && first.GetPart<StackerPart>().StackCount == 99 && second.GetPart<StackerPart>().StackCount == 1;
        private IEnumerator OpenSack(InputHandler input)
        {
            yield return Tap(Key.C); yield return Tap(Key.DownArrow);
            _bench.Check("native_sack_target", input.WorldActionMenuUI.SelectedTarget == _bench.Sack);
            yield return MenuAction(input, "OpenContainer");
            _bench.Check("native_loot_open", input.PickupUI.IsOpen && Read(input.PickupUI, "_sourceContainer") == _bench.Sack);
        }
        private IEnumerator Loot(InputHandler input, Entity item)
        {
            var ui = input.PickupUI; var items = Read(ui, "_items") as List<Entity>; int row = items?.IndexOf(item) ?? -1;
            _bench.Check("native_loot_item_row", ui.IsOpen && row >= 0);
            yield return MoveCursor(ui, "_cursorIndex", row); yield return Tap(Key.Enter);
        }
        private IEnumerator CloseLoot(InputHandler input)
        {
            if (input.PickupUI.IsOpen) yield return Tap(Key.Escape);
            _bench.Check("native_loot_exit", !input.PickupUI.IsOpen && State(input) == "Normal");
        }
        private IEnumerator EatOne(InputHandler input, Entity item)
        {
            int before = item.GetPart<StackerPart>().StackCount; int events = Outcomes("FoodEaten", item.ID, "healing");
            yield return CarriedAction(input, item, "Eat");
            _bench.Check("native_eat_one_dispatched", item.GetPart<StackerPart>().StackCount == before - 1
                && Outcomes("FoodEaten", item.ID, "healing") == events + 1);
        }
        private IEnumerator CarriedAction(InputHandler input, Entity item, string command)
        {
            yield return Tap(Key.I); yield return Tap(Key.Tab);
            var ui = input.InventoryUI; var rows = Read(ui, "_rows") as IList; int row = -1;
            for (int i = 0; rows != null && i < rows.Count; i++) if (Read(Read(rows[i], "Item"), "Item") == item) row = i;
            _bench.Check("native_carried_inventory_row", ui.IsOpen && row >= 0 && !(bool)Read(Read(rows[row], "Item"), "IsEquipped"));
            yield return MoveCursor(ui, "_cursorIndex", row); yield return Tap(Key.Enter);
            var popup = Read(ui, "_itemActionPopup"); var actions = Read(popup, "Actions") as IList; int action = -1;
            for (int i = 0; actions != null && i < actions.Count; i++) if ((string)Read(actions[i], "Command") == command) action = i;
            _bench.Check("native_inventory_action_" + command, Read(popup, "Item") == item && action >= 0);
            yield return MoveCursor(popup, "CursorIndex", action); yield return Tap(Key.Enter);
            _bench.Check("native_inventory_action_closed", Read(ui, "_itemActionPopup") == null && ui.IsOpen);
            yield return Tap(Key.Escape);
            _bench.Check("native_inventory_exit", State(input) == "Normal");
        }
        private IEnumerator OpenTrade(InputHandler input)
        {
            yield return Tap(Key.C); yield return Tap(Key.RightArrow);
            _bench.Check("native_merchant_target", input.WorldActionMenuUI.SelectedTarget == _bench.Merchant);
            yield return MenuAction(input, "Chat");
            _bench.Check("native_merchant_dialogue", input.DialogueUI.IsOpen && ConversationManager.Speaker == _bench.Merchant);
            if ((bool)Read(input.DialogueUI, "_revealing"))
            {
                string node = ConversationManager.CurrentNode.ID; yield return Tap(Key.Enter);
                _bench.Check("native_merchant_reveal_only", ConversationManager.CurrentNode.ID == node && !(bool)Read(input.DialogueUI, "_revealing"));
            }
            int choice = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Actions != null && c.Actions.Any(a => a.Key == "StartTrade"));
            if (choice < 0) throw new InvalidOperationException("Authored merchant trade choice missing.");
            yield return MoveCursor(input.DialogueUI, "_cursorIndex", choice); yield return Tap(Key.Enter);
            _bench.Check("native_trade_open", input.TradeUI.IsOpen && Read(input.TradeUI, "_trader") == _bench.Merchant);
        }
        private IEnumerator MenuAction(InputHandler input, string command)
        {
            var menu = input.WorldActionMenuUI; var actions = Read(menu, "_actions") as List<InventoryAction>;
            int row = actions?.FindIndex(a => a.Command == command) ?? -1;
            _bench.Check("native_menu_" + command.Split(':')[0], menu.IsOpen && row >= 0);
            yield return MoveCursor(menu, "_cursorIndex", row); yield return Tap(Key.Enter);
        }
        private IEnumerator TradeItem(InputHandler input, Entity item, bool sell)
        {
            var ui = input.TradeUI; int panel = sell ? 1 : 0;
            if ((int)Read(ui, "_panel") != panel) yield return Tap(Key.Tab);
            var rows = Read(ui, sell ? "_rightRows" : "_leftRows") as IList; int row = -1;
            for (int i = 0; rows != null && i < rows.Count; i++) if (Read(rows[i], "Item") == item) row = i;
            _bench.Check("native_trade_item_row", ui.IsOpen && (int)Read(ui, "_panel") == panel && row >= 0);
            yield return MoveCursor(ui, sell ? "_rightCursor" : "_leftCursor", row); yield return Tap(Key.Enter);
            _bench.Check("native_trade_confirmation", (bool)Read(ui, "_confirmActive") && (int)Read(ui, "_confirmPanel") == panel
                && (int)Read(ui, "_confirmIndex") == row && (int)Read(ui, "_confirmChoice") == 0);
            yield return Tap(Key.Enter);
        }
        private IEnumerator MoveCursor(object ui, string field, int row)
        {
            for (int i = 0; i < 100; i++)
            {
                int cursor = (int)Read(ui, field); if (cursor == row) yield break;
                yield return Tap(cursor < row ? Key.DownArrow : Key.UpArrow);
                if ((int)Read(ui, field) == cursor) throw new InvalidOperationException("Native cursor did not move toward intended row.");
            }
            throw new InvalidOperationException("Native cursor did not reach intended row.");
        }
        private int Outcomes(string kind, string target, string payload) => DiagQuery.Apply(new DiagQuery.Filter
            { Kind = kind, Actor = _ctx.PlayerEntity.ID, Target = target, Limit = 500 }).Records.Count(e => e.PayloadJson != null && e.PayloadJson.Contains(payload));
        private IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key)); yield return new WaitForSecondsRealtime(0.06f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return new WaitForSecondsRealtime(0.12f);
        }
        private static object Read(object owner, string field) => owner?.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(owner);
        private static string State(InputHandler input) => Read(input, "_inputState")?.ToString();
        private void Finish()
        {
            Failures = Math.Max(Failures, _bench.Failures);
            var report = new Report { runId = _bench.RunId, seconds = Time.realtimeSinceStartupAsDouble - _started,
                cases = _bench.Cases, failures = Failures, audit = _bench.Audit.ToArray() };
            string directory = Path.Combine(Application.dataPath, "../Docs/Verification/GameSystemAudit"); Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "GA02d-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditAcquisitionBench] " + JsonUtility.ToJson(report)); Finished = true;
        }
        private void OnDestroy()
        {
            Diag.SetChannel("event", _oldEvents);
            Diag.SetChannel("trade", _oldTrade);
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;
        }
        [Serializable] private sealed class Report
        { public string runId; public double seconds; public int cases, failures; public string[] audit; }
    }
}
