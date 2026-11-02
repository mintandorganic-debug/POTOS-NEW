using Mirror;
using System.Collections;
using UnityEngine;

public class AttackSequence : NetworkBehaviour
{
    public static AttackSequence Instance { get; private set; }

    [Header("Timing")]
    [SerializeField] private float targetSelectionTime = 10f;
    [SerializeField] private float targetActionTime = 5f;
    [SerializeField] private float stealActionTime = 5f;

    [Header("Steal")]
    [SerializeField] private int stealCoinsAmount = 5;
    [SerializeField] private float stealCoinsRainDuration = 5f;

    private Card selectedCard;
    private Player attacker;
    private PlayerInteractable target;

    private bool isActive;

    private Coroutine attackRoutine;
    private Coroutine targetSelectionRoutine;

    private StealPhase steal;

    public bool IsActive =>
        isActive;

    public Card SelectedCard =>
        selectedCard;

    public PlayerInteractable Target =>
        target;

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
                () => isActive,
                FinishSequence);

        GameLog.Write(
            "AttackSequence",
            "Awake StealPhase created");
    }

    private void OnDestroy()
    {
        GameLog.Write(
            "AttackSequence",
            $"OnDestroy wasInstance={Instance == this} isActive={isActive}");

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
            $"CmdStartSequence cardNetId={(cardIdentity != null ? cardIdentity.netId.ToString() : "NULL")} senderConn={(sender != null ? sender.connectionId.ToString() : "NULL")} isServer={isServer} isActive={isActive}");

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
            FindPlayerByConnection(sender);

        if (cardOwner == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdStartSequence rejected: no player for connection {sender.connectionId}");

            return;
        }

        if (!IsCardOwnedByPlayer(
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

            if (!isActive ||
                attacker == null ||
                target == null)
            {
                GameLog.Write(
                    "AttackSequence",
                    $"CmdStartSequence defense rejected: sequence not ready isActive={isActive} attacker={(attacker != null ? attacker.netId.ToString() : "NULL")} target={(target != null ? target.name : "NULL")}");

                return;
            }

            Player targetPlayer =
                target.GetComponentInParent<Player>();

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
            $"CmdSetTarget targetNetId={(targetIdentity != null ? targetIdentity.netId.ToString() : "NULL")} senderConn={(sender != null ? sender.connectionId.ToString() : "NULL")} isServer={isServer} isActive={isActive}");

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

        if (!isActive ||
            attacker == null ||
            selectedCard == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdSetTarget rejected: sequence not ready isActive={isActive} attacker={(attacker != null ? attacker.netId.ToString() : "NULL")} selectedCard={(selectedCard != null && selectedCard.Data != null ? selectedCard.Data.name : "NULL")}");

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

        if (targetPlayer == attacker)
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdSetTarget rejected: target is the attacker player={attacker.netId}");

            return;
        }

        if (attacker.netIdentity.connectionToClient != sender)
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdSetTarget rejected: attacker mismatch attacker={attacker.netId} senderConn={sender.connectionId}");

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
            $"CmdSetTarget accepted attacker={attacker.netId} targetPlayer={targetPlayer.netId}");

        SetTarget(
            playerTarget);
    }

    [Command(requiresAuthority = false)]
    public void CmdCancelSequence(
        NetworkConnectionToClient sender = null)
    {
        GameLog.Write(
            "AttackSequence",
            $"CmdCancelSequence senderConn={(sender != null ? sender.connectionId.ToString() : "NULL")} isServer={isServer} isActive={isActive}");

        if (!isServer ||
            sender == null)
        {
            GameLog.Write(
                "AttackSequence",
                "CmdCancelSequence rejected: not server or sender is null");

            return;
        }

        if (!isActive ||
            attacker == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdCancelSequence rejected: sequence not ready isActive={isActive} attacker={(attacker != null ? attacker.netId.ToString() : "NULL")}");

            return;
        }

        if (attacker.netIdentity.connectionToClient != sender)
        {
            GameLog.Write(
                "AttackSequence",
                $"CmdCancelSequence rejected: attacker mismatch attacker={attacker.netId} senderConn={sender.connectionId}");

            return;
        }

        GameLog.Write(
            "AttackSequence",
            $"CmdCancelSequence accepted attacker={attacker.netId}");

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

        if (!IsCardOwnedByPlayer(
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
            $"StartSequence cancelling previous sequence wasActive={isActive}");

        CancelSequence();

        selectedCard = card;
        this.attacker = attacker;
        target = null;

        isActive = true;

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
        GameLog.Write(
            "AttackSequence",
            $"StartTargetSelectionTimer time={targetSelectionTime} previousRunning={targetSelectionRoutine != null}");

        StopTargetSelectionTimer();

        targetSelectionRoutine =
            StartCoroutine(
                TargetSelectionRoutine());

        GameLog.Write(
            "AttackSequence",
            "StartTargetSelectionTimer timer started");
    }

    [Server]
    private void StopTargetSelectionTimer()
    {
        GameLog.Write(
            "AttackSequence",
            $"StopTargetSelectionTimer running={targetSelectionRoutine != null}");

        if (targetSelectionRoutine != null)
        {
            StopCoroutine(
                targetSelectionRoutine);

            targetSelectionRoutine = null;

            GameLog.Write(
                "AttackSequence",
                "StopTargetSelectionTimer timer stopped");
        }
    }

    [Server]
    private IEnumerator TargetSelectionRoutine()
    {
        GameLog.Write(
            "AttackSequence",
            $"TargetSelectionRoutine start time={targetSelectionTime}");

        float remainingTime =
            targetSelectionTime;

        while (remainingTime > 0f)
        {
            if (!isActive ||
                selectedCard == null ||
                attacker == null)
            {
                GameLog.Write(
                    "AttackSequence",
                    $"TargetSelectionRoutine end: sequence no longer valid isActive={isActive} remaining={remainingTime}");

                targetSelectionRoutine = null;
                yield break;
            }

            if (target != null)
            {
                GameLog.Write(
                    "AttackSequence",
                    $"TargetSelectionRoutine end: target selected target={target.name} remaining={remainingTime}");

                targetSelectionRoutine = null;
                yield break;
            }

            remainingTime -=
                Time.deltaTime;

            yield return null;
        }

        targetSelectionRoutine = null;

        GameLog.Write(
            "AttackSequence",
            $"TargetSelectionRoutine timed out isActive={isActive} targetSelected={target != null}");

        if (isActive &&
            target == null)
        {
            GameLog.Write(
                "AttackSequence",
                "TargetSelectionRoutine timeout: no target chosen, cancelling sequence");

            CancelSequence();
        }

        GameLog.Write(
            "AttackSequence",
            "TargetSelectionRoutine end");
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

        if (!IsCardOwnedByPlayer(
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

        selectedCard =
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
            $"StartDefenseSequence starting Vortex roulette toward attacker={(attacker != null ? attacker.netId.ToString() : "NULL")}");

        roulette.StartRoulette(
            attacker.transform,
            RouletteRotator.Pattern.Vortex,
            () => OnDefenseVortexCompleted(defenseCard));

        if (attacker != null)
        {
            GameLog.Write(
                "AttackSequence",
                $"StartDefenseSequence calling RpcTriggerAttackAnimation attacker={attacker.netId}");

            RpcTriggerAttackAnimation(
                attacker.netIdentity);
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
            $"SetTarget target={(target != null ? target.name : "NULL")} isActive={isActive} currentTarget={(this.target != null ? this.target.name : "NULL")}");

        if (!isActive)
        {
            GameLog.Write(
                "AttackSequence",
                "SetTarget rejected: sequence not active");

            return;
        }

        if (selectedCard == null ||
            attacker == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"SetTarget rejected: selectedCard or attacker is null selectedCard={(selectedCard != null && selectedCard.Data != null ? selectedCard.Data.name : "NULL")} attacker={(attacker != null ? attacker.netId.ToString() : "NULL")}");

            return;
        }

        if (target == null)
        {
            GameLog.Write(
                "AttackSequence",
                "SetTarget rejected: target is null");

            return;
        }

        if (this.target != null)
        {
            GameLog.Write(
                "AttackSequence",
                $"SetTarget rejected: target already set target={this.target.name}");

            return;
        }

        Player targetPlayer =
            target.GetComponentInParent<Player>();

        if (targetPlayer == null ||
            targetPlayer == attacker)
        {
            GameLog.Write(
                "AttackSequence",
                $"SetTarget rejected: invalid target player targetPlayer={(targetPlayer != null ? targetPlayer.netId.ToString() : "NULL")} attacker={attacker.netId}");

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

        if (selectedCard.Data != null &&
            selectedCard.Data.CardClass ==
                Card.CardClass.Defence)
        {
            GameLog.Write(
                "AttackSequence",
                $"SetTarget rejected: selected card is a Defence card card={selectedCard.Data.name}");

            return;
        }

        this.target = target;

        GameLog.Write(
            "AttackSequence",
            $"SetTarget target set target={target.name} targetPlayer={targetPlayer.netId} attacker={attacker.netId}");

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
            $"OnRouletteReachedTarget isActive={isActive} target={(target != null ? target.name : "NULL")} attacker={(attacker != null ? attacker.netId.ToString() : "NULL")}");

        if (!isActive ||
            selectedCard == null ||
            attacker == null ||
            target == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"OnRouletteReachedTarget ignored: sequence not valid isActive={isActive} selectedCard={(selectedCard != null && selectedCard.Data != null ? selectedCard.Data.name : "NULL")}");

            return;
        }

        Player victim =
            target.GetComponentInParent<Player>();

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
                AttackDelayRoutine());
    }

    // =========================================================
    // ATTACK TIMER
    // =========================================================

    [Server]
    private IEnumerator AttackDelayRoutine()
    {
        GameLog.Write(
            "AttackSequence",
            $"AttackDelayRoutine start targetActionTime={targetActionTime}");

        Player victim =
            target != null
                ? target.GetComponentInParent<Player>()
                : null;

        if (victim == null)
        {
            GameLog.Write(
                "AttackSequence",
                "AttackDelayRoutine failed: victim is null, cancelling");

            CancelSequence();
            yield break;
        }

        NetworkConnectionToClient victimConnection =
            victim.netIdentity.connectionToClient;

        if (victimConnection != null)
        {
            GameLog.Write(
                "AttackSequence",
                $"AttackDelayRoutine calling TargetShowDefenseTimer victim={victim.netId} time={targetActionTime}");

            TargetShowDefenseTimer(
                victimConnection,
                targetActionTime);
        }

        float remainingTime =
            targetActionTime;

        while (remainingTime > 0f)
        {
            if (!isActive ||
                selectedCard == null ||
                attacker == null ||
                target == null)
            {
                GameLog.Write(
                    "AttackSequence",
                    $"AttackDelayRoutine interrupted: sequence changed during defense window isActive={isActive} remaining={remainingTime}");

                if (victimConnection != null)
                {
                    GameLog.Write(
                        "AttackSequence",
                        $"AttackDelayRoutine calling TargetHideDefenseTimer victim={(victim != null ? victim.netId.ToString() : "NULL")}");

                    TargetHideDefenseTimer(
                        victimConnection);
                }

                yield break;
            }

            remainingTime -=
                Time.deltaTime;

            if (victimConnection != null)
            {
                TargetSetDefenseTimer(
                    victimConnection,
                    remainingTime);
            }

            yield return null;
        }

        GameLog.Write(
            "AttackSequence",
            "AttackDelayRoutine defense window elapsed");

        if (victimConnection != null)
        {
            GameLog.Write(
                "AttackSequence",
                $"AttackDelayRoutine calling TargetHideDefenseTimer victim={(victim != null ? victim.netId.ToString() : "NULL")}");

            TargetHideDefenseTimer(
                victimConnection);
        }

        if (!isActive ||
            selectedCard == null ||
            attacker == null ||
            target == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"AttackDelayRoutine end: sequence no longer valid after timer isActive={isActive}");

            yield break;
        }

        Card card =
            selectedCard;

        Player attackPlayer =
            attacker;

        PlayerInteractable attackTarget =
            target;

        Player targetPlayer =
            attackTarget.GetComponentInParent<Player>();

        if (targetPlayer == null)
        {
            GameLog.Write(
                "AttackSequence",
                "AttackDelayRoutine failed: targetPlayer is null, cancelling");

            CancelSequence();
            yield break;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
        {
            GameLog.Write(
                "AttackSequence",
                "AttackDelayRoutine failed: GameMaster.Instance is null, cancelling");

            CancelSequence();
            yield break;
        }

        if (!gameMaster.SkipDefense(
                targetPlayer))
        {
            GameLog.Write(
                "AttackSequence",
                $"AttackDelayRoutine failed: GameMaster.SkipDefense returned false targetPlayer={targetPlayer.netId}, cancelling");

            CancelSequence();
            yield break;
        }

        GameLog.Write(
            "AttackSequence",
            $"AttackDelayRoutine defense skipped, applying attack card={(card.Data != null ? card.Data.name : "NULL")} attacker={attackPlayer.netId} targetPlayer={targetPlayer.netId}");

        GameLog.Write(
            "AttackSequence",
            $"AttackDelayRoutine calling RpcTriggerAttackAnimation attacker={attackPlayer.netId}");

        RpcTriggerAttackAnimation(
            attackPlayer.netIdentity);

        attackTarget.Attack(
            card,
            attackPlayer);

        if (card.Data != null &&
            card.Data.CardClass !=
                Card.CardClass.AttackSteal &&
            card.Data.cardEffect != null)
        {
            GameLog.Write(
                "AttackSequence",
                $"AttackDelayRoutine applying card effect to target card={card.Data.name} targetPlayer={targetPlayer.netId}");

            card.Data.cardEffect.ApplyEffectToTarget(
                targetPlayer);
        }

        if (victimConnection != null)
        {
            GameLog.Write(
                "AttackSequence",
                $"AttackDelayRoutine calling TargetStartDamagePass victim={targetPlayer.netId}");

            TargetStartDamagePass(
                victimConnection,
                targetPlayer.netIdentity);
        }

        StartAttackVortex(
            card,
            attackPlayer);

        attackRoutine = null;

        GameLog.Write(
            "AttackSequence",
            "AttackDelayRoutine end: attack applied");

        yield break;
    }

    // =========================================================
    // ATTACK VORTEX
    // =========================================================

    [Server]
    private void StartAttackVortex(
        Card card,
        Player attackPlayer)
    {
        GameLog.Write(
            "AttackSequence",
            $"StartAttackVortex card={(card != null && card.Data != null ? card.Data.name : "NULL")} attackPlayer={(attackPlayer != null ? attackPlayer.netId.ToString() : "NULL")}");

        if (card == null ||
            attackPlayer == null)
        {
            GameLog.Write(
                "AttackSequence",
                "StartAttackVortex failed: card or attackPlayer is null, finishing sequence");

            FinishSequence();
            return;
        }

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette == null)
        {
            GameLog.Write(
                "AttackSequence",
                "StartAttackVortex failed: RouletteRotator.Instance is null, destroying card and ending turn");

            DestroyCard(
                card);

            if (GameMaster.Instance != null)
            {
                GameLog.Write(
                    "AttackSequence",
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
            "AttackSequence",
            $"StartAttackVortex isStealCard={isStealCard}");

        if (isStealCard)
        {
            Player victim =
                target != null
                    ? target.GetComponentInParent<Player>()
                    : null;

            GameLog.Write(
                "AttackSequence",
                $"StartAttackVortex steal.TakeCoins attacker={attackPlayer.netId} victim={(victim != null ? victim.netId.ToString() : "NULL")}");

            steal.TakeCoins(
                attackPlayer,
                victim);
        }

        GameLog.Write(
            "AttackSequence",
            $"StartAttackVortex starting Vortex roulette attacker={attackPlayer.netId}");

        roulette.StartRoulette(
            attackPlayer.transform,
            RouletteRotator.Pattern.Vortex,
            () => OnAttackVortexCompleted(card));
    }

    [Server]
    private void OnAttackVortexCompleted(
        Card card)
    {
        bool isStealCard =
            card != null &&
            card.Data != null &&
            card.Data.CardClass ==
                Card.CardClass.AttackSteal;

        GameLog.Write(
            "AttackSequence",
            $"OnAttackVortexCompleted card={(card != null && card.Data != null ? card.Data.name : "NULL")} isStealCard={isStealCard}");

        DestroyCard(
            card);

        if (isStealCard)
        {
            GameLog.Write(
                "AttackSequence",
                "OnAttackVortexCompleted steal card, requesting GameMaster.BeginStealPhase");

            if (GameMaster.Instance == null ||
                !GameMaster.Instance.BeginStealPhase())
            {
                GameLog.Write(
                    "AttackSequence",
                    $"OnAttackVortexCompleted steal phase not started (GameMaster exists={GameMaster.Instance != null}), finishing sequence");

                FinishSequence();
                return;
            }

            GameLog.Write(
                "AttackSequence",
                "OnAttackVortexCompleted steal phase started, calling steal.Begin");

            steal.Begin();
            return;
        }

        if (GameMaster.Instance != null)
        {
            GameLog.Write(
                "AttackSequence",
                "OnAttackVortexCompleted calling GameMaster.EndTurn");

            GameMaster.Instance.EndTurn();
        }

        GameLog.Write(
            "AttackSequence",
            "OnAttackVortexCompleted finishing sequence");

        FinishSequence();
    }

    // =========================================================
    // DEFENSE VORTEX
    // =========================================================

    [Server]
    private void OnDefenseVortexCompleted(
        Card card)
    {
        GameLog.Write(
            "AttackSequence",
            $"OnDefenseVortexCompleted card={(card != null && card.Data != null ? card.Data.name : "NULL")}");

        if (card == null)
        {
            GameLog.Write(
                "AttackSequence",
                "OnDefenseVortexCompleted card is null, finishing defense sequence");

            FinishDefenseSequence();
            return;
        }

        card.SetInRoulette(false);

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette != null)
        {
            roulette.DetachCard(
                card.transform);
        }

        if (card.gameObject != null)
        {
            GameLog.Write(
                "AttackSequence",
                $"OnDefenseVortexCompleted destroying defense card={(card.Data != null ? card.Data.name : "NULL")}");

            NetworkServer.Destroy(
                card.gameObject);
        }

        if (GameMaster.Instance != null)
        {
            GameLog.Write(
                "AttackSequence",
                "OnDefenseVortexCompleted calling GameMaster.EndTurn");

            GameMaster.Instance.EndTurn();
        }

        selectedCard = null;
        attacker = null;
        target = null;

        isActive = false;
        attackRoutine = null;
        targetSelectionRoutine = null;

        GameLog.Write(
            "AttackSequence",
            "OnDefenseVortexCompleted state cleared isActive=false");
    }

    [Server]
    private void FinishDefenseSequence()
    {
        GameLog.Write(
            "AttackSequence",
            $"FinishDefenseSequence isActive={isActive}");

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette != null)
        {
            roulette.StopRoulette();
        }

        if (GameMaster.Instance != null)
        {
            GameLog.Write(
                "AttackSequence",
                "FinishDefenseSequence calling GameMaster.EndTurn");

            GameMaster.Instance.EndTurn();
        }

        selectedCard = null;
        attacker = null;
        target = null;

        isActive = false;
        attackRoutine = null;
        targetSelectionRoutine = null;

        GameLog.Write(
            "AttackSequence",
            "FinishDefenseSequence state cleared isActive=false");
    }

    // =========================================================
    // DESTROY CARD
    // =========================================================

    [Server]
    private void DestroyCard(
        Card card)
    {
        GameLog.Write(
            "AttackSequence",
            $"DestroyCard card={(card != null && card.Data != null ? card.Data.name : "NULL")}");

        if (card == null)
        {
            GameLog.Write(
                "AttackSequence",
                "DestroyCard skipped: card is null");

            return;
        }

        card.SetInRoulette(false);

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette != null)
        {
            roulette.DetachCard(
                card.transform);
        }

        if (card.gameObject != null)
        {
            GameLog.Write(
                "AttackSequence",
                $"DestroyCard NetworkServer.Destroy card={(card.Data != null ? card.Data.name : "NULL")}");

            NetworkServer.Destroy(
                card.gameObject);
        }
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
            $"CancelSequence isActive={isActive} card={(selectedCard != null && selectedCard.Data != null ? selectedCard.Data.name : "NULL")} attacker={(attacker != null ? attacker.netId.ToString() : "NULL")} target={(target != null ? target.name : "NULL")}");

        if (!isActive)
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
            attacker;

        Card previousCard =
            selectedCard;

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (previousCard != null)
        {
            previousCard.SetInRoulette(false);
        }

        if (roulette != null)
        {
            roulette.StopRoulette();

            if (previousCard != null)
            {
                roulette.DetachCard(
                    previousCard.transform);
            }
        }

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

        selectedCard = null;
        attacker = null;
        target = null;

        isActive = false;

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
            $"FinishSequence isActive={isActive} card={(selectedCard != null && selectedCard.Data != null ? selectedCard.Data.name : "NULL")} attacker={(attacker != null ? attacker.netId.ToString() : "NULL")}");

        StopTargetSelectionTimer();

        steal.Stop();

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (selectedCard != null)
        {
            selectedCard.SetInRoulette(false);
        }

        if (roulette != null)
        {
            roulette.StopRoulette();

            if (selectedCard != null)
            {
                roulette.DetachCard(
                    selectedCard.transform);
            }
        }

        selectedCard = null;
        attacker = null;
        target = null;

        isActive = false;
        attackRoutine = null;

        GameLog.Write(
            "AttackSequence",
            "FinishSequence state cleared isActive=false");
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    [Server]
    private bool IsCardOwnedByPlayer(
        Card card,
        Player player)
    {
        GameLog.Write(
            "AttackSequence",
            $"IsCardOwnedByPlayer card={(card != null && card.Data != null ? card.Data.name : "NULL")} player={(player != null ? player.netId.ToString() : "NULL")}");

        if (card == null ||
            player == null)
        {
            GameLog.Write(
                "AttackSequence",
                "IsCardOwnedByPlayer false: card or player is null");

            return false;
        }

        return card.IsOwnedByLocalConnection(
            player.netIdentity.connectionToClient);
    }

    [Server]
    private Player FindPlayerByConnection(
        NetworkConnectionToClient connection)
    {
        GameLog.Write(
            "AttackSequence",
            $"FindPlayerByConnection connection={(connection != null ? connection.connectionId.ToString() : "NULL")}");

        if (connection == null)
        {
            GameLog.Write(
                "AttackSequence",
                "FindPlayerByConnection null: connection is null");

            return null;
        }

        if (connection.identity == null)
        {
            GameLog.Write(
                "AttackSequence",
                $"FindPlayerByConnection null: connection {connection.connectionId} has no identity");

            return null;
        }

        return connection.identity.GetComponent<Player>();
    }
}
