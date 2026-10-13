using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerEffects : NetworkBehaviour
{
    [SerializeField] private CameraLook cameraController;

    private readonly HashSet<Card> blockedCards =
        new HashSet<Card>();

    private readonly Dictionary<Card, Coroutine> cardBlockCoroutines =
        new Dictionary<Card, Coroutine>();

    public void ApplyCameraMultiplier(
        float multiplier,
        float duration)
    {
        if (!isServer)
            return;

        if (connectionToClient == null)
            return;

        TargetApplyCameraMultiplier(
            connectionToClient,
            multiplier,
            duration);
    }

    [Server]
    public void BlockAllCards(
        float duration)
    {
        List<Card> cards =
            GetOwnedCards();

        foreach (Card card in cards)
        {
            BlockCard(
                card,
                duration);
        }
    }

    [Server]
    public void BlockRandomCard(
        float duration)
    {
        List<Card> cards =
            GetAvailableCards();

        if (cards.Count == 0)
            return;

        Card card =
            cards[Random.Range(0, cards.Count)];

        BlockCard(
            card,
            duration);
    }

    [Server]
    private void BlockCard(
        Card card,
        float duration)
    {
        if (card == null)
            return;

        if (cardBlockCoroutines.TryGetValue(
                card,
                out Coroutine existingCoroutine))
        {
            if (existingCoroutine != null)
            {
                StopCoroutine(
                    existingCoroutine);
            }
        }

        blockedCards.Add(card);

        card.SetAvailable(
            false,
            Card.UnavailableReason.Effect);

        Coroutine coroutine =
            StartCoroutine(
                UnblockCardAfterDuration(
                    card,
                    duration));

        cardBlockCoroutines[card] =
            coroutine;
    }

    [Server]
    private IEnumerator UnblockCardAfterDuration(
        Card card,
        float duration)
    {
        yield return new WaitForSeconds(
            Mathf.Max(duration, 0f));

        if (card != null)
        {
            blockedCards.Remove(card);

            card.RefreshAvailability();
        }

        if (cardBlockCoroutines.ContainsKey(card))
        {
            cardBlockCoroutines.Remove(card);
        }
    }

    [Server]
    private List<Card> GetOwnedCards()
    {
        List<Card> cards =
            new List<Card>();

        if (NetworkServer.spawned == null)
            return cards;

        foreach (NetworkIdentity identity
                 in NetworkServer.spawned.Values)
        {
            if (identity == null)
                continue;

            Card card =
                identity.GetComponent<Card>();

            if (card == null)
                continue;

            if (!card.IsOwnedByLocalConnection(
                    connectionToClient))
            {
                continue;
            }

            cards.Add(card);
        }

        return cards;
    }

    [Server]
    private List<Card> GetAvailableCards()
    {
        List<Card> cards =
            GetOwnedCards();

        cards.RemoveAll(
            card =>
                card == null ||
                !card.IsAvailable ||
                card.IsInRoulette);

        return cards;
    }

    [TargetRpc]
    private void TargetApplyCameraMultiplier(
        NetworkConnectionToClient connection,
        float multiplier,
        float duration)
    {
        if (cameraController == null)
            return;

        cameraController.ApplyMultiplyer(
            multiplier,
            duration);
    }
}