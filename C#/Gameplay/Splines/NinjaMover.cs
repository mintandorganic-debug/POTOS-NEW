using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class NinjaMover : MonoBehaviour
{
    public static NinjaMover Instance { get; private set; }

    [Header("Circle")]
    [SerializeField] private SplineContainer circleSpline;
    [SerializeField] private float circleDuration = 3f;

    [Header("Fly")]
    [SerializeField] private float flyDuration = 0.7f;
    [SerializeField] private float rotationSpeed = 360f;

    [Header("Delay")]
    [SerializeField] private float delay = 0.3f;

    private Transform card;
    private Transform target;
    private Transform enemyCardMarker;

    private SplineContainer flySplineContainer;
    private Coroutine movementCoroutine;

    private void Awake()
    {
        Instance = this;
        flySplineContainer = gameObject.AddComponent<SplineContainer>();
    }

    public void MoveCard(
        Transform card,
        Transform target,
        Transform enemyCardMarker)
    {
        this.card = card;
        this.target = target;
        this.enemyCardMarker = enemyCardMarker;

        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        movementCoroutine = StartCoroutine(MoveRoutine());
    }

    public void StopMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        card = null;
        target = null;
        enemyCardMarker = null;
    }

    private IEnumerator MoveRoutine()
    {
        CreateFlySpline();

        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime / flyDuration;

            float t = Mathf.Clamp01(time);

            card.position =
                flySplineContainer.EvaluatePosition(t);

            RotateCardDuringFly();

            yield return null;
        }

        LookAtTarget();

        yield return new WaitForSeconds(delay);

        yield return StartCoroutine(CircleRoutine());
    }

    private void RotateCardDuringFly()
    {
        // Rotate around the card's local Y axis.
        card.Rotate(
            0f,
            rotationSpeed * Time.deltaTime,
            0f,
            Space.Self
        );
    }

    private IEnumerator CircleRoutine()
    {
        float splineTime =
            FindClosestPointOnCircle(card.position);

        while (true)
        {
            splineTime += Time.deltaTime / circleDuration;
            splineTime %= 1f;

            Vector3 position =
                circleSpline.EvaluatePosition(splineTime);

            Vector3 tangent =
                circleSpline.EvaluateTangent(splineTime);

            card.position = position;

            RotateCardOnCircle(position, tangent);

            yield return null;
        }
    }

    private void CreateFlySpline()
    {
        Spline spline = new Spline();

        Vector3 start = card.position;

        // Use the marker's X/Z position while keeping the card's current height.
        Vector3 end = new Vector3(
            enemyCardMarker.position.x,
            start.y,
            enemyCardMarker.position.z
        );

        Vector3 direction = end - start;

        if (direction.sqrMagnitude < 0.001f)
            direction = card.forward;

        direction.Normalize();

        float distance = Vector3.Distance(start, end);

        Vector3 p1 = start + direction * distance * 0.33f;
        Vector3 p2 = start + direction * distance * 0.66f;

        BezierKnot startKnot = new BezierKnot
        {
            Position =
                flySplineContainer.transform.InverseTransformPoint(start),

            TangentIn = Vector3.zero,

            TangentOut =
                flySplineContainer.transform
                    .InverseTransformDirection(p1 - start)
        };

        BezierKnot endKnot = new BezierKnot
        {
            Position =
                flySplineContainer.transform.InverseTransformPoint(end),

            TangentIn =
                flySplineContainer.transform
                    .InverseTransformDirection(p2 - end),

            TangentOut = Vector3.zero
        };

        spline.Add(startKnot);
        spline.Add(endKnot);

        spline.SetTangentMode(0, TangentMode.Mirrored);
        spline.SetTangentMode(1, TangentMode.Mirrored);

        flySplineContainer.Spline = spline;
    }

    private void RotateCardOnCircle(
        Vector3 position,
        Vector3 tangent)
    {
        // Local Z points upward.
        Vector3 z = Vector3.up;

        // Local Y points outward from the circle.
        Vector3 y =
            position - circleSpline.transform.position;

        y.y = 0f;

        if (y.sqrMagnitude < 0.001f)
            return;

        y.Normalize();

        // Local X follows the circle tangent.
        Vector3 x =
            Vector3.Cross(y, z).normalized;

        tangent.y = 0f;

        if (tangent.sqrMagnitude > 0.001f)
        {
            tangent.Normalize();

            if (Vector3.Dot(x, tangent) < 0f)
                x = -x;
        }

        y = Vector3.Cross(x, z).normalized;

        // X = tangent
        // Y = outward
        // Z = up
        card.rotation = Quaternion.LookRotation(z, y);
    }

    private float FindClosestPointOnCircle(Vector3 position)
    {
        const int samples = 100;

        float closestT = 0f;
        float closestDistance = float.MaxValue;

        for (int i = 0; i <= samples; i++)
        {
            float t = (float)i / samples;

            Vector3 point =
                circleSpline.EvaluatePosition(t);

            float distance =
                (point - position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestT = t;
            }
        }

        return closestT;
    }

    private void LookAtTarget()
    {
        Vector3 direction =
            target.position - card.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        card.rotation = Quaternion.LookRotation(
            direction.normalized,
            Vector3.up
        );
    }
}