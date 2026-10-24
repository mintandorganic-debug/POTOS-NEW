using Mirror;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBank : NetworkBehaviour
{
    [Serializable]
    public struct ChipAmount
    {
        public int denomination;
        public int amount;

        public ChipAmount(
            int denomination,
            int amount)
        {
            this.denomination = denomination;
            this.amount = amount;
        }
    }

    [Header("Available Denominations")]
    [SerializeField]
    private List<int> availableDenominations = new()
    {
        1,
        10,
        50,
        100,
    };

    [Header("Initial Currency")]
    [SerializeField]
    private int initialCoins = 0;

    [SerializeField]
    private int initialChips = 4;

    [Header("UI")]
    [SerializeField]
    private TMP_Text coinsText;

    [SerializeField]
    private TMP_Text chipsText;

    [Header("Money Rain")]
    [SerializeField]
    private PlayerBankVisual visual;

    [SerializeField]
    private float moneyRainDuration = 5f;

    private const int MaxCoins = 30;

    private readonly SyncList<ChipAmount> chips = new();

    [SyncVar(hook = nameof(OnCoinsChanged))]
    private int coins;

    private Coroutine coinsRainRoutine;
    private Coroutine chipsRainRoutine;

    public int Coins =>
        coins;

    public event Action<int, int> CoinsChanged;
    public event Action<int, int> ChipsChanged;

    public IReadOnlyList<int> AvailableDenominations =>
        availableDenominations;

    public override void OnStartServer()
    {
        base.OnStartServer();

        coins =
            Mathf.Clamp(
                initialCoins,
                0,
                MaxCoins);

        foreach (int denomination in availableDenominations)
        {
            if (GetChipIndex(denomination) == -1)
            {
                chips.Add(
                    new ChipAmount(
                        denomination,
                        Mathf.Max(
                            0,
                            initialChips)));
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        chips.Callback += OnChipsChanged;

        RefreshUI();
    }

    public override void OnStopClient()
    {
        chips.Callback -= OnChipsChanged;

        base.OnStopClient();
    }

    #region Coins

    [Server]
    public bool AddCoins(int amount)
    {
        if (amount <= 0)
            return false;

        int newCoins =
            Mathf.Min(
                coins + amount,
                MaxCoins);

        if (newCoins == coins)
            return false;

        coins =
            newCoins;

        return true;
    }

    [Server]
    public bool RemoveCoins(int amount)
    {
        if (amount <= 0)
            return false;

        if (coins < amount)
            return false;

        coins -= amount;

        return true;
    }

    [Server]
    public bool TryTransferCoinsTo(
        PlayerBank recipient,
        int amount)
    {
        if (recipient == null ||
            recipient == this ||
            amount <= 0 ||
            coins < amount ||
            recipient.coins > MaxCoins - amount)
        {
            return false;
        }

        coins -= amount;
        recipient.coins += amount;

        return true;
    }

    [Server]
    public bool TryStealCoinsTo(
        PlayerBank recipient,
        int amount,
        float duration)
    {
        if (recipient == null ||
            recipient == this ||
            amount <= 0 ||
            coins < amount ||
            recipient.coins > MaxCoins - amount)
        {
            return false;
        }

        coins -= amount;
        recipient.StartCoinsRain(
            amount,
            duration);

        return true;
    }

    [Server]
    public bool TrySpendCoins(int amount)
    {
        return RemoveCoins(amount);
    }

    public bool HasCoins(int amount)
    {
        return amount >= 0 &&
               coins >= amount;
    }

    [Server]
    public void StartCoinsRain(
        int amount,
        float duration,
        bool addCoins = true)
    {
        if (amount <= 0)
            return;

        int actualAmount =
            addCoins
                ? Mathf.Min(
                    amount,
                    MaxCoins - coins)
                : amount;

        if (actualAmount <= 0)
            return;

        if (coinsRainRoutine != null)
        {
            StopCoroutine(
                coinsRainRoutine);

            coinsRainRoutine = null;
        }

        RpcPlayCoinsRain(
            actualAmount);

        if (!addCoins ||
            duration <= 0f)
        {
            if (addCoins)
                AddCoins(actualAmount);

            return;
        }

        coinsRainRoutine =
            StartCoroutine(
                CoinsRainRoutine(
                    actualAmount,
                    duration));
    }

    [Server]
    private IEnumerator CoinsRainRoutine(
        int amount,
        float duration)
    {
        float startTime =
            Time.time;

        int addedAmount = 0;

        while (addedAmount < amount)
        {
            float progress =
                Mathf.Clamp01(
                    (Time.time - startTime) /
                    duration);

            int targetAmount =
                Mathf.FloorToInt(
                    progress * amount);

            targetAmount =
                Mathf.Min(
                    targetAmount,
                    amount);

            int delta =
                targetAmount -
                addedAmount;

            if (delta > 0)
            {
                AddCoins(delta);

                addedAmount =
                    targetAmount;
            }

            if (addedAmount >= amount)
                break;

            yield return null;
        }

        coinsRainRoutine = null;
    }

    [ClientRpc]
    private void RpcPlayCoinsRain(
        int count)
    {
        if (visual == null)
            return;

        visual.PlayCoinsRain(count);
    }

    #endregion

    #region Chips

    [Server]
    public bool AddChips(
        int denomination,
        int amount)
    {
        if (amount <= 0)
            return false;

        int index =
            GetChipIndex(denomination);

        if (index == -1)
        {
            Debug.LogWarning(
                $"Unknown chip denomination: {denomination}");

            return false;
        }

        ChipAmount chip =
            chips[index];

        chip.amount += amount;

        chips[index] =
            chip;

        return true;
    }

    [Server]
    public bool RemoveChips(
        int denomination,
        int amount)
    {
        if (amount <= 0)
            return false;

        int index =
            GetChipIndex(denomination);

        if (index == -1)
            return false;

        ChipAmount chip =
            chips[index];

        if (chip.amount < amount)
            return false;

        chip.amount -= amount;

        chips[index] =
            chip;

        return true;
    }

    [Server]
    public bool TrySpendChips(
        int denomination,
        int amount)
    {
        return RemoveChips(
            denomination,
            amount);
    }

    public int GetChips(int denomination)
    {
        int index =
            GetChipIndex(denomination);

        if (index == -1)
            return 0;

        return chips[index].amount;
    }

    public bool HasChips(
        int denomination,
        int amount)
    {
        if (amount < 0)
            return false;

        return GetChips(denomination) >= amount;
    }

    [Server]
    public void StartChipsRain(
        int denomination,
        int amount,
        float duration)
    {
        if (amount <= 0)
            return;

        if (GetChipIndex(denomination) == -1)
        {
            Debug.LogWarning(
                $"Unknown chip denomination: {denomination}");

            return;
        }

        if (chipsRainRoutine != null)
        {
            StopCoroutine(
                chipsRainRoutine);

            chipsRainRoutine = null;
        }

        RpcPlayChipsRain(
            amount);

        if (duration <= 0f)
        {
            AddChips(
                denomination,
                amount);

            return;
        }

        chipsRainRoutine =
            StartCoroutine(
                ChipsRainRoutine(
                    denomination,
                    amount,
                    duration));
    }

    [Server]
    private IEnumerator ChipsRainRoutine(
        int denomination,
        int amount,
        float duration)
    {
        float startTime =
            Time.time;

        int addedAmount = 0;

        while (addedAmount < amount)
        {
            float progress =
                Mathf.Clamp01(
                    (Time.time - startTime) /
                    duration);

            int targetAmount =
                Mathf.FloorToInt(
                    progress * amount);

            targetAmount =
                Mathf.Min(
                    targetAmount,
                    amount);

            int delta =
                targetAmount -
                addedAmount;

            if (delta > 0)
            {
                AddChips(
                    denomination,
                    delta);

                addedAmount =
                    targetAmount;
            }

            if (addedAmount >= amount)
                break;

            yield return null;
        }

        chipsRainRoutine = null;
    }

    [ClientRpc]
    private void RpcPlayChipsRain(
        int count)
    {
        if (visual == null)
            return;

        visual.PlayChipsRain(count);
    }

    #endregion

    #region Total Value

    public int GetTotalChipValue()
    {
        int total = 0;

        foreach (ChipAmount chip in chips)
        {
            total +=
                chip.denomination *
                chip.amount;
        }

        return total;
    }

    public int GetTotalCurrencyValue()
    {
        return coins +
               GetTotalChipValue();
    }

    #endregion

    #region UI

    private void RefreshUI()
    {
        RefreshCoinsUI();
        RefreshChipsUI();
    }

    private void RefreshCoinsUI()
    {
        if (coinsText == null)
            return;

        coinsText.text =
            coins.ToString();
    }

    private void RefreshChipsUI()
    {
        if (chipsText == null)
            return;

        chipsText.text =
            GetTotalChipValue().ToString();
    }

    #endregion

    #region Internal

    private int GetChipIndex(
        int denomination)
    {
        for (int i = 0;
             i < chips.Count;
             i++)
        {
            if (chips[i].denomination ==
                denomination)
            {
                return i;
            }
        }

        return -1;
    }

    private void OnCoinsChanged(
        int oldValue,
        int newValue)
    {
        RefreshCoinsUI();

        CoinsChanged?.Invoke(
            oldValue,
            newValue);
    }

    private void OnChipsChanged(
        SyncList<ChipAmount>.Operation operation,
        int index,
        ChipAmount oldItem,
        ChipAmount newItem)
    {
        if (operation !=
            SyncList<ChipAmount>.Operation.OP_SET)
        {
            return;
        }

        RefreshChipsUI();

        ChipsChanged?.Invoke(
            newItem.denomination,
            newItem.amount);
    }

    #endregion

    #region Editor

    [ContextMenu("Coins Rain 10")]
    private void TestCoinsRain10()
    {
        if (!isServer)
            return;

        StartCoinsRain(
            10,
            moneyRainDuration);
    }

    [ContextMenu("Coins Rain 50")]
    private void TestCoinsRain50()
    {
        if (!isServer)
            return;

        StartCoinsRain(
            50,
            moneyRainDuration);
    }

    [ContextMenu("Coins Rain 100")]
    private void TestCoinsRain100()
    {
        if (!isServer)
            return;

        StartCoinsRain(
            100,
            moneyRainDuration);
    }

    [ContextMenu("Chips Rain 10")]
    private void TestChipsRain10()
    {
        if (!isServer)
            return;

        StartChipsRain(
            1,
            10,
            moneyRainDuration);
    }

    [ContextMenu("Chips Rain 50")]
    private void TestChipsRain50()
    {
        if (!isServer)
            return;

        StartChipsRain(
            1,
            50,
            moneyRainDuration);
    }

    [ContextMenu("Chips Rain 100")]
    private void TestChipsRain100()
    {
        if (!isServer)
            return;

        StartChipsRain(
            1,
            100,
            moneyRainDuration);
    }

    #endregion
}