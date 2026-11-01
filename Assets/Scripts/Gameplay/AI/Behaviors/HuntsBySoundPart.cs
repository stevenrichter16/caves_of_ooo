namespace CavesOfOoo.Core
{
    /// <summary>
    /// P5 B5 — eyeless. Canon (catacomb_village_design.md): the dead-zone "eyeless apex
    /// predators" the catacomb Wardens hunt. This creature perceives by hearing alone:
    /// whatever moved, struck or cast within <see cref="Window"/> ticks and within its
    /// radius (the brain's sight radius) is perceived through darkness and around corners;
    /// whatever stood still is not, even beside it. Stand still and it passes.
    /// It replaces line of sight wherever an AI asks what it can perceive
    /// (<see cref="AIHelpers"/>), so finding, hunting and losing a target all follow it.
    /// </summary>
    public sealed class HuntsBySoundPart : Part, IPerceptionBehavior
    {
        public override string Name => "HuntsBySound";

        public int Window = 1;

        public bool TryPerceive(Entity other, Zone zone, out bool perceived)
        {
            perceived = Noise.Recent(other, Window);
            return true;
        }
    }
}
