# Dialogue travel topics: UI and dependency audit

Status: source audit complete; native RED confirmed for all identified failures.
The bounded UI fix is implemented and awaiting the coordinator's GREEN run. No Unity tools were used by this audit.
Source line references below identify the pre-polish implementation.

## Relevant current contracts

- `Assets/Scripts/Presentation/Rendering/CenteredPopupLayout.cs:12-13` defines
  the same 80-column, 45-row overlay at 1280x800 and 1280x720. Camera aspect changes
  cell width, not the logical row budget. Its position helper clamps the size it
  centers; it does not clip or paginate a caller that draws more rows.
- `Assets/Scripts/Presentation/UI/DialogueUI.cs:23-31,306-310` caps width at 72
  columns, with a 10-column portrait gutter. At maximum width, speech has 58
  characters per line. This reader uses the existing CP437 UI font, not the
  larger field-ledger font.
- `Assets/Scripts/Presentation/Input/NativeGamepadInput.cs:42-45,243-249` already
  maps A/B to Return/Escape, D-pad/left stick to arrows, shoulders to Left/Right,
  and vertical right stick to PageUp/PageDown. No new controller mapping is needed.
- Dialogue first-confirm behavior is deliberate: Return/Space/click/choice hotkey
  completes the typewriter reveal before a subsequent press can select. Escape
  respects `AllowEscape`; Up/Down can position the cursor during reveal.

## Confirmed source-level hazards and minimal fix

### Choice window

`DialogueUI.cs:399-400,495-535` renders only choices 0 through 9. In contrast,
`:208-220` lets the cursor reach and confirm every visible-choice index. Thus an
11th choice can be focused and activated without its row being drawn. Mouse hit
mapping (`:700-712`) also assumes the window always starts at zero.

Use one cursor-following window of at most ten rows. Preserve absolute shortcut
identities, render the focused row, and translate mouse rows back to absolute
choice indices through the same window. Do not drop choices to satisfy the cap.
This is a general guard, not a claim that all authored 12-choice nodes display
12 choices simultaneously: many predicates are mutually exclusive.

### Speech overflow

`DialogueUI.cs:368-379` computes height as all wrapped speech rows plus at most ten
choices plus four structural rows. `:457-487` then draws every speech row; there
is no paging or height limit. A long current report can therefore push choices
and the footer below the camera's 45-row overlay. Centering alone does not solve it.

Bound speech pages to `45 - displayed choice rows - 4`, with Left/LB and Right/RB
(and PageUp/PageDown) navigation. Every page retains the choice list. Reset reveal
for a new page, so its first confirm reveals text and the next confirm selects.
Keep the existing dimensions and controls for ordinary nodes that already fit.
Reuse the cached foreground/background clearing when a last page or next node
shrinks. Page movement must not call `SelectChoice`, remember a report, accept a
quest or alter current dialogue text.

### Choice-label budget

With the default portrait and `[Talk]` tag, the actual maximum choice text is 48
characters, not 52 (`DialogueUI.cs:506-530`). The existing report text test's
52-character assertion is therefore not proof of unclipped labels. Current
short report labels fit; new topic labels should also fit without hiding their
meaningful distinction. Existing long authored responses still intentionally
truncate with `~`; wrapping every response is outside this small polish scope.

### Device hints and font scope

The old footer (`DialogueUI.cs:593`) lists only keyboard controls. Paged speech
needs explicit paired keyboard/shoulder hints. This pass does not replace the
font, enlarge all dialogue, or claim physical handheld readability. Native
1280x800/720 captures remain necessary, especially for long reports and footers.

## Topic and node-ID dependencies

Two providers currently attach unrelated topics to the same IDs:

- `RegionalGuidance.cs:91-99`: Scribe/Morrowfast Vennit `RegionOverview`, Innkeeper
  `Rumors`; these append up to four settlement note choices.
- `SpreadDiscoveryReports.cs:108-146`: Scribe `RegionOverview`, Innkeeper `Rumors`,
  resident seed keeper/cook `Nearby`; these append wilderness report text and
  remembered-report choices. `:175-205` limits each report list to three.
- `ConversationManager.cs:20-25` composes provider text; `:199-245` filters authored
  choices, appends both providers, then adds eligible trade and attack choices.
  Splitting only JSON labels or only appended choices would leave mismatched prose.

Preserve settlement semantics for existing settlement IDs where practical and
move wilderness generation and its tests to the new wilderness IDs. Update both
text and choice admission together, while keeping the same context/offer-token
validation and no-action-on-read guarantees. Keep resident `Nearby` semantics
unless their authored conversation also changes.

Tests and native scenario callers requiring deliberate review:

- `Assets/Tests/EditMode/Gameplay/Storylets/RegionalGuidanceTests.cs:104` and
  `RegionalGuidanceAdversarialTests.cs:43-46`: settlement topic routing.
- `Assets/Tests/EditMode/Gameplay/Conversations/SpreadDiscoveryReportsTests.cs`:
  fixture default nodes (line 27), positive topic cases (123-124), the old assertion
  that settlement choices coexist (140), and wrong-role/node counterchecks.
- `SpreadDiscoveryTextTests.cs:9-10`, `SpreadDiscoveryWayhouseTests.cs:13-14`, and
  `SpreadDiscoveryAdversarialTests.cs:33`: old wilderness node IDs and stale-node
  rejection. Change expected routing; preserve all ownership/revalidation tests.
- `SpreadResidentReportsTests.cs`, `ExpeditionReportTests.cs:227` and
  `SpreadEverydayResidentTests.cs`: resident `Nearby` behavior remains distinct.
- `Assets/Scripts/Scenarios/Custom/SpreadDiscoveryNativePlayer.cs:96`: currently
  finds the old mixed node for the real report walkthrough; needs the wilderness
  route. `ChunkGameplayNativeAudit.cs:165` requests regional directions and should
  continue selecting the settlement route. `SpreadDiscoveryNativePlayer.Reports.cs`
  selects resident `Nearby` and should not be blindly renamed.
- `FirstHourGuidance` and `LocalPeople` use separate node predicates and currently
  do not select `RegionOverview` or `Rumors`; avoid gratuitous rewrites.

The baseline static JSON scan found 660 authored nodes. The largest source lists
have 12 choices, before predicate filtering and injected actions. This is not a
runtime NPC population or simultaneous-menu count.

## Regression fixture and acceptance

`Assets/Tests/EditMode/Presentation/Input/DialogueTravelPagingTests.cs` reuses the
existing native-input `ShortcutFixture` and actual tilemaps. Its sixteen cases
cover row bounds at both Deck resolutions, tail/return paging, paired navigation
keys, first-confirm reveal, no-action paging, choice 10/12 visibility before
confirmation, mouse/window identity, reverse scrolling, ordinary short layout,
foreground/background clearing after a shorter reply and close, visible-only
shortcut activation, and no restarted reveal when paging past either endpoint. New metadata
uses a generated 32-hex UUID.

`GameAuditShortcutTests` and `GameAuditShortcutAdversarialTests` protect existing
positional shortcuts, reserved J/K behavior, reveal-before-activation and
`AllowEscape`. `CenteredModalUIViewTests` protects the gameplay/sidebar overlay
and modal return path; its existing short-dialogue case alone cannot prove that
long reports fit.

Can verify here: source-level layout bounds and failure mechanisms, exact routing
dependencies, authored text/choice counts and the regression assertions written.
Cannot verify here: native test outcome, actual glyph legibility at Deck sizes,
controller feel, or Linux rendering. The root agent owns native RED/GREEN and
visual acceptance; no Assets production changes or Unity calls occurred in this
audit before that RED gate.

## Native RED checkpoint

The coordinator's `initial-red.xml` contains 76 cases overall (49 pass, 27 fail).
The UI fixture contributes 12: 11 expected behavior failures and one ordinary
short-dialogue control passing. An earlier incidental test compile error
(`BoundsInt.PositionEnumerator` is not LINQ-enumerable) was corrected with a
`foreach` collector before this receipt; stale assembly runs are not evidence.
No production UI change preceded the confirmed behavior RED.

## Implementation checkpoint

`topics-green-ui-red.xml` confirms the hidden shortcut selected `Chosen0` when
it should have stayed on `Start`; the visible shortcut and both page-endpoint
counterchecks passed. Only after that RED did `DialogueUI.cs` change.

The implementation now bounds speech to the current choice budget within 45 rows,
provides Left/LB and Right/RB paging (also PageUp/PageDown), and resets typewriter
reveal only when a different page is reached. Long speech alone may widen to the
existing 72-column maximum so paired hints fit. Ordinary short nodes retain their
old size and footer. A ten-row choice window follows the cursor; labels keep
absolute indices, hidden shortcuts do nothing, and pointer hit testing returns
the absolute index for the current window. A range indicator occupies the
existing separator row when choices overflow. Existing cached rectangle clearing
is retained for last-page/node shrink and close.

Source self-review checked empty/small choice lists, first/last page bounds,
absolute hotkeys, portrait-column rejection, and page-local reveal counters.
The paired paging footer is 62 characters for a normal page fraction, within the
72-column paged layout. No conversation provider, gameplay action, note token,
controller mapping, save data or world state was changed by this UI implementation.
Native GREEN and final screenshots are still pending.
