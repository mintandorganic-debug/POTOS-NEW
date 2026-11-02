using UnityEngine;

public class TurnController
{
    private readonly ITurnContext context;
    private readonly AttackSelection selection;

    public TurnController(
        ITurnContext context,
        AttackSelection selection)
    {
        this.context = context;
        this.selection = selection;

        GameLog.Write(
            "TurnController",
            "Created");
    }

    public void BeginTurn(
        int playerIndex)
    {
        GameLog.Write(
            "TurnController",
            $"BeginTurn playerIndex={playerIndex} gameStarted={context.GameStarted} players={context.Players.Count} previousPlayerIn={context.CurrentPlayerIndex}");

        if (!context.GameStarted)
        {
            GameLog.Write(
                "TurnController",
                "BeginTurn skipped: game not started");

            return;
        }

        if (playerIndex < 0 ||
            playerIndex >= context.Players.Count)
        {
            GameLog.Write(
                "TurnController",
                $"BeginTurn rejected: invalid player index {playerIndex} players={context.Players.Count}");

            Debug.LogError(
                $"GameMaster: invalid player index {playerIndex}.");

            return;
        }

        context.SetCurrentPlayer(
            playerIndex);

        selection.Clear();

        GameLog.Write(
            "TurnController",
            $"BeginTurn current player set currentPlayerIn={context.CurrentPlayerIndex} player={(context.Players[context.CurrentPlayerIndex] != null ? context.Players[context.CurrentPlayerIndex].netId.ToString() : "NULL")} selectedAttack=NULL selectedTarget=NULL");

        context.SetPhase(
            GameMaster.GamePhase.Attack);

        GameLog.Write(
            "TurnController",
            $"BeginTurn calling OnTurnStart player={(context.Players[context.CurrentPlayerIndex] != null ? context.Players[context.CurrentPlayerIndex].netId.ToString() : "NULL")}");

        context.Players[context.CurrentPlayerIndex].OnTurnStart();
    }

    public void EndTurn()
    {
        GameLog.Write(
            "TurnController",
            $"EndTurn gameStarted={context.GameStarted} currentPlayerIn={context.CurrentPlayerIndex} phase={context.Phase} players={context.Players.Count}");

        if (!context.GameStarted)
        {
            GameLog.Write(
                "TurnController",
                "EndTurn skipped: game not started");

            return;
        }

        if (context.Players.Count == 0)
        {
            GameLog.Write(
                "TurnController",
                "EndTurn skipped: no players");

            return;
        }

        if (context.CurrentPlayerIndex < 0 ||
            context.CurrentPlayerIndex >= context.Players.Count)
        {
            GameLog.Write(
                "TurnController",
                $"EndTurn rejected: invalid currentPlayerIndex {context.CurrentPlayerIndex} players={context.Players.Count}");

            Debug.LogError(
                $"GameMaster: invalid currentPlayerIndex {context.CurrentPlayerIndex}.");

            return;
        }

        selection.Clear();

        GameLog.Write(
            "TurnController",
            $"EndTurn calling OnTurnEnd player={(context.Players[context.CurrentPlayerIndex] != null ? context.Players[context.CurrentPlayerIndex].netId.ToString() : "NULL")} next={(context.CurrentPlayerIndex + 1) % context.Players.Count}");

        context.Players[context.CurrentPlayerIndex].OnTurnEnd();

        BeginTurn(
            (context.CurrentPlayerIndex + 1) %
            context.Players.Count);
    }

    public bool IsCurrentPlayer(
        Player player)
    {
        GameLog.Write(
            "TurnController",
            $"IsCurrentPlayer player={(player != null ? player.netId.ToString() : "NULL")} currentPlayerIn={context.CurrentPlayerIndex} players={context.Players.Count}");

        if (player == null)
        {
            GameLog.Write(
                "TurnController",
                "IsCurrentPlayer false: player is null");

            return false;
        }

        if (context.CurrentPlayerIndex < 0 ||
            context.CurrentPlayerIndex >= context.Players.Count)
        {
            GameLog.Write(
                "TurnController",
                $"IsCurrentPlayer false: invalid currentPlayerIndex {context.CurrentPlayerIndex}");

            return false;
        }

        return context.Players[context.CurrentPlayerIndex] ==
               player;
    }

    public void ClearTurnState()
    {
        GameLog.Write(
            "TurnController",
            "ClearTurnState selectedAttack=NULL selectedTarget=NULL");

        selection.Clear();
    }
}
