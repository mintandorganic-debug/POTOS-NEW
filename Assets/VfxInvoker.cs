
using Mirror;
using UnityEngine;

public class VfxInvoker : StateMachineBehaviour
{
    [Header("VFX")]
    [SerializeField] private int vfxIndex;

    [Header("Timing")]
    [SerializeField] private int invokeFrame = 15;

    private PlayerVfx playerVfx;
    private NetworkIdentity networkIdentity;
    private bool hasInvoked;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        playerVfx = animator.GetComponentInParent<PlayerVfx>();
        networkIdentity = animator.GetComponentInParent<NetworkIdentity>();

        hasInvoked = false;
    }

    public override void OnStateUpdate(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (hasInvoked || networkIdentity == null || !networkIdentity.isServer)
            return;

        if (playerVfx == null)
            return;

        float frameRate = animator.GetCurrentAnimatorClipInfo(layerIndex)[0].clip.frameRate;

        int currentFrame = Mathf.FloorToInt(
            stateInfo.normalizedTime *
            animator.GetCurrentAnimatorClipInfo(layerIndex)[0].clip.length *
            frameRate);

        if (currentFrame < invokeFrame)
            return;

        hasInvoked = true;
        playerVfx.PlayVfx(vfxIndex);
    }
}
