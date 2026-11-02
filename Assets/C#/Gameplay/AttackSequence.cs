using Mirror;
using System.Collections;
using UnityEngine;

public class AttackSequence : NetworkBehaviour, IAttackResolverContext, IAttackVortexContext
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

        if (cardIdentity == null ||
            sender == null)
        {
            GameLog.Write(
                "AttackSequence",
                "CmdStartSequence rejected: cardIdentity or sender is null");

            return;
        }

        if (!cardIdentity.TryGetComponent(
                out Card card))
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdStartSequence rejected: no Card component on netId={cardIdentity.netId}");

            return;
        }

        if (!card.IsAvailable ||
            card.IsInRoulette)
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdStartSequence rejected: card unavailable card={(card.Data != null ? card.Data.name : "NULL")} isAvailable={card.IsAvailable} isInRoulette={card.IsInRoulette}");

            return;
        }

        Player cardOwner =
            AttackValidator.FindPlayerByConnection(sender);

        if (cardOwner == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdStartSequence rejected: no player for connection {sender.connectionId}");

            return;
        }

        if (!AttackValidator.IsCardOwnedByPlayer(
                card,
                cardOwner))
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdStartSequence rejected: card not owned by player card={(card.Data != null ? card.Data.name : "NULL")} player={cardOwner.netId}");

            return;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
        {
            GameLog.Write(
                "AttackSequence",
                "CmdStartSequence rejected: GameMaster.Instance is null");

            return;
        }

        // -----------------------------------------------------
        // DEFENSE CARD
        // -----------------------------------------------------

        if (card.Data != null &&
            card.Data.CardClass == Card.CardClass.Defence)
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdStartSequence defense card card={card.Data.name} owner={cardOwner.netId}");

            if (!state.IsActive ||
                state.Attacker == null ||
                state.Target == null)
            {
                GameLog.Write(
                    "AttackSequence",
                    $"CmdStartSequence defense rejected: sequence not ready isActive={state.IsActive} attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")} target={(state.Target != null ? state.Target.name : "NULL")}");

                return;
            }

            Player targetPlayer =
                state.Target.GetComponentInParent<Player>();

            if (targetPlayer == null ||
                targetPlayer.netIdentity.connectionToClient != sender)
            {
                GameLog.Write(
                    "AttackSequence",
                    $"CmdStartSequence defense rejected: sender is not the target player targetPlayer={(targetPlayer != null ? targetPlayer.netId.ToString() : "NULL")} senderConn={sender.connectionId}");

                return;
            }

            if (!gameMaster.SelectDefense(
                    card,
                    targetPlayer))
            {
                GameLog.Write(
                    "AttackSequence",
                    $"CmdStartSequence defense rejected: GameMaster.SelectDefense returned false card={card.Data.name} targetPlayer={targetPlayer.netId}");

                return;
            }

            GameLog.Write(
                "AttackSequence",
                $"CmdStartSequence defense accepted card={card.Data.name} targetPlayer={targetPlayer.netId}");

            StartDefenseSequence(
                card,
                targetPlayer);

            return;
        }

        // -----------------------------------------------------
        // ATTACK CARD
        // -----------------------------------------------------

        if (!gameMaster.SelectAttack(
                card,
                cardOwner))
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdStartSequence attack rejected: GameMaster.SelectAttack returned false card={(card.Data != null ? card.Data.name : "NULL")} owner={cardOwner.netId}");

            return;
        }

        GameLog.Write(
            "AttackSequence",
            $"CmdStartSequence attack accepted card={(card.Data != null ? card.Data.name : "NULL")} owner={cardOwner.netId}");

        StartSequence(
            card,
            cardOwner);
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

        if (targetIdentity == null ||
            sender == null)
        {
            GameLog.Write(
                "AttackSequence",
                "CmdSetTarget rejected: targetIdentity or sender is null");

            return;
        }

        if (!state.IsActive ||
            state.Attacker == null ||
            state.Card == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdSetTarget rejected: sequence not ready isActive={state.IsActive} attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")} selectedCard={(state.Card != null && state.Card.Data != null ? state.Card.Data.name : "NULL")}");

            return;
        }

        if (!targetIdentity.TryGetComponent(
                out PlayerInteractable playerTarget))
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdSetTarget rejected: no PlayerInteractable on netId={targetIdentity.netId}");

            return;
        }

        Player targetPlayer =
            playerTarget.GetComponentInParent<Player>();

        if (targetPlayer == null)
        {
            GameLog.Write(
                "AttackSequence",
                "CmdSetTarget rejected: no Player in parents of target");

            return;
        }

        if (targetPlayer == state.Attacker)
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdSetTarget rejected: target is the attacker player={state.Attacker.netId}");

            return;
        }

        if (state.Attacker.netIdentity.connectionToClient != sender)
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdSetTarget rejected: attacker mismatch attacker={state.Attacker.netId} senderConn={sender.connectionId}");

            return;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
        {
            GameLog.Write(
                "AttackSequence",
                "CmdSetTarget rejected: GameMaster.Instance is null");

            return;
        }

        GameLog.Write(
            "AttackSequence",
            $"CmdSetTarget accepted attacker={state.Attacker.netId} targetPlayer={targetPlayer.netId}");

        SetTarget(
            playerTarget);
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

        if (!state.IsActive ||
            state.Attacker == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdCancelSequence rejected: sequence not ready isActive={state.IsActive} attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")}");

            return;
        }

        if (state.Attacker.netIdentity.connectionToClient != sender)
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdCancelSequence rejected: attacker mismatch attacker={state.Attacker.netId} senderConn={sender.connectionId}");

            return;
        }

        GameLog.Write(
            "AttackSequence",
            $"CmdCancelSequence accepted attacker={state.Attacker.netId}");

        CancelSequence();
    }

    // =========================================================
    // SERVER
    // =========================================================

    [Server]
    private void StartSequence(
        Card card,
        Player attacker)
    {
        GameLog.Write(
            "AttackSequence",
            $"StartSequence card={(card != null && card.Data != null ? card.Data.name : "NULL")} attacker={(attacker != null ? attacker.netId.ToString() : "NULL")}");

        if (card == null ||
            attacker == null)
        {
            GameLog.Write(
                "AttackSequence",
                "StartSequence rejected: card or attacker is null");

            return;
        }

        if (!card.IsAvailable ||
            card.IsInRoulette)
        {
            GameLog.Write(
                "AttackSequence",
                $"StartSequence rejected: card unavailable isAvailable={card.IsAvailable} isInRoulette={card.IsInRoulette}");

            return;
        }

        if (!AttackValidator.IsCardOwnedByPlayer(
                card,
                attacker))
        {
            GameLog.Write(
                "AttackSequence",
                $"StartSequence rejected: card not owned by attacker attacker={attacker.netId}");

            return;
        }

        GameLog.Write(
            "AttackSequence",
            $"StartSequence cancelling previous sequence wasActive={state.IsActive}");

        CancelSequence();

        state.Begin(
            card,
            attacker);

        GameLog.Write(
            "AttackSequence",
            $"StartSequence state set isActive=true selectedCard={(card.Data != null ? card.Data.name : "NULL")} attacker={attacker.netId} target=NULL");

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette == null)
        {
            GameLog.Write(
                "AttackSequence",
                "StartSequence failed: RouletteRotator.Instance is null, cancelling");

            Debug.LogError(
                "AttackSequence: RouletteRotator.Instance not found.",
                this);

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
            "AttackSequence",
            $"StartSequence roulette started pattern=Direct attacker={attacker.netId}");

        GameLog.Write(
            "AttackSequence",
            $"StartSequence calling RpcStartAttackPreparation attacker={attacker.netId} actionId={(card.Data != null ? card.Data.ActionId : string.Empty)}");

        RpcStartAttackPreparation(
            attacker.netIdentity,
            card.Data != null
                ? card.Data.ActionId
                : string.Empty);

        StartTargetSelectionTimer();
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
        GameLog.Write(
            "AttackSequence",
            $"StartDefenseSequence defenseCard={(defenseCard != null && defenseCard.Data != null ? defenseCard.Data.name : "NULL")} defensePlayer={(defensePlayer != null ? defensePlayer.netId.ToString() : "NULL")}");

        if (defenseCard == null ||
            defensePlayer == null)
        {
            GameLog.Write(
                "AttackSequence",
                "StartDefenseSequence rejected: defenseCard or defensePlayer is null");

            return;
        }

        if (!defenseCard.IsAvailable ||
            defenseCard.IsInRoulette)
        {
            GameLog.Write(
                "AttackSequence",
                $"StartDefenseSequence rejected: card unavailable isAvailable={defenseCard.IsAvailable} isInRoulette={defenseCard.IsInRoulette}");

            return;
        }

        if (defenseCard.Data == null ||
            defenseCard.Data.CardClass !=
                Card.CardClass.Defence)
        {
            GameLog.Write(
                "AttackSequence",
                "StartDefenseSequence rejected: card is not a Defence card");

            return;
        }

        if (!AttackValidator.IsCardOwnedByPlayer(
                defenseCard,
                defensePlayer))
        {
            GameLog.Write(
                "AttackSequence",
                $"StartDefenseSequence rejected: card not owned by defensePlayer defensePlayer={defensePlayer.netId}");

            return;
        }

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette == null)
        {
            GameLog.Write(
                "AttackSequence",
                "StartDefenseSequence failed: RouletteRotator.Instance is null");

            Debug.LogError(
                "AttackSequence: RouletteRotator.Instance not found.",
                this);

            return;
        }

        StopTargetSelectionTimer();

        if (attackRoutine != null)
        {
            GameLog.Write(
                "AttackSequence",
                "StartDefenseSequence stopping attackRoutine (defense timer)");

            StopCoroutine(
                attackRoutine);

            attackRoutine = null;
        }

        state.Card =
            defenseCard;

        GameLog.Write(
            "AttackSequence",
            $"StartDefenseSequence selectedCard changed to defense card={defenseCard.Data.name}");

        defenseCard.SetInRoulette(true);

        roulette.StopRoulette();

        roulette.AttachDefenseCard(
            defenseCard.transform);

        GameLog.Write(
            "AttackSequence",
            $"StartDefenseSequence starting Vortex roulette toward attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")}");

        roulette.StartRoulette(
            state.Attacker.transform,
            RouletteRotator.Pattern.Vortex,
            () => OnDefenseVortexCompleted(defenseCard));

        if (state.Attacker != null)
        {
            GameLog.Write(
                "AttackSequence",
                $"StartDefenseSequence calling RpcTriggerAttackAnimation attacker={state.Attacker.netId}");

            RpcTriggerAttackAnimation(
                state.Attacker.netIdentity);
        }

        GameLog.Write(
            "AttackSequence",
            $"StartDefenseSequence calling RpcStartAttackPreparation defensePlayer={defensePlayer.netId} actionId={defenseCard.Data.ActionId}");

        RpcStartAttackPreparation(
            defensePlayer.netIdentity,
            defenseCard.Data.ActionId);
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
        GameLog.Write(
            "AttackSequence",
            $"CancelSequence isActive={state.IsActive} card={(state.Card != null && state.Card.Data != null ? state.Card.Data.name : "NULL")} attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")} target={(state.Target != null ? state.Target.name : "NULL")}");

        if (!state.IsActive)
        {
            GameLog.Write(
                "AttackSequence",
                "CancelSequence skipped: sequence not active");

            return;
        }

        StopTargetSelectionTimer();

        steal.Stop();

        if (attackRoutine != null)
        {
            GameLog.Write(
                "AttackSequence",
                "CancelSequence stopping attackRoutine");

            StopCoroutine(
                attackRoutine);

            attackRoutine = null;
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
                "AttackSequence",
                $"CancelSequence returning card to owner card={(previousCard.Data != null ? previousCard.Data.name : "NULL")}");

            previousCard.ReturnToOwner();
        }

        if (previousAttacker != null)
        {
            GameLog.Write(
                "AttackSequence",
                $"CancelSequence calling RpcCancelAttackAnimation attacker={previousAttacker.netId}");

            RpcCancelAttackAnimation(
                previousAttacker.netIdentity);
        }

        state.Reset();

        GameLog.Write(
            "AttackSequence",
            "CancelSequence state cleared isActive=false");

        if (GameMaster.Instance != null)
        {
            GameLog.Write(
                "AttackSequence",
                "CancelSequence calling GameMaster.EndTurn");

            GameMaster.Instance.EndTurn();
        }
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
        GameLog.Write(
            "AttackSequence",
            $"FinishSequence isActive={state.IsActive} card={(state.Card != null && state.Card.Data != null ? state.Card.Data.name : "NULL")} attacker={(state.Attacker != null ? state.Attacker.netId.ToString() : "NULL")}");

        StopTargetSelectionTimer();

        steal.Stop();

        RouletteCardReleaser.Release(
            state.Card);

        state.Reset();
        attackRoutine = null;

        GameLog.Write(
            "AttackSequence",
            "FinishSequence state cleared isActive=false");
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
}
