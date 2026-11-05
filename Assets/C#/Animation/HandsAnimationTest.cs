#if UNITY_EDITOR
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandsAnimationTest : MonoBehaviour
{
    private const string HandsControllerName = "Player_AC";
    private const string IdleState = "hands|hands_idle";

    private static readonly string[] States =
    {
        "DicePrep",
        "DiceReady",
        "DiceCharge",
        "DiceAttack"
    };

    private int step;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go =
            new GameObject("HandsAnimationTest");

        DontDestroyOnLoad(go);

        go.AddComponent<HandsAnimationTest>();
    }

    private void Update()
    {
        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null ||
            !keyboard.f7Key.wasPressedThisFrame)
        {
            return;
        }

        Animator hands =
            FindHands();

        if (hands == null)
        {
            GameLog.Write(
                "HandsAnimationTest",
                "F7: no local hands animator, start a match first");

            return;
        }

        string state =
            step < States.Length
                ? States[step]
                : IdleState;

        GameLog.Write(
            "HandsAnimationTest",
            $"F7 step={step} crossfade to {state}");

        hands.CrossFade(
            state,
            0.1f,
            0);

        StartCoroutine(
            Probe(
                hands,
                state));

        step =
            (step + 1) % (States.Length + 1);
    }

    private IEnumerator Probe(
        Animator hands,
        string state)
    {
        Transform marker =
            FindDeep(
                hands.transform,
                "dice1");

        for (int i = 0;
             i < 6;
             i++)
        {
            yield return new WaitForSeconds(
                0.5f);

            AnimatorStateInfo info =
                hands.GetCurrentAnimatorStateInfo(0);

            GameLog.Write(
                "HandsAnimationTest",
                $"Probe {state} t={(i + 1) * 0.5f} inState={info.IsName(state)} normalizedTime={info.normalizedTime:0.00} dice1Pos={(marker != null ? marker.position.ToString("F3") : "NULL")}");
        }
    }

    private static Animator FindHands()
    {
        if (NetworkClient.localPlayer == null)
        {
            return null;
        }

        foreach (Animator candidate in
                 NetworkClient.localPlayer.GetComponentsInChildren<Animator>(true))
        {
            if (candidate.runtimeAnimatorController != null &&
                candidate.runtimeAnimatorController.name == HandsControllerName)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Transform FindDeep(
        Transform root,
        string name)
    {
        foreach (Transform child in
                 root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }
}
#endif
