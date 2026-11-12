#if UNITY_EDITOR
using UnityEngine;

public class DiceAimLine
{
    private const int LineSegments = 24;

    private LineRenderer line;

    public void Draw(
        Transform wrist,
        Vector3 landing,
        float power)
    {
        Build();

        Vector3 origin =
            DiceSpawner.HoldPosition(
                wrist,
                1);

        Vector3 velocity =
            DiceThrower.LaunchVelocity(
                origin,
                landing,
                power,
                0f,
                out Vector3 spot);

        for (int i = 0;
             i <= LineSegments;
             i++)
        {
            line.SetPosition(
                i,
                DiceThrower.PointAt(
                    origin,
                    velocity,
                    DiceThrower.FlightTime * i / LineSegments));
        }

        line.enabled = true;
    }

    public void Hide()
    {
        if (line != null)
        {
            line.enabled = false;
        }
    }

    private void Build()
    {
        if (line != null)
        {
            return;
        }

        GameObject lineObject =
            new GameObject("DiceAimLine");

        line =
            lineObject.AddComponent<LineRenderer>();

        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader =
                Shader.Find("Hidden/Internal-Colored");
        }

        line.material =
            new Material(
                shader);

        line.startColor = Color.red;
        line.endColor = Color.yellow;
        line.startWidth = 0.01f;
        line.endWidth = 0.01f;
        line.positionCount = LineSegments + 1;
    }
}
#endif
