using System;
using CavesOfOoo.Data;
namespace CavesOfOoo.Core
{
    public sealed class MorrowfastBuilder : IZoneBuilder
    {
        public string Name=>"Morrowfast";
        public int Priority=>1000;
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)=>MorrowfastSceneRuntime.Install(zone,factory);
    }
}
