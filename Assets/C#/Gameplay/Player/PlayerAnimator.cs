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

    private string currentAttackWeapon;
    private bool pendingAttackAftermath;

    private void Awake()
    {
        actionIds = new Dictionary<string, int>();
        overriders = new Dictionary<int, AnimatorOverrider>();

        foreach (ActionEntry action in actions)
        {
            if (string.IsNullOrEmpty(action.actionId))
                continue;

            if (actionIds.ContainsKey(action.actionId))
            {
                Debug.LogWarning(
                    $"Duplicate animation action ID: {action.actionId}",
                    this);

                continue;
            }

            actionIds.Add(
                action.actionId,
                action.animatorId);
        }

        foreach (ActionOverrider entry in actionOverriders)
        {
            if (entry.overrider == null)
                continue;

            if (overriders.ContainsKey(entry.actionId))
            {
                Debug.LogWarning(
                    $"Duplicate action overrider ID: {entry.actionId}",
                    this);

                continue;
            }

            overriders.Add(
                entry.actionId,
                entry.overrider);
        }
    }

    public void StartAttackAnimation(string actionId)
    {
        currentAttackWeapon = actionId;

        if (!TryGetAnimatorId(
                actionId,
                out int animatorId))
        {
            return;
        }

        animator.SetBool(
            "Attack",
            false);

        animator.SetInteger(
            "ActionId",
            animatorId);
    }

    public void CancelAttackAnimation(
        string actionId = "")
    {
        if (!string.IsNullOrEmpty(actionId))
            currentAttackWeapon = actionId;

        pendingAttackAftermath = false;

        animator.SetInteger(
            "ActionId",
            0);

        if (!string.IsNullOrEmpty(currentAttackWeapon))
        {
            StartCoroutine(AttackAftermathDelayRoutine(currentAttackWeapon));
        }
    }

    public void TriggerAttack(string weaponKey = "")
    {
        if (!string.IsNullOrEmpty(weaponKey))
            currentAttackWeapon = weaponKey;

        pendingAttackAftermath = true;

        animator.SetTrigger(
            "Attack");

        animator.SetInteger(
            "ActionId",
            0);
    }

    public void DiscardAnimationStarted()
    {
        if (pendingAttackAftermath)
        {
            pendingAttackAftermath = false;
            StartCoroutine(AttackAftermathDelayRoutine(currentAttackWeapon));
        }
    }

    private System.Collections.IEnumerator AttackAftermathDelayRoutine(string weaponKey)
    {
        // 300 ms delay after the discard animation starts
        yield return new WaitForSeconds(0.3f);

        if (GameAudioManager.Instance != null)
        {
            Player p = GetComponentInParent<Player>();
            Mirror.NetworkIdentity netId = p != null ? p.netIdentity : GetComponent<Mirror.NetworkIdentity>();
            GameAudioManager.Instance.PlayWeaponAftermath(weaponKey, netId);
        }
    }

    public void EnableCardLift()
    {
        animator.SetTrigger(
            "CardHandLift");
    }

    public void EnableCardFocus()
    {
        animator.SetBool(
            "CardSelect",
            true);
    }

    public void DisableCardFocus()
    {
        animator.SetBool(
            "CardSelect",
            false);
    }

    private bool TryGetAnimatorId(
        string actionId,
        out int animatorId)
    {
        if (string.IsNullOrEmpty(actionId))
        {
            animatorId = 0;
            return false;
        }

        if (!actionIds.TryGetValue(
                actionId,
                out animatorId))
        {
            Debug.LogWarning(
                $"Animation action not found: {actionId}",
                this);

            return false;
        }

        return true;
    }

    private bool TryGetOverrider(
        int actionId,
        out AnimatorOverrider overrider)
    {
        if (!overriders.TryGetValue(
                actionId,
                out overrider))
        {
            Debug.LogWarning(
                $"Animation overrider not found: {actionId}",
                this);

            return false;
        }

        return true;
    }

    public void PreparationFinished(int actionId)
    {
        if (!TryGetOverrider(
                actionId,
                out AnimatorOverrider overrider))
        {
            return;
        }

        overrider.enabled = true;
        cameraLook.enableHandRotation = true;
    }

    public void AttackStartedOrCanceled(int actionId)
    {
        if (!TryGetOverrider(
                actionId,
                out AnimatorOverrider overrider))
        {
            return;
        }

        overrider.enabled = false;
        cameraLook.enableHandRotation = false;
    }
}
