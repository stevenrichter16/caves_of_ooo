# Native creature trap and Grove restriction checks — November 21

Bounded actual GameBootstrap keyboard/menu play on runtime74d85569a. No runtime
changes or repeated native regression were needed for these existing behaviors.
Both workshop trials use authored400HP/Strength70/cap5000 fixture supplies,
not normal acquisition/balance evidence. Bridge calls launch/configure/observe;
ordinary game input performs movement, actions, abilities, saves and Continue.

## A creature crosses a trap through shared forced movement

[Native observations](npc-observations.json), [actual aftermath](npc-after-jet.png),
[fresh native Look reader](npc-fresh-look.png).

- Resume earned220 fire-kit/oil archive from ColdOilPlay20261121. Walk D4/S2
  to40,12 at280. C northwest pour1oil280→290 at39,11 (pool2703); lay loaded
  fire kit2146 at290→300 (device2704). Flask2231 keeps2oil.
- C east to authored proving post2410; **its normal menu** calls CaveSlime2705
  at37,9,18HP, heat resistance0. Calling is free and stays300. This is a fixture
  service, not an ad-hoc entity spawn or ordinary-world acquisition claim.
- Ordinary waiting lets the slime approach; it does not voluntarily enter the
  trap during this trial. It spits acid and attacks. Root repositioned rather
  than changing its AI or moving it through an API. No claim of deliberate trap
  awareness follows from the observed route.
- At400, player41,13/HP366, slime40,12/18HP. Digit3 selects **Jet Blast**;
  input state AwaitingDirection; Numpad7 casts northwest. It pushes the slime
  across39,11. Log: "A spring trap goes off under cave slime."
- At410, slime is4HP (14 combined spell/trap damage), player364 from continuing
  acid exposure. Trap and pool disappear. Tile retains water5/embers3/smoke2;
  diagnostic `ignite_oil_heat` at39,11 reports1physical unit consumed/1occupant.
  The readback is **after the full ordinary turn**, including the foe's response;
  slime's final40,12 position is not a claim that the push failed.
- F5, stop, fresh Play/Continue retain410/player364, slime sameID2705/4HP,
  flask2231 oil2, no trap/pool, exact water/embers/smoke counters. Native L then
  northwest Look shows cave slime4/18, hostile, soaked79%, water5. Look is free.

Archive: `/private/tmp/coo-npc-trap-earned-20261121/d3c56928ecec4cd0a4532b3402867974`.
This earns a nonplayer forced-movement trigger/shared-system outcome. It does
not prove voluntary trap entry by all NPCs, AI avoidance policy, ordinary-player
combat balance or independent game operation.

## Grove fire refusal preserves a practical cold alternative

[Native observations](grove-observations.json), [fire refusal](grove-fire-refusal.png),
[grouped cold alternative](grove-cold-alternative.png).

- Fresh workshop launch requested Grovelands/seed64, but observed start was
  Overworld.11.10.0, non-Choir. Do not treat requested launch parameters as proof
  of the current biome. A later independent source trace explains the limitation:
  FreshGameStart.WildStartZoneId requires installation-eligible WildSurface;
  HeldPlaces.AllowedBiomes excludes Grovelands. No candidate survives and
  ResolveStartZone falls back to ordinary Spread. These files match committed
  74d85569a; this is a source explanation, not another Unity reproduction.
  Pending fields clearing/N checkpointing are expected lifecycle behavior.
  Future biome auditions should be distinct from installation eligibility or
  explicitly refuse unsupported requests; do not relax Grove placement laws.
  Main's configured start is empty. Actual qualification used world-map travel.
- Load fire kit2716 at10→20, FireMoss8→7. Ascend to WorldMap, W4/A5 to6,6,
  descend to Overworld.6.6.0 at39,11/tick220. GroveLaw predicate is true there.
  No teleport, zone-id rewrite or biome mutation was used. A second ascend while
  already on WorldMap produced the ordinary no-stairs message; no gate claimed
  from that intermediate attempt.
- C west targets38,11. Native row reads "lay the fire moss trap [spring trap]",
  disabled `grove-law`, reason "Fire is not laid on the Choir's ground."
  Highlight+Enter remains220/HP400, no device, both kit IDs retained, moss7,
  lichen8. No time or additional cartridge is charged by refusal.
- Load spare kit2717 with FrostLichen8→7 at220→230. C west's grouped placement
  submenu shows disabled fire plus enabled frost. Native select frost at230→240:
  cold device7437 at38,11; fire kit2716 remains carried. Same ground admits
  the cold alternative; no claim of a cold-trigger trial here.
- F5, separate stop/fresh Play/Continue retain240/HP400, cold device7437,
  fire kit2716, moss7/lichen7. C east shows the fire restriction again; Enter
  stays240 with all quantities/IDs unchanged. West was now occupied by the cold
  device, so the fresh refusal was correctly checked on the other clear cell.

Archive: `/private/tmp/coo-grove-trap-earned-20261121/9c5cc9cf4f914a9c960a22f44927bedf`.
Only canonical surface6,6 is qualified here, not every Grove/Cathedral location,
all prohibited fire methods, faction penalties or stakeholder attitudes.

## Review, preservation and remaining work

Independent source/artifact review found zero P1/P2 publication blockers across
the four images, native observations and shared push/trap/law source; it is not
another operator playing Unity. No new performance benchmark or test rerun.
Main stopped clean/nullroot, original input removed/restored, seed0/last scenario
empty/lastWild false/Spread restored. Fresh84foreign source/861normal game hashes
unchanged; standalone app preferences still logically match the pre-boot snapshot.
All saves/builds/art preserved. No whole-directory/prefs byte-exact claim.

The current Mac candidate boots but lacks packaged movement/action/reload proof
because native OS input access is unavailable. Town/Posy/repair/freight and
sounding noise-response checks remain; all twelve complete-machine gates open.
