# Preferred final contact sheet

Use `starter-spells-unity-contact-sheet-refined.png` for delivery. The initial sheet remains preserved beside it.

Only two cards changed after visual review. Calm now uses the recorded 0.3030895-second frame, where the open loop is clearer around the recipient. Rain uses the recorded 0.3043613-second frame, where the falling strokes are higher above the crop. Both cards use clearly labelled 4× nearest-neighbor detail crops; the other five spells retain their exact 2× views. Rain remains visually understated in the actual town lighting at the normal game scale.

`contact-refinement-verification.json` identifies both original PNG hashes and crop bounds. `refine_contact_sheet.py` reproduces the refinement from the initial sheet and the same native report. Every enlarged pixel is repeated from its source; nothing is recolored or retouched. GIF/MP4 bytes and their original measured timing remain unchanged.

The initial README, media receipt and delivery inventory describe the initial composition and remain intact. `final-delivery-audit.json` verifies both the preserved initial outputs and the preferred refined sheet.
