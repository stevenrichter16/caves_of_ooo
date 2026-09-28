# Finite pool draw invalidation — before production

Root-authorized service-level redraw contract. WaterVesselService.cs40–41 and LiquidVesselService.cs96–97 currently decrement a plain LiquidPoolPart.Volume field then call only RetireExhaustedPouredPool. Its exact PouredLiquidPool guard preserves natural/persistent owner identity but emits no draw invalidation. InputHandler.CloseInventory:3608 already forces full redraw after the ordinary inventory path, so this is not a demonstrated keyboard rendering failure.

Minimum: shared after-commit observer, exact initial source/cell/pool identity, no redraw from rollback/refusal or removed/replaced/moved sources. Keep existing poured retirement body unchanged, persistent source stays owned at Volume0. Test direct services with a real outer transaction; capture and restore exact registry, message state and dirty callbacks. No Unity invocation.
