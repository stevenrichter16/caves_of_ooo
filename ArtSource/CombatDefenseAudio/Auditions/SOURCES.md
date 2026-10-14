# Audio sources and credits

License/source pages checked on 2026-10-13. Original downloads are retained in `sources/`; exact URLs, sizes and SHA256 hashes are in `sources/downloads.json`.

## Original synthesis

Parry 1–2 and Guard 1/3 are wholly original deterministic procedural synthesis written for this project. All other candidates include original synthesis layers. No external composition or third-party recording enters those original layers. No claim of exclusive rights is made for algorithmic output.

## Recorded metal contact

- **Machete vs. Spatula**, by **CYBERDEViL**.
- Source and CC0 declaration: https://opengameart.org/content/machete-vs-spatula
- License: CC0 1.0, https://creativecommons.org/publicdomain/zero/1.0/
- The author identifies these as raw spatula-on-machete recordings.
- `machete_vs_spatula_2.wav` is used in Parry 3; `machete_vs_spatula_1.wav` in Parry 4.
- Changes: mono 48 kHz decode, leading silence trim, bounded excerpts, 5500/3800 Hz low-pass filters, original resonant layers, edge fades, explicit peak headroom, WAV and MP3 export. The supplied download files remain unmodified.

## Recorded soft contact

- **10 Impact/Shield Blocks**, by **StarNinjas**.
- Source and CC0 declaration: https://opengameart.org/content/10-impactshield-blocks
- Author profile: https://opengameart.org/users/starninjas
- License: CC0 1.0, https://creativecommons.org/publicdomain/zero/1.0/
- The author describes cardboard impact recordings. These are not recordings of leather or actual shields; the material names in audition options describe the designed effect.
- `impact.3.ogg` from `impact_-_starninjas.zip` is used in Guard 2; `impact.6.ogg` in Guard 4.
- Changes: normalization before processing (the decoded source can exceed 0 dBFS), leading silence trim, mono 48 kHz, 2300 Hz low-pass filtering, original body/metal layers, edge fades, explicit peak headroom, WAV and MP3 export. The source ZIP remains unmodified.

Credits are retained although CC0 does not require attribution. Parry 3 and Guard 4 were selected by the user on 2026-10-13. Their unchanged WAV masters now have runtime catalog bindings; the other candidates remain auditions. Runtime credits are also preserved in Assets/Resources/Audio/World/DEFENSE-CREDITS.txt.
