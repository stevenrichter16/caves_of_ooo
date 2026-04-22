# Caves of Qud Message Log Side Menu Analysis

This document summarizes how the Caves of Qud side menu that shows game logs works in the decompiled project at `qud-decompiled-project/`.

The main finding is that there are two different implementations depending on UI mode:

1. Classic text mode uses `XRL.UI.Sidebar` to draw the log directly into the 80x25 `ScreenBuffer`.
2. Modern UI uses `Qud.UI.MessageLogWindow` to show a live Unity scroll view, while the full history is exposed separately through `Qud.UI.MessageLogStatusScreen`.

## Key Files

| File | Role |
| --- | --- |
| `qud-decompiled-project/XRL.Messages/MessageQueue.cs` | Owns the player message list, adds messages, formats recent lines, trims history, and exposes the full log screen. |
| `qud-decompiled-project/XRL.UI/Sidebar.cs` | Classic text sidebar state, placement, caching, and rendering. |
| `qud-decompiled-project/XRL.UI/Text.cs` | Wraps and draws sidebar message text bottom-to-top inside a rectangular region. |
| `qud-decompiled-project/XRL.Core/XRLCore.cs` | Calls `Sidebar.UpdateState()`, `Sidebar.Update()`, and `Sidebar.Render()` during turn and render flow; also handles sidebar-related commands and message-log callbacks. |
| `qud-decompiled-project/Qud.UI/MessageLogWindow.cs` | Modern Unity live message-log window shown alongside the stage. |
| `qud-decompiled-project/MessageLogPooledScrollRect.cs` | Thin specialization of the pooled scroll view used by the modern message log. |
| `qud-decompiled-project/PooledScrollRect.cs` | Core Unity scrolling behavior: append items, reflow, scroll locking, hover behavior, and auto-scroll-to-bottom. |
| `qud-decompiled-project/MessageLogElement.cs` | Individual Unity line renderer for the live message log. |
| `qud-decompiled-project/Qud.UI/RTF.cs` | Converts Qud text markup into Unity rich text for the modern log. |
| `qud-decompiled-project/Qud.UI/MovableSceneFrameWindowBase.cs` | Shared window behavior for movable/dockable stage-side panels, including side switching and saved placement. |
| `qud-decompiled-project/Assets.Game.UI.Windows.Stage/StageDock.cs` | Lays out the docked right or left stage sidebar, including the message log pane. |
| `qud-decompiled-project/GameManager.cs` | Chooses preferred sidebar side in modern UI and aligns the stage camera/safe area around the dock. |
| `qud-decompiled-project/Qud.UI/MessageLogStatusScreen.cs` | Full history message-log status screen used outside the live side panel. |

## High-Level Architecture

At a high level, the pipeline is:

1. Gameplay systems call `MessageQueue.AddPlayerMessage(...)`.
2. The message is normalized and appended to the player’s `MessageQueue`.
3. The message queue invalidates its cached recent-lines block and broadcasts a callback through `XRLCore`.
4. The UI renders the latest log in one of two ways:
   - Classic UI: `Sidebar.Render()` pulls recent lines from `MessageQueue.GetLines(0, 12)` and writes them into the sidebar region of the text console.
   - Modern UI: `MessageLogWindow` receives the callback, converts the message to rich text, and appends it to a Unity scroll view.

The important distinction is:

- Classic mode is pull-based during frame render.
- Modern mode is push-based through message callbacks.

## Message Creation and Storage

The underlying message store is `MessageQueue`.

- `MessageQueue.AddPlayerMessage(...)` colorizes and capitalizes the text, then forwards it to the player’s queue at `XRLCore.Core.Game.Player.Messages.Add(Message)` in `XRL.Messages/MessageQueue.cs:106-125`.
- `MessageQueue.Add(string Message)` invalidates the cached recent-lines block, runs `Markup.Transform`, appends the final string to `Messages`, and calls `XRLCore.CallNewMessageLogEntryCallbacks(Message)` in `XRL.Messages/MessageQueue.cs:138-149`.
- `MessageQueue.BeginPlayerTurn()` advances `LastMessage` and `PreviousMessage` markers and trims old history once the list exceeds 2000 entries in `XRL.Messages/MessageQueue.cs:94-104`.

Those turn markers are used to visually distinguish newer messages from older ones when the classic sidebar asks for recent lines.

## Classic Sidebar Path

### Main Files

- `XRL.UI/Sidebar.cs`
- `XRL.UI/Text.cs`
- `XRL.Core/XRLCore.cs`

### How It Works

`XRLCore` drives the sidebar every turn and every frame:

- `PlayerTurn()` increments `Sidebar.SidebarTick`, calls `Game.Player.Messages.BeginPlayerTurn()`, then runs `Sidebar.UpdateState()` and `Sidebar.Update()` in `XRL.Core/XRLCore.cs:662-705`.
- During rendering, `RenderBaseToBuffer(...)` calls `Sidebar.UpdateState()` and then `Sidebar.Render(Buffer)` in `XRL.Core/XRLCore.cs:2354-2448`.
- `RenderBase(...)` also refreshes sidebar data with `Sidebar.Update()` before the base draw in `XRL.Core/XRLCore.cs:2517-2530`.

### Sidebar Placement

The classic sidebar is not permanently fixed to the right edge. `Sidebar.UpdateState()` swaps sides based on the player’s map position:

- If the player is far enough right and the sidebar is on the right, it moves left.
- If the player is far enough left and the sidebar is on the left, it moves right.

That logic is in `XRL.UI/Sidebar.cs:474-500`, with thresholds at X `> 42` and `< 38`.

It also flips the popup stack between top and bottom using Y thresholds in the same method.

### Sidebar Modes

The classic sidebar has four display modes controlled by `Sidebar.SidebarState`:

- `0`: full stats panel plus log area.
- `1`: compact HP/weight header plus larger log area.
- `2`: abilities plus log area.
- `3`: nearby hostiles plus log area.

The cycling command is handled in `XRL.Core/XRLCore.cs:2171-2176`.

The default visibility toggle is `CmdShowSidebar`, handled in `XRL.Core/XRLCore.cs:2168-2170`.

### How the Log Region Is Drawn

The actual message log area is drawn by the classic sidebar through `MessageQueue.GetLines(0, 12)` and `Text.DrawBottomToTop(...)`.

Relevant code paths:

- `Sidebar.Render(...)` begins in `XRL.UI/Sidebar.cs:719`.
- The default mode `SidebarState == 0` draws the message block in `XRL.UI/Sidebar.cs:1081-1108`.
- State `2` and `3` also draw the log in `XRL.UI/Sidebar.cs:968-991` and `XRL.UI/Sidebar.cs:853-940`.
- `Text.DrawBottomToTop(...)` wraps the text to the available width, then writes lines from the bottom upward in `XRL.UI/Text.cs:21-35`.

The sidebar uses a `MessageCache` list so it does not rewrap the recent log block every frame when `MessageQueue.Cache_0_12Valid` is still true. That cache is invalidated whenever:

- a new message is added in `MessageQueue.Add(...)`, or
- the sidebar mode changes via `Sidebar.SidebarState`, which explicitly clears the queue cache in `XRL.UI/Sidebar.cs:300-310`.

### How Recent Messages Are Formatted

`MessageQueue.GetLines(...)` and `GetLinesList(...)` add classic Qud text markup before the sidebar draws anything:

- Newer messages get one color prefix.
- Older messages get another.
- Normal lines are prefixed with `>`.
- Lines starting with `#` are treated specially and drawn without the usual `>` prefix.

That behavior is in `XRL.Messages/MessageQueue.cs:152-220`.

So the classic side log is not just a raw dump of strings. It is a small formatted window over the player’s message history with recency coloring and text wrapping.

### Hidden Sidebar Mode

If the sidebar is hidden, `Sidebar.Render(...)` does not draw the full side panel. It only draws a tiny toggle marker, the player HP line, optional target wound level, and the current-cell popup in `XRL.UI/Sidebar.cs:781-816`.

This is why the classic right-side log can collapse almost entirely while still leaving a minimal HUD stub.

## Modern UI Path

### Main Files

- `Qud.UI/MessageLogWindow.cs`
- `MessageLogPooledScrollRect.cs`
- `PooledScrollRect.cs`
- `MessageLogElement.cs`
- `Qud.UI/RTF.cs`

### Core Behavior

When `Options.ModernUI` is enabled, `Sidebar.Render(...)` stops drawing the classic CP437 sidebar and instead only updates a few shared values like HP and XP in `XRL.UI/Sidebar.cs:725-765`.

The live right-hand log panel is then handled by `Qud.UI.MessageLogWindow`.

Key behaviors in `Qud.UI/MessageLogWindow.cs`:

- The view is declared as a Unity window with `UIView("MessageLog", ...)` in `Qud.UI/MessageLogWindow.cs:9-12`.
- `Init()` registers `AddMessage` with `XRLCore.RegisterNewMessageLogEntryCallback(...)` in `Qud.UI/MessageLogWindow.cs:94-98`.
- `AddMessage()` queues UI-thread work, prepends `":: "`, then calls `_AddMessage(...)` in `Qud.UI/MessageLogWindow.cs:100-106`.
- `_AddMessage(...)` converts markup to rich text and appends the line to the pooled scroll rect in `Qud.UI/MessageLogWindow.cs:109-112`.

The callback registration itself is in `XRL.Core/XRLCore.cs:629-645`.

### Rich Text Conversion

Modern Unity UI does not directly consume Qud’s `&` and `^` console formatting codes, so the message must be converted:

- `Qud.UI.RTF.FormatToRTF(...)` delegates to `Sidebar.FormatToRTF(...)` in `Qud.UI/RTF.cs:7-34`.
- `Sidebar.FormatToRTF(...)` parses Qud inline color codes, strips background-style codes, maps CP437 glyphs to displayable Unicode, and emits Unity `<color=...>` rich text in `XRL.UI/Sidebar.cs:632-717`.

That means classic and modern paths share the same markup grammar, but the final renderer is different.

### Scroll View Mechanics

The live modern log is a pooled scroll list:

- `MessageLogPooledScrollRect` is a thin specialization over `PooledScrollRect<string, MessageLogElement>`.
- `PooledScrollRect.Add(...)` creates or reuses a visual element, binds the new line, and then scrolls to the bottom in `PooledScrollRect.cs:98-142`.
- `PooledScrollRect.Update()` keeps the view pinned to the bottom unless the user has manually unlocked scrolling in `PooledScrollRect.cs:144-176`.
- Hover state is tracked by `OnPointerEnter` and `OnPointerExit` in `PooledScrollRect.cs:338-346`.

Each individual entry is rendered by `MessageLogElement`:

- `Setup(...)` writes the final string into a `TextMeshProUGUI` component in `MessageLogElement.cs:11-17`.
- `Update()` applies the configurable message-log font size adjustment in `MessageLogElement.cs:25-31`.

### Important Modern-UI Detail

The live side window is incremental, not rebuilt from `The.Game.Player.Messages.Messages` every frame. It depends on `AddMessage(...)` callbacks to append new entries.

The full historical log is handled separately by `MessageLogStatusScreen`, not by the live side window.

## Full Log Screen

The full message history can be opened through `MessageQueue.Show()`:

- In modern UI it opens status screen index `7` via `StatusScreensScreen.show(7, ...)` in `XRL.Messages/MessageQueue.cs:78-87`.
- In classic UI it opens a book-style full text dump via `BookUI.ShowBook(...)` in the same method.

The modern full-screen implementation is `Qud.UI.MessageLogStatusScreen`.

Important details:

- It rebuilds its contents from `The.Game.Player.Messages.Messages` every time `UpdateViewFromData()` runs in `Qud.UI/MessageLogStatusScreen.cs:146-162`.
- Each raw message is split on newline before being converted into list rows in `Qud.UI/MessageLogStatusScreen.cs:154-157`.
- It supports fuzzy text search through `FuzzySharp` in `Qud.UI/MessageLogStatusScreen.cs:159-160`.
- It places selection at the newest entry when shown in `Qud.UI/MessageLogStatusScreen.cs:164-178`.

One technical quirk: `MessageLogStatusScreen` contains category metadata that looks copied from the journal UI, but `UpdateViewFromData()` does not actually filter by category. The real data source is just the flat message list.

## How the Side Menu Is Handled in the UI

### Classic UI

In classic mode the sidebar is just part of the console frame:

- It is rendered into the same `ScreenBuffer` as the main map.
- The vertical divider is drawn directly with CP437 box characters in `XRL.UI/Sidebar.cs:823-848`.
- The message log occupies a rectangular region within the 80x25 text console.
- The main map and sidebar coexist in the same terminal-style render target.

So in classic Qud the “right-hand log menu” is not a separate Unity widget. It is a text-console sub-rectangle.

### Modern UI

In modern UI the message log becomes a real Unity window.

The important pieces are:

- `MessageLogWindow` inherits from `MovableSceneFrameWindowBase<T>`, which gives it:
  - saved position and size via `PlayerPrefs`,
  - left/right anchor swapping,
  - docking awareness,
  - optional masking,
  - shift-to-hide behavior,
  - passthrough input.

That shared behavior is implemented in `Qud.UI/MovableSceneFrameWindowBase.cs:52-258`.

When docked, `StageDock` physically reserves space for the side pane and shrinks the playable stage safe area:

- It sizes and positions `messagelogPane` beneath optional minimap and nearby-items panes in `Assets.Game.UI.Windows.Stage/StageDock.cs:103-151`.
- It anchors the whole dock to the left or right in `Assets.Game.UI.Windows.Stage/StageDock.cs:152-170`.
- If the dock is hidden, it restores the full stage safe area in `Assets.Game.UI.Windows.Stage/StageDock.cs:184-201`.

`GameManager` coordinates side preference and camera framing:

- `UpdatePreferredSidebarPosition()` swaps preferred side based on player screen position in `GameManager.cs:682-727`.
- Screen size changes also trigger sidebar/overlay refresh in `GameManager.cs:3002-3007`.
- The camera letterbox alignment is set left, right, or centered based on dock state and `MessageLogWindow.Shown` in `GameManager.cs:3299-3311`.

This is the modern equivalent of the classic “move the sidebar away from the player” behavior.

## Control and Visibility

The main visibility control is shared conceptually across modes:

- `ControlManager` maps `"Toggle Message Log"` to `CmdShowSidebar` in `ControlManager.cs:230`.
- In classic mode, `CmdShowSidebar` toggles `Sidebar.Hidden` in `XRL.Core/XRLCore.cs:2168-2170`.
- In modern UI, `MessageLogWindow.Update()` also listens for `CmdShowSidebar` and toggles `MessageLogWindow.Shown` in `Qud.UI/MessageLogWindow.cs:49-60`.

There is also a classic-only sidebar mode cycling command:

- `CmdShowSidebarMessages` increments `Sidebar.SidebarState` and wraps after `3` in `XRL.Core/XRLCore.cs:2171-2176`.

## Practical Summary

If your goal is to understand the right-hand message log specifically, the shortest correct description is:

- The actual log data lives in `MessageQueue`.
- Classic Qud renders the recent log directly inside `Sidebar.Render()` using the text console buffer.
- Modern Qud does not use the old text sidebar for the live log. Instead, it pushes new messages into `MessageLogWindow`, a Unity scroll view that lives in the stage dock or as a movable side window.
- The full backlog is exposed through `MessageLogStatusScreen`, which rebuilds its list from `The.Game.Player.Messages.Messages`.

## Most Important Files to Read First

If you only want the essential implementation files, start here in this order:

1. `qud-decompiled-project/XRL.Messages/MessageQueue.cs`
2. `qud-decompiled-project/XRL.UI/Sidebar.cs`
3. `qud-decompiled-project/XRL.Core/XRLCore.cs`
4. `qud-decompiled-project/Qud.UI/MessageLogWindow.cs`
5. `qud-decompiled-project/Assets.Game.UI.Windows.Stage/StageDock.cs`
6. `qud-decompiled-project/Qud.UI/MovableSceneFrameWindowBase.cs`
7. `qud-decompiled-project/Qud.UI/MessageLogStatusScreen.cs`
