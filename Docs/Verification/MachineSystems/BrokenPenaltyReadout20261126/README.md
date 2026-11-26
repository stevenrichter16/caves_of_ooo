# Broken equipment penalties show a readable minus sign

November 26, 2026 UTC. Broken equipment descriptions use an ASCII minus.
The shared display mapper also converts the Unicode minus to the same
hyphen glyph. Skill text still wraps its existing `>=` and minus forms.

## Actual play

Both runs opened the same archived Sill save at tick 160 and used the
inventory and F1 reader to select the exact carried dagger `2716`. This
dagger was supplied already broken in the earlier storage trial. Its
saved penalty stayed at two points and its hit bonus remained -2.
The condition paragraph showed `?2 melee accuracy` before and
`-2 melee accuracy` after. Closing the reader returned to the exact
selected dagger without advancing the turn.

The two 2315×1189 captures differ by 136 pixels, all within the single
sign glyph. [Before](01-before.png) · [After](02-after.png)

## Verification

The compiling RED had 38 passing controls and four failures. These were
the two signed equipment descriptions, the minus mapping and the archived
UI glyph sweep. The final focused native run passed all 241 cases, including
actual broken dagger and leather armor descriptions, the shared UI tile,
skill wrapping, and repair and inventory checks. Independent source and
visual review matched the recorded change. Armor was tested in Unity
EditMode. The rendered comparison used the dagger.

The source sweep now passes in the focused run. The inherited full
regression remains open. [Screenshots, input save and result hashes](receipt.json)
