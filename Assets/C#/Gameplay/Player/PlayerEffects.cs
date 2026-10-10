using Mirror;
using UnityEngine;

public class PlayerEffects : NetworkBehaviour
{
    [SerializeField] private CameraLook cameraController;

    public void ApplyCameraMultiplier(
        float multiplier,
        float duration)
    {
        if (!isServer)
            return;

        if (connectionToClient == null)
            return;

        TargetApplyCameraMultiplier(
            connectionToClient,
            multiplier,
            duration);
    }

    [TargetRpc]
    private void TargetApplyCameraMultiplier(
        NetworkConnectionToClient connection,
        float multiplier,
        float duration)
    {
        if (cameraController == null)
            return;

        cameraController.ApplyMultiplyer(
            multiplier,
            duration);
    }
}