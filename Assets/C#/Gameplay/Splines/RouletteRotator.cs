using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class RouletteRotator : NetworkBehaviour
{
    public enum Pattern
    {
        Direct,
        Vortex
    }

    public static RouletteRotator Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform roulettePivot;
    [SerializeField] private Transform cardMarker;
    [SerializeField] private Transform defenseCardMarker;
    [SerializeField] private Animator liftAnimator;

    [Header("Movement")]
    [SerializeField] private float normalSpeed = 360f;
    [SerializeField] private float springSpeed = 900f;

    [Header("Inertia")]
    [SerializeField] private float acceleration = 720f;
    [SerializeField] private float deceleration = 180f;

    [Header("Target Slowdown")]
    [SerializeField] private float slowdownAngle = 35f;
    [SerializeField] private float stopAngle = 1f;
    [SerializeField] private float minSpeed = 120f;

    [Header("Initial Rotation")]
    [SerializeField] private float initialRounds = 1f;

    [Header("Vortex")]
    [SerializeField] private float vortexRounds = 5f;
    [SerializeField] private float vortexRadiusThreshold = 0.5f;
    [SerializeField] private float vortexDepth = -0.5f;
    [SerializeField] private float vortexDepthSpeed = 0.35f;
    [SerializeField] private float vortexSpeedMultiplier = 0.45f;

    private Coroutine rouletteCoroutine;

    private Transform target;
    private Transform attachedCard;

    private Action onCompleted;

    private float currentAngularSpeed;
    private Pattern currentPattern;

    private Vector3 defaultCardMarkerLocalPosition;

    public bool IsRunning =>
        rouletteCoroutine != null;

    public bool HasTarget =>
        target != null;

    public Pattern CurrentPattern =>
        currentPattern;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (roulettePivot == null)
            roulettePivot = transform;

        if (cardMarker != null)
        {
            defaultCardMarkerLocalPosition =
                cardMarker.localPosition;
        }

        ValidateMarkerHierarchy();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void ValidateMarkerHierarchy()
    {
        if (cardMarker == null ||
            defenseCardMarker == null)
        {
            return;
        }

        if (defenseCardMarker.parent != cardMarker)
        {
            Debug.LogWarning(
                "RouletteRotator: defenseCardMarker must be a child of cardMarker.",
                this);
        }
    }

    // =========================================================
    // SERVER API
    // =========================================================

    [Server]
    public void StartRoulette(
        Transform attacker,
        Pattern pattern = Pattern.Direct,
        Action onCompleted = null)
    {
        if (attacker == null)
            return;

        Transform inheritedTarget = null;

        if (pattern == Pattern.Vortex)
        {
            inheritedTarget = target;
        }

        StopRoulette();

        ResetMarker();

        target = inheritedTarget;

        this.onCompleted = onCompleted;
        currentPattern = pattern;

        if (currentPattern == Pattern.Vortex &&
            target != null)
        {
            RotateTowardTarget(target);
        }

        rouletteCoroutine =
            StartCoroutine(
                RouletteRoutine());
    }

    [Server]
    public void SetTarget(
        Transform target,
        Action onCompleted)
    {
        if (!IsRunning ||
            currentPattern == Pattern.Vortex ||
            target == null)
        {
            return;
        }

        if (this.target != null)
            return;

        this.target = target;
        this.onCompleted = onCompleted;
    }

    [Server]
    public void StopRoulette()
    {
        if (rouletteCoroutine != null)
        {
            StopCoroutine(rouletteCoroutine);
            rouletteCoroutine = null;
        }

        target = null;
        onCompleted = null;

        currentAngularSpeed = 0f;
        currentPattern = Pattern.Direct;

        ResetMarker();
    }

    // =========================================================
    // CARD
    // =========================================================

    [Server]
    public void AttachCard(
        Transform card)
    {
        if (card == null ||
            cardMarker == null)
        {
            return;
        }

        AttachCardInternal(
            card,
            cardMarker);
    }

    [Server]
    public void AttachDefenseCard(
        Transform card)
    {
        if (card == null ||
            defenseCardMarker == null)
        {
            return;
        }

        AttachCardInternal(
            card,
            defenseCardMarker);
    }

    [Server]
    private void AttachCardInternal(
        Transform card,
        Transform marker)
    {
        Card cardComponent =
            card.GetComponent<Card>();

        if (cardComponent != null)
        {
            cardComponent.OnAttachmentToRoulette();
        }

        attachedCard = card;

        card.SetParent(
            marker,
            false);

        card.localPosition =
            Vector3.zero;

        card.localRotation =
            Quaternion.identity;

        card.localScale =
            Vector3.one;
    }

    [Server]
    public void DetachCard(
        Transform card)
    {
        if (card == null)
            return;

        if (attachedCard == card)
        {
            attachedCard = null;
        }

        card.SetParent(
            null,
            true);
    }

    // =========================================================
    // MARKER
    // =========================================================

    private void ResetMarker()
    {
        if (cardMarker == null)
            return;

        cardMarker.localPosition =
            defaultCardMarkerLocalPosition;
    }

    // =========================================================
    // RADIUS
    // =========================================================

    public float GetRadius()
    {
        if (cardMarker == null ||
            roulettePivot == null)
        {
            return 0f;
        }

        Vector3 offset =
            cardMarker.position -
            roulettePivot.position;

        offset.y = 0f;

        return offset.magnitude;
    }

    // =========================================================
    // MAIN ROUTINE
    // =========================================================

    [Server]
    private IEnumerator RouletteRoutine()
    {
        yield return FullCircle();

        if (currentPattern == Pattern.Vortex)
        {
            yield return MoveVortex();

            currentAngularSpeed = 0f;
            rouletteCoroutine = null;

            Action callback =
                onCompleted;

            onCompleted = null;
            target = null;
            currentPattern = Pattern.Direct;

            callback?.Invoke();

            yield break;
        }

        while (target == null)
        {
            currentAngularSpeed =
                MoveSpeedWithInertia(
                    normalSpeed);

            float rotation =
                currentAngularSpeed *
                Time.deltaTime;

            Rotate(
                rotation,
                true);

            yield return null;
        }

        Transform directTarget =
            target;

        yield return MoveToTarget(
            directTarget);

        currentAngularSpeed = 0f;
        rouletteCoroutine = null;

        Action directCallback =
            onCompleted;

        onCompleted = null;

        directCallback?.Invoke();
    }

    // =========================================================
    // INITIAL FULL CIRCLE
    // =========================================================

    [Server]
    private IEnumerator FullCircle()
    {
        float remainingAngle =
            initialRounds * 360f;

        while (remainingAngle > 0f)
        {
            currentAngularSpeed =
                MoveSpeedWithInertia(
                    normalSpeed);

            float rotation =
                currentAngularSpeed *
                Time.deltaTime;

            rotation =
                Mathf.Min(
                    rotation,
                    remainingAngle);

            Rotate(
                rotation,
                true);

            remainingAngle -=
                rotation;

            yield return null;
        }
    }

    // =========================================================
    // DIRECT
    // =========================================================

    [Server]
    private IEnumerator MoveToTarget(
        Transform movementTarget)
    {
        float remainingAngle =
            GetAngleToTarget(
                movementTarget,
                true);

        while (remainingAngle > stopAngle)
        {
            float speedMultiplier = 1f;

            if (remainingAngle < slowdownAngle)
            {
                speedMultiplier =
                    Mathf.InverseLerp(
                        stopAngle,
                        slowdownAngle,
                        remainingAngle);
            }

            float desiredSpeed =
                springSpeed *
                speedMultiplier;

            currentAngularSpeed =
                MoveSpeedWithInertia(
                    desiredSpeed);

            if (remainingAngle <= slowdownAngle &&
                currentAngularSpeed < minSpeed)
            {
                currentAngularSpeed =
                    minSpeed;
            }

            float rotation =
                currentAngularSpeed *
                Time.deltaTime;

            rotation =
                Mathf.Min(
                    rotation,
                    remainingAngle);

            Rotate(
                rotation,
                true);

            remainingAngle =
                GetAngleToTarget(
                    movementTarget,
                    true);

            yield return null;
        }

        currentAngularSpeed = 0f;

        SnapToTarget(
            movementTarget);
    }

    // =========================================================
    // VORTEX
    // =========================================================

    [Server]
    private IEnumerator MoveVortex()
    {
        if (roulettePivot == null ||
            cardMarker == null)
        {
            yield break;
        }

        float startRadius =
            GetRadius();

        if (startRadius < 0.0001f)
            yield break;

        float startLocalY =
            cardMarker.localPosition.y;

        float targetLocalY =
            startLocalY +
            vortexDepth;

        float totalAngle =
            Mathf.Max(
                1f,
                vortexRounds) *
            360f;

        float passedAngle = 0f;

        bool depthStarted = false;

        if (liftAnimator != null)
        {
            liftAnimator.SetTrigger(
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
                normalSpeed *
                vortexSpeedMultiplier;

            currentAngularSpeed =
                MoveSpeedWithInertia(
                    desiredSpeed);

            float minimumSpeed =
                minSpeed *
                vortexSpeedMultiplier;

            if (currentAngularSpeed <
                minimumSpeed)
            {
                currentAngularSpeed =
                    minimumSpeed;
            }

            float rotation =
                currentAngularSpeed *
                Time.deltaTime;

            rotation =
                Mathf.Min(
                    rotation,
                    totalAngle -
                    passedAngle);

            Rotate(
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
                cardMarker.position -
                roulettePivot.position;

            offset.y = 0f;

            if (offset.sqrMagnitude >
                0.0001f)
            {
                offset.Normalize();

                Vector3 worldPosition =
                    roulettePivot.position +
                    offset * desiredRadius;

                Vector3 localPosition =
                    roulettePivot.InverseTransformPoint(
                        worldPosition);

                localPosition.y =
                    cardMarker.localPosition.y;

                cardMarker.localPosition =
                    localPosition;
            }

            // -------------------------------------------------
            // DEPTH
            // -------------------------------------------------

            if (!depthStarted &&
                desiredRadius <= vortexRadiusThreshold)
            {
                depthStarted = true;
            }

            if (depthStarted)
            {
                Vector3 localPosition =
                    cardMarker.localPosition;

                localPosition.y =
                    Mathf.MoveTowards(
                        localPosition.y,
                        targetLocalY,
                        vortexDepthSpeed *
                        Time.deltaTime);

                cardMarker.localPosition =
                    localPosition;
            }

            yield return null;
        }

        // -----------------------------------------------------
        // FINAL POSITION
        // -----------------------------------------------------

        Vector3 finalLocalPosition =
            cardMarker.localPosition;

        finalLocalPosition.x = 0f;
        finalLocalPosition.z = 0f;
        finalLocalPosition.y = targetLocalY;

        cardMarker.localPosition =
            finalLocalPosition;

        currentAngularSpeed = 0f;
    }

    // =========================================================
    // INERTIA
    // =========================================================

    private float MoveSpeedWithInertia(
        float desiredSpeed)
    {
        float rate =
            desiredSpeed >
            currentAngularSpeed
                ? acceleration
                : deceleration;

        return Mathf.MoveTowards(
            currentAngularSpeed,
            desiredSpeed,
            rate * Time.deltaTime);
    }

    // =========================================================
    // ROTATION
    // =========================================================

    private void Rotate(
        float degrees,
        bool counterClockwise)
    {
        roulettePivot.Rotate(
            Vector3.up,
            counterClockwise
                ? degrees
                : -degrees,
            Space.Self);
    }

    // =========================================================
    // ROTATE TOWARD TARGET
    // =========================================================

    [Server]
    private void RotateTowardTarget(
        Transform movementTarget)
    {
        if (roulettePivot == null ||
            cardMarker == null ||
            movementTarget == null)
        {
            return;
        }

        Vector3 markerDirection =
            cardMarker.position -
            roulettePivot.position;

        Vector3 targetDirection =
            movementTarget.position -
            roulettePivot.position;

        markerDirection.y = 0f;
        targetDirection.y = 0f;

        if (markerDirection.sqrMagnitude < 0.0001f ||
            targetDirection.sqrMagnitude < 0.0001f)
        {
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

        roulettePivot.Rotate(
            Vector3.up,
            delta,
            Space.World);
    }

    // =========================================================
    // ANGLES
    // =========================================================

    private float GetAngleToTarget(
        Transform movementTarget,
        bool counterClockwise)
    {
        if (cardMarker == null ||
            movementTarget == null)
        {
            return 0f;
        }

        Vector3 markerDirection =
            cardMarker.position -
            roulettePivot.position;

        Vector3 targetDirection =
            movementTarget.position -
            roulettePivot.position;

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

    // =========================================================
    // SNAP
    // =========================================================

    [Server]
    private void SnapToTarget(
        Transform movementTarget)
    {
        if (cardMarker == null)
            return;

        Vector3 targetDirection =
            movementTarget.position -
            roulettePivot.position;

        targetDirection.y = 0f;

        if (targetDirection.sqrMagnitude < 0.001f)
            return;

        targetDirection.Normalize();

        float targetAngle =
            Mathf.Atan2(
                targetDirection.x,
                targetDirection.z) *
            Mathf.Rad2Deg;

        float markerAngle =
            GetMarkerAngle();

        float delta =
            Mathf.Repeat(
                targetAngle -
                markerAngle,
                360f);

        roulettePivot.Rotate(
            Vector3.up,
            delta,
            Space.Self);
    }

    private float GetMarkerAngle()
    {
        if (cardMarker == null)
            return 0f;

        Vector3 direction =
            cardMarker.position -
            roulettePivot.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return 0f;

        return Mathf.Atan2(
            direction.x,
            direction.z) *
            Mathf.Rad2Deg;
    }
}