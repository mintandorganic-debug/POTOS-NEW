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
        GameLog.Write(
            "Preparation",
            $"OnStateEnter index={index} animator={(animator != null ? animator.name : "NULL")} layer={layerIndex}");

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

        GameLog.Write(
            "Preparation",
            $"OnStateUpdate preparation state finished index={index} animator={(animator != null ? animator.name : "NULL")} layer={layerIndex}");

        PlayerAnimator playerAnimator =
            animator.GetComponentInParent<PlayerAnimator>();

        if (playerAnimator == null)
        {
            GameLog.Write(
                "Preparation",
                $"OnStateUpdate PlayerAnimator not found index={index}");

            return;
        }

        playerAnimator.PreparationFinished(index);
    }
}
