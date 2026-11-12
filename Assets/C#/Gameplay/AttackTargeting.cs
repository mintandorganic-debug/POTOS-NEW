public class AttackTargeting
{
    private readonly IAttackTargetingContext context;
    private readonly AttackState state;
    private readonly TargetSelectionTimer targetTimer;

    public AttackTargeting(
        IAttackTargetingContext context,
        AttackState state,
        TargetSelectionTimer targetTimer)
    {
        this.context = context;
        this.state = state;
        this.targetTimer = targetTimer;

        GameLog.Write(
            "AttackTargeting",
            "Created");
    }

    public void SetTarget(
        PlayerInteractable target)
    {
        GameLog.Write(
            "AttackTargeting",
            $"SetTarget target={(target != null ? target.name : "NULL")} isActive={state.IsActive} currentTarget={(state.Target != null ? state.Target.name : "NULL")}");

        if (!state.IsActive)
        {
            GameLog.Write(
                "AttackTargeting",
                "SetTarget rejected: sequence not active");

            return;
        }

        if (state.Card == null ||
            state.Attacker == null)
        {
            GameLog.Write(
                "AttackTargeting",
                $"SetTarget rejected: selectedCard or attacker is null selectedCard={(state.Card != null && state.Card.Data != null ? state.Card.Data.name : "NULL")} attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")}");

            return;
        }

        if (target == null)
        {
            GameLog.Write(
                "AttackTargeting",
                "SetTarget rejected: target is null");

            return;
        }

        if (state.Target != null)
        {
            GameLog.Write(
                "AttackTargeting",
                $"SetTarget rejected: target already set target={state.Target.name}");

            return;
        }

        Player targetPlayer =
            target.GetComponentInParent<Player>();

        if (targetPlayer == null ||
            targetPlayer == state.Attacker)
        {
            GameLog.Write(
                "AttackTargeting",
                $"SetTarget rejected: invalid target player targetPlayer={(targetPlayer != null ? targetPlayer.netId.ToString() : "NULL")} attacker={state.Attacker.netId}");

            return;
        }

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette == null ||
            !roulette.IsRunning)
        {
            GameLog.Write(
                "AttackTargeting",
                $"SetTarget rejected: roulette missing or not running rouletteExists={roulette != null} isRunning={(roulette != null ? roulette.IsRunning.ToString() : "NULL")}");

            return;
        }

        if (state.Card.Data != null &&
            state.Card.Data.CardClass ==
                Card.CardClass.Defence)
        {
            GameLog.Write(
                "AttackTargeting",
                $"SetTarget rejected: selected card is a Defence card card={state.Card.Data.name}");

            return;
        }

        state.Target = target;

        GameLog.Write(
            "AttackTargeting",
            $"SetTarget target set target={target.name} targetPlayer={targetPlayer.netId} attacker={state.Attacker.netId}");

        targetTimer.Stop();

        roulette.SetTarget(
            target.EnemyCardMarker,
            OnRouletteReachedTarget);

        GameLog.Write(
            "AttackTargeting",
            "SetTarget roulette.SetTarget called, waiting for OnRouletteReachedTarget");
    }

    private void OnRouletteReachedTarget()
    {
        GameLog.Write(
            "AttackTargeting",
            $"OnRouletteReachedTarget isActive={state.IsActive} target={(state.Target != null ? state.Target.name : "NULL")} attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")}");

        if (!state.IsActive ||
            state.Card == null ||
            state.Attacker == null ||
            state.Target == null)
        {
            GameLog.Write(
                "AttackTargeting",
                $"OnRouletteReachedTarget ignored: sequence not valid isActive={state.IsActive} selectedCard={(state.Card != null && state.Card.Data != null ? state.Card.Data.name : "NULL")}");

            return;
        }

        Player victim =
            state.Target.GetComponentInParent<Player>();

        if (victim == null)
        {
            GameLog.Write(
                "AttackTargeting",
                "OnRouletteReachedTarget failed: victim Player not found, cancelling");

            context.CancelSequence();
            return;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
        {
            GameLog.Write(
                "AttackTargeting",
                "OnRouletteReachedTarget failed: GameMaster.Instance is null, cancelling");

            context.CancelSequence();
            return;
        }

        // The target becomes the actual game target only after
        // the roulette has reached and stopped at the target.
        if (!gameMaster.SelectTarget(
                victim))
        {
            GameLog.Write(
                "AttackTargeting",
                $"OnRouletteReachedTarget failed: GameMaster.SelectTarget returned false victim={victim.netId}, cancelling");

            context.CancelSequence();
            return;
        }

        if (context.HasAttackRoutine)
        {
            GameLog.Write(
                "AttackTargeting",
                "OnRouletteReachedTarget stopping previous attackRoutine");

            context.StopAttackRoutine();
        }

        GameLog.Write(
            "AttackTargeting",
            $"OnRouletteReachedTarget starting AttackDelayRoutine victim={victim.netId}");

        context.StartAttackRoutine();
    }
}
