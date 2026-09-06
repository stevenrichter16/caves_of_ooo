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
    public sealed class GameAuditTransferBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditTransferBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;
        private bool _oldEvents, _oldTrade;

        public void Initialize(ScenarioContext ctx, GameAuditTransferBench bench)
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
            var body = _ctx.PlayerEntity.GetPart<Body>();
            var equippedParts = body.GetParts().Where(p => p._Equipped == _bench.EquippedDagger).ToArray();
            var container = _bench.Sack.GetPart<ContainerPart>(); var shelf = _bench.Merchant.GetPart<InventoryPart>();
            int originalIndex = inv.Objects.IndexOf(_bench.CarriedDagger);
            _bench.Check("real_capacity_and_identity_preconditions", originalIndex >= 0 && container.Contents.Count == 6
                && container.MaxItems == 6 && !_bench.Sack.GetPart<PhysicsPart>().Solid && shelf.MaxWeight == 150
                && shelf.GetCarriedWeight() == 150 && _bench.CarriedDagger != _bench.EquippedDagger
                && _bench.CarriedDagger.GetPart<StackerPart>().StackCount == 2 && _bench.EquippedDagger.GetPart<StackerPart>().StackCount == 1
                && equippedParts.Length > 0);
            int refused = Outcomes("ItemDispositionRejected", _bench.EquippedDagger.ID, "PutInContainer");
            yield return PutEquipped(input, false);
            _bench.Check("native_full_sack_refusal_dispatched", Outcomes("ItemDispositionRejected", _bench.EquippedDagger.ID, "PutInContainer") == refused + 1);
            _bench.Check("native_refusal_preserves_equipment_and_stack", inv.Objects[originalIndex] == _bench.CarriedDagger
                && _bench.CarriedDagger.GetPart<StackerPart>().StackCount == 2 && _bench.EquippedDagger.GetPart<StackerPart>().StackCount == 1
                && InventorySystem.IsEquipped(_ctx.PlayerEntity, _bench.EquippedDagger)
                && equippedParts.All(p => p._Equipped == _bench.EquippedDagger)
                && _bench.EquippedDagger.GetPart<PhysicsPart>().Equipped == _ctx.PlayerEntity
                && _bench.EquippedDagger.GetPart<PhysicsPart>().InInventory == null && container.Contents.Count == 6
                && _ctx.Zone.GetEntityCell(_bench.EquippedDagger) == null);
            yield return Tap(Key.Escape); // Close retained failure popup.
            yield return Tap(Key.Escape); // Then leave inventory.
            _bench.Check("native_inventory_refusal_exit", State(input) == "Normal");

            // Player plus Sack makes an actual pile. Reach its object menu through the normal picker.
            yield return Tap(Key.C); yield return Tap(Key.Period);
            yield return MenuAction(input, "PickCell");
            yield return MenuAction(input, "PickTarget:" + _bench.Sack.ID);
            _bench.Check("native_sack_target_selected", input.WorldActionMenuUI.SelectedTarget == _bench.Sack);
            yield return MenuAction(input, "OpenContainer");
            var pickup = input.PickupUI;
            var items = Read(pickup, "_items") as List<Entity>;
            int fillerIndex = items?.IndexOf(_bench.Filler) ?? -1;
            _bench.Check("native_container_loot_menu", pickup.IsOpen && Read(pickup, "_sourceContainer") == _bench.Sack && fillerIndex >= 0);
            yield return MoveCursor(pickup, "_cursorIndex", fillerIndex);
            yield return Tap(Key.Enter);
            _bench.Check("native_loot_frees_one_slot", pickup.PickedUpAny && inv.Objects.Contains(_bench.Filler)
                && !container.Contents.Contains(_bench.Filler) && container.Contents.Count == 5);
            yield return Tap(Key.Escape);
            _bench.Check("native_loot_exit", State(input) == "Normal");
            yield return PutEquipped(input, true);
            _bench.Check("native_put_succeeds_after_freeing_slot", container.Contents.Count == 6
                && container.Contents.Contains(_bench.EquippedDagger) && !InventorySystem.IsEquipped(_ctx.PlayerEntity, _bench.EquippedDagger)
                && body.GetParts().All(p => p._Equipped != _bench.EquippedDagger)
                && !inv.Contains(_bench.EquippedDagger) && _bench.EquippedDagger.GetPart<PhysicsPart>().InInventory == _bench.Sack
                && _bench.EquippedDagger.GetPart<PhysicsPart>().Equipped == null
                && inv.Objects[originalIndex] == _bench.CarriedDagger && _bench.CarriedDagger.GetPart<StackerPart>().StackCount == 2);
            yield return Tap(Key.Escape);

            yield return Tap(Key.C); yield return Tap(Key.RightArrow);
            _bench.Check("native_merchant_target", input.WorldActionMenuUI.SelectedTarget == _bench.Merchant);
            yield return MenuAction(input, "Chat");
            _bench.Check("native_merchant_dialogue", input.DialogueUI.IsOpen && ConversationManager.Speaker == _bench.Merchant);
            if ((bool)Read(input.DialogueUI, "_revealing"))
            {
                string node = ConversationManager.CurrentNode.ID; yield return Tap(Key.Enter);
                _bench.Check("native_merchant_reveal_only", ConversationManager.CurrentNode.ID == node && !(bool)Read(input.DialogueUI, "_revealing"));
            }
            int tradeChoice = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Actions != null && c.Actions.Any(a => a.Key == "StartTrade"));
            if (tradeChoice < 0) throw new InvalidOperationException("Actual merchant trade choice missing.");
            yield return MoveCursor(input.DialogueUI, "_cursorIndex", tradeChoice); yield return Tap(Key.Enter);
            var trade = input.TradeUI;
            _bench.Check("native_trade_open", trade.IsOpen && Read(trade, "_trader") == _bench.Merchant);
            int playerBefore = TradeSystem.GetDrams(_ctx.PlayerEntity), traderBefore = TradeSystem.GetDrams(_bench.Merchant);
            int saleRefusals = Outcomes("SaleRejected", _bench.Merchant.ID, _bench.CarriedDagger.ID);
            yield return TradeItem(input, _bench.CarriedDagger, sell: true);
            _bench.Check("native_sale_refusal_dispatched", Outcomes("SaleRejected", _bench.Merchant.ID, _bench.CarriedDagger.ID) == saleRefusals + 1
                && (string)Read(trade, "_statusMessage") == "The trader cannot carry that much weight.");
            _bench.Check("native_sale_refusal_keeps_item_and_wallets", inv.Objects.Contains(_bench.CarriedDagger)
                && _bench.CarriedDagger.GetPart<StackerPart>().StackCount == 2 && shelf.GetCarriedWeight() == 150
                && TradeSystem.GetDrams(_ctx.PlayerEntity) == playerBefore && TradeSystem.GetDrams(_bench.Merchant) == traderBefore);
            int buyPrice = TradeSystem.GetBuyPrice(_bench.SmallStock, TradeSystem.GetTradePerformance(_ctx.PlayerEntity), _bench.Merchant);
            yield return TradeItem(input, _bench.SmallStock, sell: false);
            _bench.Check("native_purchase_frees_trader_capacity", inv.Objects.Contains(_bench.SmallStock)
                && _bench.SmallStock.GetPart<StackerPart>().StackCount == 51 && shelf.GetCarriedWeight() == 99
                && TradeSystem.GetDrams(_ctx.PlayerEntity) == playerBefore - buyPrice && TradeSystem.GetDrams(_bench.Merchant) == traderBefore + buyPrice);
            int salePrice = TradeSystem.GetSellPrice(_bench.CarriedDagger, TradeSystem.GetTradePerformance(_ctx.PlayerEntity), _bench.Merchant);
            int sales = Outcomes("Sold", _bench.Merchant.ID, _bench.CarriedDagger.ID);
            yield return TradeItem(input, _bench.CarriedDagger, sell: true);
            _bench.Check("native_sale_delivers_and_pays_once", !inv.Contains(_bench.CarriedDagger) && shelf.Objects.Contains(_bench.CarriedDagger)
                && _bench.CarriedDagger.GetPart<StackerPart>().StackCount == 2 && shelf.GetCarriedWeight() == 107
                && _bench.CarriedDagger.GetPart<PhysicsPart>().InInventory == _bench.Merchant
                && TradeSystem.GetDrams(_ctx.PlayerEntity) == playerBefore - buyPrice + salePrice
                && TradeSystem.GetDrams(_bench.Merchant) == traderBefore + buyPrice - salePrice
                && Outcomes("Sold", _bench.Merchant.ID, _bench.CarriedDagger.ID) == sales + 1 && Read(trade, "_statusMessage") == null);
            yield return Tap(Key.Escape);
            _bench.Check("native_trade_exit", State(input) == "Normal" && !trade.IsOpen);
        }
        private IEnumerator PutEquipped(InputHandler input, bool expectedSuccess)
        {
            yield return Tap(Key.I); yield return Tap(Key.Tab);
            var ui = input.InventoryUI; var rows = Read(ui, "_rows") as IList;
            int row = -1;
            for (int i = 0; rows != null && i < rows.Count; i++)
                if (Read(Read(rows[i], "Item"), "Item") == _bench.EquippedDagger) row = i;
            _bench.Check("native_equipped_inventory_row", ui.IsOpen && row >= 0 && (bool)Read(Read(rows[row], "Item"), "IsEquipped"));
            yield return MoveCursor(ui, "_cursorIndex", row); yield return Tap(Key.Enter);
            var popup = Read(ui, "_itemActionPopup"); var actions = Read(popup, "Actions") as IList; int action = -1;
            for (int i = 0; actions != null && i < actions.Count; i++)
                if ((string)Read(actions[i], "Command") == "put_container" && Read(actions[i], "Container") == _bench.Sack) action = i;
            _bench.Check("native_put_action_available", Read(popup, "Item") == _bench.EquippedDagger && action >= 0);
            yield return MoveCursor(popup, "CursorIndex", action); yield return Tap(Key.Enter);
            _bench.Check("native_put_popup_outcome", ui.IsOpen && (expectedSuccess
                ? Read(ui, "_itemActionPopup") == null && string.IsNullOrEmpty((string)Read(ui, "_actionStatus"))
                : ReferenceEquals(Read(ui, "_itemActionPopup"), popup) && Read(popup, "Item") == _bench.EquippedDagger
                    && (int)Read(popup, "CursorIndex") == action && (string)Read(ui, "_actionStatus") == "Container is full."));
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
            File.WriteAllText(Path.Combine(directory, "FLOW2-transfer-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditTransferBench] " + JsonUtility.ToJson(report)); Finished = true;
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
