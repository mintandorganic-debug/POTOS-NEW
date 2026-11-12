using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEffects : NetworkBehaviour
{
    [SerializeField] private CameraLook cameraController;

    private readonly HashSet<Card> blockedCards =
        new HashSet<Card>();

    private readonly Dictionary<Card, Coroutine> cardBlockCoroutines =
        new Dictionary<Card, Coroutine>();

    [Server]
    public void ApplyCameraMultiplier(
        float multiplier,
        float duration)
    {
        GameLog.Write(
            "PlayerEffects",
            $"ApplyCameraMultiplier netId={netId} multiplier={multiplier} duration={duration}");

        if (multiplier <= 0f ||
            duration < 0f)
        {
            GameLog.Write(
                "PlayerEffects",
                $"ApplyCameraMultiplier rejected: invalid multiplier or duration netId={netId} multiplier={multiplier} duration={duration}");

            return;
        }

        Player player =
            GetComponentInParent<Player>();

        NetworkConnectionToClient targetConnection =
            player != null &&
            player.netIdentity != null
                ? player.netIdentity.connectionToClient
                : connectionToClient;

        GameLog.Write(
            "PlayerEffects",
            $"ApplyCameraMultiplier target resolved netId={netId} player={(player != null ? player.name : "NULL")} connection={(targetConnection != null ? targetConnection.connectionId.ToString() : "NULL")}");

        if (targetConnection == null)
        {
            GameLog.Write(
                "PlayerEffects",
                $"ApplyCameraMultiplier rejected: target connection is NULL netId={netId}");
            return;
        }

        GameLog.Write(
            "PlayerEffects",
            $"ApplyCameraMultiplier sending to client netId={netId} connection={targetConnection.connectionId} multiplier={multiplier} duration={duration}");

        TargetApplyCameraMultiplier(
            targetConnection,
            multiplier,
            duration);
    }
     
    [Server]
    public void BlockAllCards(
        float duration)
    {
        GameLog.Write(
            "PlayerEffects",
            $"BlockAllCards netId={netId} duration={duration}");

        List<Card> cards =
            GetOwnedCards();

        GameLog.Write(
            "PlayerEffects",
            $"BlockAllCards owned cards found netId={netId} count={cards.Count}");

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
        GameLog.Write(
            "PlayerEffects",
            $"BlockRandomCard netId={netId} duration={duration}");

        List<Card> cards =
            GetAvailableCards();

        if (cards.Count == 0)
        {
            GameLog.Write(
                "PlayerEffects",
                $"BlockRandomCard rejected: no available cards netId={netId}");
            return;
        }

        Card card =
            cards[Random.Range(0, cards.Count)];

        GameLog.Write(
            "PlayerEffects",
            $"BlockRandomCard picked card netId={netId} card={(card != null ? card.name : "NULL")} availableCount={cards.Count}");

        BlockCard(
            card,
            duration);
    }

    [Server]
    private void BlockCard(
        Card card,
        float duration)
    {
        GameLog.Write(
            "PlayerEffects",
            $"BlockCard netId={netId} card={(card != null ? card.name : "NULL")} duration={duration} alreadyBlocked={blockedCards.Count}");

        if (card == null)
        {
            GameLog.Write(
                "PlayerEffects",
                $"BlockCard rejected: card is NULL netId={netId}");
            return;
        }

        if (cardBlockCoroutines.TryGetValue(
                card,
                out Coroutine existingCoroutine))
        {
            if (existingCoroutine != null)
            {
                GameLog.Write(
                    "PlayerEffects",
                    $"BlockCard restarting block timer netId={netId} card={(card != null ? card.name : "NULL")}");

                StopCoroutine(
                    existingCoroutine);
            }
        }

        blockedCards.Add(card);

        GameLog.Write(
            "PlayerEffects",
            $"BlockCard applying block netId={netId} card={(card != null ? card.name : "NULL")} duration={duration} blockedCount={blockedCards.Count}");

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
        GameLog.Write(
            "PlayerEffects",
            $"UnblockCardAfterDuration start netId={netId} card={(card != null ? card.name : "NULL")} duration={duration}");

        yield return new WaitForSeconds(
            Mathf.Max(duration, 0f));

        GameLog.Write(
            "PlayerEffects",
            $"UnblockCardAfterDuration elapsed netId={netId} card={(card != null ? card.name : "NULL")}");

        if (card != null)
        {
            blockedCards.Remove(card);

            GameLog.Write(
                "PlayerEffects",
                $"UnblockCardAfterDuration unblocking netId={netId} card={(card != null ? card.name : "NULL")} blockedCount={blockedCards.Count}");

            card.RefreshAvailability();
        }
        else
        {
            GameLog.Write(
                "PlayerEffects",
                $"UnblockCardAfterDuration card no longer exists netId={netId}");
        }

        if (cardBlockCoroutines.ContainsKey(card))
        {
            cardBlockCoroutines.Remove(card);

            GameLog.Write(
                "PlayerEffects",
                $"UnblockCardAfterDuration block entry removed netId={netId} remainingEntries={cardBlockCoroutines.Count}");
        }

        GameLog.Write(
            "PlayerEffects",
            $"UnblockCardAfterDuration finished netId={netId}");
    }

    [Server]
    private List<Card> GetOwnedCards()
    {
        GameLog.Write(
            "PlayerEffects",
            $"GetOwnedCards netId={netId} connection={(connectionToClient != null ? connectionToClient.connectionId.ToString() : "NULL")}");

        List<Card> cards =
            new List<Card>();

        if (NetworkServer.spawned == null)
        {
            GameLog.Write(
                "PlayerEffects",
                $"GetOwnedCards rejected: NetworkServer.spawned is NULL netId={netId}");
            return cards;
        }

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

        GameLog.Write(
            "PlayerEffects",
            $"GetOwnedCards done netId={netId} count={cards.Count}");

        return cards;
    }

    [Server]
    private List<Card> GetAvailableCards()
    {
        GameLog.Write(
            "PlayerEffects",
            $"GetAvailableCards netId={netId}");

        List<Card> cards =
            GetOwnedCards();

        cards.RemoveAll(
            card =>
                card == null ||
                !card.IsAvailable ||
                card.IsInRoulette);

        GameLog.Write(
            "PlayerEffects",
            $"GetAvailableCards done netId={netId} count={cards.Count}");

        return cards;
    }

    [TargetRpc]
    private void TargetApplyCameraMultiplier(
        NetworkConnectionToClient connection,
        float multiplier,
        float duration)
    {
        GameLog.Write(
            "PlayerEffects",
            $"TargetApplyCameraMultiplier netId={netId} connection={(connection != null ? connection.connectionId.ToString() : "NULL")} multiplier={multiplier} duration={duration} cameraController={(cameraController != null ? "set" : "NULL")}");

        if (cameraController == null)
        {
            Player player =
                GetComponentInParent<Player>();

            if (player != null)
            {
                cameraController =
                    player.cameraController;
            }

            if (cameraController == null)
            {
                cameraController =
                    GetComponentInParent<CameraLook>();
            }

            if (cameraController == null)
            {
                cameraController =
                    GetComponentInChildren<CameraLook>(true);
            }
        }

        if (cameraController == null)
        {
            GameLog.Write(
                "PlayerEffects",
                $"TargetApplyCameraMultiplier rejected: CameraLook not found netId={netId}");
            return;
        }

        GameLog.Write(
            "PlayerEffects",
            $"TargetApplyCameraMultiplier applying netId={netId} multiplier={multiplier} duration={duration}");

        cameraController.ApplyMultiplyer(
            multiplier,
            duration);
    }
}