#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DiceResultDisplay : MonoBehaviour
{
    private const float SpinTime = 1f;
    private const float Distance = 0.6f;
    private const float Spacing = 0.14f;
    private const float Scale = 1.2f;
    private const float PanelBottom = 75f;
    private const float PanelGap = 12f;
    private const float SettleTime = 0.35f;
    private const float MinSpinSpeed = 360f;
    private const float MaxSpinSpeed = 900f;

    // Faces in the local space of the die mesh root: 1 opposite 6, 2 opposite 5, 3 opposite 4.
    private static readonly Vector3[] FaceDirections =
    {
        new Vector3(-1f, 0f, 0f),
        new Vector3(0f, 1f, 0f),
        new Vector3(0f, 0f, -1f),
        new Vector3(0f, 0f, 1f),
        new Vector3(0f, -1f, 0f),
        new Vector3(1f, 0f, 0f)
    };

    private readonly List<GameObject> copies = new List<GameObject>();
    private string resultText = string.Empty;
    private Coroutine running;
    private float dieSize = 0.1f;

    public int[] LastValues { get; private set; } = new int[0];
    public string LastCombination { get; private set; } = string.Empty;

    public void Show(
        IList<Rigidbody> dice)
    {
        Hide();

        running =
            StartCoroutine(
                Run(
                    dice));
    }

    public void Hide()
    {
        if (running != null)
        {
            StopCoroutine(
                running);

            running = null;
        }

        foreach (GameObject copy in copies)
        {
            if (copy != null)
            {
                Destroy(
                    copy);
            }
        }

        copies.Clear();

        resultText = string.Empty;
    }

    private static int ReadValue(
        Transform die)
    {
        Transform mesh =
            die.childCount > 0
                ? die.GetChild(0)
                : die;

        int best = 0;
        float bestDot = float.MinValue;

        for (int i = 0;
             i < FaceDirections.Length;
             i++)
        {
            float dot =
                Vector3.Dot(
                    mesh.rotation * FaceDirections[i],
                    Vector3.up);

            if (dot > bestDot)
            {
                bestDot = dot;
                best = i;
            }
        }

        return best + 1;
    }

    private IEnumerator Run(
        IList<Rigidbody> dice)
    {
        int[] values =
            dice
                .Where(d => d != null)
                .Select(d => ReadValue(d.transform))
                .ToArray();

        int[] sorted =
            values
                .OrderBy(v => v)
                .ToArray();

        IDiceCombination combination =
            DiceCombinationEvaluator
                .CreateDefault()
                .Evaluate(
                    sorted);

        LastValues = values;

        LastCombination =
            combination != null
                ? $"{combination.Name} (+{combination.Coins})"
                : "no combination";

        resultText =
            combination != null
                ? $"{string.Join("  ", values)}  -  {combination.Name} (+{combination.Coins})"
                : $"{string.Join("  ", values)}  -  no combination";

        GameLog.Write(
            "DiceResultDisplay",
            $"Result {resultText}");

        GameObject prefab =
            Resources.Load<GameObject>(
                "Dice/SingleDie");

        Transform cam =
            Camera.main != null
                ? Camera.main.transform
                : null;

        if (prefab == null ||
            cam == null)
        {
            yield break;
        }

        for (int i = 0;
             i < values.Length;
             i++)
        {
            GameObject copy =
                Instantiate(
                    prefab);

            foreach (Collider collider in
                     copy.GetComponentsInChildren<Collider>(true))
            {
                Destroy(
                    collider);
            }

            Rigidbody body =
                copy.GetComponent<Rigidbody>();

            if (body != null)
            {
                Destroy(
                    body);
            }

            copy.transform.localScale *= Scale;

            copies.Add(
                copy);
        }

        Vector3[] spinAxes =
            new Vector3[copies.Count];

        float[] spinSpeeds =
            new float[copies.Count];

        for (int i = 0;
             i < copies.Count;
             i++)
        {
            spinAxes[i] =
                Random.onUnitSphere;

            spinSpeeds[i] =
                Random.Range(
                    MinSpinSpeed,
                    MaxSpinSpeed);

            copies[i].transform.rotation =
                Random.rotation;
        }

        Renderer firstRenderer =
            copies[0].GetComponentInChildren<Renderer>();

        if (firstRenderer != null)
        {
            Vector3 size =
                firstRenderer.bounds.size;

            dieSize =
                Mathf.Max(
                    size.x,
                    Mathf.Max(
                        size.y,
                        size.z));
        }

        float time = 0f;

        while (time < SpinTime)
        {
            time += Time.deltaTime;

            for (int i = 0;
                 i < copies.Count;
                 i++)
            {
                Place(
                    copies[i],
                    cam,
                    i);

                copies[i].transform.Rotate(
                    spinAxes[i],
                    spinSpeeds[i] * Time.deltaTime,
                    Space.World);
            }

            yield return null;
        }

        Quaternion[] startRotations =
            new Quaternion[copies.Count];

        for (int i = 0;
             i < copies.Count;
             i++)
        {
            startRotations[i] =
                copies[i].transform.rotation;
        }

        time = 0f;

        while (true)
        {
            time += Time.deltaTime;

            float blend =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        time / SettleTime));

            for (int i = 0;
                 i < copies.Count;
                 i++)
            {
                Place(
                    copies[i],
                    cam,
                    i);

                copies[i].transform.rotation =
                    Quaternion.Slerp(
                        startRotations[i],
                        FrontRotation(
                            values[i],
                            cam),
                        blend);
            }

            yield return null;
        }
    }

    private static Quaternion FrontRotation(
        int value,
        Transform cam)
    {
        Vector3 face =
            FaceDirections[value - 1];

        Vector3 edge =
            Mathf.Abs(face.y) < 0.9f
                ? Vector3.up
                : Vector3.forward;

        Quaternion die =
            Quaternion.LookRotation(
                face,
                edge);

        Quaternion world =
            Quaternion.LookRotation(
                -cam.forward,
                cam.up);

        return world * Quaternion.Inverse(die);
    }

    private void Place(
        GameObject copy,
        Transform cam,
        int index)
    {
        Camera camera =
            cam.GetComponent<Camera>();

        float distance = Distance;

        float viewHeight =
            2f * distance *
            Mathf.Tan(
                camera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        float viewWidth =
            viewHeight * camera.aspect;

        float top =
            1f - (PanelBottom + PanelGap) / Screen.height;

        float y =
            top - dieSize * 0.5f / viewHeight;

        float x =
            0.5f + (index - 1) * dieSize * 1.4f / viewWidth;

        copy.transform.position =
            camera.ViewportToWorldPoint(
                new Vector3(
                    x,
                    y,
                    distance));
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(resultText))
        {
            return;
        }

        GUIStyle title =
            new GUIStyle(
                GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };

        float width = 260f;

        GUI.Box(
            new Rect(
                (Screen.width - width) * 0.5f,
                20f,
                width,
                55f),
            GUIContent.none);

        GUI.Label(
            new Rect(
                (Screen.width - width) * 0.5f,
                22f,
                width,
                25f),
            string.Join("   ", LastValues),
            title);

        GUI.Label(
            new Rect(
                (Screen.width - width) * 0.5f,
                47f,
                width,
                25f),
            LastCombination,
            title);
    }
}
#endif
