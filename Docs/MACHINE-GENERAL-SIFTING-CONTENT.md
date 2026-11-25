# General sieving: useful mixed stocks before more recipes

November24local/25UTC,2026. Design and source qualification only. The user asked
for a general size-sorting tool, then explicitly asked to brainstorm the missing
content and uses for its fractions. Current shipped sieve is the bounded
QuarrySweepings preparation; this document adds no runtime acceptance.

## Player purpose

Ordinary travel should reveal optional mixed stocks worth taking apart: a
workshop's grading bin, an extra spoil pocket, rough traded crop stock or a
known cold fire residue. Sifting can reduce bulk, supply two useful materials,
prepare a grade for a project, or concentrate a fraction for another tool.
Both the retained and passing fractions can be valuable. Readers and previews
state contents, actual quantities, mesh choice, losses and destinations.

The same physical size rule should work across material identities and places.
Many materials can occupy one size band. A mesh is not an assay, density pan,
sterilizer or chemical purifier. Overlapping particle sizes stay mixed until
another appropriate operation separates them. Sorting never removes a harmful
property merely because a smaller fraction is collected.

## Source corrections from the first content pass

These are direct source findings, not new gameplay observations.

| Candidate | Actual current foundation | Consequence for proposed content |
| --- | --- | --- |
| Fire-clay workings | RepairClayBank is a finite Harvestable source of exactly4FireClay. FireClay repairs a well lining or batch pan for2units and is a hand-pump ingredient. | Strong existing fine-fraction consumers. Preserve the ordinary4clay harvest. An optional extra dry spoil lot needs its own declared budget; ordinary soil is not automatically fire clay. Coarse aggregate still needs a genuine consumer. |
| Old wood-fire residue | AshPile is rendering-only. AshBed is a hot thermal/tile source. FuelPart has FuelMass and ExhaustProduct but no declared recoverable charcoal/ash manifest or feed identity. | First content would be a newly authored finite known-wood residue lot. No free mining of decorative ash or retrospective output for every spent fuel unit. Charcoal fuel and ash soap/soil consumers are new work, not shipped hooks. |
| Grain and seed cleaning | Emberwheat is already an edible2d4 **sheaf**, directly Cookable into ToastedEmberwheat. Knotflax harvest already strips/twists stems into2cord and returns1seed. | Preserve these convenient yields/actions. A loose detached viable-seed lot must be new authored stock; screening does not thresh a sheaf or reconstruct one from grain. Never force ordinary crops through cleaning to regain their current benefit. Chaff has no demonstrated packing/fuel consumer yet. |
| Workshop sweepings | RecoveredIronBlank and declared damaged-tool manifests already feed bounded cold processing. Existing material names and composition never imply extra recoverable stock. | New sweepings need actual declared metal identity/quantities and an outlet for both grades. Dust is not another whole iron blank, and a sieve does not concentrate metals of identical particle size. |
| Peat bank | PeatBank has a solid body, Peat material and10HP Destructible; it has no recoverable constituent manifest. | Cutting/destroying it is not evidence of a peat fuel/fibre yield. Recovery and both consumers remain proposals. |
| Grove compost | CompostRow describes reclaimed bodies; CompostCache yields a finite pocket find of1–3coins at60percent. | Exclude these from garden amendment feedstock. Any mature gardening compost must be a separately authored ordinary material, with no incidental change to the Grove's bodies or their meaning. |
| Renewable source budget | SourceStockPart refills on saved world-day intervals. | Do not use it for finite mineral, clay or fire-residue lots. A new optional weather deposit also needs a finite per-event/source budget and no travel/reload refill. |

Primary source paths:

- Assets/Resources/Content/Blueprints/Objects.json: RepairClayBank, FireClay,
  AshPile/AshBed, Emberwheat/Crop, KnotflaxCrop, PeatBank, CompostRow/Cache.
- Assets/Resources/Content/Data/Repairs/Tier1Repairs.json: clay-well-lining and
  clay-batch-pan; Assets/Scripts/Gameplay/Repairs/RepairRecipeRegistry.cs.
- Assets/Scripts/Gameplay/Items/HarvestablePart.cs: saved once-only source
  consumption, quantity range, claims and output overflow/rollback.
- Assets/Scripts/Gameplay/Materials/FuelPart.cs and
  Assets/Scripts/Gameplay/Alchemy/ProofingSources.cs: distinguish spent fuel
  from a recoverable material manifest and renewing source stock.
- Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs and the
  MaterialProcess family: keep current convenient preparation/transactions.
- Lore/History/08_MaterialCulture.md, Lore/README.md, Lore/10_Bible.md as amended
  by Lore/11_SecondSpine.md and Lore/MYSTERY-LEDGER.md remain authority. Size
  grading creates no new ward, universal power source or protected revelation.

## First useful content bundle to qualify

**Clay-working spoil is the leading candidate**, because appropriate fine clay
already has concrete repair consumers. Its ordinary bank remains useful without
a sieve. Proposed extra dry loose spoil contains declared fine FireClay and
coarser aggregate, each finite. The exact values, access and placement are not
chosen yet. A saved old bank gains neither a new lot nor extra harvest output.

Coarse aggregate needs a practical use before this becomes a complete playable
addition. Two alternatives to qualify are shared suitable-grit traction, and
a lasting workpad/drainage project with a concrete service benefit. Neither
consumer is implemented by this document. Ordinary water is already walkable;
do not revive a fictitious bridge/access gate merely to consume gravel. Plain
aggregate does not become SilverSand, and its material alone grants no recipe.

Source correction: CombatUtilityActions currently admits **SilverSand only**
and writes grit for12turns. Admitting suitable coarse grit through an explicit
shared material capability is new behavior, with clay/fine-dust negatives and
unchanged SilverSand controls. It is the smallest proposed coarse consumer;
a project is deferred until a distinct lasting benefit has been qualified.

The next broadening candidates are a separately authored rough seed/crop lot
and a declared workshop residue. Fire residue remains useful design, but both
its provenance/budget and the fine ash consumer must be solved first. These
candidate lots are not three completed capabilities or three earned sources.

## Minimum mechanical contract to critique

Use a few readable size bands rather than simulating particles. Authored or
properly acquired instance stock states actual constituent identities, finite
quantities and size ranges. The same mesh comparison handles every admitted
stock; adding an appropriate material should not require a new tool-specific
blueprint pairing. A lot containing overlapping sizes produces a mixed grade,
not falsely pure ingredients. Do not invent quantities from a material tag.

The first prototype can refuse lots with overlapping/uncertain grades rather
than implement nested mixed fractions. Work on complete batches and integral
commodity units; incomplete units remain accounted stock, never rounded into
whole outputs. Empty/all-passing/all-retained passes create no falsely purified
new products. Repeated passes, stack splitting, interruption and reload cannot
reset the original finite constituents. The current quarry packet also has
declared rock-dust loss but no size manifest: do not silently reinterpret its
legacy preparation as a proven general mesh partition.

Keep this within existing Parts and supervised action transactions. Qualify
actual source ownership, mesh/tool identity, both output capacities, partial
stock, callback interruption, exact custody and save/reload. Preserve private
state rather than erase contamination, origin, treatment or identification.
Prefer existing quantity/claim/output-admission contracts to a generic job,
particle, terrain-excavation or composition simulation framework.

Manual selection is an alternative where individual pieces can reasonably be
picked out; NPC preparation and finished stock remain alternatives elsewhere.
A mixed sack whose contents can already be individually taken in one action
does not by itself earn a new sorting tool. Before coding, demonstrate what
the sieve saves or enables beyond the existing container menu.

## Acceptance and scope

Independent bounded content/source critique by capability_audit is complete.
It supports optional dry clay-working spoil plus a new useful coarse-grit
consumer as the smallest coherent candidate. It does not accept a new source,
generic sieve operation or implementation. Three design risks become gates:

- P1 transmutation: size must preserve actual identity, dryness, purity,
  treatment and viability; screening is not threshing, washing or assay.
- P1 multiplication: integral quantities, partial stocks, repeated passes and
  fuel-derived residues cannot duplicate commodity units or usable energy.
- P2 compulsory chores/useless outputs: preserve current harvests and services;
  qualify a purpose for each useful fraction before adding byproducts.

No new tests, runtime,
Unity play, source yields, stock placement or machine gate is accepted here.
The next implementation must first have finite inputs, meaningful destinations
for both useful fractions and a reason to use size sorting. It then requires
native RED/countercases, actual ordinary menu/movement/use/F5/fresh evidence and
independent source/recorded-play review. A broad framework or a list of new
recipe IDs alone does not satisfy the user's general-tool direction.
