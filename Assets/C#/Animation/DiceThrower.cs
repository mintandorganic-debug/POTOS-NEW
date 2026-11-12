#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

// The throw itself: parabola calculation and release of the dice.
public static class DiceThrower
{
    public const float FlightTime = 0.7f;

    private const float NearDistance = 0.35f;
    private const float LateralSpread = 0.02f;
    private const float StopSpeed = 0.05f;

    public static Vector3 LaunchVelocity(
        Vector3 origin,
        Vector3 landing,
        float power,
        float side,
        out Vector3 spot)
    {
        Vector3 toCenter =
            landing - origin;

        toCenter.y = 0f;

        Vector3 flightDirection =
            toCenter.sqrMagnitude > 0.0001f
                ? toCenter.normalized
                : Vector3.forward;

        Vector3 near =
            origin +
            flightDirection * NearDistance;

        near.y = landing.y;

        Vector3 target =
            Vector3.Lerp(
                near,
                landing,
                power);

        Vector3 sideways =
            Vector3.Cross(
                Vector3.up,
                flightDirection).normalized;

        spot =
            target +
            Vector3.up * 0.05f +
            sideways * side * LateralSpread;

        return
            (spot - origin -
             0.5f * Physics.gravity * FlightTime * FlightTime) /
            FlightTime;
    }

    public static Vector3 PointAt(
        Vector3 origin,
        Vector3 velocity,
        float time)
    {
        return
            origin +
            velocity * time +
            0.5f * Physics.gravity * time * time;
    }

    public static void Release(
        IList<Rigidbody> dice,
        Vector3 landing,
        float power)
    {
        for (int i = 0;
             i < dice.Count;
             i++)
        {
            Rigidbody body =
                dice[i];

            Vector3 velocity =
                LaunchVelocity(
                    body.position,
                    landing,
                    power,
                    i - 1,
                    out Vector3 spot);

            DiceSpawner.SetColliders(
                body,
                true);

            body.isKinematic = false;
            body.linearVelocity = velocity;
            body.angularVelocity = Vector3.zero;

            GameLog.Write(
                "DiceThrower",
                $"Release die from={body.position.ToString("F2")} to={spot.ToString("F2")} velocity={velocity.ToString("F2")}");
        }
    }

    public static bool AllStopped(
        IList<Rigidbody> dice)
    {
        foreach (Rigidbody body in dice)
        {
            if (body != null &&
                body.linearVelocity.magnitude > StopSpeed)
            {
                return false;
            }
        }

        return true;
    }
}
#endif
