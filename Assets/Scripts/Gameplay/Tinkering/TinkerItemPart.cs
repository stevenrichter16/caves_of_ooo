namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item metadata used by tinkering systems.
    /// This mirrors the Qud pattern where item parts carry build/disassembly metadata.
    /// </summary>
    public class TinkerItemPart : Part
    {
        public override string Name => "TinkerItem";

        public bool CanDisassemble = true;
        public bool CanBuild = true;

        public int BuildTier = 0;
        public int NumberMade = 1;

        public string Ingredient = "";
        public string SubstituteBlueprint = "";
        public string RepairCost = "";
        public string BuildCost = "";
    }
}
