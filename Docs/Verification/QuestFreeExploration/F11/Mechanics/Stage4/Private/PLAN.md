# F11 stage4: retained flight scratch

Root-approved isolated delta on frozen stage3,2026-09-29. No shared writes or Unity.

Hypothesis H1: healthy flight currently allocates >512bytes per warmed actual action due to fresh queue/set. Measure the actual method with cached delegates, diagnostics disabled, setup excluded and positive/empty counters. H2: removing snapshot copy and LINQ does not change work/party/combat authority or route tie-order. H3: warmed search data cannot affect another actor, changed obstacles, hidden-history movement, replacement saves or recursive movement callbacks.

Before production: native-reference compile new tests as a separate EditModeTests assembly and execute private RED. Send test-only manifest to root for actual native RED. Then minimal private production change: lazy private nonserialized per-grazer queue/set (value tuples only), same traversal/scoring, clear before movement; indexed stack/parent membership checks; local Eligible parts loop. No new cache authority or persisted data.

Keep original stage2/3 and336-neighbor evidence unchanged. Pair route/no-route, visible/hidden threat, first/reused buffer, separate actors, save/non-save and reentrant callback outcomes. Native real-gameplay performance remains a separate required gate; private managed microallocation is not a frame-time claim.
