using UnityEngine;

public interface IRouletteMotion
{
    Transform RoulettePivot { get; }

    Transform CardMarker { get; }

    Transform DefenseCardMarker { get; }

    Vector3 DefaultCardMarkerLocalPosition { get; }

    Animator LiftAnimator { get; }

    float NormalSpeed { get; }

    float MinSpeed { get; }

    float VortexRounds { get; }

    float VortexDepth { get; }

    float VortexRadiusThreshold { get; }

    float VortexDepthSpeed { get; }

    float VortexSpeedMultiplier { get; }

    float StopAngle { get; }

    float SlowdownAngle { get; }

    float SpringSpeed { get; }

    float CurrentAngularSpeed { get; set; }

    float GetRadius();

    float GetMarkerAngle();

    float MoveSpeedWithInertia(
        float desiredSpeed);

    void Rotate(
        float degrees,
        bool counterClockwise);
}
