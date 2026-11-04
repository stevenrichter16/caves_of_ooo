using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using CavesOfOoo.Concerns;
using CavesOfOoo.Skills;
using UnityEngine;

namespace CavesOfOoo.Web
{
    // A single-session adapter. The simulation remains the game's original C#.
    public partial class BrowserGame
    {
        EntityFactory factory;
        OverworldZoneManager zones;
        TurnManager turns;
        Entity player, world, selected, trader, container;
        Zone zone;
        System.Random rng;
        string gameId, buildId, panel = "", error = "";
        int revision, lookX, lookY;
        bool initialized;
        public int Revision => revision;
        public bool IsReady => player != null && zone != null;
        public bool Dead => IsReady && player.GetStatValue("Hitpoints") <= 0;
        static string Text(string key) => Resources.Load<TextAsset>(key)?.text;
        static IEnumerable<string> Texts(string key) => Resources.LoadAll<TextAsset>(key).Select(a=>a.text);

        public string Catalog()
        {
            StartingBuildRegistry.LoadFromResources();
            return JsonConvert.SerializeObject(StartingBuildRegistry.All);
        }
        void Initialize()
        {
            if(initialized) return;
            FactionManager.Initialize(Text("Content/Data/Factions"));
            MaterialReactionResolver.InitializeFromJsonSources(Texts("Content/Data/MaterialReactions"));
            LiquidRegistry.InitializeFromJsonSources(Texts("Content/Data/LiquidDefinitions"));
            GasRegistry.InitializeFromJsonSources(Texts("Content/Data/GasDefinitions"));
            HouseDramaLoader.LoadAll(); foreach(var drama in HouseDramaLoader.GetAll()) HouseDramaRuntime.RegisterDrama(drama);
            StoryletRegistry.LoadAll(); ConcernRegistry.LoadAll(); ConversationLoader.LoadAll();
            factory = new EntityFactory(); factory.LoadBlueprints(Text("Content/Blueprints/Objects"));
            BindFactory(); ResonanceSystem.EnsureInitialized(); TileReactionSystem.EnsureInitialized();
            LootTableRegistry.InitializeFromJsonSources(Texts("Content/Data/Loot"));
            StartingBuildRegistry.LoadFromResources();
            initialized = true;
        }
        void BindFactory()
        {
            ConversationActions.Factory=factory; MaterialReactionResolver.Factory=factory;
            CorpsePart.Factory=factory; LoadoutPart.Factory=factory; LootDropSystem.Factory=factory;
            ContainerPlacementService.Factory=factory; TraderPart.Factory=factory;
            DestructionSystem.EntityFactoryRef=factory; Cryomancy_GlacialWall.Factory=factory;
            LayRuneGoal.Factory=factory; PricklebrowNestPart.Factory=factory; AlchemyStillPart.Factory=factory;
            ForgePart.Factory=factory; SeedPart.Factory=factory; CropSystem.Factory=factory;
            HarvestablePart.Factory=factory; TraderRestockSystem.Factory=factory;
        }
        public string NewGame(string build, int seed, string name)
        {
            Initialize();
            var def=StartingBuildRegistry.Get(build);
            if(def==null) throw new ArgumentException("Choose an available starting build.");
            ConversationManager.EndConversation(); MessageLog.Clear(); AsciiFxBus.Clear(); SpellFxBus.Clear();
            WorldClock.Reset(); BeatingGlareSystem.Reset();
            FactionManager.Initialize(Text("Content/Data/Factions"));
            rng=new System.Random(seed); buildId=def.Id; gameId=Guid.NewGuid().ToString("N");
            world=new Entity {BlueprintName="World"}; world.SetTag("WorldEntity");
            var narrative=new NarrativeStatePart(); world.AddPart(narrative); NarrativeStatePart.Current=narrative;
            var stories=new StoryletPart(); world.AddPart(stories); StoryletPart.Current=stories; narrative.RegisterReactor(stories);
            world.AddPart(new GasSystemPart()); world.AddPart(new CropSystemPart()); TurnManager.World=world;
            zones=new OverworldZoneManager(factory,seed);
            player=factory.CreateEntity("Player");
            var result=StartingBuildService.Apply(player,factory,def);
            if(!result.Success) throw new InvalidOperationException(string.Join(" ",result.Errors));
            player.GetPart<RenderPart>().DisplayName=string.IsNullOrWhiteSpace(name)?"Wayfarer":name.Trim().Substring(0,Math.Min(name.Trim().Length,32));
            var pack=player.GetPart<InventoryPart>();
            foreach(var entry in new[]{("WateringGrimoire",1),("CandyCarrotSeed",4),("EmberwheatSeed",2)})
            { var item=factory.CreateEntity(entry.Item1); if(item.GetPart<StackerPart>() is StackerPart stack)stack.StackCount=entry.Item2;pack.AddObject(item); }
            FarmingAccessGrant.MarkGranted(player);
            if(!FreshGameStart.TryPlace(zones,player,"",out zone))throw new InvalidOperationException("No safe starting place was found.");
            zones.SetActiveZone(zone); turns=new TurnManager(); StoryletPart.LocalPlayer=player;
            BindClock(); RegisterCreatures(true); SettlementRuntime.ActiveZone=zone;
            turns.ProcessUntilPlayerTurn(); CropTime.ReconcileZone(zone,turns.TickCount); KitchenBatchPart.ReconcileZone(zone);
            selected=trader=container=null; panel="";error="";faulted=false;revision++; UpdateVision();
            return Snapshot();
        }
        void BindClock() { zones.SetTurnProvider(()=>turns.TickCount);MessageLog.TickProvider=()=>turns.TickCount; }
        void RegisterCreatures(bool register)
        {
            foreach(var creature in zone.GetEntitiesWithTag("Creature"))
            {
                if(register)turns.AddEntity(creature);
                var brain=creature.GetPart<BrainPart>(); if(brain==null)continue;
                brain.CurrentZone=zone;brain.Rng=rng;
                var pos=zone.GetEntityPosition(creature); if(pos.x>=0){brain.StartingCellX=pos.x;brain.StartingCellY=pos.y;}
            }
        }
        void UpdateVision()
        {
            var cell=zone.GetEntityCell(player);
            if(cell!=null)FieldOfView.Compute(zone,cell.X,cell.Y,(int)Math.Ceiling(Math.Sqrt(Zone.Width*Zone.Width+Zone.Height*Zone.Height)));
        }
        void EndTurn()
        {
            turns.EndTurn(player,zone);
            try { turns.ProcessUntilPlayerTurn(); }
            catch { turns.ForceYieldToPlayer(); throw; } // Never retry an NPC turn.
            CropTime.ReconcileZone(zone,turns.TickCount); KitchenBatchPart.ReconcileZone(zone);
            MaterialSimSystem.TickMaterialEntities(zone); ZoneTileStateSystem.OnPlayerTurnEnd(zone);
            WorldClock.NotifyPlayerTurnEnd(turns.TickCount); BeatingGlareSystem.OnPlayerTurnEnd(player,zone,turns.TickCount);
            AsciiFxBus.Clear(); SpellFxBus.Clear(); // Outcomes are synchronous; Unity animation queues are presentation only.
            UpdateVision();
        }
        void EnterZone(ZoneTransitionResult result)
        {
            foreach(var e in zone.GetEntitiesWithTag("Creature"))if(e!=player)turns.RemoveEntity(e);
            zone=result.NewZone;zones.SetActiveZone(zone);
            CropTime.ReconcileZone(zone,turns.TickCount);KitchenBatchPart.ReconcileZone(zone);
            WorldTravellers.OnZoneEntered(player,zone);ClothClaims.OnZoneEntered(player,zone);UrquPressure.OnZoneEntered(player,zone);ConcernWorld.OnZoneEntered(player,zone);
            foreach(var e in zone.GetEntitiesWithTag("Creature"))if(e!=player)turns.AddEntity(e);
            RegisterCreatures(false);SettlementRuntime.ActiveZone=zone;
            zones.SettlementManager?.RefreshActiveZonePresentation(zone);
            if(zones.SettlementManager!=null)foreach(var msg in zones.SettlementManager.ConsumePendingMessages(zone.ZoneID))MessageLog.Add(msg);
            SariAmbience.OnZoneEntered(player,zone);EconomyUpkeep.OnZoneEntered(player,zone,turns.TickCount);
            selected=trader=container=null; panel="";
        }
        public byte[] Save()
        {
            if(!IsReady)throw new InvalidOperationException("No expedition is open.");
            if(ActionResolutionGuard.IsUnstable)throw new InvalidOperationException("Wait for the action to settle.");
            if(faulted)throw new InvalidOperationException("Restore the last checkpoint before saving.");
            if(!turns.WaitingForInput && !Dead)throw new InvalidOperationException("Wait for this turn to finish.");
            using var stream=new MemoryStream();var writer=new SaveWriter(stream);
            GameSessionState.Capture(gameId,"standalone-web-1",zones,turns,player,0,world).Save(writer);
            return stream.ToArray();
        }
        public string Load(byte[] raw)
        {
            byte[] previous=IsReady&&!faulted&&!ActionResolutionGuard.IsUnstable&&(turns.WaitingForInput||Dead)?Save():null;int previousRevision=revision;var previousTurns=TurnManager.Active;
            try{return LoadCandidate(raw);}
            catch
            {
                if(previous!=null&&!ReferenceEquals(previousTurns,TurnManager.Active)){try{LoadCandidate(previous);revision=previousRevision;}catch{faulted=true;}}
                throw;
            }
        }
        string LoadCandidate(byte[] raw)
        {
            Initialize();
            if(raw==null || raw.Length<12 || raw.Length>128*1024*1024)throw new InvalidDataException("The checkpoint is incomplete or too large.");
            using var stream=new MemoryStream(raw,false);var reader=new SaveReader(stream,factory);
            var state=GameSessionState.Load(reader); // Core validates the complete candidate before publication.
            if(state.Player==null || state.ZoneManager?.ActiveZone==null || state.TurnManager==null)throw new InvalidDataException("The checkpoint has no complete expedition.");
            player=state.Player;world=state.World;zones=state.ZoneManager;turns=state.TurnManager;zone=zones.ActiveZone;gameId=state.GameID;
            rng=new System.Random(state.WorldSeed^turns.TickCount);buildId=player.GetProperty("StartingBuild");
            NarrativeStatePart.Current=world?.GetPart<NarrativeStatePart>();TurnManager.World=world;StoryletPart.Current=world?.GetPart<StoryletPart>();StoryletPart.LocalPlayer=player;
            if(NarrativeStatePart.Current!=null && StoryletPart.Current!=null)NarrativeStatePart.Current.RegisterReactor(StoryletPart.Current);
            BindFactory();BindClock();RegisterCreatures(false);SettlementRuntime.ActiveZone=zone;
            ConversationManager.EndConversation();WorldClock.Reset();BeatingGlareSystem.Reset();
            foreach(var saved in zones.CachedZones.Values)CropTime.InitializeLegacyZone(saved,turns.TickCount);
            CropTime.ReconcileZone(zone,turns.TickCount);KitchenBatchPart.ReconcileZone(zone);zones.SettlementManager?.RefreshActiveZonePresentation(zone);
            selected=trader=container=null;pendingThrow=null;panel="";error="";faulted=false;revision++;UpdateVision();return Snapshot();
        }
    }
}
