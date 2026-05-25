using CavesOfOoo.Core;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// PLAYABLE QUEST showcase — "Cinnamon Bun's Favor" (Docs/QUEST-PLAYABLE-CINNAMONBUN.md).
    /// A complete, player-exercisable quest that wires the Q5 world-object
    /// Parts into a live NPC-driven loop:
    ///
    ///   1. Talk to Cinnamon Bun (press 'c' toward him) → "[Accept]" runs the
    ///      conversation StartQuest action → quest begins (stage "errands").
    ///   2. Walk east. Pick up the scorched keepsake → its CompleteObjectiveOnTaken
    ///      Part finishes "recover_keepsake" the instant you take it (Q5.2).
    ///   3. Kill the soot gremlin → its FinishObjectiveWhenSlain Part finishes
    ///      "drive_off_gremlin" (Q5.1). Both required done → stage → "report".
    ///   4. Return to Cinnamon Bun → "[Report]" is now available → CompleteQuest
    ///      + 250 XP + 100 drams, and the Q7 accomplishment is logged.
    ///
    /// Content (real, auto-loaded — NOT inlined here):
    ///   - Quest: Resources/Content/Data/Storylets/CinnamonBunFavor.json
    ///   - Dialogue: Resources/Content/Conversations/CinnamonBun_Quest.json
    /// This scenario only PLACES the actors (NPC + keepsake + gremlin) in the
    /// player's zone and wires the Q5 Parts onto the spawned entities.
    ///
    /// Sentinel-guard pattern (see the quest JSON): the objectives + the
    /// "report" stage carry a never-set <c>IfFact:cbq_external:&gt;=:1</c>
    /// guard so the tick dispatch does NOT auto-finish them — the Q5 Parts
    /// (for objectives) and the dialogue CompleteQuest (for the report stage)
    /// are the SOLE completion mechanisms.
    ///
    /// Observable via diag: quest/QuestStarted, quest/ObjectiveFinished (x2),
    /// quest/StageAdvanced, quest/Completed, quest/Accomplishment.
    /// </summary>
    [Scenario(
        name: "Playable Quest — Cinnamon Bun's Favor",
        category: "Quest",
        description: "A complete player-driven quest: NPC gives it (dialogue StartQuest), pick up the keepsake (CompleteObjectiveOnTaken) + slay the gremlin (FinishObjectiveWhenSlain), report back (dialogue CompleteQuest + rewards).")]
    public class QuestCinnamonBunPlayable : IScenario
    {
        public const string QuestId = "CinnamonBunFavor";
        public const string ObjFetch = "recover_keepsake";
        public const string ObjSlay = "drive_off_gremlin";
        public const string ConversationId = "CinnamonBun_Quest";

        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // Player loadout — enough HP/Strength to fight the gremlin; fresh
            // drams so the reward is visibly observable.
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .SetStat("Ego", 12);
            TradeSystem.SetDrams(ctx.PlayerEntity, 0);

            // Clear a corridor east so spawns aren't blocked.
            for (int dx = 1; dx <= 7; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            // Cinnamon Bun — the quest-giver. Villager already has a
            // ConversationPart; point it at our dialogue tree.
            var cinnamonBun = ctx.Spawn("Villager")
                .WithStatMax("Hitpoints", 40)
                .WithHpAbsolute(40)
                .At(p.x + 2, p.y);
            var convo = cinnamonBun.GetPart<ConversationPart>();
            if (convo != null) convo.ConversationID = ConversationId;
            var bunRender = cinnamonBun.GetPart<RenderPart>();
            if (bunRender != null) { bunRender.DisplayName = "Cinnamon Bun"; bunRender.RenderString = "b"; bunRender.ColorString = "&Y"; }

            // The soot gremlin — slaying it finishes "drive_off_gremlin" (Q5.1).
            var gremlin = ctx.Spawn("Snapjaw")
                .AsPersonalEnemyOf(ctx.PlayerEntity)
                .WithStatMax("Hitpoints", 18)
                .WithHpAbsolute(18)
                .At(p.x + 4, p.y);
            gremlin.AddPart(new FinishObjectiveWhenSlain { Quest = QuestId, Objective = ObjSlay });
            var gremlinRender = gremlin.GetPart<RenderPart>();
            if (gremlinRender != null) { gremlinRender.DisplayName = "soot gremlin"; gremlinRender.ColorString = "&K"; }

            // The scorched keepsake — taking it finishes "recover_keepsake" (Q5.2).
            var keepsake = new Entity { ID = "ScorchedKeepsake", BlueprintName = "ScorchedKeepsake" };
            keepsake.Tags["Item"] = "";
            keepsake.AddPart(new RenderPart { DisplayName = "scorched keepsake", RenderString = "*", ColorString = "&r" });
            keepsake.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            keepsake.AddPart(new CompleteObjectiveOnTaken { Quest = QuestId, Objective = ObjFetch });
            ctx.Zone.AddEntity(keepsake, p.x + 6, p.y);

            // Defensive: StoryletPart.Current is set by GameBootstrap in
            // production; provide a fallback for scenario-runner contexts.
            if (StoryletPart.Current == null)
                StoryletPart.Current = new StoryletPart();

            // The quest itself auto-loads from CinnamonBunFavor.json. Warn (don't
            // crash) if the registry hasn't loaded it — the dialogue StartQuest
            // would then no-op.
            if (StoryletRegistry.FindQuest(QuestId) == null)
                MessageLog.Add($"[Scenario] WARNING: quest '{QuestId}' not in registry — is CinnamonBunFavor.json present?");

            MessageLog.Add("Playable Quest: Cinnamon Bun's Favor.");
            MessageLog.Add("Press 'c' toward Cinnamon Bun (the 'b' just east) to talk and take the quest.");
            MessageLog.Add("Then take the keepsake (*) and slay the soot gremlin, and report back.");
        }
    }
}
