using UnityEngine;

public class ReactionInvoker : StateMachineBehaviour
{
    [Header("Timing")]
    [SerializeField] private int targetFrame = 10;

    [Header("Reaction")]
    [SerializeField] private string id;

    private bool triggered = false;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        triggered = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (triggered)
            return;

        float frameRate = animator.GetCurrentAnimatorClipInfo(layerIndex)[0].clip.frameRate;

        int currentFrame = Mathf.FloorToInt(
            stateInfo.normalizedTime *
            animator.GetCurrentAnimatorClipInfo(layerIndex)[0].clip.length *
            frameRate);

        if (currentFrame < targetFrame)
            return;

        triggered = true;
        TriggerVictimReaction(animator);
    }

    private void TriggerVictimReaction(Animator animator)
    {
        // Trigger lamp based on weapon type
        if (AttackSequence.Instance.Lamp != null)
        {
            switch (id)
            {
                case "COLT":
                    AttackSequence.Instance.Lamp.SetTrigger("gun");
                    break;
                case "SHOTGUN":
                    AttackSequence.Instance.Lamp.SetTrigger("shotgun");
                    break;
                case "GRENADE":
                    AttackSequence.Instance.Lamp.SetTrigger("nade");
                    break;
            }
        }
        else
        {
            GameLog.Write("ReactionInvoker", "Lamp is null");
        }

        if (AttackSequence.Instance == null)
        {
            GameLog.Write("ReactionInvoker", "AttackSequence.Instance is null");
            return;
        }

        PlayerInteractable target = AttackSequence.Instance.Target;

        if (target == null)
        {
            GameLog.Write("ReactionInvoker", "Target is null");
            return;
        }

        Player victim = target.GetComponent<Player>();

        if (victim == null || victim.animator == null)
        {
            GameLog.Write("ReactionInvoker", $"Victim or animator is null: victim={victim != null} animator={victim?.animator != null}");
            return;
        }

        GameLog.Write("ReactionInvoker", $"Triggering victim reaction: victim={victim.netId} frame={targetFrame} id={id}");

        switch (id)
        {
            case "WISKEY":
                victim.StartDamagePass(1);
                break;
            default:
                victim.StartDamagePass(0);
                break;
        }
        victim.animator.StartReactionAnimation(id);

        
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        triggered = false;
    }
}