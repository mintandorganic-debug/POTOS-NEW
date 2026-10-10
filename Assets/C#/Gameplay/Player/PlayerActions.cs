using Mirror;
using UnityEngine;

public class PlayerActions : NetworkBehaviour
{
    public static PlayerActions Local { get; private set; }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        Local = this;
    }

    public override void OnStopLocalPlayer()
    {
        if (Local == this)
            Local = null;

        base.OnStopLocalPlayer();
    }

    public void TryStealAccessory(Player target, int index)
    {
        if (!isLocalPlayer)
            return;

        if (target == null)
            return;

        CmdTryStealAccessory(target.netIdentity, index);
    }

    [Command]
    private void CmdTryStealAccessory(
        NetworkIdentity targetIdentity, int index)
    {
        if (targetIdentity == null)
            return;

        if (!targetIdentity.TryGetComponent<Player>(
                out Player target))
        {
            return;
        }

        GameMaster.Instance.TryStealAccesory(target, index);
    }
}