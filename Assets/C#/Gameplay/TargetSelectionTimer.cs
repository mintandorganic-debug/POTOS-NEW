using System;
using System.Collections;
using UnityEngine;

public class TargetSelectionTimer
{
    private readonly MonoBehaviour runner;
    private readonly AttackState state;
    private readonly float time;
    private readonly Action onTimeout;

    private Coroutine routine;

    public TargetSelectionTimer(
        MonoBehaviour runner,
        AttackState state,
        float time,
        Action onTimeout)
    {
        this.runner = runner;
        this.state = state;
        this.time = time;
        this.onTimeout = onTimeout;

        GameLog.Write(
            "TargetSelectionTimer",
            $"Created time={time}");
    }

    public void Forget()
    {
        GameLog.Write(
            "TargetSelectionTimer",
            $"Forget running={routine != null}");

        routine = null;
    }

    public void Start()
    {
        GameLog.Write(
            "TargetSelectionTimer",
            $"StartTargetSelectionTimer time={time} previousRunning={routine != null}");

        Stop();

        routine =
            runner.StartCoroutine(
                Run());

        GameLog.Write(
            "TargetSelectionTimer",
            "StartTargetSelectionTimer timer started");
    }

    public void Stop()
    {
        GameLog.Write(
            "TargetSelectionTimer",
            $"StopTargetSelectionTimer running={routine != null}");

        if (routine != null)
        {
            runner.StopCoroutine(
                routine);

            routine = null;

            GameLog.Write(
                "TargetSelectionTimer",
                "StopTargetSelectionTimer timer stopped");
        }
    }

    private IEnumerator Run()
    {
        GameLog.Write(
            "TargetSelectionTimer",
            $"TargetSelectionRoutine start time={time}");

        float remainingTime =
            time;

        while (remainingTime > 0f)
        {
            if (!state.IsActive ||
                state.Card == null ||
                state.Attacker == null)
            {
                GameLog.Write(
                    "TargetSelectionTimer",
                    $"TargetSelectionRoutine end: sequence no longer valid isActive={state.IsActive} remaining={remainingTime}");

                routine = null;
                yield break;
            }

            if (state.Target != null)
            {
                GameLog.Write(
                    "TargetSelectionTimer",
                    $"TargetSelectionRoutine end: target selected target={state.Target.name} remaining={remainingTime}");

                routine = null;
                yield break;
            }

            remainingTime -=
                Time.deltaTime;

            yield return null;
        }

        routine = null;

        GameLog.Write(
            "TargetSelectionTimer",
            $"TargetSelectionRoutine timed out isActive={state.IsActive} targetSelected={state.Target != null}");

        if (state.IsActive &&
            state.Target == null)
        {
            GameLog.Write(
                "TargetSelectionTimer",
                "TargetSelectionRoutine timeout: no target chosen, cancelling sequence");

            onTimeout();
        }

        GameLog.Write(
            "TargetSelectionTimer",
            "TargetSelectionRoutine end");
    }
}
