using Unity.Profiling;

namespace CavesOfOoo.Diagnostics
{
    /// <summary>
    /// Central catalog of profiler markers used by the performance investigation harness.
    /// Keep marker definitions here so call sites stay consistent and easy to compare in captures.
    /// </summary>
    public static class PerformanceMarkers
    {
        public static class Bootstrap
        {
            public static readonly ProfilerMarker DoStart = new ProfilerMarker("COO.Bootstrap.DoStart");
            public static readonly ProfilerMarker LoadFactions = new ProfilerMarker("COO.Bootstrap.LoadFactions");
            public static readonly ProfilerMarker LoadMaterialReactions = new ProfilerMarker("COO.Bootstrap.LoadMaterialReactions");
            public static readonly ProfilerMarker InitializeMutations = new ProfilerMarker("COO.Bootstrap.InitializeMutations");
            public static readonly ProfilerMarker LoadBlueprints = new ProfilerMarker("COO.Bootstrap.LoadBlueprints");
            public static readonly ProfilerMarker GenerateZone = new ProfilerMarker("COO.Bootstrap.GenerateZone");
            public static readonly ProfilerMarker SetupPlayer = new ProfilerMarker("COO.Bootstrap.SetupPlayer");
            public static readonly ProfilerMarker SetupTurns = new ProfilerMarker("COO.Bootstrap.SetupTurns");
            public static readonly ProfilerMarker WirePresentation = new ProfilerMarker("COO.Bootstrap.WirePresentation");
        }

        public static class Zone
        {
            public static readonly ProfilerMarker LateUpdate = new ProfilerMarker("COO.ZoneRenderer.LateUpdate");
            public static readonly ProfilerMarker RenderZone = new ProfilerMarker("COO.ZoneRenderer.RenderZone");
            public static readonly ProfilerMarker RenderCell = new ProfilerMarker("COO.ZoneRenderer.RenderCell");
            public static readonly ProfilerMarker ComputeFov = new ProfilerMarker("COO.ZoneRenderer.ComputeFOV");
            public static readonly ProfilerMarker ComputeLightMap = new ProfilerMarker("COO.ZoneRenderer.ComputeLightMap");
            public static readonly ProfilerMarker RenderSidebar = new ProfilerMarker("COO.ZoneRenderer.RenderSidebar");
            public static readonly ProfilerMarker RenderHotbar = new ProfilerMarker("COO.ZoneRenderer.RenderHotbar");
            public static readonly ProfilerMarker UpdateAmbientAnimations = new ProfilerMarker("COO.ZoneRenderer.UpdateAmbientAnimations");
            public static readonly ProfilerMarker MarkDirty = new ProfilerMarker("COO.ZoneRenderer.MarkDirty");
        }

        public static class Fx
        {
            public static readonly ProfilerMarker Update = new ProfilerMarker("COO.AsciiFx.Update");
            public static readonly ProfilerMarker ConsumeRequests = new ProfilerMarker("COO.AsciiFx.ConsumeRequests");
            public static readonly ProfilerMarker UpdateAuras = new ProfilerMarker("COO.AsciiFx.UpdateAuras");
            public static readonly ProfilerMarker UpdateChargeOrbits = new ProfilerMarker("COO.AsciiFx.UpdateChargeOrbits");
            public static readonly ProfilerMarker UpdateRingWaves = new ProfilerMarker("COO.AsciiFx.UpdateRingWaves");
            public static readonly ProfilerMarker UpdateBeams = new ProfilerMarker("COO.AsciiFx.UpdateBeams");
            public static readonly ProfilerMarker UpdateChainArcs = new ProfilerMarker("COO.AsciiFx.UpdateChainArcs");
            public static readonly ProfilerMarker UpdateColumnRises = new ProfilerMarker("COO.AsciiFx.UpdateColumnRises");
            public static readonly ProfilerMarker UpdateProjectiles = new ProfilerMarker("COO.AsciiFx.UpdateProjectiles");
            public static readonly ProfilerMarker UpdateBursts = new ProfilerMarker("COO.AsciiFx.UpdateBursts");
            public static readonly ProfilerMarker UpdateParticles = new ProfilerMarker("COO.AsciiFx.UpdateParticles");
            public static readonly ProfilerMarker UpdateDustMotes = new ProfilerMarker("COO.AsciiFx.UpdateDustMotes");
            public static readonly ProfilerMarker Render = new ProfilerMarker("COO.AsciiFx.Render");
        }

        public static class Ui
        {
            public static readonly ProfilerMarker InventoryRender = new ProfilerMarker("COO.UI.Inventory.Render");
            public static readonly ProfilerMarker TradeRender = new ProfilerMarker("COO.UI.Trade.Render");
            public static readonly ProfilerMarker DialogueRender = new ProfilerMarker("COO.UI.Dialogue.Render");
            public static readonly ProfilerMarker SidebarRender = new ProfilerMarker("COO.UI.Sidebar.Render");
            public static readonly ProfilerMarker HotbarRender = new ProfilerMarker("COO.UI.Hotbar.Render");
        }

        public static class Input
        {
            public static readonly ProfilerMarker Update = new ProfilerMarker("COO.Input.Update");
        }

        public static class Turns
        {
            public static readonly ProfilerMarker Tick = new ProfilerMarker("COO.Turns.Tick");
            public static readonly ProfilerMarker ProcessUntilPlayerTurn = new ProfilerMarker("COO.Turns.ProcessUntilPlayerTurn");
            public static readonly ProfilerMarker EndTurn = new ProfilerMarker("COO.Turns.EndTurn");
            public static readonly ProfilerMarker AiTakeTurn = new ProfilerMarker("COO.Turns.AI.TakeTurn");
        }

        public static class Combat
        {
            public static readonly ProfilerMarker PerformMeleeAttack = new ProfilerMarker("COO.Combat.PerformMeleeAttack");
            public static readonly ProfilerMarker ApplyDamage = new ProfilerMarker("COO.Combat.ApplyDamage");
        }
    }
}
