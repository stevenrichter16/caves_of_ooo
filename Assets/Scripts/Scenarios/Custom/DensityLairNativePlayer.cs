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
    /// <summary>Finite native keyboard evidence for persistent lairs. One labelled surface
    /// start shortcut preserves the ordinary new-game actor. Every later move and
    /// interaction uses native keys in an isolated disposable save root.</summary>
    public sealed class DensityLairNativePlayer : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public string RunId { get; } = Guid.NewGuid().ToString("N");
        public bool Finished { get; private set; }
        public int Failures => _failures + _unexpectedErrors;
        public string ReportPath { get; private set; }
        private readonly List<string> _audit = new List<string>();
        private readonly List<string> _screenshots = new List<string>();
        private readonly List<Description> _descriptions = new List<Description>();
        private InputHandler _input;
        private ScenarioContext _context;
        private Zone _stagedZone;
        private Keyboard _keyboard, _oldKeyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground, _oldScenario, _cleaned, _errorsFinalized, _summaryEmitted;
        private int _failures, _unexpectedErrors;
        private string _ownedRoot, _fatal;
        private System.Diagnostics.Stopwatch _clock;
        private const string SurfaceID = "Overworld.10.17.0";
        private bool _usedStartShortcut;
        private int _keyboardMoves, _startingTonicsUsed;
        private string _bossID, _rewardID, _bossGearBeforeLoad;
        private int _bossHPBeforeLoad;
        private string _savePath, _checkpointHash;
        private readonly List<NativeKeyStep> _nativeKeys = new List<NativeKeyStep>();
        private NativeState _failureState;
        [Serializable] private sealed class NativeState
        {
            public string label, state, zone; public int hp, maxHp, x, y, tick, energy;
            public bool deathHandled, blockingFx; public string[] lastMessages;
        }
        [Serializable] private sealed class NativeKeyStep
        { public string keys; public int sequence; public NativeState before, after; }
        private NativeState Snapshot(string label)
        {
            var actor = _input?.PlayerEntity; var zone = _input?.CurrentZone;
            var cell = actor == null ? null : zone?.GetEntityCell(actor);
            return new NativeState { label = label, state = _input == null ? "uninitialized" : State(), zone = zone?.ZoneID,
                hp = actor?.GetStatValue("Hitpoints") ?? -1, maxHp = actor?.GetStat("Hitpoints")?.Max ?? -1,
                x = cell?.X ?? -1, y = cell?.Y ?? -1, tick = _input?.TurnManager?.TickCount ?? -1,
                energy = actor == null || _input?.TurnManager == null ? -1 : _input.TurnManager.GetEnergy(actor),
                deathHandled = actor != null && CombatSystem.IsDeathHandled(actor),
                blockingFx = _input?.ZoneRenderer?.WorldFx?.HasBlockingFx == true,
                lastMessages = MessageLog.GetRecentEntries(12).Select(e => e.Text).ToArray() };
        }
        private string RuntimeDetails()
        {
            var state = Snapshot("precondition");
            return "state=" + state.state + "; HP=" + state.hp + "/" + state.maxHp
                + "; dead=" + state.deathHandled + "; zone=" + state.zone + "; cell=" + state.x + "," + state.y
                + "; FX=" + state.blockingFx + "; messages=" + string.Join(" | ", state.lastMessages);
        }
        private IEnumerator WaitForNativeFx()
        {
            double began = Time.realtimeSinceStartupAsDouble;
            while (_input != null && (State() == "WaitingForFxResolution" || _input.ZoneRenderer?.WorldFx?.HasBlockingFx == true))
            {
                Require(_input.PlayerEntity.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(_input.PlayerEntity), "actor died while native FX resolved; " + RuntimeDetails());
                Require(Time.realtimeSinceStartupAsDouble - began < 8, "native FX did not settle; " + RuntimeDetails());
                yield return null;
            }
        }
        private static readonly string[] RequiredChecks =
        {
            "ordinary_player_and_starting_kit", "starting_dagger_equipped_by_keyboard",
            "single_labelled_start_shortcut", "surface_stairs_and_no_early_boss",
            "keyboard_descends_to_intermediate", "one_final_boss_and_claimed_reward",
            "boss_examine_through_native_menu", "reward_taken_by_keyboard",
            "checkpoint_saved_by_keyboard", "checkpoint_mutation_by_keyboard",
            "native_save_load_preserves_real_owners_and_depletion", "keyboard_returns_to_surface",
            "keyboard_revisit_never_refills_or_duplicates", "ordinary_living_actor_after_route"
        };
        private readonly List<string> _routeZones = new List<string>();
        private readonly List<string> _transcript = new List<string>();
        private OverworldZoneManager Manager => (OverworldZoneManager)_input.ZoneManager;
        private LairStackRecord Plan => LairStacks.Inspect(Manager, SurfaceID);
        private string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath,
            "../Docs/Verification/DensityCompletion/Underground/LairStacks/Native", RunId));

        public void Initialize(ScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Completion audit requires its isolated native launcher.");
            _context = context; _ownedRoot = SaveGameService.SaveRootOverride;
            _clock = System.Diagnostics.Stopwatch.StartNew();
            _oldScenario = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            _oldKeyboard = Keyboard.current; _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings);
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _settings;
            _oldBackground = Application.runInBackground; Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            StartCoroutine(RunSafely(RunAudit()));
        }

        private IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);
            _input = FindFirstObjectByType<InputHandler>();
            Require(_input != null, "native input");
            var boot = (BootMenuController)Field(_input, "_bootMenuController");
            Require(boot != null && boot.IsActive, "isolated new-game menu");
            yield return Tap(Key.N);
            Require(State() == "Normal", "new game enters normal input");
            Check("ordinary_player_and_starting_kit", !DevMode.Enabled
                && !_input.PlayerEntity.HasPart<BitLockerPart>()
                && _input.PlayerEntity.GetStat("Hitpoints").Max == 40
                && _input.PlayerEntity.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == "Dagger"));
            var dagger = _input.PlayerEntity.GetPart<InventoryPart>().Objects.First(e => e.BlueprintName == "Dagger");
            yield return ItemAction(dagger, "equip_auto");
            if (State() == "InventoryOpen") yield return Tap(Key.I);
            Require(State() == "Normal", "equipment menu closes");
            Check("starting_dagger_equipped_by_keyboard", dagger.GetPart<PhysicsPart>().Equipped == _input.PlayerEntity);
            var poi = Manager.WorldMap.GetPOI(10, 17);
            Require(poi != null && poi.Type == POIType.Lair && poi.Tier == 3
                && Manager.WorldMap.Tiles[10, 17] == BiomeType.Beating, "fixed seed64 actual Beating lair");
            _stagedZone = Manager.GetZone(SurfaceID);
            Require(_stagedZone != null && Plan != null && !Plan.Legacy && Plan.FinalDepth == 2,
                "real generated two-level lair stack");
            PositionAtStartOnly(_stagedZone);
            Check("single_labelled_start_shortcut", _usedStartShortcut && _input.CurrentZone.ZoneID == SurfaceID);
            Check("surface_stairs_and_no_early_boss", _input.CurrentZone.GetReadOnlyEntities().Any(e => e.HasPart<StairsDownPart>())
                && !_input.CurrentZone.GetReadOnlyEntities().Any(e => e.BlueprintName == poi.BossBlueprint));
            yield return Capture("01-generated-beating-surface");
            yield return Traverse(true);
            Check("keyboard_descends_to_intermediate", _input.CurrentZone.ZoneID == "Overworld.10.17.1"
                && !_input.CurrentZone.GetReadOnlyEntities().Any(e => e.BlueprintName == poi.BossBlueprint));
            yield return Capture("02-intermediate-floor");
            yield return Traverse(true);
            Require(_input.CurrentZone.ZoneID == "Overworld.10.17.2", "final floor reached by keyboard");
            _bossID = Plan.BossID; _rewardID = Plan.RewardID;
            var boss = CurrentOwner(_bossID); var cache = CurrentOwner(_rewardID);
            Check("one_final_boss_and_claimed_reward", boss != null && cache?.GetPart<ContainerPart>() != null
                && _input.CurrentZone.GetReadOnlyEntities().Count(e => e.ID == _bossID) == 1
                && !_input.CurrentZone.GetReadOnlyEntities().Any(e => e.HasPart<StairsDownPart>()));
            Require(boss != null && cache != null, "actual final owners");
            int beforeExamine = MessageLog.GetRecentEntries(1).LastOrDefault().Serial;
            yield return WorldAction(boss, "Examine");
            string name = boss.GetDisplayName();
            var messages = MessageLog.GetRecentEntries(16).Where(e => e.Serial > beforeExamine).Select(e => e.Text).ToList();
            Check("boss_examine_through_native_menu", messages.Any(m => m.StartsWith("You see", StringComparison.OrdinalIgnoreCase)
                && m.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0));
            _transcript.AddRange(messages);
            yield return Capture("03-final-boss-examine");
            yield return CloseToNormal();
            yield return ProtectRouteWithStartingControl();
            yield return WalkIntoReach(cache);
            int stock = cache.GetPart<ContainerPart>().Contents.Count;
            Require(stock > 0, "actual stocked reward before taking");
            yield return WorldAction(cache, "OpenContainer");
            Require(State() == "PickupOpen", "native reward pickup menu");
            yield return Capture("04-final-reward-menu");
            yield return Tap(Key.Tab);
            yield return CloseToNormal();
            Check("reward_taken_by_keyboard", cache.GetPart<ContainerPart>().Contents.Count == 0);
            boss = CurrentOwner(_bossID);
            Require(boss != null, "living owner available for save identity observation");
            _bossHPBeforeLoad = boss.GetStat("Hitpoints").Value;
            _bossGearBeforeLoad = GearIdentity(boss);
            string inventory = GearIdentity(_input.PlayerEntity);
            var atSave = Cell(); int sx = atSave.X, sy = atSave.Y;
            var checkpoint = SaveGameService.GetSaveInfo("Quick");
            Require(checkpoint != null, "ordinary new-game checkpoint metadata");
            string gameID = checkpoint.GameID;
            _savePath = Path.Combine(_ownedRoot, gameID, "Quick.sav.gz");
            string previousHash = HashFile(_savePath);
            long serial = MessageLog.NextSerialValue;
            int savedTick = _input.TurnManager.TickCount;
            int savedEnergy = _input.TurnManager.GetEnergy(_input.PlayerEntity);
            int savedHP = _input.PlayerEntity.GetStatValue("Hitpoints");
            string savedPlayerID = _input.PlayerEntity.ID;
            yield return Tap(Key.F5);
            _checkpointHash = HashFile(_savePath);
            Check("checkpoint_saved_by_keyboard", MessageLog.GetLast() == "Game saved."
                && MessageLog.NextSerialValue > serial && _checkpointHash != previousHash
                && SaveGameService.GetSaveInfo("Quick")?.GameID == gameID
                && SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID == _input.CurrentZone.ZoneID);
            // A legal one-cell native move changes the live graph after the save.
            // No transfer, state rewrite, idle-loop or fake checkpoint is used.
            var mutation = CheckpointStep(sx, sy);
            Require(mutation != null, "safe adjacent checkpoint-proof step");
            yield return Tap(Direction(mutation.X - sx, mutation.Y - sy));
            _keyboardMoves++;
            Check("checkpoint_mutation_by_keyboard", Cell().X == mutation.X && Cell().Y == mutation.Y
                && (Cell().X != sx || Cell().Y != sy)
                && _input.TurnManager.GetEnergy(_input.PlayerEntity) == savedEnergy - TurnManager.ActionThreshold
                    + (_input.TurnManager.TickCount - savedTick) * _input.PlayerEntity.GetStatValue("Speed", TurnManager.DefaultSpeed)
                && HashFile(_savePath) == _checkpointHash);
            var oldPlayer = _input.PlayerEntity;
            yield return Tap(Key.F6);
            double loadBegan = Time.realtimeSinceStartupAsDouble;
            while (ReferenceEquals(oldPlayer, _input.PlayerEntity))
            {
                Require(Time.realtimeSinceStartupAsDouble - loadBegan < 8, "F6 replaces the saved player graph");
                yield return null;
            }
            yield return CloseToNormal();
            boss = CurrentOwner(_bossID); cache = CurrentOwner(_rewardID);
            Check("native_save_load_preserves_real_owners_and_depletion", boss != null && cache != null
                && Plan.BossID == _bossID && Plan.RewardID == _rewardID
                && boss.GetStat("Hitpoints").Value == _bossHPBeforeLoad && GearIdentity(boss) == _bossGearBeforeLoad
                && cache.GetPart<ContainerPart>().Contents.Count == 0 && GearIdentity(_input.PlayerEntity) == inventory
                && _input.PlayerEntity.ID == savedPlayerID && _input.PlayerEntity.GetStatValue("Hitpoints") == savedHP
                && _input.TurnManager.TickCount == savedTick && _input.TurnManager.GetEnergy(_input.PlayerEntity) == savedEnergy
                && HashFile(_savePath) == _checkpointHash && Cell().X == sx && Cell().Y == sy);
            yield return Capture("05-loaded-final-floor");
            yield return Traverse(false);
            Require(_input.CurrentZone.ZoneID == "Overworld.10.17.1", "keyboard ascends intermediate");
            yield return Traverse(false);
            Check("keyboard_returns_to_surface", _input.CurrentZone.ZoneID == SurfaceID);
            yield return Capture("06-returned-surface");
            yield return Traverse(true);
            yield return Traverse(true);
            Check("keyboard_revisit_never_refills_or_duplicates", _input.CurrentZone.ZoneID == "Overworld.10.17.2"
                && Plan.BossID == _bossID && Plan.RewardID == _rewardID
                && CurrentOwner(_rewardID)?.GetPart<ContainerPart>().Contents.Count == 0
                && _input.CurrentZone.GetReadOnlyEntities().Count(e => e.ID == _rewardID) == 1
                && _input.CurrentZone.GetReadOnlyEntities().Count(e => e.ID == _bossID) <= 1);
            Check("ordinary_living_actor_after_route", State() == "Normal" && !CombatSystem.IsDeathHandled(_input.PlayerEntity)
                && _input.PlayerEntity.GetStat("Hitpoints").Value > 0 && _input.PlayerEntity.GetStat("Hitpoints").Max == 40
                && !_input.PlayerEntity.HasPart<BitLockerPart>() && _keyboardMoves > 0);
            yield return Capture("07-revisited-final-floor");
        }

        // One declared scenario start shortcut. No actor transfer is used again.
        // All subsequent movement, stairs, examination, looting and saving use keys.
        private void PositionAtStartOnly(Zone zone)
        {
            Require(!_usedStartShortcut && _routeZones.Count == 0 && State() == "Normal", "only one startup shortcut");
            var stairs = zone.GetReadOnlyEntities().Single(e => e.HasPart<StairsDownPart>());
            var cell = zone.GetEntityCell(stairs);
            Require(cell != null && !cell.BlocksMovement(_input.PlayerEntity), "legal real stair starting cell");
            var old = _input.CurrentZone;
            Require(old.TryTransferEntityTo(_input.PlayerEntity, zone, cell.X, cell.Y), "start shortcut preserves ordinary actor");
            typeof(InputHandler).GetMethod("HandleZoneTransition", Private).Invoke(_input, new object[] {
                new ZoneTransitionResult { Success = true, NewZone = zone, NewPlayerX = cell.X, NewPlayerY = cell.Y } });
            _usedStartShortcut = true;
            _routeZones.Add(zone.ZoneID);
            _descriptions.Add(new Description { subject = "Scenario start shortcut", source = "actual generated seed64 Beating lair",
                text = "Ordinary new-game actor transferred once to the real surface down stair at " + zone.ZoneID + ":" + cell.X + "," + cell.Y + ". No later transfers or stat/kit changes." });
            _input.CameraFollow?.SnapToPlayer(); ZoneRenderHooks.MarkFullDirty("DensityLairAuditStart");
        }

        private IEnumerator Traverse(bool down)
        {
            Require(State() == "Normal", "normal input before stairs");
            var stair = _input.CurrentZone.GetReadOnlyEntities().Single(e => down ? e.HasPart<StairsDownPart>() : e.HasPart<StairsUpPart>());
            var cell = _input.CurrentZone.GetEntityCell(stair);
            yield return WalkTo(cell.X, cell.Y);
            string previous = _input.CurrentZone.ZoneID;
            yield return Tap(Key.LeftShift, down ? Key.Period : Key.Comma);
            Require(State() == "Normal" && _input.CurrentZone.ZoneID != previous, "actual keyboard stair transition");
            _routeZones.Add(_input.CurrentZone.ZoneID);
        }

        private bool IsActiveVisibleThreat(Entity entity)
        {
            return entity != _input.PlayerEntity && entity.HasTag("Creature")
                && entity.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(entity)
                && FactionManager.IsHostile(entity, _input.PlayerEntity)
                && entity.GetPart<BrainPart>()?.HasGoal<NoFightGoal>() != true
                && entity.GetEffect<FrozenEffect>()?.Cold > 0 != true
                && _input.CurrentZone.GetOccupiedCells(entity).Any(c => c.IsVisible);
        }
        private ActivatedAbility ReadyAbility(string command, out int slot)
        {
            var abilities = _input.PlayerEntity.GetPart<ActivatedAbilitiesPart>();
            for (int i = 0; i < ActivatedAbilitiesPart.SlotCount; i++)
            {
                var ability = abilities?.GetAbilityBySlot(i);
                if (ability?.Command == command && ability.CooldownRemaining == 0)
                { slot = i; return ability; }
            }
            slot = -1; return null;
        }
        private bool ClearCastLine(Entity target, int range, out int dx, out int dy)
        {
            dx = dy = 0; var from = Cell();
            foreach (var at in _input.CurrentZone.GetOccupiedCells(target))
            {
                int x = at.X - from.X, y = at.Y - from.Y;
                if (x != 0 && y != 0 && Math.Abs(x) != Math.Abs(y)) continue;
                if (Math.Max(Math.Abs(x), Math.Abs(y)) > range) continue;
                int tx = Math.Sign(x), ty = Math.Sign(y);
                if (tx == 0 && ty == 0) continue;
                // Read the same physical first-impact rules without starting any
                // capture or applying an ability. The actual cast must verify its
                // cooldown and effect afterward; blocked rays are never forced.
                Entity first = null;
                for (int n = 1; n <= range; n++)
                {
                    var cell = _input.CurrentZone.GetCell(from.X + tx * n, from.Y + ty * n);
                    if (cell == null) break;
                    first = cell.Occupants.FirstOrDefault(e => AbilityTargeting.IsCreatureTarget(e, _input.PlayerEntity))
                        ?? cell.Occupants.FirstOrDefault(e => e != null && !e.HasTag("Creature") && AbilityTargeting.IsElementalTarget(e, _input.PlayerEntity));
                    if (first != null || cell.IsSolid()) break;
                }
                if (ReferenceEquals(first, target)) { dx = tx; dy = ty; return true; }
            }
            return false;
        }
        private IEnumerator ProtectRouteWithStartingControl()
        {
            // The original dagger-only attempt is retained as a real death.
            // This separate tactic uses only naturally granted ready starter slots.
            var from = Cell();
            var threats = _input.CurrentZone.GetReadOnlyEntities().Where(IsActiveVisibleThreat)
                .OrderByDescending(e => e.HasTag("Boss"))
                .ThenBy(e => SpatialQuery.DistanceToCell(_input.CurrentZone, e, from.X, from.Y)).ToArray();
            foreach (var target in threats)
            foreach (string command in new[] { "CommandCalm", "CommandRimeGrip" })
            {
                int range = command == "CommandCalm" ? 6 : 5;
                var ability = ReadyAbility(command, out int slot);
                if (ability == null || !ClearCastLine(target, range, out int dx, out int dy)) continue;
                int serial = MessageLog.GetRecentEntries(1).LastOrDefault().Serial;
                yield return Tap((Key)Enum.Parse(typeof(Key), slot == 9 ? "Digit0" : "Digit" + (slot + 1)));
                Require(State() == "AwaitingDirection", "actual ready starter asks for direction; " + RuntimeDetails());
                yield return Tap(Direction(dx, dy));
                Require(State() == "Normal" && ability.CooldownRemaining > 0, "native starting control consumes its real cooldown; " + RuntimeDetails());
                bool applied = command == "CommandCalm" ? target.GetPart<BrainPart>()?.HasGoal<NoFightGoal>() == true
                    : target.GetEffect<FrozenEffect>()?.Cold > 0 || CombatSystem.IsDeathHandled(target);
                Require(applied, "native control actually affected intended owner " + target.ID + "; " + RuntimeDetails());
                _descriptions.Add(new Description { subject = "actual starter control", source = target.ID,
                    text = command + " via slot " + slot + "; cooldown=" + ability.CooldownRemaining + "; target=" + target.GetDisplayName() });
                _transcript.AddRange(MessageLog.GetRecentEntries(16).Where(e => e.Serial > serial).Select(e => e.Text));
                yield break;
            }
        }
        private bool SafeRouteCell(int x, int y, Entity[] threats)
        {
            var cells = _input.CurrentZone.GetOccupiedCells(_input.PlayerEntity, x, y);
            if (cells.Count == 0 || cells.Any(c => c == null || c.BlocksMovement(_input.PlayerEntity)
                || c.Occupants.Any(e => e != _input.PlayerEntity && (e.HasTag("Creature") || e.HasPart<TriggerOnStepPart>())))) return false;
            foreach (var threat in threats)
                if (cells.Any(c => SpatialQuery.DistanceToCell(_input.CurrentZone, threat, c.X, c.Y) <= 2)) return false;
            return true;
        }
        private List<Cell> SafeRoute(int x, int y)
        {
            var origin = Cell(); var start = (origin.X, origin.Y); var end = (x, y);
            if (start == end) return new List<Cell>();
            var threats = _input.CurrentZone.GetReadOnlyEntities().Where(IsActiveVisibleThreat).ToArray();
            if (!SafeRouteCell(x, y, threats)) return null;
            var previous = new Dictionary<(int, int), (int, int)> { [start] = start };
            var queue = new Queue<(int, int)>(); queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var at = queue.Dequeue();
                for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var next = (at.Item1 + dx, at.Item2 + dy);
                    if (previous.ContainsKey(next) || !SafeRouteCell(next.Item1, next.Item2, threats)) continue;
                    previous[next] = at;
                    if (next == end)
                    {
                        var path = new List<Cell>(); var p = end;
                        while (p != start) { path.Add(_input.CurrentZone.GetCell(p.x, p.y)); p = previous[p]; }
                        path.Reverse(); return path;
                    }
                    queue.Enqueue(next);
                }
            }
            return null;
        }
        private IEnumerator WalkIntoReach(Entity target)
        {
            yield return ProtectRouteWithStartingControl();
            var from = Cell(); Cell best = null; int cost = int.MaxValue;
            foreach (var owned in _input.CurrentZone.GetOccupiedCells(target))
                for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var cell = _input.CurrentZone.GetCell(owned.X + dx, owned.Y + dy);
                    if (cell == null || cell.BlocksMovement(_input.PlayerEntity) || cell.Objects.Any(e => e.HasPart<TriggerOnStepPart>())) continue;
                    var path = SafeRoute(cell.X, cell.Y);
                    if (path != null && path.Count < cost) { best = cell; cost = path.Count; }
                }
            Require(best != null, "native walking approach to reward");
            yield return WalkTo(best.X, best.Y);
        }

        private Cell CheckpointStep(int x, int y)
        {
            foreach (var direction in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var cell = _input.CurrentZone.GetCell(x + direction.Item1, y + direction.Item2);
                if (cell == null || cell.BlocksMovement(_input.PlayerEntity)
                    || cell.Occupants.Any(e => e.HasTag("Creature") || e.HasPart<TriggerOnStepPart>()
                        || e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>() || e.HasPart<CampfirePart>())
                    || _input.CurrentZone.TileState.Heat(cell.X, cell.Y) > 0
                    || _input.CurrentZone.TileState.Get(cell.X, cell.Y)?.Coatings?.Any(c => c.Turns > 0) == true)
                    continue;
                return cell;
            }
            return null;
        }
        private static string HashFile(string path)
        {
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        private IEnumerator CloseToNormal()
        {
            for (int i = 0; State() != "Normal" && i < 4; i++) yield return Tap(Key.Escape);
            Require(State() == "Normal", "native menus close");
        }
        private Entity CurrentOwner(string id) => _input.CurrentZone.GetReadOnlyEntities().SingleOrDefault(e => e.ID == id);
        private static string GearIdentity(Entity actor)
        {
            var inventory = actor.GetPart<InventoryPart>();
            return inventory == null ? "" : string.Join("|", inventory.Objects.Concat(inventory.EquippedItems.Values).Distinct()
                .Select(e => e.ID + ":" + e.BlueprintName + ":" + (e.GetPart<StackerPart>()?.StackCount ?? 1)).OrderBy(s => s));
        }
        private Cell Cell()=>_input.CurrentZone.GetEntityCell(_input.PlayerEntity);
        private IEnumerator WalkTo(int x,int y)
        {
            for(int step=0;step<160;step++)
            {
                yield return WaitForNativeFx();
                Require(State()=="Normal"&&_input.PlayerEntity.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(_input.PlayerEntity),"live ordinary actor while walking; "+RuntimeDetails());
                var c=Cell();if(c.X==x&&c.Y==y)yield break;
                if (_input.PlayerEntity.GetStat("Hitpoints").Value <= 20 && _startingTonicsUsed < 2)
                {
                    var tonic = _input.PlayerEntity.GetPart<InventoryPart>().Objects.FirstOrDefault(e => e.BlueprintName == "HealingTonic");
                    if (tonic != null)
                    {
                        yield return ItemAction(tonic, "ApplyTonic");
                        yield return CloseToNormal();
                        _startingTonicsUsed++;
                        _transcript.Add("Keyboard used actual starting healing tonic " + _startingTonicsUsed + " of2; no direct health modification.");
                    }
                }
                yield return ProtectRouteWithStartingControl();
                c = Cell();
                var path = SafeRoute(x, y);
                Require(path != null && path.Count > 0, "safe native route without bumping creature bodies; " + RuntimeDetails());
                var next = path[0];
                yield return Tap(Direction(next.X - c.X, next.Y - c.Y)); _keyboardMoves++;
            }
            throw new InvalidOperationException("Finite native walking route exceeded160 steps.");
        }
        private static Key Direction(int x,int y)
        {
            if(x==0)return y<0?Key.W:Key.S;if(y==0)return x<0?Key.A:Key.D;
            return y<0?(x<0?Key.Numpad7:Key.Numpad9):(x<0?Key.Numpad1:Key.Numpad3);
        }
        private IEnumerator WorldAction(Entity target, string command)
        {
            var from = Cell(); var to = SpatialQuery.ClosestCell(_input.CurrentZone, target, from.X, from.Y);
            if (command != "Examine")
                Require(Math.Max(Math.Abs(from.X - to.X), Math.Abs(from.Y - to.Y)) <= 1, "walked into actual action reach");
            yield return Tap(Key.L); Require(State() == "LookMode", "native look");
            var cursor = (WorldCursorState)Field(_input, "_worldCursorState");
            for (int step = 0; cursor.X != to.X || cursor.Y != to.Y; step++)
            {
                Require(step < 40, "bounded keyboard look cursor");
                yield return Tap(Direction(Math.Sign(to.X - cursor.X), Math.Sign(to.Y - cursor.Y)));
            }
            yield return Tap(Key.Enter);
            Require(State() == "WorldActionMenuOpen" && ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, target), "native owner selection");
            var actions = (List<InventoryAction>)Field(_input.WorldActionMenuUI, "_actions");
            int index = actions.FindIndex(a => a.Command == command);
            Require(index >= 0, "native action " + command);
            yield return Tap((Key)Enum.Parse(typeof(Key), MenuShortcutMap.Key(MenuShortcutMap.ForActions(actions)[index]).ToString()));
        }

        private IEnumerator ItemAction(Entity item, string command)
        {
            yield return Tap(Key.I); Require(State() == "InventoryOpen", "native I opens inventory");
            yield return Tap(Key.Tab); Require(InvField<int>("_panel") == 1, "native item list");
            int row = RowIndex(item); Require(row >= 0, "owned item row");
            for (int step = 0; InvField<int>("_cursorIndex") != row; step++)
            { Require(step < 80, "bounded item navigation"); yield return Tap(InvField<int>("_cursorIndex") < row ? Key.DownArrow : Key.UpArrow); }
            yield return Tap(Key.Enter);
            object popup = InvField<object>("_itemActionPopup"); Require(popup != null, "native item actions");
            var actions = ((IList)Field(popup, "Actions")).Cast<object>().ToArray();
            int action = Array.FindIndex(actions, a => (string)Field(a, "Command") == command);
            Require(action >= 0 && action < 9, "native action shortcut " + command + "; available="+string.Join(",",actions.Select(a=>(string)Field(a,"Command"))));
            yield return Tap((Key)Enum.Parse(typeof(Key), ((char)('A' + action)).ToString()));
        }

        private int RowIndex(Entity item)
        {
            var rows = (IList)Field(_input.InventoryUI, "_rows");
            for (int i = 0; i < rows.Count; i++)
                if (ReferenceEquals(((InventoryScreenData.ItemDisplay)Field(rows[i], "Item"))?.Item, item)) return i;
            return -1;
        }
        private T InvField<T>(string name) => (T)Field(_input.InventoryUI, name);
        private string State() => Field(_input, "_inputState").ToString();
        private static object Field(object owner, string name)
        {
            var member = owner.GetType().GetField(name, Private | BindingFlags.Public);
            if (member == null) throw new InvalidOperationException("Missing observed field " + owner.GetType().Name + "." + name);
            return member.GetValue(owner);
        }
        private IEnumerator Tap(params Key[] keys)
        {
            Require(_clock.Elapsed.TotalSeconds < 240, "finite native content-completion deadline");
            double began = Time.realtimeSinceStartupAsDouble;
            while (_input != null && Time.time - (float)Field(_input, "_lastMoveTime") < _input.MoveRepeatDelay)
            { Require(Time.realtimeSinceStartupAsDouble - began < 3, "input rate gate reopens"); yield return null; }
            yield return WaitForNativeFx();
            var action = new NativeKeyStep { sequence = _nativeKeys.Count, keys = string.Join("+", keys.Select(k => k.ToString())), before = Snapshot("before-key") };
            _nativeKeys.Add(action);
            _keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null;
            yield return new WaitForSecondsRealtime(.13f);
            action.after = Snapshot("after-key"); WriteReport();
            yield return WaitForNativeFx();
            action.after = Snapshot("after-native-fx"); WriteReport();
            if (_input?.PlayerEntity != null)
                Require(_input.PlayerEntity.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(_input.PlayerEntity), "actor died after native key; " + RuntimeDetails());
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.15f); yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(DirectoryPath);
            string path = Path.Combine(DirectoryPath, name + ".png");
            DensityNativeScreenshot.CaptureToFile(path);
            Require(File.Exists(path) && new FileInfo(path).Length > 0, "screenshot " + name);
            _screenshots.Add(path);
        }
        private static void Require(bool condition, string reason)
        { if (!condition) throw new InvalidOperationException("Density completion precondition: " + reason); }
        private void Check(string name, bool passed)
        {
            if (!passed) _failures++;
            _audit.Add((passed ? "PASS " : "FAIL ") + name);
            Diag.Record("scenario", "DensityLairNativeCase", payload: new { runId = RunId, name, passed });
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception error = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception caught) { error = caught; }
                if (error != null) { _failureState = Snapshot("failure"); _fatal = error.ToString(); Check("native_precondition_failed", false); Debug.LogError("[DensityLairNative] " + error); break; }
                if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            Finish();
        }
        public void SetUnexpectedErrors(int errors)
        { _unexpectedErrors = errors; _errorsFinalized = true; WriteReport(); EmitSummary(); }
        public void Abort(string reason)
        { if (Finished) return; StopAllCoroutines(); _fatal = reason; Check("native_aborted", false); Finish(); }
        private void Finish() { Cleanup(); Finished = true; WriteReport(); }
        private void EmitSummary()
        {
            if (_summaryEmitted) return; _summaryEmitted = true;
            Diag.Record("scenario", "DensityLairNativeSummary", payload: new
            { runId = RunId, cases = _audit.Count, failures = Failures, complete = Complete, errorsFinalized = _errorsFinalized, screenshots = _screenshots.Count });
        }
        private bool Complete => Finished && _errorsFinalized && Failures == 0 && _audit.Count == RequiredChecks.Length
            && RequiredChecks.All(name => _audit.Contains("PASS " + name)) && _screenshots.Count >= 7;
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath); ReportPath = Path.Combine(DirectoryPath, "report.json");
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report
            {
                runId = RunId, cases = _audit.Count, failures = Failures, unexpectedErrors = _unexpectedErrors,
                complete = Complete, errorsFinalized = _errorsFinalized, seconds = _clock?.Elapsed.TotalSeconds ?? 0,
                keyboardMoves = _keyboardMoves, startingTonicsUsed = _startingTonicsUsed, routeZones = _routeZones.ToArray(), transcript = _transcript.ToArray(),
                startShortcut = _usedStartShortcut, bossID = _bossID, rewardID = _rewardID, checkpointHash = _checkpointHash,
                zone = _input?.CurrentZone?.ZoneID, failureState = _failureState, nativeKeys = _nativeKeys.ToArray(), fatal = _fatal, audit = _audit.ToArray(), screenshots = _screenshots.ToArray(), descriptions = _descriptions.ToArray(),
                canVerify = "Actual seed64 Beating lair; ordinary40HP starting actor and equipped dagger; keyboard descent through intermediate/final floors, boss examination, real reward collection, verified changed F5 checkpoint, real post-save keyboard move, F6 graph replacement and owner/gear/HP/energy/depletion restoration, ascent and revisit. Rendered screenshot files require human inspection.",
                cannotVerify = "One explicit starting shortcut transfers the unchanged new-game actor to the actual generated surface stair. All later route choices are scripted keyboard input. The first dagger-only approach died and remains in its separate failed receipt. This attempt uses real ready starting Calm/Rime Grip through the hotbar and avoids bumping bodies; no direct stat, health, kit, AI or visibility boosts; up to the two actual starting healing tonics may be consumed through the native inventory action. No natural discovery, player judgement, combat balance or enjoyment claim. Death/refusal is recorded as failure and is never bypassed. Save/load and native RNG are not cross-runtime deterministic replay claims."
            }, true));
            Debug.Log("[DensityLairNative] report=" + ReportPath + " failures=" + Failures);
        }
        private void Cleanup()
        {
            if (_cleaned) return; _cleaned = true;
            if (_keyboard != null) { InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); InputSystem.RemoveDevice(_keyboard); }
            if (_oldKeyboard != null && _oldKeyboard.added) _oldKeyboard.MakeCurrent();
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;

        }
        private void OnDestroy()
        {
            if (!Finished && _clock != null) { _fatal = "Play stopped before completion."; Check("native_interrupted", false); Finish(); }
            Cleanup(); EmitSummary(); Diag.SetChannel("scenario", _oldScenario);
        }
        [Serializable] private sealed class Description
        { public string subject, source, text; public int units; }
        [Serializable] private sealed class Report
        {
            public string runId, zone, fatal, canVerify, cannotVerify;
            public NativeState failureState; public NativeKeyStep[] nativeKeys;
            public string[] audit, screenshots;
            public Description[] descriptions;
            public int cases, failures, unexpectedErrors;
            public bool complete, errorsFinalized;
            public double seconds;
            public int keyboardMoves, startingTonicsUsed;
            public string[] routeZones, transcript;
            public string bossID, rewardID, checkpointHash;
            public bool startShortcut;
        }
    }
}
