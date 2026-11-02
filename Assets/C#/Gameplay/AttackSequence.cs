using Mirror;
using System.Collections;
using UnityEngine;

public class AttackSequence : NetworkBehaviour, IAttackResolverContext, IAttackVortexContext, IAttackLifecycleContext, IAttackCommandContext
{
    public static AttackSequence Instance { get; private set; }

    [Header("Timing")]
    [SerializeField] private float targetSelectionTime = 10f;
    [SerializeField] private float targetActionTime = 5f;
    [SerializeField] private float stealActionTime = 5f;

    [Header("Steal")]
    [SerializeField] private int stealCoinsAmount = 5;
    [SerializeField] private float stealCoinsRainDuration = 5f;

    private readonly AttackState state = new();

    private Coroutine attackRoutine;
    private TargetSelectionTimer targetTimer;
    private AttackResolver resolver;
    private AttackVortexFlow vortex;
    private AttackLifecycle lifecycle;
    private AttackCommandHandler commands;

    private StealPhase steal;

    public bool IsActive =>
        state.IsActive;

    public Card SelectedCard =>
        state.Card;

    public PlayerInteractable Target =>
        state.Target;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        GameLog.Write(
            "AttackSequence",
            $"Awake instanceExists={Instance != null} isDuplicate={Instance != null && Instance != this}");

        if (Instance != null &&
            Instance != this)
        {
            GameLog.Write(
                "AttackSequence",
                "Awake duplicate instance, destroying gameObject");

            Destroy(gameObject);
            return;
        }

        Instance = this;

        GameLog.Write(
            "AttackSequence",
            $"Awake Instance set timing: targetSelectionTime={targetSelectionTime} targetActionTime={targetActionTime} stealActionTime={stealActionTime} stealCoinsAmount={stealCoinsAmount} stealCoinsRainDuration={stealCoinsRainDuration}");

        steal =
            new StealPhase(
                this,
                stealActionTime,
                stealCoinsAmount,
                stealCoinsRainDuration,
                () => state.IsActive,
                FinishSequence);

        targetTimer =
            new TargetSelectionTimer(
                this,
                state,
                targetSelectionTime,
                CancelSequence);

        resolver =
            new AttackResolver(
                this,
                state,
                targetActionTime);

        vortex =
            new AttackVortexFlow(
                this,
                state,
                steal,
                targetTimer);

        lifecycle =
            new AttackLifecycle(
                this,
                this,
                state,
                steal,
                targetTimer,
                vortex);

        commands =
            new AttackCommandHandler(
                this,
                state);

        GameLog.Write(
            "AttackSequence",
            "Awake StealPhase created");
    }

    private void OnDestroy()
    {
        GameLog.Write(
            "AttackSequence",
            $"OnDestroy wasInstance={Instance == this} isActive={state.IsActive}");

        if (Instance == this)
            Instance = null;
    }

    // =========================================================
    // CLIENT -> SERVER
    // =========================================================

    [Command(requiresAuthority = false)]
    public void CmdStartSequence(
        NetworkIdentity cardIdentity,
        NetworkConnectionToClient sender = null)
    {
        GameLog.Write(
            "AttackSequence",
            $"CmdStartSequence cardNetId={(cardIdentity != null ? cardIdentity.netId.ToString() : "NULL")} senderConn={(sender != null ? sender.connectionId.ToString() : "NULL")} isServer={isServer} isActive={state.IsActive}");

        if (!isServer)
        {
            GameLog.Write(
                "AttackSequence",
                "CmdStartSequence rejected: not server");

            return;
        }

        commands.StartRequested(
            cardIdentity,
            sender);
    }

    [Command(requiresAuthority = false)]
    public void CmdSetTarget(
        NetworkIdentity targetIdentity,
        NetworkConnectionToClient sender = null)
    {
        GameLog.Write(
            "AttackSequence",
            $"CmdSetTarget targetNetId={(targetIdentity != null ? targetIdentity.netId.ToString() : "NULL")} senderConn={(sender != null ? sender.connectionId.ToString() : "NULL")} isServer={isServer} isActive={state.IsActive}");

        if (!isServer)
        {
            GameLog.Write(
                "AttackSequence",
                "CmdSetTarget rejected: not server");

            return;
        }

        commands.SetTargetRequested(
            targetIdentity,
            sender);
    }

    [Command(requiresAuthority = false)]
    public void CmdCancelSequence(
        NetworkConnectionToClient sender = null)
    {
        GameLog.Write(
            "AttackSequence",
            $"CmdCancelSequence senderConn={(sender != null ? sender.connectionId.ToString() : "NULL")} isServer={isServer} isActive={state.IsActive}");

        if (!isServer ||
            sender == null)
        {
            GameLog.Write(
                "AttackSequence",
                "CmdCancelSequence rejected: not server or sender is null");

            return;
        }

        commands.CancelRequested(
            sender);
    }

    // =========================================================
    // SERVER
    // =========================================================

    [Server]
    private void StartSequence(
        Card card,
        Player attacker)
    {
        lifecycle.StartSequence(
            card,
            attacker);
    }
    // =========================================================
    // TARGET SELECTION TIMER
    // =========================================================

    [Server]
    private void StartTargetSelectionTimer()
    {
        targetTimer.Start();
    }

    [Server]
    private void StopTargetSelectionTimer()
    {
        targetTimer.Stop();
    }

    // =========================================================
    // DEFENSE SEQUENCE
    // =========================================================

    [Server]
    private void StartDefenseSequence(
        Card defenseCard,
        Player defensePlayer)
    {
        lifecycle.StartDefenseSequence(
            defenseCard,
            defensePlayer);
    }
    // =========================================================
    // TARGET
    // =========================================================

    [Server]
    private void SetTarget(
        PlayerInteractable target)
    {
        GameLog.Write(
            "AttackSequence",
            $"SetTarget target={(target != null ? target.name : "NULL")} isActive={state.IsActive} currentTarget={(state.Target != null ? state.Target.name : "NULL")}");

        if (!state.IsActive)
        {
            GameLog.Write(
                "AttackSequence",
                "SetTarget rejected: sequence not active");

            return;
        }

        if (state.Card == null ||
            state.Attacker == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"SetTarget rejected: selectedCard or attacker is null selectedCard={(state.Card != null && state.Card.Data != null ? state.Card.Data.name : "NULL")} attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")}");

            return;
        }

        if (target == null)
        {
            GameLog.Write(
                "AttackSequence",
                "SetTarget rejected: target is null");

            return;
        }

        if (state.Target != null)
        {
            GameLog.Write(
                "AttackSequence",
                $"SetTarget rejected: target already set target={state.Target.name}");

            return;
        }

        Player targetPlayer =
            target.GetComponentInParent<Player>();

        if (targetPlayer == null ||
            targetPlayer == state.Attacker)
        {
            GameLog.Write(
                "AttackSequence",
                $"SetTarget rejected: invalid target player targetPlayer={(targetPlayer != null ? targetPlayer.netId.ToString() : "NULL")} attacker={state.Attacker.netId}");

            return;
        }

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette == null ||
            !roulette.IsRunning)
        {
            GameLog.Write(
                "AttackSequence",
                $"SetTarget rejected: roulette missing or not running rouletteExists={roulette != null} isRunning={(roulette != null ? roulette.IsRunning.ToString() : "NULL")}");

            return;
        }

        if (state.Card.Data != null &&
            state.Card.Data.CardClass ==
                Card.CardClass.Defence)
        {
            GameLog.Write(
                "AttackSequence",
                $"SetTarget rejected: selected card is a Defence card card={state.Card.Data.name}");

            return;
        }

        state.Target = target;

        GameLog.Write(
            "AttackSequence",
            $"SetTarget target set target={target.name} targetPlayer={targetPlayer.netId} attacker={state.Attacker.netId}");

        StopTargetSelectionTimer();

        roulette.SetTarget(
            target.EnemyCardMarker,
            OnRouletteReachedTarget);

        GameLog.Write(
            "AttackSequence",
            "SetTarget roulette.SetTarget called, waiting for OnRouletteReachedTarget");
    }

    // =========================================================
    // ATTACK ROULETTE CALLBACK
    // =========================================================

    [Server]
    private void OnRouletteReachedTarget()
    {
        GameLog.Write(
            "AttackSequence",
            $"OnRouletteReachedTarget isActive={state.IsActive} target={(state.Target != null ? state.Target.name : "NULL")} attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")}");

        if (!state.IsActive ||
            state.Card == null ||
            state.Attacker == null ||
            state.Target == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"OnRouletteReachedTarget ignored: sequence not valid isActive={state.IsActive} selectedCard={(state.Card != null && state.Card.Data != null ? state.Card.Data.name : "NULL")}");

            return;
        }

        Player victim =
            state.Target.GetComponentInParent<Player>();

        if (victim == null)
        {
            GameLog.Write(
                "AttackSequence",
                "OnRouletteReachedTarget failed: victim Player not found, cancelling");

            CancelSequence();
            return;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
        {
            GameLog.Write(
                "AttackSequence",
                "OnRouletteReachedTarget failed: GameMaster.Instance is null, cancelling");

            CancelSequence();
            return;
        }

        // The target becomes the actual game target only after
        // the roulette has reached and stopped at the target.
        if (!gameMaster.SelectTarget(
                victim))
        {
            GameLog.Write(
                "AttackSequence",
                $"OnRouletteReachedTarget failed: GameMaster.SelectTarget returned false victim={victim.netId}, cancelling");

            CancelSequence();
            return;
        }

        if (attackRoutine != null)
        {
            GameLog.Write(
                "AttackSequence",
                "OnRouletteReachedTarget stopping previous attackRoutine");

            StopCoroutine(
                attackRoutine);
        }

        GameLog.Write(
            "AttackSequence",
            $"OnRouletteReachedTarget starting AttackDelayRoutine victim={victim.netId}");

        attackRoutine =
            StartCoroutine(
                resolver.Run());
    }

    // =========================================================
    // ATTACK TIMER
    // =========================================================

    // =========================================================
    // ATTACK VORTEX
    // =========================================================

    [Server]
    private void StartAttackVortex(
        Card card,
        Player attackPlayer)
    {
        vortex.StartAttackVortex(
            card,
            attackPlayer);
    }

    [Server]
    private void OnDefenseVortexCompleted(
        Card card)
    {
        vortex.OnDefenseVortexCompleted(
            card);
    }

    // =========================================================
    // CLIENT RPC
    // =========================================================

    [ClientRpc]
    private void RpcStartAttackPreparation(
        NetworkIdentity attackerIdentity,
        string actionId)
    {
        GameLog.Write(
            "AttackSequence",
            $"RpcStartAttackPreparation attackerNetId={(attackerIdentity != null ? attackerIdentity.netId.ToString() : "NULL")} actionId={actionId}");

        if (attackerIdentity == null)
        {
            GameLog.Write(
                "AttackSequence",
                "RpcStartAttackPreparation skipped: attackerIdentity is null");

            return;
        }

        Player player =
            attackerIdentity.GetComponent<Player>();

        if (player == null ||
            player.animator == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"RpcStartAttackPreparation skipped: playerNull={player == null} animatorNull={player == null || player.animator == null}");

            return;
        }

        GameLog.Write(
            "AttackSequence",
            $"RpcStartAttackPreparation starting attack animation player={player.netId} actionId={actionId}");

        player.animator.StartAttackAnimation(
            actionId);
    }

    [ClientRpc]
    private void RpcTriggerAttackAnimation(
        NetworkIdentity attackerIdentity)
    {
        GameLog.Write(
            "AttackSequence",
            $"RpcTriggerAttackAnimation attackerNetId={(attackerIdentity != null ? attackerIdentity.netId.ToString() : "NULL")}");

        if (attackerIdentity == null)
        {
            GameLog.Write(
                "AttackSequence",
                "RpcTriggerAttackAnimation skipped: attackerIdentity is null");

            return;
        }

        Player player =
            attackerIdentity.GetComponent<Player>();

        if (player == null ||
            player.animator == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"RpcTriggerAttackAnimation skipped: playerNull={player == null} animatorNull={player == null || player.animator == null}");

            return;
        }

        GameLog.Write(
            "AttackSequence",
            $"RpcTriggerAttackAnimation triggering attack player={player.netId}");

        player.animator.TriggerAttack();
    }

    // =========================================================
    // TARGET RPC
    // =========================================================

    [TargetRpc]
    private void TargetShowDefenseTimer(
        NetworkConnectionToClient connection,
        float time)
    {
        GameLog.Write(
            "AttackSequence",
            $"TargetShowDefenseTimer time={time} hudExists={HudController.Instance != null}");

        HudController.Instance?.ShowDefenseTimer(
            time);
    }

    [TargetRpc]
    private void TargetSetDefenseTimer(
        NetworkConnectionToClient connection,
        float time)
    {
        HudController.Instance?.SetDefenseTimer(
            time);
    }

    [TargetRpc]
    private void TargetHideDefenseTimer(
        NetworkConnectionToClient connection)
    {
        GameLog.Write(
            "AttackSequence",
            $"TargetHideDefenseTimer hudExists={HudController.Instance != null}");

        HudController.Instance?.HideDefenseTimer();
    }

    [TargetRpc]
    private void TargetStartDamagePass(
        NetworkConnectionToClient connection,
        NetworkIdentity victimIdentity)
    {
        GameLog.Write(
            "AttackSequence",
            $"TargetStartDamagePass victimNetId={(victimIdentity != null ? victimIdentity.netId.ToString() : "NULL")}");

        if (victimIdentity == null)
        {
            GameLog.Write(
                "AttackSequence",
                "TargetStartDamagePass skipped: victimIdentity is null");

            return;
        }

        Player victim =
            victimIdentity.GetComponent<Player>();

        if (victim == null)
        {
            GameLog.Write(
                "AttackSequence",
                "TargetStartDamagePass skipped: victim Player not found");

            return;
        }

        GameLog.Write(
            "AttackSequence",
            $"TargetStartDamagePass starting damage pass victim={victim.netId}");

        victim.StartDamagePass();
    }

    // =========================================================
    // CANCEL
    // =========================================================

    [Server]
    private void CancelSequence()
    {
        lifecycle.CancelSequence();
    }
    [ClientRpc]
    private void RpcCancelAttackAnimation(
        NetworkIdentity attackerIdentity)
    {
        GameLog.Write(
            "AttackSequence",
            $"RpcCancelAttackAnimation attackerNetId={(attackerIdentity != null ? attackerIdentity.netId.ToString() : "NULL")}");

        if (attackerIdentity == null)
        {
            GameLog.Write(
                "AttackSequence",
                "RpcCancelAttackAnimation skipped: attackerIdentity is null");

            return;
        }

        Player player =
            attackerIdentity.GetComponent<Player>();

        if (player == null ||
            player.animator == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"RpcCancelAttackAnimation skipped: playerNull={player == null} animatorNull={player == null || player.animator == null}");

            return;
        }

        GameLog.Write(
            "AttackSequence",
            $"RpcCancelAttackAnimation cancelling attack animation player={player.netId}");

        player.animator.CancelAttackAnimation(
            string.Empty);
    }

    // =========================================================
    // FINISH
    // =========================================================

    [Server]
    private void FinishSequence()
    {
        lifecycle.FinishSequence();
    }
    // =========================================================
    // VALIDATION
    // =========================================================

    void IAttackResolverContext.ShowDefenseTimer(
        NetworkConnectionToClient connection,
        float time)
    {
        TargetShowDefenseTimer(
            connection,
            time);
    }

    void IAttackResolverContext.SetDefenseTimer(
        NetworkConnectionToClient connection,
        float time)
    {
        TargetSetDefenseTimer(
            connection,
            time);
    }

    void IAttackResolverContext.HideDefenseTimer(
        NetworkConnectionToClient connection)
    {
        TargetHideDefenseTimer(
            connection);
    }

    void IAttackResolverContext.TriggerAttackAnimation(
        NetworkIdentity attackerIdentity)
    {
        RpcTriggerAttackAnimation(
            attackerIdentity);
    }

    void IAttackResolverContext.StartDamagePass(
        NetworkConnectionToClient connection,
        NetworkIdentity victimIdentity)
    {
        TargetStartDamagePass(
            connection,
            victimIdentity);
    }

    void IAttackResolverContext.StartAttackVortex(
        Card card,
        Player attackPlayer)
    {
        StartAttackVortex(
            card,
            attackPlayer);
    }

    void IAttackResolverContext.CancelSequence()
    {
        CancelSequence();
    }

    void IAttackResolverContext.ReleaseAttackRoutine()
    {
        attackRoutine = null;
    }

    void IAttackVortexContext.FinishSequence()
    {
        FinishSequence();
    }

    void IAttackVortexContext.ReleaseAttackRoutine()
    {
        attackRoutine = null;
    }

    bool IAttackLifecycleContext.HasAttackRoutine =>
        attackRoutine != null;

    void IAttackLifecycleContext.StopAttackRoutine()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(
                attackRoutine);

            attackRoutine = null;
        }
    }

    void IAttackLifecycleContext.ReleaseAttackRoutine()
    {
        attackRoutine = null;
    }

    void IAttackLifecycleContext.StartAttackPreparation(
        NetworkIdentity attackerIdentity,
        string actionId)
    {
        RpcStartAttackPreparation(
            attackerIdentity,
            actionId);
    }

    void IAttackLifecycleContext.TriggerAttackAnimation(
        NetworkIdentity attackerIdentity)
    {
        RpcTriggerAttackAnimation(
            attackerIdentity);
    }

    void IAttackLifecycleContext.CancelAttackAnimation(
        NetworkIdentity attackerIdentity)
    {
        RpcCancelAttackAnimation(
            attackerIdentity);
    }

    void IAttackCommandContext.StartSequence(
        Card card,
        Player attacker)
    {
        StartSequence(
            card,
            attacker);
    }

    void IAttackCommandContext.StartDefenseSequence(
        Card defenseCard,
        Player defensePlayer)
    {
        StartDefenseSequence(
            defenseCard,
            defensePlayer);
    }

    void IAttackCommandContext.SetTarget(
        PlayerInteractable target)
    {
        SetTarget(
            target);
    }

    void IAttackCommandContext.CancelSequence()
    {
        CancelSequence();
    }
}
