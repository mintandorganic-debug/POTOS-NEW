using UnityEngine;

public class Cancelation : StateMachineBehaviour
{
    [SerializeField] int index;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        PlayerAnimator playerAnimator =
            animator.GetComponentInParent<PlayerAnimator>();
        if (playerAnimator != null)
        {
            playerAnimator.AttackStartedOrCanceled(index);
            playerAnimator.DiscardAnimationStarted();
        }
    }
}
