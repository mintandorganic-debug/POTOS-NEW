using System.Collections;
using Mirror;
using UnityEngine;

public class AttackSequence : NetworkBehaviour
{
    public static AttackSequence Instance { get; private set; }

    [Header("Timing")]
    [SerializeField] private float targetSelectionTime = 10f;
    [SerializeField] private float targetActionTime = 5f;
    [SerializeField] private float stealActionTime = 5f;

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

            StartDefenseSequence(
                card,
                targetPlayer);

            return;
        }

        // -----------------------------------------------------
        // NORMAL ATTACK CARD
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // DEFENSE CARD
        //
        // Safety fallback. Normally handled by
        // StartDefenseSequence().
        // -----------------------------------------------------

        if (card.Data != null &&
            card.Data.CardClass == Card.CardClass.Defence)
        {
            card.SetInRoulette(true);

            roulette.AttachDefenseCard(
                card.transform);

            roulette.StartRoulette(
                attacker.transform,
                RouletteRotator.Pattern.Vortex,
                () => OnDefenseVortexCompleted(card));

            if (GameMaster.Instance != null)
            {
                GameMaster.Instance.SetGamePhase(
                    GameMaster.GamePhase.Wait);
            }

            RpcStartAttackPreparation(
                attacker.netIdentity,
                card.Data.ActionId);

            return;
        }

        // -----------------------------------------------------
        // NORMAL ATTACK CARD
        // -----------------------------------------------------

        card.SetInRoulette(true);

        roulette.AttachCard(
            card.transform);

        roulette.StartRoulette(
            attacker.transform,
            RouletteRotator.Pattern.Direct);

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.SetGamePhase(
                GameMaster.GamePhase.SelectTarget);
        }

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
            defenseCard.Data.CardClass != Card.CardClass.Defence)
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

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.SetGamePhase(
                GameMaster.GamePhase.Wait);
        }

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
            selectedCard.Data.CardClass == Card.CardClass.Defence)
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

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.SetGamePhase(
                GameMaster.GamePhase.Defense);
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

            if (selectedCard.Data != null &&
                selectedCard.Data.CardClass == Card.CardClass.Defence)
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

        RpcTriggerAttackAnimation(
            attackPlayer.netIdentity);

        attackTarget.Attack(
            card,
            attackPlayer);

        if (card.Data != null &&
            card.Data.cardEffect != null)
        {
            card.Data.cardEffect.ApplyEffectToTarget(
                targetPlayer);
        }

        card.OnActionPerformed();

        StartAttackVortex(
            card,
            attackPlayer);

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

            return;
        }

        card.SetInRoulette(true);

        roulette.StopRoulette();

        roulette.AttachCard(
            card.transform);

        roulette.StartRoulette(
            attackPlayer.transform,
            RouletteRotator.Pattern.Vortex,
            () => OnAttackVortexCompleted(card));

        if (card.Data == null ||
            card.Data.CardClass != Card.CardClass.AttackSteal)
        {
            if (GameMaster.Instance != null)
            {
                GameMaster.Instance.SetGamePhase(
                    GameMaster.GamePhase.Wait);
            }
        }
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
            StartStealPhase();
        }
    }

    // =========================================================
    // STEAL PHASE
    // =========================================================

    [Server]
    private void StartStealPhase()
    {
        if (stealRoutine != null)
        {
            StopCoroutine(
                stealRoutine);
        }

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.SetGamePhase(
                GameMaster.GamePhase.Steal);
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
            remainingTime -=
                Time.deltaTime;

            yield return null;
        }

        stealRoutine = null;

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.EndTurn();
        }
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

        selectedCard = null;

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.SetGamePhase(
                GameMaster.GamePhase.Attack);
        }

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
            GameMaster.Instance.SetGamePhase(
                GameMaster.GamePhase.Attack);
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
        {
            FinishSequence();
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

        bool isStealCard =
            card.Data != null &&
            card.Data.CardClass ==
            Card.CardClass.AttackSteal;

        if (!isStealCard &&
            GameMaster.Instance != null)
        {
            GameMaster.Instance.SetGamePhase(
                GameMaster.GamePhase.Attack);
        }

        if (card.gameObject != null)
        {
            NetworkServer.Destroy(
                card.gameObject);
        }

        selectedCard = null;
        attacker = null;
        target = null;

        isActive = false;
        attackRoutine = null;
        targetSelectionRoutine = null;
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

    // =========================================================
    // CANCEL
    // =========================================================

    [Server]
    private void CancelSequence()
    {
        if (!isActive)
            return;

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

        if (previousAttacker != null)
        {
            RpcCancelAttackAnimation(
                previousAttacker.netIdentity);
        }

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.EndTurn();

            GameMaster.Instance.SetGamePhase(
                GameMaster.GamePhase.Attack);
        }

        selectedCard = null;
        attacker = null;
        target = null;

        isActive = false;
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

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.SetGamePhase(
                GameMaster.GamePhase.Attack);
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