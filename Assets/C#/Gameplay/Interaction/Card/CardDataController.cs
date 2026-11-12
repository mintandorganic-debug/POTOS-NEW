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
        GameLog.Write(
            "CardDataController",
            $"RandomData begin netId={netId} types={(availableCardTypes == null ? -1 : availableCardTypes.Count)}");

        if (availableCardTypes == null ||
            availableCardTypes.Count == 0)
        {
            GameLog.Write(
                "CardDataController",
                $"RandomData failed netId={netId}: availableCardTypes is empty");

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
            GameLog.Write(
                "CardDataController",
                $"RandomData failed netId={netId}: CardData at index {index} is null");

            Debug.LogError(
                $"CardDataController: CardData at index {index} is null.",
                this);

            return;
        }

        cardDataIndex = index;

        GameLog.Write(
            "CardDataController",
            $"RandomData netId={netId} index={index}/{availableCardTypes.Count} data={selectedData.name}");

        ApplyCardData();
    }

    private void OnCardDataChanged(
        int oldValue,
        int newValue)
    {
        GameLog.Write(
            "CardDataController",
            $"OnCardDataChanged netId={netId} {oldValue}->{newValue}");

        ApplyCardData();
    }

    private void ApplyCardData()
    {
        if (availableCardTypes == null ||
            cardDataIndex < 0 ||
            cardDataIndex >= availableCardTypes.Count)
        {
            GameLog.Write(
                "CardDataController",
                $"ApplyCardData skipped netId={netId} index={cardDataIndex} types={(availableCardTypes == null ? -1 : availableCardTypes.Count)}");

            return;
        }

        CardData newData =
            availableCardTypes[cardDataIndex];

        if (newData == null)
        {
            GameLog.Write(
                "CardDataController",
                $"ApplyCardData skipped netId={netId} index={cardDataIndex}: CardData is NULL");

            return;
        }

        cardData = newData;

        GameLog.Write(
            "CardDataController",
            $"ApplyCardData netId={netId} index={cardDataIndex} data={cardData.name}");

        ApplyCardVisual();

        DataChanged?.Invoke(cardData);
    }

    private void ApplyCardVisual()
    {
        if (cardData == null ||
            cardMesh == null)
        {
            GameLog.Write(
                "CardDataController",
                $"ApplyCardVisual skipped netId={netId}: data={(cardData == null ? "NULL" : cardData.name)} mesh={(cardMesh == null ? "NULL" : "ok")}");

            return;
        }

        Material[] materials =
            cardMesh.materials;

        if (materials == null ||
            materials.Length <= 1)
        {
            GameLog.Write(
                "CardDataController",
                $"ApplyCardVisual skipped netId={netId}: materials={(materials == null ? -1 : materials.Length)}");

            return;
        }

        Material material =
            materials[1];

        if (cardData.texture == null)
        {
            GameLog.Write(
                "CardDataController",
                $"ApplyCardVisual skipped netId={netId}: texture is NULL for data={cardData.name}");

            return;
        }

        GameLog.Write(
            "CardDataController",
            $"ApplyCardVisual netId={netId} texture={cardData.texture.name}");

        material.SetTexture(
            "_BaseColorMap",
            cardData.texture);

        material.SetTexture(
            "_EmissiveColorMap",
            cardData.texture);
    }

    public override void OnStartClient()
    {
        GameLog.Write(
            "CardDataController",
            $"OnStartClient netId={netId} index={cardDataIndex}");

        base.OnStartClient();

        ApplyCardData();
    }
}