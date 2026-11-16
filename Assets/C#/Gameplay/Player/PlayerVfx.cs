
using Mirror;
using UnityEngine;

public class PlayerVfx : NetworkBehaviour
{
    [SerializeField] private ParticleSystem[] effects;

    [Server]
    public void PlayVfx(int index)
    {
        if (effects == null || index < 0 || index >= effects.Length)
        {
            Debug.LogWarning($"PlayerVfx: Invalid VFX index {index}.", this);
            return;
        }

        RpcPlayVfx(index);
    }

    [ClientRpc]
    private void RpcPlayVfx(int index)
    {
        if (effects == null || index < 0 || index >= effects.Length)
            return;

        ParticleSystem effect = effects[index];

        if (effect == null)
            return;

        effect.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear);

        effect.Play();
    }
}
