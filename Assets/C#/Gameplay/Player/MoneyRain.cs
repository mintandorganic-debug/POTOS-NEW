using System.Collections;
using UnityEngine;

public class MoneyRain
{
    private readonly MonoBehaviour runner;
    private readonly IMoneyRainContext context;

    private Coroutine coinsRainRoutine;
    private Coroutine chipsRainRoutine;

    public MoneyRain(
        MonoBehaviour runner,
        IMoneyRainContext context)
    {
        this.runner = runner;
        this.context = context;

        GameLog.Write(
            "MoneyRain",
            "Created");
    }

    public void StartCoinsRain(
        int amount,
        float duration,
        bool addCoins = true)
    {
        GameLog.Write(
            "MoneyRain",
            $"StartCoinsRain netId={context.NetId} amount={amount} duration={duration} addCoins={addCoins} coins={context.Coins}");

        if (amount <= 0)
        {
            GameLog.Write(
                "MoneyRain",
                $"StartCoinsRain rejected: amount <= 0 netId={context.NetId} amount={amount}");
            return;
        }

        int actualAmount =
            addCoins
                ? Mathf.Min(
                    amount,
                    context.MaxCoins - context.Coins)
                : amount;

        if (actualAmount <= 0)
        {
            GameLog.Write(
                "MoneyRain",
                $"StartCoinsRain rejected: actualAmount <= 0 netId={context.NetId} amount={amount} actualAmount={actualAmount} coins={context.Coins} max={context.MaxCoins}");
            return;
        }

        if (coinsRainRoutine != null)
        {
            GameLog.Write(
                "MoneyRain",
                $"StartCoinsRain stopping previous rain routine netId={context.NetId}");

            runner.StopCoroutine(
                coinsRainRoutine);

            coinsRainRoutine = null;
        }

        GameLog.Write(
            "MoneyRain",
            $"StartCoinsRain playing rain netId={context.NetId} actualAmount={actualAmount}");

        context.PlayCoinsRain(
            actualAmount);

        if (!addCoins ||
            duration <= 0f)
        {
            GameLog.Write(
                "MoneyRain",
                $"StartCoinsRain instant (no routine) netId={context.NetId} actualAmount={actualAmount} addCoins={addCoins} duration={duration}");

            if (addCoins)
                context.AddCoins(actualAmount);

            return;
        }

        GameLog.Write(
            "MoneyRain",
            $"StartCoinsRain starting routine netId={context.NetId} actualAmount={actualAmount} duration={duration}");

        coinsRainRoutine =
            runner.StartCoroutine(
                CoinsRainRoutine(
                    actualAmount,
                    duration));
    }

    private IEnumerator CoinsRainRoutine(
        int amount,
        float duration)
    {
        GameLog.Write(
            "MoneyRain",
            $"CoinsRainRoutine start netId={context.NetId} amount={amount} duration={duration} coins={context.Coins}");

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
                context.AddCoins(delta);

                addedAmount =
                    targetAmount;
            }

            if (addedAmount >= amount)
                break;

            yield return null;
        }

        GameLog.Write(
            "MoneyRain",
            $"CoinsRainRoutine finished netId={context.NetId} amount={amount} added={addedAmount} coins={context.Coins}");

        coinsRainRoutine = null;
    }

    public void StartChipsRain(
        int denomination,
        int amount,
        float duration)
    {
        GameLog.Write(
            "MoneyRain",
            $"StartChipsRain netId={context.NetId} denomination={denomination} amount={amount} duration={duration}");

        if (amount <= 0)
        {
            GameLog.Write(
                "MoneyRain",
                $"StartChipsRain rejected: amount <= 0 netId={context.NetId} denomination={denomination} amount={amount}");
            return;
        }

        if (context.GetChipIndex(denomination) == -1)
        {
            GameLog.Write(
                "MoneyRain",
                $"StartChipsRain rejected: unknown denomination netId={context.NetId} denomination={denomination}");

            Debug.LogWarning(
                $"Unknown chip denomination: {denomination}");

            return;
        }

        if (chipsRainRoutine != null)
        {
            GameLog.Write(
                "MoneyRain",
                $"StartChipsRain stopping previous rain routine netId={context.NetId}");

            runner.StopCoroutine(
                chipsRainRoutine);

            chipsRainRoutine = null;
        }

        GameLog.Write(
            "MoneyRain",
            $"StartChipsRain playing rain netId={context.NetId} amount={amount}");

        context.PlayChipsRain(
            amount);

        if (duration <= 0f)
        {
            GameLog.Write(
                "MoneyRain",
                $"StartChipsRain instant (no routine) netId={context.NetId} denomination={denomination} amount={amount} duration={duration}");

            context.AddChips(
                denomination,
                amount);

            return;
        }

        GameLog.Write(
            "MoneyRain",
            $"StartChipsRain starting routine netId={context.NetId} denomination={denomination} amount={amount} duration={duration}");

        chipsRainRoutine =
            runner.StartCoroutine(
                ChipsRainRoutine(
                    denomination,
                    amount,
                    duration));
    }

    private IEnumerator ChipsRainRoutine(
        int denomination,
        int amount,
        float duration)
    {
        GameLog.Write(
            "MoneyRain",
            $"ChipsRainRoutine start netId={context.NetId} denomination={denomination} amount={amount} duration={duration}");

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
                context.AddChips(
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
            "MoneyRain",
            $"ChipsRainRoutine finished netId={context.NetId} denomination={denomination} amount={amount} added={addedAmount}");

        chipsRainRoutine = null;
    }
}
