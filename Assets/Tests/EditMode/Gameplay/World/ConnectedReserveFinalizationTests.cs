using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedReserveFinalizationTests
    {
        sealed class ObservedManager : OverworldZoneManager
        {
            readonly bool inject;
            internal bool Observed;
            internal ObservedManager(EntityFactory factory, bool inject) : base(factory, 64)
            {
                this.inject = inject;
                typeof(OverworldZoneManager).GetProperty("Exploration", BindingFlags.Instance | BindingFlags.Public)
                    .SetValue(this, SpreadExplorationPlan.Create(this));
            }
            protected override void OnZoneGenerated(Zone zone, string id)
            {
                base.OnZoneGenerated(zone, id);
                if (id != LocalGatheringClaimPart.ReserveZoneID) return;
                var claim = zone.GetReadOnlyEntities().Select(e => e.GetPart<LocalGatheringClaimPart>()).SingleOrDefault(p => p != null);
                Assert.NotNull(claim, "The callback must observe an actually bound new reserve.");
                Observed = true;
                if (inject)
                    claim.Permissions.Add(new ReserveAccessRecord
                    {
                        Player = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "UnexpectedGrantedPlayer" },
                        State = ReserveAccessState.Granted
                    });
            }
        }

        [TestCase(false)][TestCase(true)]
        public void FinalAdmissionPinsEmptyPermissionMembersRatherThanOnlyListIdentity(bool injectedGrant)
        {
            using (var content = new HaulingContentScope())
            {
                content.Seed(64);
                var manager = new ObservedManager(content.Factory, injectedGrant);
                var zone = manager.GetZone(LocalGatheringClaimPart.ReserveZoneID);
                Assert.True(manager.Observed);
                if (injectedGrant)
                {
                    Assert.Null(zone, "A late callback must not mint initial access through a mutable saved collection.");
                    Assert.False(manager.CachedZones.ContainsKey(LocalGatheringClaimPart.ReserveZoneID));
                    Assert.AreEqual(0, manager.Exploration.DispositionFor(LocalGatheringClaimPart.ReserveZoneID));
                }
                else
                {
                    Assert.NotNull(zone);
                    Assert.AreEqual(2, manager.Exploration.DispositionFor(LocalGatheringClaimPart.ReserveZoneID));
                }
            }
        }
    }
}
