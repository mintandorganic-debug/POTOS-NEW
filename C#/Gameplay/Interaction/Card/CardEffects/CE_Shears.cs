using Mirror;
using UnityEngine;

public class CE_Shears : CE_Base
{
    [SerializeField] private float multiplier = 0.5f;
    [SerializeField] private float duration = 30f;

    public override void ApplyEffectToTarget(
        Player player)
    {
        if (player == null)
            return;

        NetworkIdentity identity =
            player.netIdentity;

        if (identity == null)
            return;

        NetworkConnectionToClient connection =
            identity.connectionToClient;

        if (connection == null)
            return;

        TargetApplyEffect(
            connection,
            multiplier,
            duration);
    }

    [TargetRpc]
    private void TargetApplyEffect(
        NetworkConnectionToClient connection,
        float multiplier,
        float duration)
    {
        Player localPlayer =
            NetworkClient.localPlayer != null
                ? NetworkClient.localPlayer.GetComponent<Player>()
                : null;

        if (localPlayer == null)
            return;

        if (localPlayer.cameraController == null)
            return;

        localPlayer.cameraController.ApplyMultiplyer(
            multiplier,
            duration);
    }
}
