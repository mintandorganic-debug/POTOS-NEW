using System.Collections;
using UnityEngine;

public class DicePhase
{
    private const float AutoThrowPower = 0.5f;

    private static readonly int[] Denominations =
    {
        100,
        50,
        10,
        1
    };

    private readonly IDicePhaseContext context;
    private readonly MonoBehaviour runner;
    private readonly float duration;
    private readonly float resultDelay;

    private bool active;
    private bool thrown;

    public DicePhase(
        IDicePhaseContext context,
        MonoBehaviour runner,
        float duration,
        float resultDelay)
    {
        this.context = context;
        this.runner = runner;
        this.duration = duration;
        this.resultDelay = resultDelay;

        GameLog.Write(
            "DicePhase",
            $"Created duration={duration} resultDelay={resultDelay}");
    }

    public bool TryBegin()
    {
        if (active)
        {
            GameLog.Write(
                "DicePhase",
                "TryBegin ignored: dice phase already active, turn cannot be skipped");

            return true;
        }

        if (!context.GameStarted ||
            context.Players.Count == 0 ||
            context.CurrentPlayerIndex < 0 ||
            context.CurrentPlayerIndex >= context.Players.Count)
        {
            GameLog.Write(
                "DicePhase",
                $"TryBegin skipped: gameStarted={context.GameStarted} players={context.Players.Count} currentPlayerIn={context.CurrentPlayerIndex}");

            return false;
        }

        Player player =
            context.Players[context.CurrentPlayerIndex];

        if (player == null)
        {
            GameLog.Write(
                "DicePhase",
                "TryBegin skipped: current player is NULL");

            return false;
        }

        active = true;
        thrown = false;

        context.SetPhase(
            GameMaster.GamePhase.Dice);

        context.NotifyDicePhaseStarted(
            player.netId,
            duration);

        GameLog.Write(
            "DicePhase",
            $"TryBegin started player={player.netId} seat={context.CurrentPlayerIndex} duration={duration}");

        runner.StartCoroutine(
            Run(player));

        return true;
    }

    public bool RequestThrow(
        Player player,
        float power)
    {
        if (!active ||
            thrown)
        {
            GameLog.Write(
                "DicePhase",
                $"RequestThrow rejected: active={active} thrown={thrown}");

            return false;
        }

        if (context.CurrentPlayerIndex < 0 ||
            context.CurrentPlayerIndex >= context.Players.Count ||
            context.Players[context.CurrentPlayerIndex] != player)
        {
            GameLog.Write(
                "DicePhase",
                $"RequestThrow rejected: player={(player != null ? player.netId.ToString() : "NULL")} is not the current player");

            return false;
        }

        Throw(
            player,
            power);

        return true;
    }

    private IEnumerator Run(
        Player player)
    {
        float remaining =
            duration;

        while (remaining > 0f &&
               !thrown)
        {
            remaining -=
                Time.deltaTime;

            yield return null;
        }

        if (!thrown)
        {
            GameLog.Write(
                "DicePhase",
                $"Run timeout: auto throw power={AutoThrowPower}");

            Throw(
                player,
                AutoThrowPower);
        }

        yield return new WaitForSeconds(
            resultDelay);

        Finish();
    }

    private void Throw(
        Player player,
        float power)
    {
        thrown = true;

        float clampedPower =
            float.IsNaN(power)
                ? AutoThrowPower
                : Mathf.Clamp01(power);

        int[] values =
            DiceRules.Roll();

        int chips =
            DiceRules.ChipPayout(values);

        int coins =
            DiceRules.CoinPayout(values);

        GameLog.Write(
            "DicePhase",
            $"Throw player={(player != null ? player.netId.ToString() : "NULL")} power={clampedPower} values={string.Join(",", values)} chips={chips} coins={coins}");

        if (player != null)
        {
            Credit(
                player,
                chips,
                coins);

            context.NotifyDiceRolled(
                player.netId,
                values[0],
                values[1],
                values[2],
                clampedPower);
        }
    }

    private void Credit(
        Player player,
        int chips,
        int coins)
    {
        PlayerBank bank =
            player.GetComponentInChildren<PlayerBank>(true);

        if (bank == null)
        {
            GameLog.Write(
                "DicePhase",
                $"Credit skipped: PlayerBank is missing player={player.netId}");

            return;
        }

        int rest = chips;

        foreach (int denomination in Denominations)
        {
            int count =
                rest / denomination;

            if (count > 0)
            {
                bank.AddChips(
                    denomination,
                    count);

                rest -=
                    count * denomination;
            }
        }

        if (coins > 0)
        {
            bank.AddCoins(
                coins);
        }

        GameLog.Write(
            "DicePhase",
            $"Credit done player={player.netId} chips={chips} coins={coins}");
    }

    private void Finish()
    {
        GameLog.Write(
            "DicePhase",
            "Finish: advancing turn");

        active = false;
        thrown = false;

        context.AdvanceTurn();
    }
}
