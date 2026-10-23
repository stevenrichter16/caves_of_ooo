# Clearing 16×24 source pipeline

Original art direction anchored to the user's five approved sprites. The previous 3D world and the blank five-sprite study are preserved. Work record: `Docs/CLEARING-16X24-OVERNIGHT.md`.

1. `Tools/Clearing16x24/greybox.py` creates `clearing-greybox.blend` and `greybox.png` using installed Blender.
2. The built-in imagegen tool reinterpreted that screenshot plus the approved sprite lineup. `concepts/prompt-v1.txt` and `prompt-v2.txt` are the exact prompts. Both generated outputs are stored alongside them. These are art references, not finished game textures.
3. `Tools/Clearing16x24/build_kit.py` rebuilds the selected reference shapes as editable original voxel meshes and renders the exact native sprite canvases. This is concept-guided modeling, not automatic image-to-3D conversion or a claim that Tripo/Meshy was used.
4. `Tools/Clearing16x24/build_unity.cs` is an editor-authoring snippet for Unity MCP `execute_code`. It imports the native PNGs, makes prefabs and a shared sprite atlas, and assembles only `Assets/Scenes/Studies/SeedKeeperClearing16x24.unity`.

Run Blender from the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Clearing16x24/greybox.py
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Clearing16x24/build_kit.py
```

Use the Unity authoring snippet only in Edit mode with the current scene saved. It deliberately refuses Play mode or unsaved scene changes. The scene is a visual art composition. Actual native gameplay integration has separate acceptance gates in the work record; a screenshot of this scene is not evidence of trading, harvesting or movement.
