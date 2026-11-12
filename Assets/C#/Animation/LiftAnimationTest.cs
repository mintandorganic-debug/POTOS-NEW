#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Test: F1, F2, F3 play the three lift animations one by one, each separately.
public class LiftAnimationTest : MonoBehaviour
{
    private const string LayerName = "Lift";

    private LiftAnimation lift;
    private Coroutine running;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go =
            new GameObject("LiftAnimationTest");

        DontDestroyOnLoad(go);

        go.AddComponent<LiftAnimation>();
        go.AddComponent<LiftAnimationTest>();
    }

    private void Awake()
    {
        lift =
            GetComponent<LiftAnimation>();
    }

    private void Update()
    {
        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.f1Key.wasPressedThisFrame)
        {
            Play(
                "DiceAppear",
                "DICE_PLATE_APPEAR");
        }

        if (keyboard.f2Key.wasPressedThisFrame)
        {
            Play(
                "DiceHide",
                "DICE_PLATE_HIDE");
        }

        if (keyboard.f3Key.wasPressedThisFrame)
        {
            Play(
                "PlatformHide",
                "PLATFORM_HIDE");
        }
    }

    private void Play(
        string stateName,
        string clipName)
    {
        Animator animator =
            lift.Animator;

        if (animator == null)
        {
            GameLog.Write(
                "LiftAnimationTest",
                "Lift animator not found");

            return;
        }

        int layer =
            animator.GetLayerIndex(
                LayerName);

        float length =
            ClipLength(
                animator,
                clipName);

        if (layer < 0 ||
            length <= 0f)
        {
            GameLog.Write(
                "LiftAnimationTest",
                $"Cannot play {stateName}: layer={layer} clipLength={length}");

            return;
        }

        if (running != null)
        {
            StopCoroutine(
                running);
        }

        running =
            StartCoroutine(
                Run(
                    animator,
                    layer,
                    stateName,
                    length));
    }

    private IEnumerator Run(
        Animator animator,
        int layer,
        string stateName,
        float length)
    {
        GameLog.Write(
            "LiftAnimationTest",
            $"Play {stateName} ({length:0.00}s)");

        lift.LogState(
            stateName,
            3f);

        animator.SetLayerWeight(
            layer,
            1f);

        float time = 0f;

        while (time < length)
        {
            time += Time.deltaTime;

            animator.Play(
                stateName,
                layer,
                Mathf.Clamp01(time / length) * 0.999f);

            yield return null;
        }

        GameLog.Write(
            "LiftAnimationTest",
            $"{stateName} finished, holding the last frame");

        while (true)
        {
            animator.Play(
                stateName,
                layer,
                0.999f);

            yield return null;
        }
    }

    private static float ClipLength(
        Animator animator,
        string clipName)
    {
        foreach (AnimationClip clip in
                 animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name.EndsWith(clipName))
            {
                return clip.length;
            }
        }

        return 0f;
    }
}
#endif
