using System;
using System.Collections;
using UnityEngine;

public class StealPhase
{
    private readonly MonoBehaviour runner;
    private readonly float actionTime;
    private readonly int coinsAmount;
    private readonly float rainDuration;
    private readonly Func<bool> isSequenceActive;
    private readonly Action finishSequence;

    private Coroutine routine;

    public StealPhase(
        MonoBehaviour runner,
        float actionTime,
        int coinsAmount,
        float rainDuration,
        Func<bool> isSequenceActive,
        Action finishSequence)
    {
        this.runner = runner;
        this.actionTime = actionTime;
        this.coinsAmount = coinsAmount;
        this.rainDuration = rainDuration;
        this.isSequenceActive = isSequenceActive;
        this.finishSequence = finishSequence;

        GameLog.Write(
            "StealPhase",
            $"ctor runner={(runner != null ? runner.name : "NULL")} actionTime={actionTime} coinsAmount={coinsAmount} rainDuration={rainDuration}");
    }

    public void TakeCoins(
        Player attackPlayer,
        Player victim)
    {
        GameLog.Write(
            "StealPhase",
            $"TakeCoins attacker={(attackPlayer != null ? attackPlayer.netId.ToString() : "NULL")} victim={(victim != null ? victim.netId.ToString() : "NULL")} coinsAmount={coinsAmount}");

        if (attackPlayer == null ||
            victim == null ||
            attackPlayer == victim ||
            coinsAmount <= 0)
        {
            GameLog.Write(
                "StealPhase",
                $"TakeCoins skipped: invalid arguments attackerNull={attackPlayer == null} victimNull={victim == null} sameplayer={attackPlayer == victim} coinsAmount={coinsAmount}");

            return;
        }

        PlayerBank attackerBank =
            attackPlayer.GetComponentInChildren<PlayerBank>(true);

        PlayerBank victimBank =
            victim.GetComponentInChildren<PlayerBank>(true);

        if (attackerBank == null ||
            victimBank == null)
        {
            GameLog.Write(
                "StealPhase",
                $"TakeCoins skipped: PlayerBank missing attackerBankNull={attackerBank == null} victimBankNull={victimBank == null}");

            Debug.LogWarning(
                "AttackSequence: PlayerBank is missing on attacker or victim.",
                runner);

            return;
        }

        int amount =
            Mathf.Min(
                coinsAmount,
                victimBank.Coins);

        if (amount <= 0)
        {
            GameLog.Write(
                "StealPhase",
                $"TakeCoins skipped: victim has no coins victimCoins={victimBank.Coins}");

            return;
        }

        GameLog.Write(
            "StealPhase",
            $"TakeCoins stealing amount={amount} victimCoins={victimBank.Coins} rainDuration={rainDuration}");

        victimBank.TryStealCoinsTo(
            attackerBank,
            amount,
            rainDuration);
    }

    public void Begin()
    {
        GameLog.Write(
            "StealPhase",
            $"Begin phase={(GameMaster.Instance != null ? GameMaster.Instance.CurrentPhase.ToString() : "NULL")} routineRunning={routine != null}");

        if (GameMaster.Instance == null ||
            GameMaster.Instance.CurrentPhase !=
                GameMaster.GamePhase.Steal)
        {
            GameLog.Write(
                "StealPhase",
                "Begin failed: GameMaster missing or phase is not Steal, finishing sequence");

            finishSequence();
            return;
        }

        if (routine != null)
        {
            GameLog.Write(
                "StealPhase",
                "Begin stopping previous routine");

            runner.StopCoroutine(
                routine);
        }

        routine =
            runner.StartCoroutine(
                Run());

        GameLog.Write(
            "StealPhase",
            $"Begin steal timer started actionTime={actionTime}");
    }

    public void Stop()
    {
        GameLog.Write(
            "StealPhase",
            $"Stop routineRunning={routine != null}");

        if (routine != null)
        {
            runner.StopCoroutine(
                routine);

            routine = null;

            GameLog.Write(
                "StealPhase",
                "Stop steal timer stopped");
        }
    }

    private IEnumerator Run()
    {
        GameLog.Write(
            "StealPhase",
            $"Run start actionTime={actionTime}");

        float remainingTime =
            actionTime;

        while (remainingTime > 0f)
        {
            if (!isSequenceActive())
            {
                GameLog.Write(
                    "StealPhase",
                    $"Run end: sequence no longer active remaining={remainingTime}");

                routine = null;
                yield break;
            }

            if (GameMaster.Instance == null ||
                GameMaster.Instance.CurrentPhase !=
                    GameMaster.GamePhase.Steal)
            {
                GameLog.Write(
                    "StealPhase",
                    $"Run end: phase is no longer Steal phase={(GameMaster.Instance != null ? GameMaster.Instance.CurrentPhase.ToString() : "NULL")} remaining={remainingTime}, finishing sequence");

                routine = null;
                finishSequence();
                yield break;
            }

            remainingTime -=
                Time.deltaTime;

            yield return null;
        }

        routine = null;

        GameLog.Write(
            "StealPhase",
            $"Run steal time elapsed phase={(GameMaster.Instance != null ? GameMaster.Instance.CurrentPhase.ToString() : "NULL")}");

        if (GameMaster.Instance != null &&
            GameMaster.Instance.CurrentPhase ==
                GameMaster.GamePhase.Steal)
        {
            GameLog.Write(
                "StealPhase",
                "Run steal time elapsed, calling GameMaster.EndTurn");

            GameMaster.Instance.EndTurn();
        }

        GameLog.Write(
            "StealPhase",
            "Run end: finishing sequence");

        finishSequence();
    }
}
