"""Two-color visual exports without changing construction cells or geometry.

Run only after the original material-aware greedy geometry has been produced.
Recoloring before meshing would combine newly identical surfaces and change
quad/triangle topology. Exported voxel runs deliberately retain their original
spans, even when adjacent spans acquire the same color.
"""
from .materials import MATERIAL_INDEX,PALETTE_NAMES


_CLOTH={'cloth_red','cloth_cream','cloth_teal'}
_WOOD={'wood','wood_light'}
_FOLIAGE={'leaf','leaf_light','dry_leaf'}


def _semantic_pair(key,present):
    """Choose existing body/accent IDs and which shade families follow accent."""
    name,separator,suffix=key.rpartition('_')
    family=name if separator and suffix.isdigit() else key
    if family in ('crate','barrel'):
        if 'metal' in present:
            return 'wood','metal',{'metal','rust'}
        return 'wood','wood_light',{'wood_light','cloth_cream'}
    if family in ('tree','palm'):
        return 'wood','leaf',_FOLIAGE
    if family=='bed':
        return 'wood','cloth_red',_CLOTH
    if family in ('crop','reeds','scrub'):
        leaf='leaf' if 'leaf' in present else 'leaf_light'
        accent='dry_leaf' if 'dry_leaf' in present else 'earth'
        return leaf,accent,{'dry_leaf','earth'}
    if family in ('door','gate','workbench'):
        return 'wood','metal',{'metal','rust'}
    if family=='window':
        return 'sandstone','wood',_WOOD
    if family=='well':
        return 'sandstone','water',{'water','water_light'}
    if family=='trough':
        return 'wood','water',{'water','water_light'}
    if family in ('furnace','firepit'):
        return 'ancient_stone','ember',{'ember'}
    if family=='torch':
        return 'wood','ember',{'ember'}
    if family=='shelf':
        return 'wood','cloth_red',present-_WOOD
    if family=='market_stall':
        return 'wood','cloth_cream',present-_WOOD
    if family=='banner':
        return 'wood','cloth_red',_CLOTH
    if family=='irrigation':
        return 'earth','water',{'water','water_light'}
    if family=='sign':
        return 'wood','coal',{'coal'}
    if family in ('rock','ruin'):
        return 'ancient_stone','sandstone',{'sandstone','stone_light','leaf','leaf_light'}
    if family=='npc':
        cloth=next(name for name in ('cloth_teal','cloth_red','cloth_cream') if name in present)
        skin='skin_dark' if 'skin_dark' in present else 'skin'
        return cloth,skin,{'skin','skin_dark'}
    if family=='livestock':
        return 'wood','cloth_red',_CLOTH
    # New higher-color recipes need an explicit semantic decision rather than
    # silently dropping their functional accent by generic frequency voting.
    raise ValueError('Missing native two-color policy for '+key)


def cap_export_materials(row):
    """Remap only the three exported material channels, preserving every span.

    Already one/two-color rows are returned byte-identically. Chosen colors
    must already occur in this recipe; no wooden slat becomes invented metal.
    The input is a fresh JSON-ready export row, never a cached VoxelMesh.
    """
    present_ids=set(row['quadMaterials'])|set(row['triangleMaterials'])|{r[4] for r in row['runs']}
    if len(present_ids)<=2:
        return row
    present={PALETTE_NAMES[i] for i in present_ids}
    body,accent,accent_sources=_semantic_pair(row['sourceAsset'],present)
    if body not in present or accent not in present:
        raise ValueError('Native color policy selects absent material for '+row['sourceAsset'])
    body_id,accent_id=MATERIAL_INDEX[body],MATERIAL_INDEX[accent]
    mapping={i:accent_id if PALETTE_NAMES[i] in accent_sources or i==accent_id else body_id
             for i in present_ids}
    row['quadMaterials']=[mapping[i] for i in row['quadMaterials']]
    row['triangleMaterials']=[mapping[i] for i in row['triangleMaterials']]
    row['runs']=[r[:4]+[mapping[r[4]]] for r in row['runs']]
    return row
