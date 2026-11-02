using Mirror;
using UnityEngine;

public class PlayerActions : NetworkBehaviour
{
    public static PlayerActions Local { get; private set; }

    public override void OnStartLocalPlayer()
    {
        GameLog.Write(
            "PlayerActions",
            $"OnStartLocalPlayer netId={netId} object={gameObject.name}");

        base.OnStartLocalPlayer();

        Local = this;

        GameLog.Write(
            "PlayerActions",
            $"OnStartLocalPlayer Local set netId={netId}");
    }

    public override void OnStopLocalPlayer()
    {
        GameLog.Write(
            "PlayerActions",
            $"OnStopLocalPlayer netId={netId} isCurrentLocal={Local == this}");

        if (Local == this)
            Local = null;

        base.OnStopLocalPlayer();
    }

    public void StealAccessory(
        Player target,
        int index)
    {
        GameLog.Write(
            "PlayerActions",
            $"StealAccessory netId={netId} target={(target != null ? target.name : "NULL")} index={index} isLocal={isLocalPlayer}");

        if (!isLocalPlayer)
        {
            GameLog.Write(
                "PlayerActions",
                $"StealAccessory skipped: not local player netId={netId}");

            return;
        }

        if (target == null)
        {
            GameLog.Write(
                "PlayerActions",
                $"StealAccessory skipped: target is NULL netId={netId} index={index}");

            return;
        }

        GameLog.Write(
            "PlayerActions",
            $"StealAccessory sending CmdStealAccessory netId={netId} targetPlayer={target.netId} index={index}");

        CmdStealAccessory(
            target.netIdentity,
            index);
    }

    [Command]
    private void CmdStealAccessory(
        NetworkIdentity targetIdentity,
        int index,
        NetworkConnectionToClient sender = null)
    {
        GameLog.Write(
            "PlayerActions",
            $"CmdStealAccessory netId={netId} targetIdentity={(targetIdentity != null ? targetIdentity.netId : 0)} index={index} sender={(sender != null ? sender.connectionId : -1)}");

        if (targetIdentity == null ||
            sender == null)
        {
            GameLog.Write(
                "PlayerActions",
                $"CmdStealAccessory rejected: targetIdentity or sender is NULL netId={netId} index={index}");

            return;
        }

        if (!targetIdentity.TryGetComponent<Player>(
                out Player target))
        {
            GameLog.Write(
                "PlayerActions",
                $"CmdStealAccessory rejected: target has no Player component targetNetId={targetIdentity.netId} index={index}");

            return;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
        {
            GameLog.Write(
                "PlayerActions",
                $"CmdStealAccessory rejected: GameMaster is NULL netId={netId} index={index}");

            return;
        }

        GameLog.Write(
            "PlayerActions",
            $"CmdStealAccessory forwarding to GameMaster.StealAccesory sender={sender.connectionId} targetPlayer={target.netId} index={index}");

        gameMaster.StealAccesory(
            target,
            index,
            sender);
    }
}