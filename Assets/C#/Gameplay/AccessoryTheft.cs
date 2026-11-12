using Mirror;

public class AccessoryTheft
{
    private readonly IAccessoryTheftContext context;
    private readonly AttackSelection selection;

    public AccessoryTheft(
        IAccessoryTheftContext context,
        AttackSelection selection)
    {
        this.context = context;
        this.selection = selection;

        GameLog.Write(
            "AccessoryTheft",
            "Created");
    }

    public bool BeginStealPhase()
    {
        GameLog.Write(
            "AccessoryTheft",
            $"BeginStealPhase phase={context.Phase} selectedAttack={(selection.Attack != null && selection.Attack.Data != null ? selection.Attack.Data.name : "NULL")} selectedTarget={(selection.Target != null ? selection.Target.netId.ToString() : "NULL")}");

        if (!context.GameStarted)
        {
            GameLog.Write(
                "AccessoryTheft",
                "BeginStealPhase rejected: game not started");

            return false;
        }

        if (context.Phase !=
            GameMaster.GamePhase.Defense)
        {
            GameLog.Write(
                "AccessoryTheft",
                $"BeginStealPhase rejected: wrong phase phase={context.Phase}");

            return false;
        }

        if (selection.Attack == null ||
            selection.Target == null)
        {
            GameLog.Write(
                "AccessoryTheft",
                $"BeginStealPhase rejected: selectedAttack or selectedTarget is null selectedAttackNull={selection.Attack == null} selectedTargetNull={selection.Target == null}");

            return false;
        }

        if (selection.Attack.Data == null ||
            selection.Attack.Data.CardClass !=
                Card.CardClass.AttackSteal)
        {
            GameLog.Write(
                "AccessoryTheft",
                $"BeginStealPhase rejected: selected attack is not a steal card dataNull={selection.Attack.Data == null}");

            return false;
        }

        context.SetPhase(
            GameMaster.GamePhase.Steal);

        GameLog.Write(
            "AccessoryTheft",
            "BeginStealPhase accepted");

        return true;
    }

    public bool StealAccesory(
        Player target,
        int index,
        NetworkConnectionToClient sender)
    {
        GameLog.Write(
            "AccessoryTheft",
            $"StealAccesory target={(target != null ? target.netId.ToString() : "NULL")} index={index} senderConn={(sender != null ? sender.connectionId.ToString() : "NULL")} phase={context.Phase}");

        if (!context.GameStarted)
        {
            GameLog.Write(
                "AccessoryTheft",
                "StealAccesory rejected: game not started");

            return false;
        }

        if (context.Phase !=
            GameMaster.GamePhase.Steal)
        {
            GameLog.Write(
                "AccessoryTheft",
                $"StealAccesory rejected: wrong phase phase={context.Phase}");

            return false;
        }

        if (sender == null)
        {
            GameLog.Write(
                "AccessoryTheft",
                "StealAccesory rejected: sender is null");

            return false;
        }

        if (selection.Target == null ||
            target == null ||
            target != selection.Target)
        {
            GameLog.Write(
                "AccessoryTheft",
                $"StealAccesory rejected: target is not the selected target selectedTarget={(selection.Target != null ? selection.Target.netId.ToString() : "NULL")} target={(target != null ? target.netId.ToString() : "NULL")}");

            return false;
        }

        if (context.CurrentPlayerIndex < 0 ||
            context.CurrentPlayerIndex >= context.Players.Count)
        {
            GameLog.Write(
                "AccessoryTheft",
                $"StealAccesory rejected: invalid currentPlayerIndex {context.CurrentPlayerIndex} players={context.Players.Count}");

            return false;
        }

        Player thief =
            context.Players[context.CurrentPlayerIndex];

        if (thief == null ||
            thief == target)
        {
            GameLog.Write(
                "AccessoryTheft",
                $"StealAccesory rejected: invalid thief thiefNull={thief == null} thiefIsTarget={thief == target}");

            return false;
        }

        if (thief.netIdentity == null ||
            thief.netIdentity.connectionToClient !=
                sender)
        {
            GameLog.Write(
                "AccessoryTheft",
                $"StealAccesory rejected: sender is not the thief thief={thief.netId} senderConn={sender.connectionId}");

            return false;
        }

        if (!target.HasAccessory(index))
        {
            GameLog.Write(
                "AccessoryTheft",
                $"StealAccesory rejected: target has no accessory index={index} target={target.netId}");

            return false;
        }

        if (!target.SetAccesory(
                index,
                false))
        {
            GameLog.Write(
                "AccessoryTheft",
                $"StealAccesory rejected: could not remove accessory from target index={index} target={target.netId}");

            return false;
        }

        if (!thief.SetAccesory(
                index,
                true))
        {
            GameLog.Write(
                "AccessoryTheft",
                $"StealAccesory rejected: could not give accessory to thief index={index} thief={thief.netId}, restoring target accessory");

            target.SetAccesory(
                index,
                true);

            return false;
        }

        GameLog.Write(
            "AccessoryTheft",
            $"StealAccesory accessory stolen index={index} from={target.netId} to={thief.netId}, ending turn");

        context.EndTurn();

        return true;
    }
}
