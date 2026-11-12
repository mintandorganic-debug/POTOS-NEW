using System;
using System.Collections;
using UnityEngine;

public class DiceStage : ITurnStage, IDiceThrowHandler
{
    private const float AutoThrowPower = 0.5f;

    private readonly IDiceStageContext context;
    private readonly MonoBehaviour runner;
    private readonly DiceCombinationEvaluator evaluator;
    private readonly IDicePayout payout;
    private readonly float duration;
    private readonly float resultDelay;

    private Player player;
    private Action finished;
    private bool active;
    private bool thrown;

    public DiceStage(
        IDiceStageContext context,
        MonoBehaviour runner,
        DiceCombinationEvaluator evaluator,
        IDicePayout payout,
        float duration,
        float resultDelay)
    {
        this.context = context;
        this.runner = runner;
        this.evaluator = evaluator;
        this.payout = payout;
        this.duration = duration;
        this.resultDelay = resultDelay;

        GameLog.Write(
            "DiceStage",
            $"Created duration={duration} resultDelay={resultDelay}");
    }

    public TurnStage Id =>
        TurnStage.Dice;

    public bool CanSkip =>
        false;

    public void Begin(
        Player turnPlayer,
        Action onFinished)
    {
        player = turnPlayer;
        finished = onFinished;
        active = true;
        thrown = false;

        GameLog.Write(
            "DiceStage",
            $"Begin player={(player != null ? player.netId.ToString() : "NULL")} duration={duration}");

        context.SetPhase(
            GameMaster.GamePhase.Dice);

        runner.StartCoroutine(
            Run());
    }

    public bool RequestThrow(
        Player thrower,
        float power)
    {
        if (!active ||
            thrown ||
            thrower != player)
        {
            GameLog.Write(
                "DiceStage",
                $"RequestThrow rejected: active={active} thrown={thrown} isCurrentPlayer={thrower == player}");

            return false;
        }

        Throw(
            power);

        return true;
    }

    private IEnumerator Run()
    {
        context.NotifyDiceStarted(
            player != null ? player.netId : 0,
            duration);

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
                "DiceStage",
                $"Run timeout: auto throw power={AutoThrowPower}");

            Throw(
                AutoThrowPower);
        }

        yield return new WaitForSeconds(
            resultDelay);

        GameLog.Write(
            "DiceStage",
            "Run finished: stage done");

        active = false;

        finished();
    }

    private void Throw(
        float power)
    {
        thrown = true;

        float clamped =
            float.IsNaN(power)
                ? AutoThrowPower
                : Mathf.Clamp01(power);

        int[] values =
            DiceRules.Roll();

        IDiceCombination combination =
            evaluator.Evaluate(
                values);

        int chips =
            DiceRules.BaseChips(
                values);

        int coins =
            combination != null
                ? combination.Coins
                : 0;

        GameLog.Write(
            "DiceStage",
            $"Throw player={(player != null ? player.netId.ToString() : "NULL")} power={clamped} values={string.Join(",", values)} combination={(combination != null ? combination.Name : "none")} chips={chips} coins={coins}");

        payout.Pay(
            player,
            chips,
            coins);

        if (player != null)
        {
            context.NotifyDiceRolled(
                player.netId,
                values[0],
                values[1],
                values[2],
                combination != null ? combination.Name : string.Empty,
                chips,
                coins,
                clamped);
        }
    }
}
