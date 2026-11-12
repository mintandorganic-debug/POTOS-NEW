using System.Collections.Generic;

public interface IAttackSelectionContext
{
    bool GameStarted { get; }

    GameMaster.GamePhase Phase { get; }

    int CurrentPlayerIndex { get; }

    IReadOnlyList<Player> Players { get; }

    bool IsCurrentPlayer(
        Player player);

    void SetPhase(
        GameMaster.GamePhase phase);
}
