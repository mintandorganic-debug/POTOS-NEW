using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

/// <summary>Small server-side helpers shared by the test scene scripts.</summary>
public static class TestTools
{
    public static List<Player> AllPlayers()
    {
        return Object
            .FindObjectsByType<Player>()
            .Where(p => p != null)
            .OrderBy(p => p.netId)
            .ToList();
    }

    public static List<Card> AllCards()
    {
        return Object
            .FindObjectsByType<Card>()
            .Where(c => c != null)
            .ToList();
    }

    public static List<Card> HandOf(
        Player player)
    {
        if (player == null)
            return new List<Card>();

        return AllCards()
            .Where(c => c.OwnerNetId == player.netId)
            .OrderBy(c => c.SocketIndex)
            .ToList();
    }

    public static int AccessoryCount(
        Player player)
    {
        if (player == null)
            return 0;

        int count = 0;

        for (int i = 0; i < 5; i++)
        {
            if (player.HasAccessory(i))
                count++;
        }

        return count;
    }

    public static string AccessoryString(
        Player player)
    {
        if (player == null)
            return "-";

        string text = string.Empty;

        for (int i = 0; i < 5; i++)
        {
            if (player.HasAccessory(i))
                text += i;
        }

        return text.Length == 0 ? "none" : text;
    }

    /// <summary>Throws away the player's cards and deals new ones.</summary>
    [Server]
    public static void Redeal(
        Player player)
    {
        if (!NetworkServer.active ||
            player == null)
        {
            return;
        }

        foreach (Card card in HandOf(player))
            NetworkServer.Destroy(card.gameObject);

        GameLog.Event(
            LogCat.Test,
            $"Redeal {GameLog.P(player)}");

        player.SummonCards();
    }

    [Server]
    public static void RedealAll(
        IEnumerable<Player> players)
    {
        foreach (Player player in players.ToList())
            Redeal(player);
    }

    /// <summary>Index of 'data' in the card's type list (-1 if not found).</summary>
    public static int TypeIndex(
        Card card,
        CardData data)
    {
        if (card == null ||
            data == null ||
            card.AvailableTypes == null)
        {
            return -1;
        }

        for (int i = 0;
             i < card.AvailableTypes.Count;
             i++)
        {
            if (card.AvailableTypes[i] == data)
                return i;
        }

        return -1;
    }
}
