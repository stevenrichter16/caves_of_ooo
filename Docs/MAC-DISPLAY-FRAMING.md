# Mac display framing

**Status (2026-10-29):** shipped at the user's request ("ensure the camera is at a proper zoom level and resolution for my mac; it was modified to be good for codex"). The user confirmed the result.

## What was wrong

- **Editor Game view.** It was fixed at 4K UHD (3840×2160) with Low Resolution Aspect Ratios on, showing only a corner of a 4K frame. It was reset to Free Aspect, fitted at 1×, with Retina rendering. This is an editor setting only, not in the repository.
- **Mac player preferences** (`com.DefaultCompany.2D-URP`). They forced a windowed 1280×800 player, the Steam Deck capture size. The four `Screenmanager` overrides were removed, so the project default applies: fullscreen at native resolution. This is a user preference, not in the repository; the original file is backed up in the session scratchpad.
- **Automatic camera framing** (`CameraFollow`). It floored the native pixel scale. On large Retina viewports this drew the 1280×600 zone at 1× inside wide black margins.

## Change

Automatic framing now uses the larger of the floored target scale and the whole-number scale nearest the one at which the zone covers the world viewport on both axes, capped at 4×:

| Screen | Scale |
|---|---|
| Retina Game view, 2317×1191 | 2× |
| Native Mac fullscreen, 3024×1964 | 3× |
| Deck-sized views | 1× (unchanged) |

An explicit PageUp/PageDown zoom still wins, and the 1.2× gameplay zoom setting is unchanged.

## Tests (`FlatPixelZoomInputTests`)

- **RED** (change removed): the two Mac cases and the explicit-override case failed at 1×.
- **GREEN:** 28/28, including the Deck counter-checks at 1× and pixel alignment at the zone corners.
- Also covered by the combined 3,966-case rendering/camera regression in the verification copy.
