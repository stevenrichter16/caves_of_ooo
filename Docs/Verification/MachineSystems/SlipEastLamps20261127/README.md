# Oil for Slip lights two patches of the east road

Two fixed lamps east of Slip's well now use the saved oil-supply
condition. The warden can receive settled oil or bottled lamp oil
through the existing bill. On the first ordinary turn after a valid
handover, both lamps start to cast light. Examine and interaction
status read the same condition. The town tends these lamps as a
lasting service.

The fixtures sit on dry grass reachable from the well. Placement
checks both sites before adding either fixture. The same admission
runs for newly generated Slip and old cached maps, including a save
opened directly in town. The existing watch lantern keeps its own
light. Both new lamps use its approved sprite, with a dimmer tint
while out. Light follows the ordinary line-of-sight rule. The
nearby authored ground blends with measured light near each lamp
and eases back to the usual ground palette at the edge.

## The same road before and after payment

I opened the retained seed-65 Slip arrival at tick 1870. The old
map gained two dark lamps at (44,10) and (49,10). I walked to
(46,17) and saved the unlit view at tick 2000. I then walked to
Brinna Mosswick, took the existing East of the Well concern and
handed over the three carried lamp oils bought in ordinary play.
One wait at tick 2410 activated both original lamps. Reporting
paid the existing 10 drams and 60 XP. The separate water flask
kept its twelve measures.

Back at (46,17) at tick 2890, I saved the lit view. The same two
lamps and the active supply condition survived fresh Continue.
A second old save with the delivery already reported gained two
lit fixtures immediately on Continue at tick 2270.

| Visible ground from (46,17) | Before | After |
| --- | ---: | ---: |
| East (44,12) | 0.400 | 0.725 |
| East (49,12) | 0.400 | 0.725 |
| West control (39,15) | 0.400 | 0.400 |
| Far east control (53,14) | 0.400 | 0.400 |

The comparison held ambient brightness at 0.4 and used the same
ambient color and standing cell. The game showed Dusk before and
Height afterward. Fresh views of the saved pair on the final painter
confirmed the two ground tints and the lamps' dim and lit sprites.
Both lamp descriptions came from the ordinary Examine action.
The native sprite tint rose from 0.45 to 1.0. The two sampled lawn
tiles rose from 0.805 to 0.911 in the final terrain paint. Independent
pixel checks found both nearby samples brighter and identical colors
at both distant controls.

Snakes injured the player during the first approach and later
killed an elder during the return. The player finished at 34/40 HP.
The supply and report persisted separately from the names objective.
Threats continued around the lit road.

## Verification

The first two tests reproduced missing fixtures and the missing
lighting change. A connected run caught atlas-frame duplication.
The final renderer uses the existing frame through one lookup and
keeps the atlas index unique. All 213 final focused tests passed,
including same-tick condition changes, a separate world at the
same map address, legacy access, prop rendering, and nearby lighting
systems. Independent review checked the final source and recorded
play. All 876 protected Quick files and 87 unrelated tracked files
matched their pre-run hashes. Visual evidence uses Flat Pixel
Editor play.

- [Unlit road](captures/01-east-road-unlit.png)
- [Examine an unlit lamp](captures/02-east-lamp-out.png)
- [Lit road from the same spot](captures/03-east-road-lit.png)
- [Examine a lit lamp](captures/04-east-lamp-lit.png)
- [Recorded world state, light samples and save pins](observations.json)
