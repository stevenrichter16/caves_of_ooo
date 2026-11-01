# Art reference shelf

The reference shelf lives **outside the repository**, at `/Users/steven/coo-art-reference/`.
It is **reference only**: nothing in it is copied, traced or recoloured into `Assets/` or
`ArtSource/`. Every game asset is original and editable (a pixel-row Python source under
`ArtSource/Live16x24/<family>/`).

| Folder | What | How it was made |
|---|---|---|
| `qud-atlases/` | Caves of Qud's packed tile atlases (Creatures, Items, Furniture, Terrain, Walls, Tiles, Liquids, and others). | Extracted read-only from the local Steam install's `resources.assets` with UnityPy (`tools/atlases.py`), in a private venv. |
| `project-sheets/` | Contact sheets of the project's own sprites (`Sprites/Environment`, `Sprites/Actors`, `Sprites/Live16x24` small frames), each with a name list. | Built from `Assets/Resources/Sprites`. |

**What the Qud tiles teach.** They are two-tone masks: the game tints a foreground and a
detail colour at draw time. Read them for:
- **silhouette:** what makes a creature or object nameable in one 16×24 cell;
- **pose economy:** one strong gesture, no anatomy noise;
- **negative space:** gaps that separate limbs and handles;
- **the one-detail rule:** a single detail-colour accent that carries the identity.

They say nothing about palette. Palette and shading come from the project's own approved
sprites:
- a near-black outline (30, 32, 28);
- flat fills with one highlight and one shade;
- bottom-aligned figures;
- the shared 16×16 environment format for small props and fauna, and 16×24 bodies for
  actors.

Process and self-prompts: `Docs/GLYPH-TO-PIXEL-REPLACEMENT.md`.
