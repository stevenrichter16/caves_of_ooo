# Gantry's restored guest-cloth

The original Gantry pole shows a dim torn cloth and its repair bill. The
repair uses one carried cloak and one sound oak or rough field haft. The
pole and the host's existing repair offer use the same paid materials and
saved site. The hung cloth keeps the existing art and becomes a free
public rest point. Rest takes 60 ticks, heals to full and stops bleeding.
It is unavailable with nearby danger or when the traveler is already well.

In seed 65, the player began from the ordinary Sumphold branch at 33/40 HP
with the rough haft made from one reclaimed board. The retained 22-pound
pack limit came from that earlier comparison. After normal surface travel,
the torn pole showed a missing cloak while the player's cloak was equipped.
Unequipping made the repair available. One action used the exact cloak and
haft and left the original pole hung. Nearby sari-snakes initially blocked
rest. The player and a villager dealt with the last nearby snake in
ordinary turns. Rest then advanced the clock from 2000 to 2060 and restored
seven HP. The purse stayed at 31 drams.

Fresh Continue kept the original hung pole, lasting repair, 40 HP and the
same purse at tick 2060. The rest choice now showed as unavailable at full
health. The guided Editor branch used the rough haft at the pole. The oak
and host alternatives, late danger rollback and a legacy pole loaded
without either new Part were covered in the native tests.

The previous code passed two controls and failed ten missing-feature cases.
The final suite passed all twelve new cases and 109 existing cases. The
one Tine weir test failed at the same generated-object lookup in the
matched suite on the previous commit. It passed in isolation there. That
suite-dependent failure and the full current regression remain open.
The walk had an Editor bridge delay that cleared. Sampling during the delay
mostly caught rendering and repaint work. It did not establish a cause.

All 876 normal quick saves and 87 pre-existing tracked changes matched
their starting hashes. The main Editor returned to Edit Mode with the
private root cleared. Packaged gameplay and physical Intel remain open.

The universal Mac package for `0a8e2aa1f` built from 32,258 pinned inputs
in 20.91 seconds with zero errors and five warnings. Startup loaded the
world on Apple M5. Both architectures are present in all six binaries.
All 186 package files and the previous package's 186 files matched
their hashes. Startup preserved 876 normal saves and restored the nine
preferences it changed. [Mac receipt](mac-startup-verification.json)

[Torn cloth and bill](01-torn-and-missing-cloak.png) ·
[Danger refusal](03-danger-blocks-rest.png) ·
[Ready to rest](04-free-rest-ready.png) ·
[Hung cloth after Continue](06-hung-and-healthy-after-continue.png) ·
[Recorded state](observations.json)
