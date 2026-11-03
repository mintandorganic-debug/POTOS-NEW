using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class RouletteRotator : NetworkBehaviour, IRouletteMotion
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

    private RouletteCardAttachment attachment;

    private RouletteCardAttachment Attachment =>
        attachment ??= new RouletteCardAttachment(
            this);

    private RouletteTargetMover targetMover;

    private RouletteTargetMover TargetMover =>
        targetMover ??= new RouletteTargetMover(
            this);

    private RouletteVortexMover vortexMover;

    private RouletteVortexMover VortexMover =>
        vortexMover ??= new RouletteVortexMover(
            this);

    private Transform target;

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
        Attachment.AttachCard(
            card);
    }

    [Server]
    public void AttachDefenseCard(
        Transform card)
    {
        Attachment.AttachDefenseCard(
            card);
    }


    [Server]
    public void DetachCard(
        Transform card)
    {
        Attachment.DetachCard(
            card);
    }

    // =========================================================
    // MARKER
    // =========================================================

    private void ResetMarker()
    {
        Attachment.ResetMarker();
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
            yield return VortexMover.Run();

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
        return TargetMover.MoveToTarget(
            movementTarget);
    }

    // =========================================================
    // VORTEX
    // =========================================================

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
        TargetMover.RotateTowardTarget(
            movementTarget);
    }

    // =========================================================
    // ANGLES
    // =========================================================


    // =========================================================
    // SNAP
    // =========================================================


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

    Transform IRouletteMotion.RoulettePivot =>
        roulettePivot;

    Transform IRouletteMotion.CardMarker =>
        cardMarker;

    Animator IRouletteMotion.LiftAnimator =>
        liftAnimator;

    float IRouletteMotion.NormalSpeed =>
        normalSpeed;

    float IRouletteMotion.MinSpeed =>
        minSpeed;

    float IRouletteMotion.VortexRounds =>
        vortexRounds;

    float IRouletteMotion.VortexDepth =>
        vortexDepth;

    float IRouletteMotion.VortexRadiusThreshold =>
        vortexRadiusThreshold;

    float IRouletteMotion.VortexDepthSpeed =>
        vortexDepthSpeed;

    float IRouletteMotion.VortexSpeedMultiplier =>
        vortexSpeedMultiplier;

    float IRouletteMotion.CurrentAngularSpeed
    {
        get => currentAngularSpeed;
        set => currentAngularSpeed = value;
    }

    Transform IRouletteMotion.DefenseCardMarker =>
        defenseCardMarker;

    Vector3 IRouletteMotion.DefaultCardMarkerLocalPosition =>
        defaultCardMarkerLocalPosition;

    float IRouletteMotion.StopAngle =>
        stopAngle;

    float IRouletteMotion.SlowdownAngle =>
        slowdownAngle;

    float IRouletteMotion.SpringSpeed =>
        springSpeed;

    float IRouletteMotion.GetMarkerAngle()
    {
        return GetMarkerAngle();
    }

    float IRouletteMotion.MoveSpeedWithInertia(
        float desiredSpeed)
    {
        return MoveSpeedWithInertia(
            desiredSpeed);
    }

    void IRouletteMotion.Rotate(
        float degrees,
        bool counterClockwise)
    {
        Rotate(
            degrees,
            counterClockwise);
    }
}
