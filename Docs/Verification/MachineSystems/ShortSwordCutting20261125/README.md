# A short sword can cut prepared flexible stock

The existing short sword now carries the explicit flexible-stock cutting
capability. Players can use the blade they already carry for the same
prepared-material recipes as a dagger. The ordinary cutting receipt still
requires an exact sound and owned tool. The sword keeps its existing combat,
weight and market definitions. A plain hatchet still follows its established
wood and combat uses.

## Actual GameBootstrap route

The player continued the exact Sill save from the
[weapon merchant trial](../WeaponMerchants20261123/README.md), already
holding the previously bought sword `17369`. That isolated test character’s
earlier supplied 412 HP was retained. The current branch supplied neither
the sword, the offcut, nor its result.

Nerrel’s ordinary stock held two cured offcuts in stack `17377`. Native
trade bought one measure for 11 drams, leaving the original stack with one.
The player stepped out of the shop onto ordinary ground and set aside the
exact dagger. The acquired leather’s ordinary cutting action selected the
sword as its sole available provider. One action spent the exact offcut and
produced binding `30417` at tick 11430. Normal pickup recovered the same
dagger at tick 11440 and equipped it. Fresh Continue preserved both tools,
the binding, 15 drams, and the merchant’s exact remaining offcut.

## Native and independent verification

The prior catalogue passed 12 cases and reproduced the missing authored
sword choice. The final connected run passed all 190 cases with zero skips,
failures or compiler errors. It exercised the authored sword with no other
valid provider, the plain-hatchet control, existing material receipts and
merchant behavior. Old concrete swords gain one part through the existing
definition upgrade. The test also retained a deliberate removal when the
item had already seen the definition.

Independent review checked the blueprint, existing stock identity and actual
recorded outcomes. The broad regression and newer Mac package remain open.

[The cutting action](01-cutting-action.png) · [Observed states](observations.json)
· [Exact evidence](receipt.json)
