# Weapon merchant standard stock and specials — November 23, 2026

User-requested extension, in progress. Current published behavior opts in only
Cinderhold's genuine WeaponsmithStock. Other weapon merchants still use legacy
restocking until this extension is verified and published.

Extend the same saved consignment rules to every genuine matching
WeaponsmithStock declaration. Weaponsmith and the authored town Smithy use that
table. Do not opt in general merchants, tinkers or quartermasters solely because
they sometimes carry a weapon. Preserve their existing services and supplies.

Dependable weapons: one dagger, short sword and spear. Dependable components:
one oak haft and leather binding. Cinderhold retains its two iron spikes for
the already qualified pump-repair alternative; regional/sold/ordinary goods
stay intact. The two saved special slots cycle the nine other original optional
goods: split-haft token, mace, hatchet, long sword, battleaxe, warhammer, steel
blade, serrated edge and stun grenade. No new prices, reward, route, one-off
relic, Part or save field. Use the existing1200-tick consignment window and
strictly greater-than300-tick staple top-up, custody and callback guards.

Migration needs an explicit rule: a saved short sword or spear special stays
the exact same current offer until normal rotation. At rotation, retain any
now-core physical good as ordinary stock while replacing special membership.
Never withdraw/recreate that unit. Counting it toward the floor and then
retiring it would stall reconciliation or manufacture duplicates. Qualify both
clocks due, staple-only and special-only, modified/escaped promoted goods and
save/reload. Keep the existing profile ID so old state remains compatible.

Independent bounded design critique accepts this scope and the preservation
policy. Native RED/countercases, canonical fresh weapon payload checks, actual
shopping at one genuine merchant outside Cinderhold, reader/purchase/current-lot
save and later rotation, final source/gameplay-evidence review and publication
remain open. Tests do not establish ordinary affordability or discovery.

This supports the user's broader aim: familiar equipment and unusual finds
should suggest more useful things to attempt throughout the world. Stock
variety alone does not implement those interactions. All12 complete-machine
gates and the original failed full regression remain open.
