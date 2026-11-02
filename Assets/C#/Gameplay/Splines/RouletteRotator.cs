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
        GameLog.Write(
            "RouletteRotator",
            $"Awake object={name} hasInstance={Instance != null} pivot={(roulettePivot != null ? roulettePivot.name : "NULL")} cardMarker={(cardMarker != null ? cardMarker.name : "NULL")} defenseCardMarker={(defenseCardMarker != null ? defenseCardMarker.name : "NULL")}");

        if (Instance != null &&
            Instance != this)
        {
            GameLog.Write(
                "RouletteRotator",
                "Awake duplicate instance, destroying this object");

            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (roulettePivot == null)
        {
            GameLog.Write(
                "RouletteRotator",
                "Awake roulettePivot not assigned, using own transform");

            roulettePivot = transform;
        }

        if (cardMarker != null)
        {
            defaultCardMarkerLocalPosition =
                cardMarker.localPosition;

            GameLog.Write(
                "RouletteRotator",
                $"Awake defaultCardMarkerLocalPosition={defaultCardMarkerLocalPosition}");
        }

        ValidateMarkerHierarchy();
    }

    private void OnDestroy()
    {
        GameLog.Write(
            "RouletteRotator",
            $"OnDestroy object={name} wasInstance={Instance == this}");

        if (Instance == this)
            Instance = null;
    }

    private void ValidateMarkerHierarchy()
    {
        GameLog.Write(
            "RouletteRotator",
            $"ValidateMarkerHierarchy cardMarker={(cardMarker != null ? cardMarker.name : "NULL")} defenseCardMarker={(defenseCardMarker != null ? defenseCardMarker.name : "NULL")}");

        if (cardMarker == null ||
            defenseCardMarker == null)
        {
            GameLog.Write(
                "RouletteRotator",
                "ValidateMarkerHierarchy skipped: marker is null");

            return;
        }

        if (defenseCardMarker.parent != cardMarker)
        {
            GameLog.Write(
                "RouletteRotator",
                "ValidateMarkerHierarchy warning: defenseCardMarker is not a child of cardMarker");

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
        GameLog.Write(
            "RouletteRotator",
            $"StartRoulette attacker={(attacker != null ? attacker.name : "NULL")} pattern={pattern} hasCallback={onCompleted != null} currentTarget={(target != null ? target.name : "NULL")}");

        if (attacker == null)
        {
            GameLog.Write(
                "RouletteRotator",
                "StartRoulette ignored: attacker is null");

            return;
        }

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

        GameLog.Write(
            "RouletteRotator",
            $"StartRoulette starting routine pattern={currentPattern} target={(target != null ? target.name : "NULL")}");

        rouletteCoroutine =
            StartCoroutine(
                RouletteRoutine());
    }

    [Server]
    public void SetTarget(
        Transform target,
        Action onCompleted)
    {
        GameLog.Write(
            "RouletteRotator",
            $"SetTarget target={(target != null ? target.name : "NULL")} hasCallback={onCompleted != null} isRunning={IsRunning} pattern={currentPattern} currentTarget={(this.target != null ? this.target.name : "NULL")}");

        if (!IsRunning ||
            currentPattern == Pattern.Vortex ||
            target == null)
        {
            GameLog.Write(
                "RouletteRotator",
                $"SetTarget ignored: isRunning={IsRunning} pattern={currentPattern} targetNull={target == null}");

            return;
        }

        if (this.target != null)
        {
            GameLog.Write(
                "RouletteRotator",
                $"SetTarget ignored: target already set currentTarget={this.target.name}");

            return;
        }

        this.target = target;
        this.onCompleted = onCompleted;

        GameLog.Write(
            "RouletteRotator",
            $"SetTarget accepted target={target.name}");
    }

    [Server]
    public void StopRoulette()
    {
        GameLog.Write(
            "RouletteRotator",
            $"StopRoulette wasRunning={rouletteCoroutine != null} pattern={currentPattern} target={(target != null ? target.name : "NULL")} hadCallback={onCompleted != null}");

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

        GameLog.Write(
            "RouletteRotator",
            "StopRoulette done");
    }

    // =========================================================
    // CARD
    // =========================================================

    [Server]
    public void AttachCard(
        Transform card)
    {
        GameLog.Write(
            "RouletteRotator",
            $"AttachCard card={(card != null ? card.name : "NULL")} cardMarker={(cardMarker != null ? cardMarker.name : "NULL")}");

        if (card == null ||
            cardMarker == null)
        {
            GameLog.Write(
                "RouletteRotator",
                "AttachCard skipped: card or cardMarker is null");

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
        GameLog.Write(
            "RouletteRotator",
            $"AttachDefenseCard card={(card != null ? card.name : "NULL")} defenseCardMarker={(defenseCardMarker != null ? defenseCardMarker.name : "NULL")}");

        if (card == null ||
            defenseCardMarker == null)
        {
            GameLog.Write(
                "RouletteRotator",
                "AttachDefenseCard skipped: card or defenseCardMarker is null");

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
        GameLog.Write(
            "RouletteRotator",
            $"AttachCardInternal card={(card != null ? card.name : "NULL")} marker={(marker != null ? marker.name : "NULL")}");

        Card cardComponent =
            card.GetComponent<Card>();

        if (cardComponent != null)
        {
            GameLog.Write(
                "RouletteRotator",
                $"AttachCardInternal calling OnAttachmentToRoulette card={card.name}");

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
        GameLog.Write(
            "RouletteRotator",
            $"DetachCard card={(card != null ? card.name : "NULL")} wasAttachedCard={attachedCard == card}");

        if (card == null)
        {
            GameLog.Write(
                "RouletteRotator",
                "DetachCard skipped: card is null");

            return;
        }

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
        GameLog.Write(
            "RouletteRotator",
            $"ResetMarker cardMarker={(cardMarker != null ? cardMarker.name : "NULL")} defaultLocalPosition={defaultCardMarkerLocalPosition}");

        if (cardMarker == null)
        {
            GameLog.Write(
                "RouletteRotator",
                "ResetMarker skipped: cardMarker is null");

            return;
        }

        cardMarker.localPosition =
            defaultCardMarkerLocalPosition;
    }

    // =========================================================
    // RADIUS
    // =========================================================

    public float GetRadius()
    {
        GameLog.Write(
            "RouletteRotator",
            $"GetRadius cardMarker={(cardMarker != null ? cardMarker.name : "NULL")} pivot={(roulettePivot != null ? roulettePivot.name : "NULL")}");

        if (cardMarker == null ||
            roulettePivot == null)
        {
            GameLog.Write(
                "RouletteRotator",
                "GetRadius returning 0: cardMarker or pivot is null");

            return 0f;
        }

        Vector3 offset =
            cardMarker.position -
            roulettePivot.position;

        offset.y = 0f;

        GameLog.Write(
            "RouletteRotator",
            $"GetRadius result radius={offset.magnitude}");

        return offset.magnitude;
    }

    // =========================================================
    // MAIN ROUTINE
    // =========================================================

    [Server]
    private IEnumerator RouletteRoutine()
    {
        GameLog.Write(
            "RouletteRotator",
            $"RouletteRoutine start pattern={currentPattern} target={(target != null ? target.name : "NULL")}");

        yield return FullCircle();

        GameLog.Write(
            "RouletteRotator",
            $"RouletteRoutine full circle done pattern={currentPattern}");

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

            GameLog.Write(
                "RouletteRotator",
                $"RouletteRoutine vortex completed callback={(callback != null)}");

            callback?.Invoke();

            GameLog.Write(
                "RouletteRotator",
                "RouletteRoutine vortex callback invoked, routine end");

            yield break;
        }

        GameLog.Write(
            "RouletteRotator",
            $"RouletteRoutine waiting for target target={(target != null ? target.name : "NULL")}");

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

        GameLog.Write(
            "RouletteRotator",
            $"RouletteRoutine target acquired directTarget={(directTarget != null ? directTarget.name : "NULL")}");

        yield return MoveToTarget(
            directTarget);

        currentAngularSpeed = 0f;
        rouletteCoroutine = null;

        Action directCallback =
            onCompleted;

        onCompleted = null;

        GameLog.Write(
            "RouletteRotator",
            $"RouletteRoutine direct completed callback={(directCallback != null)}");

        directCallback?.Invoke();

        GameLog.Write(
            "RouletteRotator",
            "RouletteRoutine direct callback invoked, routine end");
    }

    // =========================================================
    // INITIAL FULL CIRCLE
    // =========================================================

    [Server]
    private IEnumerator FullCircle()
    {
        GameLog.Write(
            "RouletteRotator",
            $"FullCircle start initialRounds={initialRounds}");

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

        GameLog.Write(
            "RouletteRotator",
            "FullCircle finished");
    }

    // =========================================================
    // DIRECT
    // =========================================================

    [Server]
    private IEnumerator MoveToTarget(
        Transform movementTarget)
    {
        GameLog.Write(
            "RouletteRotator",
            $"MoveToTarget start movementTarget={(movementTarget != null ? movementTarget.name : "NULL")}");

        float remainingAngle =
            GetAngleToTarget(
                movementTarget,
                true);

        GameLog.Write(
            "RouletteRotator",
            $"MoveToTarget initial remainingAngle={remainingAngle}");

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

        GameLog.Write(
            "RouletteRotator",
            $"MoveToTarget reached target remainingAngle={remainingAngle}");

        SnapToTarget(
            movementTarget);
    }

    // =========================================================
    // VORTEX
    // =========================================================

    [Server]
    private IEnumerator MoveVortex()
    {
        GameLog.Write(
            "RouletteRotator",
            $"MoveVortex start pivot={(roulettePivot != null ? roulettePivot.name : "NULL")} cardMarker={(cardMarker != null ? cardMarker.name : "NULL")}");

        if (roulettePivot == null ||
            cardMarker == null)
        {
            GameLog.Write(
                "RouletteRotator",
                "MoveVortex aborted: pivot or cardMarker is null");

            yield break;
        }

        float startRadius =
            GetRadius();

        if (startRadius < 0.0001f)
        {
            GameLog.Write(
                "RouletteRotator",
                $"MoveVortex aborted: startRadius too small startRadius={startRadius}");

            yield break;
        }

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

        GameLog.Write(
            "RouletteRotator",
            $"MoveVortex spiral start startRadius={startRadius} startLocalY={startLocalY} targetLocalY={targetLocalY} totalAngle={totalAngle} liftAnimator={(liftAnimator != null ? liftAnimator.name : "NULL")}");

        if (liftAnimator != null)
        {
            GameLog.Write(
                "RouletteRotator",
                "MoveVortex trigger DropEmpty");

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

                GameLog.Write(
                    "RouletteRotator",
                    $"MoveVortex depth started desiredRadius={desiredRadius} passedAngle={passedAngle}");
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

        GameLog.Write(
            "RouletteRotator",
            $"MoveVortex finished finalLocalPosition={finalLocalPosition} passedAngle={passedAngle}");
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
        GameLog.Write(
            "RouletteRotator",
            $"RotateTowardTarget movementTarget={(movementTarget != null ? movementTarget.name : "NULL")}");

        if (roulettePivot == null ||
            cardMarker == null ||
            movementTarget == null)
        {
            GameLog.Write(
                "RouletteRotator",
                $"RotateTowardTarget skipped: missing reference pivot={(roulettePivot != null ? roulettePivot.name : "NULL")} cardMarker={(cardMarker != null ? cardMarker.name : "NULL")} target={(movementTarget != null ? movementTarget.name : "NULL")}");

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
            GameLog.Write(
                "RouletteRotator",
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
            "RouletteRotator",
            $"RotateTowardTarget rotating delta={delta}");

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
        GameLog.Write(
            "RouletteRotator",
            $"SnapToTarget movementTarget={(movementTarget != null ? movementTarget.name : "NULL")} cardMarker={(cardMarker != null ? cardMarker.name : "NULL")}");

        if (cardMarker == null)
        {
            GameLog.Write(
                "RouletteRotator",
                "SnapToTarget skipped: cardMarker is null");

            return;
        }

        Vector3 targetDirection =
            movementTarget.position -
            roulettePivot.position;

        targetDirection.y = 0f;

        if (targetDirection.sqrMagnitude < 0.001f)
        {
            GameLog.Write(
                "RouletteRotator",
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
            GetMarkerAngle();

        float delta =
            Mathf.Repeat(
                targetAngle -
                markerAngle,
                360f);

        GameLog.Write(
            "RouletteRotator",
            $"SnapToTarget rotating delta={delta} targetAngle={targetAngle} markerAngle={markerAngle}");

        roulettePivot.Rotate(
            Vector3.up,
            delta,
            Space.Self);
    }

    private float GetMarkerAngle()
    {
        GameLog.Write(
            "RouletteRotator",
            $"GetMarkerAngle cardMarker={(cardMarker != null ? cardMarker.name : "NULL")}");

        if (cardMarker == null)
        {
            GameLog.Write(
                "RouletteRotator",
                "GetMarkerAngle returning 0: cardMarker is null");

            return 0f;
        }

        Vector3 direction =
            cardMarker.position -
            roulettePivot.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            GameLog.Write(
                "RouletteRotator",
                "GetMarkerAngle returning 0: marker is at pivot");

            return 0f;
        }

        return Mathf.Atan2(
            direction.x,
            direction.z) *
            Mathf.Rad2Deg;
    }
}