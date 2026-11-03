using UnityEngine;

public partial class PlayerBank
{
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
}
