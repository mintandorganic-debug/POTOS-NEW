using Mirror;

public class AttackCommandHandler
{
    private readonly IAttackCommandContext context;
    private readonly AttackState state;

    public AttackCommandHandler(
        IAttackCommandContext context,
        AttackState state)
    {
        this.context = context;
        this.state = state;

        GameLog.Write(
            "AttackCommandHandler",
            "Created");
    }

    public void StartRequested(
        NetworkIdentity cardIdentity,
        NetworkConnectionToClient sender)
    {
        if (cardIdentity == null ||
            sender == null)
        {
            GameLog.Write(
                "AttackCommandHandler",
                "CmdStartSequence rejected: cardIdentity or sender is null");

            return;
        }

        if (!cardIdentity.TryGetComponent(
                out Card card))
        {
            GameLog.Write(
                "AttackCommandHandler",
                $"CmdStartSequence rejected: no Card component on netId={cardIdentity.netId}");

            return;
        }

        if (!card.IsAvailable ||
            card.IsInRoulette)
        {
            GameLog.Write(
                "AttackCommandHandler",
                $"CmdStartSequence rejected: card unavailable card={(card.Data != null ? card.Data.name : "NULL")} isAvailable={card.IsAvailable} isInRoulette={card.IsInRoulette}");

            return;
        }

        Player cardOwner =
            AttackValidator.FindPlayerByConnection(sender);

        if (cardOwner == null)
        {
            GameLog.Write(
                "AttackCommandHandler",
                $"CmdStartSequence rejected: no player for connection {sender.connectionId}");

            return;
        }

        if (!AttackValidator.IsCardOwnedByPlayer(
                card,
                cardOwner))
        {
            GameLog.Write(
                "AttackCommandHandler",
                $"CmdStartSequence rejected: card not owned by player card={(card.Data != null ? card.Data.name : "NULL")} player={cardOwner.netId}");

            return;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
        {
            GameLog.Write(
                "AttackCommandHandler",
                "CmdStartSequence rejected: GameMaster.Instance is null");

            return;
        }

        // -----------------------------------------------------
        // DEFENSE CARD
        // -----------------------------------------------------

        if (card.Data != null &&
            card.Data.CardClass == Card.CardClass.Defence)
        {
            GameLog.Write(
                "AttackCommandHandler",
                $"CmdStartSequence defense card card={card.Data.name} owner={cardOwner.netId}");

            if (!state.IsActive ||
                state.Attacker == null ||
                state.Target == null)
            {
                GameLog.Write(
                    "AttackCommandHandler",
                    $"CmdStartSequence defense rejected: sequence not ready isActive={state.IsActive} attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")} target={(state.Target != null ? state.Target.name : "NULL")}");

                return;
            }

            Player targetPlayer =
                state.Target.GetComponentInParent<Player>();

            if (targetPlayer == null ||
                targetPlayer.netIdentity.connectionToClient != sender)
            {
                GameLog.Write(
                    "AttackCommandHandler",
                    $"CmdStartSequence defense rejected: sender is not the target player targetPlayer={(targetPlayer != null ? targetPlayer.netId.ToString() : "NULL")} senderConn={sender.connectionId}");

                return;
            }

            if (!gameMaster.SelectDefense(
                    card,
                    targetPlayer))
            {
                GameLog.Write(
                    "AttackCommandHandler",
                    $"CmdStartSequence defense rejected: GameMaster.SelectDefense returned false card={card.Data.name} targetPlayer={targetPlayer.netId}");

                return;
            }

            GameLog.Write(
                "AttackCommandHandler",
                $"CmdStartSequence defense accepted card={card.Data.name} targetPlayer={targetPlayer.netId}");

            context.StartDefenseSequence(
                card,
                targetPlayer);

            return;
        }

        // -----------------------------------------------------
        // ATTACK CARD
        // -----------------------------------------------------

        if (!gameMaster.SelectAttack(
                card,
                cardOwner))
        {
            GameLog.Write(
                "AttackCommandHandler",
                $"CmdStartSequence attack rejected: GameMaster.SelectAttack returned false card={(card.Data != null ? card.Data.name : "NULL")} owner={cardOwner.netId}");

            return;
        }

        GameLog.Write(
            "AttackCommandHandler",
            $"CmdStartSequence attack accepted card={(card.Data != null ? card.Data.name : "NULL")} owner={cardOwner.netId}");

        context.StartSequence(
            card,
            cardOwner);
    }

    public void SetTargetRequested(
        NetworkIdentity targetIdentity,
        NetworkConnectionToClient sender)
    {
        if (targetIdentity == null ||
            sender == null)
        {
            GameLog.Write(
                "AttackCommandHandler",
                "CmdSetTarget rejected: targetIdentity or sender is null");

            return;
        }

        if (!state.IsActive ||
            state.Attacker == null ||
            state.Card == null)
        {
            GameLog.Write(
                "AttackCommandHandler",
                $"CmdSetTarget rejected: sequence not ready isActive={state.IsActive} attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")} selectedCard={(state.Card != null && state.Card.Data != null ? state.Card.Data.name : "NULL")}");

            return;
        }

        if (!targetIdentity.TryGetComponent(
                out PlayerInteractable playerTarget))
        {
            GameLog.Write(
                "AttackCommandHandler",
                $"CmdSetTarget rejected: no PlayerInteractable on netId={targetIdentity.netId}");

            return;
        }

        Player targetPlayer =
            playerTarget.GetComponentInParent<Player>();

        if (targetPlayer == null)
        {
            GameLog.Write(
                "AttackCommandHandler",
                "CmdSetTarget rejected: no Player in parents of target");

            return;
        }

        if (targetPlayer == state.Attacker)
        {
            GameLog.Write(
                "AttackCommandHandler",
                $"CmdSetTarget rejected: target is the attacker player={state.Attacker.netId}");

            return;
        }

        if (state.Attacker.netIdentity.connectionToClient != sender)
        {
            GameLog.Write(
                "AttackCommandHandler",
                $"CmdSetTarget rejected: attacker mismatch attacker={state.Attacker.netId} senderConn={sender.connectionId}");

            return;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
        {
            GameLog.Write(
                "AttackCommandHandler",
                "CmdSetTarget rejected: GameMaster.Instance is null");

            return;
        }

        GameLog.Write(
            "AttackCommandHandler",
            $"CmdSetTarget accepted attacker={state.Attacker.netId} targetPlayer={targetPlayer.netId}");

        context.SetTarget(
            playerTarget);
    }

    public void CancelRequested(
        NetworkConnectionToClient sender)
    {
        if (!state.IsActive ||
            state.Attacker == null)
        {
            GameLog.Write(
                "AttackCommandHandler",
                $"CmdCancelSequence rejected: sequence not ready isActive={state.IsActive} attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")}");

            return;
        }

        if (state.Attacker.netIdentity.connectionToClient != sender)
        {
            GameLog.Write(
                "AttackCommandHandler",
                $"CmdCancelSequence rejected: attacker mismatch attacker={state.Attacker.netId} senderConn={sender.connectionId}");

            return;
        }

        GameLog.Write(
            "AttackCommandHandler",
            $"CmdCancelSequence accepted attacker={state.Attacker.netId}");

        context.CancelSequence();
    }
}
