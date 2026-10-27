using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : NetworkBehaviour
{
    [SerializeField] private Player player;

    private readonly SyncList<uint> slots =
        new SyncList<uint>();

    [SyncVar(hook = nameof(OnSwapStateChanged))]
    private uint swapTargetNetId;

    [SyncVar]
    private double swapEndTime;

    [SyncVar]
    private int swapMaxCards;

    private Coroutine swapRoutine;

    public event Action Changed;
    public event Action SwapChanged;

    public int SlotCount =>
        slots.Count;

    public bool IsSwapOpen =>
        swapTargetNetId != 0 &&
        NetworkTime.time < swapEndTime;

    public double SwapEndTime =>
        swapEndTime;

    public int SwapMaxCards =>
        swapMaxCards;

    private void Awake()
    {
        if (player == null)
            player = GetComponent<Player>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        slots.Clear();

        int count =
            player != null &&
            player.cardSockets != null
                ? player.cardSockets.Count
                : 0;

        for (int i = 0;
             i < count;
             i++)
        {
            slots.Add(0);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        slots.OnChange += OnSlotsChanged;
    }

    public override void OnStopClient()
    {
        slots.OnChange -= OnSlotsChanged;

        base.OnStopClient();
    }

    private void OnSlotsChanged(
        SyncList<uint>.Operation operation,
        int index,
        uint value)
    {
        Changed?.Invoke();
    }

    // =========================================================
    // SERVER
    // =========================================================

    [Server]
    public bool AddCard(
        Card card)
    {
        if (card == null)
            return false;

        int slot =
            slots.IndexOf(0);

        if (slot < 0)
            return false;

        PlaceCard(
            card,
            slot);

        return true;
    }

    [Server]
    public void RemoveCard(
        Card card)
    {
        if (card == null)
            return;

        int slot =
            slots.IndexOf(card.netId);

        if (slot < 0)
            return;

        slots[slot] = 0;
    }

    [Server]
    public bool TransferCard(
        Card card,
        PlayerHand target)
    {
        if (card == null ||
            target == null ||
            target == this)
        {
            return false;
        }

        int slot =
            target.slots.IndexOf(0);

        if (slot < 0)
            return false;

        RemoveCard(card);

        target.PlaceCard(
            card,
            slot);

        return true;
    }

    [Server]
    public bool SwapCards(
        Card ownCard,
        PlayerHand otherHand,
        Card otherCard)
    {
        if (ownCard == null ||
            otherCard == null ||
            otherHand == null ||
            otherHand == this)
        {
            return false;
        }

        int ownSlot =
            slots.IndexOf(ownCard.netId);

        int otherSlot =
            otherHand.slots.IndexOf(otherCard.netId);

        if (ownSlot < 0 ||
            otherSlot < 0)
        {
            return false;
        }

        if (ownCard.IsInRoulette ||
            otherCard.IsInRoulette)
        {
            return false;
        }

        PlaceCard(
            otherCard,
            ownSlot);

        otherHand.PlaceCard(
            ownCard,
            otherSlot);

        return true;
    }

    [Server]
    private void PlaceCard(
        Card card,
        int slot)
    {
        slots[slot] = card.netId;

        card.owner = player;

        NetworkIdentity identity =
            card.netIdentity;

        NetworkConnectionToClient connection =
            player != null
                ? player.connectionToClient
                : null;

        if (identity.connectionToClient != connection)
        {
            if (identity.connectionToClient != null)
                identity.RemoveClientAuthority();

            if (connection != null)
                identity.AssignClientAuthority(connection);
        }

        card.SetOwner(
            netId,
            slot);
    }

    // =========================================================
    // SWAP
    // =========================================================

    [Server]
    public void OpenSwap(
        PlayerHand target,
        float duration,
        int maxCards)
    {
        if (target == null ||
            target == this ||
            duration <= 0f ||
            maxCards <= 0)
        {
            return;
        }

        if (swapRoutine != null)
            StopCoroutine(swapRoutine);

        swapMaxCards = maxCards;
        swapEndTime = NetworkTime.time + duration;
        swapTargetNetId = target.netId;

        swapRoutine =
            StartCoroutine(
                CloseSwapAfter(duration));
    }

    [Server]
    public void CloseSwap()
    {
        if (swapRoutine != null)
        {
            StopCoroutine(swapRoutine);
            swapRoutine = null;
        }

        swapTargetNetId = 0;
        swapEndTime = 0;
        swapMaxCards = 0;
    }

    [Server]
    private IEnumerator CloseSwapAfter(
        float duration)
    {
        yield return new WaitForSeconds(
            duration);

        swapRoutine = null;

        CloseSwap();
    }

    [Command]
    public void CmdRequestSwap(
        uint[] ownCards,
        uint[] targetCards)
    {
        if (!IsSwapOpen ||
            !TryGetSwapTarget(
                out PlayerHand target))
        {
            return;
        }

        if (ownCards == null ||
            targetCards == null ||
            ownCards.Length == 0 ||
            ownCards.Length != targetCards.Length ||
            ownCards.Length > swapMaxCards)
        {
            return;
        }

        List<Card> own =
            ResolveCards(
                ownCards,
                this);

        List<Card> theirs =
            ResolveCards(
                targetCards,
                target);

        if (own == null ||
            theirs == null)
        {
            return;
        }

        for (int i = 0;
             i < own.Count;
             i++)
        {
            SwapCards(
                own[i],
                target,
                theirs[i]);
        }

        CloseSwap();
    }

    [Command]
    public void CmdCancelSwap()
    {
        CloseSwap();
    }

    [Server]
    private List<Card> ResolveCards(
        uint[] cardNetIds,
        PlayerHand hand)
    {
        List<Card> cards =
            new List<Card>();

        foreach (uint cardNetId in cardNetIds)
        {
            if (!hand.slots.Contains(cardNetId))
                return null;

            if (!NetworkServer.spawned.TryGetValue(
                    cardNetId,
                    out NetworkIdentity identity) ||
                !identity.TryGetComponent(
                    out Card card))
            {
                return null;
            }

            if (cards.Contains(card) ||
                card.IsInRoulette)
            {
                return null;
            }

            cards.Add(card);
        }

        return cards;
    }

    public bool TryGetSwapTarget(
        out PlayerHand target)
    {
        target = null;

        if (swapTargetNetId == 0)
            return false;

        if (!TryGetSpawned(
                swapTargetNetId,
                out NetworkIdentity identity))
        {
            return false;
        }

        return identity.TryGetComponent(
            out target);
    }

    private void OnSwapStateChanged(
        uint oldValue,
        uint newValue)
    {
        SwapChanged?.Invoke();
    }

    // =========================================================
    // QUERY
    // =========================================================

    public List<Card> GetCards()
    {
        List<Card> cards =
            new List<Card>();

        foreach (uint cardNetId in slots)
        {
            if (cardNetId == 0)
                continue;

            if (!TryGetSpawned(
                    cardNetId,
                    out NetworkIdentity identity))
            {
                continue;
            }

            if (identity.TryGetComponent(
                    out Card card))
            {
                cards.Add(card);
            }
        }

        return cards;
    }

    public bool Contains(
        Card card)
    {
        return card != null &&
               slots.Contains(card.netId);
    }

    private bool TryGetSpawned(
        uint id,
        out NetworkIdentity identity)
    {
        if (isServer)
        {
            return NetworkServer.spawned.TryGetValue(
                id,
                out identity);
        }

        return NetworkClient.spawned.TryGetValue(
            id,
            out identity);
    }
}
