using System;

public class SlotStage : ITurnStage
{
    public TurnStage Id =>
        TurnStage.Slot;

    public bool CanSkip =>
        true;

    public void Begin(
        Player player,
        Action onFinished)
    {
        GameLog.Write(
            "SlotStage",
            $"Begin player={(player != null ? player.netId.ToString() : "NULL")}: STAGE 3 (SLOT MACHINE) IS NOT IMPLEMENTED YET, skipping to the next player");

        onFinished();
    }
}
