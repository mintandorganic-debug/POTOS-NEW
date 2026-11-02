using UnityEngine;

public class AttackLifecycle
{
    private readonly MonoBehaviour owner;
    private readonly IAttackLifecycleContext context;
    private readonly AttackState state;
    private readonly StealPhase steal;
    private readonly TargetSelectionTimer targetTimer;
    private readonly AttackVortexFlow vortex;

    public AttackLifecycle(
        MonoBehaviour owner,
        IAttackLifecycleContext context,
        AttackState state,
        StealPhase steal,
        TargetSelectionTimer targetTimer,
        AttackVortexFlow vortex)
    {
        this.owner = owner;
        this.context = context;
        this.state = state;
        this.steal = steal;
        this.targetTimer = targetTimer;
        this.vortex = vortex;

        GameLog.Write(
            "AttackLifecycle",
            "Created");
    }

    public void StartSequence(
        Card card,
        Player attacker)
    {
        GameLog.Write(
            "AttackLifecycle",
            $"StartSequence card={(card != null && card.Data != null ? card.Data.name : "NULL")} attacker={(attacker != null ? attacker.netId.ToString() : "NULL")}");

        if (card == null ||
            attacker == null)
        {
            GameLog.Write(
                "AttackLifecycle",
                "StartSequence rejected: card or attacker is null");

            return;
        }

        if (!card.IsAvailable ||
            card.IsInRoulette)
        {
            GameLog.Write(
                "AttackLifecycle",
                $"StartSequence rejected: card unavailable isAvailable={card.IsAvailable} isInRoulette={card.IsInRoulette}");

            return;
        }

        if (!AttackValidator.IsCardOwnedByPlayer(
                card,
                attacker))
        {
            GameLog.Write(
                "AttackLifecycle",
                $"StartSequence rejected: card not owned by attacker attacker={attacker.netId}");

            return;
        }

        GameLog.Write(
            "AttackLifecycle",
            $"StartSequence cancelling previous sequence wasActive={state.IsActive}");

        CancelSequence();

        state.Begin(
            card,
            attacker);

        GameLog.Write(
            "AttackLifecycle",
            $"StartSequence state set isActive=true selectedCard={(card.Data != null ? card.Data.name : "NULL")} attacker={attacker.netId} target=NULL");

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette == null)
        {
            GameLog.Write(
                "AttackLifecycle",
                "StartSequence failed: RouletteRotator.Instance is null, cancelling");

            Debug.LogError(
                "AttackSequence: RouletteRotator.Instance not found.",
                owner);

            CancelSequence();
            return;
        }

        card.SetInRoulette(true);

        roulette.AttachCard(
            card.transform);

        roulette.StartRoulette(
            attacker.transform,
            RouletteRotator.Pattern.Direct);

        GameLog.Write(
            "AttackLifecycle",
            $"StartSequence roulette started pattern=Direct attacker={attacker.netId}");

        GameLog.Write(
            "AttackLifecycle",
            $"StartSequence calling RpcStartAttackPreparation attacker={attacker.netId} actionId={(card.Data != null ? card.Data.ActionId : string.Empty)}");

        context.StartAttackPreparation(
            attacker.netIdentity,
            card.Data != null
                ? card.Data.ActionId
                : string.Empty);

        targetTimer.Start();
    }

    public void StartDefenseSequence(
        Card defenseCard,
        Player defensePlayer)
    {
        GameLog.Write(
            "AttackLifecycle",
            $"StartDefenseSequence defenseCard={(defenseCard != null && defenseCard.Data != null ? defenseCard.Data.name : "NULL")} defensePlayer={(defensePlayer != null ? defensePlayer.netId.ToString() : "NULL")}");

        if (defenseCard == null ||
            defensePlayer == null)
        {
            GameLog.Write(
                "AttackLifecycle",
                "StartDefenseSequence rejected: defenseCard or defensePlayer is null");

            return;
        }

        if (!defenseCard.IsAvailable ||
            defenseCard.IsInRoulette)
        {
            GameLog.Write(
                "AttackLifecycle",
                $"StartDefenseSequence rejected: card unavailable isAvailable={defenseCard.IsAvailable} isInRoulette={defenseCard.IsInRoulette}");

            return;
        }

        if (defenseCard.Data == null ||
            defenseCard.Data.CardClass !=
                Card.CardClass.Defence)
        {
            GameLog.Write(
                "AttackLifecycle",
                "StartDefenseSequence rejected: card is not a Defence card");

            return;
        }

        if (!AttackValidator.IsCardOwnedByPlayer(
                defenseCard,
                defensePlayer))
        {
            GameLog.Write(
                "AttackLifecycle",
                $"StartDefenseSequence rejected: card not owned by defensePlayer defensePlayer={defensePlayer.netId}");

            return;
        }

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette == null)
        {
            GameLog.Write(
                "AttackLifecycle",
                "StartDefenseSequence failed: RouletteRotator.Instance is null");

            Debug.LogError(
                "AttackSequence: RouletteRotator.Instance not found.",
                owner);

            return;
        }

        targetTimer.Stop();

        if (context.HasAttackRoutine)
        {
            GameLog.Write(
                "AttackLifecycle",
                "StartDefenseSequence stopping attackRoutine (defense timer)");

            context.StopAttackRoutine();
        }

        state.Card =
            defenseCard;

        GameLog.Write(
            "AttackLifecycle",
            $"StartDefenseSequence selectedCard changed to defense card={defenseCard.Data.name}");

        defenseCard.SetInRoulette(true);

        roulette.StopRoulette();

        roulette.AttachDefenseCard(
            defenseCard.transform);

        GameLog.Write(
            "AttackLifecycle",
            $"StartDefenseSequence starting Vortex roulette toward attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")}");

        roulette.StartRoulette(
            state.Attacker.transform,
            RouletteRotator.Pattern.Vortex,
            () => vortex.OnDefenseVortexCompleted(defenseCard));

        if (state.Attacker != null)
        {
            GameLog.Write(
                "AttackLifecycle",
                $"StartDefenseSequence calling RpcTriggerAttackAnimation attacker={state.Attacker.netId}");

            context.TriggerAttackAnimation(
                state.Attacker.netIdentity);
        }

        GameLog.Write(
            "AttackLifecycle",
            $"StartDefenseSequence calling RpcStartAttackPreparation defensePlayer={defensePlayer.netId} actionId={defenseCard.Data.ActionId}");

        context.StartAttackPreparation(
            defensePlayer.netIdentity,
            defenseCard.Data.ActionId);
    }

    public void CancelSequence()
    {
        GameLog.Write(
            "AttackLifecycle",
            $"CancelSequence isActive={state.IsActive} card={(state.Card != null && state.Card.Data != null ? state.Card.Data.name : "NULL")} attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")} target={(state.Target != null ? state.Target.name : "NULL")}");

        if (!state.IsActive)
        {
            GameLog.Write(
                "AttackLifecycle",
                "CancelSequence skipped: sequence not active");

            return;
        }

        targetTimer.Stop();

        steal.Stop();

        if (context.HasAttackRoutine)
        {
            GameLog.Write(
                "AttackLifecycle",
                "CancelSequence stopping attackRoutine");

            context.StopAttackRoutine();
        }

        Player previousAttacker =
            state.Attacker;

        Card previousCard =
            state.Card;

        RouletteCardReleaser.Release(
            previousCard);

        if (previousCard != null)
        {
            GameLog.Write(
                "AttackLifecycle",
                $"CancelSequence returning card to owner card={(previousCard.Data != null ? previousCard.Data.name : "NULL")}");

            previousCard.ReturnToOwner();
        }

        if (previousAttacker != null)
        {
            GameLog.Write(
                "AttackLifecycle",
                $"CancelSequence calling RpcCancelAttackAnimation attacker={previousAttacker.netId}");

            context.CancelAttackAnimation(
                previousAttacker.netIdentity);
        }

        state.Reset();

        GameLog.Write(
            "AttackLifecycle",
            "CancelSequence state cleared isActive=false");

        if (GameMaster.Instance != null)
        {
            GameLog.Write(
                "AttackLifecycle",
                "CancelSequence calling GameMaster.EndTurn");

            GameMaster.Instance.EndTurn();
        }
    }

    public void FinishSequence()
    {
        GameLog.Write(
            "AttackLifecycle",
            $"FinishSequence isActive={state.IsActive} card={(state.Card != null && state.Card.Data != null ? state.Card.Data.name : "NULL")} attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")}");

        targetTimer.Stop();

        steal.Stop();

        RouletteCardReleaser.Release(
            state.Card);

        state.Reset();
        context.ReleaseAttackRoutine();

        GameLog.Write(
            "AttackLifecycle",
            "FinishSequence state cleared isActive=false");
    }
}
