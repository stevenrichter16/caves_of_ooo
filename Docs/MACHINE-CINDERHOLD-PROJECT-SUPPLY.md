# Lasting Cinderhold work expands the smith's consignment

November24,2026. Bounded Editor accepted after native, actual and independent
source/artifact/gameplay-evidence review0P1/P2. See
[the receipt](Verification/MachineSystems/CinderholdProjectSupply20261124/README.md).

The existing Drowned Level project already accepts reclaimed iron spikes plus
an oak haft and binding for a lasting pump repair. A separate four-fire-clay
supply approach seals the seep, with its existing grove/faction consequence.
The temporary two-clay collar patch relapses. Current smiths carry reliable
repair inputs and two rotating specials, but neither lasting project route
changes their consignment. That is a useful missing consequence of local work.

Smallest extension: Cinderhold's existing dedicated smith profile gains one
ordinary paid sounding rod in each FUTURE consignment after a stable/improved
Pump site OR the existing Cinderhold.Sealed condition. Both durable approaches
qualify; a temporary patch, mere PumpRunning flag, field clearance, remote repair
or uncreated settlement must not. Keep all base stock and two rotating choices.
No instant reward/item grant, current-lot rewrite, free reroll, continuous NPC
production or claim of physical drainage/new mine access. No new project or
repair cost: reuse the existing lasting choices and their consequences.

Cinderhold is canonical iron/Black-Gall mining country with a practical,
Concord-dominated economy (Lore/History/02_Geography.md and08_MaterialCulture.md;
Lore/Factions/04_SaccharineConcord.md). The shipped rod is an iron-shod ash tool,
not a magical detector. It reports only bounded terrain bearings/distances on
ordinary cave floors and makes audible noise; protected/authored places retain
refusal. Supplying this existing tool fits restored mining work without claiming
an autonomous workshop simulation. Buying elsewhere and ordinary exploration
remain available. The lasting benefit is continuing local access to a useful
expedition tool; the player need not repeatedly repair the project to obtain it.

Reuse MerchantStockService.ProjectSupply on the already place-specific
CinderholdSmith profile. Existing ProjectSupply can carry Site and Condition;
make their qualification explicit OR, preserving Farmer site-only and Scribe
condition-only behavior. Reconcile only at existing authoritative arrival/shop
open and game-time consignment boundary, with bounded catchup, saved membership,
normal price/capacity, active-view/offer/trade protections and rollback intact.
ProjectBenefit's unmet/ready/included messages must fit78characters and describe
both lasting paths. Never create a settlement/site by merely reading stock.

Source correction: sounding rods carry LastReading, LastTick and a bounded
List<string> survey log. Existing merchant fingerprints reject unknown Parts and
non-scalar payloads. Do not broadly whitelist the rod for stacking/grouping or
add a general list serializer. Add exact merchant-only SoundingRodPart identity
for those three own fields and at most LogSize entries, bound total key length,
no Sense/ReadLog/reader invocation. Require fresh stock to have exact built-in
part, empty log/reading and LastTick=-1. The rod Part is sealed; missing/duplicate/invalid
logs fail closed. Player-used or resold tools retain their actual log and cannot
be withdrawn as unchanged unsold consignment stock. No new saved fields.

Gates: bounded independent source/design critique; compiling native RED covering
both durable routes, temporary/remote/no-state controls, next-window boundary,
paid purchase/current-lot save, retained old saves, changed/resold rod preservation,
malformed fresh rod and callback eligibility loss, no grouping/stack permission.
Retain farmer/scribe/weapon/merchant and saved-contract checks. Actual native
GameBootstrap must read the project benefit on a previously earned lasting
project save, preserve current lot, advance ordinary game time, see the third
paid special, purchase/inspect its real tool and F5/four-file archive before
Finish/full fresh. Reuse prior accepted pump/reclamation and rod/noise evidence;
do not replay them as new. Explicitly separate a consignment extension from a
complete resonator, physical drainage or fresh ordinary acquisition proof.
Capture real screenshots; independent source/gameplay-evidence review before
acceptance. Update living design, dependencies, continuation and scheduled task
with earned gates. CurrentMac632 remains the last delivered package; batch a
later coherent milestone rather than build this data/stock extension alone.
