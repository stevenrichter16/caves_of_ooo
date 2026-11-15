**Delivery update:** Published in source5e14712bd and current Mac/Linux Batch5E147. Actual Mac reader, drinking, movement, travel, save and fresh reload passed; Linux remains unexecuted. See Verification/GraphicsExecution/Batch5E147/README.md. Earlier pending-delivery statements below are historical.

# Wellmeet main well lining

The main well at 40,12 now uses the approved cracked or sealed lining artwork instead of a glyph. Its existing four stage colors, contamination flash and purification fade remain visible. The adapter is restricted to the current Wellmeet settlement and matching owner/site state. Readers do not create settlement records or fire Render events. Ordinary wells retain their existing rules.

Fouled and temporarily purified wells use the cracked frame: purification does not repair the filtration ring. Stable and caretaker stages use the sealed frame. Water stock, repair mechanics, placement, ground markers, population and shadows are unchanged.

## Validation

The missing body failed all four native RED cases. Independent review findings were reproduced and corrected. The final Wellmeet class passed 69/69; the earlier combined presentation run passed 93/93, and existing settlement/well regression passed 12/12. These runs overlap and are not additive. One initial paint failure came from the fixture activating another registry; the fixture was corrected without a production workaround.

Actual play used seed 224401440 and the earned center checkpoint at turn 5040, 39,11, 40 HP. The matched comparison changed 204 world pixels, with identical sidebar and hotbar. The full reader showed the native rim. Drawing water succeeded; the well remained solid during a walk around it. Ordinary travel 8.16 → 9.16 → 8.16 and F5/fresh Continue at turn 5170, 39,11, 40 HP passed with F12 off. Only the fouled stage was actually played; other stages are native-test evidence.

Independent source and visual review found no remaining P1/P2. Reload changed 1,254 world pixels elsewhere by at most 5 RGB levels; the well neighborhood and hotbar were unchanged. The cause remains unproven. The body is small and dark, and the black-backed ground markers remain a separate courtyard gap.

## Limits and continuation

The 182.250-second after-only Editor profile recorded renderer p95 0.075875 ms, renderer maximum 193.8755 ms, and an unexplained Main Thread maximum of 19,984.011875 ms. It includes Continue, captures and tool overhead, with no raw frame attribution or packaged performance claim. R11 remains open.

All 80 unrelated tracked changes, normal gameplay state and approved art were preserved. Unity is stopped with a null audit save root and clean scene. No cleanup occurred.

Current Mac/Linux builds at 1b4368a44 exclude the Wellmeet grade and lining. Batch both after publication, then take a fresh actual baseline in First Tent at 5.17 via Left3/Down1 from the earned Wellmeet save. Full-area polish and V0–V7 remain open. Hourly task ACTIVE; last report 2026-11-15T10:19:14Z. Steam Deck remains deferred.

Evidence: `Docs/Verification/GraphicsExecution/WellmeetMainWell/receipt.json`.
