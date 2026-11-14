using System.Collections.Generic;
using UnityEngine;

// Creation and reset of the three physical dice (original: InitializeDice).
public class DiceBodies
{
    private readonly List<GameObject> dice = new List<GameObject>();

    public IReadOnlyList<GameObject> Dice =>
        dice;

    public float[] ForceMultipliers { get; } = { 1f, 1f, 1f };

    public void Initialize(
        GameObject diePrefab,
        IList<GameObject> controlBones,
        float forceVariation)
    {
        if (dice.Count == 0)
        {
            dice.Add(Object.Instantiate(diePrefab, controlBones[0].transform.position, Quaternion.identity));
            dice.Add(Object.Instantiate(diePrefab, controlBones[1].transform.position, Quaternion.identity));
            dice.Add(Object.Instantiate(diePrefab, controlBones[2].transform.position, Quaternion.identity));
        }

        foreach (GameObject d in dice)
        {
            // Preserved from the original: the counter is declared inside the loop,
            // so every die is placed on bone 0 and writes ForceMultipliers[0].
            int count = 0;

            d.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            d.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            d.GetComponent<Rigidbody>().isKinematic = true;
            d.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
            d.layer = LayerMask.NameToLayer("Ignore Raycast");
            d.GetComponent<Rigidbody>().position = controlBones[count].transform.position;
            d.layer = LayerMask.NameToLayer("DiceSimulation");
            d.GetComponent<Collider>().enabled = true;
            d.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            d.GetComponent<Rigidbody>().isKinematic = false;
            d.GetComponent<Rigidbody>().rotation = controlBones[count].transform.rotation;
            d.transform.rotation = Quaternion.identity;
            d.transform.GetChild(0).rotation = Quaternion.identity;
            ForceMultipliers[count] = 1f + Random.Range(0f, forceVariation);
            count += 1;
        }
    }
}
