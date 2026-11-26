public class AttackVortexFlow
{
    private readonly IAttackVortexContext context;
    private readonly AttackState state;
    private readonly StealPhase steal;
    private readonly TargetSelectionTimer targetTimer;

    public AttackVortexFlow(
        IAttackVortexContext context,
        AttackState state,
        StealPhase steal,
        TargetSelectionTimer targetTimer)
    {
        this.context = context;
        this.state = state;
        this.steal = steal;
        this.targetTimer = targetTimer;

        GameLog.Write(
            "AttackVortexFlow",
            "Created");
    }

    public void StartAttackVortex(
        Card card,
        Player attackPlayer)
    {
        GameLog.Write(
            "AttackVortexFlow",
            $"StartAttackVortex card={(card != null && card.Data != null ? card.Data.name : "NULL")} attackPlayer={(attackPlayer != null ? attackPlayer.netId.ToString() : "NULL")}");

        if (card == null ||
            attackPlayer == null)
        {
            GameLog.Write(
                "AttackVortexFlow",
                "StartAttackVortex failed: card or attackPlayer is null, finishing sequence");

            context.FinishSequence();
            return;
        }

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette == null)
        {
            GameLog.Write(
                "AttackVortexFlow",
                "StartAttackVortex failed: RouletteRotator.Instance is null, destroying card and ending turn");

            RouletteCardReleaser.Destroy(
                card);

            if (GameMaster.Instance != null)
            {
                GameLog.Write(
                    "AttackVortexFlow",
                    "StartAttackVortex calling GameMaster.EndTurn");

                GameMaster.Instance.EndTurn();
            }

            return;
        }

        card.SetInRoulette(true);

        roulette.StopRoulette();

        roulette.AttachCard(
            card.transform);

        bool isStealCard =
            card.Data != null &&
            card.Data.CardClass ==
                Card.CardClass.AttackSteal;

        GameLog.Write(
            "AttackVortexFlow",
            $"StartAttackVortex isStealCard={isStealCard}");

        if (isStealCard)
        {
            Player victim =
                state.Target != null
                    ? state.Target.GetComponentInParent<Player>()
                    : null;

            GameLog.Write(
                "AttackVortexFlow",
                $"StartAttackVortex steal.TakeCoins attacker={attackPlayer.netId} victim={(victim != null ? victim.netId.ToString() : "NULL")}");

            steal.TakeCoins(
                attackPlayer,
                victim);
        }

        GameLog.Write(
            "AttackVortexFlow",
            $"StartAttackVortex starting Vortex roulette attacker={attackPlayer.netId}");

        roulette.StartRoulette(
            attackPlayer.transform,
            RouletteRotator.Pattern.Vortex,
            () => OnAttackVortexCompleted(card));
    }

    private void OnAttackVortexCompleted(
        Card card)
    {
        bool isStealCard =
            card != null &&
            card.Data != null &&
            card.Data.CardClass ==
                Card.CardClass.AttackSteal;

        GameLog.Write(
            "AttackVortexFlow",
            $"OnAttackVortexCompleted card={(card != null && card.Data != null ? card.Data.name : "NULL")} isStealCard={isStealCard}");

        AttackSequence.Instance?.RpcPlaySequenceAudio(
            AttackSequence.GetCardAudioKey(card),
            3,
            null);

        RouletteCardReleaser.Destroy(
            card);

        if (isStealCard)
        {
            GameLog.Write(
                "AttackVortexFlow",
                "OnAttackVortexCompleted steal card, requesting GameMaster.BeginStealPhase");

            if (GameMaster.Instance == null ||
                !GameMaster.Instance.BeginStealPhase())
            {
                GameLog.Write(
                    "AttackVortexFlow",
                    $"OnAttackVortexCompleted steal phase not started (GameMaster exists={GameMaster.Instance != null}), finishing sequence");

                context.FinishSequence();
                return;
            }

            GameLog.Write(
                "AttackVortexFlow",
                "OnAttackVortexCompleted steal phase started, calling steal.Begin");

            steal.Begin();
            return;
        }

        if (GameMaster.Instance != null)
        {
            GameLog.Write(
                "AttackVortexFlow",
                "OnAttackVortexCompleted calling GameMaster.EndTurn");

            GameMaster.Instance.EndTurn();
        }

        GameLog.Write(
            "AttackVortexFlow",
            "OnAttackVortexCompleted finishing sequence");

        context.FinishSequence();
    }

    public void OnDefenseVortexCompleted(
        Card card)
    {
        GameLog.Write(
            "AttackVortexFlow",
            $"OnDefenseVortexCompleted card={(card != null && card.Data != null ? card.Data.name : "NULL")}");

        if (card == null)
        {
            GameLog.Write(
                "AttackVortexFlow",
                "OnDefenseVortexCompleted card is null, finishing defense sequence");

            FinishDefenseSequence();
            return;
        }

        GameLog.Write(
            "AttackVortexFlow",
            $"OnDefenseVortexCompleted destroying defense card={(card.Data != null ? card.Data.name : "NULL")}");

        AttackSequence.Instance?.RpcPlaySequenceAudio(
            AttackSequence.GetCardAudioKey(card),
            3,
            null);

        RouletteCardReleaser.Destroy(
            card);

        if (GameMaster.Instance != null)
        {
            GameLog.Write(
                "AttackVortexFlow",
                "OnDefenseVortexCompleted calling GameMaster.EndTurn");

            GameMaster.Instance.EndTurn();
        }

        state.Reset();
        context.ReleaseAttackRoutine();
        targetTimer.Forget();

        GameLog.Write(
            "AttackVortexFlow",
            "OnDefenseVortexCompleted state cleared isActive=false");
    }

    private void FinishDefenseSequence()
    {
        GameLog.Write(
            "AttackVortexFlow",
            $"FinishDefenseSequence isActive={state.IsActive}");

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette != null)
        {
            roulette.StopRoulette();
        }

        if (GameMaster.Instance != null)
        {
            GameLog.Write(
                "AttackVortexFlow",
                "FinishDefenseSequence calling GameMaster.EndTurn");

            GameMaster.Instance.EndTurn();
        }

        state.Reset();
        context.ReleaseAttackRoutine();
        targetTimer.Forget();

        GameLog.Write(
            "AttackVortexFlow",
            "FinishDefenseSequence state cleared isActive=false");
    }
}
