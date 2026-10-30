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
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        steal =
            new StealPhase(
                this,
                stealActionTime,
                stealCoinsAmount,
                stealCoinsRainDuration,
                () => isActive,
                FinishSequence);
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

        if (!card.IsAvailable ||
            card.IsInRoulette)
        {
            return;
        }

        Player cardOwner =
            FindPlayerByConnection(sender);

        if (cardOwner == null)
            return;

        if (!IsCardOwnedByPlayer(
                card,
                cardOwner))
        {
            return;
        }

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
            return;

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
                return;
            }

            Player targetPlayer =
                target.GetComponentInParent<Player>();

            if (targetPlayer == null ||
                targetPlayer.netIdentity.connectionToClient != sender)
            {
                return;
            }

            if (!gameMaster.SelectDefense(
                    card,
                    targetPlayer))
            {
                return;
            }

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
            return;
        }

        StartSequence(
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

        if (!isActive ||
            attacker == null ||
            selectedCard == null)
        {
            return;
        }

        if (!targetIdentity.TryGetComponent(
                out PlayerInteractable playerTarget))
        {
            return;
        }

        Player targetPlayer =
            playerTarget.GetComponentInParent<Player>();

        if (targetPlayer == null)
            return;

        if (targetPlayer == attacker)
            return;

        if (attacker.netIdentity.connectionToClient != sender)
            return;

        GameMaster gameMaster =
            GameMaster.Instance;

        if (gameMaster == null)
            return;

        SetTarget(
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

        if (!isActive ||
            attacker == null)
        {
            return;
        }

        if (attacker.netIdentity.connectionToClient != sender)
            return;

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

        selectedCard =
            defenseCard;

        defenseCard.SetInRoulette(true);

        roulette.StopRoulette();

        roulette.AttachDefenseCard(
            defenseCard.transform);

        roulette.StartRoulette(
            attacker.transform,
            RouletteRotator.Pattern.Vortex,
            () => OnDefenseVortexCompleted(defenseCard));

        if (attacker != null)
        {
            RpcTriggerAttackAnimation(
                attacker.netIdentity);
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
            attackPlayer.netIdentity);

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

        StartAttackVortex(
            card,
            attackPlayer);

        attackRoutine = null;

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

            steal.TakeCoins(
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

            steal.Begin();
            return;
        }

        if (GameMaster.Instance != null)
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
        NetworkIdentity attackerIdentity)
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
    private void CancelSequence()
    {
        if (!isActive)
            return;

        StopTargetSelectionTimer();

        steal.Stop();

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

        if (previousAttacker != null)
        {
            RpcCancelAttackAnimation(
                previousAttacker.netIdentity);
        }

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
        NetworkIdentity attackerIdentity)
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
            string.Empty);
    }

    // =========================================================
    // FINISH
    // =========================================================

    [Server]
    private void FinishSequence()
    {
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
    }

    // =========================================================
    // VALIDATION
    // =========================================================

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

        return card.IsOwnedByLocalConnection(
            player.netIdentity.connectionToClient);
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