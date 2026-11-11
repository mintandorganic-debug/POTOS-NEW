using System;

public class CardAttackStage : ITurnStage
{
    public TurnStage Id =>
        TurnStage.CardAttack;

    public bool CanSkip =>
        true;

    public void Begin(
        Player player,
        Action onFinished)
    {
        GameLog.Write(
            "CardAttackStage",
            $"Begin player={(player != null ? player.netId.ToString() : "NULL")}: attack and defence flow runs in AttackSequence, the stage ends when it calls EndTurn");
    }
}
