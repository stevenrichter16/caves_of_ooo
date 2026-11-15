# Three traversal systems: routes, passages, and field intelligence

Design proposal requested 2026-11-15. These are concepts, not implemented mechanics or a change to the current graphics priority. Existing foundations are distinguished from proposed additions. The purpose is to give journeys decisions, recognizable places, and lasting consequences without making every trip slower.

## Design target

A good journey should offer a reason to choose one way over another, a local problem worth understanding, and a consequence the player can recognize on a return visit. Quiet stretches remain valuable. Most ordinary movement should stay quick; the interesting decisions belong at a small number of meaningful junctions.

The three systems have different responsibilities:

| System | Main decision | Lasting result |
| --- | --- | --- |
| Earned Routes | Which approach and return path suit this expedition? | Useful knowledge of real connections and access |
| Environmental Passage | What will I physically change to get through? | A persistent, legible change to a place |
| Field Intelligence | What do I believe, investigate, and tell other people? | New reasons to travel and visible consequences on revisits |

Do not award all three systems for unlocking the same shortcut. They can interact around one location while retaining these separate rewards.

## 1. Earned Routes

The player gradually builds a personally understood network of approaches, local crossings, cave connections, and return points. Learning a place should make later expeditions more deliberate and convenient.

Existing foundations include persistent world-map visits, local travel that respects known hazards, and saved zone connections. Optional exploration content already contains a Ginmere rope shortcut requiring both landings to be visited and two cord, plus a Sodden permit costing four drams for one passage. These are foundations to expose and connect, not new features to claim. World-map travel already advances ten clock ticks without simulating ordinary actor turns. Therefore, generic overland speed bonuses would be a weak reward, and this proposal must not degrade normal map travel to manufacture demand.

**The proposed loop:** choose a destination or personal objective; discover and physically demonstrate an alternative approach; decide whether to invest in access; record the connection and its actual limitations; choose an approach and return path on later expeditions. Record facts automatically. Do not require a survey button, repeated walking to maintain knowledge, or a route-experience meter.

The most valuable routes reach different local entry points or connect different levels. “Arrive at the known upper landing” is useful because it changes the local problem faced on arrival. It is more meaningful than “travel 8% faster.” Any alternate arrival must correspond to a real connected exit, with a valid destination and ordinary safety checks. Merely uncovering a tile does not authorize arbitrary teleportation into it. Underground links remain independent of overland map convenience.

A compact planner might show: **upper landing — rigged, last crossed on day 6; main entrance — observed occupied; cutbank passage — one permit required.** An observed condition has a date. A proved connection is not a promise of permanent safety. The planner must not inspect hidden actors to calculate the “best” route.

Three examples:

- At Ginmere, the player reaches both rope landings and must decide whether to spend two cord before going deeper. Installing it provides a real return connection. Keeping the cord preserves another equipment or repair option. The existing two-level passage supplies an immediate prototype.
- A Sodden expedition takes one approach outbound and uses a paid crossing on the return when injured or carrying valuable salvage. Only offer a longer dry alternative if the actual topology supports it. A one-passage permit never appears as permanent access.
- A return to a known ruin begins from a different demonstrated approach. The player trades proximity to the destination against a narrower retreat or a previously observed hazard. Later, a real item cache could support retrieval, but caches should be deferred until they add a decision beyond repeated hauling.

**Progression:** more connections, knowledge and relationships expand available plans. Better gear changes what an approach is good for. Avoid an upgrade that simply dominates every route. A shorter connection may lack space to retreat; a service stop may offer resupply at the cost of a detour; a paid passage may preserve health but consume money.

**First scope:** record and display the existing Ginmere connection and one ordinary return approach. Test whether players notice and use this information before building a general expedition UI or adding caches. Do not start with caravans, route deterioration or an economic simulation.

**Integrity:** save facts by world, zone and endpoint identity. Reopening a planner must never consume cord or install anything. Revalidate a selected arrival and its physical link when used. Update the small known-connection graph on relevant changes; never generate remote chunks just to score alternatives. Retain existing per-step interruption on local travel.

Sources: `Assets/Scripts/Gameplay/World/Map/WorldMapTravelCostPart.cs`, `Assets/Scripts/Gameplay/World/ControllerTravel.cs`, `Assets/Scripts/Gameplay/Exploration/SecondExplorationRoutes.cs`, `Assets/Scripts/Gameplay/Exploration/SecondExplorationSites.cs`, `Assets/Scripts/Gameplay/Save/SaveSystem.cs`.

## 2. Environmental Passage

Place a few small, understandable obstacles where changing the environment creates a different way through. Give the player a viable bypass and two or more materially different choices. The reward is a changed place, not a traversal score.

The existing game already supports structural destruction, repair, item handling, liquid contact, cold sources and saved tile state. The hedge repair/break/bypass loop and source-bound wilderness placements are already shipped. Spread passage generation checks connectivity, including diagonal alternatives. Reuse those places in the first shared journey. Supported spans would be the first substantial new environmental passage behavior; do not present existing hedge gameplay as a new mechanic.

**The proposed loop:** notice an obstacle and inspect its actual cause; compare bypass, exposure and intervention; see the cost and affected cells; commit an interruptible action; cross; recognize the retained change later. A “clear this obstruction” command can perform repeated safe work automatically and stop when danger appears. It should not require twenty identical button presses.

Three examples, in increasing implementation complexity:

- **Buckled hedge wicket.** Walk around, break it open, or repair it. Repair consumes material but preserves a controllable barrier. Breaking saves repair supplies and permanently opens the passage, surrendering the ability to close it. Bypassing preserves both resources and the existing barrier. This entire basic interaction and its source-bound placement already exist. It is an onboarding example to reuse, not a proposed new system by itself. Extra layouts would be optional content. It needs no new cultivation system.
- **Broken mire crossing.** Take a dry bend, endure wet-ground exposure, or position a short salvaged span between real supports. A portable span competes with other carried equipment. Leaving it creates a useful return crossing; reclaiming it restores portability. Deployable spans and safe footing would be new mechanics: current Duckboard art alone does not suppress a real pool’s contact effects. Preserve the pool below, define contact explicitly, and reject placing or removing a span when occupants would be stranded or invalidly supported. No quiet deletion of liquids or actors.
- **Cold throat.** An authored frost vent makes the direct passage costly. A physical hood can redirect its discharge into a recess, or toward an alternative route. The hood and directional source behavior are proposed. Existing cold remains until it dissipates normally; turning a hood does not instantly cleanse the room. This stays a bounded source-and-neighbor rule. It does not require simulating offscreen fluid flow.

**Progression:** tools, strength and preparation add solutions. Early passages teach bypass versus intervention. Later ones combine two familiar constraints: preserving a barrier, managing carried weight, or deciding which corridor to expose. Better equipment reduces work or makes a heavy intervention possible; it should not erase every drawback.

**Preventing a dominant solution:** repair, destroy and bypass must leave different useful states. If repair is always cheap, permanent and strictly superior, there is no decision. Likewise, a weightless universal bridge would trivialize water. Limit spans through physical length, support requirements and meaningful portability rather than arbitrary cooldowns.

**Preventing chores:** sparse placement, no passive decay, no recurring repair tax, no mandatory clearing of every rubble cell. Preserve a bypass where the area is meant to remain traversable. Telegraph exposure before the player commits. Refused actions cost nothing; interrupted work retains only explicitly committed progress.

**First scope:** reuse one shipped hedge-wicket placement in the shared information-and-route journey. Then prototype one supported short span in an authored location as the first new passage mechanic. Validate distinct approach choices before adding more layouts. Defer vent hoods until that intervention loop proves enjoyable.

**Integrity:** keep changes on real saved owners, with versioned construction state where needed. Commit material consumption and the new physical state atomically. Old cached chunks remain literal. Connectivity tests must include diagonal movement, blocked supports, occupied destinations, interrupted work, reload and failed transactions. Recompute affected cells and local connectivity only when a change commits.

Sources: `Assets/Scripts/Gameplay/World/Generation/SpreadExplorationPassage.cs`, `Assets/Scripts/Gameplay/Repairs/RepairablePart.cs`, `Assets/Scripts/Gameplay/World/DestructiblePart.cs`, `Assets/Scripts/Gameplay/Items/HandlingService.cs`, `Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs`, `Assets/Scripts/Gameplay/World/Map/TileStateSourcePart.cs`, `Docs/SODDEN-EXPEDITION-DESIGN.md`, `Assets/Scripts/Gameplay/World/Generation/ReferenceGladePlan.cs`, `Docs/EXPLORATION-DEPTH-NEXT-DESIGN.md`.

## 3. Field Intelligence

Make information about a place useful before the visit and consequential afterward. A journey can be about finding out what is happening, choosing whom to trust, and deciding who benefits from the discovery.

Existing travel notes already retain informants, destinations and unconfirmed reports. Newer records are world-bound and capped. Named NPC roles provide reports; rare encounter selection is saved; physical warnings can have readable approaches and bypasses. The proposed extension adds evidence states and a small number of visible reactions, not a replacement quest framework.

**The proposed loop:** hear a report; choose whether and how to investigate; observe a concrete sign or actor; decide what the evidence supports; use or share that information; encounter the consequence on a later journey. One useful observation should be sufficient for a qualified report. Corroborating every clue must not become mandatory.

A note should retain provenance: **“Innkeeper reported armed travelers here, day 4.”** A later entry records what the player actually saw: **“Two armed figures seen at the eastern bend, day 6.”** That supports a warning, not a claim that every road is blocked. Show source age separately from reliability. An old, once-accurate report is not automatically a lie. Do not infer unseen enemy positions or treat a missing actor as proof of death.

Three examples:

- **Ditch-cutters’ road.** An informant supplies an old warning. Scored shale and dragged scraps suggest the approach; observing the existing pair confirms the threat without requiring combat. The player may bypass, engage, or warn a carrier. A verified warning changes that carrier’s declared next stop. On a later visit, the player meets them at the detour and hears why. The carrier reaction is new; the threat must remain a real actor situation.
- **Disputed hedge.** A gathering lead and a viper warning refer to the same place. A direct snake observation supports “danger near the north approach,” not “the resource is exhausted.” The player can gather cautiously, avoid it, or give a useful warning. A forager later waits at a different approach. The snake does not disappear because a journal stage advanced.
- **The works’ second visitor.** A lead brings the player to apparently abandoned works, but fresh occupation traces complicate that description. Sharing the discovery with an interested group produces a bounded later visit and a competing claim; withholding it preserves privacy. No endlessly replenished loot, silent deletion of player possessions, or offscreen settlement simulation is required.

**Progression:** relationships and expertise yield more precise accounts and better questions. Later incidents offer competing interpretations or competing recipients. A reputation consequence should follow a clearly disclosed choice, not an invisible truth quiz. Reward useful testimony once, at the level supported by evidence. Repeating rumors, cycling dialogue or repeatedly confirming the same threat gives no extra reward.

**Avoiding combat dominance:** paying, detouring, warning, observing and negotiating must produce useful outcomes. If killing everyone is the easiest path to “confirmed safe,” investigation has failed. Evidence should often concern intentions, access or ownership that combat cannot resolve cleanly.

**First scope:** one incident with a physical warning, a direct observation, and one visible revisit consequence. Two interpretations and one meaningful disclosure choice are enough. Extend to a second incident only after players can explain why their later encounter changed. Preserve quiet journeys and cap active incidents in each region.

**Integrity:** save source IDs, world identity, observed turn, evidence identity, disclosure and committed consequence. Keep historical wording rather than silently rewriting it. Reconcile only relevant incidents on player action or zone activation. Do not rescan every actor in every region, regenerate encounters on reentry, or read hidden state to update the journal.

Sources: `Assets/Scripts/Gameplay/World/SpreadDiscoveryNotes.cs`, `Assets/Scripts/Gameplay/Conversations/SpreadDiscoveryReports.cs`, `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadRareEncounterBuilder.cs`, `Assets/Scripts/Gameplay/World/Generation/SpreadRareEncounterPlan.cs`, `Assets/Scripts/Gameplay/AI/PlayerReputation.cs`.

## Refined first implementation order

Build one shared journey around an already working passage: an inhabited stop, a nearby destination, and two actual approaches. A witness reports danger. The player observes it, chooses an existing repair/break/bypass solution, reaches the destination and returns. The route record captures the connection. Sharing the observation changes one inhabitant’s later meeting place. A revisit shows both the physical passage state and the social consequence.

Start with Field Intelligence and automatic route recording around existing traversal. This tests whether information changes journey choices before investing in new environmental machinery. Add one supported span next. Caches, vent hoods, generalized alternate-arrival selection and broader incident networks follow only when they offer a distinct decision.

Acceptance should be experiential: players choose different approaches for understandable reasons; a return trip feels different because of a retained change; they can explain the consequence without reading a debug log; and crossing a familiar quiet chunk remains quick. Also verify ordinary movement/readers/interactions, interrupted transactions, old saves, reload, hidden-information boundaries and bounded performance. Do not implement all three systems broadly at once.

Design review: independent source-grounded proposals and cross-system critique completed2026-11-15. One accuracy issue was corrected: the hedge repair/break/bypass loop and source-bound placement are shipped foundations. Final independent review reports no remaining P1/P2 findings within this design scope. No gameplay implementation is authorized by this document.
