using System.Collections.Generic;

public class TurnStageSequence
{
    private readonly ITurnStageContext context;
    private readonly IReadOnlyList<ITurnStage> stages;

    private Player player;
    private int index = -1;
    private bool active;

    public TurnStageSequence(
        ITurnStageContext context,
        IReadOnlyList<ITurnStage> stages)
    {
        this.context = context;
        this.stages = stages;

        GameLog.Write(
            "TurnStageSequence",
            $"Created stages={stages.Count}");
    }

    public bool IsActive =>
        active;

    public void Begin(
        Player turnPlayer)
    {
        GameLog.Write(
            "TurnStageSequence",
            $"Begin player={(turnPlayer != null ? turnPlayer.netId.ToString() : "NULL")}");

        player = turnPlayer;
        index = -1;
        active = true;

        Next();
    }

    public bool TrySkipCurrent()
    {
        if (!active ||
            index < 0 ||
            index >= stages.Count)
        {
            GameLog.Write(
                "TurnStageSequence",
                "TrySkipCurrent ignored: no active stage");

            return false;
        }

        ITurnStage current =
            stages[index];

        if (!current.CanSkip)
        {
            GameLog.Write(
                "TurnStageSequence",
                $"TrySkipCurrent rejected: stage {current.Id} cannot be skipped");

            return false;
        }

        GameLog.Write(
            "TurnStageSequence",
            $"TrySkipCurrent skipping stage {current.Id}");

        Next();

        return true;
    }

    private void Next()
    {
        index++;

        if (index >= stages.Count)
        {
            GameLog.Write(
                "TurnStageSequence",
                "Next: all stages done, advancing turn");

            active = false;
            index = -1;

            context.AdvanceTurn();

            return;
        }

        ITurnStage stage =
            stages[index];

        GameLog.Write(
            "TurnStageSequence",
            $"Next: starting stage {index + 1}/{stages.Count} {stage.Id}");

        context.SetStage(
            stage.Id);

        stage.Begin(
            player,
            OnStageFinished(
                index));
    }

    private System.Action OnStageFinished(
        int stageIndex)
    {
        return () =>
        {
            if (!active ||
                index != stageIndex)
            {
                GameLog.Write(
                    "TurnStageSequence",
                    $"OnStageFinished ignored: stale callback stageIndex={stageIndex} index={index}");

                return;
            }

            GameLog.Write(
                "TurnStageSequence",
                $"OnStageFinished stage={stages[stageIndex].Id}");

            Next();
        };
    }
}
