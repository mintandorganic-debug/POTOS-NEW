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
        CreateCoinStacks();
        CreateChipStacks();

        bank.CoinsChanged += OnCoinsChanged;
        bank.ChipsChanged += OnChipsChanged;

        Refresh();
    }

    private void OnDestroy()
    {
        if (bank == null)
            return;

        bank.CoinsChanged -= OnCoinsChanged;
        bank.ChipsChanged -= OnChipsChanged;
    }

    #region Money Rain

    public void PlayCoinsRain(int count)
    {
        if (coinsVfx == null)
            return;

        if (count <= 0)
            return;

        coinsVfx.SetInt(
            VfxCountProperty,
            count);

        coinsVfx.Play();
    }

    public void PlayChipsRain(int count)
    {
        if (chipsVfx == null)
            return;

        if (count <= 0)
            return;

        chipsVfx.SetInt(
            VfxCountProperty,
            count);

        chipsVfx.Play();
    }

    #endregion

    #region Coins

    private void CreateCoinStacks()
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning(
                "Coin prefab is not assigned.",
                this);

            return;
        }

        if (coinStack10Position == null)
        {
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
        }

        if (coinStack20Position == null)
        {
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
        }
    }

    private void OnCoinsChanged(
        int oldValue,
        int newValue)
    {
        RebuildCoinVisuals();
    }

    private void RebuildCoinVisuals()
    {
        int firstStackAmount =
            Mathf.Min(
                bank.Coins,
                10);

        int secondStackAmount =
            Mathf.Max(
                bank.Coins - 10,
                0);

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
        stacks.Clear();

        foreach (int denomination in
                 bank.AvailableDenominations)
        {
            ChipPrefabEntry entry =
                GetPrefabEntry(
                    denomination);

            if (entry == null)
            {
                Debug.LogWarning(
                    $"No chip prefab entry for denomination {denomination}.",
                    this);

                continue;
            }

            if (entry.prefab == null)
            {
                Debug.LogWarning(
                    $"No chip prefab for denomination {denomination}.",
                    this);

                continue;
            }

            if (entry.stackPosition == null)
            {
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
                continue;

            stacks.Add(
                denomination,
                stack);
        }
    }

    private ChipPrefabEntry GetPrefabEntry(
        int denomination)
    {
        foreach (ChipPrefabEntry entry in chipPrefabs)
        {
            if (entry.denomination ==
                denomination)
            {
                return entry;
            }
        }

        return null;
    }

    private void OnChipsChanged(
        int denomination,
        int amount)
    {
        RebuildChipVisuals();
    }

    private void RebuildChipVisuals()
    {
        Dictionary<int, int> visualAmounts =
            CalculateVisualAmounts();

        foreach (int denomination in
                 bank.AvailableDenominations)
        {
            if (!stacks.TryGetValue(
                    denomination,
                    out ChipStackView stack))
            {
                continue;
            }

            visualAmounts.TryGetValue(
                denomination,
                out int amount);

            stack.SetAmount(
                amount);
        }
    }

    private Dictionary<int, int> CalculateVisualAmounts()
    {
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
        }

        return result;
    }

    #endregion

    #region Stack Creation

    private ChipStackView CreateStack(
        string stackName,
        Transform position,
        Transform prefab)
    {
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

        return stack;
    }

    #endregion

    #region Refresh

    private void Refresh()
    {
        RebuildCoinVisuals();
        RebuildChipVisuals();
    }

    #endregion
}   