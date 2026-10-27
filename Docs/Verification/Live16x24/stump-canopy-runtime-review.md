# Stump next-three runtime and test review

Result: **no concrete significant defect found in the reviewed change**. This is an independent source/fixture review of the three-species extension, not a native GREEN, actual-image or device-performance result. The reviewer authored the earlier five-species base but did not author this extension or its new fixture.

## Scope checked

- Exact added owners/resources are SummitSinger/summit_singer, BrocchiniaSentinel/brocchinia_sentinel and SkySari/sky_sari; glyphs remain f/l/E. Default construction loads all eight resources, while the existing five-argument constructor delegates with three explicit nulls and does not load hidden resources. The eight-argument injection order matches the frozen test contract.
- Existing five sheet construction still creates centered FullRect16x24 cells at(0,360),16PPU. BuildFrame extraction is mechanically unchanged for those bodies. The three new textures require exact16x16 and get centered full-texture square sprites,1x1bounds. No canvas scaling, resource mutation, importer reslicing or PNG generation occurs. Independent optional groups and disposal retain borrowed textures.
- HasKnownRole admits only the added exact blueprint names. Shared live HP, owned Render/Physics/Body/Brain, canonical glyph, visual-override, custody, footprint and stack guards are unchanged. There is no habitat, current population flag, health-at-max, faction, passive-AI or conversation predicate. Existing Greybox current graph/selection/fog/reader authority and one-Render-event color path are unchanged.
- The new19cases meaningfully cover three public world/authorized-reader positives, three real nonlethal damage paths, actual wet application/removal, selected semantic faults, normal Sentinel movement onto/off a real Tank, current graph/custody/footprint, habitat/Urqu independence, resource shapes and independence, five-argument compatibility, unrelated glyph/corpse identities and owned-resource disposal. World identity transforms and square source bounds are asserted; reader full UV and square integer-size layout are also asserted.
- The original44-case deferred-species test remains legitimate: it deliberately constructs the five-argument catalog, whose new three inputs remain null. It need not remove those negative cases merely because default construction now admits the three.
- The fixture uses the real submitted painter, real ApplyDamage and actual effect application. Reader/neighbor checks do not manufacture Render events. Current document authority is checked through the retained document rather than broadening the raw semantic helper. DensityLootTestScope restores NarrativeStatePart.Current; the painter restores render hooks, prior tile-state callbacks and performance counters.

## Evidence and limits

Independently parsed the retained native RED:19executed,19failed,0passed,0skipped at14:17:21–14:17:24UTC. Failures are absent-native body or missing8arg overload, not a compile-only failure. Native GREEN and related reruns remain pending/root-owned at this review.

The four earlier handoff files other than the changed catalog remain hash-identical, as do all ten prior approved sheet PNG/meta inputs. The three new original16x16 textures/importers were inspected read-only in the preceding geometry assessment. Actual current palette contrast, Sentinel/Tank separation, Singer back marking, SkySari scale and ordinary restart still require root gameplay evidence. A square reader may enlarge more than a padded16x24portrait; this preserves aspect and native pixels but does not promise identical padded-portrait scale. No all-biome, animation, allocation, Linux or Deck acceptance is inferred.

## Frozen reviewed inputs

- `Assets/Scripts/Presentation/Rendering/FlatPixelStumpFaunaCatalog.cs` — SHA256 `01d14c55bb7864438c37717a14909894c0232ec26e9059f82052ffd4058fd44c`
- `Assets/Tests/EditMode/FlatPixelStumpSummitFaunaTests.cs` — SHA256 `83222eaf191026ba9d63958e5fd697ebd05cdb028b7fd7f68f233678f98a06f6`
- `Docs/Verification/Live16x24/stump-canopy-red.xml` — SHA256 `f962ad7b4ffae4c4a2edc71f55ab3e87af6790f87e22e73af963ca55e38488fc`

## Native-discovered fixture premise correction

Root's first implementation run is retained as stump-canopy-initial-green.xml:19executed,1passed,18failed. Independent XML inspection confirms all18failures are the same helper assertion: expected sprite bounds(1,1,0), actual(1,1,0.20). The planar X/Y geometry is correct; Unity's Z culling extent was an unsupported fixture assumption. The current helper compares Vector2.one to(Vector2)sprite.bounds.size, retaining the actual no-stretch requirement and all rectangle/pivot/PPU/transform/reader controls. The source diff against preserved RED tests is exactly that assertion. This is a legitimate test-premise repair, not a runtime or geometry relaxation. My earlier review missed the Z-extent premise. Runtime source remains unchanged; final native rerun is pending/root-owned.

Current reviewed corrected fixture SHA256: `83222eaf191026ba9d63958e5fd697ebd05cdb028b7fd7f68f233678f98a06f6`. Initial implementation-run XML SHA256: `aa1d761d227de97b7c1b4768ed57ede6ee6510bb7c1414bde659e9f36d7c1f82`. The fixture hash above is the corrected current fixture. The original RED source is retained separately as stump-canopy-red-tests.cs.txt.

Root result addendum14:20UTC: corrected focused19/19 and seven related suites266/266 native PASS; no skips/failures. Exact XMLs retained. Actual gameplay comparison is next. No runtime edits since review.
