using Mirror;
using UnityEngine;

/// <summary>
/// Network manager of the test scene. Unlike the game's NetManager it has no
/// lobby: the Player is spawned as soon as somebody connects, and the match is
/// started by hand from the test panel.
///
/// Configured automatically by  Tools > Testing > Create Test Scene.
/// </summary>
public class TestNetworkManager : NetworkManager
{
    public override void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        base.Awake();
    }
    public override void OnServerConnect(
        NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        GameLog.Event(
            LogCat.Net,
            $"Client connected: connection {conn.connectionId}");
    }

    public override void OnServerAddPlayer(
        NetworkConnectionToClient conn)
    {
        GameObject playerObject =
            Instantiate(
                playerPrefab);

        string nickname =
            conn.connectionId == 0
                ? "HOST"
                : $"TESTER-{conn.connectionId}";

        if (playerObject.TryGetComponent(
                out Player player))
        {
            player.SetSteamData(
                0UL,
                nickname);
        }

        playerObject.name =
            $"Player [{nickname}]";

        NetworkServer.AddPlayerForConnection(
            conn,
            playerObject);

        GameLog.Event(
            LogCat.Net,
            $"Player spawned for connection {conn.connectionId} ({nickname})");
    }

    public override void OnServerDisconnect(
        NetworkConnectionToClient conn)
    {
        GameLog.Warn(
            LogCat.Net,
            $"Client disconnected: connection {conn.connectionId}");

        bool matchRunning =
            GameMaster.Instance != null &&
            GameMaster.Instance.IsMatchRunning;

        base.OnServerDisconnect(conn);

        if (matchRunning)
        {
            GameLog.Warn(
                LogCat.Match,
                "A player left during a match: resetting the match.");

            GameMaster.Instance.ResetMatch();
        }
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        GameLog.Event(
            LogCat.Net,
            "Connected to server.");
    }

    public override void OnStartHost()
    {
        GameLog.Event(
            LogCat.Net,
            "Host started.");
    }

    public override void OnStopHost()
    {
        GameLog.Event(
            LogCat.Net,
            "Host stopped.");
    }
}
