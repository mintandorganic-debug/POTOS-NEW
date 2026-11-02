using Mirror;
using UnityEngine;

public class CE_Shears : CE_Base
{
    [SerializeField] private float multiplier = 0.5f;
    [SerializeField] private float duration = 30f;

    public override void ApplyEffectToTarget(
        Player player)
    {
        GameLog.Write(
            "CE_Shears",
            $"ApplyEffectToTarget effect={name} target={(player != null ? player.name : "NULL")} multiplier={multiplier} duration={duration}");

        if (player == null)
        {
            GameLog.Write(
                "CE_Shears",
                $"ApplyEffectToTarget skipped effect={name}: player is NULL");

            return;
        }

        NetworkIdentity identity =
            player.netIdentity;

        if (identity == null)
        {
            GameLog.Write(
                "CE_Shears",
                $"ApplyEffectToTarget skipped effect={name} target={player.name}: netIdentity is NULL");

            return;
        }

        NetworkConnectionToClient connection =
            identity.connectionToClient;

        if (connection == null)
        {
            GameLog.Write(
                "CE_Shears",
                $"ApplyEffectToTarget skipped effect={name} target={player.name}: connectionToClient is NULL");

            return;
        }

        GameLog.Write(
            "CE_Shears",
            $"ApplyEffectToTarget effect={name} target={player.name}: sending TargetApplyEffect to connection={connection.connectionId}");

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
        GameLog.Write(
            "CE_Shears",
            $"TargetApplyEffect effect={name} multiplier={multiplier} duration={duration}");

        Player localPlayer =
            NetworkClient.localPlayer != null
                ? NetworkClient.localPlayer.GetComponent<Player>()
                : null;

        if (localPlayer == null)
        {
            GameLog.Write(
                "CE_Shears",
                $"TargetApplyEffect skipped effect={name}: local player is NULL");

            return;
        }

        if (localPlayer.cameraController == null)
        {
            GameLog.Write(
                "CE_Shears",
                $"TargetApplyEffect skipped effect={name}: cameraController is NULL on local player={localPlayer.name}");

            return;
        }

        GameLog.Write(
            "CE_Shears",
            $"TargetApplyEffect effect={name} local player={localPlayer.name}: calling ApplyMultiplyer multiplier={multiplier} duration={duration}");

        localPlayer.cameraController.ApplyMultiplyer(
            multiplier,
            duration);
    }
}
