#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Test on F10: the dice logic of the artist (rotation of the UI dice and the green glow).
// Press F10 to throw, press F10 again to remove everything.
public class DiceRollTest : MonoBehaviour
{
    private const float HandHeight = 0.35f;
    private const float HandDistance = 1.3f;
    private const float ThrowTime = 0.7f;
    private const float ReleaseTime = 0.3f;
    private const float Spacing = 0.03f;

    private readonly DiceRollRig rig = new DiceRollRig();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go =
            new GameObject("DiceRollTest");

        DontDestroyOnLoad(go);

        go.AddComponent<DiceRollTest>();
    }

    private void Update()
    {
        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null ||
            !keyboard.f10Key.wasPressedThisFrame)
        {
            return;
        }

        if (rig.IsBuilt)
        {
            StopAllCoroutines();

            rig.Dispose();

            GameLog.Write(
                "DiceRollTest",
                "F10 again: everything removed");

            return;
        }

        StartCoroutine(
            Run());
    }

    private IEnumerator Run()
    {
        int[] score =
            {
                Random.Range(1, 7),
                Random.Range(1, 7),
                Random.Range(1, 7)
            };

        GameLog.Write(
            "DiceRollTest",
            $"F10: throw, required score {string.Join(" ", score)}");

        rig.Build(
            score);

        rig.State.IsCharging = true;
        rig.State.IsAttackOrReady = false;

        Vector3 start =
            new Vector3(
                DiceRollRig.PlateCenter.x + HandDistance,
                HandHeight,
                DiceRollRig.PlateCenter.z);

        for (int i = 0;
             i < 3;
             i++)
        {
            rig.Bones[i].position =
                start + Vector3.forward * (i - 1) * Spacing;
        }

        yield return new WaitForSeconds(
            0.5f);

        rig.State.IsCharging = false;

        float time = 0f;

        while (time < ThrowTime)
        {
            time += Time.deltaTime;

            float t =
                Mathf.Clamp01(time / ThrowTime);

            for (int i = 0;
                 i < 3;
                 i++)
            {
                Vector3 end =
                    DiceRollRig.PlateCenter +
                    Vector3.up * 0.05f +
                    Vector3.forward * (i - 1) * Spacing;

                Vector3 position =
                    Vector3.Lerp(
                        start + Vector3.forward * (i - 1) * Spacing,
                        end,
                        t);

                position.y +=
                    Mathf.Sin(t * Mathf.PI) * 0.25f;

                rig.Bones[i].position = position;
            }

            if (time >= ReleaseTime)
            {
                rig.Controller.Settings.FollowWeight = 0f;
            }

            yield return null;
        }

        rig.State.IsAttackOrReady = true;

        GameLog.Write(
            "DiceRollTest",
            "Throw finished: the dice follow the physics, the UI dice rotate and flash when they stop");
    }
}
#endif
