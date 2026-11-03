using System.Linq;

public class AttackSelection
{
    private readonly IAttackSelectionContext context;

    private Card selectedAttack;
    private Player selectedTarget;

    public AttackSelection(
        IAttackSelectionContext context)
    {
        this.context = context;

        GameLog.Write(
            "AttackSelection",
            "Created");
    }

    public Card Attack =>
        selectedAttack;

    public Player Target =>
        selectedTarget;

    public void Clear()
    {
        GameLog.Write(
            "AttackSelection",
            "Clear selectedAttack=NULL selectedTarget=NULL");

        selectedAttack = null;
        selectedTarget = null;
    }

    public bool SelectAttack(
        Card card,
        Player player)
    {
        GameLog.Write(
            "AttackSelection",
            $"SelectAttack card={(card != null && card.Data != null ? card.Data.name : "NULL")} player={(player != null ? player.netId.ToString() : "NULL")} phase={context.Phase}");

        if (!context.GameStarted)
        {
            GameLog.Write(
                "AttackSelection",
                "SelectAttack rejected: game not started");

            return false;
        }

        if (context.Phase !=
            GameMaster.GamePhase.Attack)
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectAttack rejected: wrong phase phase={context.Phase}");

            return false;
        }

        if (card == null ||
            player == null)
        {
            GameLog.Write(
                "AttackSelection",
                "SelectAttack rejected: card or player is null");

            return false;
        }

        if (!context.IsCurrentPlayer(player))
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectAttack rejected: not current player player={player.netId} currentPlayerIn={context.CurrentPlayerIndex}");

            return false;
        }

        if (!card.IsAvailable ||
            card.IsInRoulette)
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectAttack rejected: card unavailable isAvailable={card.IsAvailable} isInRoulette={card.IsInRoulette}");

            return false;
        }

        if (card.OwnerNetId !=
            player.netId)
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectAttack rejected: card owner mismatch cardOwner={card.OwnerNetId} player={player.netId}");

            return false;
        }

        if (card.Data == null)
        {
            GameLog.Write(
                "AttackSelection",
                "SelectAttack rejected: card has no Data");

            return false;
        }

        if (card.Data.CardClass !=
                Card.CardClass.AttackLight &&
            card.Data.CardClass !=
                Card.CardClass.AttackSteal)
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectAttack rejected: not an attack card class={card.Data.CardClass}");

            return false;
        }

        selectedAttack = card;

        GameLog.Write(
            "AttackSelection",
            $"SelectAttack accepted selectedAttack={card.Data.name} class={card.Data.CardClass} player={player.netId}");

        context.SetPhase(
            GameMaster.GamePhase.SelectTarget);

        return true;
    }

    public bool SelectTarget(
        Player player)
    {
        GameLog.Write(
            "AttackSelection",
            $"SelectTarget player={(player != null ? player.netId.ToString() : "NULL")} phase={context.Phase}");

        if (!context.GameStarted)
        {
            GameLog.Write(
                "AttackSelection",
                "SelectTarget rejected: game not started");

            return false;
        }

        if (context.Phase !=
            GameMaster.GamePhase.SelectTarget)
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectTarget rejected: wrong phase phase={context.Phase}");

            return false;
        }

        if (selectedAttack == null ||
            player == null)
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectTarget rejected: selectedAttack or player is null selectedAttackNull={selectedAttack == null} playerNull={player == null}");

            return false;
        }

        if (context.CurrentPlayerIndex < 0 ||
            context.CurrentPlayerIndex >= context.Players.Count)
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectTarget rejected: invalid currentPlayerIndex {context.CurrentPlayerIndex} players={context.Players.Count}");

            return false;
        }

        Player attacker =
            context.Players[context.CurrentPlayerIndex];

        if (player == attacker)
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectTarget rejected: target is the attacker player={player.netId}");

            return false;
        }

        if (!context.Players.Contains(player))
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectTarget rejected: player is not in the game player={player.netId}");

            return false;
        }

        selectedTarget = player;

        GameLog.Write(
            "AttackSelection",
            $"SelectTarget accepted selectedTarget={player.netId} attacker={(attacker != null ? attacker.netId.ToString() : "NULL")}");

        context.SetPhase(
            GameMaster.GamePhase.Defense);

        return true;
    }

    public bool SelectDefense(
        Card card,
        Player player)
    {
        GameLog.Write(
            "AttackSelection",
            $"SelectDefense card={(card != null && card.Data != null ? card.Data.name : "NULL")} player={(player != null ? player.netId.ToString() : "NULL")} phase={context.Phase}");

        if (!context.GameStarted)
        {
            GameLog.Write(
                "AttackSelection",
                "SelectDefense rejected: game not started");

            return false;
        }

        if (context.Phase !=
            GameMaster.GamePhase.Defense)
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectDefense rejected: wrong phase phase={context.Phase}");

            return false;
        }

        if (selectedAttack == null ||
            selectedTarget == null)
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectDefense rejected: selectedAttack or selectedTarget is null selectedAttackNull={selectedAttack == null} selectedTargetNull={selectedTarget == null}");

            return false;
        }

        if (player == null ||
            player != selectedTarget)
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectDefense rejected: player is not the selected target player={(player != null ? player.netId.ToString() : "NULL")} selectedTarget={selectedTarget.netId}");

            return false;
        }

        if (card == null)
        {
            GameLog.Write(
                "AttackSelection",
                "SelectDefense rejected: card is null");

            return false;
        }

        if (card.OwnerNetId !=
            player.netId)
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectDefense rejected: card owner mismatch cardOwner={card.OwnerNetId} player={player.netId}");

            return false;
        }

        if (card.IsInRoulette ||
            !card.IsAvailable)
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectDefense rejected: card unavailable isAvailable={card.IsAvailable} isInRoulette={card.IsInRoulette}");

            return false;
        }

        if (card.Data == null ||
            card.Data.CardClass !=
                Card.CardClass.Defence)
        {
            GameLog.Write(
                "AttackSelection",
                $"SelectDefense rejected: not a defence card dataNull={card.Data == null}");

            return false;
        }

        GameLog.Write(
            "AttackSelection",
            $"SelectDefense accepted card={card.Data.name} player={player.netId}");

        return true;
    }

    public bool SkipDefense(
        Player player)
    {
        GameLog.Write(
            "AttackSelection",
            $"SkipDefense player={(player != null ? player.netId.ToString() : "NULL")} phase={context.Phase}");

        if (!context.GameStarted)
        {
            GameLog.Write(
                "AttackSelection",
                "SkipDefense rejected: game not started");

            return false;
        }

        if (context.Phase !=
            GameMaster.GamePhase.Defense)
        {
            GameLog.Write(
                "AttackSelection",
                $"SkipDefense rejected: wrong phase phase={context.Phase}");

            return false;
        }

        if (selectedTarget == null ||
            player == null)
        {
            GameLog.Write(
                "AttackSelection",
                $"SkipDefense rejected: selectedTarget or player is null selectedTargetNull={selectedTarget == null} playerNull={player == null}");

            return false;
        }

        if (player != selectedTarget)
        {
            GameLog.Write(
                "AttackSelection",
                $"SkipDefense rejected: player is not the selected target player={player.netId} selectedTarget={selectedTarget.netId}");

            return false;
        }

        GameLog.Write(
            "AttackSelection",
            $"SkipDefense accepted player={player.netId}");

        return true;
    }
}
