using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    internal sealed class HotbarSaveFixture : IDisposable
    {
        internal const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        private const BindingFlags Static=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
        private readonly NewGameSaveFixture _save=new NewGameSaveFixture();
        private readonly List<(FieldInfo field,object value,object[] items)> _statics=new List<(FieldInfo,object,object[])>();
        private readonly List<Camera> _borrowedCameras=new List<Camera>();
        public readonly GameObject Root;
        public readonly GameBootstrap Bootstrap;
        public readonly InputHandler Input;
        public readonly ZoneRenderer Renderer;
        public HotbarSaveFixture(bool withInput=true,bool withRenderer=true)
        {
            try
            {
                foreach(var type in new[]{typeof(NarrativeStatePart),typeof(StoryletPart),typeof(WorldClock),typeof(BeatingGlareSystem),typeof(ConversationManager),typeof(SettlementRuntime),typeof(ZoneRenderHooks)})Snapshot(type);
                Field(typeof(TurnManager),"World");Field(typeof(MessageLog),"TickProvider");
                foreach(var type in new[]{typeof(ConversationActions),typeof(MaterialReactionResolver),typeof(CorpsePart),typeof(LoadoutPart),typeof(LootDropSystem),typeof(ContainerPlacementService),typeof(TraderPart),typeof(Cryomancy_GlacialWall),typeof(LayRuneGoal),typeof(PricklebrowNestPart),typeof(AlchemyStillPart),typeof(ForgePart),typeof(SeedPart),typeof(CropSystem)})Field(type,"Factory");
                Field(typeof(DestructionSystem),"EntityFactoryRef");
                // Do not let ApplyLoadedGame end or alter a borrowed speaker's conversation.
                ConversationManager.Speaker=null;ConversationManager.Listener=null;ConversationManager.CurrentConversation=null;ConversationManager.CurrentNode=null;
                foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(camera.CompareTag("MainCamera")){_borrowedCameras.Add(camera);camera.tag="Untagged";}
                Root=new GameObject("Hotbar save fixture");Root.SetActive(false);
                Bootstrap=Root.AddComponent<GameBootstrap>();
                if(withInput)Input=Root.AddComponent<InputHandler>();
                if(withRenderer)Renderer=Root.AddComponent<ZoneRenderer>();
                Bootstrap.ZoneRenderer=Renderer;
                BindOld(MakeState(0,0));
            }
            catch{Dispose();throw;}
        }
        private void Snapshot(Type type)
        {foreach(var field in type.GetFields(Static))if(!field.IsLiteral&&!field.IsInitOnly)Store(field);}
        private void Field(Type type,string name)
        {var field=type.GetField(name,Static);Assert.NotNull(field,type.Name+"."+name);Store(field);}
        private void Store(FieldInfo field)
        {
            object value=field.GetValue(null);object[] items=null;
            if(value is IList list&&!list.IsFixedSize&&!list.IsReadOnly)items=list.Cast<object>().ToArray();
            _statics.Add((field,value,items));
        }
        public void BindOld(GameSessionState state)
        {
            Set(Bootstrap,"_player",state.Player);Set(Bootstrap,"_zoneManager",state.ZoneManager);Set(Bootstrap,"_zone",state.ZoneManager.ActiveZone);Set(Bootstrap,"_turnManager",state.TurnManager);Set(Bootstrap,"_gameID",state.GameID);
            if(Input!=null){Input.PlayerEntity=state.Player;Input.CurrentZone=state.ZoneManager.ActiveZone;Input.ZoneManager=state.ZoneManager;Input.TurnManager=state.TurnManager;Input.ZoneRenderer=Renderer;Set(Input,"_selectedHotbarSlot",state.SelectedHotbarSlot);}
            if(Renderer!=null){Renderer.PlayerEntity=state.Player;Renderer.SetHotbarState(state.SelectedHotbarSlot,null);}
        }
        public static GameSessionState MakeState(int selected,params int[] slots)
        {
            var player=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="HotbarSavePlayer"};player.Tags["Player"]="true";
            player.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=20,Max=20,Owner=player};player.Statistics["Speed"]=new Stat{Name="Speed",BaseValue=100,Max=1000,Owner=player};FarmingAccessGrant.MarkGranted(player);
            var abilities=new ActivatedAbilitiesPart();player.AddPart(abilities);
            foreach(int slot in slots){Guid id=abilities.AddAbility("Audit "+slot,"AuditHotbar"+slot,"Audit");abilities.AssignAbilityToSlot(id,slot);}
            var zone=new Zone("Overworld.10.10.0");Assert.IsTrue(zone.AddEntity(player,3,4));
            var manager=new OverworldZoneManager(null,333);manager.ReplaceLoadedState(new Dictionary<string,Zone>{{zone.ZoneID,zone}},zone.ZoneID,new Dictionary<string,List<ZoneConnection>>());
            var turns=new TurnManager();turns.RestoreSavedState(17,true,player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=player,Energy=1000}});
            return GameSessionState.Capture(Guid.NewGuid().ToString("N"),"hotbar-audit",manager,turns,player,selected);
        }
        public GameSessionState Capture()=>(GameSessionState)typeof(GameBootstrap).GetMethod("CaptureGameSessionState",Flags).Invoke(Bootstrap,null);
        public static GameSessionState RoundTrip(GameSessionState state)
        {using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;return GameSessionState.Load(new SaveReader(stream,null));}}
        public int Selected=>Input==null?-1:(int)Get(Input,"_selectedHotbarSlot");
        public int Rendered=>Renderer==null?-1:(int)Get(Renderer,"_selectedHotbarSlot");
        public void Restore(int selection)
        {var method=typeof(InputHandler).GetMethod("RestoreHotbarSelection",Flags);Assert.NotNull(method,"Explicit selection restoration must not cast an ability.");method.Invoke(Input,new object[]{selection});}
        public int CaptureInput()
        {var method=typeof(InputHandler).GetMethod("CaptureHotbarSelection",Flags);Assert.NotNull(method);return (int)method.Invoke(Input,null);}
        public static object Get(object owner,string name)=>owner.GetType().GetField(name,Flags).GetValue(owner);
        public static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Flags).SetValue(owner,value);
        public void Dispose()
        {
            if(Root!=null)UnityEngine.Object.DestroyImmediate(Root);
            for(int i=_statics.Count-1;i>=0;i--){var entry=_statics[i];entry.field.SetValue(null,entry.value);if(entry.items!=null){var list=(IList)entry.value;list.Clear();foreach(var item in entry.items)list.Add(item);}}
            foreach(var camera in _borrowedCameras)if(camera!=null)camera.tag="MainCamera";
            _save.Dispose();
        }
    }
    public class GameAuditHotbarSelectionTests
    {
        [TestCase(0)] [TestCase(5)] [TestCase(9)]
        public void BootstrapCaptureUsesActualOccupiedSelection(int slot)
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(slot,slot));Assert.AreEqual(slot,f.Capture().SelectedHotbarSlot);}}
        [Test]
        public void CaptureBeforeFirstUpdateFallsBackToFirstOccupiedSlot()
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(-1,5,9));Assert.AreEqual(5,f.Capture().SelectedHotbarSlot);Assert.AreEqual(5,f.Selected);}}
        [TestCase(false)] [TestCase(true)]
        public void MissingInputOrEmptyAbilitiesCaptureNoSelection(bool withInput)
        {using(var f=new HotbarSaveFixture(withInput)){f.BindOld(HotbarSaveFixture.MakeState(0));Assert.AreEqual(-1,f.Capture().SelectedHotbarSlot);}}
        [Test]
        public void ApplyLoadValidatesAgainstReplacementActorBeforeSynchronizingRenderer()
        {
            using(var f=new HotbarSaveFixture())
            {var loaded=HotbarSaveFixture.RoundTrip(HotbarSaveFixture.MakeState(9,5,9));f.Bootstrap.ApplyLoadedGame(loaded);Assert.AreSame(loaded.Player,f.Input.PlayerEntity);Assert.AreSame(loaded.Player,f.Renderer.PlayerEntity);Assert.AreEqual(9,f.Selected);Assert.AreEqual(9,f.Rendered);}
        }
        [Test]
        public void ApplyLoadOfEmptyHotbarClearsOldSelectionImmediately()
        {using(var f=new HotbarSaveFixture()){var loaded=HotbarSaveFixture.RoundTrip(HotbarSaveFixture.MakeState(9));f.Bootstrap.ApplyLoadedGame(loaded);Assert.AreEqual(-1,f.Selected);Assert.AreEqual(-1,f.Rendered);}}
        [Test]
        public void ApplyLoadRestoresZeroFromDifferentOldSelection()
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(9,9));f.Bootstrap.ApplyLoadedGame(HotbarSaveFixture.RoundTrip(HotbarSaveFixture.MakeState(0,0,5)));Assert.AreEqual(0,f.Selected);Assert.AreEqual(0,f.Rendered);}}
        [Test]
        public void LoadedCooldownAbilityRemainsSelectedWithoutChangingItsClock()
        {
            using(var f=new HotbarSaveFixture())
            {var state=HotbarSaveFixture.MakeState(9,5,9);state.Player.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(9).CooldownRemaining=7;var loaded=HotbarSaveFixture.RoundTrip(state);f.Bootstrap.ApplyLoadedGame(loaded);Assert.AreEqual(9,f.Selected);Assert.AreEqual(9,f.Rendered);Assert.AreEqual(7,loaded.Player.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(9).CooldownRemaining);Assert.AreEqual(17,loaded.TurnManager.TickCount);Assert.AreEqual(1000,loaded.TurnManager.GetEnergy(loaded.Player));}
        }
        [Test]
        public void ExistingV7FieldAlreadyRoundTripsNonzeroSelection()
        {using(var f=new HotbarSaveFixture()){var loaded=HotbarSaveFixture.RoundTrip(HotbarSaveFixture.MakeState(9,5,9));Assert.AreEqual(7,loaded.SaveVersion);Assert.AreEqual(9,loaded.SelectedHotbarSlot);Assert.AreSame(loaded.Player,loaded.TurnManager.CurrentActor);}}
        [Test]
        public void ExplicitRestoreSelectsWithoutTargetingOrSpendingActionCost()
        {
            using(var f=new HotbarSaveFixture())
            {f.BindOld(HotbarSaveFixture.MakeState(5,5,9));var actor=f.Input.PlayerEntity;var turns=f.Input.TurnManager;var pending=HotbarSaveFixture.Get(f.Input,"_pendingAbility");var mode=HotbarSaveFixture.Get(f.Input,"_inputState");f.Restore(9);Assert.AreEqual(9,f.Selected);Assert.AreEqual(9,f.Rendered);Assert.AreEqual(mode,HotbarSaveFixture.Get(f.Input,"_inputState"));Assert.AreSame(pending,HotbarSaveFixture.Get(f.Input,"_pendingAbility"));Assert.AreEqual(17,turns.TickCount);Assert.AreEqual(1000,turns.GetEnergy(actor));}
        }
        [Test]
        public void UnoccupiedSavedSlotFallsBackToFirstOccupiedLoadedSlot()
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(9,5,9));f.Restore(3);Assert.AreEqual(5,f.Selected);Assert.AreEqual(5,f.Rendered);}}
    }
}
