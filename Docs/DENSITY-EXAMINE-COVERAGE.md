# C5 — remaining authored examine coverage

**Status:** 39 short descriptions applied by root after executable RED; focused
verification is GREEN. Existing runtime weapon/armor/tonic details remain the
source of mechanics. Root owns Objects.json and integration.

The C1 bounded actual-source corpus contains common equipment with no authored
flavor: ChainMail 26 units, Dagger 19, LeatherArmor 11, Claymore 10, IronBuckler
and LongSword 8 each, Warhammer 7, Greatsword 6, Battleaxe and Spear 5 each.
These counts include opened containers and ordinary starting shops; they are not
per-hour acquisition rates. The new 190-zone scenery sample has 2,303 Bush,
1,891 Reeds, 1,872 Tree, 157 Crate, 149 Chair, 109 Bed and 96 Sack instances with
no authored prose. BerryBush (29) and Beehive (3) already advertise Harvest but
have no authored prose. These are useful leaf-blueprint targets; blanket prose
on PhysicalObject/Terrain would leak onto unrelated authored things.

The sweep resolves inheritance and respects **both** Examinable.Text and its
Description alias. The earlier scenery report's Description-only field is not
sufficient to declare a missing description; these targets were checked against
both spellings. Existing authored signs, shrines, native story props and plants
with Text are excluded. Dynamic item mechanics already work and must appear
exactly once after new prose. New wording contains no damage number, bonus,
rarity, guaranteed stock, purity, temperature, mystery answer or forged provenance.
No new verb is promised for beds/trees/reeds. Only BerryBush's already-real
Harvest is directly named. Natural containers retain their existing loot roles.

## Proposed bounded first pass

The structured draft at
`Verification/DensityCompletion/ExamineCoverage/proposed-descriptions.json`
contains **39 leaf blueprints: 23 equipment, 14 scenery, two plants**. Each gets
one short tactile/structural observation. This is not dialogue and does not add
lore revelations. Materials and physical identity must be checked against each
final blueprint before root applies a surgical Examinable.Text parameter.

| Blueprint | Draft text |
|---|---|
| Dagger | A short blade with a close-fitting grip. The point has been kept sharp. |
| ShortSword | A compact sword with a straight edge and enough grip to keep the hand behind the guard. |
| LongSword | A long, straight blade balanced above a plain crossguard. |
| Claymore | A broad sword with a long grip and a guard that spreads well clear of the blade. |
| Greatsword | A large blade with room for both hands on the hilt. Its reach begins well beyond the grip. |
| Hatchet | A short haft carries a wedge-shaped head. The back of the blade is thick enough to take a knock. |
| Battleaxe | A broad axe head sits on a reinforced haft. The cutting edge curves away from the socket. |
| Cudgel | A stout club, heavier at the striking end and smoothed where a hand holds it. |
| Mace | A heavy head on a short haft. Its raised faces concentrate the weight of a blow. |
| Warhammer | A dense striking head is fixed across a long handle. The face is narrower than the mass behind it. |
| Spear | A narrow point extends from a long shaft. The join is bound close beneath the head. |
| LeatherArmor | Panels of thick leather are joined where the body needs to bend. The fastening straps show repeated use. |
| ChainMail | Small metal rings overlap in a flexible mesh. Their weight gathers wherever the garment folds. |
| PlateArmor | Shaped plates overlap at the joints, with straps holding each section against the body. |
| Buckler | A small shield with a raised centre and a grip behind it. |
| IronBuckler | A compact iron shield. Its turned rim leaves a shallow bowl behind the face. |
| IronHelmet | A worked iron shell with an opening for the face. The rim has been turned away from the skin. |
| LeatherBoots | Supple leather boots with thicker soles and reinforced seams around the toes. |
| IronshodBoots | Iron strengthens the soles of these boots. Scuffs mark the places that meet the ground first. |
| LeatherGloves | Close-fitting gloves with extra leather across the palms. |
| LeatherCap | A fitted leather cap, stitched in curved panels to follow the head. |
| Cloak | A broad piece of cloth with a fastening at the neck. Its hem carries the wear of travel. |
| WardedCloak | Fine stitching follows the edges of this cloak. The repeated pattern continues around the fastening. |
| Tree | A woody trunk rises from a spread of roots. Branches fill the space above it. |
| Bush | Low branches and leaves form a close tangle near the ground. |
| VineWall | Interwoven stems have thickened into a wall. More growth knots around the older strands. |
| Reeds | Jointed stems stand close together, with narrow leaves folded along their sides. |
| Crate | Boards enclose a cargo space. Its corners bear the marks of lifting and stacking. |
| Chest | A hinged lid closes over a deep storage box. The frame is reinforced at the corners. |
| Sack | Coarse cloth is gathered at the mouth. The folds conceal whatever has been packed inside. |
| WovenBasket | A close weave rises to a stiff rim, making a light container for things that need carrying. |
| HollowLog | The centre of the wood is hollow. The opening leaves room to store small things inside. |
| Chair | A seat and back are joined above a set of legs. The places hands rest are worn smooth. |
| Bed | Straw fills the sleeping place. The top has been pressed flat and gathered up again. |
| Campfire | Stones mark a place for a fire. Ash has collected between them. |
| Well | Hands and hauled ropes have polished the rim. The water lies below. |
| MarketStall | A counter gives someone a place to set out goods. Trading belongs to whoever tends it. |
| BerryBush | Wild berries cluster among the leaves. The fruit can be gathered. |
| Beehive | Wax cells crowd the inside. Some hold honey. |

## RED and acceptance proposal

Use actual factory instances for all 39, asserting nonblank authored text and the
public BuildExamineLine includes it once. Current instances provide the missing-
text RED. For representative weapon/armor/tonic mechanics and a modified instance,
assert live numbers remain correct after the flavor prefix. Preserve existing
sign/quest prose, no-description base templates and harvested/changed runtime
state as controls. Verify every new character fits the renderer's ASCII contract,
no newline/layout overflow, read-only state, and exact parsed before/after object
diff limited to approved Examinable params. Native acceptance can inspect one
found weapon plus one real harvest plant/scenery target, not 39 staged screenshots.

**Can verify now:** inheritance, existing actual-source frequencies, current
absence of either supported text parameter on these targets, and mechanics kept
out of proposed flavor. **Cannot verify yet:** a native rendering pass, final art
shape correspondence for all equipment, or the final prose after root review.

## Executable missing-text RED

The private actual-factory fixture ran **47 cases: 39 expected missing-text
failures and eight controls pass**. Controls retain blank base templates, changed
live weapon/armor mechanics, previously authored Signpost/BreacherCleaver text and
spent/unspent plant state. The 39 target cases also require one flavor occurrence,
ASCII-only short copy, working Description alias and stable repeat inspection.
`ExamineCoverage/authored-red.json`, compressed XML and the exact test source
preserve the pre-data evidence. No blueprint has been edited by this agent.

## Implementation, counter-checks and review

Root reviewed and surgically applied the 39 Examinable parts. Parsed receipt
`ExamineCoverage/content-parsed-diff.json` proves exactly 39 changed objects and
455 unrelated objects unchanged. No item mechanics, harvest yields, actions,
loot tables or mystery answers change. The cudgel description avoids an
unsupported material claim; the bed matches its authored straw-bed identity.

The published `DensityAuthoredExamineCoverageTests` passes **47/47** after data.
The existing core and dedicated adversarial ItemExamine fixtures also pass, for
**116/116** total (`ExamineCoverage/authored-green.json` / compressed XML). They
cover dynamic weapon/armor changes, legacy tonic details, enhancement/affliction
composition, unknown/malformed effects, read-only state and non-item exclusions.
The new paired controls preserve blank abstract templates and both spent and
unspent plants. This change reuses that existing adversarial machinery rather
than inventing a second inspection implementation.

Q1/Q2: both supported Text/Description spellings resolve to one field and one
composition path. Q3: static flavor cannot replace changed runtime numbers or
reset a harvest flag; new leaf copy must not leak into base templates. Q4: counts
are bounded content-sample observations, not global acquisition rates; words
express appearance, not new powers. No open correctness finding was identified.

**Can verify:** actual factory inheritance, all 39 authored texts, short ASCII
copy appearing once, unchanged runtime mechanics and direct inspection state.
**Cannot verify yet:** final native popup wrapping/readability after this added
copy, correspondence with every final art asset, or player discovery in ordinary
play. Existing item popup pagination remains responsible for long combinations.

The final broad regression exposed one intentional legacy pin: the Chest Examine
message formerly ended after its generic name. ExaminablePartTests now requires
the exact reviewed full sentence, explicitly rejects weapon/armor stat text, and
retains contained-item identity/owner and lock state. Its full 25-case fixture is
GREEN in `Underground/lair-placement-shared-green.json` (81 total neighboring
cases). No runtime composition change was required.
