using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PlayerBankVisual : MonoBehaviour
{
    [SerializeField] private PlayerBank bank;

    [Header("Chip Stacks")]
    [SerializeField] private List<ChipPrefabEntry> chipPrefabs = new();

    [Header("Coins")]
    [SerializeField] private Transform coinPrefab;
    [SerializeField] private Transform coinsSpawnPoint;

    [SerializeField] private Transform coinStack10Position;
    [SerializeField] private Transform coinStack20Position;

    [Header("VFX")]
    [SerializeField] private VisualEffect coinsVfx;
    [SerializeField] private VisualEffect chipsVfx;

    [Header("Layout")]
    [SerializeField] private float chipHeight = 0.03f;

    private const string VfxCountProperty = "Count";

    private readonly Dictionary<int, ChipStackView> stacks = new();

    private ChipStackView coinStack10;
    private ChipStackView coinStack20;

    [System.Serializable]
    private class ChipPrefabEntry
    {
        public int denomination;

        public Transform prefab;

        public Transform stackPosition;
    }

    private void Start()
    {
        GameLog.Write(
            "PlayerBankVisual",
            $"Start bank={(bank != null ? bank.netId.ToString() : "NULL")} object={name} chipPrefabs={chipPrefabs.Count}");

        CreateCoinStacks();
        CreateChipStacks();

        bank.CoinsChanged += OnCoinsChanged;
        bank.ChipsChanged += OnChipsChanged;

        GameLog.Write(
            "PlayerBankVisual",
            $"Start subscribed to bank events bank={(bank != null ? bank.netId.ToString() : "NULL")}");

        Refresh();
    }

    private void OnDestroy()
    {
        GameLog.Write(
            "PlayerBankVisual",
            $"OnDestroy bank={(bank != null ? bank.netId.ToString() : "NULL")} object={name}");

        if (bank == null)
        {
            GameLog.Write(
                "PlayerBankVisual",
                $"OnDestroy skipped: bank is NULL");
            return;
        }

        bank.CoinsChanged -= OnCoinsChanged;
        bank.ChipsChanged -= OnChipsChanged;

        GameLog.Write(
            "PlayerBankVisual",
            $"OnDestroy unsubscribed from bank events bank={(bank != null ? bank.netId.ToString() : "NULL")}");
    }

    #region Money Rain

    public void PlayCoinsRain(int count)
    {
        GameLog.Write(
            "PlayerBankVisual",
            $"PlayCoinsRain count={count} vfx={(coinsVfx != null ? "set" : "NULL")}");

        if (coinsVfx == null)
        {
            GameLog.Write(
                "PlayerBankVisual",
                $"PlayCoinsRain skipped: coinsVfx is NULL count={count}");
            return;
        }

        if (count <= 0)
        {
            GameLog.Write(
                "PlayerBankVisual",
                $"PlayCoinsRain skipped: count <= 0 count={count}");
            return;
        }

        GameLog.Write(
            "PlayerBankVisual",
            $"PlayCoinsRain playing count={count}");

        coinsVfx.SetInt(
            VfxCountProperty,
            count);

        coinsVfx.Play();
    }

    public void PlayChipsRain(int count)
    {
        GameLog.Write(
            "PlayerBankVisual",
            $"PlayChipsRain count={count} vfx={(chipsVfx != null ? "set" : "NULL")}");

        if (chipsVfx == null)
        {
            GameLog.Write(
                "PlayerBankVisual",
                $"PlayChipsRain skipped: chipsVfx is NULL count={count}");
            return;
        }

        if (count <= 0)
        {
            GameLog.Write(
                "PlayerBankVisual",
                $"PlayChipsRain skipped: count <= 0 count={count}");
            return;
        }

        GameLog.Write(
            "PlayerBankVisual",
            $"PlayChipsRain playing count={count}");

        chipsVfx.SetInt(
            VfxCountProperty,
            count);

        chipsVfx.Play();
    }

    #endregion

    #region Coins

    private void CreateCoinStacks()
    {
        GameLog.Write(
            "PlayerBankVisual",
            $"CreateCoinStacks coinPrefab={(coinPrefab != null ? "set" : "NULL")} stack10Position={(coinStack10Position != null ? "set" : "NULL")} stack20Position={(coinStack20Position != null ? "set" : "NULL")}");

        if (coinPrefab == null)
        {
            GameLog.Write(
                "PlayerBankVisual",
                $"CreateCoinStacks aborted: coin prefab is not assigned");

            Debug.LogWarning(
                "Coin prefab is not assigned.",
                this);

            return;
        }

        if (coinStack10Position == null)
        {
            GameLog.Write(
                "PlayerBankVisual",
                $"CreateCoinStacks stack 10 position is not assigned");

            Debug.LogWarning(
                "Coin stack 10 position is not assigned.",
                this);
        }
        else
        {
            coinStack10 =
                CreateStack(
                    "CoinStack_10",
                    coinStack10Position,
                    coinPrefab);

            GameLog.Write(
                "PlayerBankVisual",
                $"CreateCoinStacks stack 10 created stack={(coinStack10 != null ? "set" : "NULL")}");
        }

        if (coinStack20Position == null)
        {
            GameLog.Write(
                "PlayerBankVisual",
                $"CreateCoinStacks stack 20 position is not assigned");

            Debug.LogWarning(
                "Coin stack 20 position is not assigned.",
                this);
        }
        else
        {
            coinStack20 =
                CreateStack(
                    "CoinStack_20",
                    coinStack20Position,
                    coinPrefab);

            GameLog.Write(
                "PlayerBankVisual",
                $"CreateCoinStacks stack 20 created stack={(coinStack20 != null ? "set" : "NULL")}");
        }
    }

    private void OnCoinsChanged(
        int oldValue,
        int newValue)
    {
        GameLog.Write(
            "PlayerBankVisual",
            $"OnCoinsChanged old={oldValue} new={newValue}");

        RebuildCoinVisuals();
    }

    private void RebuildCoinVisuals()
    {
        GameLog.Write(
            "PlayerBankVisual",
            $"RebuildCoinVisuals bank.Coins={(bank != null ? bank.Coins.ToString() : "NULL")} stack10={(coinStack10 != null ? "set" : "NULL")} stack20={(coinStack20 != null ? "set" : "NULL")}");

        int firstStackAmount =
            Mathf.Min(
                bank.Coins,
                10);

        int secondStackAmount =
            Mathf.Max(
                bank.Coins - 10,
                0);

        GameLog.Write(
            "PlayerBankVisual",
            $"RebuildCoinVisuals amounts first={firstStackAmount} second={secondStackAmount}");

        if (coinStack10 != null)
        {
            coinStack10.SetAmount(
                firstStackAmount);
        }

        if (coinStack20 != null)
        {
            coinStack20.SetAmount(
                secondStackAmount);
        }
    }

    #endregion

    #region Chips

    private void CreateChipStacks()
    {
        GameLog.Write(
            "PlayerBankVisual",
            $"CreateChipStacks bank={(bank != null ? bank.netId.ToString() : "NULL")} existingStacks={stacks.Count}");

        stacks.Clear();

        foreach (int denomination in
                 bank.AvailableDenominations)
        {
            ChipPrefabEntry entry =
                GetPrefabEntry(
                    denomination);

            if (entry == null)
            {
                GameLog.Write(
                    "PlayerBankVisual",
                    $"CreateChipStacks skipped: no chip prefab entry denomination={denomination}");

                Debug.LogWarning(
                    $"No chip prefab entry for denomination {denomination}.",
                    this);

                continue;
            }

            if (entry.prefab == null)
            {
                GameLog.Write(
                    "PlayerBankVisual",
                    $"CreateChipStacks skipped: no chip prefab denomination={denomination}");

                Debug.LogWarning(
                    $"No chip prefab for denomination {denomination}.",
                    this);

                continue;
            }

            if (entry.stackPosition == null)
            {
                GameLog.Write(
                    "PlayerBankVisual",
                    $"CreateChipStacks skipped: no stack position denomination={denomination}");

                Debug.LogWarning(
                    $"No stack position for denomination {denomination}.",
                    this);

                continue;
            }

            ChipStackView stack =
                CreateStack(
                    $"ChipStack_{denomination}",
                    entry.stackPosition,
                    entry.prefab);

            if (stack == null)
            {
                GameLog.Write(
                    "PlayerBankVisual",
                    $"CreateChipStacks skipped: stack is NULL denomination={denomination}");
                continue;
            }

            stacks.Add(
                denomination,
                stack);

            GameLog.Write(
                "PlayerBankVisual",
                $"CreateChipStacks stack added denomination={denomination} totalStacks={stacks.Count}");
        }
    }

    private ChipPrefabEntry GetPrefabEntry(
        int denomination)
    {
        GameLog.Write(
            "PlayerBankVisual",
            $"GetPrefabEntry denomination={denomination} entries={chipPrefabs.Count}");

        foreach (ChipPrefabEntry entry in chipPrefabs)
        {
            if (entry.denomination ==
                denomination)
            {
                GameLog.Write(
                    "PlayerBankVisual",
                    $"GetPrefabEntry found denomination={denomination}");

                return entry;
            }
        }

        GameLog.Write(
            "PlayerBankVisual",
            $"GetPrefabEntry not found denomination={denomination}");

        return null;
    }

    private void OnChipsChanged(
        int denomination,
        int amount)
    {
        GameLog.Write(
            "PlayerBankVisual",
            $"OnChipsChanged denomination={denomination} amount={amount}");

        RebuildChipVisuals();
    }

    private void RebuildChipVisuals()
    {
        GameLog.Write(
            "PlayerBankVisual",
            $"RebuildChipVisuals bank={(bank != null ? bank.netId.ToString() : "NULL")} stacks={stacks.Count}");

        Dictionary<int, int> visualAmounts =
            CalculateVisualAmounts();

        foreach (int denomination in
                 bank.AvailableDenominations)
        {
            if (!stacks.TryGetValue(
                    denomination,
                    out ChipStackView stack))
            {
                GameLog.Write(
                    "PlayerBankVisual",
                    $"RebuildChipVisuals skipped: no stack denomination={denomination}");

                continue;
            }

            visualAmounts.TryGetValue(
                denomination,
                out int amount);

            GameLog.Write(
                "PlayerBankVisual",
                $"RebuildChipVisuals set stack denomination={denomination} visualAmount={amount}");

            stack.SetAmount(
                amount);
        }
    }

    private Dictionary<int, int> CalculateVisualAmounts()
    {
        GameLog.Write(
            "PlayerBankVisual",
            $"CalculateVisualAmounts bank={(bank != null ? bank.netId.ToString() : "NULL")} denominations={(bank != null ? bank.AvailableDenominations.Count.ToString() : "NULL")}");

        Dictionary<int, int> result =
            new Dictionary<int, int>();

        List<int> denominations =
            new List<int>(
                bank.AvailableDenominations);

        denominations.Sort();

        foreach (int denomination in denominations)
        {
            result[denomination] =
                bank.GetChips(
                    denomination);
        }

        for (int i = 0;
             i < denominations.Count - 1;
             i++)
        {
            int current =
                denominations[i];

            int next =
                denominations[i + 1];

            int currentAmount =
                result[current];

            int value =
                currentAmount * current;

            int converted =
                value / next;

            int remainder =
                value % next;

            result[current] =
                remainder / current;

            result[next] +=
                converted;

            GameLog.Write(
                "PlayerBankVisual",
                $"CalculateVisualAmounts converted current={current} next={next} value={value} converted={converted} remainder={remainder}");
        }

        GameLog.Write(
            "PlayerBankVisual",
            $"CalculateVisualAmounts done entries={result.Count}");

        return result;
    }

    #endregion

    #region Stack Creation

    private ChipStackView CreateStack(
        string stackName,
        Transform position,
        Transform prefab)
    {
        GameLog.Write(
            "PlayerBankVisual",
            $"CreateStack name={stackName} position={(position != null ? position.name : "NULL")} prefab={(prefab != null ? prefab.name : "NULL")} chipHeight={chipHeight}");

        GameObject stackObject =
            new GameObject(
                stackName);

        stackObject.transform.SetParent(
            position,
            false);

        stackObject.transform.localPosition =
            Vector3.zero;

        stackObject.transform.localRotation =
            Quaternion.identity;

        stackObject.transform.localScale =
            Vector3.one;

        ChipStackView stack =
            stackObject.AddComponent<ChipStackView>();

        stack.Initialize(
            prefab,
            chipHeight);

        GameLog.Write(
            "PlayerBankVisual",
            $"CreateStack created name={stackName}");

        return stack;
    }

    #endregion

    #region Refresh

    private void Refresh()
    {
        GameLog.Write(
            "PlayerBankVisual",
            $"Refresh bank={(bank != null ? bank.netId.ToString() : "NULL")}");

        RebuildCoinVisuals();
        RebuildChipVisuals();
    }

    #endregion
}   