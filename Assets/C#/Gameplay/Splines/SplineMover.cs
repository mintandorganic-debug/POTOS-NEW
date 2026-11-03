using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class SplineMover : MonoBehaviour
{
    [Header("Spline")]
    [SerializeField] private SplineContainer container;

    [Header("Durations")]
    [SerializeField] private float entryDuration = 0.5f;
    [SerializeField] private float fullCircleDuration = 1.5f;
    [SerializeField] private float partialCircleDuration = 0.5f;
    [SerializeField] private float exitDuration = 0.5f;

    [Header("Exit")]
    [SerializeField] private float targetHeight = 1.5f;

    public static SplineMover instance;

    private void Start()
    {
        GameLog.Write(
            "SplineMover",
            $"Start object={name} container={(container != null ? container.name : "NULL")}");

        instance = this;
    }

    public void MoveCard(
        Transform card,
        Transform playerB)
    {
        GameLog.Write(
            "SplineMover",
            $"MoveCard card={(card != null ? card.name : "NULL")} playerB={(playerB != null ? playerB.name : "NULL")} container={(container != null ? container.name : "NULL")}");

        if (card == null)
        {
            GameLog.Write(
                "SplineMover",
                "MoveCard aborted: card is null");

            Debug.LogError("Cannot move card: card is null.");
            return;
        }

        if (playerB == null)
        {
            GameLog.Write(
                "SplineMover",
                "MoveCard aborted: playerB is null");

            Debug.LogError("Cannot move card: player B is null.");
            return;
        }

        if (container == null)
        {
            GameLog.Write(
                "SplineMover",
                "MoveCard aborted: container is null");

            Debug.LogError("Cannot move card: SplineContainer is null.");
            return;
        }

        if (container.Splines.Count < 2)
        {
            GameLog.Write(
                "SplineMover",
                $"MoveCard aborted: not enough splines count={container.Splines.Count}");

            Debug.LogError(
                "SplineContainer must contain at least Entry and Circle splines.");
            return;
        }

        EnsureGeneratedSplines();

        StartCoroutine(
            MoveRoutine(
                card,
                playerB));
    }

    private void EnsureGeneratedSplines()
    {
        GameLog.Write(
            "SplineMover",
            $"EnsureGeneratedSplines splineCount={container.Splines.Count}");

        while (container.Splines.Count < 4)
        {
            container.AddSpline();
        }

        GameLog.Write(
            "SplineMover",
            $"EnsureGeneratedSplines done splineCount={container.Splines.Count}");
    }

    private IEnumerator MoveRoutine(
        Transform card,
        Transform playerB)
    {
        GameLog.Write(
            "SplineMover",
            $"MoveRoutine start card={(card != null ? card.name : "NULL")} playerB={(playerB != null ? playerB.name : "NULL")}");

        Vector3 circleCenter =
            container.transform.position;

        // Move from the player's hand to K0.
        yield return MoveAlongSpline(
            card,
            0,
            entryDuration,
            false,
            circleCenter);

        // Complete one full revolution.
        yield return MoveAlongSpline(
            card,
            1,
            fullCircleDuration,
            true,
            circleCenter);

        GameLog.Write(
            "SplineMover",
            "MoveRoutine entry and full circle finished");

        // Calculate the exit angle relative to K0.
        float exitAngle =
            GetExitAngle(
                circleCenter,
                playerB);

        GameLog.Write(
            "SplineMover",
            $"MoveRoutine exitAngle={exitAngle} partialCircle={exitAngle > 0.01f}");

        // Build the second part of the same circle.
        BuildPartialCircle(
            circleCenter,
            exitAngle);

        // Move along the partial circle.
        if (exitAngle > 0.01f)
        {
            yield return MoveAlongSpline(
                card,
                2,
                partialCircleDuration,
                true,
                circleCenter);
        }

        // Build the path from the circle to player B.
        BuildExitSpline(
            circleCenter,
            playerB);

        // Move to player B.
        yield return MoveAlongSpline(
            card,
            3,
            exitDuration,
            false,
            circleCenter);

        GameLog.Write(
            "SplineMover",
            $"MoveRoutine finished card={(card != null ? card.name : "NULL")}");
    }

    private float GetExitAngle(
        Vector3 circleCenter,
        Transform playerB)
    {
        GameLog.Write(
            "SplineMover",
            $"GetExitAngle circleCenter={circleCenter} playerB={(playerB != null ? playerB.name : "NULL")}");

        Vector3 toPlayer =
            playerB.position - circleCenter;

        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.001f)
        {
            GameLog.Write(
                "SplineMover",
                "GetExitAngle returning 0: player is at circle center");

            return 0f;
        }

        toPlayer.Normalize();

        Spline circle =
            container.Splines[1];

        Vector3 localK0 =
            circle.EvaluatePosition(0f);

        Vector3 worldK0 =
            container.transform.TransformPoint(
                localK0);

        Vector3 fromCenter =
            worldK0 - circleCenter;

        fromCenter.y = 0f;

        if (fromCenter.sqrMagnitude < 0.001f)
        {
            GameLog.Write(
                "SplineMover",
                "GetExitAngle returning 0: K0 is at circle center");

            return 0f;
        }

        fromCenter.Normalize();

        float angle =
            Vector3.SignedAngle(
                fromCenter,
                toPlayer,
                Vector3.up);

        if (angle < 0f)
            angle += 360f;

        GameLog.Write(
            "SplineMover",
            $"GetExitAngle result angle={angle}");

        return angle;
    }

    private void BuildPartialCircle(
        Vector3 circleCenter,
        float endAngle)
    {
        GameLog.Write(
            "SplineMover",
            $"BuildPartialCircle circleCenter={circleCenter} endAngle={endAngle}");

        Spline sourceCircle =
            container.Splines[1];

        Spline partialCircle =
            container.Splines[2];

        partialCircle.Clear();
        partialCircle.Closed = false;

        Vector3 localK0 =
            sourceCircle.EvaluatePosition(0f);

        Vector3 worldK0 =
            container.transform.TransformPoint(
                localK0);

        Vector3 radial =
            worldK0 - circleCenter;

        radial.y = 0f;

        float radius =
            radial.magnitude;

        if (radius < 0.001f)
        {
            GameLog.Write(
                "SplineMover",
                $"BuildPartialCircle aborted: radius too small radius={radius}");

            Debug.LogError(
                "Cannot build partial circle: circle radius is too small.");
            return;
        }

        radial.Normalize();

        float startAngle =
            Mathf.Atan2(
                radial.x,
                radial.z) *
            Mathf.Rad2Deg;

        float totalAngle =
            Mathf.Clamp(
                endAngle,
                0f,
                360f);

        if (totalAngle < 0.01f)
        {
            GameLog.Write(
                "SplineMover",
                $"BuildPartialCircle skipped: totalAngle too small totalAngle={totalAngle}");

            return;
        }

        const float maxSegmentAngle = 90f;

        int segmentCount =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    totalAngle /
                    maxSegmentAngle));

        float segmentAngle =
            totalAngle /
            segmentCount;

        float tangentFactor =
            4f / 3f *
            Mathf.Tan(
                Mathf.Deg2Rad *
                segmentAngle /
                4f);

        for (int i = 0; i <= segmentCount; i++)
        {
            float angle =
                startAngle +
                segmentAngle * i;

            Vector3 radialDirection =
                Quaternion.Euler(
                    0f,
                    angle - startAngle,
                    0f) *
                radial;

            Vector3 position =
                circleCenter +
                radialDirection * radius;

            Vector3 tangent =
                Vector3.Cross(
                    Vector3.up,
                    radialDirection).normalized;

            Vector3 localPosition =
                container.transform.InverseTransformPoint(
                    position);

            BezierKnot knot =
                new BezierKnot(
                    localPosition);

            if (i > 0)
            {
                Vector3 tangentIn =
                    -tangent *
                    radius *
                    tangentFactor;

                knot.TangentIn =
                    container.transform
                        .InverseTransformDirection(
                            tangentIn);
            }

            if (i < segmentCount)
            {
                Vector3 tangentOut =
                    tangent *
                    radius *
                    tangentFactor;

                knot.TangentOut =
                    container.transform
                        .InverseTransformDirection(
                            tangentOut);
            }

            partialCircle.Add(knot);
        }

        GameLog.Write(
            "SplineMover",
            $"BuildPartialCircle done segmentCount={segmentCount} totalAngle={totalAngle} radius={radius}");
    }

    private void BuildExitSpline(
        Vector3 circleCenter,
        Transform playerB)
    {
        GameLog.Write(
            "SplineMover",
            $"BuildExitSpline circleCenter={circleCenter} playerB={(playerB != null ? playerB.name : "NULL")} targetHeight={targetHeight}");

        Spline partialCircle =
            container.Splines[2];

        Spline exitSpline =
            container.Splines[3];

        exitSpline.Clear();
        exitSpline.Closed = false;

        Vector3 start =
            container.transform.TransformPoint(
                partialCircle.EvaluatePosition(1f));

        Vector3 target =
            playerB.position +
            Vector3.up * targetHeight;

        Vector3 toTarget =
            target - start;

        float distance =
            toTarget.magnitude;

        if (distance < 0.001f)
        {
            GameLog.Write(
                "SplineMover",
                $"BuildExitSpline skipped: distance too small distance={distance}");

            return;
        }

        Vector3 direction =
            toTarget.normalized;

        Vector3 outward =
            start - circleCenter;

        outward.y = 0f;

        if (outward.sqrMagnitude < 0.001f)
            outward = direction;

        outward.Normalize();

        Vector3 tangent =
            Vector3.Cross(
                Vector3.up,
                outward).normalized;

        Vector3 control1 =
            start +
            tangent *
            Mathf.Min(
                distance * 0.35f,
                2f);

        Vector3 control2 =
            target -
            direction *
            Mathf.Min(
                distance * 0.25f,
                1.5f);

        AddBezierSegment(
            exitSpline,
            start,
            control1,
            control2,
            target);
    }

    private void AddBezierSegment(
        Spline spline,
        Vector3 start,
        Vector3 control1,
        Vector3 control2,
        Vector3 end)
    {
        GameLog.Write(
            "SplineMover",
            $"AddBezierSegment start={start} control1={control1} control2={control2} end={end}");

        Vector3 localStart =
            container.transform.InverseTransformPoint(
                start);

        Vector3 localEnd =
            container.transform.InverseTransformPoint(
                end);

        Vector3 localControl1 =
            container.transform.InverseTransformPoint(
                control1);

        Vector3 localControl2 =
            container.transform.InverseTransformPoint(
                control2);

        BezierKnot startKnot =
            new BezierKnot(
                localStart);

        startKnot.TangentOut =
            localControl1 - localStart;

        BezierKnot endKnot =
            new BezierKnot(
                localEnd);

        endKnot.TangentIn =
            localControl2 - localEnd;

        spline.Add(startKnot);
        spline.Add(endKnot);
    }

    private IEnumerator MoveAlongSpline(
        Transform card,
        int splineIndex,
        float duration,
        bool isCircle,
        Vector3 circleCenter)
    {
        GameLog.Write(
            "SplineMover",
            $"MoveAlongSpline start card={(card != null ? card.name : "NULL")} splineIndex={splineIndex} duration={duration} isCircle={isCircle} circleCenter={circleCenter}");

        Spline spline =
            container.Splines[splineIndex];

        float time = 0f;

        while (time < duration)
        {
            float t =
                time / duration;

            Vector3 localPosition =
                spline.EvaluatePosition(t);

            Vector3 position =
                container.transform.TransformPoint(
                    localPosition);

            card.position =
                position;

            UpdateCardRotation(
                card,
                spline,
                t,
                position,
                isCircle,
                circleCenter);

            time += Time.deltaTime;

            yield return null;
        }

        Vector3 finalLocalPosition =
            spline.EvaluatePosition(1f);

        Vector3 finalPosition =
            container.transform.TransformPoint(
                finalLocalPosition);

        card.position =
            finalPosition;

        UpdateCardRotation(
            card,
            spline,
            1f,
            finalPosition,
            isCircle,
            circleCenter);

        GameLog.Write(
            "SplineMover",
            $"MoveAlongSpline end splineIndex={splineIndex} finalPosition={finalPosition}");
    }

    private void UpdateCardRotation(
        Transform card,
        Spline spline,
        float t,
        Vector3 position,
        bool isCircle,
        Vector3 circleCenter)
    {
        if (isCircle)
        {
            Vector3 outward =
                position - circleCenter;

            outward.y = 0f;

            if (outward.sqrMagnitude > 0.001f)
            {
                outward.Normalize();

                // Y points away from the circle center.
                // Z points upward.
                card.rotation =
                    Quaternion.LookRotation(
                        Vector3.up,
                        outward);
            }

            return;
        }

        Vector3 localTangent =
            spline.EvaluateTangent(t);

        Vector3 tangent =
            container.transform.TransformDirection(
                localTangent);

        if (tangent.sqrMagnitude > 0.001f)
        {
            card.rotation =
                Quaternion.LookRotation(
                    tangent.normalized,
                    Vector3.up);
        }
    }
}