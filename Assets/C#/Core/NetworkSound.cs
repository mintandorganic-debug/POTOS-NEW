using Mirror;
using UnityEngine;

public class NetworkSound : NetworkBehaviour
{
    [SerializeField] private AudioSource source;   // Spatial Blend = 1 (3D)
    [SerializeField] private AudioClip[] clips;

    // Called on the server: everyone hears it, including the host
    [Server]
    public void Play(int clipIndex)
    {
        RpcPlay(clipIndex);
    }

    // Called by the owning client, for example for footsteps
    [Command]
    public void CmdPlay(int clipIndex)
    {
        RpcPlay(clipIndex);
    }

    [ClientRpc]
    private void RpcPlay(int clipIndex)
    {
        if (source == null ||
            clipIndex < 0 ||
            clipIndex >= clips.Length)
        {
            return;
        }

        source.PlayOneShot(clips[clipIndex]);
    }
}