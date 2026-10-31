using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class WorldLogHistoryInputTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        readonly InputTestFixture devices=new InputTestFixture();
        readonly List<GameObject> objects=new List<GameObject>();
        List<MessageLog.Entry> saved;List<string> queue;int flash,next;Func<int> tickProvider;Action<string> onMessage;
        TurnManager oldActive;Entity oldWorld;
        InputHandler input;Entity actor;Zone zone;TurnManager turns;Keyboard keyboard;Gamepad pad;Tilemap fg,bg;
        [SetUp] public void SetUp()
        {
            saved=MessageLog.GetAllEntries();queue=MessageLog.GetPendingAnnouncementsSnapshot();flash=MessageLog.FlashStamp;next=MessageLog.NextSerialValue;tickProvider=MessageLog.TickProvider;onMessage=MessageLog.OnMessage;
            oldActive=TurnManager.Active;oldWorld=TurnManager.World;TurnManager.World=null;
            MessageLog.OnMessage=null;MessageLog.TickProvider=null;MessageLog.Clear();devices.Setup();
            keyboard=InputSystem.AddDevice<Keyboard>();pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();
            var host=Make("history input");host.SetActive(false);input=host.AddComponent<InputHandler>();
            input.WorldActionMenuUI=host.AddComponent<WorldActionMenuUI>();input.AnnouncementUI=host.AddComponent<AnnouncementUI>();
            var grid=Make("history grid");grid.AddComponent<Grid>();var text=Make("history text");text.transform.SetParent(grid.transform);fg=text.AddComponent<Tilemap>();
            var background=Make("history background");background.transform.SetParent(grid.transform);bg=background.AddComponent<Tilemap>();
            input.AnnouncementUI.Tilemap=fg;input.AnnouncementUI.BgTilemap=bg;input.WorldActionMenuUI.Tilemap=fg;input.WorldActionMenuUI.BgTilemap=bg;
            actor=new Entity{BlueprintName="Player"};actor.SetTag("Player");actor.SetTag("Creature");actor.AddPart(new PhysicsPart{Solid=true});actor.AddPart(new RenderPart{DisplayName="you"});actor.AddPart(new InventoryPart());
            actor.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=20,Max=20};actor.Statistics["Speed"]=new Stat{Name="Speed",BaseValue=100,Min=25,Max=200};
            zone=new Zone("HistoryInput");zone.AddEntity(actor,10,10);zone.GetCell(10,10).IsVisible=true;
            turns=new TurnManager();turns.AddEntity(actor);turns.ProcessUntilPlayerTurn();input.PlayerEntity=actor;input.CurrentZone=zone;input.TurnManager=turns;
            Pad(new GamepadState());
            for(int i=0;i<100;i++)MessageLog.Add("old-record-"+i.ToString("D3"));
        }
        [TearDown] public void TearDown()
        { for(int i=objects.Count-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(objects[i]);objects.Clear();devices.TearDown();TurnManager.World=oldWorld;typeof(TurnManager).GetProperty("Active",BindingFlags.Public|BindingFlags.Static).SetValue(null,oldActive);MessageLog.Restore(saved,queue,flash,next);MessageLog.TickProvider=tickProvider;MessageLog.OnMessage=onMessage; }
        GameObject Make(string name){var go=new GameObject(name);objects.Add(go);return go;}
        object Call(string name,params object[] args){var m=typeof(InputHandler).GetMethod(name,Private);Assert.NotNull(m,name);return m.Invoke(input,args);}
        string State=>typeof(InputHandler).GetField("_inputState",Private).GetValue(input).ToString();
        void Mode(string state){if(state=="LookMode"){Assert.True((bool)Call("EnterLookMode"));return;}var f=typeof(InputHandler).GetField("_inputState",Private);f.SetValue(input,Enum.Parse(f.FieldType,state));}
        void Update(){typeof(InputHandler).GetField("_lastMoveTime",Private).SetValue(input,-999f);Call("Update");}
        void Pad(GamepadState value){InputSystem.QueueStateEvent(pad,value);InputSystem.Update();Update();}
        void Key(params UnityEngine.InputSystem.Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();Update();}
        // Unity clamps a camera viewport to the actual screen (batch mode is
        // 640x480 with the legacy pointer at the origin), so a rect centred on
        // the pointer can lose its lower-left half. Fill the real screen and
        // move the camera until the unmoved pointer lands on the requested row.
        Camera OverlapPointer(string name,Vector3 pointer,int rowCount,int row=0)
        {
            var camera=Make(name).AddComponent<Camera>();camera.enabled=false;
            camera.pixelRect=new Rect(0,0,Screen.width,Screen.height);
            if(!camera.pixelRect.Contains(pointer))Assert.Inconclusive("The legacy pointer "+pointer+" is outside the "+Screen.width+"x"+Screen.height+" screen.");
            camera.orthographic=true;camera.orthographicSize=22.5f;
            var target=new Vector3(CenteredPopupLayout.GetCenteredOriginX(48)+2.5f,CenteredPopupLayout.GetCenteredTopY(rowCount+5)-3-row+.5f,-10);
            camera.transform.position=target;
            var hit=camera.ScreenToWorldPoint(new Vector3(pointer.x,pointer.y,10));
            camera.transform.position=new Vector3(2*target.x-hit.x,2*target.y-hit.y,target.z);
            return camera;
        }
        void Unchanged(int tick,int energy){Assert.AreEqual(tick,turns.TickCount);Assert.AreEqual(energy,turns.GetEnergy(actor));Assert.AreEqual((10,10),zone.GetEntityPosition(actor));}
        [Test] public void ActualCharacterMenuAppendsWorldLogAndNativeAOpensOlderHistoryWithoutCost()
        {
            int tick=turns.TickCount,energy=turns.GetEnergy(actor);Pad(new GamepadState().WithButton(GamepadButton.Start));Assert.AreEqual("ControllerMenu",State);
            var rows=(List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",Private).GetValue(input.WorldActionMenuUI);
            CollectionAssert.AreEqual(new[]{"Inventory and equipment","Attributes and current effects","Skills","Abilities","Quest log","Factions","Controls","World log"},rows.Select(x=>x.Display).ToArray());
            Pad(new GamepadState());input.WorldActionMenuUI.RestoreHighlight(rows.Last());Pad(new GamepadState().WithButton(GamepadButton.South));
            Assert.AreEqual("LogHistoryOpen",State);Assert.True(input.LogHistoryUI.IsOpen);Assert.False(input.AnnouncementUI.IsOpen);Assert.True(fg.GetUsedTilesCount()>0);
            Pad(new GamepadState());for(int i=0;i<12&&input.LogHistoryUI.Model.FirstEntryIndex>0;i++){Pad(new GamepadState().WithButton(GamepadButton.LeftShoulder));Pad(new GamepadState());}
            Assert.Less(input.LogHistoryUI.Model.FirstEntryIndex,30);Pad(new GamepadState().WithButton(GamepadButton.East));Assert.AreEqual("Normal",State);Assert.False(input.LogHistoryUI.IsOpen);Unchanged(tick,energy);
        }
        [TestCase(true)] [TestCase(false)]
        public void StationaryLegacyPointerCannotUndoCharacterNavigationToWorldLog(bool controller)
        {
            int tick=turns.TickCount,energy=turns.GetEnergy(actor);
            Pad(new GamepadState().WithButton(GamepadButton.Start));Assert.AreEqual("ControllerMenu",State);
            var menu=input.WorldActionMenuUI;
            var rows=(List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",Private).GetValue(menu);
            Assert.AreEqual("World log",rows.Last().Display);

            // Project the actual legacy pointer onto row zero without moving it
            // or replacing Input.mousePosition. This recreates the native overlap.
            Vector3 pointer=UnityEngine.Input.mousePosition;
            menu.PopupCamera=OverlapPointer("stationary pointer overlap",pointer,rows.Count);
            Assert.AreEqual(0,(int)typeof(WorldActionMenuUI).GetMethod("GetRowAtMouse",Private).Invoke(menu,null),
                "Real pointer must hit row zero, otherwise the old hover-reset defect is not exercised.");
            Pad(new GamepadState());Key();
            for(int i=1;i<rows.Count;i++)
            {
                if(controller)Pad(new GamepadState().WithButton(GamepadButton.DpadDown));else Key(UnityEngine.InputSystem.Key.DownArrow);
                Assert.AreSame(rows[i],menu.HighlightedAction,"Each fresh direction reaches the next actual row.");
                if(controller)Pad(new GamepadState());else Key();
                Assert.AreEqual(pointer,UnityEngine.Input.mousePosition,"This regression requires a stationary legacy pointer.");
                Assert.AreSame(rows[i],menu.HighlightedAction,"A neutral frame must not restore the stale hover row.");
            }
            if(controller)Pad(new GamepadState().WithButton(GamepadButton.South));else Key(UnityEngine.InputSystem.Key.Enter);
            Assert.AreEqual("LogHistoryOpen",State);Assert.True(input.LogHistoryUI.IsOpen);Unchanged(tick,energy);
        }

        [Test]
        public void PointerOverlapFixtureFollowsTheRequestedRowAtTheActualScreenSize()
        {
            Pad(new GamepadState().WithButton(GamepadButton.Start));Pad(new GamepadState());
            var menu=input.WorldActionMenuUI;
            var rows=(List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",Private).GetValue(menu);
            var hit=typeof(WorldActionMenuUI).GetMethod("GetRowAtMouse",Private);
            Vector3 pointer=UnityEngine.Input.mousePosition;
            foreach(int row in new[]{0,1,rows.Count-1})
            {
                menu.PopupCamera=OverlapPointer("fixture row "+row,pointer,rows.Count,row);
                Assert.AreEqual(row,(int)hit.Invoke(menu,null),"The fixture must aim the unmoved pointer at the row it names.");
            }
            // Counter-check: two lines above row zero is the title, not a choice.
            menu.PopupCamera=OverlapPointer("fixture title",pointer,rows.Count,-2);
            Assert.AreEqual(-1,(int)hit.Invoke(menu,null),"The title line is not a selectable row.");
            Assert.AreEqual(pointer,UnityEngine.Input.mousePosition);
        }

        [Test]
        public void PriorPointerMovementReclaimsHoverOnceAndOpenRearmsTheSample()
        {
            Pad(new GamepadState().WithButton(GamepadButton.Start));Pad(new GamepadState());
            var menu=input.WorldActionMenuUI;
            var rows=(List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",Private).GetValue(menu);
            Vector3 pointer=UnityEngine.Input.mousePosition;
            menu.PopupCamera=OverlapPointer("pointer rearm overlap",pointer,rows.Count);
            Assert.AreEqual(0,(int)typeof(WorldActionMenuUI).GetMethod("GetRowAtMouse",Private).Invoke(menu,null));
            var previous=typeof(WorldActionMenuUI).GetField("_lastMousePosition",Private);
            Assert.NotNull(previous,"Menu must retain its previous pointer sample.");
            var last=rows.Last();menu.RestoreHighlight(last);
            // Seed only a prior sample; hit testing still reads the real legacy
            // pointer. This does not claim to synthesize OS mouse movement.
            previous.SetValue(menu,pointer+Vector3.right);menu.HandleInput();
            Assert.AreSame(rows[0],menu.HighlightedAction,"A changed sample reclaims hover normally.");
            menu.RestoreHighlight(last);menu.HandleInput();
            Assert.AreSame(last,menu.HighlightedAction,"The consumed movement cannot repeatedly defeat navigation.");
            previous.SetValue(menu,pointer+Vector3.right);
            menu.Open(actor,actor,zone.GetEntityCell(actor),new List<InventoryAction>(rows),zone,title:"Character");
            Assert.AreEqual(pointer,(Vector3)previous.GetValue(menu));
            menu.RestoreHighlight(last);menu.HandleInput();Assert.AreSame(last,menu.HighlightedAction);
            Assert.AreEqual(pointer,UnityEngine.Input.mousePosition);
        }

        [TestCase("Normal")] [TestCase("LookMode")]
        public void KeyboardF2HomeEndEscapeUsesHistoryAndRestoresExactInputState(string mode)
        {
            Mode(mode);int tick=turns.TickCount,energy=turns.GetEnergy(actor);Key(UnityEngine.InputSystem.Key.F2);Assert.AreEqual("LogHistoryOpen",State);Key();
            Key(UnityEngine.InputSystem.Key.Home);Assert.Zero(input.LogHistoryUI.Model.FirstEntryIndex);Key();Key(UnityEngine.InputSystem.Key.End);Assert.AreEqual(MessageLog.Count,input.LogHistoryUI.Model.EndEntryIndex);Key();
            Key(UnityEngine.InputSystem.Key.Escape);Assert.AreEqual(mode,State);Unchanged(tick,energy);
        }
        [TestCase("Normal")] [TestCase("LookMode")]
        public void HistoryDoesNotEnqueueConsumeOrAudioArmPendingAnnouncements(string mode)
        {
            MessageLog.AddAnnouncement("Queued exact document\nEND");var raw=MessageLog.GetAllEntries();var notices=MessageLog.GetPendingAnnouncementsSnapshot();int stamp=MessageLog.FlashStamp;
            Mode(mode);input.OpenLogHistory();Assert.AreEqual("LogHistoryOpen",State);input.LogHistoryUI.Model.Oldest();input.LogHistoryUI.Model.Latest();
            CollectionAssert.AreEqual(raw,MessageLog.GetAllEntries());CollectionAssert.AreEqual(notices,MessageLog.GetPendingAnnouncementsSnapshot());Assert.AreEqual(stamp,MessageLog.FlashStamp);Assert.False(input.AnnouncementUI.IsOpen);
            input.LogHistoryUI.Close();Call("HandleLogHistoryInput");Assert.AreEqual(mode,State);CollectionAssert.AreEqual(notices,MessageLog.GetPendingAnnouncementsSnapshot());
        }
        [Test] public void MissingPopupCanvasCannotTrapInputBehindAnInvisibleModal()
        {
            input.AnnouncementUI.Tilemap=null;input.AnnouncementUI.BgTilemap=null;input.OpenLogHistory();
            Assert.AreEqual("Normal",State);Assert.True(input.LogHistoryUI==null||!input.LogHistoryUI.IsOpen);
        }
        [Test] public void HistoryBrowsingPreservesExactSerializedLogBytesAndRestoresAllRawFields()
        {
            MessageLog.TickProvider=()=>123;MessageLog.AddAnnouncement("Raw\r\n\nquoted  history");
            byte[] Save(){using(var stream=new MemoryStream()){SaveGraphSerializer.SaveMessageLog(new SaveWriter(stream));return stream.ToArray();}}
            var before=Save();var entries=MessageLog.GetAllEntries();var announcements=MessageLog.GetPendingAnnouncementsSnapshot();int savedNext=MessageLog.NextSerialValue,savedFlash=MessageLog.FlashStamp;
            input.OpenLogHistory();input.LogHistoryUI.Model.Oldest();while(input.LogHistoryUI.Model.Newer()){}input.LogHistoryUI.Close();Call("HandleLogHistoryInput");
            CollectionAssert.AreEqual(before,Save());MessageLog.Clear();using(var stream=new MemoryStream(before))SaveGraphSerializer.LoadMessageLog(new SaveReader(stream,null));
            CollectionAssert.AreEqual(entries,MessageLog.GetAllEntries());CollectionAssert.AreEqual(announcements,MessageLog.GetPendingAnnouncementsSnapshot());Assert.AreEqual(savedNext,MessageLog.NextSerialValue);Assert.AreEqual(savedFlash,MessageLog.FlashStamp);
        }
        [Test] public void ExistingInventoryF2IsNotInterceptedByHistory()
        {
            Mode("InventoryOpen");input.OpenLogHistory();Assert.AreEqual("InventoryOpen",State);Assert.Null(input.LogHistoryUI);
        }
        [Test] public void ActorOrZoneReplacementClosesStaleHistoryInsteadOfReturningOldLookState()
        {
            Mode("LookMode");input.OpenLogHistory();input.CurrentZone=new Zone("replacement");Pad(new GamepadState());Assert.AreEqual("Normal",State);Assert.False(input.LogHistoryUI.IsOpen);
        }
        [Test] public void NativeHeldConfirmThroughCloseCannotWaitOrMoveTheWorld()
        {
            int tick=turns.TickCount,energy=turns.GetEnergy(actor);input.OpenLogHistory();Pad(new GamepadState());
            Pad(new GamepadState{rightTrigger=1});Pad(new GamepadState{rightTrigger=1}.WithButton(GamepadButton.East));Assert.AreEqual("Normal",State);
            Pad(new GamepadState{rightTrigger=1});Unchanged(tick,energy);Pad(new GamepadState());
        }
        [Test] public void RetainedViewDoesNotRepaintOrRewrapUntilInputContentOrDeviceActuallyChanges()
        {
            input.OpenLogHistory();var ui=input.LogHistoryUI;int render=ui.RenderCount,builds=ui.Model.BuildCount;
            for(int i=0;i<20;i++)ui.HandleInput();Assert.AreEqual(render,ui.RenderCount);Assert.AreEqual(builds,ui.Model.BuildCount);
            MessageLog.Add("new append");ui.HandleInput();Assert.Greater(ui.RenderCount,render);Assert.AreEqual(builds,ui.Model.BuildCount);
            ui.Close();Assert.Zero(fg.GetUsedTilesCount());Assert.Zero(bg.GetUsedTilesCount());
        }
    }
}
