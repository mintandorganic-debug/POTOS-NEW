using UnityEngine;

public class ReactionCancel : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetInteger("ReactionId", 0);
    }
}
