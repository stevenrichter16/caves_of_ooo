
using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Real authored content, with native startup/save/pickup and explicit combat API stimuli.
    /// No blueprint copies, injected Loadout values, new art keys or stock deletion.</summary>
    [Scenario(name: "Equipment Content Audit", category: "Items",
        description: "Verify authored humanoid kits, real village/profile producers, natural attacks, exact drops and saves.")]
    public sealed class GameAuditEquipmentContentBench : IScenario
    {
        public string RunId { get; private set; }
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            if (!Application.isPlaying || string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Launch through the isolated editor batch in native PlayMode.");
            RunId = Guid.NewGuid().ToString("N");
            new GameObject("Equipment Content Native Audit").AddComponent<GameAuditEquipmentContentBenchPlayer>().Initialize(ctx, this);
        }
        public void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++;
            Audit.Add((passed ? "PASS " : "FAIL ") + name);
            if (!passed) throw new InvalidOperationException("Equipment content audit failed: " + name);
        }
    }
    /// <summary>Temporary observable veto. Removed before any save; never part of authored content.</summary>
    public sealed class GameAuditContentVetoPart : Part
    {
        public override string Name => nameof(GameAuditContentVetoPart);
        public int Before;
        public override bool HandleEvent(GameEvent e)
        { if (e.ID != "BeforeDismember") return true; Before++; return false; }
    }
}
