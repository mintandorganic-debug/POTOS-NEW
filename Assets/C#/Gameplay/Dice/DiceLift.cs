using System.Collections;
using UnityEngine;

public class DiceLift : MonoBehaviour
{
    private const string LiftControllerName = "Lift_AC";
    private const float FallbackClipLength = 1f;
    private const float RiseMargin = 0.3f;

    private Animator animator;
    private bool platformLowered;
    private bool diceRaised;

    public float RiseDuration
    {
        get
        {
            if (!FindAnimator() ||
                !HasParameter("PlateUp"))
            {
                return 0f;
            }

            return GetClipLength("DICE_PLATE_APPEAR") +
                   RiseMargin;
        }
    }

    private void Start()
    {
        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.GamePhaseChanged += OnPhaseChanged;
        }

        GameLog.Write(
            "DiceLift",
            $"Start subscribed={GameMaster.Instance != null}");
    }

    private void OnDestroy()
    {
        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.GamePhaseChanged -= OnPhaseChanged;
        }
    }

    private void OnPhaseChanged(
        GameMaster.GamePhase phase)
    {
        GameLog.Write(
            "DiceLift",
            $"OnPhaseChanged phase={phase} platformLowered={platformLowered} diceRaised={diceRaised}");

        if (!FindAnimator() ||
            !HasParameter("PlateUp"))
        {
            GameLog.Write(
                "DiceLift",
                "OnPhaseChanged skipped: Lift_AC has no dice triggers");

            return;
        }

        if (phase == GameMaster.GamePhase.Dice)
        {
            Raise();

            return;
        }

        if (phase == GameMaster.GamePhase.Wait)
        {
            return;
        }

        Lower();
    }

    private void Raise()
    {
        if (diceRaised)
        {
            return;
        }

        GameLog.Write(
            "DiceLift",
            "Raise dice plate with platform");

        animator.ResetTrigger(
            "DropEmpty");

        animator.ResetTrigger(
            "PlatformDown");

        animator.ResetTrigger(
            "PlateDown");

        animator.SetTrigger(
            "PlateUp");

        diceRaised = true;
        platformLowered = false;

        StartCoroutine(
            Probe());
    }

    public void DebugRaise()
    {
        if (FindAnimator() &&
            HasParameter("PlateUp"))
        {
            diceRaised = false;
            Raise();
        }
    }

    public void DebugLower()
    {
        if (FindAnimator() &&
            HasParameter("PlateUp"))
        {
            Lower();
        }
    }

    private IEnumerator Probe()
    {
        Transform plate = null;

        foreach (Transform child in
                 animator.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "plate")
            {
                plate = child;
                break;
            }
        }

        SkinnedMeshRenderer[] renderers =
            animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        int lastLayer =
            animator.layerCount - 1;

        for (int i = 0;
             i < 6;
             i++)
        {
            yield return new WaitForSeconds(
                0.5f);

            AnimatorStateInfo info =
                animator.GetCurrentAnimatorStateInfo(lastLayer);

            string state = "other";

            foreach (string name in new[]
                     {
                         "Idle",
                         "PlatformHide",
                         "DiceAppear",
                         "DiceHide"
                     })
            {
                if (info.IsName(name))
                {
                    state = name;
                }
            }

            int visible = 0;

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (renderer.isVisible)
                {
                    visible++;
                }
            }

            GameLog.Write(
                "DiceLift",
                $"Probe t={(i + 1) * 0.5f} state={state} normalizedTime={info.normalizedTime:0.00} layerWeight={animator.GetLayerWeight(lastLayer)} animatorEnabled={animator.isActiveAndEnabled} plateWorldPos={(plate != null ? plate.position.ToString("F2") : "NULL")} visibleRenderers={visible}/{renderers.Length}");
        }
    }

    private void Lower()
    {
        animator.ResetTrigger(
            "PlateUp");

        if (diceRaised)
        {
            GameLog.Write(
                "DiceLift",
                "Lower dice plate with platform");

            animator.SetTrigger(
                "PlateDown");

            diceRaised = false;
            platformLowered = true;

            return;
        }

        if (platformLowered)
        {
            return;
        }

        GameLog.Write(
            "DiceLift",
            "Lower platform");

        animator.SetTrigger(
            "PlatformDown");

        platformLowered = true;
    }

    private bool HasParameter(
        string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in
                 animator.parameters)
        {
            if (parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private bool FindAnimator()
    {
        if (animator != null)
        {
            return true;
        }

        foreach (Animator candidate in
                 FindObjectsByType<Animator>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (candidate.runtimeAnimatorController != null &&
                candidate.runtimeAnimatorController.name == LiftControllerName)
            {
                animator = candidate;

                GameLog.Write(
                    "DiceLift",
                    $"FindAnimator found animator={candidate.name}");

                return true;
            }
        }

        GameLog.Write(
            "DiceLift",
            $"FindAnimator failed: no animator with controller {LiftControllerName}");

        return false;
    }

    private float GetClipLength(
        string clipName)
    {
        foreach (AnimationClip clip in
                 animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName ||
                clip.name.EndsWith("|" + clipName))
            {
                return clip.length;
            }
        }

        GameLog.Write(
            "DiceLift",
            $"GetClipLength clip not found name={clipName}, using fallback {FallbackClipLength}");

        return FallbackClipLength;
    }
}
