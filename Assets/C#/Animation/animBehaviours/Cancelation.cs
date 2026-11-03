using UnityEngine;

public class Cancelation : StateMachineBehaviour
{
    [SerializeField] int index;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        GameLog.Write(
            "Cancelation",
            $"OnStateEnter index={index} animator={(animator != null ? animator.name : "NULL")} layer={layerIndex}");

        PlayerAnimator playerAnimator =
            animator.GetComponentInParent<PlayerAnimator>();
        playerAnimator.AttackStartedOrCanceled(index);
    }
}
