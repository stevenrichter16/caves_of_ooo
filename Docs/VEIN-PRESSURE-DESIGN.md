# Vein Pressure — deferred design follow-up

Status: saved for later at the user's request, 2026-09-14. No pressure/adrenaline
gameplay changes are included in the current biome work. Revisit when returning to
combat and survival design; no calendar reminder was requested.

## User's concept

A circulatory damage model in which open wounds leak systemic pressure.
Low pressure weakens melee and stamina, but speeds clotting, reduces the
character's heat signature and slows toxin progression. High pressure from
adrenaline, stimulants, heat or rage increases damage and action speed, while
lacerations become much more dangerous and poisons act faster. Players can
travel at low pressure and deliberately surge for a dangerous fight. Chemicals
become strategic tools rather than interchangeable healing items.

The original proposal could replace bleeding/HP. The motivating player-facing
feature is a low-health adrenaline response that temporarily enhances attacks.

## Initial discussion, not an accepted implementation specification

Keep injury/HP and current pressure distinct during the first prototype. A badly
wounded character could therefore surge without every low-health character
automatically receiving a permanent damage bonus.

Four readable states: low, steady, surging, overpressured. A substantial enemy
hit crossing roughly35% HP could trigger a three-turn surge with tentative
25% melee damage and15% faster physical actions. These are exploratory numbers,
not balance commitments. Use a limited reserve recovered through meaningful
rest; avoid threshold bouncing and repeated small-damage farming.

Wounds should have distinct leak/closure behavior. Sealing wounds, replenishing
pressure and restoring injury are separate jobs. Low pressure should buy time
against poison without deleting its accumulated burden. Surging should warn
about poison acceleration and open-wound risk before causing a crash.

Possible world-specific tools: wound-sealing resins, clot-promoting fungi,
cooling salves and stimulants. These are suggestions, not newly established lore
or shipped item blueprints.

## Questions to verify before a future implementation plan

- Audit actual damage, bleeding, stamina/action scheduling, body temperature,
  thermal sensing, toxins, resting and chemical item APIs before choosing hooks.
- Decide whether pressure applies to the player alone or other circulatory
  creatures, and how nonliving or unusual bodies participate.
- Establish injury/pressure UI, explicit surge/crash warnings and counterplay.
- Test low-health trigger hysteresis, damage-source eligibility, reserve recovery,
  wound reopening, poison delay/acceleration, death/recovery and equipment effects.
- Prototype one complete encounter loop before expanding into a replacement
  damage model. Follow CLAUDE.md's plan, RED, adversarial, review and living-doc gates.

Return point: the user asked to park this idea and continue a different
lore-grounded biome/area's procedural voxel composition system first.
