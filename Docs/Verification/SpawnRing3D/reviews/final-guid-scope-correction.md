# GUID scan scope correction

The 3,457 count in the first preservation report used `rg --files --hidden Assets -g '*.meta'`, which still honors `.gitignore`. It was **not a complete filesystem count**; the wording “all Assets metadata” was too broad and is corrected here.

The current same-scope count is3,458. The one added path since that earlier review is the deliberate `Assets/Tests/EditMode/Presentation/Rendering/SpawnRing3DProfileWalkTests.cs.meta`. A filesystem scan finds another148 ignored metadata files:78 under `Assets/Screenshots` and70 under `Assets/_Recovery`. They are excluded by `.gitignore:59` and`:69`. Thus3,457 +1 declared test +148 ignored files = **3,606**. The149-count difference must not be described as149 newly generated assets.

The full filesystem scan confirms **3,606 valid unique GUIDs, no collisions or invalid GUIDs**. Independently compared every metadata file against the final native source-before snapshot `Docs/Verification/SpawnRing3D/native-launch-462ccc2113084d22a18058d86e146386/source-before.json`: **all3,606 already existed; zero new paths, zero missing paths, zero changed bytes**. This agrees with root's comparison. No post-native asset creation/churn is inferred.

Full path lists and exact comparison counts are recorded in `/tmp/codex-v3d-final-new-metadata-audit.json`. No repository/Unity changes were made.
