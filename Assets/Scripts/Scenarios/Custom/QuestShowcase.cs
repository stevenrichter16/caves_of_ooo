using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Quest system showcase — drives the full QS.1-6 substrate
    /// end-to-end with a minimal 2-stage IronKey-fetch quest.
    ///
    /// Setup:
    ///   - Player at center, HP 200, Strength 24, 0 drams to start
    ///   - Marceline NPC (Villager blueprint) 3E
    ///   - IronKey on the floor 5E
    ///   - StoryletRegistry seeded with the IronKeyShowcaseQuest
    ///     (defined inline rather than reading from JSON, so the
    ///     showcase is self-contained and doesn't depend on a JSON
    ///     file that could be edited out from under it)
    ///
    /// Player flow:
    ///   1. Player walks east, picks up IronKey (when storylet system
    ///      fires the quest's stage-0 trigger IfHaveItem(IronKey)).
    ///      Stage advances 0→1; rewards fire (XP + drams).
    ///   2. Player walks to Marceline, talks (dialogue calls
    ///      AdvanceQuestStage to advance from stage 1 past terminal
    ///      → auto-completes).
    ///
    /// What's observable via diag substrate (the quest channel
    /// added in QS.3):
    ///   quest/Started        when player accepts quest from dialogue
    ///   quest/StageAdvanced  on key pickup (0→1)
    ///   quest/Completed      after Marceline dialogue (terminal)
    ///
    /// Mirrors the established scenario diag fixture pattern from
    /// the 11+ prior showcases — verifiable end-to-end via
    /// QuestShowcaseDiagTests.
    /// </summary>
    [Scenario(
        name: "Quest Showcase",
        category: "Combat",
        description: "Quest system end-to-end demo: 2-stage IronKey fetch quest. Demonstrates QS.2 predicates / QS.3 actions / QS.4 dispatch / QS.5 rewards / QS.6 quest log substrate.")]
    public class QuestShowcase : IScenario
    {
        /// <summary>
        /// The quest ID used by the showcase. Public so tests can
        /// drive StartQuest / AdvanceQuestStage against it.
        /// </summary>
        public const string QuestId = "IronKeyShowcaseQuest";

        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // Player loadout — fresh, no XP, no drams. Lets the
            // QS.5 reward grant be visibly observable.
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .SetStat("Ego", 12);
            CavesOfOoo.Core.TradeSystem.SetDrams(ctx.PlayerEntity, 0);

            // Clear corridor 1..6 so spawns aren't blocked.
            for (int dx = 1; dx <= 6; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            // Spawn Marceline 3 east (Villager blueprint — has
            // ConversationPart we'll use for dialogue once content
            // is wired). Showcase test bypasses dialogue and calls
            // ConversationActions directly.
            ctx.Spawn("Villager")
                .WithStatMax("Hitpoints", 50)
                .WithHpAbsolute(50)
                .At(p.x + 3, p.y);

            // Spawn IronKey on the floor 5 east (free pickup).
            ctx.Spawn("IronKey").At(p.x + 5, p.y);

            // Seed the StoryletRegistry with the quest definition.
            // 2 stages: "find_iron_key" (auto-advances on pickup),
            //           "deliver_to_marceline" (rewards on entry,
            //                                    explicit advance to complete)
            var quest = new StoryletData
            {
                ID = QuestId,
                OneShot = false,
                Tracked = true,
                Triggers = new List<ConversationParam>(),
                Effects = new List<ConversationParam>(),
                Quest = new QuestData
                {
                    Stages = new List<QuestStageData>
                    {
                        new QuestStageData
                        {
                            ID = "find_iron_key",
                            Triggers = new List<ConversationParam>
                            {
                                new ConversationParam
                                {
                                    Key = "IfHaveItem",
                                    Value = "IronKey",
                                },
                            },
                            OnEnter = new List<ConversationParam>(),
                        },
                        new QuestStageData
                        {
                            ID = "deliver_to_marceline",
                            Triggers = new List<ConversationParam>(),
                            OnEnter = new List<ConversationParam>
                            {
                                new ConversationParam
                                {
                                    Key = "AwardXP",
                                    Value = "100",
                                },
                                new ConversationParam
                                {
                                    Key = "GiveDrams",
                                    Value = "50",
                                },
                            },
                        },
                    },
                },
            };
            StoryletRegistry.Register(quest);

            // Ensure StoryletPart.Current exists (in production this
            // is set by GameBootstrap; in test/scenario-runner contexts
            // it might be null so we provide a fallback).
            if (StoryletPart.Current == null)
                StoryletPart.Current = new StoryletPart();

            MessageLog.Add("Quest Showcase: a 2-stage IronKey fetch quest.");
            MessageLog.Add("Pick up the iron key east, then talk to Marceline.");
            MessageLog.Add("Use the diag_query MCP tool to inspect quest/* records.");
        }
    }
}
