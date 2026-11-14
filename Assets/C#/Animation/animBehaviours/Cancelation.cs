using UnityEngine;

public class Cancelation : StateMachineBehaviour
{
    [SerializeField] int itemIndex;
    [SerializeField] int soundIndex = -1;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        GameLog.Write(
            "Cancelation",
            $"OnStateEnter index={itemIndex} animator={(animator != null ? animator.name : "NULL")} layer={layerIndex}");

        PlayerAnimator playerAnimator =
            animator.GetComponentInParent<PlayerAnimator>();
        if (playerAnimator != null)
        {
            playerAnimator.AttackStartedOrCanceled(itemIndex);
            playerAnimator.DiscardAnimationStarted();
        }

        NetworkSound soundController =
          animator.GetComponentInParent<NetworkSound>();
        if (soundController != null && soundIndex >= 0)
        {
            soundController.Play(soundIndex);
        }
    }
}
