using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Split-screen layout metrics for the gameplay map and persistent sidebar.
    /// </summary>
    public readonly struct GameplayScreenLayout
    {
        public GameplayScreenLayout(
            Rect mapRect,
            Rect hotbarRect,
            Rect sidebarRect,
            float displayAspect,
            float mapAspect,
            float hotbarAspect,
            float sidebarAspect,
            float scale,
            float charWidthWorld,
            float sidebarWorldWidth,
            float hotbarWorldHeight)
        {
            MapRect = mapRect;
            HotbarRect = hotbarRect;
            SidebarRect = sidebarRect;
            DisplayAspect = displayAspect;
            MapAspect = mapAspect;
            HotbarAspect = hotbarAspect;
            SidebarAspect = sidebarAspect;
            Scale = scale;
            CharWidthWorld = charWidthWorld;
            SidebarWorldWidth = sidebarWorldWidth;
            HotbarWorldHeight = hotbarWorldHeight;
        }

        public Rect MapRect { get; }
        public Rect GameplayRect => MapRect;
        public Rect HotbarRect { get; }
        public Rect SidebarRect { get; }
        public float DisplayAspect { get; }
        public float MapAspect { get; }
        public float GameplayAspect => MapAspect;
        public float HotbarAspect { get; }
        public float SidebarAspect { get; }
        public float Scale { get; }
        public float CharWidthWorld { get; }
        public float SidebarWorldWidth { get; }
        public float HotbarWorldHeight { get; }
    }

    /// <summary>
    /// Camera-space text layout metrics for the dedicated sidebar camera.
    /// </summary>
    public readonly struct SidebarCameraMetrics
    {
        public SidebarCameraMetrics(
            float scale,
            float charWidthWorld,
            int startCharX,
            int topTextY,
            int visibleRowCount)
        {
            Scale = scale;
            CharWidthWorld = charWidthWorld;
            StartCharX = startCharX;
            TopTextY = topTextY;
            VisibleRowCount = visibleRowCount;
        }

        public float Scale { get; }
        public float CharWidthWorld { get; }
        public int StartCharX { get; }
        public int TopTextY { get; }
        public int VisibleRowCount { get; }
        public int BottomTextY => TopTextY - (VisibleRowCount - 1);
    }

    public static class GameplayViewportLayout
    {
        public const int DefaultHotbarRows = GameplayHotbarLayout.GridHeight;

        public static GameplayScreenLayout Measure(Camera camera, float referenceZoom, int sidebarWidthChars)
        {
            if (camera == null)
                return default;

            return Measure(GetDisplayAspect(camera), camera.orthographicSize, referenceZoom, sidebarWidthChars, DefaultHotbarRows);
        }

        public static GameplayScreenLayout Measure(Camera camera, float referenceZoom, int sidebarWidthChars, int hotbarRows)
        {
            if (camera == null)
                return default;

            return Measure(GetDisplayAspect(camera), camera.orthographicSize, referenceZoom, sidebarWidthChars, hotbarRows);
        }

        public static GameplayScreenLayout Measure(
            float displayAspect,
            float orthographicSize,
            float referenceZoom,
            int sidebarWidthChars)
        {
            return Measure(displayAspect, orthographicSize, referenceZoom, sidebarWidthChars, DefaultHotbarRows);
        }

        public static GameplayScreenLayout Measure(
            float displayAspect,
            float orthographicSize,
            float referenceZoom,
            int sidebarWidthChars,
            int hotbarRows)
        {
            float clampedAspect = Mathf.Max(0.01f, displayAspect);
            float scale = orthographicSize / Mathf.Max(0.01f, referenceZoom);
            float charWidthWorld = 0.5f * scale;
            float sidebarWorldWidth = Mathf.Max(0, sidebarWidthChars) * charWidthWorld;
            float hotbarWorldHeight = Mathf.Max(0, hotbarRows) * scale;
            float totalWorldWidth = orthographicSize * 2f * clampedAspect;
            float totalWorldHeight = orthographicSize * 2f;
            float sidebarWidthFraction = sidebarWorldWidth <= 0f || totalWorldWidth <= 0f
                ? 0f
                : Mathf.Clamp01(sidebarWorldWidth / totalWorldWidth);
            float leftColumnWidth = Mathf.Clamp01(1f - sidebarWidthFraction);
            float hotbarHeightFraction = hotbarWorldHeight <= 0f || totalWorldHeight <= 0f
                ? 0f
                : Mathf.Clamp(hotbarWorldHeight / totalWorldHeight, 0f, 0.4f);

            Rect hotbarRect = hotbarHeightFraction > 0f && leftColumnWidth > 0f
                ? new Rect(0f, 0f, leftColumnWidth, hotbarHeightFraction)
                : Rect.zero;
            Rect mapRect = new Rect(0f, hotbarRect.height, leftColumnWidth, Mathf.Max(0f, 1f - hotbarRect.height));
            Rect sidebarRect = sidebarWidthFraction > 0f
                ? new Rect(leftColumnWidth, 0f, sidebarWidthFraction, 1f)
                : Rect.zero;

            float mapAspect = MeasureRectAspect(clampedAspect, mapRect);
            float hotbarAspect = MeasureRectAspect(clampedAspect, hotbarRect);
            float sidebarAspect = MeasureRectAspect(clampedAspect, sidebarRect);

            return new GameplayScreenLayout(
                mapRect,
                hotbarRect,
                sidebarRect,
                clampedAspect,
                Mathf.Max(0.01f, mapAspect),
                Mathf.Max(0.01f, hotbarAspect),
                Mathf.Max(0.01f, sidebarAspect),
                scale,
                charWidthWorld,
                sidebarWorldWidth,
                hotbarWorldHeight);
        }

        public static SidebarCameraMetrics MeasureSidebarCamera(Camera camera, float referenceZoom, int sidebarWidthChars)
        {
            if (camera == null)
                return default;

            float scale = camera.orthographicSize / Mathf.Max(0.01f, referenceZoom);
            float halfH = camera.orthographicSize;
            float halfW = halfH * camera.aspect;
            float worldLeft = camera.transform.position.x - halfW;
            float worldTop = camera.transform.position.y + halfH;
            float worldBottom = camera.transform.position.y - halfH;
            float charWidthWorld = 0.5f * scale;
            int startCharX = Mathf.RoundToInt(worldLeft / Mathf.Max(0.001f, charWidthWorld));
            int topTextY = Mathf.FloorToInt((worldTop - 0.5f * scale) / Mathf.Max(0.001f, scale));
            int visibleRowCount = Mathf.Max(1, Mathf.FloorToInt((worldTop - worldBottom) / Mathf.Max(0.001f, scale)));

            return new SidebarCameraMetrics(
                scale,
                charWidthWorld,
                startCharX,
                topTextY,
                visibleRowCount);
        }

        public static float GetDisplayAspect(Camera camera)
        {
            if (camera == null)
                return 16f / 9f;

            Rect rect = camera.rect;
            float rectWidth = Mathf.Max(0.0001f, rect.width);
            float rectHeight = Mathf.Max(0.0001f, rect.height);
            return Mathf.Max(0.01f, camera.aspect * rectHeight / rectWidth);
        }

        private static float MeasureRectAspect(float displayAspect, Rect rect)
        {
            if (rect.width <= 0f || rect.height <= 0f)
                return 0.01f;

            return Mathf.Max(0.01f, displayAspect * rect.width / rect.height);
        }
    }

    internal static class GameplayRenderLayers
    {
        public const int DefaultLayer = 0;
        public const int WorldLayer = 8;
        public const int SidebarLayer = 9;
        public const int PopupOverlayLayer = 10;
        public const int HotbarLayer = 11;

        /// <summary>
        /// Phase 10 — the AI thought-log overlay's dedicated render layer.
        /// Hosted by <c>ThoughtOverlayCamera</c> (orthographic, URP Overlay
        /// render type, transparent clear). Culled by the gameplay camera so
        /// the overlay tilemaps don't leak into the world view.
        /// </summary>
        public const int ThoughtOverlayLayer = 12;

        public static int DefaultMask => 1 << DefaultLayer;
        public static int WorldMask => 1 << WorldLayer;
        public static int SidebarMask => 1 << SidebarLayer;
        public static int PopupOverlayMask => 1 << PopupOverlayLayer;
        public static int HotbarMask => 1 << HotbarLayer;
        public static int ThoughtOverlayMask => 1 << ThoughtOverlayLayer;
        public static int GameplayCameraMask => WorldMask | DefaultMask;

        public static void SetLayerRecursive(GameObject gameObject, int layer)
        {
            if (gameObject == null)
                return;

            gameObject.layer = layer;

            Transform transform = gameObject.transform;
            for (int i = 0; i < transform.childCount; i++)
                SetLayerRecursive(transform.GetChild(i).gameObject, layer);
        }
    }
}
