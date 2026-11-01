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
    private Coroutine stealRoutine;

    public bool IsActive =>
        isActive;

    public Card SelectedCard =>
        selectedCard;

    public PlayerInteractable Target =>
        target;

    public Player Attacker =>
        attacker;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
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
        if (!isServer)
            return;

        if (cardIdentity == null ||
            sender == null)
        {
            return;
        }

        if (!cardIdentity.TryGetComponent(
                out Card card))
        {
            return;
        }

        Player cardOwner =
            FindPlayerByConnection(sender);

        if (cardOwner == null)
            return;

        ServerPlayCard(
            card,
            cardOwner);
    }

    [Command(requiresAuthority = false)]
    public void CmdSetTarget(
        NetworkIdentity targetIdentity,
        NetworkConnectionToClient sender = null)
    {
        if (!isServer)
            return;

        if (targetIdentity == null ||
            sender == null)
        {
            return;
        }

        Player requester =
            FindPlayerByConnection(sender);

        if (requester == null)
            return;

        if (!targetIdentity.TryGetComponent(
                out PlayerInteractable playerTarget))
        {
            return;
        }

        ServerSetTarget(
            requester,
            playerTarget);
    }

    [Command(requiresAuthority = false)]
    public void CmdCancelSequence(
        NetworkConnectionToClient sender = null)
    {
        if (!isServer ||
            sender == null)
        {
            return;
        }

        ServerCancel(
            FindPlayerByConnection(sender));
    }

    // =========================================================
    // SERVER ENTRY POINTS
    // (used by the Cmd* above, by bots and by the test scene)
    // =========================================================

    private static bool Reject(string reason)
    {
        GameLog.Warn(LogCat.Attack, "REJECTED: " + reason);
        return false;
    }

    /// <summary>
    /// 'cardOwner' plays 'card': an attack card starts a sequence,
    /// a defence card answers the running attack.
    /// </summary>
    [Server]
    public bool ServerPlayCard(
        Card card,
        Player cardOwner)
    {
        if (card == null ||
            cardOwner == null)
        {
            return Reject(
                "PlayCard: card or player is null");
        }

        if (!card.IsAvailable ||
            card.IsInRoulette)
        {
            return Reject(
                $"PlayCard: {GameLog.C(card)} is not available " +
                $"(available={card.IsAvailable}, " +
                $"inRoulette={card.IsInRoulette}, " +
                $"reason={card.Reason})");
        }

        if (!IsCardOwnedByPlayer(
                card,
                cardOwner))
        {
            return Reject(
                $"PlayCard: {GameLog.C(card)} is not owned by " +
                GameLog.P(cardOwner));
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
            return Reject("PlayCard: no GameMaster");

        // -----------------------------------------------------
        // DEFENSE CARD
        // -----------------------------------------------------

        if (card.Data != null &&
            card.Data.CardClass == Card.CardClass.Defence)
        {
            if (!isActive ||
                attacker == null ||
                target == null)
            {
                return Reject(
                    $"PlayCard: defence {GameLog.C(card)} but no " +
                    "attack is waiting for a defence");
            }

            Player targetPlayer =
                target.GetComponentInParent<Player>();

            if (targetPlayer == null ||
                targetPlayer != cardOwner)
            {
                return Reject(
                    $"PlayCard: defence by {GameLog.P(cardOwner)} " +
                    "who is not the target");
            }

            if (!gameMaster.SelectDefense(
                    card,
                    targetPlayer))
            {
                return Reject(
                    $"PlayCard: GameMaster refused defence " +
                    $"(phase={gameMaster.CurrentPhase})");
            }

            StartDefenseSequence(
                card,
                targetPlayer);

            return true;
        }

        // -----------------------------------------------------
        // ATTACK CARD
        // -----------------------------------------------------

        if (!gameMaster.SelectAttack(
                card,
                cardOwner))
        {
            return Reject(
                $"PlayCard: GameMaster refused attack " +
                $"{GameLog.C(card)} by {GameLog.P(cardOwner)} " +
                $"(phase={gameMaster.CurrentPhase}, " +
                $"current={gameMaster.CurrentPlayerIndex})");
        }

        StartSequence(
            card,
            cardOwner);

        return isActive;
    }

    /// <summary>The attacker picks 'playerTarget'.</summary>
    [Server]
    public bool ServerSetTarget(
        Player requester,
        PlayerInteractable playerTarget)
    {
        if (playerTarget == null ||
            requester == null)
        {
            return Reject(
                "SetTarget: target or requester is null");
        }

        if (!isActive ||
            attacker == null ||
            selectedCard == null)
        {
            return Reject(
                "SetTarget: no attack sequence is active");
        }

        Player targetPlayer =
            playerTarget.GetComponentInParent<Player>();

        if (targetPlayer == null)
            return Reject("SetTarget: target has no Player");

        if (targetPlayer == attacker)
        {
            return Reject(
                "SetTarget: attacker targeted himself");
        }

        if (attacker != requester)
        {
            return Reject(
                $"SetTarget: {GameLog.P(requester)} is not the " +
                $"attacker ({GameLog.P(attacker)})");
        }

        if (GameMaster.Instance == null)
            return Reject("SetTarget: no GameMaster");

        SetTarget(
            playerTarget);

        return target == playerTarget;
    }

    /// <summary>The attacker cancels the running sequence.</summary>
    [Server]
    public bool ServerCancel(
        Player requester)
    {
        if (!isActive ||
            attacker == null)
        {
            return Reject(
                "Cancel: no attack sequence is active");
        }

        if (requester == null ||
            attacker != requester)
        {
            return Reject(
                $"Cancel: {GameLog.P(requester)} is not the " +
                $"attacker ({GameLog.P(attacker)})");
        }

        CancelSequence();

        return true;
    }

    /// <summary>Test only: drop everything without side effects.</summary>
    [Server]
    public void ServerForceReset()
    {
        GameLog.Event(
            LogCat.Attack,
            "FORCE RESET AttackSequence");

        StopTargetSelectionTimer();

        if (stealRoutine != null)
        {
            StopCoroutine(
                stealRoutine);

            stealRoutine = null;
        }

        if (attackRoutine != null)
        {
            StopCoroutine(
                attackRoutine);

            attackRoutine = null;
        }

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

        if (attacker != null)
        {
            RpcCancelAttackAnimation(
                attacker.netIdentity,
                string.Empty);
        }

        selectedCard = null;
        attacker = null;
        target = null;

        isActive = false;
    }

    /// <summary>Test only: shorten / lengthen the windows.</summary>
    public void ConfigureTimers(
        float selection,
        float defense,
        float steal)
    {
        targetSelectionTime = Mathf.Max(0.5f, selection);
        targetActionTime = Mathf.Max(0.5f, defense);
        stealActionTime = Mathf.Max(0.5f, steal);

        GameLog.Event(
            LogCat.Test,
            $"Timers: select={targetSelectionTime:0.0}s " +
            $"defense={targetActionTime:0.0}s " +
            $"steal={stealActionTime:0.0}s");
    }

    public float TargetSelectionTime =>
        targetSelectionTime;

    public float DefenseWindowTime =>
        targetActionTime;

    public float StealWindowTime =>
        stealActionTime;

    // =========================================================
    // SERVER
    // =========================================================

    [Server]
    private void StartSequence(
        Card card,
        Player attacker)
    {
        if (card == null ||
            attacker == null)
        {
            return;
        }

        if (!card.IsAvailable ||
            card.IsInRoulette)
        {
            return;
        }

        if (!IsCardOwnedByPlayer(
                card,
                attacker))
        {
            return;
        }

        CancelSequence();

        selectedCard = card;
        this.attacker = attacker;
        target = null;

        isActive = true;

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette == null)
        {
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

        RpcStartAttackPreparation(
            attacker.netIdentity,
            card.Data != null
                ? card.Data.ActionId
                : string.Empty);

        StartTargetSelectionTimer();
        RpcPlaySequenceAudio(GetCardAudioKey(card), 0, attacker != null ? attacker.netIdentity : null);
    }

    // =========================================================
    // TARGET SELECTION TIMER
    // =========================================================

    [Server]
    private void StartTargetSelectionTimer()
    {
        StopTargetSelectionTimer();

        targetSelectionRoutine =
            StartCoroutine(
                TargetSelectionRoutine());
    }

    [Server]
    private void StopTargetSelectionTimer()
    {
        if (targetSelectionRoutine != null)
        {
            StopCoroutine(
                targetSelectionRoutine);

            targetSelectionRoutine = null;
        }
    }

    [Server]
    private IEnumerator TargetSelectionRoutine()
    {
        float remainingTime =
            targetSelectionTime;

        while (remainingTime > 0f)
        {
            if (!isActive ||
                selectedCard == null ||
                attacker == null)
            {
                targetSelectionRoutine = null;
                yield break;
            }

            if (target != null)
            {
                targetSelectionRoutine = null;
                yield break;
            }

            remainingTime -=
                Time.deltaTime;

            yield return null;
        }

        targetSelectionRoutine = null;

        if (isActive &&
            target == null)
        {
            CancelSequence();
        }
    }

    // =========================================================
    // DEFENSE SEQUENCE
    // =========================================================

    [Server]
    private void StartDefenseSequence(
        Card defenseCard,
        Player defensePlayer)
    {
        if (defenseCard == null ||
            defensePlayer == null)
        {
            return;
        }

        if (!defenseCard.IsAvailable ||
            defenseCard.IsInRoulette)
        {
            return;
        }

        if (defenseCard.Data == null ||
            defenseCard.Data.CardClass !=
                Card.CardClass.Defence)
        {
            return;
        }

        if (!IsCardOwnedByPlayer(
                defenseCard,
                defensePlayer))
        {
            return;
        }

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette == null)
        {
            Debug.LogError(
                "AttackSequence: RouletteRotator.Instance not found.",
                this);

            return;
        }

        StopTargetSelectionTimer();

        if (attackRoutine != null)
        {
            StopCoroutine(
                attackRoutine);

            attackRoutine = null;
        }

        string attackCardKey = GetCardAudioKey(selectedCard);
        selectedCard = defenseCard;

        defenseCard.SetInRoulette(true);

        roulette.StopRoulette();

        roulette.AttachDefenseCard(
            defenseCard.transform);

        roulette.StartRoulette(
            attacker.transform,
            RouletteRotator.Pattern.Vortex,
            () => OnDefenseVortexCompleted(defenseCard));
        RpcPlaySequenceAudio(attackCardKey, 2, attacker != null ? attacker.netIdentity : null);
        GameMaster.Instance?.TryTriggerGoodReaction(defensePlayer, 0.5f);
        GameMaster.Instance?.TryTriggerOtherPlayerScoff(attacker, 0.55f);
        GameMaster.Instance?.TryTriggerBadReaction(attacker, 0.65f);

        if (attacker != null)
        {
            RpcTriggerAttackAnimation(
                attacker.netIdentity,
                attackCardKey);
        }

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
        if (!isActive)
            return;

        if (selectedCard == null ||
            attacker == null)
        {
            return;
        }

        if (target == null)
            return;

        if (this.target != null)
            return;

        Player targetPlayer =
            target.GetComponentInParent<Player>();

        if (targetPlayer == null ||
            targetPlayer == attacker)
        {
            return;
        }

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette == null ||
            !roulette.IsRunning)
        {
            return;
        }

        if (selectedCard.Data != null &&
            selectedCard.Data.CardClass ==
                Card.CardClass.Defence)
        {
            return;
        }

        this.target = target;

        StopTargetSelectionTimer();

        roulette.SetTarget(
            target.EnemyCardMarker,
            OnRouletteReachedTarget);
    }

    // =========================================================
    // ATTACK ROULETTE CALLBACK
    // =========================================================

    [Server]
    private void OnRouletteReachedTarget()
    {
        if (!isActive ||
            selectedCard == null ||
            attacker == null ||
            target == null)
        {
            return;
        }

        Player victim =
            target.GetComponentInParent<Player>();

        if (victim == null)
        {
            CancelSequence();
            return;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
        {
            CancelSequence();
            return;
        }

        // The target becomes the actual game target only after
        // the roulette has reached and stopped at the target.
        if (!gameMaster.SelectTarget(
                victim))
        {
            CancelSequence();
            return;
        }

        if (attackRoutine != null)
        {
            StopCoroutine(
                attackRoutine);
        }

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
        Player victim =
            target != null
                ? target.GetComponentInParent<Player>()
                : null;

        if (victim == null)
        {
            CancelSequence();
            yield break;
        }

        NetworkConnectionToClient victimConnection =
            victim.netIdentity.connectionToClient;

        if (victimConnection != null)
        {
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
                if (victimConnection != null)
                {
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

        if (victimConnection != null)
        {
            TargetHideDefenseTimer(
                victimConnection);
        }

        if (!isActive ||
            selectedCard == null ||
            attacker == null ||
            target == null)
        {
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
            CancelSequence();
            yield break;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
        {
            CancelSequence();
            yield break;
        }

        if (!gameMaster.SkipDefense(
                targetPlayer))
        {
            CancelSequence();
            yield break;
        }

        RpcTriggerAttackAnimation(
            attackPlayer.netIdentity,
            GetCardAudioKey(card));

        attackTarget.Attack(
            card,
            attackPlayer);

        if (card.Data != null &&
            card.Data.CardClass !=
                Card.CardClass.AttackSteal &&
            card.Data.cardEffect != null)
        {
            card.Data.cardEffect.ApplyEffectToTarget(
                targetPlayer);
        }

        if (victimConnection != null)
        {
            TargetStartDamagePass(
                victimConnection,
                targetPlayer.netIdentity);
        }

        RpcPlaySequenceAudio(GetCardAudioKey(card), 1, attackPlayer != null ? attackPlayer.netIdentity : null);
        gameMaster.TryTriggerGoodReaction(attackPlayer, 0.5f);
        gameMaster.TryTriggerBadReaction(targetPlayer, 0.65f);
        StartAttackVortex(
            card,
            attackPlayer);

        attackRoutine = null;

        yield break;
    }

    [Server]
    private void StealCoins(
        Player attackPlayer,
        Player victim)
    {
        if (attackPlayer == null ||
            victim == null ||
            attackPlayer == victim ||
            stealCoinsAmount <= 0)
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
                this);

            return;
        }

        int amount =
            Mathf.Min(
                stealCoinsAmount,
                victimBank.Coins);

        if (amount <= 0)
            return;

        victimBank.TryStealCoinsTo(
            attackerBank,
            amount,
            stealCoinsRainDuration);
    }

    // =========================================================
    // ATTACK VORTEX
    // =========================================================

    [Server]
    private void StartAttackVortex(
        Card card,
        Player attackPlayer)
    {
        if (card == null ||
            attackPlayer == null)
        {
            FinishSequence();
            return;
        }

        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette == null)
        {
            RpcPlaySequenceAudio(GetCardAudioKey(card), 3, null);
        DestroyCard(
                card);

            if (GameMaster.Instance != null)
            {
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

        if (isStealCard)
        {
            Player victim =
                target != null
                    ? target.GetComponentInParent<Player>()
                    : null;

            StealCoins(
                attackPlayer,
                victim);
        }

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

        RpcPlaySequenceAudio(GetCardAudioKey(card), 3, null);
        DestroyCard(
            card);

        if (isStealCard)
        {
            if (GameMaster.Instance == null ||
                !GameMaster.Instance.BeginStealPhase())
            {
                FinishSequence();
                return;
            }

            StartStealPhase();
            return;
        }

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.EndTurn();
        }

        FinishSequence();
    }

    // =========================================================
    // STEAL PHASE
    // =========================================================

    [Server]
    private void StartStealPhase()
    {
        if (GameMaster.Instance == null ||
            GameMaster.Instance.CurrentPhase !=
                GameMaster.GamePhase.Steal)
        {
            FinishSequence();
            return;
        }

        if (stealRoutine != null)
        {
            StopCoroutine(
                stealRoutine);
        }

        stealRoutine =
            StartCoroutine(
                StealRoutine());
    }

    [Server]
    private IEnumerator StealRoutine()
    {
        float remainingTime =
            stealActionTime;

        while (remainingTime > 0f)
        {
            if (!isActive)
            {
                stealRoutine = null;
                yield break;
            }

            if (GameMaster.Instance == null ||
                GameMaster.Instance.CurrentPhase !=
                    GameMaster.GamePhase.Steal)
            {
                stealRoutine = null;
                FinishSequence();
                yield break;
            }

            remainingTime -=
                Time.deltaTime;

            yield return null;
        }

        stealRoutine = null;

        if (GameMaster.Instance != null &&
            GameMaster.Instance.CurrentPhase ==
                GameMaster.GamePhase.Steal)
        {
            GameMaster.Instance.EndTurn();
        }

        FinishSequence();
    }

    // =========================================================
    // DEFENSE VORTEX
    // =========================================================

    [Server]
    private void OnDefenseVortexCompleted(
        Card card)
    {
        if (card == null)
        {
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
            NetworkServer.Destroy(
                card.gameObject);
        }

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.EndTurn();
        }

        selectedCard = null;
        attacker = null;
        target = null;

        isActive = false;
        attackRoutine = null;
        targetSelectionRoutine = null;
    }

    [Server]
    private void FinishDefenseSequence()
    {
        RouletteRotator roulette =
            RouletteRotator.Instance;

        if (roulette != null)
        {
            roulette.StopRoulette();
        }

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.EndTurn();
        }

        selectedCard = null;
        attacker = null;
        target = null;

        isActive = false;
        attackRoutine = null;
        targetSelectionRoutine = null;
    }

    // =========================================================
    // DESTROY CARD
    // =========================================================

    [Server]
    private void DestroyCard(
        Card card)
    {
        if (card == null)
            return;

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
        if (attackerIdentity == null)
            return;

        Player player =
            attackerIdentity.GetComponent<Player>();

        if (player == null ||
            player.animator == null)
        {
            return;
        }

        player.animator.StartAttackAnimation(
            actionId);
    }

    [ClientRpc]
    private void RpcTriggerAttackAnimation(
        NetworkIdentity attackerIdentity,
        string cardAudioKey)
    {
        if (attackerIdentity == null)
            return;

        Player player =
            attackerIdentity.GetComponent<Player>();

        if (player == null ||
            player.animator == null)
        {
            return;
        }

        player.animator.TriggerAttack(cardAudioKey);
    }

    // =========================================================
    // TARGET RPC
    // =========================================================

    [TargetRpc]
    private void TargetShowDefenseTimer(
        NetworkConnectionToClient connection,
        float time)
    {
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
        GameAudioManager.Instance?.StopHeartbeat();
        HudController.Instance?.HideDefenseTimer();
    }

    [TargetRpc]
    private void TargetStartDamagePass(
        NetworkConnectionToClient connection,
        NetworkIdentity victimIdentity)
    {
        if (victimIdentity == null)
            return;

        Player victim =
            victimIdentity.GetComponent<Player>();

        if (victim == null)
            return;

        victim.StartDamagePass();
    }

    // =========================================================
    // CANCEL
    // =========================================================

    [Server]
    private void CancelSequence(
        [System.Runtime.CompilerServices.CallerMemberName] string caller = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
    {
        if (!isActive)
            return;

        GameLog.Event(
            LogCat.Attack,
            $"CANCEL SEQUENCE (called from {caller}:{line}): " +
            $"attacker={GameLog.P(attacker)}, " +
            $"card={GameLog.C(selectedCard)}, " +
            $"target={(target != null ? GameLog.P(target.GetComponentInParent<Player>()) : "none")}");

        StopTargetSelectionTimer();

        if (stealRoutine != null)
        {
            StopCoroutine(
                stealRoutine);

            stealRoutine = null;
        }

        if (attackRoutine != null)
        {
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
            previousCard.ReturnToOwner();
        }

        string cancelledCardKey = GetCardAudioKey(previousCard);

        if (previousAttacker != null)
        {
            RpcCancelAttackAnimation(
                previousAttacker.netIdentity,
                cancelledCardKey);
        }

        RpcPlaySequenceAudio(cancelledCardKey, 4, previousAttacker != null ? previousAttacker.netIdentity : null);
        GameMaster.Instance?.TryTriggerOtherPlayerScoff(previousAttacker, 0.5f);

        selectedCard = null;
        attacker = null;
        target = null;

        isActive = false;

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.EndTurn();
        }
    }

    [ClientRpc]
    private void RpcCancelAttackAnimation(
        NetworkIdentity attackerIdentity,
        string cardOrActionName)
    {
        if (attackerIdentity == null)
            return;

        Player player =
            attackerIdentity.GetComponent<Player>();

        if (player == null ||
            player.animator == null)
        {
            return;
        }

        player.animator.CancelAttackAnimation(
            cardOrActionName);
    }

    [ClientRpc]
    private void RpcPlaySequenceAudio(string cardName, int eventType, NetworkIdentity attackerIdentity)
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
                // card_destroy disabled (misc)
                break;
            case 4:
                GameAudioManager.Instance.SuppressHoverFor(0.4f);
                GameAudioManager.Instance.PlayWeaponCancel(cardName, attackerIdentity);
                break;
        }
    }

    // =========================================================
    // FINISH
    // =========================================================

    [Server]
    private void FinishSequence()
    {
        GameLog.Event(
            LogCat.Attack,
            "SEQUENCE FINISH");

        StopTargetSelectionTimer();

        if (stealRoutine != null)
        {
            StopCoroutine(
                stealRoutine);

            stealRoutine = null;
        }

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
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private static string GetCardAudioKey(Card card)
    {
        if (card == null || card.Data == null)
            return string.Empty;

        return $"{card.Data.ActionId} {card.Data.CardName} {card.Data.name}";
    }

    [Server]
    private bool IsCardOwnedByPlayer(
        Card card,
        Player player)
    {
        if (card == null ||
            player == null)
        {
            return false;
        }

        // Bots have no connection: compare the owner id directly.
        if (card.OwnerNetId == player.netId)
            return true;

        if (player.netIdentity != null && player.netIdentity.connectionToClient != null)
        {
            return card.IsOwnedByLocalConnection(
                player.netIdentity.connectionToClient);
        }

        return false;
    }

    [Server]
    private Player FindPlayerByConnection(
        NetworkConnectionToClient connection)
    {
        if (connection == null)
            return null;

        if (connection.identity == null)
            return null;

        return connection.identity.GetComponent<Player>();
    }
}