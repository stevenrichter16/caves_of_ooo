// Inert stand-ins for the handful of Presentation-layer types the game core
// touches. Each is null/no-op, matching an EditMode run with no scene.
using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Presentation.Effects
{
    public class HitStopController
    {
        public static HitStopController Instance => null;
        public void PunchHeavy() { }
        public void PunchMedium() { }
        public void PunchLight() { }
    }
}

namespace CavesOfOoo
{
    public class GameBootstrap : UnityEngine.Object
    {
        public static event Action<Zone, EntityFactory, Entity, TurnManager> OnAfterBootstrap;
        internal static void CooRunTouch() => OnAfterBootstrap?.Invoke(null, null, null, null);
    }
}
