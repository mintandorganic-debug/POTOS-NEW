using System.Collections;
using UnityEngine;

public class DiceLift : MonoBehaviour
{
    private const string LiftControllerName = "Lift_AC";
    private const float FallbackClipLength = 1f;

    private Animator animator;
    private Coroutine routine;
    private bool raised;

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
            $"OnPhaseChanged phase={phase} raised={raised}");

        if (phase == GameMaster.GamePhase.Dice)
        {
            if (!raised)
            {
                Run(
                    RaiseRoutine());
            }

            return;
        }

        if (raised)
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
        if (!FindAnimator())
        {
            yield break;
        }

        if (!HasParameter("PlatformUp"))
        {
            GameLog.Write(
                "DiceLift",
                "RaiseRoutine skipped: Lift_AC has no PlatformUp trigger");

            yield break;
        }

        raised = true;

        GameLog.Write(
            "DiceLift",
            "RaiseRoutine platform up");

        animator.SetTrigger(
            "PlatformUp");

        yield return new WaitForSeconds(
            GetClipLength("PLATFORM_APPEAR"));

        GameLog.Write(
            "DiceLift",
            "RaiseRoutine plate up");

        animator.SetTrigger(
            "PlateUp");
    }

    private IEnumerator LowerRoutine()
    {
        if (!FindAnimator())
        {
            yield break;
        }

        raised = false;

        GameLog.Write(
            "DiceLift",
            "LowerRoutine plate down");

        animator.SetTrigger(
            "PlateDown");

        yield return new WaitForSeconds(
            GetClipLength("DICE_PLATE_HIDE"));

        GameLog.Write(
            "DiceLift",
            "LowerRoutine platform down");

        animator.SetTrigger(
            "PlatformDown");
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
