using System.Collections.Generic;
using UnityEngine;

public class ParticleAttractor : MonoBehaviour
{
    [SerializeField] private ParticleSystem particleSystem;
    [SerializeField] private Transform attractPoint;
    [SerializeField] private Transform cardTransform;

    [Header("Movement")]
    [SerializeField] private float attractStrength = 2f;
    [SerializeField] private float orbitStrength = 5f;

    [Header("Life")]
    [SerializeField] private float attractionByLife = 4f;
    [SerializeField] private float orbitFadeByLife = 1f;

    private ParticleSystem.Particle[] particles;

    private readonly Dictionary<uint, Vector3> startPositions = new();

    private void LateUpdate()
    {
        if (particleSystem == null || attractPoint == null || cardTransform == null)
            return;

        int maxParticles = particleSystem.main.maxParticles;

        if (particles == null || particles.Length < maxParticles)
            particles = new ParticleSystem.Particle[maxParticles];

        int count = particleSystem.GetParticles(particles);

        ParticleSystem.MainModule main = particleSystem.main;

        for (int i = 0; i < count; i++)
        {
            uint id = particles[i].randomSeed;

            Vector3 particlePosition = GetWorldPosition(
                particles[i].position,
                main
            );

            if (!startPositions.TryGetValue(id, out Vector3 startPosition))
            {
                startPosition = particlePosition;
                startPositions.Add(id, startPosition);
            }

            Vector3 endPosition = attractPoint.position;

            float life01 =
                1f - particles[i].remainingLifetime /
                particles[i].startLifetime;

            Vector3 sphereCenter =
                (startPosition + endPosition) * 0.5f;

            Vector3 startFromCenter =
                startPosition - sphereCenter;

            float radius = startFromCenter.magnitude;

            if (radius <= 0.001f)
                continue;

            Vector3 sphereForward =
                startFromCenter.normalized;

            Vector3 chordDirection =
                (endPosition - startPosition).normalized;

            float randomAngle =
                Hash01(id) * 360f;

            Quaternion randomRotation =
                Quaternion.AngleAxis(randomAngle, chordDirection);

            Vector3 referenceDirection =
                randomRotation * cardTransform.up;

            Vector3 sphereSide =
                Vector3.ProjectOnPlane(
                    referenceDirection,
                    sphereForward
                ).normalized;

            if (sphereSide.sqrMagnitude < 0.001f)
            {
                sphereSide =
                    Vector3.Cross(
                        sphereForward,
                        cardTransform.right
                    ).normalized;
            }

            float angle = life01 * Mathf.PI;

            Vector3 sphericalPosition =
                sphereCenter +
                sphereForward * Mathf.Cos(angle) * radius +
                sphereSide * Mathf.Sin(angle) * radius;

            Vector3 toSpherePath =
                sphericalPosition - particlePosition;

            Vector3 toAttractionPoint =
                endPosition - particlePosition;

            float currentOrbitStrength =
                Mathf.Lerp(
                    orbitStrength,
                    orbitStrength * orbitFadeByLife,
                    life01
                );

            float currentAttractStrength =
                attractStrength +
                attractionByLife * life01;

            Vector3 velocity =
                toSpherePath.normalized * currentOrbitStrength +
                toAttractionPoint.normalized * currentAttractStrength;

            particles[i].velocity =
                WorldToSimulationDirection(
                    velocity,
                    main
                );
        }

        particleSystem.SetParticles(particles, count);

        CleanupDeadParticles(count);
    }

    private Vector3 GetWorldPosition(
        Vector3 position,
        ParticleSystem.MainModule main)
    {
        if (main.simulationSpace ==
            ParticleSystemSimulationSpace.Local)
        {
            return particleSystem.transform.TransformPoint(position);
        }

        return position;
    }

    private Vector3 WorldToSimulationDirection(
        Vector3 direction,
        ParticleSystem.MainModule main)
    {
        if (main.simulationSpace ==
            ParticleSystemSimulationSpace.Local)
        {
            return particleSystem.transform
                .InverseTransformDirection(direction);
        }

        return direction;
    }

    private float Hash01(uint seed)
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;

        return (seed & 0x00FFFFFF) /
               16777215f;
    }

    private void CleanupDeadParticles(int count)
    {
        if (startPositions.Count == 0)
            return;

        HashSet<uint> aliveParticles = new();

        for (int i = 0; i < count; i++)
            aliveParticles.Add(particles[i].randomSeed);

        List<uint> particlesToRemove = new();

        foreach (uint id in startPositions.Keys)
        {
            if (!aliveParticles.Contains(id))
                particlesToRemove.Add(id);
        }

        for (int i = 0; i < particlesToRemove.Count; i++)
            startPositions.Remove(particlesToRemove[i]);
    }
}