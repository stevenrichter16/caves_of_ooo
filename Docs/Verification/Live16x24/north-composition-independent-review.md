# Independent review — northern works composition

2026-10-25 UTC. Read-only review of the current `SpreadCompositionPlan.cs` diff,
`NorthWorksCompositionTests.cs`, `GRAPHICS-G3-NORTH-COMPOSITION.md`, the native
before/RED receipts and the existing composition/worksite builders. No Unity,
test execution, production edits, image edits or Git mutation by this reviewer.

## Finding

**No actionable P1/P2 defect found in the bounded implementation.** Native GREEN
and matched actual after captures remain separate acceptance gates. This is not
approval of the complete opening or release quality.

The scope is correctly exact `Overworld.11.9.0`, non-Legacy, Fallow / `overgrown
crofts`. `northernFragments` remains its default false throughout the constructor's
original `ConnectOpenPockets()` queries and is assigned only after repair. Public
routes therefore see the original obstacles during repair. The later mask only
intersects successful old boundary hedge rolls. It cannot add hedges, alter the
old Roll result, enter an interior Tree/Bush branch, change the shared ports, or
change the formation/condition/mirroring RNG sequence. The existing `ObjectAt`
exclusions continue to take precedence.

The five mask cases preserve the proposed unequal perimeter types instead of
applying one rotated L everywhere. Geometry is expressed in the existing parcel
coordinates and existing mirror state. There is no new random draw, per-query
collection allocation, renderer pass or live graph mutation. The builder creates
only returned owners and retains its existing receipt/provenance authority.

## Test and evidence assessment

- The literal before-image hedge masks and full-cell nonhedge hashes are
  independent of the proposed mask. Their combination catches both newly added
  hedges and changes to ground, water, routes, trees, shrubs and former empty
  cells. Hashing null in place of Hedge is intentional and is paired with the
  original-hedge subset assertion.
- Broad-break assertions count consecutive **previously occupied** hedge cells;
  original random gaps and public corridors cannot satisfy them. Substantial
  remaining cover, reduced long horizontal runs and distinct deterministic
  topologies are separate checks.
- Five seeds across all three new topologies cover zero, negative and maximum
  integer seeds as well as the current 64/65 visual cases. Exact native signature
  pins cover Legacy and outside sites. Same Fallow grammar outside and wrong
  Hedgerow grammar inside are explicit scope counter-checks.
- Cardinal floods cover every resulting open plan cell and all shared ports.
  Actual detached cold generation verifies both observed 64/65 worksite
  admissions, real still ownership, finite attached forage, and a safe reachable
  approach. A real Harvest command and repeated cached-zone reads verify that
  removed forage/hedges are not reconstructed.
- I inspected `north-composition-red-summary.json`: native RED recorded 22 cases,
  17 passed, 5 expected composition failures, zero skipped. All five fail at the
  first OffsetLanes count assertion. That proves the requested reduction was
  absent; it does not establish each later assertion as independently RED.
- The scoped source/test/doc `git diff --check` is clean. The wider shared tree
  currently contains Unity-normalized sprite metadata whitespace and generated
  controlled-render images; those were neither changed nor restored here and
  should not be attributed to this plan-only production change.

## Small clarification and limits

**P3 wording:** the new field comment says subtraction cannot change existing
lanes “or other owners.” Narrow this to “or other planned terrain cells.” This
algorithm preserves the plan's other cells; newly available space and fewer
created owners can still change downstream placement candidates, process-local
IDs and later populations. The living document already states that distinction
correctly. This is a comment/claim clarification, not a demonstrated runtime bug.

The actual-owner tests establish admission, source validity and at least one
safe interaction approach for the tested generated worlds. They do not pin every
worksite coordinate, every downstream actor, full finite-stock equality, or all
world seeds. The existing worksite `Fits` checks remain authoritative for its
original two physical approaches and withdrawal geometry. Saved-graph acceptance
in the new fixture is cached in-memory return, not a disk save/load proof.

The fixture's 40–125 hedge bound is not a 50–65% retained ratio guarantee. The
latter was a design target from raw perimeter geometry; report actual native
counts and inspect the paired-band reduction before claiming that target. More
open ground may change sightlines and threat approach even while preserving
existing paths. Ordinary arrival, worksite use, departure/return and visual
comparison at the same framing remain necessary.

The living document correctly rejects the imagegen output as production pixels,
retains approved art and names conditional worksite admission. Its pending status
needs root's final RED/GREEN, actual capture, review and limitation receipts before
commit. No new asset packet is warranted for this layout-only correction.


## Final receipt review, 03:40UTC

Independent follow-up found no runtime P1/P2. It verified138distinct native
passes and actual source6944/6945 absence, coating6974 persistence, exact carried
inventory/equipment and32HP on return. It identified stale pending-status wording
in the composition document; root corrected the header and dated earlier status.
Seed65 visuals, full-route and packaged/Deck gates remain open.
