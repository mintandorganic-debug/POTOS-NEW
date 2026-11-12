#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

// Creation of the three physical dice and holding them in the hand.
public class DiceSpawner
{
    private const float DiceScale = 1f / 3f;
    private const float DiceSpacing = 0.03f;

    private readonly List<Rigidbody> dice = new List<Rigidbody>();

    public IList<Rigidbody> Bodies =>
        dice;

    public static Vector3 HoldPosition(
        Transform wrist,
        int index)
    {
        Transform cam =
            Camera.main != null
                ? Camera.main.transform
                : wrist;

        return
            wrist.position +
            Vector3.up * 0.03f +
            cam.right * (index - 1) * DiceSpacing +
            cam.forward * 0.04f;
    }

    public static void SetColliders(
        Rigidbody body,
        bool value)
    {
        foreach (Collider collider in
                 body.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = value;
        }
    }

    public void Spawn()
    {
        Clear();

        GameObject diePrefab =
            Resources.Load<GameObject>(
                "Dice/SingleDie");

        if (diePrefab == null)
        {
            GameLog.Write(
                "DiceSpawner",
                "Spawn failed: SingleDie prefab not found");

            return;
        }

        int layerId =
            DiceCollisionLayers.LayerId;

        for (int i = 0;
             i < 3;
             i++)
        {
            GameObject die =
                Object.Instantiate(
                    diePrefab);

            if (layerId >= 0)
            {
                die.layer = layerId;
            }

            die.transform.localScale *= DiceScale;

            Rigidbody body =
                die.GetComponent<Rigidbody>();

            body.isKinematic = true;

            SetColliders(
                body,
                false);

            dice.Add(
                body);
        }
    }

    public void Hold(
        Transform wrist)
    {
        for (int i = 0;
             i < dice.Count;
             i++)
        {
            Vector3 position =
                HoldPosition(
                    wrist,
                    i);

            dice[i].position = position;
            dice[i].transform.position = position;
        }
    }

    public void Clear()
    {
        foreach (Rigidbody body in dice)
        {
            if (body != null)
            {
                Object.Destroy(
                    body.gameObject);
            }
        }

        dice.Clear();
    }
}
#endif
