using Mirror;

public static class AttackValidator
{
    public static bool IsCardOwnedByPlayer(
        Card card,
        Player player)
    {
        GameLog.Write(
            "AttackValidator",
            $"IsCardOwnedByPlayer card={(card != null && card.Data != null ? card.Data.name : "NULL")} player={(player != null ? player.netId.ToString() : "NULL")}");

        if (card == null ||
            player == null)
        {
            GameLog.Write(
                "AttackValidator",
                "IsCardOwnedByPlayer false: card or player is null");

            return false;
        }

        return card.IsOwnedByLocalConnection(
            player.netIdentity.connectionToClient);
    }

    public static Player FindPlayerByConnection(
        NetworkConnectionToClient connection)
    {
        GameLog.Write(
            "AttackValidator",
            $"FindPlayerByConnection connection={(connection != null ? connection.connectionId.ToString() : "NULL")}");

        if (connection == null)
        {
            GameLog.Write(
                "AttackValidator",
                "FindPlayerByConnection null: connection is null");

            return null;
        }

        if (connection.identity == null)
        {
            GameLog.Write(
                "AttackValidator",
                $"FindPlayerByConnection null: connection {connection.connectionId} has no identity");

            return null;
        }

        return connection.identity.GetComponent<Player>();
    }
}
