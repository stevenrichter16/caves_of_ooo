# Packaged village roles and sidearms — independent review

2026-10-28. **No concrete P1/P2 blocker found in the reviewed helper changes, archive integrity, all 12 Mac captures or completed restoration receipt.** This closes the bounded delivery review. No whole-area polish, Linux execution or physical Steam Deck acceptance is implied.

## Helpers and provenance

Read the new wrappers without importing or executing them. Package wrapper SHA256 `fe96ad68c6249b2ca64c2ddc290a4794ef238b600bfc63cf9cea0e4abee18f6e`; restore wrapper `662bc9dcffb13680cdfe58c6e1093acd2e32e1d0cf300a06e03624c4fa722f86`. Both pinned base hashes match. The package wrapper changes nine checked release literals and the native evidence adapter, retaining the underlying archive/output/build guards. Its 21 current source/art/test/evidence pins match. The restore wrapper retains the existing guards, exact source/snapshot CLI binding and six named metadata paths with the existing whitespace-only restriction; this is source review, not restoration execution.

The complete successful native JSON contains 146 distinct Passed cases across the exact six fixture groups, with actual job times and no earlier subset double-counting. Root source/build provenance is `0b888da80571bf35ab4f5348ff6e600d289340e7`; this review does not independently prove source-to-binary reproducibility. Raw reports record Mac 88.55087 s / 234.33 MB and Linux 86.228922 s / 214.65 MB, each successful with zero errors and four warnings.

## Independent archive read

Streamed the entire fresh archive without extracting or executing it:

`Builds/SteamDeck/2026-10-28-village-roles-0b888da80-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`

- 128,763,314 bytes; SHA256 `c023ae9399f8ff430ceced5df451e3d7ee9d1cdd97a27b1500307b66ddee96a2`.
- 195 unique safe members: 182 regular files and 13 directories; no links or unexpected member types. Every file hash/size/mode and every directory mode matched the unpacked package tree; its complete file set matched.
- 225,573,708 unpacked file bytes. All 181 internal checksum entries match and cover every file except the checksum manifest itself. Gzip was drained through EOF, validating its footer/CRC/size.
- Archived build-info equals the saved receipt and identifies the expected source. Credits and license texts are present and hashed. The executable header is ELF64 little-endian x86-64; it was not executed.

## Actual Mac views and limits

Opened all 12 actual 1280×856 captures. Farmer, Child and Elder are visible as distinct bodies in the world and their own readers. Farmer retains the upright 16×24 body; Elder/Child retain complete 16×16 proportions. No obvious stretching, wrong portrait or clipping appeared. Elder still has a neutral static body while the reader explicitly reports Sitting; no seated animation is accepted.

Independent decoded RGB comparison of saved 05 / fresh Continue 06 reproduces zero changed pixels in `[0,96,940,696]`: 564,000 world pixels. The root's receipt records process 13739 → 14187 and explicitly discloses that a CUA state query after Cmd-Q relaunched the application before ordinary Continue 156. Pixel equality alone does not establish process provenance.

Captures 07–10 show the Mace and ShortSword in their preserved inventory branches and F1 readers. The mace head/haft and short sword blade/guard remain distinct and readable, with matching portraits and no evident stretching or clipping. Captures 11–12 show the small Dagger on the real ground cell and its matching blade portrait; the reader identifies the selected dagger at 60,3 while the player remains at 61,3. These are unchanged approved 16×16 bodies, not a new unified 16×24 art pass. The five processes and independent fixture branches are documented in actual-play.json; no branch merge or new packaged purchase is claimed.

Completed run 1–5 logs retain respectively 7/6/6/6/7 premature-thread-finalization notices and one ComputeBuffer-disposal line each. The bounded error/exception/failure text scan found no additional matching line; this is not a clean-log or performance claim.

The shown views establish bodies, readers, inventory presentation and restart continuity, not new combat, Wet, death, purchase or Farmer services. Broad disclosed grass lanes, repeated small ground marks, water glyph rows, mixed cast scale and remaining props remain composition debt. Dim remembered/unexplored regions are excluded from bare-terrain judgments. This delivery does not establish that the chunk is full or polished.

## Completed restoration receipt

Read the root's applied restoration receipt at 09:13:55.788631 UTC. It records all 52,783 tracked snapshot objects equal, 49 pre-existing dirty paths preserved, unchanged normal Editor preferences/save tree, restored original standalone-save absence/preferences, preserved prior builds and eight backed-up/restored generated changes (four settings and four specifically allowed metadata files). The test standalone tree was archived; no cache cleanup is recorded. This is independent receipt/source-guard review, not a re-execution of restoration or a fresh post-documentation comparison of the entire working tree.

- `packaged-village-roles-actual-play.json` SHA256 `213a42dc8bdaa42f1eecb3f430fa505e41dfa16e7f594bd27f73d1547b353345`.
- `packaged-village-roles-restoration.json` SHA256 `016540897946252aefb1858bd31225827d5e214ddc1fd6912b2be504a65e91bd`.
