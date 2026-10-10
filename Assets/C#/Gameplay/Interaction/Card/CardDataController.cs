using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CardDataController : NetworkBehaviour
{
    [Header("Setup")]
    [SerializeField]
    private List<CardData> availableCardTypes;

    [Header("Visual")]
    [SerializeField]
    private MeshRenderer cardMesh;

    [SyncVar(hook = nameof(OnCardDataChanged))]
    private int cardDataIndex = -1;

    private CardData cardData;

    public event Action<CardData> DataChanged;

    public CardData Data =>
        cardData;

    public int DataIndex =>
        cardDataIndex;

    [Server]
    public void RandomData()
    {
        if (availableCardTypes == null ||
            availableCardTypes.Count == 0)
        {
            Debug.LogError(
                "CardDataController: availableCardTypes is empty.",
                this);

            return;
        }

        int index =
            UnityEngine.Random.Range(
                0,
                availableCardTypes.Count);

        CardData selectedData =
            availableCardTypes[index];

        if (selectedData == null)
        {
            Debug.LogError(
                $"CardDataController: CardData at index {index} is null.",
                this);

            return;
        }

        cardDataIndex = index;

        ApplyCardData();
    }

    private void OnCardDataChanged(
        int oldValue,
        int newValue)
    {
        ApplyCardData();
    }

    private void ApplyCardData()
    {
        if (availableCardTypes == null ||
            cardDataIndex < 0 ||
            cardDataIndex >= availableCardTypes.Count)
        {
            return;
        }

        CardData newData =
            availableCardTypes[cardDataIndex];

        if (newData == null)
            return;

        cardData = newData;

        ApplyCardVisual();

        DataChanged?.Invoke(cardData);
    }

    private void ApplyCardVisual()
    {
        if (cardData == null ||
            cardMesh == null)
        {
            return;
        }

        Material[] materials =
            cardMesh.materials;

        if (materials == null ||
            materials.Length <= 1)
        {
            return;
        }

        Material material =
            materials[1];

        if (cardData.texture == null)
            return;

        material.SetTexture(
            "_BaseColorMap",
            cardData.texture);

        material.SetTexture(
            "_EmissiveColorMap",
            cardData.texture);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        ApplyCardData();
    }
}