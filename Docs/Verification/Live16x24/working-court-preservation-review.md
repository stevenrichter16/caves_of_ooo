# Working-court final-commit preservation review

Read-only repository inspection. No Unity, staging, restores, deletions or source edits. Exact census rerun copied to `/tmp/coo-working-court-worksite-census-rerun.tsv`; all other writes are scratch reports only.

## Advice

- **Preserve and exclude** the two unrelated modified AllotmentNotice PNGs. Their current hashes still match the October24 pushed archive manifest. Do not overwrite them.
- **Exclude or restore exactly the 17 existing Live16x24 .meta paths** listed in `/tmp/coo-working-court-preservation-paths.json`. Comparing HEAD/current line-by-line after stripping trailing whitespace proves every byte difference is whitespace only. GUIDs, sprite rects, texture filters, importer settings and other non-whitespace content are unchanged. New working-court `.meta` files are authored assets and are not part of this exclusion list.
- **Exclude the unrelated census TSV from the court commit.** It is generated in `SpreadExplorationWorksitesTests.ThreeSeedOpeningYardAndForgeCensusPreservesActualSourceBudgetsAndReturns`, source lines150 onward, and is rewritten through File.WriteAllLines(path,rows). The current rerun has six rows, identical column semantics, and only runtime entity IDs differ in five rows. The exact rerun is preserved in `/tmp` before any root-owned restore. Root can keep that copy as ephemeral run evidence and restore the historical TSV; no gameplay fix or data migration is warranted.
- Stage an exact owned manifest, not all Docs/Verification, ArtSource, Assets or the entire repo. At this check there were9,245 untracked individual paths; the large majority predates this packet. A working-court name match is only a candidate list, not proof of ownership.

## Census proof

HEAD SHA256: `6e7bf05a570a98dfc4e99b7fb2702de8e6348b7a9863aeb03473a61b6e2ad1d8`
Rerun SHA256: `426df0dd578bf4d6891ab1833f1e87525ff5f008652ace82f77ea85efd2be972`

Compared all rows/columns using CSV tab parsing. Seed1 yard row is byte-identical. Changed fields are confined to sourceActors/retainedSources/addedActors/cacheOwners/depletion IDs. Removing only numeric entity IDs before colon or after harvested:/removed-stock: makes every changed field identical. Creature names, roles, guard/lane flags, seed/zone/family, disposition/result, counts, stockPreserved/originalUnchanged and cachedReturn are all unchanged. This is test-generated ID churn; it is not new source-budget acceptance beyond the native test result.

## Current substantive tracked work

The only substantive tracked code diffs at inspection were `SpreadExplorationResidents.cs` and `FlatPixelGreyboxCatalog.cs`. New court catalog/test/assets/concept/source/docs/captures are still untracked and must be explicitly included by root after ownership review. Root is still completing Play and living records, so this snapshot is not a final completeness manifest. New runtime/test `.cs` files and native atlas each have their own `.meta` counterparts.

Cleanup remains deferred until root confirms remote push; this review did not audit/delete caches, current archive, saves, sources or backups.
