#if UNITY_EDITOR
using UnityEngine;

public static class DiceFaces
{
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

    public static int ReadValue(
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

    public static Quaternion FrontRotation(
        int value,
        Transform cam,
        Vector3 diePosition)
    {
        Vector3 toCamera =
            (cam.position - diePosition).normalized;

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
                toCamera,
                cam.up);

        return world * Quaternion.Inverse(die);
    }
}
#endif
