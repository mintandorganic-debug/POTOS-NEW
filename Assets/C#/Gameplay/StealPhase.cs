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
    }

    public void TakeCoins(
        Player attackPlayer,
        Player victim)
    {
        if (attackPlayer == null ||
            victim == null ||
            attackPlayer == victim ||
            coinsAmount <= 0)
        {
            return;
        }

        PlayerBank attackerBank =
            attackPlayer.GetComponentInChildren<PlayerBank>(true);

        PlayerBank victimBank =
            victim.GetComponentInChildren<PlayerBank>(true);

        if (attackerBank == null ||
            victimBank == null)
        {
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
            return;

        victimBank.TryStealCoinsTo(
            attackerBank,
            amount,
            rainDuration);
    }

    public void Begin()
    {
        if (GameMaster.Instance == null ||
            GameMaster.Instance.CurrentPhase !=
                GameMaster.GamePhase.Steal)
        {
            finishSequence();
            return;
        }

        if (routine != null)
        {
            runner.StopCoroutine(
                routine);
        }

        routine =
            runner.StartCoroutine(
                Run());
    }

    public void Stop()
    {
        if (routine != null)
        {
            runner.StopCoroutine(
                routine);

            routine = null;
        }
    }

    private IEnumerator Run()
    {
        float remainingTime =
            actionTime;

        while (remainingTime > 0f)
        {
            if (!isSequenceActive())
            {
                routine = null;
                yield break;
            }

            if (GameMaster.Instance == null ||
                GameMaster.Instance.CurrentPhase !=
                    GameMaster.GamePhase.Steal)
            {
                routine = null;
                finishSequence();
                yield break;
            }

            remainingTime -=
                Time.deltaTime;

            yield return null;
        }

        routine = null;

        if (GameMaster.Instance != null &&
            GameMaster.Instance.CurrentPhase ==
                GameMaster.GamePhase.Steal)
        {
            GameMaster.Instance.EndTurn();
        }

        finishSequence();
    }
}
