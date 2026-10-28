# Camp props and cloth: bounded helper and preservation review

Published runtime source: `9a23718527e10e4ba7d24df203b8170bd12ba33e`.
This is the helper author's source/state review, not an independent gameplay or
archive certification. No Unity operation, native test rerun or restoration
application was performed by this reviewer.

The durable `camp-props-cloth-package-audit.py` has SHA256
`672a180abf01c7235ca03ce743d9a50a1ad76ea26cd145ec417275e528a8bb78`.
Its 15 selected file pins match, including identical source/runtime v2 cloth PNGs
(`3deb52ac4c4be95f6d5b738ca06176dfc5722a2ce564dfb9e11cfc00770ef6a8`).
The single retained native XML contains 114 unique Passed cases: 12 cloth,
26 camp props, 43 storage and 33 furniture. Earlier executions are not added;
the later art revision and actual gameplay have separate evidence. The nine
generic package functions and main bytecode remain unchanged from the pinned
auditor; only the checked labels and evidence provider are rebound.

The durable `camp-props-cloth-restore.py` is byte-identical to its scratch copy,
SHA256 `cc959c5eb644ee0a7474e499b65ec46c6ab96c45a84e87161cb360cac63f1a4a`.
It reuses the pinned durable surface-actors restorer. Its only underlying changes
are the receipt-name literal and eight specifically allowed metadata paths;
the wrapper additionally requires this exact source and fresh snapshot.
Root explicitly authorized the three older Slate/Quartzite/Obsidian PNG metas:
each had exactly six trailing spaces, and removing those spaces reproduced its
snapshot hash. No semantic importer change was accepted. Existing backup,
reviewed-delta, preference, save, index and prior-build guards remain intact.

After both player processes closed, the reviewed manifest was saved at
`/private/tmp/coo-camp-props-cloth-reviewed-restore.json`, SHA256
`9ba582ec8938ab84fc68a7e7f32278c8f0e32c79ebc7b96bfd47d3f99a4c0c99`.
The authorized read-only preflight returned exit 0. It checked all 52,522 tracked
snapshot objects, all 49 preexisting dirty paths, exactly seven generated changes
(four settings and the three metas), unchanged normal Editor preferences/saves,
and the three prior build artifacts. The complete five-entry standalone save
tree was recorded without editing gameplay contents. The only added preference
key was `CavesOfOoo.LastGameID`. Root had separately held the two owned coverage
JSON changes before this check.

Root subsequently applied restoration. The retained
`packaged-camp-props-cloth-restoration.json` records success at
`2026-10-28T04:09:49.419380+00:00`, SHA256
`60ba7df6573063ec361492b8ab160a0ef7392ec8ed113349eba16cc8703eaa51`.
It reports seven generated files backed up/restored, the entire standalone save
tree archived, original standalone absence/preferences restored, and normal
Editor state, all tracked snapshot objects and prior builds preserved. These
equality claims describe that restoration boundary, before root's authorized
coverage-record reapplication and subsequent delivery documentation. No cleanup,
Linux execution, Steam Deck test, performance improvement or area-polish claim
follows from these checks.
