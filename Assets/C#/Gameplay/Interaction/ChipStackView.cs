using System.Collections.Generic;
using UnityEngine;

public class ChipStackView : MonoBehaviour
{
    private Transform chipPrefab;
    private float chipHeight;

    private readonly List<Transform> chips = new();

    public void Initialize(
        Transform chipPrefab,
        float chipHeight)
    {
        GameLog.Write(
            "ChipStackView",
            $"Initialize object={name} chipPrefab={(chipPrefab != null ? chipPrefab.name : "NULL")} chipHeight={chipHeight}");

        this.chipPrefab = chipPrefab;
        this.chipHeight = chipHeight;
    }

    public void SetAmount(int amount)
    {
        GameLog.Write(
            "ChipStackView",
            $"SetAmount object={name} amount={amount} existingChips={chips.Count}");

        amount = Mathf.Max(0, amount);

        while (chips.Count < amount)
        {
            Transform chip =
                Instantiate(
                    chipPrefab,
                    transform);

            chips.Add(chip);
        }

        GameLog.Write(
            "ChipStackView",
            $"SetAmount object={name} amount={amount} totalChips={chips.Count}");

        for (int i = 0; i < chips.Count; i++)
        {
            bool active =
                i < amount;

            chips[i].gameObject.SetActive(active);

            if (active)
            {
                chips[i].localPosition =
                    Vector3.up * (i * chipHeight);

                chips[i].localRotation =
                    Quaternion.identity;
            }
        }
    }
}