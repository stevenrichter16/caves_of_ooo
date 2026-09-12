"""Small opaque palette shared by every generated voxel asset.

The module is importable without Blender. ``make_palette`` is the only bpy
boundary. Material indices are stable; ownership is explicit so a generator
can remove its own datablocks without touching manually authored materials.
"""

PALETTE_HEX = {
    "sandstone": "9C8968",
    "earth": "766044",
    "dark_floor": "554737",
    "wood": "67472C",
    "wood_light": "9B7545",
    "metal": "464B47",
    "rust": "9F5033",
    "water": "195A61",
    "leaf": "3D642C",
    "leaf_light": "79953A",
    "dry_leaf": "AD963D",
    "sand": "BDA475",
    "ancient_stone": "697066",
    "cloth_red": "953E2F",
    "cloth_cream": "D2BE89",
    "skin": "B98056",
    "ember": "FFB83F",
    "stone_light": "B7A481",
    "cloth_teal": "367C7B",
    "skin_dark": "6D493B",
    "water_light": "357B79",
    "coal": "272A27",
    "worn_ground": "A18B62",
    "oasis_soil": "657044",
    "sand_shade": "AD966B",
    "field_soil": "68533B",
}
PALETTE_NAMES = tuple(PALETTE_HEX)
MATERIAL_INDEX = {name: index for index, name in enumerate(PALETTE_NAMES)}


def _linear_channel(value):
    value /= 255.0
    return value / 12.92 if value <= .04045 else ((value + .055) / 1.055) ** 2.4


def make_palette(owner_token):
    """Create/update only materials carrying this exact owner's palette keys.

    Returned order is ``PALETTE_NAMES``. Colors are authored in sRGB and
    converted to linear values for Blender. No texture, alpha, bevel, or noisy
    shader is required. The ember surface is emissive; actual lights are a
    separate placement decision made by the scene realization layer.
    """
    import bpy
    if not isinstance(owner_token, str) or not owner_token.strip():
        raise ValueError("A nonempty owner token is required")
    existing = {mat.get("coo_voxel_key"): mat for mat in bpy.data.materials
                if mat.get("coo_voxel_owner") == owner_token}
    result = []
    for key, value in PALETTE_HEX.items():
        mat = existing.get(key)
        if mat is None:
            mat = bpy.data.materials.new(f"VX_{owner_token}_{key}")
        mat["coo_voxel_owner"] = owner_token
        mat["coo_voxel_key"] = key
        mat.use_nodes = True
        rgba = tuple(_linear_channel(int(value[i:i+2], 16)) for i in (0, 2, 4)) + (1.0,)
        mat.diffuse_color = rgba
        nodes = mat.node_tree.nodes
        nodes.clear()
        output = nodes.new("ShaderNodeOutputMaterial")
        surface = nodes.new("ShaderNodeBsdfPrincipled")
        surface.inputs["Base Color"].default_value = rgba
        surface.inputs["Roughness"].default_value = .28 if key.startswith("water") else .85
        surface.inputs["Metallic"].default_value = .5 if key == "metal" else .0
        if key == "ember":
            surface.inputs["Emission Color"].default_value = rgba
            surface.inputs["Emission Strength"].default_value = 2.3
        mat.node_tree.links.new(surface.outputs["BSDF"], output.inputs["Surface"])
        result.append(mat)
    return result
