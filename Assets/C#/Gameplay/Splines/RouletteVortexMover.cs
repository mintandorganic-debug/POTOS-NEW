using System.Collections;
using UnityEngine;

public class RouletteVortexMover
{
    private readonly IRouletteMotion motion;

    public RouletteVortexMover(
        IRouletteMotion motion)
    {
        this.motion = motion;

        GameLog.Write(
            "RouletteVortexMover",
            "Created");
    }

    public IEnumerator Run()
    {
        GameLog.Write(
            "RouletteVortexMover",
            $"MoveVortex start pivot={(motion.RoulettePivot != null ? motion.RoulettePivot.name : "NULL")} cardMarker={(motion.CardMarker != null ? motion.CardMarker.name : "NULL")}");

        if (motion.RoulettePivot == null ||
            motion.CardMarker == null)
        {
            GameLog.Write(
                "RouletteVortexMover",
                "MoveVortex aborted: pivot or cardMarker is null");

            yield break;
        }

        float startRadius =
            motion.GetRadius();

        if (startRadius < 0.0001f)
        {
            GameLog.Write(
                "RouletteVortexMover",
                $"MoveVortex aborted: startRadius too small startRadius={startRadius}");

            yield break;
        }

        float startLocalY =
            motion.CardMarker.localPosition.y;

        float targetLocalY =
            startLocalY +
            motion.VortexDepth;

        float totalAngle =
            Mathf.Max(
                1f,
                motion.VortexRounds) *
            360f;

        float passedAngle = 0f;

        bool depthStarted = false;

        GameLog.Write(
            "RouletteVortexMover",
            $"MoveVortex spiral start startRadius={startRadius} startLocalY={startLocalY} targetLocalY={targetLocalY} totalAngle={totalAngle} liftAnimator={(motion.LiftAnimator != null ? motion.LiftAnimator.name : "NULL")}");

        if (motion.LiftAnimator != null)
        {
            GameLog.Write(
                "RouletteVortexMover",
                "MoveVortex trigger DropEmpty");

            motion.LiftAnimator.SetTrigger(
                "DropEmpty");
        }

        while (passedAngle < totalAngle)
        {
            float progress =
                passedAngle /
                totalAngle;

            float desiredRadius =
                Mathf.Lerp(
                    startRadius,
                    0f,
                    progress);

            // -------------------------------------------------
            // ROTATE ROULETTE
            //
            // This rotates cardMarker.
            // defenseCardMarker follows automatically because
            // it is a child of cardMarker.
            // -------------------------------------------------

            float desiredSpeed =
                motion.NormalSpeed *
                motion.VortexSpeedMultiplier;

            motion.CurrentAngularSpeed =
                motion.MoveSpeedWithInertia(
                    desiredSpeed);

            float minimumSpeed =
                motion.MinSpeed *
                motion.VortexSpeedMultiplier;

            if (motion.CurrentAngularSpeed <
                minimumSpeed)
            {
                motion.CurrentAngularSpeed =
                    minimumSpeed;
            }

            float rotation =
                motion.CurrentAngularSpeed *
                Time.deltaTime;

            rotation =
                Mathf.Min(
                    rotation,
                    totalAngle -
                    passedAngle);

            motion.Rotate(
                rotation,
                true);

            passedAngle +=
                rotation;

            // -------------------------------------------------
            // MOVE MAIN MARKER TOWARD CENTER
            //
            // Only cardMarker is moved.
            // Defense marker and its card follow automatically.
            // -------------------------------------------------

            Vector3 offset =
                motion.CardMarker.position -
                motion.RoulettePivot.position;

            offset.y = 0f;

            if (offset.sqrMagnitude >
                0.0001f)
            {
                offset.Normalize();

                Vector3 worldPosition =
                    motion.RoulettePivot.position +
                    offset * desiredRadius;

                Vector3 localPosition =
                    motion.RoulettePivot.InverseTransformPoint(
                        worldPosition);

                localPosition.y =
                    motion.CardMarker.localPosition.y;

                motion.CardMarker.localPosition =
                    localPosition;
            }

            // -------------------------------------------------
            // DEPTH
            // -------------------------------------------------

            if (!depthStarted &&
                desiredRadius <= motion.VortexRadiusThreshold)
            {
                depthStarted = true;

                GameLog.Write(
                    "RouletteVortexMover",
                    $"MoveVortex depth started desiredRadius={desiredRadius} passedAngle={passedAngle}");
            }

            if (depthStarted)
            {
                Vector3 localPosition =
                    motion.CardMarker.localPosition;

                localPosition.y =
                    Mathf.MoveTowards(
                        localPosition.y,
                        targetLocalY,
                        motion.VortexDepthSpeed *
                        Time.deltaTime);

                motion.CardMarker.localPosition =
                    localPosition;
            }

            yield return null;
        }

        // -----------------------------------------------------
        // FINAL POSITION
        // -----------------------------------------------------

        Vector3 finalLocalPosition =
            motion.CardMarker.localPosition;

        finalLocalPosition.x = 0f;
        finalLocalPosition.z = 0f;
        finalLocalPosition.y = targetLocalY;

        motion.CardMarker.localPosition =
            finalLocalPosition;

        motion.CurrentAngularSpeed = 0f;

        GameLog.Write(
            "RouletteVortexMover",
            $"MoveVortex finished finalLocalPosition={finalLocalPosition} passedAngle={passedAngle}");
    }
}
