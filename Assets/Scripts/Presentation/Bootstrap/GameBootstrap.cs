using System;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace CavesOfOoo
{
    /// <summary>
    /// Bootstraps the game: loads blueprints, generates a cave zone via ZoneManager,
    /// wires up the renderer, turn manager, and input handler.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("References")]
        public ZoneRenderer ZoneRenderer;

        /// <summary>
        /// Fired exactly once per successful <see cref="Start"/>, after the zone,
        /// player, factory, and turn manager are fully wired and the game is ready
        /// to take input. Subscribers receive live references for post-init hooks
        /// (e.g., the scenario runner applies a pending scenario here).
        ///
        /// Not fired if bootstrap fails mid-init. Arguments: zone, factory, player,
        /// turnManager.
        /// </summary>
        public static event System.Action<Zone, EntityFactory, Entity, TurnManager> OnAfterBootstrap;

        private EntityFactory _factory;
        private OverworldZoneManager _zoneManager;
        private Zone _zone;
        private TurnManager _turnManager;
        private Entity _player;
        private string _gameID = Guid.NewGuid().ToString("N");
        private static readonly char[] StartingBitTypes = { 'R', 'G', 'B', 'C', 'r', 'g', 'b', 'c', 'K', 'W', 'Y', 'M' };
        private static readonly string[] StartingTonicBlueprints =
        {
            "HealingTonic",
            "PoisonTonic",
            "FireTonic",
            "Antidote",
            "BurnSalve",
            "Panacea",
            "SpeedTonic",
            "StrengthTonic"
        };

        private void Awake()
        {
            Debug.Log("[Bootstrap] Awake called");
        }

        private void Start()
        {
            Debug.Log("[Bootstrap] Start called");
            try
            {
                DoStart();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Bootstrap] FATAL exception during Start:\n{ex}");
            }
        }

        private void DoStart()
        {
            PerformanceDiagnostics.ResetAll();
            long startupStart = Stopwatch.GetTimestamp();

            using (PerformanceMarkers.Bootstrap.DoStart.Auto())
            {
                AsciiFxBus.Clear();

                Debug.Log("[Bootstrap] Step 1/9: Initializing factions...");
                PerformanceDiagnostics.MeasureStartupPhase("LoadFactions", PerformanceMarkers.Bootstrap.LoadFactions, () =>
                {
                    TextAsset factionAsset = Resources.Load<TextAsset>("Content/Data/Factions");
                    if (factionAsset != null)
                    {
                        FactionManager.Initialize(factionAsset.text);
                    }
                    else
                    {
                        Debug.LogWarning("[Bootstrap] Factions.json not found, using hardcoded defaults.");
                        FactionManager.Initialize();
                    }
                });

                Debug.Log("[Bootstrap] Step 1b/9: Initializing material reactions...");
                PerformanceDiagnostics.MeasureStartupPhase("LoadMaterialReactions", PerformanceMarkers.Bootstrap.LoadMaterialReactions, () =>
                {
                    TextAsset[] reactionAssets = Resources.LoadAll<TextAsset>("Content/Data/MaterialReactions");
                    if (reactionAssets != null && reactionAssets.Length > 0)
                    {
                        var jsonSources = new System.Collections.Generic.List<string>(reactionAssets.Length);
                        for (int i = 0; i < reactionAssets.Length; i++)
                            jsonSources.Add(reactionAssets[i].text);

                        MaterialReactionResolver.InitializeFromJsonSources(jsonSources);
                        Debug.Log($"[Bootstrap] Loaded {reactionAssets.Length} reaction file(s), {MaterialReactionResolver.ReactionCount} reaction(s) total.");
                    }
                    else
                    {
                        Debug.LogWarning("[Bootstrap] No material reaction files found, reactions will be empty.");
                        MaterialReactionResolver.Initialize(null);
                    }
                });

                Debug.Log("[Bootstrap] Step 1c/9: Loading House Dramas...");
                Data.HouseDramaLoader.LoadAll();
                foreach (var drama in Data.HouseDramaLoader.GetAll())
                    Core.HouseDramaRuntime.RegisterDrama(drama);

                Debug.Log("[Bootstrap] Step 2/9: Initializing mutations...");
                PerformanceDiagnostics.MeasureStartupPhase(
                    "InitializeMutations",
                    PerformanceMarkers.Bootstrap.InitializeMutations,
                    MutationRegistry.EnsureInitialized);

                MessageLog.OnMessage = msg => Debug.Log($"[Combat] {msg}");

                Debug.Log("[Bootstrap] Step 3/9: Creating EntityFactory...");
                _factory = new EntityFactory();

                Debug.Log("[Bootstrap] Step 4/9: Loading blueprints...");
                bool blueprintsLoaded = PerformanceDiagnostics.MeasureStartupPhase("LoadBlueprints", PerformanceMarkers.Bootstrap.LoadBlueprints, () =>
                {
                    TextAsset blueprintAsset = Resources.Load<TextAsset>("Content/Blueprints/Objects");
                    if (blueprintAsset == null)
                    {
                        Debug.LogError("[Bootstrap] FAILED: Could not load Content/Blueprints/Objects.json from Resources");
                        return false;
                    }

                    _factory.LoadBlueprints(blueprintAsset.text);
                    Debug.Log($"[Bootstrap] Loaded {_factory.Blueprints.Count} blueprints");
                    LogAsciiBlueprintValidation();
                    LogHandlingBlueprintValidation();
                    return true;
                });
                if (!blueprintsLoaded)
                    return;

                ConversationActions.Factory = _factory;
                MaterialReactionResolver.Factory = _factory;
                CorpsePart.Factory = _factory;
                LayRuneGoal.Factory = _factory;

                Debug.Log("[Bootstrap] Step 5/9: Generating starting zone...");
                bool zoneGenerated = PerformanceDiagnostics.MeasureStartupPhase("GenerateZone", PerformanceMarkers.Bootstrap.GenerateZone, () =>
                {
                    _zoneManager = new OverworldZoneManager(_factory);
                    _zone = _zoneManager.GetZone("Overworld.10.10.0");
                    _zoneManager.SetActiveZone(_zone);
                    if (_zone == null)
                    {
                        Debug.LogError("[Bootstrap] FAILED: Zone generation returned null");
                        return false;
                    }

                    Debug.Log($"[Bootstrap] Zone generated: {_zone.EntityCount} entities");
                    return true;
                });
                if (!zoneGenerated)
                    return;

                Debug.Log("[Bootstrap] Step 6/9: Creating player...");
                bool playerCreated = PerformanceDiagnostics.MeasureStartupPhase("SetupPlayer", PerformanceMarkers.Bootstrap.SetupPlayer, () =>
                {
                    _player = _factory.CreateEntity("Player");
                    if (_player == null)
                    {
                        Debug.LogError("[Bootstrap] FAILED: Player entity creation returned null");
                        return false;
                    }

                    var playerBody = _player.GetPart<Body>();
                    Debug.Log($"[Bootstrap] Player created. Has Body part: {playerBody != null}, Body initialized: {playerBody?.GetBody() != null}");
                    GrantShowcaseSpellMutations();
                    InitializePlayerStartingTinkering();
                    GivePlayerStartingTonics();
                    PlacePlayerInOpenCell();
                    SpawnDebugWeaponNearPlayer();
                    SpawnDebugNPCNearPlayer();
                    return true;
                });
                if (!playerCreated)
                    return;

                Debug.Log("[Bootstrap] Step 7/9: Setting up turns...");
                PerformanceDiagnostics.MeasureStartupPhase("SetupTurns", PerformanceMarkers.Bootstrap.SetupTurns, () =>
                {
                    _turnManager = new TurnManager();
                    _zoneManager.SetTurnProvider(() => _turnManager != null ? _turnManager.TickCount : 0);
                    MessageLog.TickProvider = () => _turnManager != null ? _turnManager.TickCount : 0;
                    RegisterCreaturesForTurns();
                });

                Debug.Log("[Bootstrap] Step 8/9: Wiring renderer...");
                PerformanceDiagnostics.MeasureStartupPhase("WirePresentation", PerformanceMarkers.Bootstrap.WirePresentation, () =>
                {
                    if (ZoneRenderer != null)
                    {
                        ZoneRenderer.SetZone(_zone);
                        ZoneRenderer.PlayerEntity = _player;
                        SettlementRuntime.ZoneDirtyCallback = () => ZoneRenderer.MarkDirty("SettlementRuntime");
                        SettlementRuntime.ActiveZone = _zone;
                        Debug.Log("[Bootstrap] ZoneRenderer wired successfully");
                    }
                    else
                    {
                        Debug.LogError("[Bootstrap] FAILED: ZoneRenderer not assigned in Inspector!");
                    }

                    Camera cam = Camera.main;
                    if (cam == null)
                    {
                        Debug.LogError("[Bootstrap] FAILED: Camera.main is null (no camera tagged MainCamera)");
                        return;
                    }

                    var cameraFollow = cam.GetComponent<CameraFollow>();
                    if (cameraFollow == null)
                        cameraFollow = cam.gameObject.AddComponent<CameraFollow>();

                    Camera sidebarCamera = EnsureSidebarCamera(cam);
                    Camera hotbarCamera = EnsureHotbarCamera(cam);
                    Camera popupOverlayCamera = EnsurePopupOverlayCamera(cam);
                    ConfigureCameraLayers(cam, sidebarCamera, hotbarCamera, popupOverlayCamera);

                    cameraFollow.Player = _player;
                    cameraFollow.CurrentZone = _zone;
                    cameraFollow.SidebarCamera = sidebarCamera;
                    cameraFollow.HotbarCamera = hotbarCamera;
                    cameraFollow.PopupOverlayCamera = popupOverlayCamera;

                    if (ZoneRenderer != null)
                    {
                        ZoneRenderer.SetSidebarCamera(sidebarCamera);
                        ZoneRenderer.SetHotbarCamera(hotbarCamera);
                        ZoneRenderer.SetPopupOverlayCamera(popupOverlayCamera);
                        cameraFollow.ReservedSidebarWidthChars = ZoneRenderer.SidebarWidthChars;
                        cameraFollow.SidebarReferenceZoom = ZoneRenderer.MessageReferenceZoom;
                        cameraFollow.ReservedHotbarHeightRows = GameplayHotbarLayout.GridHeight;
                    }

                    cameraFollow.SnapToPlayer();

                    var shakeCamera = cameraFollow;
                    DamageFlashPart.OnPlayerDamaged = amount =>
                    {
                        float intensity = amount >= 5 ? 0.25f : 0.12f;
                        shakeCamera.Shake(intensity, 0.15f);
                    };

                    DamageFlashPart.OnPlayerDealtDamage = amount =>
                    {
                        float intensity = amount >= 5 ? 0.12f : 0.06f;
                        shakeCamera.Shake(intensity, 0.1f);
                    };

                    Debug.Log("[Bootstrap] Step 9/9: Wiring input...");
                    var inputHandler = GetComponent<InputHandler>();
                    if (inputHandler == null)
                        inputHandler = gameObject.AddComponent<InputHandler>();
                    inputHandler.PlayerEntity = _player;
                    inputHandler.CurrentZone = _zone;
                    inputHandler.TurnManager = _turnManager;
                    inputHandler.ZoneRenderer = ZoneRenderer;
                    inputHandler.ZoneManager = _zoneManager;
                    inputHandler.WorldMap = _zoneManager.WorldMap;
                    inputHandler.CameraFollow = cameraFollow;
                    inputHandler.EntityFactory = _factory;

                    var screenFade = GetComponent<ScreenFade>();
                    if (screenFade == null)
                        screenFade = gameObject.AddComponent<ScreenFade>();
                    inputHandler.ScreenFade = screenFade;

                    var inventoryUI = GetComponent<InventoryUI>();
                    if (inventoryUI == null)
                        inventoryUI = gameObject.AddComponent<InventoryUI>();
                    if (ZoneRenderer != null)
                        inventoryUI.Tilemap = ZoneRenderer.GetComponent<Tilemap>();
                    inventoryUI.EntityFactory = _factory;
                    inputHandler.InventoryUI = inventoryUI;

                    var pickupUI = GetComponent<PickupUI>();
                    if (pickupUI == null)
                        pickupUI = gameObject.AddComponent<PickupUI>();
                    if (ZoneRenderer != null)
                    {
                        pickupUI.Tilemap = ZoneRenderer.CenteredPopupFgTilemap;
                        pickupUI.BgTilemap = ZoneRenderer.CenteredPopupBgTilemap;
                    }
                    pickupUI.PopupCamera = popupOverlayCamera;
                    inputHandler.PickupUI = pickupUI;

                    var containerPickerUI = GetComponent<ContainerPickerUI>();
                    if (containerPickerUI == null)
                        containerPickerUI = gameObject.AddComponent<ContainerPickerUI>();
                    if (ZoneRenderer != null)
                    {
                        containerPickerUI.Tilemap = ZoneRenderer.CenteredPopupFgTilemap;
                        containerPickerUI.BgTilemap = ZoneRenderer.CenteredPopupBgTilemap;
                    }
                    containerPickerUI.PopupCamera = popupOverlayCamera;
                    inputHandler.ContainerPickerUI = containerPickerUI;

                    var worldActionMenuUI = GetComponent<WorldActionMenuUI>();
                    if (worldActionMenuUI == null)
                        worldActionMenuUI = gameObject.AddComponent<WorldActionMenuUI>();
                    if (ZoneRenderer != null)
                    {
                        worldActionMenuUI.Tilemap = ZoneRenderer.CenteredPopupFgTilemap;
                        worldActionMenuUI.BgTilemap = ZoneRenderer.CenteredPopupBgTilemap;
                    }
                    worldActionMenuUI.PopupCamera = popupOverlayCamera;
                    inputHandler.WorldActionMenuUI = worldActionMenuUI;
                    // DIAG [Phase4d] — confirm wiring took. Remove once verified.
                    UnityEngine.Debug.Log($"[ActionMenu:wire] component={worldActionMenuUI != null} " +
                        $"Tilemap={(worldActionMenuUI.Tilemap != null ? "SET" : "NULL")} " +
                        $"BgTilemap={(worldActionMenuUI.BgTilemap != null ? "SET" : "NULL")} " +
                        $"PopupCamera={(worldActionMenuUI.PopupCamera != null ? "SET" : "NULL")}");

                    var dialogueUI = GetComponent<DialogueUI>();
                    if (dialogueUI == null)
                        dialogueUI = gameObject.AddComponent<DialogueUI>();
                    if (ZoneRenderer != null)
                    {
                        dialogueUI.Tilemap = ZoneRenderer.CenteredPopupFgTilemap;
                        dialogueUI.BgTilemap = ZoneRenderer.CenteredPopupBgTilemap;
                    }
                    dialogueUI.PopupCamera = popupOverlayCamera;
                    inputHandler.DialogueUI = dialogueUI;

                    var tradeUI = GetComponent<TradeUI>();
                    if (tradeUI == null)
                        tradeUI = gameObject.AddComponent<TradeUI>();
                    if (ZoneRenderer != null)
                        tradeUI.Tilemap = ZoneRenderer.PopupFgTilemap;
                    inputHandler.TradeUI = tradeUI;

                    var factionUI = GetComponent<FactionUI>();
                    if (factionUI == null)
                        factionUI = gameObject.AddComponent<FactionUI>();
                    if (ZoneRenderer != null)
                        factionUI.Tilemap = ZoneRenderer.PopupFgTilemap;
                    inputHandler.FactionUI = factionUI;

                    var announcementUI = GetComponent<AnnouncementUI>();
                    if (announcementUI == null)
                        announcementUI = gameObject.AddComponent<AnnouncementUI>();
                    if (ZoneRenderer != null)
                    {
                        announcementUI.Tilemap = ZoneRenderer.CenteredPopupFgTilemap;
                        announcementUI.BgTilemap = ZoneRenderer.CenteredPopupBgTilemap;
                    }
                    announcementUI.PopupCamera = popupOverlayCamera;
                    inputHandler.AnnouncementUI = announcementUI;

                    // Phase 4d — Pause menu (Esc → centered modal with Save/Load).
                    var pauseMenuUI = GetComponent<PauseMenuUI>();
                    if (pauseMenuUI == null)
                        pauseMenuUI = gameObject.AddComponent<PauseMenuUI>();
                    if (ZoneRenderer != null)
                    {
                        pauseMenuUI.Tilemap = ZoneRenderer.CenteredPopupFgTilemap;
                        pauseMenuUI.BgTilemap = ZoneRenderer.CenteredPopupBgTilemap;
                    }
                    pauseMenuUI.PopupCamera = popupOverlayCamera;
                    inputHandler.PauseMenuUI = pauseMenuUI;
                });

                _turnManager.ProcessUntilPlayerTurn();

                if (_zoneManager.SettlementManager != null)
                    _zoneManager.SettlementManager.RefreshActiveZonePresentation(_zone);

                // Fire the after-bootstrap event so scenario runners (and any future
                // post-init hooks) can apply pending state. Wrapped in try/catch so a
                // buggy scenario can't abort startup logging.
                try
                {
                    OnAfterBootstrap?.Invoke(_zone, _factory, _player, _turnManager);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Bootstrap] OnAfterBootstrap handler threw: {ex}");
                }

                SaveGameService.RegisterRuntime(CaptureGameSessionState, ApplyLoadedGame);

                // Phase 4c: if a save exists, offer the boot menu so the player
                // can choose between continuing from the save or starting fresh
                // with the world we just generated. No-op when no save exists.
                var inputHandlerForBoot = GetComponent<InputHandler>();
                if (inputHandlerForBoot != null)
                {
                    inputHandlerForBoot.TryActivateBootMenu(SaveGameService.HasQuickSave());
                }

                Debug.Log($"[Bootstrap] DONE. Zone has {_zone.EntityCount} entities. WASD/arrows to move.");
            }

            PerformanceDiagnostics.RecordStartupTotal(
                PerformanceDiagnostics.ElapsedMilliseconds(startupStart, Stopwatch.GetTimestamp()));
        }

        private static Camera EnsureSidebarCamera(Camera gameplayCamera)
        {
            if (gameplayCamera == null)
                return null;

            Transform existing = gameplayCamera.transform.parent != null
                ? gameplayCamera.transform.parent.Find("Sidebar Camera")
                : null;
            Camera sidebarCamera = existing != null ? existing.GetComponent<Camera>() : null;
            if (sidebarCamera != null)
                return sidebarCamera;

            var cameraObject = new GameObject("Sidebar Camera");
            cameraObject.transform.position = new Vector3(0f, 0f, gameplayCamera.transform.position.z);
            sidebarCamera = cameraObject.AddComponent<Camera>();
            sidebarCamera.orthographic = true;
            sidebarCamera.depth = gameplayCamera.depth;
            sidebarCamera.nearClipPlane = gameplayCamera.nearClipPlane;
            sidebarCamera.farClipPlane = gameplayCamera.farClipPlane;
            sidebarCamera.backgroundColor = Color.black;
            sidebarCamera.clearFlags = CameraClearFlags.SolidColor;
            return sidebarCamera;
        }

        private static Camera EnsurePopupOverlayCamera(Camera gameplayCamera)
        {
            if (gameplayCamera == null)
                return null;

            Transform existing = gameplayCamera.transform.parent != null
                ? gameplayCamera.transform.parent.Find("Popup Overlay Camera")
                : null;
            Camera popupOverlayCamera = existing != null ? existing.GetComponent<Camera>() : null;
            if (popupOverlayCamera != null)
                return popupOverlayCamera;

            var cameraObject = new GameObject("Popup Overlay Camera");
            cameraObject.transform.position = new Vector3(0f, 0f, gameplayCamera.transform.position.z);
            popupOverlayCamera = cameraObject.AddComponent<Camera>();
            popupOverlayCamera.orthographic = true;
            popupOverlayCamera.depth = gameplayCamera.depth + 1f;
            popupOverlayCamera.nearClipPlane = gameplayCamera.nearClipPlane;
            popupOverlayCamera.farClipPlane = gameplayCamera.farClipPlane;
            popupOverlayCamera.backgroundColor = Color.clear;
            popupOverlayCamera.clearFlags = CameraClearFlags.Depth;
            popupOverlayCamera.enabled = false;
            ConfigurePopupOverlayCameraStack(gameplayCamera, popupOverlayCamera);
            return popupOverlayCamera;
        }

        private static Camera EnsureHotbarCamera(Camera gameplayCamera)
        {
            if (gameplayCamera == null)
                return null;

            Transform existing = gameplayCamera.transform.parent != null
                ? gameplayCamera.transform.parent.Find("Hotbar Camera")
                : null;
            Camera hotbarCamera = existing != null ? existing.GetComponent<Camera>() : null;
            if (hotbarCamera != null)
                return hotbarCamera;

            var cameraObject = new GameObject("Hotbar Camera");
            cameraObject.transform.position = new Vector3(0f, 0f, gameplayCamera.transform.position.z);
            hotbarCamera = cameraObject.AddComponent<Camera>();
            hotbarCamera.orthographic = true;
            hotbarCamera.depth = gameplayCamera.depth;
            hotbarCamera.nearClipPlane = gameplayCamera.nearClipPlane;
            hotbarCamera.farClipPlane = gameplayCamera.farClipPlane;
            hotbarCamera.backgroundColor = Color.black;
            hotbarCamera.clearFlags = CameraClearFlags.SolidColor;
            return hotbarCamera;
        }

        private static void ConfigureCameraLayers(Camera gameplayCamera, Camera sidebarCamera, Camera hotbarCamera, Camera popupOverlayCamera)
        {
            if (gameplayCamera != null)
                gameplayCamera.cullingMask = GameplayRenderLayers.GameplayCameraMask;

            if (sidebarCamera != null)
                sidebarCamera.cullingMask = GameplayRenderLayers.SidebarMask;

            if (hotbarCamera != null)
                hotbarCamera.cullingMask = GameplayRenderLayers.HotbarMask;

            if (popupOverlayCamera != null)
            {
                popupOverlayCamera.cullingMask = GameplayRenderLayers.PopupOverlayMask;
                ConfigurePopupOverlayCameraStack(gameplayCamera, popupOverlayCamera);
            }
        }

        private static void ConfigurePopupOverlayCameraStack(Camera gameplayCamera, Camera popupOverlayCamera)
        {
            if (gameplayCamera == null || popupOverlayCamera == null)
                return;

            var gameplayCameraData = gameplayCamera.GetUniversalAdditionalCameraData();
            var popupCameraData = popupOverlayCamera.GetUniversalAdditionalCameraData();
            if (gameplayCameraData != null)
                gameplayCameraData.renderType = CameraRenderType.Base;

            if (popupCameraData != null)
            {
                popupCameraData.renderType = CameraRenderType.Overlay;
            }

            if (gameplayCameraData == null)
                return;

            var stack = gameplayCameraData.cameraStack;
            if (stack == null)
                return;

            if (!stack.Contains(popupOverlayCamera))
                stack.Add(popupOverlayCamera);
        }

        private GameSessionState CaptureGameSessionState()
        {
            return GameSessionState.Capture(
                _gameID,
                Application.version,
                _zoneManager,
                _turnManager,
                _player,
                selectedHotbarSlot: 0);
        }

        public void ApplyLoadedGame(GameSessionState state)
        {
            if (state == null)
                return;

            _gameID = string.IsNullOrEmpty(state.GameID) ? _gameID : state.GameID;
            _zoneManager = state.ZoneManager;
            _turnManager = state.TurnManager;
            _player = state.Player;
            _zone = _zoneManager?.ActiveZone;

            ConversationActions.Factory = _factory;
            MaterialReactionResolver.Factory = _factory;
            CorpsePart.Factory = _factory;
            LayRuneGoal.Factory = _factory;
            ConversationManager.EndConversation();

            if (_zoneManager != null)
                _zoneManager.SetTurnProvider(() => _turnManager != null ? _turnManager.TickCount : 0);
            MessageLog.TickProvider = () => _turnManager != null ? _turnManager.TickCount : 0;

            RewireLoadedBrains();
            WirePresentationForLoadedGame();

            if (_turnManager != null && !_turnManager.WaitingForInput)
                _turnManager.ProcessUntilPlayerTurn();

            if (_zoneManager?.SettlementManager != null)
                _zoneManager.SettlementManager.RefreshActiveZonePresentation(_zone);

            SaveGameService.RegisterRuntime(CaptureGameSessionState, ApplyLoadedGame);
        }

        private void RewireLoadedBrains()
        {
            if (_zoneManager == null)
                return;

            foreach (var kvp in _zoneManager.CachedZones)
            {
                Zone zone = kvp.Value;
                foreach (Entity entity in zone.GetAllEntities())
                {
                    var brain = entity.GetPart<BrainPart>();
                    if (brain != null)
                        brain.CurrentZone = zone;
                }
            }
        }

        private void WirePresentationForLoadedGame()
        {
            if (ZoneRenderer != null)
            {
                ZoneRenderer.SetZone(_zone);
                ZoneRenderer.PlayerEntity = _player;
                SettlementRuntime.ZoneDirtyCallback = () => ZoneRenderer.MarkDirty("SettlementRuntime");
                SettlementRuntime.ActiveZone = _zone;
            }

            Camera cam = Camera.main;
            CameraFollow cameraFollow = null;
            if (cam != null)
            {
                cameraFollow = cam.GetComponent<CameraFollow>();
                if (cameraFollow == null)
                    cameraFollow = cam.gameObject.AddComponent<CameraFollow>();

                cameraFollow.Player = _player;
                cameraFollow.CurrentZone = _zone;
                cameraFollow.SnapToPlayer();
            }

            var inputHandler = GetComponent<InputHandler>();
            if (inputHandler != null)
            {
                inputHandler.PlayerEntity = _player;
                inputHandler.CurrentZone = _zone;
                inputHandler.TurnManager = _turnManager;
                inputHandler.ZoneRenderer = ZoneRenderer;
                inputHandler.ZoneManager = _zoneManager;
                inputHandler.WorldMap = _zoneManager?.WorldMap;
                inputHandler.CameraFollow = cameraFollow;
                inputHandler.EntityFactory = _factory;
            }
        }

        /// <summary>
        /// Start the player with full V1 tinkering access:
        /// all known recipes plus 5 units of every bit type.
        /// </summary>
        private void InitializePlayerStartingTinkering()
        {
            if (_player == null)
                return;

            var bitLocker = _player.GetPart<BitLockerPart>();
            if (bitLocker == null)
            {
                _player.AddPart(new BitLockerPart());
                bitLocker = _player.GetPart<BitLockerPart>();
            }

            if (bitLocker == null)
            {
                Debug.LogWarning("[Bootstrap/Tinkering] Failed to initialize BitLockerPart on player.");
                return;
            }

            TinkerRecipeRegistry.EnsureInitialized();

            int learnedNow = 0;
            foreach (var recipe in TinkerRecipeRegistry.GetAllRecipes())
            {
                if (recipe == null || string.IsNullOrWhiteSpace(recipe.ID))
                    continue;

                if (bitLocker.KnowsRecipe(recipe.ID))
                    continue;

                bitLocker.LearnRecipe(recipe.ID);
                learnedNow++;
            }

            const int startingAmountPerBit = 5;
            bitLocker.AddBits(BuildUniformBitGrant(startingAmountPerBit));

            Debug.Log(
                "[Bootstrap/Tinkering] " +
                $"Learned {learnedNow} recipe(s), total known: {bitLocker.GetKnownRecipes().Count}. " +
                $"Granted {startingAmountPerBit} of each bit type.");
        }

        /// <summary>
        /// Debug-first mutation grant so projectile/spell FX is reachable immediately.
        /// Keeps the player blueprint stable while the progression loop is unfinished.
        /// </summary>
        private void GrantShowcaseSpellMutations()
        {
            if (_player == null)
                return;

            // KnowsPurifyWater is now learned by reading the Water-Keeper's Grimoire

            var mutations = _player.GetPart<MutationsPart>();
            if (mutations == null)
            {
                Debug.LogWarning("[Bootstrap/Mutations] Player has no MutationsPart; showcase spell grant skipped.");
                return;
            }

            string[] showcaseMutations =
            {
                "FireBoltMutation",
                "IceShardMutation",
                "PoisonSpitMutation",
                "PrismaticBeamMutation",
                "FrostNovaMutation",
                "ChainLightningMutation"
            };

            int granted = 0;
            for (int i = 0; i < showcaseMutations.Length; i++)
            {
                string className = showcaseMutations[i];
                if (mutations.HasMutation(className))
                    continue;

                if (mutations.AddMutation(className, 1))
                    granted++;
            }

            Debug.Log("[Bootstrap/Mutations] Granted " + granted + " showcase projectile mutation(s).");
        }

        private void GivePlayerStartingTonics()
        {
            if (_player == null || _factory == null)
                return;

            var inventory = _player.GetPart<InventoryPart>();
            if (inventory == null)
            {
                Debug.LogWarning("[Bootstrap/Tonics] Player has no InventoryPart; starting tonic grant skipped.");
                return;
            }

            int granted = 0;
            for (int i = 0; i < StartingTonicBlueprints.Length; i++)
            {
                string blueprint = StartingTonicBlueprints[i];
                var tonic = _factory.CreateEntity(blueprint);
                if (tonic == null)
                {
                    Debug.LogWarning($"[Bootstrap/Tonics] Failed to create starting tonic '{blueprint}'.");
                    continue;
                }

                if (!inventory.AddObject(tonic))
                {
                    Debug.LogWarning($"[Bootstrap/Tonics] Failed to add starting tonic '{blueprint}' to player inventory.");
                    continue;
                }

                granted++;
            }

            Debug.Log($"[Bootstrap/Tonics] Granted {granted} starting tonic(s) to the player.");
        }

        private static string BuildUniformBitGrant(int amountPerBit)
        {
            if (amountPerBit <= 0 || StartingBitTypes.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(StartingBitTypes.Length * amountPerBit);
            for (int i = 0; i < amountPerBit; i++)
            {
                for (int j = 0; j < StartingBitTypes.Length; j++)
                    builder.Append(StartingBitTypes[j]);
            }

            return builder.ToString();
        }

        private void LogAsciiBlueprintValidation()
        {
            if (_factory == null)
                return;

            var issues = _factory.ValidateAsciiWorldBlueprints();
            if (issues.Count == 0)
            {
                Debug.Log("[Bootstrap/ASCII] Blueprint render validation passed.");
                return;
            }

            Debug.LogWarning($"[Bootstrap/ASCII] Found {issues.Count} world render metadata issue(s).");
            for (int i = 0; i < issues.Count; i++)
                Debug.LogWarning($"[Bootstrap/ASCII] {issues[i]}");
        }

        private void LogHandlingBlueprintValidation()
        {
            if (_factory == null)
                return;

            var issues = _factory.ValidateHandlingBlueprints();
            if (issues.Count == 0)
            {
                Debug.Log("[Bootstrap/Handling] Blueprint handling validation passed.");
                return;
            }

            Debug.LogWarning($"[Bootstrap/Handling] Found {issues.Count} handling metadata issue(s).");
            for (int i = 0; i < issues.Count; i++)
                Debug.LogWarning($"[Bootstrap/Handling] {issues[i]}");
        }

        /// <summary>
        /// Debug: spawn a weapon next to the player so equipment-drop-on-dismember can be tested.
        /// </summary>
        private void SpawnDebugWeaponNearPlayer()
        {
            var pos = _zone.GetEntityPosition(_player);
            if (pos.x < 0) return;

            // Try adjacent cells for open spots
            int[] dx = { 1, -1, 0, 0, 1, -1, 1, -1 };
            int[] dy = { 0, 0, 1, -1, 1, -1, -1, 1 };

            // Spawn dagger on the first open cell
            int spotsUsed = 0;
            for (int i = 0; i < dx.Length && spotsUsed < 2; i++)
            {
                int nx = pos.x + dx[i];
                int ny = pos.y + dy[i];
                if (!_zone.InBounds(nx, ny)) continue;
                var cell = _zone.GetCell(nx, ny);
                if (cell == null || !cell.IsPassable()) continue;

                if (spotsUsed == 0)
                {
                    var dagger = _factory.CreateEntity("Dagger");
                    if (dagger != null)
                    {
                        _zone.AddEntity(dagger, nx, ny);
                        Debug.Log($"[Bootstrap] Debug: Spawned {dagger.GetDisplayName()} at ({nx},{ny}) near player");
                    }
                }
                else
                {
                    string[] twoHanders = { "Battleaxe", "Greatsword", "Warhammer" };
                    string pick = twoHanders[UnityEngine.Random.Range(0, twoHanders.Length)];
                    var weapon = _factory.CreateEntity(pick);
                    if (weapon != null)
                    {
                        _zone.AddEntity(weapon, nx, ny);
                        Debug.Log($"[Bootstrap] Debug: Spawned {weapon.GetDisplayName()} at ({nx},{ny}) near player");
                    }
                }
                spotsUsed++;
            }
        }

        /// <summary>
        /// Debug: spawn a friendly NPC near the player for dialogue testing.
        /// </summary>
        private void SpawnDebugNPCNearPlayer()
        {
            var pos = _zone.GetEntityPosition(_player);
            if (pos.x < 0) return;

            // Find an open adjacent cell (try 2 cells away so it's not blocked by weapons)
            int[] offsets = { 2, -2, 3, -3 };
            for (int i = 0; i < offsets.Length; i++)
            {
                int nx = pos.x + offsets[i];
                int ny = pos.y;
                if (!_zone.InBounds(nx, ny)) continue;
                var cell = _zone.GetCell(nx, ny);
                if (cell == null || !cell.IsPassable()) continue;

                var elder = _factory.CreateEntity("Elder");
                if (elder != null)
                {
                    _zone.AddEntity(elder, nx, ny);

                    // Give elder trade inventory and currency
                    TradeSystem.SetDrams(elder, 200);
                    var elderInv = elder.GetPart<InventoryPart>();
                    if (elderInv != null)
                    {
                        var tonic = _factory.CreateEntity("HealingTonic");
                        if (tonic != null) elderInv.AddObject(tonic);
                        var dagger = _factory.CreateEntity("Dagger");
                        if (dagger != null) elderInv.AddObject(dagger);
                        var armor = _factory.CreateEntity("LeatherArmor");
                        if (armor != null) elderInv.AddObject(armor);
                    }

                    Debug.Log($"[Bootstrap] Debug: Spawned {elder.GetDisplayName()} at ({nx},{ny}) near player");
                }
                return;
            }
        }

        /// <summary>
        /// Find an open cell near the center of the zone to place the player.
        /// Searches outward from center in a spiral.
        /// </summary>
        private void PlacePlayerInOpenCell()
        {
            int cx = Zone.Width / 2;
            int cy = Zone.Height / 2;

            for (int radius = 0; radius < Math.Max(Zone.Width, Zone.Height); radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue;
                        int x = cx + dx;
                        int y = cy + dy;
                        if (!_zone.InBounds(x, y)) continue;
                        var cell = _zone.GetCell(x, y);
                        if (cell != null && cell.IsPassable())
                        {
                            _zone.AddEntity(_player, x, y);
                            return;
                        }
                    }
                }
            }

            // Fallback: place at center regardless
            _zone.AddEntity(_player, cx, cy);
        }

        /// <summary>
        /// Register all creatures in the zone with the turn manager.
        /// </summary>
        private void RegisterCreaturesForTurns()
        {
            var creatures = _zone.GetEntitiesWithTag("Creature");
            foreach (var creature in creatures)
            {
                _turnManager.AddEntity(creature);

                // Wire BrainPart with zone and RNG so AI can function
                var brain = creature.GetPart<BrainPart>();
                if (brain != null)
                {
                    brain.CurrentZone = _zone;
                    brain.Rng = new System.Random();

                    // Set starting cell eagerly so it's available before first turn
                    var pos = _zone.GetEntityPosition(creature);
                    if (pos.x >= 0)
                    {
                        brain.StartingCellX = pos.x;
                        brain.StartingCellY = pos.y;
                    }
                }
            }
            Debug.Log($"GameBootstrap: Registered {creatures.Count} creatures for turns");
        }
    }
}
