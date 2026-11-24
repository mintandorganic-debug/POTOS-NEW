using UnityEngine;

public class Cancelation : StateMachineBehaviour
{
    [SerializeField] int itemIndex;

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

    }
}
