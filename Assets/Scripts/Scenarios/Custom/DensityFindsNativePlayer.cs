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
    /// <summary>Finite native acquisition evidence for actual modified finds and
    /// exceptional keepers. Two labelled source starts preserve one ordinary actor;
    /// every acquisition, combat, inspection and checkpoint action uses real keys.</summary>
    public sealed class DensityFindsNativePlayer : MonoBehaviour
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
        private bool _oldBackground, _oldScenario, _oldDamage, _cleaned, _errorsFinalized, _summaryEmitted;
        private int _failures, _unexpectedErrors;
        private string _ownedRoot, _fatal;
        private System.Diagnostics.Stopwatch _clock;
        private Zone _deepZone, _keeperZone;
        private Entity _deepChest, _deepKey, _deepItem, _sentinel, _keeper, _legendaryItem;
        private Cell _deepEntry;
        private string _deepItemID, _legendaryItemID;
        private int _starts, _generatedZones;
        private readonly List<CandidateRow> _candidates = new List<CandidateRow>();
        [Serializable] private sealed class CandidateRow
        { public int seed; public string zone, reason, ownerID, ownerBlueprint, itemID, itemBlueprint, modifiers; }
        private bool _usedStartShortcut;
        private int _keyboardMoves, _startingTonicsUsed;
        private string _bossID, _rewardID, _bossGearBeforeLoad;
        private int _bossHPBeforeLoad;
        private string _savePath, _checkpointHash;
        private readonly List<NativeKeyStep> _nativeKeys = new List<NativeKeyStep>();
        private NativeState _failureState;
        [Serializable] private sealed class NativeState
        {
            public string label, state, zone, pendingEquipmentID, inventoryStatus, inventoryActionItemID, announcement; public string[] pendingDisplacements, nearbyCreatures, controlCooldowns; public int hp, maxHp, x, y, tick, energy, inventoryPanel;
            public bool deathHandled, blockingFx; public string[] lastMessages;
        }
        [Serializable] private sealed class NativeKeyStep
        { public string keys; public int sequence; public NativeState before, after; }
        private NativeState Snapshot(string label)
        {
            var actor = _input?.PlayerEntity; var zone = _input?.CurrentZone;
            var cell = actor == null ? null : zone?.GetEntityCell(actor);
            var inventory = _input?.InventoryUI;
            var confirmation = inventory == null ? null : Field(inventory, "_displaceConfirm");
            var itemActions = inventory == null ? null : Field(inventory, "_itemActionPopup");
            return new NativeState { label = label, state = _input == null ? "uninitialized" : State(), zone = zone?.ZoneID,
                hp = actor?.GetStatValue("Hitpoints") ?? -1, maxHp = actor?.GetStat("Hitpoints")?.Max ?? -1,
                x = cell?.X ?? -1, y = cell?.Y ?? -1, tick = _input?.TurnManager?.TickCount ?? -1,
                energy = actor == null || _input?.TurnManager == null ? -1 : _input.TurnManager.GetEnergy(actor),
                deathHandled = actor != null && CombatSystem.IsDeathHandled(actor),
                blockingFx = _input?.ZoneRenderer?.WorldFx?.HasBlockingFx == true,
                pendingEquipmentID = confirmation == null ? null : ((Entity)Field(confirmation, "ItemToEquip"))?.ID,
                pendingDisplacements = confirmation == null ? Array.Empty<string>()
                    : ((List<InventorySystem.Displacement>)Field(confirmation, "Displacements"))
                        .Select(d => (d.Item?.ID ?? "missing-item") + "@" + (d.BodyPart == null ? "missing-part" : d.BodyPart.ID.ToString())).ToArray(),
                inventoryPanel = inventory == null ? -1 : (int)Field(inventory, "_panel"),
                inventoryStatus = inventory == null ? null : (string)Field(inventory, "_actionStatus"),
                inventoryActionItemID = itemActions == null ? null : ((Entity)Field(itemActions, "Item"))?.ID,
                announcement = _input?.AnnouncementUI == null || !_input.AnnouncementUI.IsOpen ? null
                    : (string)Field(_input.AnnouncementUI, "_message"),
                nearbyCreatures = zone == null || actor == null || cell == null ? Array.Empty<string>()
                    : zone.GetReadOnlyEntities().Where(e => e != actor && e.HasTag("Creature")
                        && SpatialQuery.DistanceToCell(zone, e, cell.X, cell.Y) <= 9)
                        .Select(e => e.ID + ":" + e.BlueprintName + "@" + string.Join("/", zone.GetOccupiedCells(e).Select(c => c.X + "," + c.Y))
                            + ";HP=" + e.GetStatValue("Hitpoints") + ";visible=" + zone.GetOccupiedCells(e).Any(c => c.IsVisible)
                            + ";noFight=" + (e.GetPart<BrainPart>()?.HasGoal<NoFightGoal>() == true)
                            + ";cold=" + (e.GetEffect<FrozenEffect>()?.Cold ?? 0)).ToArray(),
                controlCooldowns = actor?.GetPart<ActivatedAbilitiesPart>() == null ? Array.Empty<string>()
                    : Enumerable.Range(0, ActivatedAbilitiesPart.SlotCount).Select(i => actor.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(i))
                        .Where(a => a != null && (a.Command == "CommandCalm" || a.Command == "CommandRimeGrip"))
                        .Select(a => a.Command + ":" + a.CooldownRemaining).ToArray(),
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
        private static readonly string[] RequiredChecks = { "production_enhancement_registry", "ordinary_start", "actual_unforced_sources", "stamped_key_taken", "real_key_unlocks", "modified_item_acquired", "modified_item_equipped", "real_guardian_defeated", "keeper_actual_reward_inspected", "real_keeper_defeated", "same_legendary_item_acquired", "legendary_item_equipped", "ordinary_finish", "checkpoint_saved", "checkpoint_mutated", "checkpoint_restores_acquired_graph", "loaded_sources_preserve_depletion", "modified_item_inspected", "legendary_item_inspected" };
        private readonly List<string> _routeZones = new List<string>();
        private readonly List<string> _transcript = new List<string>();
        private OverworldZoneManager Manager => (OverworldZoneManager)_input.ZoneManager;
        private string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath,
            "../Docs/Verification/DensityCompletion/HigherTiers/NativeAcquisition", RunId));

        public void Initialize(ScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Completion audit requires its isolated native launcher.");
            _context = context; _ownedRoot = SaveGameService.SaveRootOverride;
            _clock = System.Diagnostics.Stopwatch.StartNew();
            _oldScenario = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            _oldDamage = Diag.IsChannelEnabled("damage"); Diag.SetChannel("damage", true);
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
            VerifyProductionEnhancements();
            yield return Tap(Key.N);
            Require(State() == "Normal" && Manager.WorldSeed == 1, "ordinary seed1 new game");
            Check("ordinary_start", !DevMode.Enabled && !_input.PlayerEntity.HasPart<BitLockerPart>()
                && _input.PlayerEntity.GetStat("Hitpoints").Max == 40
                && _input.PlayerEntity.GetStatValue("Strength") == 18
                && _input.PlayerEntity.GetStatValue("Agility") == 18
                && _input.PlayerEntity.GetStatValue("Toughness") == 18
                && TradeSystem.GetDrams(_input.PlayerEntity) == 50);
            var dagger = Owned().First(e => e.BlueprintName == "Dagger");
            yield return EquipActual(dagger);
            Observe("ordinary-start");
            yield return FindModifiedSource();
            Require(_deepZone != null, "first actual modified reliquary within96 unique zones");
            FindLegendarySource();
            Require(_keeperZone != null, "actual unforced keeper in this fixed world");
            Check("actual_unforced_sources", _generatedZones <= 96 && _deepItem.Parts.OfType<IItemEnhancement>().Count() == 1
                && _deepItem.Properties.ContainsKey(FoundEquipmentEnhancements.RollMarker)
                && _keeper.GetPart("LegendaryIdentity") != null);

            StartAt(_deepZone, _deepEntry, "actual reliquary entrance; stage1 of2");
            Observe("reliquary-arrival"); yield return Capture("01-real-reliquary-arrival");
            Require(_input.PlayerEntity.GetPart<InventoryPart>().Objects.All(e => e.GetPart<KeyPart>()?.KeyId != "iron"), "no granted iron key");
            yield return WalkIntoReach(_deepKey); yield return WorldAction(_deepKey, "Take"); yield return CloseToNormal();
            Check("stamped_key_taken", Owned().Any(e => ReferenceEquals(e, _deepKey)) && _deepZone.GetEntityCell(_deepKey) == null);
            yield return WalkIntoReach(_deepChest);
            Require(_deepChest.GetPart<LockPart>().IsLocked, "chest remains locked before real unlock");
            yield return WorldAction(_deepChest, "Unlock"); yield return CloseToNormal();
            Check("real_key_unlocks", !_deepChest.GetPart<LockPart>().IsLocked && Owned().Contains(_deepKey));
            yield return WorldAction(_deepChest, "OpenContainer"); Require(State() == "PickupOpen", "real deep-cache pickup menu");
            yield return Capture("02-modified-cache-open"); yield return Tap(Key.Tab); yield return CloseToNormal();
            Check("modified_item_acquired", Owned().Contains(_deepItem) && !_deepChest.GetPart<ContainerPart>().Contents.Contains(_deepItem));
            _deepItemID = _deepItem.ID;
            // All choices below are existing earned objects. No replacement kit
            // is created if the first actual source is difficult or inconvenient.
            var weapon = Owned().Where(e => e.HasPart<MeleeWeaponPart>()).OrderByDescending(TradeSystem.GetItemValue).FirstOrDefault();
            if (weapon != null) yield return EquipActual(weapon);
            var armor = Owned().Where(e => e.HasPart<ArmorPart>()).OrderByDescending(e => e.GetPart<ArmorPart>().AV).FirstOrDefault();
            if (armor != null) yield return EquipActual(armor);
            yield return EquipActual(_deepItem);
            Check("modified_item_equipped", _deepItem.GetPart<PhysicsPart>().Equipped == _input.PlayerEntity);
            yield return InspectActual(_deepItem, "modified_item_inspected");
            Observe("deep-find-equipped"); yield return Capture("03-earned-modified-kit");
            yield return Fight(_sentinel, "guardian");
            Check("real_guardian_defeated", CombatSystem.IsDeathHandled(_sentinel) && _deepZone.GetEntityCell(_sentinel) == null);
            Observe("guardian-defeated"); yield return Capture("04-real-guardian-outcome");

            var up = _keeperZone.GetReadOnlyEntities().Single(e => e.HasPart<StairsUpPart>());
            StartAt(_keeperZone, _keeperZone.GetEntityCell(up), "actual final-lair arrival stair; stage2 of2");
            yield return WalkIntoReach(_keeper);
            Require(!CombatSystem.IsDeathHandled(_keeper), "live keeper available for actual worn-reward inspection");
            _bossID = _keeper.ID;
            var identity = _keeper.GetPart("LegendaryIdentity"); _legendaryItemID = (string)Field(identity, "RewardID"); _rewardID = _legendaryItemID;
            _legendaryItem = Gear(_keeper).Single(e => e.ID == _legendaryItemID);
            int serial = MessageLog.GetRecentEntries(1).LastOrDefault().Serial;
            yield return WorldAction(_keeper, "Examine"); yield return CloseToNormal();
            var lines = MessageLog.GetRecentEntries(20).Where(e => e.Serial > serial).Select(e => e.Text).ToArray();
            _transcript.AddRange(lines);
            Check("keeper_actual_reward_inspected", _legendaryItem.GetPart<PhysicsPart>().Equipped == _keeper
                && lines.Any(s => s.Contains(_keeper.GetDisplayName()) && s.Contains(_legendaryItem.GetDisplayName())));
            Observe("keeper-before-combat"); yield return Capture("05-visible-keeper-and-real-gear");
            yield return Fight(_keeper, "keeper");
            Check("real_keeper_defeated", CombatSystem.IsDeathHandled(_keeper) && _keeperZone.GetEntityCell(_keeper) == null);
            Require(_keeperZone.GetEntityCell(_legendaryItem) != null, "same enhanced reward actually spilled on ground");
            yield return WalkIntoReach(_legendaryItem); yield return WorldAction(_legendaryItem, "Take"); yield return CloseToNormal();
            Check("same_legendary_item_acquired", Owned().Any(e => ReferenceEquals(e, _legendaryItem))
                && _keeperZone.GetEntityCell(_legendaryItem) == null && _legendaryItem.ID == _legendaryItemID);
            yield return EquipActual(_legendaryItem);
            Check("legendary_item_equipped", _legendaryItem.GetPart<PhysicsPart>().Equipped == _input.PlayerEntity);
            yield return InspectActual(_legendaryItem, "legendary_item_inspected");
            Observe("earned-legendary-kit"); yield return Capture("06-earned-keeper-reward");
            yield return ProveCheckpoint();
            Check("ordinary_finish", !DevMode.Enabled && !_input.PlayerEntity.HasPart<BitLockerPart>()
                && !_input.PlayerEntity.HasTag("Invulnerable") && !CombatSystem.IsDeathHandled(_input.PlayerEntity)
                && _input.PlayerEntity.GetStatValue("Hitpoints") > 0 && _starts == 2 && _startingTonicsUsed <= 2);
            Observe("complete-ordinary-acquisition"); yield return Capture("08-restored-acquired-gear");
        }

        private void VerifyProductionEnhancements()
        {
            // Use the production lazy-init path without repairing test pollution.
            // A disabled domain reload can retain a fixture's intentionally empty
            // or stub-only registry; such a run cannot measure source chance.
            EnhancementFactory.EnsureInitialized();
            var required = new[] { typeof(EnhancementSerrated), typeof(EnhancementPaleSalt),
                typeof(EnhancementChoirIron), typeof(EnhancementLacquered), typeof(EnhancementGlowQuartz) };
            var missing = new List<string>();
            foreach (var expected in required)
            {
                bool found = EnhancementFactory.TryGet(expected.Name, out var actual);
                _descriptions.Add(new Description { subject = "Production enhancement preflight",
                    source = expected.FullName, text = found ? actual.AssemblyQualifiedName : "MISSING" });
                if (!found || actual != expected) missing.Add(expected.FullName);
            }
            WriteReport();
            Require(missing.Count == 0, "enhancement registry is contaminated or incomplete before world generation: "
                + string.Join(", ", missing) + ". Run in a fresh domain; do not reinterpret unmarked items as chance misses.");
            Check("production_enhancement_registry", true);
        }

        private IEnumerator FindModifiedSource()
        {
            foreach (var biome in new[] { BiomeType.Spread, BiomeType.Sodden, BiomeType.Beating })
            {
                int generatedForBiome = 0;
                for (int y = 0; y < WorldMap.Height && generatedForBiome < 32; y++)
                for (int x = 0; x < WorldMap.Width && generatedForBiome < 32; x++)
                {
                    if (Manager.WorldMap.GetBiome(x, y) != biome) continue;
                    string surface = WorldMap.ToZoneID(x, y, 0), zoneID = WorldMap.ToZoneID(x, y, 9);
                    if (Manager.WorldMap.GetPOI(x, y) != null || OverworldZoneManager.AuthoredWildernessZoneIDs.Contains(surface)
                        || surface == MultiCellPilotRuntime.ZoneID)
                    { Candidate(zoneID, "excluded-authored-or-POI", null, null); continue; }
                    generatedForBiome++; _generatedZones++;
                    var zone = Manager.GetZone(zoneID);
                    if (zone == null) { Candidate(zoneID, "generation-refused", null, null); yield return null; continue; }
                    bool hadFind = false;
                    foreach (var chest in zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "LockedChest"))
                    foreach (var item in chest.GetPart<ContainerPart>().Contents.Where(e => e.GetTag("Tier") == "4"))
                    {
                        hadFind = true;
                        if (!item.Properties.ContainsKey(FoundEquipmentEnhancements.RollMarker))
                        { Candidate(zoneID, "missing-enhancement-roll-marker", chest, item); continue; }
                        int enhancements = ItemEnhancing.CountEnhancements(item);
                        if (enhancements == 0)
                        { Candidate(zoneID, "ordinary-chance-miss", chest, item); continue; }
                        if (enhancements != 1)
                        { Candidate(zoneID, "unexpected-enhancement-count", chest, item); continue; }
                        var at = zone.GetEntityCell(chest);
                        var key = zone.GetCell(at.X + 2, at.Y + 1)?.Occupants.SingleOrDefault(e => e.BlueprintName == "IronKey");
                        var guard = zone.GetCell(at.X + 3, at.Y)?.Occupants.SingleOrDefault(e => e.BlueprintName == "VaultSentinel");
                        var entry = zone.GetCell(at.X + 4, at.Y + 1);
                        if (key == null || guard == null || entry == null || entry.BlocksMovement(_input.PlayerEntity)
                            || chest.GetPart<LockPart>()?.IsLocked != true)
                        { Candidate(zoneID, "missing-source-authority-or-entry", chest, item); continue; }
                        _deepZone = zone; _deepChest = chest; _deepKey = key; _sentinel = guard; _deepItem = item; _deepEntry = entry;
                        Candidate(zoneID, "selected-first-real-modified-source", chest, item); yield break;
                    }
                    if (!hadFind) Candidate(zoneID, "no-reliquary-find", null, null);
                    Manager.UnloadZone(zoneID); // Rejected inactive source; never revisited or rerolled by this scan.
                    yield return null;
                }
            }
        }
        private void FindLegendarySource()
        {
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
            {
                var poi = Manager.WorldMap.GetPOI(x, y); if (poi?.Type != POIType.Lair) continue;
                string surface = WorldMap.ToZoneID(x, y, 0);
                if (poi.BossBlueprint != "MarlbackWallkeeper" || poi.Tier < 2 || poi.Tier > 3)
                { Candidate(surface, "ineligible-keeper-family-or-tier", null, null); continue; }
                var zone = Manager.GetZone(WorldMap.ToZoneID(x, y, poi.Tier < 3 ? 1 : 2));
                var boss = zone?.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "MarlbackWallkeeper");
                if (boss?.GetPart("LegendaryIdentity") == null)
                { Candidate(zone?.ZoneID ?? surface, "ordinary-keeper-chance-miss", boss, null); continue; }
                _keeperZone = zone; _keeper = boss;
                Candidate(zone.ZoneID, "selected-first-unforced-keeper", boss, null); return;
            }
        }
        private void Candidate(string zone, string reason, Entity owner, Entity item)
        {
            _candidates.Add(new CandidateRow { seed = Manager.WorldSeed, zone = zone, reason = reason,
                ownerID = owner?.ID, ownerBlueprint = owner?.BlueprintName, itemID = item?.ID,
                itemBlueprint = item?.BlueprintName, modifiers = item == null ? "" : ModifierSignature(item) });
            WriteReport();
        }
        private void StartAt(Zone zone, Cell cell, string reason)
        {
            Require(_starts < 2 && State() == "Normal" && cell != null && !cell.BlocksMovement(_input.PlayerEntity), "legal labelled stage start");
            var old = _input.CurrentZone;
            Require(old.TryTransferEntityTo(_input.PlayerEntity, zone, cell.X, cell.Y), "preserve ordinary actor at stage start");
            typeof(InputHandler).GetMethod("HandleZoneTransition", Private).Invoke(_input, new object[] {
                new ZoneTransitionResult { Success = true, NewZone = zone, NewPlayerX = cell.X, NewPlayerY = cell.Y } });
            _starts++; _usedStartShortcut = true; _stagedZone = zone; _routeZones.Add(zone.ZoneID);
            _descriptions.Add(new Description { subject = "Labelled content-stage start", source = zone.ZoneID, text = reason + " @ " + cell.X + "," + cell.Y });
            _input.CameraFollow?.SnapToPlayer(); ZoneRenderHooks.MarkFullDirty("DensityFindsAuditStart");
        }
        private IEnumerable<Entity> Owned() => Gear(_input.PlayerEntity);
        private static IEnumerable<Entity> Gear(Entity actor) => actor.GetPart<InventoryPart>().Objects
            .Concat(actor.GetPart<InventoryPart>().EquippedItems.Values).Distinct();
        private static string EquipmentSignature(Entity actor)
        {
            var inventory = actor.GetPart<InventoryPart>();
            string slots = string.Join("|", inventory.EquippedItems.OrderBy(p => p.Key).Select(p => p.Key + ":" + p.Value.ID));
            string body = string.Join("|", actor.GetPart<Body>().GetParts().Where(p => p.Equipped != null)
                .OrderBy(p => p.ID).Select(p => p.ID + ":" + p.Equipped.ID));
            string links = string.Join("|", Gear(actor).OrderBy(e => e.ID).Select(e => e.ID + ":carried="
                + (e.GetPart<PhysicsPart>()?.InInventory?.ID ?? "") + ":equipped=" + (e.GetPart<PhysicsPart>()?.Equipped?.ID ?? "")));
            return slots + ";body=" + body + ";links=" + links;
        }
        private static string ModifierSignature(Entity item) => string.Join("|", item.Parts.OfType<IItemEnhancement>().Select(e => e.Name + ":" + e.Tier).OrderBy(s => s));
        private IEnumerator EquipActual(Entity item)
        {
            Require(Owned().Contains(item), "equip an actual earned/starting entity");
            if (item.GetPart<PhysicsPart>()?.Equipped == _input.PlayerEntity) yield break;
            yield return ItemAction(item, "equip_auto");
            object confirmation = InvField<object>("_displaceConfirm");
            if (confirmation != null)
            {
                Require(State() == "InventoryOpen" && ReferenceEquals(Field(confirmation, "ItemToEquip"), item),
                    "actual replacement confirmation belongs to requested item");
                var displaced = (List<InventorySystem.Displacement>)Field(confirmation, "Displacements");
                Require(displaced.Count > 0 && displaced.All(d => d.Item != null && Owned().Contains(d.Item)
                    && d.BodyPart != null && ReferenceEquals(d.BodyPart.Equipped, d.Item)),
                    "replacement confirmation names real owned equipment");
                _descriptions.Add(new Description { subject = "Native equipment replacement confirmation", source = item.ID,
                    text = string.Join(" | ", displaced.Select(d => d.Item.ID + " " + d.Item.GetDisplayName() + " @ " + d.BodyPart.ID)) });
                yield return Tap(Key.Y);
                Require(InvField<object>("_displaceConfirm") == null, "native Y completes replacement; " + InvField<string>("_actionStatus"));
            }
            Require(item.GetPart<PhysicsPart>()?.Equipped == _input.PlayerEntity,
                "native equipment success " + item.BlueprintName + "; " + InvField<string>("_actionStatus"));
            yield return CloseToNormal();
        }
        private IEnumerator InspectActual(Entity item, string check)
        {
            int tick = _input.TurnManager.TickCount; int energy = _input.TurnManager.GetEnergy(_input.PlayerEntity);
            yield return ItemAction(item, "examine_item");
            Require(State() == "AnnouncementOpen", "native item inspection");
            string text = (string)Field(_input.AnnouncementUI, "_message");
            bool truthful = text.Contains(item.GetDisplayName()) && item.Parts.OfType<IItemEnhancement>().All(e => text.Contains(e.GetEffectDescription()));
            _descriptions.Add(new Description { subject = check, source = item.ID, text = text });
            yield return Capture(check); yield return Tap(Key.Escape); yield return CloseToNormal();
            Check(check, truthful && _input.TurnManager.TickCount == tick && _input.TurnManager.GetEnergy(_input.PlayerEntity) == energy);
        }
        private IEnumerator Fight(Entity target, string role)
        {
            int attacks = 0;
            while (!CombatSystem.IsDeathHandled(target) && target.GetStatValue("Hitpoints") > 0)
            {
                Require(_input.PlayerEntity.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(_input.PlayerEntity), "ordinary actor survives " + role);
                Require(attacks++ < 120, "bounded real combat " + role);
                yield return WalkIntoReach(target);
                if (CombatSystem.IsDeathHandled(target) || target.GetStatValue("Hitpoints") <= 0) break;
                if (_input.PlayerEntity.GetStatValue("Hitpoints") <= 20 && _startingTonicsUsed < 2)
                {
                    var tonic = Owned().FirstOrDefault(e => e.BlueprintName == "HealingTonic");
                    if (tonic != null) { yield return ItemAction(tonic, "ApplyTonic"); yield return CloseToNormal(); _startingTonicsUsed++; }
                }
                var from = Cell(); var to = SpatialQuery.ClosestCell(_input.CurrentZone, target, from.X, from.Y);
                int serial = MessageLog.GetRecentEntries(1).LastOrDefault().Serial;
                yield return Tap(Direction(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y)));
                _transcript.AddRange(MessageLog.GetRecentEntries(12).Where(e => e.Serial > serial).Select(e => role + ": " + e.Text));
                yield return DismissEarnedAdvancement();
                Observe(role + "-attack-" + attacks);
            }
            yield return DismissEarnedAdvancement();
        }
        private IEnumerator DismissEarnedAdvancement()
        {
            // Announcements are opened on a later Update than the killing key.
            // Let the actual UI display its queue; never consume it directly.
            yield return null;
            for (int dismissed = 0; dismissed < 8; dismissed++)
            {
                double began = Time.realtimeSinceStartupAsDouble;
                while (MessageLog.HasPendingAnnouncement && State() != "AnnouncementOpen")
                {
                    Require(State() == "Normal" && Time.realtimeSinceStartupAsDouble - began < 2,
                        "pending advancement reaches actual announcement UI; " + RuntimeDetails());
                    yield return null;
                }
                if (State() != "AnnouncementOpen") yield break;
                string message = (string)Field(_input.AnnouncementUI, "_message");
                const string prefix = "You advance to level ";
                Require(message != null && message.StartsWith(prefix, StringComparison.Ordinal)
                    && message.EndsWith("!", StringComparison.Ordinal)
                    && int.TryParse(message.Substring(prefix.Length, message.Length - prefix.Length - 1), out int level)
                    && level >= 2 && level <= _input.PlayerEntity.GetStatValue("Level")
                    && MessageLog.GetRecentEntries(64).Any(e => e.Text == message),
                    "only an actual earned-level announcement may be dismissed automatically; " + message);
                var actor = _input.PlayerEntity; var at = Cell();
                int tick = _input.TurnManager.TickCount, energy = _input.TurnManager.GetEnergy(actor);
                string stats = StatSignature(actor), gear = GearIdentity(actor), equipment = EquipmentSignature(actor);
                _descriptions.Add(new Description { subject = "Native earned advancement dismissal", source = actor.ID,
                    text = message + "; preserved stats=" + stats });
                yield return Tap(Key.Escape);
                Require(ReferenceEquals(actor, _input.PlayerEntity) && ReferenceEquals(at, Cell())
                    && _input.TurnManager.TickCount == tick && _input.TurnManager.GetEnergy(actor) == energy
                    && StatSignature(actor) == stats && GearIdentity(actor) == gear && EquipmentSignature(actor) == equipment,
                    "dismissing earned advancement preserves all actual gains and costs no turn");
                yield return null;
            }
            Require(State() != "AnnouncementOpen" && !MessageLog.HasPendingAnnouncement,
                "bounded earned-advancement queue drained");
        }
        private static string StatSignature(Entity actor)
            => string.Join("|", actor.Statistics.OrderBy(p => p.Key)
                .Select(p => p.Key + ":" + p.Value.BaseValue + ":" + p.Value.Value + ":" + p.Value.Min + ":" + p.Value.Max));

        private IEnumerator ProveCheckpoint()
        {
            var p = _input.PlayerEntity; var at = Cell(); int sx = at.X, sy = at.Y;
            var info = SaveGameService.GetSaveInfo("Quick"); Require(info != null, "real checkpoint metadata");
            _savePath = Path.Combine(_ownedRoot, info.GameID, "Quick.sav.gz");
            string previous = HashFile(_savePath), inventory = GearIdentity(p), equipment = EquipmentSignature(p);
            string deepModifiers = ModifierSignature(Owned().Single(e => e.ID == _deepItemID));
            string legendaryModifiers = ModifierSignature(Owned().Single(e => e.ID == _legendaryItemID));
            int tick = _input.TurnManager.TickCount, energy = _input.TurnManager.GetEnergy(p), hp = p.GetStatValue("Hitpoints");
            long serial = MessageLog.NextSerialValue;
            yield return Tap(Key.F5); _checkpointHash = HashFile(_savePath);
            Check("checkpoint_saved", MessageLog.GetLast() == "Game saved." && MessageLog.NextSerialValue > serial
                && _checkpointHash != previous && SaveGameService.GetSaveInfo("Quick")?.GameID == info.GameID
                && SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID == _keeperZone.ZoneID);
            var step = CheckpointStep(sx, sy); Require(step != null, "safe real checkpoint mutation step");
            yield return Tap(Direction(step.X - sx, step.Y - sy)); _keyboardMoves++;
            Check("checkpoint_mutated", Cell().X == step.X && Cell().Y == step.Y && HashFile(_savePath) == _checkpointHash
                && _input.TurnManager.GetEnergy(p) == energy - TurnManager.ActionThreshold
                    + (_input.TurnManager.TickCount - tick) * p.GetStatValue("Speed", TurnManager.DefaultSpeed));
            yield return Tap(Key.F6); double began = Time.realtimeSinceStartupAsDouble;
            while (ReferenceEquals(p, _input.PlayerEntity)) { Require(Time.realtimeSinceStartupAsDouble - began < 8, "F6 replaces actual player graph"); yield return null; }
            yield return CloseToNormal();
            Check("checkpoint_restores_acquired_graph", GearIdentity(_input.PlayerEntity) == inventory
                && EquipmentSignature(_input.PlayerEntity) == equipment
                && ModifierSignature(Owned().Single(e => e.ID == _deepItemID)) == deepModifiers
                && ModifierSignature(Owned().Single(e => e.ID == _legendaryItemID)) == legendaryModifiers
                && _input.PlayerEntity.GetStatValue("Hitpoints") == hp && _input.TurnManager.TickCount == tick
                && _input.TurnManager.GetEnergy(_input.PlayerEntity) == energy && Cell().X == sx && Cell().Y == sy
                && HashFile(_savePath) == _checkpointHash);
            string finalID = _keeperZone.ZoneID; var final = Manager.GetZone(finalID);
            var deep = Manager.GetZone(_deepZone.ZoneID);
            Check("loaded_sources_preserve_depletion", final.GetReadOnlyEntities().All(e => e.ID != _bossID && e.ID != _legendaryItemID)
                && deep.GetReadOnlyEntities().All(e => e.ID != _sentinel.ID)
                && deep.GetReadOnlyEntities().Single(e => e.ID == _deepChest.ID).GetPart<ContainerPart>().Contents.All(e => e.ID != _deepItemID)
                && Owned().Count(e => e.ID == _deepItemID) == 1 && Owned().Count(e => e.ID == _legendaryItemID) == 1);
            yield return Capture("07-real-checkpoint-restored");
        }
        private void Observe(string phase)
        {
            var actor = _input.PlayerEntity;
            _descriptions.Add(new Description { subject = phase, source = _input.CurrentZone.ZoneID,
                text = "HP=" + actor.GetStatValue("Hitpoints") + "/" + actor.GetStat("Hitpoints").Max
                    + "; AV=" + CombatSystem.GetAV(actor) + "; DV=" + CombatSystem.GetDV(actor)
                    + "; level=" + actor.GetStatValue("Level") + "; strength=" + actor.GetStatValue("Strength")
                    + "; agility=" + actor.GetStatValue("Agility") + "; toughness=" + actor.GetStatValue("Toughness")
                    + "; tick=" + _input.TurnManager.TickCount + "; actualGear=" + GearIdentity(actor) });
            WriteReport();
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
            => ClearCastLineFrom(Cell(), target, range, out dx, out dy);
        private bool ClearCastLineFrom(Cell from, Entity target, int range, out int dx, out int dy)
        {
            dx = dy = 0;
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
            // Acquisition uses naturally granted ready starter slots.
            // Actual cooldown and target effect are observed after native input.
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
                yield return DismissEarnedAdvancement();
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
            => SafeRouteTo(c => c.X == x && c.Y == y);
        private List<Cell> SafeRouteTo(Func<Cell, bool> destination)
        {
            var origin = Cell(); var start = (origin.X, origin.Y);
            if (destination(origin)) return new List<Cell>();
            var threats = _input.CurrentZone.GetReadOnlyEntities().Where(IsActiveVisibleThreat).ToArray();
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
                    var cell = _input.CurrentZone.GetCell(next.Item1, next.Item2);
                    if (destination(cell))
                    {
                        var path = new List<Cell>(); var point = next;
                        while (point != start)
                        {
                            path.Add(_input.CurrentZone.GetCell(point.Item1, point.Item2));
                            point = previous[point];
                        }
                        path.Reverse(); return path;
                    }
                    queue.Enqueue(next);
                }
            }
            return null;
        }
        private int _controlRecoveryActions, _routeMeleeAttempts;
        private readonly List<string> _routeMeleeWindows = new List<string>();
        private Entity AdjacentRouteThreat(Entity destinationOwner)
        {
            var actor = _input.PlayerEntity; var zone = _input.CurrentZone;
            return zone.GetReadOnlyEntities().Where(enemy => enemy != destinationOwner && IsActiveVisibleThreat(enemy)
                && FactionManager.IsHostile(actor, enemy)
                && ReferenceEquals(enemy.GetPart<BrainPart>()?.CurrentZone, zone)
                && _input.TurnManager.IsRegistered(enemy) && SpatialQuery.Distance(zone, actor, enemy) == 1)
                .OrderBy(enemy => enemy.GetStatValue("Hitpoints")).ThenBy(enemy => enemy.ID, StringComparer.Ordinal).FirstOrDefault();
        }
        private IEnumerator StrikeAdjacentRouteThreat(Entity enemy, Entity destinationOwner)
        {
            Require(_routeMeleeAttempts < 40 && ReferenceEquals(AdjacentRouteThreat(destinationOwner), enemy),
                "bounded native melee against exact current adjacent visible active hostile; " + RuntimeDetails());
            var actor = _input.PlayerEntity; var zone = _input.CurrentZone; var from = Cell();
            var to = SpatialQuery.ClosestCell(zone, enemy, from.X, from.Y);
            Require(zone.GetReadOnlyEntities().Count(e => e.ID == enemy.ID) == 1
                && Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y)) == 1,
                "unique adjacent native route enemy owns its physical cell");
            int serial = MessageLog.GetRecentEntries(1).LastOrDefault().Serial;
            Diag.Record("scenario", "DensityFindsRouteMeleeStart", actor, enemy,
                new { runId = RunId, destinationOwner = destinationOwner.ID,
                    fromX = from.X, fromY = from.Y, targetX = to.X, targetY = to.Y,
                    actorHP = actor.GetStatValue("Hitpoints"), enemyHP = enemy.GetStatValue("Hitpoints") });
            string marker = Diag.Snapshot(1).Single().TraceId;
            IReadOnlyList<Diag.Entry> rows = null;
            try
            {
                _routeMeleeAttempts++;
                yield return Tap(Direction(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y)));
            }
            finally
            {
                rows = Diag.Snapshot(Diag.BufferCapacity);
                _routeMeleeWindows.Add(Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    marker, actor = actor.ID, enemy = enemy.ID, blueprint = enemy.BlueprintName,
                    actorHP = actor.GetStatValue("Hitpoints"), actorDead = CombatSystem.IsDeathHandled(actor),
                    enemyHP = enemy.GetStatValue("Hitpoints"), enemyDead = CombatSystem.IsDeathHandled(enemy),
                    records = rows.SkipWhile(row => row.TraceId != marker).ToArray()
                }));
            }
            Require(rows.Count(row => row.TraceId == marker && row.Kind == "DensityFindsRouteMeleeStart"
                && row.ActorId == actor.ID && row.TargetId == enemy.ID) == 1
                && rows.SkipWhile(row => row.TraceId != marker).Any(row => row.Category == "damage" && row.Kind == "HitRoll"
                    && row.ActorId == actor.ID && row.TargetId == enemy.ID && !string.IsNullOrEmpty(row.CauseTraceId)),
                "real native route melee dispatched against the intended hostile");
            yield return DismissEarnedAdvancement();
            _transcript.AddRange(MessageLog.GetRecentEntries(24).Where(e => e.Serial > serial).Select(e => "route combat: " + e.Text));
            _descriptions.Add(new Description { subject = "Actual adjacent route combat", source = enemy.ID,
                text = "native movement attack with actual earned kit; " + enemy.BlueprintName
                    + "; HP=" + enemy.GetStatValue("Hitpoints") + "; dead=" + CombatSystem.IsDeathHandled(enemy)
                    + "; attempt=" + _routeMeleeAttempts + "; marker=" + marker });
        }
        private ActivatedAbility[] CoolingStartingControls()
        {
            var abilities = _input.PlayerEntity.GetPart<ActivatedAbilitiesPart>();
            return Enumerable.Range(0, ActivatedAbilitiesPart.SlotCount)
                .Select(slot => abilities?.GetAbilityBySlot(slot))
                .Where(ability => ability != null && ability.CooldownRemaining > 0
                    && (ability.Command == "CommandCalm" || ability.Command == "CommandRimeGrip")).ToArray();
        }
        private IEnumerator RecoverStartingControl()
        {
            Require(_controlRecoveryActions < 12, "bounded12 native recovery actions; " + RuntimeDetails());
            var cooling = CoolingStartingControls();
            Require(cooling.Length > 0, "a real starting control is cooling down; " + RuntimeDetails());
            var actor = _input.PlayerEntity; var from = Cell();
            var threats = _input.CurrentZone.GetReadOnlyEntities().Where(IsActiveVisibleThreat).ToArray();
            bool waiting = SafeRouteCell(from.X, from.Y, threats);
            Cell retreat = null;
            if (!waiting)
            {
                // Keep the same two-cell threat exclusion. A retreat that cannot
                // satisfy it is refused, rather than stepping through danger.
                var candidates = new List<Cell>();
                for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0 || !SafeRouteCell(from.X + dx, from.Y + dy, threats)) continue;
                    candidates.Add(_input.CurrentZone.GetCell(from.X + dx, from.Y + dy));
                }
                retreat = candidates.OrderByDescending(c => threats.Length == 0 ? 0
                    : threats.Min(e => SpatialQuery.DistanceToCell(_input.CurrentZone, e, c.X, c.Y)))
                    .ThenBy(c => c.X).ThenBy(c => c.Y).FirstOrDefault();
                Require(retreat != null, "no permitted native wait or retreat while controls recover; " + RuntimeDetails());
            }
            int tick = _input.TurnManager.TickCount, energy = _input.TurnManager.GetEnergy(actor);
            int speed = actor.GetStatValue("Speed", TurnManager.DefaultSpeed);
            int[] before = cooling.Select(ability => ability.CooldownRemaining).ToArray();
            _descriptions.Add(new Description { subject = "Native control recovery action", source = actor.ID,
                text = (waiting ? "ordinary stationary wait" : "ordinary retreat to " + retreat.X + "," + retreat.Y)
                    + "; origin=" + from.X + "," + from.Y + "; cooling="
                    + string.Join(",", cooling.Select(ability => ability.Command + ":" + ability.CooldownRemaining)) });
            yield return Tap(waiting ? Key.Period : Direction(retreat.X - from.X, retreat.Y - from.Y));
            yield return DismissEarnedAdvancement();
            _controlRecoveryActions++;
            if (!waiting) _keyboardMoves++;
            Require(ReferenceEquals(actor, _input.PlayerEntity) && State() == "Normal"
                && actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(actor),
                "ordinary actor survives real control recovery; " + RuntimeDetails());
            Require(waiting ? ReferenceEquals(Cell(), from) : Cell().X == retreat.X && Cell().Y == retreat.Y,
                "native recovery key performs its actual wait or retreat");
            Require(_input.TurnManager.TickCount > tick && _input.TurnManager.GetEnergy(actor)
                == energy - TurnManager.ActionThreshold + (_input.TurnManager.TickCount - tick) * speed,
                "native recovery spends exactly one ordinary scheduler action");
            Require(cooling.Where((ability, index) => ability.CooldownRemaining < before[index]).Any(),
                "real starting cooldown advances through the scheduler");
        }
        private bool CanUseReadyControlFrom(Cell from)
        {
            foreach (var threat in _input.CurrentZone.GetReadOnlyEntities().Where(IsActiveVisibleThreat))
            foreach (string command in new[] { "CommandCalm", "CommandRimeGrip" })
            {
                if (ReadyAbility(command, out _) == null) continue;
                int range = command == "CommandCalm" ? 6 : 5;
                if (ClearCastLineFrom(from, threat, range, out _, out _)) return true;
            }
            return false;
        }
        private IEnumerator WalkIntoReach(Entity target)
        {
            // Native actors move and visibility changes after every action.
            // Never keep walking toward an old adjacent cell selected before a
            // target moved. A control ray must be reached by real safe steps.
            for (int step = 0; step < 160; step++)
            {
                yield return WaitForNativeFx();
                yield return DismissEarnedAdvancement();
                Require(State() == "Normal" && _input.PlayerEntity.GetStatValue("Hitpoints") > 0
                    && !CombatSystem.IsDeathHandled(_input.PlayerEntity), "live native approach; " + RuntimeDetails());
                if (_input.PlayerEntity.GetStatValue("Hitpoints") <= 20 && _startingTonicsUsed < 2)
                {
                    var tonic = Owned().FirstOrDefault(e => e.BlueprintName == "HealingTonic");
                    if (tonic != null)
                    {
                        yield return ItemAction(tonic, "ApplyTonic"); yield return CloseToNormal(); _startingTonicsUsed++;
                        _transcript.Add("Keyboard used actual starting healing tonic " + _startingTonicsUsed + " of2; no direct health modification.");
                    }
                }
                if (CombatSystem.IsDeathHandled(target) || (target.HasTag("Creature") && target.GetStatValue("Hitpoints") <= 0)) yield break;
                Require(_input.CurrentZone.GetEntityCell(target) != null, "approach owner remains in this native zone");
                if (SpatialQuery.Distance(_input.CurrentZone, _input.PlayerEntity, target) <= 1) yield break;
                yield return ProtectRouteWithStartingControl();
                if (CombatSystem.IsDeathHandled(target) || (target.HasTag("Creature") && target.GetStatValue("Hitpoints") <= 0)) yield break;
                if (SpatialQuery.Distance(_input.CurrentZone, _input.PlayerEntity, target) <= 1) yield break;
                var adjacentThreat = AdjacentRouteThreat(target);
                if (adjacentThreat != null)
                {
                    yield return StrikeAdjacentRouteThreat(adjacentThreat, target);
                    continue;
                }
                var path = SafeRouteTo(c => SpatialQuery.DistanceToCell(_input.CurrentZone, target, c.X, c.Y) <= 1);
                string goal = "current owner reach";
                if (path == null)
                {
                    path = SafeRouteTo(CanUseReadyControlFrom);
                    goal = "ready native control casting line";
                }
                _descriptions.Add(new Description { subject = "Replanned native approach", source = target.ID,
                    text = goal + "; origin=" + Cell().X + "," + Cell().Y + "; path="
                        + (path == null ? "none" : string.Join("/", path.Select(c => c.X + "," + c.Y))) });
                if (path != null && path.Count == 0 && goal == "ready native control casting line")
                {
                    yield return ProtectRouteWithStartingControl();
                    continue;
                }
                if (path == null && CoolingStartingControls().Length > 0)
                {
                    yield return RecoverStartingControl();
                    continue;
                }
                Require(path != null && path.Count > 0, "safe native reach or control-position route; " + RuntimeDetails());
                var next = path[0]; var from = Cell();
                yield return Tap(Direction(next.X - from.X, next.Y - from.Y)); _keyboardMoves++;
            }
            throw new InvalidOperationException("Finite native owner approach exceeded160 steps.");
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
                yield return DismissEarnedAdvancement();
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
            Require(State() == "WorldActionMenuOpen", "native cell action menu");
            // A real defeated keeper leaves several items on one cell. Follow
            // the visible pile/owner picker before selecting this exact drop.
            // The previous audit incorrectly assumed Enter always picked it.
            if (_input.WorldActionMenuUI.SelectedCellIsPile)
                yield return WorldMenuAction(WorldInteractionSystem.PickCellCommand);
            if (!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, target)
                || ((List<InventoryAction>)Field(_input.WorldActionMenuUI, "_actions"))
                    .Any(a => a.Command == WorldInteractionSystem.PickTargetCommandPrefix + target.ID))
                yield return WorldMenuAction(WorldInteractionSystem.PickTargetCommandPrefix + target.ID);
            Require(State() == "WorldActionMenuOpen" && ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, target), "native owner selection");
            yield return WorldMenuAction(command);
        }

        private IEnumerator WorldMenuAction(string command)
        {
            Require(State() == "WorldActionMenuOpen", "native menu remains open for " + command);
            var actions = (List<InventoryAction>)Field(_input.WorldActionMenuUI, "_actions");
            int index = actions.FindIndex(a => a.Command == command);
            Require(index >= 0, "native action " + command);
            yield return Tap((Key)Enum.Parse(typeof(Key), MenuShortcutMap.Key(MenuShortcutMap.ForActions(actions)[index]).ToString()));
        }

        private IEnumerator ItemAction(Entity item, string command)
        {
            yield return Tap(Key.I); Require(State() == "InventoryOpen", "native I opens inventory");
            // Opening normally selects equipment, but real pointer movement can
            // select another visible panel. Navigate the observed panel with
            // native keys; never set UI fields or assume one Tab reached it.
            for (int step = 0; InvField<int>("_panel") != 1; step++)
            {
                Require(step < 5, "bounded native inventory panel navigation; panel=" + InvField<int>("_panel"));
                yield return Tap(InvField<int>("_panel") == 0 ? Key.Tab : Key.LeftArrow);
            }
            Require(InvField<int>("_panel") == 1, "native combined inventory/equipment item list");
            int row = RowIndex(item); Require(row >= 0, "owned item row");
            for (int step = 0; InvField<int>("_cursorIndex") != row; step++)
            { Require(step < 80, "bounded item navigation"); yield return Tap(InvField<int>("_cursorIndex") < row ? Key.DownArrow : Key.UpArrow); }
            yield return Tap(Key.Enter);
            object popup = InvField<object>("_itemActionPopup"); Require(popup != null, "native item actions");
            var actions = ((IList)Field(popup, "Actions")).Cast<object>().ToArray();
            int action = Array.FindIndex(actions, a => (string)Field(a, "Command") == command);
            Require(action >= 0, "native requested action exists " + command);
            for (int step = 0; (int)Field(popup, "CursorIndex") != action; step++)
            { Require(step < 80, "bounded native action navigation"); yield return Tap((int)Field(popup, "CursorIndex") < action ? Key.DownArrow : Key.UpArrow); }
            yield return Tap(Key.Enter);
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
            Require(_clock.Elapsed.TotalSeconds < 660, "finite native content-completion deadline");
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
            Diag.Record("scenario", "DensityFindsNativeCase", payload: new { runId = RunId, name, passed });
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception error = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception caught) { error = caught; }
                if (error != null) { _failureState = Snapshot("failure"); _fatal = error.ToString(); Check("native_precondition_failed", false); Debug.LogError("[DensityFindsNative] " + error); break; }
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
            Diag.Record("scenario", "DensityFindsNativeSummary", payload: new
            { runId = RunId, cases = _audit.Count, failures = Failures, complete = Complete, errorsFinalized = _errorsFinalized, screenshots = _screenshots.Count });
        }
        private bool Complete => Finished && _errorsFinalized && Failures == 0 && _audit.Count == RequiredChecks.Length
            && RequiredChecks.All(name => _audit.Contains("PASS " + name)) && _screenshots.Count >= 10;
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath); ReportPath = Path.Combine(DirectoryPath, "report.json");
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report
            {
                runId = RunId, cases = _audit.Count, failures = Failures, unexpectedErrors = _unexpectedErrors,
                complete = Complete, errorsFinalized = _errorsFinalized, seconds = _clock?.Elapsed.TotalSeconds ?? 0,
                keyboardMoves = _keyboardMoves, startingTonicsUsed = _startingTonicsUsed, routeZones = _routeZones.ToArray(), transcript = _transcript.ToArray(),
                startShortcut = _usedStartShortcut, bossID = _bossID, rewardID = _rewardID, checkpointHash = _checkpointHash,
                zone = _input?.CurrentZone?.ZoneID, failureState = _failureState, nativeKeys = _nativeKeys.ToArray(), routeMeleeWindows = _routeMeleeWindows.ToArray(), candidates = _candidates.ToArray(), stageStarts = _starts, generatedZones = _generatedZones, fatal = _fatal, audit = _audit.ToArray(), screenshots = _screenshots.ToArray(), descriptions = _descriptions.ToArray(),
                canVerify = "Fixed seed1 actual modified deep reliquary and unforced named final keeper; native key pickup, unlock, collection, equipment, inspection, ordinary active-AI fights, same actual rewards and real F5/mutate/F6 proof. Actual starting Calm/Rime Grip may pacify/freeze targets through native hotbar use; this is an acquisition audit, not a duel. All candidate outcomes recorded.",
                cannotVerify = "Two explicitly labelled content-stage starts shorten travel to real source entrances. No source/gear/currency/stat/health grant or modifier roll override. Starting stats are ordinary; naturally earned XP/equipment remain real. First combat failure is retained with no reseed/retry. Bounded generated-source selection is not discovery, frequency, natural progression or combat-balance evidence. Screenshots still need visual review."
            }, true));
            Debug.Log("[DensityFindsNative] report=" + ReportPath + " failures=" + Failures);
        }
        private void Cleanup()
        {
            if (_cleaned) return; _cleaned = true;
            Diag.SetChannel("damage", _oldDamage);
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
            public string[] routeMeleeWindows;
            public CandidateRow[] candidates; public int stageStarts, generatedZones;
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
