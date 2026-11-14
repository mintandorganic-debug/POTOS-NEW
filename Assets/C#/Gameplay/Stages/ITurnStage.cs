using System;

public interface ITurnStage
{
    TurnStage Id { get; }

    bool CanSkip { get; }

    void Begin(
        Player player,
        Action onFinished);
}
