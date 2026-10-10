using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NetManager : NetworkRoomManager
{
    private bool gameStarting;
    private bool sceneChanging;

    // =========================================================
    // ROOM PLAYER
    // =========================================================

    public override void OnServerAddPlayer(
        NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        if (LobbyHandler.instance == null)
        {
            Debug.LogWarning(
                "LobbyHandler instance is missing. Lobby refresh skipped.");

            return;
        }

        LobbyHandler.instance.RpcRefreshLobby();
    }

    // =========================================================
    // GAME PLAYER
    // =========================================================

    public override GameObject OnRoomServerCreateGamePlayer(
        NetworkConnectionToClient conn,
        GameObject roomPlayer)
    {
        GameObject gamePlayer =
            Instantiate(
                playerPrefab);

        LobbyPlayer lobbyPlayer =
            roomPlayer.GetComponent<LobbyPlayer>();

        Player player =
            gamePlayer.GetComponent<Player>();

        if (lobbyPlayer != null &&
            player != null)
        {
            player.SetSteamData(
                lobbyPlayer.SteamId,
                lobbyPlayer.Nickname);
        }
        else
        {
            if (lobbyPlayer == null)
            {
                Debug.LogWarning(
                    "NetManager: Room player does not have a LobbyPlayer component.",
                    roomPlayer);
            }

            if (player == null)
            {
                Debug.LogWarning(
                    "NetManager: Game player prefab does not have a Player component.",
                    gamePlayer);
            }
        }

        return gamePlayer;
    }

    // =========================================================
    // GAME START
    // =========================================================

    [Server]
    public bool TryStartGame()
    {
        var slots = roomSlots;

        if (!NetworkServer.active ||
            gameStarting ||
            sceneChanging ||
            roomSlots.Count < minPlayers ||
            roomSlots.Any(p => p == null) ||
            roomSlots.Distinct().Count() != roomSlots.Count ||
            roomSlots.Count != NetworkServer.connections.Count ||
            roomSlots.Skip(1).Any(p => !p.readyToBegin))
        {
            return false;
        }

        gameStarting = true;
        sceneChanging = true;

        ServerChangeScene(
            GameplayScene);

        return true;
    }

    // =========================================================
    // DISCONNECT
    // =========================================================

    public override void OnServerDisconnect(
        NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(
            conn);

        bool wasStarting =
            gameStarting ||
            sceneChanging;

        if (wasStarting)
            StopHost();

        if (LobbyHandler.instance == null)
        {
            Debug.LogWarning(
                "LobbyHandler instance is missing. Lobby refresh skipped.");

            return;
        }

        LobbyHandler.instance.RpcRefreshLobby();
    }

    // =========================================================
    // TRANSPORT
    // =========================================================

    public void ChangeTransport(
        Transport _transport)
    {
        transport = _transport;
    }

    // =========================================================
    // SCENE
    // =========================================================

    public override void OnRoomServerSceneChanged(
        string sceneName)
    {
        base.OnRoomServerSceneChanged(
            sceneName);

        gameStarting = false;
        sceneChanging = false;
    }

    public override void OnRoomServerPlayersReady()
    {
        // Do nothing
    }
}