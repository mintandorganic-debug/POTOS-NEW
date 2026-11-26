using Mirror;
using System.Collections;
using UnityEngine;

public class AttackResolver
{
    private readonly IAttackResolverContext context;
    private readonly AttackState state;
    private readonly float defenseTime;

    public AttackResolver(
        IAttackResolverContext context,
        AttackState state,
        float defenseTime)
    {
        this.context = context;
        this.state = state;
        this.defenseTime = defenseTime;

        GameLog.Write(
            "AttackResolver",
            $"Created defenseTime={defenseTime}");
    }

    public IEnumerator Run()
    {
        GameLog.Write(
            "AttackResolver",
            $"AttackDelayRoutine start defenseTime={defenseTime}");

        Player victim =
            state.Target != null
                ? state.Target.GetComponentInParent<Player>()
                : null;

        if (victim == null)
        {
            GameLog.Write(
                "AttackResolver",
                "AttackDelayRoutine failed: victim is null, cancelling");

            context.CancelSequence();
            yield break;
        }

        NetworkConnectionToClient victimConnection =
            victim.netIdentity.connectionToClient;

        if (victimConnection != null)
        {
            GameLog.Write(
                "AttackResolver",
                $"AttackDelayRoutine calling TargetShowDefenseTimer victim={victim.netId} time={defenseTime}");

            context.ShowDefenseTimer(
                victimConnection,
                defenseTime);
        }

        float remainingTime =
            defenseTime;

        while (remainingTime > 0f)
        {
            if (!state.IsActive ||
                state.Card == null ||
                state.Attacker == null ||
                state.Target == null)
            {
                GameLog.Write(
                    "AttackResolver",
                    $"AttackDelayRoutine interrupted: sequence changed during defense window isActive={state.IsActive} remaining={remainingTime}");

                if (victimConnection != null)
                {
                    GameLog.Write(
                        "AttackResolver",
                        $"AttackDelayRoutine calling TargetHideDefenseTimer victim={(victim != null ? victim.netId.ToString() : "NULL")}");

                    context.HideDefenseTimer(
                        victimConnection);
                }

                yield break;
            }

            remainingTime -=
                Time.deltaTime;

            if (victimConnection != null)
            {
                context.SetDefenseTimer(
                    victimConnection,
                    remainingTime);
            }

            yield return null;
        }

        GameLog.Write(
            "AttackResolver",
            "AttackDelayRoutine defense window elapsed");

        if (victimConnection != null)
        {
            GameLog.Write(
                "AttackResolver",
                $"AttackDelayRoutine calling TargetHideDefenseTimer victim={(victim != null ? victim.netId.ToString() : "NULL")}");

            context.HideDefenseTimer(
                victimConnection);
        }

        if (!state.IsActive ||
            state.Card == null ||
            state.Attacker == null ||
            state.Target == null)
        {
            GameLog.Write(
                "AttackResolver",
                $"AttackDelayRoutine end: sequence no longer valid after timer isActive={state.IsActive}");

            yield break;
        }

        Card card =
            state.Card;

        Player attackPlayer =
            state.Attacker;

        PlayerInteractable attackTarget =
            state.Target;

        Player targetPlayer =
            attackTarget.GetComponentInParent<Player>();

        if (targetPlayer == null)
        {
            GameLog.Write(
                "AttackResolver",
                "AttackDelayRoutine failed: targetPlayer is null, cancelling");

            context.CancelSequence();
            yield break;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
        {
            GameLog.Write(
                "AttackResolver",
                "AttackDelayRoutine failed: GameMaster.Instance is null, cancelling");

            context.CancelSequence();
            yield break;
        }

        if (!gameMaster.SkipDefense(
                targetPlayer))
        {
            GameLog.Write(
                "AttackResolver",
                $"AttackDelayRoutine failed: GameMaster.SkipDefense returned false targetPlayer={targetPlayer.netId}, cancelling");

            context.CancelSequence();
            yield break;
        }

        GameLog.Write(
            "AttackResolver",
            $"AttackDelayRoutine defense skipped, applying attack card={(card.Data != null ? card.Data.name : "NULL")} attacker={attackPlayer.netId} targetPlayer={targetPlayer.netId}");

        GameLog.Write(
            "AttackResolver",
            $"AttackDelayRoutine calling RpcTriggerAttackAnimation attacker={attackPlayer.netId}");

        context.TriggerAttackAnimation(
            attackPlayer.netIdentity);

        attackTarget.Attack(
            card,
            attackPlayer);

        AttackSequence.Instance?.RpcPlaySequenceAudio(
            AttackSequence.GetCardAudioKey(card),
            1,
            attackPlayer != null ? attackPlayer.netIdentity : null);

        if (card.Data != null &&
            card.Data.CardClass !=
                Card.CardClass.AttackSteal &&
            card.Data.cardEffect != null)
        {
            GameLog.Write(
                "AttackResolver",
                $"AttackDelayRoutine applying card effect to target card={card.Data.name} targetPlayer={targetPlayer.netId}");

            card.Data.cardEffect.ApplyEffectToTarget(
                targetPlayer);
        }

        if (victimConnection != null)
        {
            GameLog.Write(
                "AttackResolver",
                $"AttackDelayRoutine calling TargetStartDamagePass victim={targetPlayer.netId}");

            context.StartDamagePass(
                victimConnection,
                targetPlayer.netIdentity);
        }

        context.StartAttackVortex(
            card,
            attackPlayer);

        context.ReleaseAttackRoutine();

        GameLog.Write(
            "AttackResolver",
            "AttackDelayRoutine end: attack applied");

        yield break;
    }
}
