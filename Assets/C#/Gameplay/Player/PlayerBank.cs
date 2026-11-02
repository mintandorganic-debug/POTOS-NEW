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
        GameLog.Write(
            "PlayerBank",
            $"OnStartServer netId={netId} initialCoins={initialCoins} initialChips={initialChips} maxCoins={MaxCoins} denominations={availableDenominations.Count}");

        base.OnStartServer();

        coins =
            Mathf.Clamp(
                initialCoins,
                0,
                MaxCoins);

        GameLog.Write(
            "PlayerBank",
            $"OnStartServer coins initialized netId={netId} coins={coins}");

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

                GameLog.Write(
                    "PlayerBank",
                    $"OnStartServer chip entry added netId={netId} denomination={denomination} amount={Mathf.Max(0, initialChips)}");
            }
            else
            {
                GameLog.Write(
                    "PlayerBank",
                    $"OnStartServer chip entry already exists netId={netId} denomination={denomination}");
            }
        }
    }

    public override void OnStartClient()
    {
        GameLog.Write(
            "PlayerBank",
            $"OnStartClient netId={netId} coins={coins} chipsEntries={chips.Count} visual={(visual != null ? visual.name : "NULL")}");

        base.OnStartClient();

        chips.Callback += OnChipsChanged;

        RefreshUI();
    }

    public override void OnStopClient()
    {
        GameLog.Write(
            "PlayerBank",
            $"OnStopClient netId={netId}");

        chips.Callback -= OnChipsChanged;

        base.OnStopClient();
    }

    #region Coins

    [Server]
    public bool AddCoins(int amount)
    {
        GameLog.Write(
            "PlayerBank",
            $"AddCoins netId={netId} amount={amount} coins={coins}");

        if (amount <= 0)
        {
            GameLog.Write(
                "PlayerBank",
                $"AddCoins rejected: amount <= 0 netId={netId} amount={amount}");
            return false;
        }

        int newCoins =
            Mathf.Min(
                coins + amount,
                MaxCoins);

        if (newCoins == coins)
        {
            GameLog.Write(
                "PlayerBank",
                $"AddCoins rejected: no change (max reached) netId={netId} coins={coins} max={MaxCoins} amount={amount}");
            return false;
        }

        GameLog.Write(
            "PlayerBank",
            $"AddCoins applied netId={netId} old={coins} new={newCoins} added={newCoins - coins}");

        coins =
            newCoins;

        return true;
    }

    [Server]
    public bool RemoveCoins(int amount)
    {
        GameLog.Write(
            "PlayerBank",
            $"RemoveCoins netId={netId} amount={amount} coins={coins}");

        if (amount <= 0)
        {
            GameLog.Write(
                "PlayerBank",
                $"RemoveCoins rejected: amount <= 0 netId={netId} amount={amount}");
            return false;
        }

        if (coins < amount)
        {
            GameLog.Write(
                "PlayerBank",
                $"RemoveCoins rejected: not enough coins netId={netId} coins={coins} amount={amount}");
            return false;
        }

        GameLog.Write(
            "PlayerBank",
            $"RemoveCoins applied netId={netId} old={coins} new={coins - amount}");

        coins -= amount;

        return true;
    }

    [Server]
    public bool TransferCoinsTo(
        PlayerBank recipient,
        int amount)
    {
        GameLog.Write(
            "PlayerBank",
            $"TransferCoinsTo netId={netId} recipient={(recipient != null ? recipient.netId.ToString() : "NULL")} amount={amount} coins={coins} recipientCoins={(recipient != null ? recipient.coins.ToString() : "NULL")}");

        if (recipient == null ||
            recipient == this ||
            amount <= 0 ||
            coins < amount ||
            recipient.coins > MaxCoins - amount)
        {
            GameLog.Write(
                "PlayerBank",
                $"TransferCoinsTo rejected netId={netId} recipientNull={recipient == null} self={recipient == this} amount={amount} coins={coins} recipientCoins={(recipient != null ? recipient.coins.ToString() : "NULL")} max={MaxCoins}");
            return false;
        }

        GameLog.Write(
            "PlayerBank",
            $"TransferCoinsTo applied netId={netId} recipient={recipient.netId} amount={amount} senderOld={coins} senderNew={coins - amount} recipientOld={recipient.coins} recipientNew={recipient.coins + amount}");

        coins -= amount;
        recipient.coins += amount;

        return true;
    }

    [Server]
    public bool StealCoinsTo(
        PlayerBank recipient,
        int amount,
        float duration)
    {
        GameLog.Write(
            "PlayerBank",
            $"StealCoinsTo netId={netId} recipient={(recipient != null ? recipient.netId.ToString() : "NULL")} amount={amount} duration={duration} coins={coins} recipientCoins={(recipient != null ? recipient.coins.ToString() : "NULL")}");

        if (recipient == null ||
            recipient == this ||
            amount <= 0 ||
            coins < amount ||
            recipient.coins > MaxCoins - amount)
        {
            GameLog.Write(
                "PlayerBank",
                $"StealCoinsTo rejected netId={netId} recipientNull={recipient == null} self={recipient == this} amount={amount} coins={coins} recipientCoins={(recipient != null ? recipient.coins.ToString() : "NULL")} max={MaxCoins}");
            return false;
        }

        GameLog.Write(
            "PlayerBank",
            $"StealCoinsTo applied netId={netId} recipient={recipient.netId} amount={amount} victimOld={coins} victimNew={coins - amount} duration={duration}");

        coins -= amount;
        recipient.StartCoinsRain(
            amount,
            duration);

        return true;
    }

    [Server]
    public bool SpendCoins(int amount)
    {
        GameLog.Write(
            "PlayerBank",
            $"SpendCoins netId={netId} amount={amount} coins={coins}");
        return RemoveCoins(amount);
    }

    public bool HasCoins(int amount)
    {
        GameLog.Write(
            "PlayerBank",
            $"HasCoins netId={netId} amount={amount} coins={coins}");
        return amount >= 0 &&
               coins >= amount;
    }

    [Server]
    public void StartCoinsRain(
        int amount,
        float duration,
        bool addCoins = true)
    {
        GameLog.Write(
            "PlayerBank",
            $"StartCoinsRain netId={netId} amount={amount} duration={duration} addCoins={addCoins} coins={coins}");

        if (amount <= 0)
        {
            GameLog.Write(
                "PlayerBank",
                $"StartCoinsRain rejected: amount <= 0 netId={netId} amount={amount}");
            return;
        }

        int actualAmount =
            addCoins
                ? Mathf.Min(
                    amount,
                    MaxCoins - coins)
                : amount;

        if (actualAmount <= 0)
        {
            GameLog.Write(
                "PlayerBank",
                $"StartCoinsRain rejected: actualAmount <= 0 netId={netId} amount={amount} actualAmount={actualAmount} coins={coins} max={MaxCoins}");
            return;
        }

        if (coinsRainRoutine != null)
        {
            GameLog.Write(
                "PlayerBank",
                $"StartCoinsRain stopping previous rain routine netId={netId}");

            StopCoroutine(
                coinsRainRoutine);

            coinsRainRoutine = null;
        }

        GameLog.Write(
            "PlayerBank",
            $"StartCoinsRain playing rain netId={netId} actualAmount={actualAmount}");

        RpcPlayCoinsRain(
            actualAmount);

        if (!addCoins ||
            duration <= 0f)
        {
            GameLog.Write(
                "PlayerBank",
                $"StartCoinsRain instant (no routine) netId={netId} actualAmount={actualAmount} addCoins={addCoins} duration={duration}");

            if (addCoins)
                AddCoins(actualAmount);

            return;
        }

        GameLog.Write(
            "PlayerBank",
            $"StartCoinsRain starting routine netId={netId} actualAmount={actualAmount} duration={duration}");

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
        GameLog.Write(
            "PlayerBank",
            $"CoinsRainRoutine start netId={netId} amount={amount} duration={duration} coins={coins}");

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

        GameLog.Write(
            "PlayerBank",
            $"CoinsRainRoutine finished netId={netId} amount={amount} added={addedAmount} coins={coins}");

        coinsRainRoutine = null;
    }

    [ClientRpc]
    private void RpcPlayCoinsRain(
        int count)
    {
        GameLog.Write(
            "PlayerBank",
            $"RpcPlayCoinsRain netId={netId} count={count} visual={(visual != null ? visual.name : "NULL")}");

        if (visual == null)
        {
            GameLog.Write(
                "PlayerBank",
                $"RpcPlayCoinsRain skipped: visual is NULL netId={netId}");
            return;
        }

        visual.PlayCoinsRain(count);
    }

    #endregion

    #region Chips

    [Server]
    public bool AddChips(
        int denomination,
        int amount)
    {
        GameLog.Write(
            "PlayerBank",
            $"AddChips netId={netId} denomination={denomination} amount={amount}");

        if (amount <= 0)
        {
            GameLog.Write(
                "PlayerBank",
                $"AddChips rejected: amount <= 0 netId={netId} denomination={denomination} amount={amount}");
            return false;
        }

        int index =
            GetChipIndex(denomination);

        if (index == -1)
        {
            GameLog.Write(
                "PlayerBank",
                $"AddChips rejected: unknown denomination netId={netId} denomination={denomination}");

            Debug.LogWarning(
                $"Unknown chip denomination: {denomination}");

            return false;
        }

        ChipAmount chip =
            chips[index];

        chip.amount += amount;

        GameLog.Write(
            "PlayerBank",
            $"AddChips applied netId={netId} denomination={denomination} old={chip.amount - amount} new={chip.amount}");

        chips[index] =
            chip;

        return true;
    }

    [Server]
    public bool RemoveChips(
        int denomination,
        int amount)
    {
        GameLog.Write(
            "PlayerBank",
            $"RemoveChips netId={netId} denomination={denomination} amount={amount}");

        if (amount <= 0)
        {
            GameLog.Write(
                "PlayerBank",
                $"RemoveChips rejected: amount <= 0 netId={netId} denomination={denomination} amount={amount}");
            return false;
        }

        int index =
            GetChipIndex(denomination);

        if (index == -1)
        {
            GameLog.Write(
                "PlayerBank",
                $"RemoveChips rejected: unknown denomination netId={netId} denomination={denomination}");
            return false;
        }

        ChipAmount chip =
            chips[index];

        if (chip.amount < amount)
        {
            GameLog.Write(
                "PlayerBank",
                $"RemoveChips rejected: not enough chips netId={netId} denomination={denomination} have={chip.amount} amount={amount}");
            return false;
        }

        chip.amount -= amount;

        GameLog.Write(
            "PlayerBank",
            $"RemoveChips applied netId={netId} denomination={denomination} old={chip.amount + amount} new={chip.amount}");

        chips[index] =
            chip;

        return true;
    }

    [Server]
    public bool SpendChips(
        int denomination,
        int amount)
    {
        GameLog.Write(
            "PlayerBank",
            $"SpendChips netId={netId} denomination={denomination} amount={amount}");
        return RemoveChips(
            denomination,
            amount);
    }

    public int GetChips(int denomination)
    {
        GameLog.Write(
            "PlayerBank",
            $"GetChips netId={netId} denomination={denomination}");

        int index =
            GetChipIndex(denomination);

        if (index == -1)
        {
            GameLog.Write(
                "PlayerBank",
                $"GetChips unknown denomination netId={netId} denomination={denomination}");
            return 0;
        }

        return chips[index].amount;
    }

    public bool HasChips(
        int denomination,
        int amount)
    {
        GameLog.Write(
            "PlayerBank",
            $"HasChips netId={netId} denomination={denomination} amount={amount}");

        if (amount < 0)
        {
            GameLog.Write(
                "PlayerBank",
                $"HasChips rejected: amount < 0 netId={netId} denomination={denomination} amount={amount}");
            return false;
        }

        return GetChips(denomination) >= amount;
    }

    [Server]
    public void StartChipsRain(
        int denomination,
        int amount,
        float duration)
    {
        GameLog.Write(
            "PlayerBank",
            $"StartChipsRain netId={netId} denomination={denomination} amount={amount} duration={duration}");

        if (amount <= 0)
        {
            GameLog.Write(
                "PlayerBank",
                $"StartChipsRain rejected: amount <= 0 netId={netId} denomination={denomination} amount={amount}");
            return;
        }

        if (GetChipIndex(denomination) == -1)
        {
            GameLog.Write(
                "PlayerBank",
                $"StartChipsRain rejected: unknown denomination netId={netId} denomination={denomination}");

            Debug.LogWarning(
                $"Unknown chip denomination: {denomination}");

            return;
        }

        if (chipsRainRoutine != null)
        {
            GameLog.Write(
                "PlayerBank",
                $"StartChipsRain stopping previous rain routine netId={netId}");

            StopCoroutine(
                chipsRainRoutine);

            chipsRainRoutine = null;
        }

        GameLog.Write(
            "PlayerBank",
            $"StartChipsRain playing rain netId={netId} amount={amount}");

        RpcPlayChipsRain(
            amount);

        if (duration <= 0f)
        {
            GameLog.Write(
                "PlayerBank",
                $"StartChipsRain instant (no routine) netId={netId} denomination={denomination} amount={amount} duration={duration}");

            AddChips(
                denomination,
                amount);

            return;
        }

        GameLog.Write(
            "PlayerBank",
            $"StartChipsRain starting routine netId={netId} denomination={denomination} amount={amount} duration={duration}");

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
        GameLog.Write(
            "PlayerBank",
            $"ChipsRainRoutine start netId={netId} denomination={denomination} amount={amount} duration={duration}");

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

        GameLog.Write(
            "PlayerBank",
            $"ChipsRainRoutine finished netId={netId} denomination={denomination} amount={amount} added={addedAmount}");

        chipsRainRoutine = null;
    }

    [ClientRpc]
    private void RpcPlayChipsRain(
        int count)
    {
        GameLog.Write(
            "PlayerBank",
            $"RpcPlayChipsRain netId={netId} count={count} visual={(visual != null ? visual.name : "NULL")}");

        if (visual == null)
        {
            GameLog.Write(
                "PlayerBank",
                $"RpcPlayChipsRain skipped: visual is NULL netId={netId}");
            return;
        }

        visual.PlayChipsRain(count);
    }

    #endregion

    #region Total Value

    public int GetTotalChipValue()
    {
        GameLog.Write(
            "PlayerBank",
            $"GetTotalChipValue netId={netId} entries={chips.Count}");

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
        GameLog.Write(
            "PlayerBank",
            $"GetTotalCurrencyValue netId={netId} coins={coins}");
        return coins +
               GetTotalChipValue();
    }

    #endregion

    #region UI

    private void RefreshUI()
    {
        GameLog.Write(
            "PlayerBank",
            $"RefreshUI netId={netId}");

        RefreshCoinsUI();
        RefreshChipsUI();
    }

    private void RefreshCoinsUI()
    {
        GameLog.Write(
            "PlayerBank",
            $"RefreshCoinsUI netId={netId} coins={coins} text={(coinsText != null ? "set" : "NULL")}");

        if (coinsText == null)
        {
            GameLog.Write(
                "PlayerBank",
                $"RefreshCoinsUI skipped: coinsText is NULL netId={netId}");
            return;
        }

        coinsText.text =
            coins.ToString();
    }

    private void RefreshChipsUI()
    {
        GameLog.Write(
            "PlayerBank",
            $"RefreshChipsUI netId={netId} text={(chipsText != null ? "set" : "NULL")}");

        if (chipsText == null)
        {
            GameLog.Write(
                "PlayerBank",
                $"RefreshChipsUI skipped: chipsText is NULL netId={netId}");
            return;
        }

        chipsText.text =
            GetTotalChipValue().ToString();
    }

    #endregion

    #region Internal

    private int GetChipIndex(
        int denomination)
    {
        GameLog.Write(
            "PlayerBank",
            $"GetChipIndex netId={netId} denomination={denomination} entries={chips.Count}");

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
        GameLog.Write(
            "PlayerBank",
            $"OnCoinsChanged netId={netId} old={oldValue} new={newValue}");

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
        GameLog.Write(
            "PlayerBank",
            $"OnChipsChanged netId={netId} operation={operation} index={index} old={oldItem.denomination}x{oldItem.amount} new={newItem.denomination}x{newItem.amount}");

        if (operation !=
            SyncList<ChipAmount>.Operation.OP_SET)
        {
            GameLog.Write(
                "PlayerBank",
                $"OnChipsChanged ignored: operation is not OP_SET netId={netId} operation={operation}");
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
        GameLog.Write(
            "PlayerBank",
            $"TestCoinsRain10 netId={netId} isServer={isServer}");

        if (!isServer)
        {
            GameLog.Write(
                "PlayerBank",
                $"TestCoinsRain10 skipped: not server netId={netId}");
            return;
        }

        StartCoinsRain(
            10,
            moneyRainDuration);
    }

    [ContextMenu("Coins Rain 50")]
    private void TestCoinsRain50()
    {
        GameLog.Write(
            "PlayerBank",
            $"TestCoinsRain50 netId={netId} isServer={isServer}");

        if (!isServer)
        {
            GameLog.Write(
                "PlayerBank",
                $"TestCoinsRain50 skipped: not server netId={netId}");
            return;
        }

        StartCoinsRain(
            50,
            moneyRainDuration);
    }

    [ContextMenu("Coins Rain 100")]
    private void TestCoinsRain100()
    {
        GameLog.Write(
            "PlayerBank",
            $"TestCoinsRain100 netId={netId} isServer={isServer}");

        if (!isServer)
        {
            GameLog.Write(
                "PlayerBank",
                $"TestCoinsRain100 skipped: not server netId={netId}");
            return;
        }

        StartCoinsRain(
            100,
            moneyRainDuration);
    }

    [ContextMenu("Chips Rain 10")]
    private void TestChipsRain10()
    {
        GameLog.Write(
            "PlayerBank",
            $"TestChipsRain10 netId={netId} isServer={isServer}");

        if (!isServer)
        {
            GameLog.Write(
                "PlayerBank",
                $"TestChipsRain10 skipped: not server netId={netId}");
            return;
        }

        StartChipsRain(
            1,
            10,
            moneyRainDuration);
    }

    [ContextMenu("Chips Rain 50")]
    private void TestChipsRain50()
    {
        GameLog.Write(
            "PlayerBank",
            $"TestChipsRain50 netId={netId} isServer={isServer}");

        if (!isServer)
        {
            GameLog.Write(
                "PlayerBank",
                $"TestChipsRain50 skipped: not server netId={netId}");
            return;
        }

        StartChipsRain(
            1,
            50,
            moneyRainDuration);
    }

    [ContextMenu("Chips Rain 100")]
    private void TestChipsRain100()
    {
        GameLog.Write(
            "PlayerBank",
            $"TestChipsRain100 netId={netId} isServer={isServer}");

        if (!isServer)
        {
            GameLog.Write(
                "PlayerBank",
                $"TestChipsRain100 skipped: not server netId={netId}");
            return;
        }

        StartChipsRain(
            1,
            100,
            moneyRainDuration);
    }

    #endregion
}