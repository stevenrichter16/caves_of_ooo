# One-measure withdrawal at the public clay tray

The ordinary tray menu can take one measure from an eligible two-measure
stack. It previews one pound of carried load and leaves the other measure in
the original stack. The Open action still supports whole-stack retrieval. The
shipping box and receiving shelf keep their existing actions.

## Actual GameBootstrap play

The earned seed 64 checkpoint began beside tray `2703` at tick 1160. The
tray held the two original cellar measures. The player carried twelve
pounds, had fifty drams, and the well was still unusable.

| Tick | Observed result |
| --- | --- |
| 1160–1170 | The normal action menu offered one pound. One action put the first marked measure in the pack and kept the original stack owner and second mark in the tray. Carrying weight became thirteen pounds. |
| 1170, fresh Continue | The existing source owner held one, and the distinct carried owner still held one. Both marks, the pack weight and the unrepaired well matched. |
| 1180 | Open and ordinary Take collected the remaining measure. It merged into the same carried stack with both original marks. Carrying weight became fourteen pounds. |
| 1190 | Ordinary repair used both exact measures and restored well service. The player drank the cool water. The separate parcel and shelf stayed empty. |
| 1190, fresh Continue | The well, empty tray, cargo stores, pack, purse and actor matched. |

The [earlier whole-take branch](../PublicClayTray20261125/README.md) used
the same earned starting checkpoint and recovered both pounds at once. The
new choice gives the original load a smaller alternative with a separate
handling action. The one-pound hard pack-capacity comparison is native test
evidence. This Editor branch kept the player’s normal capacity.

- [Ordinary one-measure action](01-take-one.png)

## Native checks

The first compiling run passed thirteen cases and failed the two missing
action cases. The final connected run passed 130 cases with one explicit
generator skip and zero failures. It verifies exact origins, a compatible
pack merge at one pound of room, altered stock, same-grade replacement,
parcel and shelf exclusion, and existing saved and paid delivery behavior.

Independent review checked the native log, twelve exact save bodies, backup
links, item custody and the screenshot. The chosen 86 foreign and 861 normal
files matched their original hashes. The inherited broad regression and a new
packaged build remain open.

[Exact test and save hashes](receipt.json) · [Actual screenshots and state readback](observations.json)
