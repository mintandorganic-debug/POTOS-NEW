using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private CameraLook cameraLook;

    [Header("Animation Actions")]
    [SerializeField] private List<ActionEntry> actions = new();

    [System.Serializable]
    private class ActionEntry
    {
        public string actionId;
        public int animatorId;
    }

    [System.Serializable]
    private class ActionOverrider
    {
        public int actionId;
        public AnimatorOverrider overrider;
    }

    [SerializeField] private List<ActionOverrider> actionOverriders;

    private Dictionary<string, int> actionIds;
    private Dictionary<int, AnimatorOverrider> overriders;

    private void Awake()
    {
        GameLog.Write(
            "PlayerAnimator",
            $"Awake object={gameObject.name} actions={(actions != null ? actions.Count : -1)} overriders={(actionOverriders != null ? actionOverriders.Count : -1)} animator={(animator != null ? animator.name : "NULL")} cameraLook={(cameraLook != null ? cameraLook.name : "NULL")}");

        actionIds = new Dictionary<string, int>();
        overriders = new Dictionary<int, AnimatorOverrider>();

        foreach (ActionEntry action in actions)
        {
            if (string.IsNullOrEmpty(action.actionId))
            {
                GameLog.Write(
                    "PlayerAnimator",
                    $"Awake skipped action entry: empty actionId animatorId={action.animatorId}");

                continue;
            }

            if (actionIds.ContainsKey(action.actionId))
            {
                Debug.LogWarning(
                    $"Duplicate animation action ID: {action.actionId}",
                    this);

                GameLog.Write(
                    "PlayerAnimator",
                    $"Awake skipped action entry: duplicate actionId={action.actionId} animatorId={action.animatorId}");

                continue;
            }

            actionIds.Add(
                action.actionId,
                action.animatorId);

            GameLog.Write(
                "PlayerAnimator",
                $"Awake action registered actionId={action.actionId} animatorId={action.animatorId}");
        }

        foreach (ActionOverrider entry in actionOverriders)
        {
            if (entry.overrider == null)
            {
                GameLog.Write(
                    "PlayerAnimator",
                    $"Awake skipped overrider entry: overrider is NULL actionId={entry.actionId}");

                continue;
            }

            if (overriders.ContainsKey(entry.actionId))
            {
                Debug.LogWarning(
                    $"Duplicate action overrider ID: {entry.actionId}",
                    this);

                GameLog.Write(
                    "PlayerAnimator",
                    $"Awake skipped overrider entry: duplicate actionId={entry.actionId}");

                continue;
            }

            overriders.Add(
                entry.actionId,
                entry.overrider);

            GameLog.Write(
                "PlayerAnimator",
                $"Awake overrider registered actionId={entry.actionId} overrider={entry.overrider.name}");
        }

        GameLog.Write(
            "PlayerAnimator",
            $"Awake done actionIds={actionIds.Count} overriders={overriders.Count}");
    }

    public void StartAttackAnimation(string actionId)
    {
        GameLog.Write(
            "PlayerAnimator",
            $"StartAttackAnimation actionId={actionId} animator={(animator != null ? animator.name : "NULL")}");

        if (!GetAnimatorId(
                actionId,
                out int animatorId))
        {
            GameLog.Write(
                "PlayerAnimator",
                $"StartAttackAnimation aborted: animator id not found actionId={actionId}");

            return;
        }

        animator.SetBool(
            "Attack",
            false);

        animator.SetInteger(
            "ActionId",
            animatorId);

        GameLog.Write(
            "PlayerAnimator",
            $"StartAttackAnimation applied actionId={actionId} animatorId={animatorId} Attack=false");
    }

    public void CancelAttackAnimation(
        string actionId)
    {
        GameLog.Write(
            "PlayerAnimator",
            $"CancelAttackAnimation actionId={actionId} animator={(animator != null ? animator.name : "NULL")} ActionId reset to 0");

        animator.SetInteger(
            "ActionId",
            0);
    }

    public void TriggerAttack()
    {
        GameLog.Write(
            "PlayerAnimator",
            $"TriggerAttack animator={(animator != null ? animator.name : "NULL")} Attack trigger set, ActionId reset to 0");

        animator.SetTrigger(
            "Attack");

        animator.SetInteger(
            "ActionId",
            0);
    }

    public void EnableCardLift()
    {
        GameLog.Write(
            "PlayerAnimator",
            $"EnableCardLift animator={(animator != null ? animator.name : "NULL")} CardHandLift trigger set");

        animator.SetTrigger(
            "CardHandLift");
    }

    public void EnableCardFocus()
    {
        GameLog.Write(
            "PlayerAnimator",
            $"EnableCardFocus animator={(animator != null ? animator.name : "NULL")} CardSelect=true");

        animator.SetBool(
            "CardSelect",
            true);
    }

    public void DisableCardFocus()
    {
        GameLog.Write(
            "PlayerAnimator",
            $"DisableCardFocus animator={(animator != null ? animator.name : "NULL")} CardSelect=false");

        animator.SetBool(
            "CardSelect",
            false);
    }

    private bool GetAnimatorId(
        string actionId,
        out int animatorId)
    {
        GameLog.Write(
            "PlayerAnimator",
            $"GetAnimatorId actionId={actionId}");

        if (string.IsNullOrEmpty(actionId))
        {
            animatorId = 0;

            GameLog.Write(
                "PlayerAnimator",
                $"GetAnimatorId failed: actionId is null or empty");

            return false;
        }

        if (!actionIds.TryGetValue(
                actionId,
                out animatorId))
        {
            Debug.LogWarning(
                $"Animation action not found: {actionId}",
                this);

            GameLog.Write(
                "PlayerAnimator",
                $"GetAnimatorId failed: action not found actionId={actionId}");

            return false;
        }

        GameLog.Write(
            "PlayerAnimator",
            $"GetAnimatorId found actionId={actionId} animatorId={animatorId}");

        return true;
    }

    private bool GetOverrider(
        int actionId,
        out AnimatorOverrider overrider)
    {
        GameLog.Write(
            "PlayerAnimator",
            $"GetOverrider actionId={actionId}");

        if (!overriders.TryGetValue(
                actionId,
                out overrider))
        {
            Debug.LogWarning(
                $"Animation overrider not found: {actionId}",
                this);

            GameLog.Write(
                "PlayerAnimator",
                $"GetOverrider failed: overrider not found actionId={actionId}");

            return false;
        }

        GameLog.Write(
            "PlayerAnimator",
            $"GetOverrider found actionId={actionId} overrider={(overrider != null ? overrider.name : "NULL")}");

        return true;
    }

    public void PreparationFinished(int actionId)
    {
        GameLog.Write(
            "PlayerAnimator",
            $"PreparationFinished actionId={actionId} cameraLook={(cameraLook != null ? cameraLook.name : "NULL")}");

        if (!GetOverrider(
                actionId,
                out AnimatorOverrider overrider))
        {
            GameLog.Write(
                "PlayerAnimator",
                $"PreparationFinished aborted: overrider not found actionId={actionId}");

            return;
        }

        overrider.enabled = true;
        cameraLook.enableHandRotation = true;

        GameLog.Write(
            "PlayerAnimator",
            $"PreparationFinished applied actionId={actionId} overrider enabled, enableHandRotation=true");
    }

    public void AttackStartedOrCanceled(int actionId)
    {
        GameLog.Write(
            "PlayerAnimator",
            $"AttackStartedOrCanceled actionId={actionId} cameraLook={(cameraLook != null ? cameraLook.name : "NULL")}");

        if (!GetOverrider(
                actionId,
                out AnimatorOverrider overrider))
        {
            GameLog.Write(
                "PlayerAnimator",
                $"AttackStartedOrCanceled aborted: overrider not found actionId={actionId}");

            return;
        }

        overrider.enabled = false;
        cameraLook.enableHandRotation = false;

        GameLog.Write(
            "PlayerAnimator",
            $"AttackStartedOrCanceled applied actionId={actionId} overrider disabled, enableHandRotation=false");
    }
}