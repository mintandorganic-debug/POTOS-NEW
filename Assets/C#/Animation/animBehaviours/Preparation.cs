using UnityEngine;

public class Preparation : StateMachineBehaviour
{
    [SerializeField] int index;

    private bool hasFinished;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        hasFinished = false;
    }

    public override void OnStateUpdate(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (hasFinished ||
            stateInfo.normalizedTime < 1f)
        {
            return;
        }

        hasFinished = true;

        PlayerAnimator playerAnimator =
            animator.GetComponentInParent<PlayerAnimator>();

        if (playerAnimator == null)
            return;

        playerAnimator.PreparationFinished(index);
    }
}