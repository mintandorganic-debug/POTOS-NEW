#if UNITY_EDITOR
using System.Collections;
using UnityEngine;

public class DiceLiftTest : MonoBehaviour
{
    private const string LiftControllerName = "Lift_AC";

    private Animator animator;
    private Transform plate;
    private bool raised;
    private bool platformLowered;

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

    public bool IsRaised =>
        raised;

    public Transform Plate
    {
        get
        {
            Find();

            return plate;
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

    public void LogPlateRenderer()
    {
        if (!Find())
        {
            return;
        }

        foreach (SkinnedMeshRenderer renderer in
                 animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            string bones = string.Empty;

            for (int i = 0;
                 i < renderer.bones.Length && i < 4;
                 i++)
            {
                bones += $" {renderer.bones[i].name}={renderer.bones[i].position.ToString("F2")}";
            }

            Mesh baked =
                new Mesh();

            renderer.BakeMesh(
                baked);

            Vector3 bakedCenter =
                renderer.transform.TransformPoint(
                    baked.bounds.center);

            GameLog.Write(
                "DiceLiftTest",
                $"Renderer {renderer.name} bakedWorldCenter={bakedCenter.ToString("F2")} bakedSize={baked.bounds.size.ToString("F2")} isVisible={renderer.isVisible} enabled={renderer.enabled} layer={LayerMask.LayerToName(renderer.gameObject.layer)}");

            Destroy(
                baked);

            GameLog.Write(
                "DiceLiftTest",
                $"Renderer {renderer.name} root={(renderer.rootBone != null ? renderer.rootBone.name + renderer.rootBone.position.ToString("F2") : "NULL")} bones={renderer.bones.Length}{bones} bounds={renderer.bounds.center.ToString("F2")}/{renderer.bounds.size.ToString("F2")} local={renderer.localBounds.center.ToString("F2")}/{renderer.localBounds.size.ToString("F2")}");
        }
    }

    public bool TryGetPlateBounds(
        out Bounds bounds)
    {
        bounds = default;

        if (!Find())
        {
            return false;
        }

        foreach (SkinnedMeshRenderer renderer in
                 animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.name == "Plate")
            {
                bounds = renderer.bounds;

                return true;
            }
        }

        return false;
    }

    public void Raise()
    {
        if (!Find() ||
            !HasParameter("PlateUp"))
        {
            GameLog.Write(
                "DiceLiftTest",
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

        GameLog.Write(
            "DiceLiftTest",
            "Raise: platform and plate up");

        StartCoroutine(
            Probe());
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
                "DiceLiftTest",
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

    private IEnumerator Probe()
    {
        Transform platformBone = null;

        foreach (Transform child in
                 animator.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "platform" &&
                child.childCount >= 0 &&
                child.GetComponent<Renderer>() == null)
            {
                platformBone = child;
                break;
            }
        }

        for (int i = 0;
             i < 20;
             i++)
        {
            GameLog.Write(
                "DiceLiftTest",
                $"Probe t={i * 0.2f:0.0} platformBone={(platformBone != null ? platformBone.position.ToString("F2") : "NULL")} plateBone={(plate != null ? plate.position.ToString("F2") : "NULL")}");

            yield return new WaitForSeconds(
                0.2f);
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
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (candidate.runtimeAnimatorController == null ||
                candidate.runtimeAnimatorController.name != LiftControllerName)
            {
                continue;
            }

            animator = candidate;

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

            foreach (Transform child in
                     candidate.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "plate")
                {
                    plate = child;
                    break;
                }
            }

            GameLog.Write(
                "DiceLiftTest",
                $"Find animator={candidate.name} skinnedRenderers={renderers.Length} plate={(plate != null)}");

            return true;
        }

        return false;
    }
}
#endif
