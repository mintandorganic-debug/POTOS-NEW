using UnityEngine;

public class Attack : StateMachineBehaviour
{
    [SerializeField] int index;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        PlayerAnimator playerAnimator =
            animator.GetComponentInParent<PlayerAnimator>();
        playerAnimator.AttackStartedOrCanceled(index);
    }
}
