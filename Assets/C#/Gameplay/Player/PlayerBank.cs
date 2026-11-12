using Mirror;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerBank : NetworkBehaviour, IMoneyRainContext, IBankStore
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

    private CoinWallet coinWallet;

    private ChipWallet chipWallet;

    private CoinWallet CoinOps =>
        coinWallet ??= new CoinWallet(
            this);

    private ChipWallet ChipOps =>
        chipWallet ??= new ChipWallet(
            this);

    private MoneyRain rain;

    private MoneyRain Rain =>
        rain ??= new MoneyRain(
            this,
            this);


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
    public bool AddCoins(
        int amount)
    {
        return CoinOps.AddCoins(
            amount);
    }

    [Server]
    public bool RemoveCoins(
        int amount)
    {
        return CoinOps.RemoveCoins(
            amount);
    }

    [Server]
    public bool TransferCoinsTo(
        PlayerBank recipient,
        int amount)
    {
        return CoinOps.TransferCoinsTo(
            recipient,
            amount);
    }

    [Server]
    public bool StealCoinsTo(
        PlayerBank recipient,
        int amount,
        float duration)
    {
        return CoinOps.StealCoinsTo(
            recipient,
            amount,
            duration);
    }

    [Server]
    public bool SpendCoins(
        int amount)
    {
        return CoinOps.SpendCoins(
            amount);
    }

    public bool HasCoins(
        int amount)
    {
        return CoinOps.HasCoins(
            amount);
    }

    [Server]
    public void StartCoinsRain(
        int amount,
        float duration,
        bool addCoins = true)
    {
        Rain.StartCoinsRain(
            amount,
            duration,
            addCoins);
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
        return ChipOps.AddChips(
            denomination,
            amount);
    }

    [Server]
    public bool RemoveChips(
        int denomination,
        int amount)
    {
        return ChipOps.RemoveChips(
            denomination,
            amount);
    }

    [Server]
    public bool SpendChips(
        int denomination,
        int amount)
    {
        return ChipOps.SpendChips(
            denomination,
            amount);
    }

    public int GetChips(
        int denomination)
    {
        return ChipOps.GetChips(
            denomination);
    }

    public bool HasChips(
        int denomination,
        int amount)
    {
        return ChipOps.HasChips(
            denomination,
            amount);
    }

    [Server]
    public void StartChipsRain(
        int denomination,
        int amount,
        float duration)
    {
        Rain.StartChipsRain(
            denomination,
            amount,
            duration);
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
        return ChipOps.GetTotalChipValue();
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
        return ChipOps.GetChipIndex(
            denomination);
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

    uint IMoneyRainContext.NetId =>
        netId;

    int IMoneyRainContext.MaxCoins =>
        MaxCoins;

    int IMoneyRainContext.GetChipIndex(
        int denomination)
    {
        return GetChipIndex(
            denomination);
    }

    void IMoneyRainContext.PlayCoinsRain(
        int count)
    {
        RpcPlayCoinsRain(
            count);
    }

    void IMoneyRainContext.PlayChipsRain(
        int count)
    {
        RpcPlayChipsRain(
            count);
    }

    uint IBankStore.NetId =>
        netId;

    int IBankStore.Coins
    {
        get => coins;
        set => coins = value;
    }

    int IBankStore.MaxCoins =>
        MaxCoins;

    IList<ChipAmount> IBankStore.Chips =>
        chips;
}
