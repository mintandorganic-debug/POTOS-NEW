using UnityEngine;

public class Attack : StateMachineBehaviour
{
    [SerializeField] int index;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        GameLog.Write(
            "Attack",
            $"OnStateEnter index={index} animator={(animator != null ? animator.name : "NULL")} layer={layerIndex}");

        PlayerAnimator playerAnimator =
            animator.GetComponentInParent<PlayerAnimator>();
        playerAnimator.AttackStartedOrCanceled(index);
    }
}
