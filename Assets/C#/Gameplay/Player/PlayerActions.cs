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

    public void TryStealAccessory(
        Player target,
        int index)
    {
        GameLog.Write(
            "PlayerActions",
            $"TryStealAccessory netId={netId} target={(target != null ? target.name : "NULL")} index={index} isLocal={isLocalPlayer}");

        if (!isLocalPlayer)
        {
            GameLog.Write(
                "PlayerActions",
                $"TryStealAccessory skipped: not local player netId={netId}");

            return;
        }

        if (target == null)
        {
            GameLog.Write(
                "PlayerActions",
                $"TryStealAccessory skipped: target is NULL netId={netId} index={index}");

            return;
        }

        GameLog.Write(
            "PlayerActions",
            $"TryStealAccessory sending CmdTryStealAccessory netId={netId} targetPlayer={target.netId} index={index}");

        CmdTryStealAccessory(
            target.netIdentity,
            index);
    }

    [Command]
    private void CmdTryStealAccessory(
        NetworkIdentity targetIdentity,
        int index,
        NetworkConnectionToClient sender = null)
    {
        GameLog.Write(
            "PlayerActions",
            $"CmdTryStealAccessory netId={netId} targetIdentity={(targetIdentity != null ? targetIdentity.netId : 0)} index={index} sender={(sender != null ? sender.connectionId : -1)}");

        if (targetIdentity == null ||
            sender == null)
        {
            GameLog.Write(
                "PlayerActions",
                $"CmdTryStealAccessory rejected: targetIdentity or sender is NULL netId={netId} index={index}");

            return;
        }

        if (!targetIdentity.TryGetComponent<Player>(
                out Player target))
        {
            GameLog.Write(
                "PlayerActions",
                $"CmdTryStealAccessory rejected: target has no Player component targetNetId={targetIdentity.netId} index={index}");

            return;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
        {
            GameLog.Write(
                "PlayerActions",
                $"CmdTryStealAccessory rejected: GameMaster is NULL netId={netId} index={index}");

            return;
        }

        GameLog.Write(
            "PlayerActions",
            $"CmdTryStealAccessory forwarding to GameMaster.TryStealAccesory sender={sender.connectionId} targetPlayer={target.netId} index={index}");

        gameMaster.TryStealAccesory(
            target,
            index,
            sender);
    }
}