using UnityEngine;

public class Attack : StateMachineBehaviour
{
    [SerializeField] int itemIndex;
    [SerializeField] int soundIndex = -1;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        GameLog.Write(
            "Attack",
            $"OnStateEnter index={itemIndex} animator={(animator != null ? animator.name : "NULL")} layer={layerIndex}");

        PlayerAnimator playerAnimator =
            animator.GetComponentInParent<PlayerAnimator>();
        playerAnimator.AttackStartedOrCanceled(itemIndex);

        NetworkSound soundController =
            animator.GetComponentInParent<NetworkSound>();
        soundController.Play(soundIndex);
    }
}
