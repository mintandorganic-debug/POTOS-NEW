using System.Collections;
using UnityEngine;

public class DiceLift : MonoBehaviour
{
    private const string LiftControllerName = "Lift_AC";
    private const float FallbackClipLength = 1f;
    private const float RiseMargin = 0.2f;

    private Animator animator;
    private Coroutine routine;
    private bool platformLowered;
    private bool plateRaised;

    public float RiseDuration
    {
        get
        {
            if (!FindAnimator() ||
                !HasParameter("PlatformUp"))
            {
                return 0f;
            }

            return GetClipLength("PLATFORM_APPEAR") +
                   GetClipLength("DICE_PLATE_APPEAR") +
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
            $"OnPhaseChanged phase={phase} platformLowered={platformLowered} plateRaised={plateRaised}");

        if (phase == GameMaster.GamePhase.Dice)
        {
            Run(
                RaiseRoutine());

            return;
        }

        if (phase == GameMaster.GamePhase.Wait)
        {
            return;
        }

        if (plateRaised ||
            !platformLowered)
        {
            Run(
                LowerRoutine());
        }
    }

    private void Run(
        IEnumerator next)
    {
        if (routine != null)
        {
            StopCoroutine(
                routine);
        }

        routine =
            StartCoroutine(
                next);
    }

    private IEnumerator RaiseRoutine()
    {
        if (!FindAnimator() ||
            !HasParameter("PlatformUp"))
        {
            GameLog.Write(
                "DiceLift",
                "RaiseRoutine skipped: Lift_AC has no dice triggers");

            yield break;
        }

        GameLog.Write(
            "DiceLift",
            "RaiseRoutine platform up");

        animator.ResetTrigger(
            "DropEmpty");

        animator.ResetTrigger(
            "PlatformDown");

        animator.ResetTrigger(
            "PlateDown");

        animator.SetTrigger(
            "PlatformUp");

        platformLowered = false;

        yield return new WaitForSeconds(
            GetClipLength("PLATFORM_APPEAR"));

        GameLog.Write(
            "DiceLift",
            "RaiseRoutine plate up");

        animator.SetTrigger(
            "PlateUp");

        plateRaised = true;
    }

    private IEnumerator LowerRoutine()
    {
        if (!FindAnimator() ||
            !HasParameter("PlatformDown"))
        {
            yield break;
        }

        animator.ResetTrigger(
            "PlatformUp");

        animator.ResetTrigger(
            "PlateUp");

        if (plateRaised)
        {
            GameLog.Write(
                "DiceLift",
                "LowerRoutine plate down");

            animator.SetTrigger(
                "PlateDown");

            plateRaised = false;

            yield return new WaitForSeconds(
                GetClipLength("DICE_PLATE_HIDE"));
        }

        if (platformLowered)
        {
            yield break;
        }

        GameLog.Write(
            "DiceLift",
            "LowerRoutine platform down");

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
