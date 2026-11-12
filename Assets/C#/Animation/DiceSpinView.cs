#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceSpinView : MonoBehaviour
{
    private const float MinSpinTime = 0.5f;
    private const float MaxSpinTime = 3f;
    private const float EndSpinSpeed = 0.1f;
    private const float Distance = 0.6f;
    private const float Scale = 1.2f;
    private const float PanelGap = 12f;
    private const float SettleTime = 0.6f;
    private const float MinSpinSpeed = 360f;
    private const float MaxSpinSpeed = 900f;

    private readonly List<GameObject> copies = new List<GameObject>();
    private Coroutine running;
    private float dieSize = 0.1f;

    public void Show(
        int[] values,
        float power)
    {
        Hide();

        running =
            StartCoroutine(
                Run(
                    values,
                    power));
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
    }

    private IEnumerator Run(
        int[] values,
        float power)
    {
        float spinTime =
            Mathf.Lerp(
                MinSpinTime,
                MaxSpinTime,
                Mathf.Clamp01(
                    power));

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
            copies.Add(
                CreateCopy(
                    prefab));
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

        MeasureDie();

        float time = 0f;

        while (time < spinTime)
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

                float slowdown =
                    Mathf.Lerp(
                        1f,
                        EndSpinSpeed,
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            time / spinTime));

                copies[i].transform.Rotate(
                    spinAxes[i],
                    spinSpeeds[i] * slowdown * Time.deltaTime,
                    Space.World);
            }

            yield return null;
        }

        Quaternion[] startRotations =
            new Quaternion[copies.Count];

        Vector3[] settleAxes =
            new Vector3[copies.Count];

        float[] settleAngles =
            new float[copies.Count];

        for (int i = 0;
             i < copies.Count;
             i++)
        {
            startRotations[i] =
                copies[i].transform.rotation;

            Quaternion target =
                DiceFaces.FrontRotation(
                    values[i],
                    cam,
                    copies[i].transform.position);

            Quaternion delta =
                target * Quaternion.Inverse(startRotations[i]);

            delta.ToAngleAxis(
                out float angle,
                out Vector3 axis);

            if (Vector3.Dot(axis, spinAxes[i]) < 0f)
            {
                axis = -axis;
                angle = 360f - angle;
            }

            settleAxes[i] = axis;
            settleAngles[i] = angle;
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
                    Quaternion.AngleAxis(
                        settleAngles[i] * blend,
                        settleAxes[i]) *
                    startRotations[i];
            }

            yield return null;
        }
    }

    private static GameObject CreateCopy(
        GameObject prefab)
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

        return copy;
    }

    private void MeasureDie()
    {
        Renderer firstRenderer =
            copies[0].GetComponentInChildren<Renderer>();

        if (firstRenderer == null)
        {
            return;
        }

        Vector3 size =
            firstRenderer.bounds.size;

        dieSize =
            Mathf.Max(
                size.x,
                Mathf.Max(
                    size.y,
                    size.z));
    }

    private void Place(
        GameObject copy,
        Transform cam,
        int index)
    {
        Camera camera =
            cam.GetComponent<Camera>();

        float viewHeight =
            2f * Distance *
            Mathf.Tan(
                camera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        float viewWidth =
            viewHeight * camera.aspect;

        float top =
            1f - (DiceResultPanel.BottomPixels + PanelGap) / Screen.height;

        float y =
            top - dieSize * 0.5f / viewHeight;

        float x =
            0.5f + (index - 1) * dieSize * 1.4f / viewWidth;

        copy.transform.position =
            camera.ViewportToWorldPoint(
                new Vector3(
                    x,
                    y,
                    Distance));
    }
}
#endif
