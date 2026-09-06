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

namespace CavesOfOoo.Tests
{
    public class GameAuditShortcutAdversarialTests : ShortcutFixture
    {
        static InventoryAction Action(char key,string command="Examine")=>new InventoryAction("row","row",command,key);
        WorldActionMenuUI Menu(params InventoryAction[] actions)=>World(Item("Chest"),actions.ToList());
        static int Content(WorldActionMenuUI ui)=>(int)typeof(WorldActionMenuUI).GetProperty("ContentY",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);
        static char Label(WorldActionMenuUI ui,int index)=>RowKey(ui,ui.Tilemap,index-(int)Get(ui,"_scrollOffset"),Content(ui));
        [TestCase('!')] [TestCase('1')] [TestCase('\0')] [TestCase(' ')]
        public void UnsupportedOrMissingAuthoredKeyGetsUsableFallback(char raw)
        {var action=Action(raw);var ui=Menu(action);Assert.AreEqual('a',Label(ui,0));Press(Key.A,ui.HandleInput);Assert.AreSame(action,ui.SelectedAction);Assert.AreEqual(raw,action.Key);}
        [TestCase('j')] [TestCase('k')] public void ReservedAuthoredKeysGetFallbackAndKeepNavigation(char raw)
        {var first=Action(raw);var second=Action('q');var ui=Menu(first,second);Assert.AreEqual('a',Label(ui,0));Press(Key.J,ui.HandleInput);Assert.AreEqual(1,Get(ui,"_cursorIndex"));Assert.IsFalse(ui.SelectionMade);Press(Key.K,ui.HandleInput);Assert.AreEqual(0,Get(ui,"_cursorIndex"));Press(Key.A,ui.HandleInput);Assert.AreSame(first,ui.SelectedAction);}
        [TestCase('B','b')] [TestCase('G','g')] [TestCase('S','s')] [TestCase('W','w')]
        public void ValidUppercasePreferenceDisplaysAndDispatchesNormalizedKey(char raw,char displayed)
        {var action=Action(raw);var ui=Menu(action);Assert.AreEqual(displayed,Label(ui,0));Press(Letter(displayed),ui.HandleInput);Assert.AreSame(action,ui.SelectedAction);Assert.AreEqual(raw,action.Key);Assert.AreEqual((KeyCode)((int)KeyCode.A+displayed-'a'),ui.SelectedActivationKey);}
        [TestCase(false)] [TestCase(true)] public void FallbackNeverStealsLaterAuthoredPreference(bool heading)
        {var first=Action('\0',heading?"CraftNoop":"Examine");var authored=Action('a');var ui=Menu(first,authored);Assert.AreEqual(heading?'\0':'b',Label(ui,0));Assert.AreEqual('a',Label(ui,1));Press(Key.A,ui.HandleInput);Assert.AreSame(authored,ui.SelectedAction);}
        [TestCase('q','Q')] [TestCase('Q','q')] public void DuplicateCasePreferencesPreserveFirstAndAllocateSecond(char a,char b)
        {var first=Action(a);var second=Action(b);var ui=Menu(first,second);Assert.AreEqual('q',Label(ui,0));Assert.AreEqual('a',Label(ui,1));Press(Key.A,ui.HandleInput);Assert.AreSame(second,ui.SelectedAction);Assert.AreEqual(a,first.Key);Assert.AreEqual(b,second.Key);}
        [TestCase(false)] [TestCase(true)] public void HeadingNeverReservesOrConsumesASelectionLetter(bool heading)
        {var first=Action('a',heading?"CraftNoop":"Examine");var second=Action('\0');var ui=Menu(first,second);Assert.AreEqual(heading?'\0':'a',Label(ui,0));Assert.AreEqual(heading?'a':'b',Label(ui,1));Press(heading?Key.A:Key.B,ui.HandleInput);Assert.AreSame(second,ui.SelectedAction);}
        [TestCase(false)] [TestCase(true)] public void ScrolledWorldBindingIsAbsoluteAndOverflowHasNoInventedKey(bool overflow)
        {var actions=Enumerable.Range(0,28).Select(_=>Action('\0')).ToArray();var ui=Menu(actions);int index=overflow?24:23;for(int i=0;i<index;i++)Press(Key.J,ui.HandleInput);Assert.AreEqual(index,Get(ui,"_cursorIndex"));Assert.AreEqual(overflow?'\0':'z',Label(ui,index));Press(overflow?Key.Enter:Key.Z,ui.HandleInput);Assert.AreSame(actions[index],ui.SelectedAction);Assert.AreEqual(overflow?KeyCode.None:KeyCode.Z,ui.SelectedActivationKey);}
        [TestCase(false)] [TestCase(true)] public void SelectionProvenanceClearsOnConsumeAndReopen(bool consume)
        {var action=Action('s');var ui=Menu(action);Press(Key.S,ui.HandleInput);Assert.AreEqual(KeyCode.S,ui.SelectedActivationKey);if(consume)ui.ConsumeSelection();else ui.Open(Player,Item("Chest"),Zone.GetCell(11,10),new List<InventoryAction>{action},Zone);Assert.AreEqual(KeyCode.None,ui.SelectedActivationKey);Assert.IsFalse(ui.SelectionMade);Assert.IsNull(ui.SelectedAction);}
        [TestCase(Key.Enter)] [TestCase(Key.Escape)] public void ReopenedConfirmOrCancelCannotReusePriorLetter(Key key)
        {var action=Action('s');var ui=Menu(action);Press(Key.S,ui.HandleInput);ui.Open(Player,Item("Chest"),Zone.GetCell(11,10),new List<InventoryAction>{action},Zone);Press(key,ui.HandleInput);Assert.AreEqual(KeyCode.None,ui.SelectedActivationKey);Assert.AreEqual(key==Key.Enter,ui.SelectionMade);Assert.AreEqual(key==Key.Escape,ui.SelectionCancelled);}
        [TestCase(false)] [TestCase(true)] public void PickupRemovalUpdatesAbsoluteBindingAndDoesNotReuseOldReference(bool remove)
        {var items=Drops(8);var ui=Pickup(items);if(remove)Press(Key.A,ui.HandleInput);Assert.AreEqual('h',RowKey(ui,ui.Tilemap,6));Press(Key.H,ui.HandleInput);Assert.IsTrue(Inventory.Contains(items[remove?7:6]));Assert.IsFalse(Inventory.Contains(items[remove?6:7]));}
        [TestCase(false)] [TestCase(true)] public void PickupScrolledLastBindingAndOverflowRemainAccessible(bool overflow)
        {var items=Drops(25);var ui=Pickup(items);int index=overflow?23:22;for(int i=0;i<index;i++)Press(Key.J,ui.HandleInput);int offset=(int)Get(ui,"_scrollOffset");Assert.AreEqual(overflow?'\0':'z',RowKey(ui,ui.Tilemap,index-offset));Press(overflow?Key.Enter:Key.Z,ui.HandleInput);Assert.IsTrue(Inventory.Contains(items[index]));}
        [Test] public void ContainerOverflowUsesEnterWithoutFabricatedLabel()
        {var items=Enumerable.Range(0,25).Select(_=>Item("Sack")).ToList();var ui=Containers(items);for(int i=0;i<23;i++)Press(Key.J,ui.HandleInput);Assert.AreEqual('\0',RowKey(ui,ui.Tilemap,23-(int)Get(ui,"_scrollOffset")));Press(Key.Enter,ui.HandleInput);Assert.AreSame(items[23],ui.SelectedContainer);Assert.AreEqual(KeyCode.None,ui.SelectedActivationKey);}
        [Test] public void DialogueDrawsTheSameTenthKeyThatRevealAndDispatchUse()
        {var ui=Dialogue();bool found=false;foreach(var pos in ui.Tilemap.cellBounds.allPositionsWithin)if(ui.Tilemap.GetTile(pos)==CP437TilesetGenerator.GetUiTile('l')&&ui.Tilemap.GetTile(pos+Vector3Int.right)==CP437TilesetGenerator.GetUiTile(')'))found=true;Assert.IsTrue(found,"actual tenth label l)");Press(Key.L,ui.HandleInput);Assert.IsTrue(ui.IsOpen);Assert.AreEqual(KeyCode.None,ui.ClosingActivationKey);Press(Key.L,ui.HandleInput);Assert.IsFalse(ui.IsOpen);Assert.AreEqual(KeyCode.L,ui.ClosingActivationKey);}
        [TestCase(-1)] [TestCase(24)] public void PositionalOutOfRangeHasNoKey(int index)
        {var type=typeof(PickupUI).Assembly.GetType("CavesOfOoo.Rendering.MenuShortcutMap");Assert.NotNull(type);var method=type.GetMethod("Positional",BindingFlags.Static|BindingFlags.NonPublic);Assert.AreEqual('\0',method.Invoke(null,new object[]{index,false}));}
        [Test] public void EmptyWorldMenuHasNoSelectionAndCanCancel()
        {var ui=Menu();Press(Key.A,ui.HandleInput);Assert.IsTrue(ui.IsOpen);Assert.IsFalse(ui.SelectionMade);Press(Key.Escape,ui.HandleInput);Assert.IsFalse(ui.IsOpen);Assert.IsTrue(ui.SelectionCancelled);Assert.AreEqual(KeyCode.None,ui.SelectedActivationKey);}
    }
}
