using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Neutral body of the exact repeatable Greatdew plant. No trigger,
    /// effect, RNG or Render event is evaluated here; callers own disclosure and
    /// document authority. The body does not depict a captured or disabled state.</summary>
    public sealed class FlatPixelGreatdewCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/greatdew";
        readonly Tile body;
        bool disposed;
        public FlatPixelGreatdewCatalog() : this(Resources.Load<Texture2D>(ResourcePath)) { }

        /// <summary>Missing/malformed input stays unavailable. Only the generated
        /// sprite and tile are owned; the original pixels remain borrowed.</summary>
        public FlatPixelGreatdewCatalog(Texture2D texture)
        {
            if (texture == null || texture.width != 16 || texture.height != 24) return;
            var sprite = Sprite.Create(texture, new Rect(0, 0, 16, 24), new Vector2(.5f, .5f),
                16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 greatdew"; sprite.hideFlags = HideFlags.HideAndDontSave;
            body = ScriptableObject.CreateInstance<Tile>(); body.name = sprite.name;
            body.hideFlags = HideFlags.HideAndDontSave; body.sprite = sprite;
            body.flags = TileFlags.None; body.colliderType = Tile.ColliderType.None;
        }
        public static bool HasKnownRole(Entity owner) => owner?.BlueprintName == "Greatdew";
        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || body == null || !HasKnownRole(owner) || !owner.HasTag("Vegetation")
                || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Item")
                || owner.HasTag("Corpse") || owner.HasTag("Terrain") || owner.HasTag("Wall")
                || owner.HasTag("Solid") || owner.GetStat("Hitpoints") != null || ForeignMeaning(owner)) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            var material = Owned<MaterialPart>(owner); var structure = Owned<DestructiblePart>(owner);
            var snare = Owned<GreatdewSnarePart>(owner);
            if (render == null || !render.Visible || render.RenderString != "(" || render.ColorString != "&C"
                || render.RenderLayer != 2 || !string.IsNullOrEmpty(render.BackgroundColor)
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants) || physics == null || physics.Solid
                || physics.Takeable || physics.InInventory != null || physics.Equipped != null
                || material == null || material.MaterialID != "Plant" || structure == null
                || structure.Gone || structure.HP <= 0 || Owned<ExaminablePart>(owner) == null
                || snare == null || !ReferenceEquals(snare, Owned<TriggerOnStepPart>(owner))
                || snare.ConsumeOnTrigger || !string.IsNullOrEmpty(snare.TriggerFaction)
                || snare.GrabChance < 0 || snare.GrabChance > 100 || snare.HoldTurns < 0
                || !float.IsFinite(snare.Corrosion) || snare.Corrosion < 0
                || !SingleCell(owner) || !OptionalOwned<StatusEffectsPart>(owner)) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            tile = body; return true;
        }
        static bool ForeignMeaning(Entity owner)
            => owner.HasPart<BrainPart>() || owner.HasPart<Body>() || owner.HasPart<TraderPart>()
                || owner.HasPart<ConversationPart>() || owner.HasPart<ContainerPart>() || owner.HasPart<LockPart>()
                || owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<HarvestablePart>()
                || owner.HasPart<CultivatedSoilPart>() || owner.HasPart<LiquidPoolPart>() || owner.HasPart<GasPoolPart>()
                || owner.HasPart<TileStateSourcePart>() || owner.HasPart<WellPart>() || owner.HasPart<ForgePart>()
                || owner.HasPart<AlchemyStillPart>() || owner.HasPart<DoorPart>() || owner.HasPart<RepairablePart>()
                || owner.HasPart<ThermalPart>() || owner.HasPart<TrapJammingPart>();
        static bool OptionalOwned<T>(Entity owner) where T : Part => !owner.HasPart<T>() || Owned<T>(owner) != null;
        static T Owned<T>(Entity owner) where T : Part
        {
            T found = null;
            for (int i = 0; i < owner.Parts.Count; i++)
                if (owner.Parts[i] is T part)
                { if (found != null || part.ParentEntity != owner) return null; found = part; }
            return found;
        }
        static bool SingleCell(Entity owner)
        {
            if (!owner.HasPart<SpatialFootprintPart>()) return true;
            var part = Owned<SpatialFootprintPart>(owner);
            return part != null && part.Offsets.Length == 1 && part.Offsets[0] == Vector2Int.zero
                && (owner.SpatialZone == null || owner.SpatialZone.IsFootprintCurrent(owner));
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (body == null) return;
            DestroyOwned(body.sprite); DestroyOwned(body);
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
