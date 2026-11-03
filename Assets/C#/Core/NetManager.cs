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
        GameLog.Write(
            "NetManager",
            $"OnServerAddPlayer conn={(conn != null ? conn.connectionId.ToString() : "NULL")} roomSlots={roomSlots.Count}");

        base.OnServerAddPlayer(conn);

        if (LobbyHandler.instance == null)
        {
            GameLog.Write(
                "NetManager",
                "OnServerAddPlayer lobby refresh skipped: LobbyHandler.instance is NULL");

            Debug.LogWarning(
                "LobbyHandler instance is missing. Lobby refresh skipped.");

            return;
        }

        GameLog.Write(
            "NetManager",
            $"OnServerAddPlayer RpcRefreshLobby roomSlots={roomSlots.Count}");

        LobbyHandler.instance.RpcRefreshLobby();
    }

    // =========================================================
    // GAME PLAYER
    // =========================================================

    public override GameObject OnRoomServerCreateGamePlayer(
        NetworkConnectionToClient conn,
        GameObject roomPlayer)
    {
        GameLog.Write(
            "NetManager",
            $"OnRoomServerCreateGamePlayer conn={(conn != null ? conn.connectionId.ToString() : "NULL")} roomPlayer={(roomPlayer != null ? roomPlayer.name : "NULL")}");

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
            GameLog.Write(
                "NetManager",
                $"OnRoomServerCreateGamePlayer SetSteamData steamId={lobbyPlayer.SteamId} nickname={lobbyPlayer.Nickname} gamePlayer={gamePlayer.name}");

            player.SetSteamData(
                lobbyPlayer.SteamId,
                lobbyPlayer.Nickname);
        }
        else
        {
            if (lobbyPlayer == null)
            {
                GameLog.Write(
                    "NetManager",
                    "OnRoomServerCreateGamePlayer room player has no LobbyPlayer component");

                Debug.LogWarning(
                    "NetManager: Room player does not have a LobbyPlayer component.",
                    roomPlayer);
            }

            if (player == null)
            {
                GameLog.Write(
                    "NetManager",
                    "OnRoomServerCreateGamePlayer game player prefab has no Player component");

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
    public bool StartGame()
    {
        GameLog.Write(
            "NetManager",
            $"StartGame serverActive={NetworkServer.active} gameStarting={gameStarting} sceneChanging={sceneChanging} roomSlots={roomSlots.Count} minPlayers={minPlayers} connections={NetworkServer.connections.Count}");

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
            GameLog.Write(
                "NetManager",
                $"StartGame refused serverActive={NetworkServer.active} gameStarting={gameStarting} sceneChanging={sceneChanging} roomSlots={roomSlots.Count} minPlayers={minPlayers} nullSlots={roomSlots.Count(p => p == null)} distinctSlots={roomSlots.Distinct().Count()} connections={NetworkServer.connections.Count} notReady={roomSlots.Count(p => p != null && !p.readyToBegin)}");

            return false;
        }

        gameStarting = true;
        sceneChanging = true;

        GameLog.Write(
            "NetManager",
            $"StartGame ServerChangeScene scene={GameplayScene} players={roomSlots.Count}");

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
        GameLog.Write(
            "NetManager",
            $"OnServerDisconnect conn={(conn != null ? conn.connectionId.ToString() : "NULL")} gameStarting={gameStarting} sceneChanging={sceneChanging}");

        base.OnServerDisconnect(
            conn);

        bool wasStarting =
            gameStarting ||
            sceneChanging;

        if (wasStarting)
        {
            GameLog.Write(
                "NetManager",
                "OnServerDisconnect disconnect during game start, StopHost");

            StopHost();
        }

        if (LobbyHandler.instance == null)
        {
            GameLog.Write(
                "NetManager",
                "OnServerDisconnect lobby refresh skipped: LobbyHandler.instance is NULL");

            Debug.LogWarning(
                "LobbyHandler instance is missing. Lobby refresh skipped.");

            return;
        }

        GameLog.Write(
            "NetManager",
            "OnServerDisconnect RpcRefreshLobby");

        LobbyHandler.instance.RpcRefreshLobby();
    }

    // =========================================================
    // TRANSPORT
    // =========================================================

    public void ChangeTransport(
        Transport _transport)
    {
        GameLog.Write(
            "NetManager",
            $"ChangeTransport transport={(_transport != null ? _transport.GetType().Name : "NULL")}");

        transport = _transport;
    }

    // =========================================================
    // SCENE
    // =========================================================

    public override void OnRoomServerSceneChanged(
        string sceneName)
    {
        GameLog.Write(
            "NetManager",
            $"OnRoomServerSceneChanged scene={sceneName}");

        base.OnRoomServerSceneChanged(
            sceneName);

        gameStarting = false;
        sceneChanging = false;
    }

    public override void OnRoomServerPlayersReady()
    {
        GameLog.Write(
            "NetManager",
            $"OnRoomServerPlayersReady ignored roomSlots={roomSlots.Count}");

        // Do nothing
    }
}
