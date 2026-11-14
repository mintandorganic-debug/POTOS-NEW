using UnityEngine;

public class Preparation : StateMachineBehaviour
{
    [SerializeField] int itemIndex;
    [SerializeField] int soundIndex = -1;
    private bool hasFinished;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        GameLog.Write(
            "Preparation",
            $"OnStateEnter index={itemIndex} animator={(animator != null ? animator.name : "NULL")} layer={layerIndex}");

        hasFinished = false;

        NetworkSound soundController =
           animator.GetComponentInParent<NetworkSound>();
        soundController.Play(soundIndex);
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
            $"OnStateUpdate preparation state finished index={itemIndex} animator={(animator != null ? animator.name : "NULL")} layer={layerIndex}");

        PlayerAnimator playerAnimator =
            animator.GetComponentInParent<PlayerAnimator>();

        if (playerAnimator == null)
        {
            GameLog.Write(
                "Preparation",
                $"OnStateUpdate PlayerAnimator not found index={itemIndex}");

            return;
        }

        playerAnimator.PreparationFinished(itemIndex);
    }
}
