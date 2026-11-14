#if UNITY_EDITOR
using System.Collections;
using UnityEngine;

// Independent lift control: anyone can call Raise / Lower.
public class LiftAnimation : MonoBehaviour
{
    private const string LiftControllerName = "Lift_AC";

    private Animator animator;
    private bool raised;
    private bool platformLowered;

    public Animator Animator
    {
        get
        {
            Find();

            return animator;
        }
    }

    public bool IsInState(
        string stateName)
    {
        if (!Find())
        {
            return false;
        }

        int layer =
            animator.GetLayerIndex(
                "Lift");

        return
            !animator.IsInTransition(layer) &&
            animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName);
    }

    public Transform PlatformPart
    {
        get
        {
            if (!Find())
            {
                return null;
            }

            foreach (Transform child in
                     animator.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "platform" &&
                    child.Find("Cube.016") != null &&
                    child.Find("Cylinder.018") != null)
                {
                    return child;
                }
            }

            return null;
        }
    }

    public float RiseDuration
    {
        get
        {
            if (!Find())
            {
                return 0f;
            }

            foreach (AnimationClip clip in
                     animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name.EndsWith("DICE_PLATE_APPEAR"))
                {
                    return clip.length + 0.3f;
                }
            }

            return 1.5f;
        }
    }

    public void Raise()
    {
        if (!Find() ||
            !HasParameter("PlateUp"))
        {
            GameLog.Write(
                "LiftAnimation",
                "Raise skipped: Lift_AC has no PlateUp trigger, wait for the editor setup");

            return;
        }

        animator.ResetTrigger(
            "DropEmpty");

        animator.ResetTrigger(
            "PlatformDown");

        animator.ResetTrigger(
            "PlateDown");

        animator.SetTrigger(
            "PlateUp");

        raised = true;
        platformLowered = false;

        LogState(
            "key3",
            3f);

        GameLog.Write(
            "LiftAnimation",
            "Raise: platform and plate up");
    }

    public void Lower()
    {
        if (!Find() ||
            !HasParameter("PlateUp"))
        {
            return;
        }

        animator.ResetTrigger(
            "PlateUp");

        if (raised)
        {
            animator.SetTrigger(
                "PlateDown");

            raised = false;
            platformLowered = true;

            GameLog.Write(
                "LiftAnimation",
                "Lower: plate and platform down");

            return;
        }

        if (!platformLowered)
        {
            animator.SetTrigger(
                "PlatformDown");

            platformLowered = true;
        }
    }

    public void LogState(
        string source,
        float seconds)
    {
        if (Find())
        {
            StartCoroutine(
                LogStateRoutine(
                    source,
                    seconds));
        }
    }

    private IEnumerator LogStateRoutine(
        string source,
        float seconds)
    {
        int layer =
            animator.GetLayerIndex(
                "Lift");

        string[] states =
        {
            "Idle",
            "PlatformHide",
            "DiceAppear",
            "DiceHide"
        };

        float time = 0f;

        while (time < seconds)
        {
            string current = "?";

            AnimatorStateInfo info =
                animator.GetCurrentAnimatorStateInfo(
                    layer);

            foreach (string state in states)
            {
                if (info.IsName(state))
                {
                    current = state;
                }
            }

            AnimatorStateInfo baseInfo =
                animator.GetCurrentAnimatorStateInfo(
                    0);

            SkinnedMeshRenderer plateMesh = null;

            foreach (SkinnedMeshRenderer renderer in
                     animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.name == "Plate")
                {
                    plateMesh = renderer;
                }
            }

            GameLog.Write(
                "LiftAnimation",
                $"[{source}] t={time:0.0} liftLayer={current} n={info.normalizedTime:0.00} weight={animator.GetLayerWeight(layer):0.0} inTransition={animator.IsInTransition(layer)} baseLayerHash={baseInfo.shortNameHash} speed={animator.speed:0.0} mode={animator.updateMode} plateMesh={(plateMesh != null ? plateMesh.enabled + "/visible=" + plateMesh.isVisible + "/layer=" + LayerMask.LayerToName(plateMesh.gameObject.layer) : "none")}");

            yield return new WaitForSeconds(
                0.25f);

            time += 0.25f;
        }
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

    private bool Find()
    {
        if (animator != null)
        {
            return true;
        }

        foreach (Animator candidate in
                 FindObjectsByType<Animator>(
                     FindObjectsInactive.Include))
        {
            if (candidate.runtimeAnimatorController == null ||
                candidate.runtimeAnimatorController.name != LiftControllerName)
            {
                continue;
            }

            animator = candidate;

            animator.updateMode = AnimatorUpdateMode.Fixed;

            SkinnedMeshRenderer[] renderers =
                candidate.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                renderer.updateWhenOffscreen = false;

                renderer.localBounds =
                    new Bounds(
                        Vector3.zero,
                        Vector3.one * 40f);
            }

            GameLog.Write(
                "LiftAnimation",
                $"Find animator={candidate.name} skinnedRenderers={renderers.Length}");

            return true;
        }

        return false;
    }
}
#endif
