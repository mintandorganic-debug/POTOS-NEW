using Mirror;
using System.Collections;
using UnityEngine;

public class AttackSequence : NetworkBehaviour, IAttackResolverContext, IAttackVortexContext, IAttackLifecycleContext, IAttackCommandContext, IAttackTargetingContext
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
    private AttackTargeting targeting;

    private StealPhase steal;

    public bool IsActive =>
        state.IsActive;

    public Card SelectedCard =>
        state.Card;

    public PlayerInteractable Target =>
        state.Target;

    public Player Attacker =>
        state.Attacker;

    public float TargetSelectionTime =>
        targetSelectionTime;

    public float DefenseWindowTime =>
        targetActionTime;

    public float StealWindowTime =>
        stealActionTime;

    public static string GetCardAudioKey(Card card)
    {
        if (card == null || card.Data == null)
            return string.Empty;

        return $"{card.Data.ActionId} {card.Data.CardName} {card.Data.name}";
    }

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

        targeting =
            new AttackTargeting(
                this,
                state,
                targetTimer);

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

        RpcPlaySequenceAudio(GetCardAudioKey(card), 0, attacker != null ? attacker.netIdentity : null);
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
        targeting.SetTarget(
            target);
    }

    // =========================================================
    // ATTACK ROULETTE CALLBACK
    // =========================================================

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

        GameAudioManager.Instance?.PlayHeartbeat();
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

        GameAudioManager.Instance?.StopHeartbeat();
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
        string cancelledCardKey = GetCardAudioKey(state.Card);
        Player prevAttacker = state.Attacker;

        lifecycle.CancelSequence();

        RpcPlaySequenceAudio(cancelledCardKey, 4, prevAttacker != null ? prevAttacker.netIdentity : null);
        GameMaster.Instance?.TryTriggerOtherPlayerScoff(prevAttacker, 0.5f);
    }

    [ClientRpc]
    public void RpcPlaySequenceAudio(string cardName, int eventType, NetworkIdentity attackerIdentity)
    {
        if (GameAudioManager.Instance == null)
            return;

        switch (eventType)
        {
            case 0:
                GameAudioManager.Instance.PlayWeaponReady(cardName, attackerIdentity);
                break;
            case 1:
                GameAudioManager.Instance.PlayWeaponAttackAndHit(cardName, true, attackerIdentity);
                GameAudioManager.Instance.PlayAttackSuccessful(0.5f);
                break;
            case 2:
                GameAudioManager.Instance.PlayWeaponAttackAndHit(cardName, false, attackerIdentity);
                GameAudioManager.Instance.PlayAttackUnsuccessful(0.5f);
                break;
            case 3:
                GameAudioManager.Instance.PlayCardBurn();
                break;
            case 4:
                GameAudioManager.Instance.SuppressHoverFor(0.4f);
                GameAudioManager.Instance.PlayWeaponCancel(cardName, attackerIdentity);
                break;
        }
    }

    // =========================================================
    // TESTING / BOT SERVER ENTRY POINTS
    // =========================================================

    [Server]
    public bool ServerPlayCard(Card card, Player cardOwner)
    {
        if (card == null || cardOwner == null) return false;
        if (!card.IsAvailable || card.IsInRoulette) return false;

        if (card.Data != null && card.Data.CardClass == Card.CardClass.Defence)
        {
            if (!state.IsActive || state.Attacker == null || state.Target == null) return false;
            StartDefenseSequence(card, cardOwner);
            return true;
        }

        StartSequence(card, cardOwner);
        return true;
    }

    [Server]
    public bool ServerSetTarget(Player requester, PlayerInteractable playerTarget)
    {
        if (requester == null || playerTarget == null) return false;
        if (!state.IsActive || state.Attacker == null || state.Card == null) return false;
        SetTarget(playerTarget);
        return true;
    }

    [Server]
    public bool ServerCancel(Player requester)
    {
        if (!state.IsActive || state.Attacker == null)
            return false;

        if (requester == null || requester != state.Attacker)
            return false;

        CancelSequence();
        return true;
    }

    [Server]
    public void ServerForceReset()
    {
        CancelSequence();
    }

    public void ConfigureTimers(float selectTime, float actionTime, float stealTime)
    {
        targetSelectionTime = selectTime;
        targetActionTime = actionTime;
        stealActionTime = stealTime;
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

    void IAttackTargetingContext.CancelSequence()
    {
        CancelSequence();
    }

    bool IAttackTargetingContext.HasAttackRoutine =>
        attackRoutine != null;

    void IAttackTargetingContext.StopAttackRoutine()
    {
        StopCoroutine(
            attackRoutine);
    }

    void IAttackTargetingContext.StartAttackRoutine()
    {
        attackRoutine =
            StartCoroutine(
                resolver.Run());
    }
}
