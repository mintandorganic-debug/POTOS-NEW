using System.Collections;
using UnityEngine;

public class RouletteTargetMover
{
    private readonly IRouletteMotion motion;

    public RouletteTargetMover(
        IRouletteMotion motion)
    {
        this.motion = motion;

        GameLog.Write(
            "RouletteTargetMover",
            "Created");
    }

    public IEnumerator MoveToTarget(
        Transform movementTarget)
    {
        GameLog.Write(
            "RouletteTargetMover",
            $"MoveToTarget start movementTarget={(movementTarget != null ? movementTarget.name : "NULL")}");

        float remainingAngle =
            GetAngleToTarget(
                movementTarget,
                true);

        GameLog.Write(
            "RouletteTargetMover",
            $"MoveToTarget initial remainingAngle={remainingAngle}");

        while (remainingAngle > motion.StopAngle)
        {
            float speedMultiplier = 1f;

            if (remainingAngle < motion.SlowdownAngle)
            {
                speedMultiplier =
                    Mathf.InverseLerp(
                        motion.StopAngle,
                        motion.SlowdownAngle,
                        remainingAngle);
            }

            float desiredSpeed =
                motion.SpringSpeed *
                speedMultiplier;

            motion.CurrentAngularSpeed =
                motion.MoveSpeedWithInertia(
                    desiredSpeed);

            if (remainingAngle <= motion.SlowdownAngle &&
                motion.CurrentAngularSpeed < motion.MinSpeed)
            {
                motion.CurrentAngularSpeed =
                    motion.MinSpeed;
            }

            float rotation =
                motion.CurrentAngularSpeed *
                Time.deltaTime;

            rotation =
                Mathf.Min(
                    rotation,
                    remainingAngle);

            motion.Rotate(
                rotation,
                true);

            remainingAngle =
                GetAngleToTarget(
                    movementTarget,
                    true);

            yield return null;
        }

        motion.CurrentAngularSpeed = 0f;

        GameLog.Write(
            "RouletteTargetMover",
            $"MoveToTarget reached target remainingAngle={remainingAngle}");

        SnapToTarget(
            movementTarget);
    }

    public float GetAngleToTarget(
        Transform movementTarget,
        bool counterClockwise)
    {
        if (motion.CardMarker == null ||
            movementTarget == null)
        {
            return 0f;
        }

        Vector3 markerDirection =
            motion.CardMarker.position -
            motion.RoulettePivot.position;

        Vector3 targetDirection =
            movementTarget.position -
            motion.RoulettePivot.position;

        markerDirection.y = 0f;
        targetDirection.y = 0f;

        if (markerDirection.sqrMagnitude < 0.001f ||
            targetDirection.sqrMagnitude < 0.001f)
        {
            return 0f;
        }

        markerDirection.Normalize();
        targetDirection.Normalize();

        float markerAngle =
            Mathf.Atan2(
                markerDirection.x,
                markerDirection.z) *
            Mathf.Rad2Deg;

        float targetAngle =
            Mathf.Atan2(
                targetDirection.x,
                targetDirection.z) *
            Mathf.Rad2Deg;

        if (counterClockwise)
        {
            return Mathf.Repeat(
                targetAngle -
                markerAngle,
                360f);
        }

        return Mathf.Repeat(
            markerAngle -
            targetAngle,
            360f);
    }

    public void SnapToTarget(
        Transform movementTarget)
    {
        GameLog.Write(
            "RouletteTargetMover",
            $"SnapToTarget movementTarget={(movementTarget != null ? movementTarget.name : "NULL")} cardMarker={(motion.CardMarker != null ? motion.CardMarker.name : "NULL")}");

        if (motion.CardMarker == null)
        {
            GameLog.Write(
                "RouletteTargetMover",
                "SnapToTarget skipped: cardMarker is null");

            return;
        }

        Vector3 targetDirection =
            movementTarget.position -
            motion.RoulettePivot.position;

        targetDirection.y = 0f;

        if (targetDirection.sqrMagnitude < 0.001f)
        {
            GameLog.Write(
                "RouletteTargetMover",
                "SnapToTarget skipped: target is at pivot");

            return;
        }

        targetDirection.Normalize();

        float targetAngle =
            Mathf.Atan2(
                targetDirection.x,
                targetDirection.z) *
            Mathf.Rad2Deg;

        float markerAngle =
            motion.GetMarkerAngle();

        float delta =
            Mathf.Repeat(
                targetAngle -
                markerAngle,
                360f);

        GameLog.Write(
            "RouletteTargetMover",
            $"SnapToTarget rotating delta={delta} targetAngle={targetAngle} markerAngle={markerAngle}");

        motion.RoulettePivot.Rotate(
            Vector3.up,
            delta,
            Space.Self);
    }

    public void RotateTowardTarget(
        Transform movementTarget)
    {
        GameLog.Write(
            "RouletteTargetMover",
            $"RotateTowardTarget movementTarget={(movementTarget != null ? movementTarget.name : "NULL")}");

        if (motion.RoulettePivot == null ||
            motion.CardMarker == null ||
            movementTarget == null)
        {
            GameLog.Write(
                "RouletteTargetMover",
                $"RotateTowardTarget skipped: missing reference pivot={(motion.RoulettePivot != null ? motion.RoulettePivot.name : "NULL")} cardMarker={(motion.CardMarker != null ? motion.CardMarker.name : "NULL")} target={(movementTarget != null ? movementTarget.name : "NULL")}");

            return;
        }

        Vector3 markerDirection =
            motion.CardMarker.position -
            motion.RoulettePivot.position;

        Vector3 targetDirection =
            movementTarget.position -
            motion.RoulettePivot.position;

        markerDirection.y = 0f;
        targetDirection.y = 0f;

        if (markerDirection.sqrMagnitude < 0.0001f ||
            targetDirection.sqrMagnitude < 0.0001f)
        {
            GameLog.Write(
                "RouletteTargetMover",
                "RotateTowardTarget skipped: marker or target is at pivot");

            return;
        }

        markerDirection.Normalize();
        targetDirection.Normalize();

        float markerAngle =
            Mathf.Atan2(
                markerDirection.x,
                markerDirection.z) *
            Mathf.Rad2Deg;

        float targetAngle =
            Mathf.Atan2(
                targetDirection.x,
                targetDirection.z) *
            Mathf.Rad2Deg;

        float delta =
            Mathf.DeltaAngle(
                markerAngle,
                targetAngle);

        GameLog.Write(
            "RouletteTargetMover",
            $"RotateTowardTarget rotating delta={delta}");

        motion.RoulettePivot.Rotate(
            Vector3.up,
            delta,
            Space.World);
    }
}
