using System.Collections.Generic;
using UnityEngine;

public class SpawnpointCalculator : MonoBehaviour
{
    [SerializeField] private float radius = 5f;

    private readonly List<Pose> spawnPoints = new();

    public IReadOnlyList<Pose> SpawnPoints => spawnPoints;

    public void CalculateSpawnPoints(int playerCount)
    {
        spawnPoints.Clear();

        for (int i = 0; i < playerCount; i++)
        {
            float angle = i * Mathf.PI * 2f / playerCount;

            Vector3 position = transform.position + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);

            Quaternion rotation = Quaternion.LookRotation(
                transform.position - position,
                Vector3.up);

            spawnPoints.Add(new Pose(position, rotation));
        }
    }
}