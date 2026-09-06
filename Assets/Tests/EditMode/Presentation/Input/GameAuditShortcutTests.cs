using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public abstract class ShortcutFixture : StackIdentityFixture
    {
        protected Entity Player; protected Zone Zone; protected InventoryPart Inventory => Player.GetPart<InventoryPart>();
        protected readonly List<GameObject> Objects = new List<GameObject>();
        readonly InputTestFixture _inputFixture = new InputTestFixture();
        Keyboard _keyboard; EntityFactory _oldStill; TurnManager _oldTurns;
        protected static readonly string[] GroundBlueprints = { "Dagger", "SilverSand", "FireClay", "WardOil", "HealingTonic", "CandyCarrotSeed", "SteelBladeComponent" };
        [SetUp] public void SetupShortcuts()
        {
            Player=Actor(); Inventory.MaxWeight=-1; Zone=new Zone("ShortcutAudit"); Assert.IsTrue(Zone.AddEntity(Player,10,10));
            _inputFixture.Setup(); _keyboard=InputSystem.AddDevice<Keyboard>(); _keyboard.MakeCurrent(); Release();
            _oldTurns=TurnManager.Active; _oldStill=AlchemyStillPart.Factory; AlchemyStillPart.Factory=Factory;
            FactionManager.Initialize(); ConversationManager.EndConversation(); ConversationLoader.Reset(); ConversationPredicates.Reset(); ConversationActions.Reset();
            ConversationManager.PendingAttackTarget=null; ConversationManager.PendingTradePartner=null;
        }
        [TearDown] public void CleanupShortcuts()
        {
            ConversationManager.EndConversation(); ConversationManager.PendingAttackTarget=null; ConversationManager.PendingTradePartner=null;
            ConversationLoader.Reset(); ConversationPredicates.Reset(); ConversationActions.Reset(); FactionManager.Reset();
            foreach(var go in Objects) if(go!=null) UnityEngine.Object.DestroyImmediate(go); Objects.Clear();
            _inputFixture.TearDown();
            typeof(TurnManager).GetProperty("Active").GetSetMethod(true).Invoke(null,new object[]{_oldTurns});
            AlchemyStillPart.Factory=_oldStill;
        }
        protected T Component<T>() where T:Component
        {var go=new GameObject(typeof(T).Name); Objects.Add(go);return go.AddComponent<T>();}
        protected Tilemap Tiles() {var grid=Component<Grid>();var map=Component<Tilemap>();map.transform.SetParent(grid.transform);return map;}
        protected Camera OutsideCamera(){var c=Component<Camera>();c.orthographic=true;c.transform.position=new Vector3(10000,10000,-10);return c;}
        protected void Release(){InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.Update();}
        protected void Press(Key key,Action handle){Release();InputSystem.QueueStateEvent(_keyboard,new KeyboardState(key));InputSystem.Update();Assert.IsTrue(_keyboard[key].wasPressedThisFrame,"isolated fixture must deliver a new key press");handle();}
        protected static object Get(object obj,string field){var f=obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);Assert.NotNull(f,field);return f.GetValue(obj);}
        protected static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).SetValue(obj,value);
        protected static object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args);
        protected static void State(InputHandler input,string field,string value){var f=typeof(InputHandler).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic);f.SetValue(input,Enum.Parse(f.FieldType,value));}
        protected List<Entity> Drops(int count=7)
        {var items=new List<Entity>();for(int i=0;i<count;i++){var item=Give(Player,i==7?"OakHaftComponent":GroundBlueprints[i%GroundBlueprints.Length]);Assert.IsTrue(InventorySystem.Drop(Player,item,Zone));items.Add(item);}return items;}
        protected PickupUI Pickup(List<Entity> items,Entity chest=null)
        {var ui=Component<PickupUI>();ui.Tilemap=Tiles();ui.PopupCamera=OutsideCamera();ui.PlayerEntity=Player;ui.CurrentZone=Zone;ui.Open(items,chest);return ui;}
        protected ContainerPickerUI Containers(List<Entity> items)
        {var ui=Component<ContainerPickerUI>();ui.Tilemap=Tiles();ui.PopupCamera=OutsideCamera();ui.Open(items);return ui;}
        protected WorldActionMenuUI World(Entity target,List<InventoryAction> actions=null)
        {var ui=Component<WorldActionMenuUI>();ui.Tilemap=Tiles();ui.PopupCamera=OutsideCamera();ui.Open(Player,target,Zone.GetCell(11,10),actions??WorldInteractionSystem.GatherActions(target,Player),Zone);return ui;}
        protected static char RowKey(object ui,Tilemap tiles,int row,int contentY=3)
        {int x=(int)Get(ui,"_worldOriginX")+2,y=(int)Get(ui,"_worldTopY")-contentY-row;var tile=tiles.GetTile(new Vector3Int(x,y,0));for(char c='a';c<='z';c++)if(tile==CP437TilesetGenerator.GetUiTile(c))return c;return '\0';}
        protected static Key Letter(char c)=>(Key)((int)Key.A+c-'a');
        protected static int Units(InventoryPart inv)=>inv.Objects.Where(x=>x.HasPart<BrewItemPart>()||x.BlueprintName=="BrewedTonic").Sum(x=>x.GetPart<StackerPart>()?.StackCount??1);
        protected static void Execute(Entity actor,Entity target,Zone zone,InventoryAction action)
        {var e=GameEvent.New("InventoryAction");e.SetParameter("Actor",(object)actor);e.SetParameter("Zone",(object)zone);e.SetParameter("Command",action.Command);try{target.FireEvent(e);}finally{e.Release();}}
        protected DialogueUI Dialogue(int authored=9,bool allowEscape=true)
        {
            var conv=new ConversationData{ID="ShortcutDialogue"};conv.Nodes.Add(new NodeData{ID="Start",Text="A staged conversation with enough visible choices to exercise reserved-key boundaries.",AllowEscape=allowEscape,Choices=Enumerable.Range(0,authored).Select(i=>new ChoiceData{Text="Choice "+i,Target="End"}).ToList()});
            ConversationLoader.Register(conv);var speaker=new Entity{BlueprintName="ShortcutSpeaker"};speaker.AddPart(new ConversationPart{ConversationID=conv.ID});speaker.AddPart(new RenderPart{DisplayName="Speaker",RenderString="@"});Assert.IsTrue(ConversationManager.StartConversation(speaker,Player));
            var ui=Component<DialogueUI>();ui.Tilemap=Tiles();ui.PopupCamera=OutsideCamera();ui.PlayerEntity=Player;ui.CurrentZone=Zone;ui.Open();return ui;
        }
    }
    public class GameAuditShortcutTests:ShortcutFixture
    {
        [TestCase(5,'f')] [TestCase(6,'h')] [TestCase(9,'m')] [TestCase(10,'n')]
        public void DisplayedPickupLetterTakesExactAbsoluteRow(int index,char key)
        {var items=Drops(index+1);var ui=Pickup(items);Assert.AreEqual(key,RowKey(ui,ui.Tilemap,index));Press(Letter(key),ui.HandleInput);Assert.IsTrue(Inventory.Contains(items[index]));foreach(var other in items.Where(x=>x!=items[index]))Assert.IsFalse(Inventory.Contains(other));}
        [TestCase(Key.G)] [TestCase(Key.Escape)] public void PickupCloseKeysNeverTakeAnItem(Key key)
        {var items=Drops();var ui=Pickup(items);Press(key,ui.HandleInput);Assert.IsFalse(ui.IsOpen);Assert.IsFalse(ui.PickedUpAny);Assert.IsEmpty(Inventory.Objects);foreach(var item in items)Assert.IsTrue(Zone.GetCell(10,10).Objects.Contains(item));}
        [Test] public void PickupNavigationKeepsItsReservedKeys()
        {var ui=Pickup(Drops(11));Press(Key.J,ui.HandleInput);Assert.AreEqual(1,Get(ui,"_cursorIndex"));Press(Key.K,ui.HandleInput);Assert.AreEqual(0,Get(ui,"_cursorIndex"));Assert.IsEmpty(Inventory.Objects);}
        [Test] public void SeventhRealChestEntryUsesUsableShortcut()
        {var chest=Item("Chest");Assert.IsTrue(Zone.AddEntity(chest,10,10));var items=GroundBlueprints.Select(Item).ToList();foreach(var item in items)Assert.IsTrue(chest.GetPart<ContainerPart>().AddItem(item));var ui=Pickup(items,chest);Assert.AreEqual('h',RowKey(ui,ui.Tilemap,6));Press(Key.H,ui.HandleInput);Assert.IsTrue(Inventory.Contains(items[6]));Assert.AreEqual(6,chest.GetPart<ContainerPart>().Contents.Count);}
        [TestCase(false)] [TestCase(true)] public void CrowdedContainerPickerSelectsShownSeventhOrCloses(bool close)
        {var items=Enumerable.Range(0,7).Select(_=>Item("Chest")).ToList();var ui=Containers(items);Assert.AreEqual('h',RowKey(ui,ui.Tilemap,6));Press(close?Key.G:Key.H,ui.HandleInput);Assert.IsFalse(ui.IsOpen);Assert.AreEqual(!close,ui.SelectionMade);Assert.AreSame(close?null:items[6],ui.SelectedContainer);}
        [TestCase(false)] [TestCase(true)] public void ActualBreakRowHasUsableShortcutOnlyWhenDestructible(bool indestructible)
        {var target=Item("Chest");target.GetPart<DestructiblePart>().Indestructible=indestructible;Assert.IsTrue(Zone.AddEntity(target,11,10));var actions=WorldInteractionSystem.GatherActions(target,Player);var action=actions.SingleOrDefault(x=>x.Command=="Break");if(indestructible){Assert.IsNull(action);return;}Assert.AreEqual('k',action.Key);var ui=World(target,actions);int index=actions.IndexOf(action);char key=RowKey(ui,ui.Tilemap,index,(int)typeof(WorldActionMenuUI).GetProperty("ContentY",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui));Assert.IsFalse(key=='k'||key=='j'||key=='\0');Press(Letter(key),ui.HandleInput);Assert.AreSame(action,ui.SelectedAction);Assert.AreEqual('k',action.Key);}
        [Test] public void ActualStillSingleThenBatchHaveDistinctShownKeysAndPayments()
        {
            var still=Item("AlchemyStill");Assert.IsTrue(Zone.AddEntity(still,11,10));var reagent=Give(Player,"GlimmerBrine");reagent.GetPart<StackerPart>().StackCount=3;CraftingMarkPart.Toggle(reagent);
            var actions=WorldInteractionSystem.GatherActions(still,Player);var single=actions.Single(x=>x.Command=="BrewMix");var batch=actions.Single(x=>x.Command=="BrewMixBatch");var ui=World(still,actions);int y=(int)typeof(WorldActionMenuUI).GetProperty("ContentY",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);
            char sk=RowKey(ui,ui.Tilemap,actions.IndexOf(single),y),bk=RowKey(ui,ui.Tilemap,actions.IndexOf(batch),y);Assert.AreEqual('b',sk);Assert.AreNotEqual(sk,bk);Assert.AreNotEqual('\0',bk);
            Press(Letter(sk),ui.HandleInput);Assert.AreSame(single,ui.SelectedAction);Execute(Player,still,Zone,ui.SelectedAction);Assert.AreEqual(2,reagent.GetPart<StackerPart>().StackCount);Assert.AreEqual(1,Units(Inventory));
            ui.Open(Player,still,Zone.GetCell(11,10),WorldInteractionSystem.GatherActions(still,Player),Zone);Press(Letter(bk),ui.HandleInput);Assert.AreEqual("BrewMixBatch",ui.SelectedAction.Command);Execute(Player,still,Zone,ui.SelectedAction);Assert.IsFalse(Inventory.Contains(reagent));Assert.AreEqual(3,Units(Inventory));Assert.AreEqual('B',batch.Key);
        }
        [TestCase(false)] [TestCase(true)] public void WorldReleaseGateUsesActualActivationRatherThanRawAuthoredKey(bool enter)
        {var target=Item("Chest");Assert.IsTrue(Zone.AddEntity(target,11,10));var action=new InventoryAction("Examine","examine","Examine",'k',1);var ui=World(target,new List<InventoryAction>{action});var input=Component<InputHandler>();input.PlayerEntity=Player;input.CurrentZone=Zone;input.WorldActionMenuUI=ui;State(input,"_inputState","WorldActionMenuOpen");State(input,"_worldActionMenuReturnState","Normal");Press(enter?Key.Enter:Key.A,()=>Call(input,"HandleWorldActionMenuInput"));Assert.IsFalse(ui.IsOpen);Assert.AreEqual("Normal",Get(input,"_inputState").ToString());Assert.AreEqual(enter?KeyCode.None:KeyCode.A,Get(input,"_worldActionKeyToRelease"));Assert.AreEqual('k',action.Key);}
        [TestCase(false)] [TestCase(true)] public void TenthDialogueShortcutRevealsBeforeSelecting(bool revealing)
        {var ui=Dialogue();var speaker=ConversationManager.Speaker;Assert.AreEqual(10,ConversationManager.VisibleChoices.Count);if(!revealing)Press(Key.Space,ui.HandleInput);Press(Key.L,ui.HandleInput);if(revealing){Assert.IsTrue(ui.IsOpen);Assert.IsNull(ConversationManager.PendingAttackTarget);Assert.IsFalse((bool)Get(ui,"_revealing"));Press(Key.L,ui.HandleInput);}Assert.IsFalse(ui.IsOpen);Assert.AreSame(speaker,ConversationManager.PendingAttackTarget);}
        [TestCase(false)] [TestCase(true)] public void DialogueReservedNavigationDoesNotRevealOrSelect(bool revealing)
        {var ui=Dialogue();if(!revealing)Press(Key.Space,ui.HandleInput);Press(Key.J,ui.HandleInput);Assert.AreEqual(1,Get(ui,"_cursorIndex"));Assert.AreEqual(revealing,Get(ui,"_revealing"));Press(Key.K,ui.HandleInput);Assert.AreEqual(0,Get(ui,"_cursorIndex"));Assert.IsTrue(ui.IsOpen);Assert.IsNull(ConversationManager.PendingAttackTarget);}
        [TestCase(false)] [TestCase(true)] public void DialogueEscapeStillHonorsNodeRule(bool allow)
        {var ui=Dialogue(9,allow);Press(Key.Escape,ui.HandleInput);Assert.AreEqual(!allow,ui.IsOpen);Assert.IsNull(ConversationManager.PendingAttackTarget);}

        [TestCase("pickup",false)] [TestCase("pickup",true)]
        [TestCase("container",false)] [TestCase("container",true)]
        [TestCase("dialogue",false)] [TestCase("dialogue",true)]
        public void ClosingSelectionDoesNotTurnOneHeldLetterIntoMovement(string kind,bool enter)
        {
            var input=Component<InputHandler>(); input.PlayerEntity=Player;input.CurrentZone=Zone;input.MoveRepeatDelay=0;Set(input,"_lastMoveTime",-999f);
            var turns=new TurnManager();turns.AddEntity(Player);turns.ProcessUntilPlayerTurn();input.TurnManager=turns;
            string handler;
            if(kind=="pickup") { input.PickupUI=Pickup(Drops(1));State(input,"_inputState","PickupOpen");handler="HandlePickupInput"; }
            else if(kind=="container") { var sack=Item("Sack");Assert.IsTrue(Zone.AddEntity(sack,10,10));Assert.IsTrue(sack.GetPart<ContainerPart>().AddItem(Item("SilverSand")));input.ContainerPickerUI=Containers(new List<Entity>{sack});State(input,"_inputState","ContainerPickerOpen");handler="HandleContainerPickerInput"; }
            else {input.DialogueUI=Dialogue(1);Press(Key.Space,input.DialogueUI.HandleInput);State(input,"_inputState","DialogueOpen");handler="HandleDialogueInput";}
            Press(enter?Key.Enter:Key.A,()=>Call(input,handler));Assert.AreEqual("Normal",Get(input,"_inputState").ToString());
            for(int i=0;i<3;i++)Call(input,"Update");Assert.AreEqual((10,10),Zone.GetEntityPosition(Player),"held menu selection must not walk");
            Assert.AreEqual(enter?KeyCode.None:KeyCode.A,Get(input,"_worldActionKeyToRelease"));
            Release();Call(input,"Update");Assert.AreEqual((10,10),Zone.GetEntityPosition(Player));Assert.AreEqual(KeyCode.None,Get(input,"_worldActionKeyToRelease"));
            Press(Key.A,()=>Call(input,"Update"));Assert.AreEqual((9,10),Zone.GetEntityPosition(Player),"fresh press still moves");
        }
    }
}
