using Mirror;
using UnityEngine;

public class LobbyHandler : NetworkBehaviour
{
    public static LobbyHandler instance;

    void Start()
    {
        GameLog.Write(
            "LobbyHandler",
            $"Start netId={netId} isServer={isServer} isClient={isClient} previousInstance={(instance != null ? instance.name : "NULL")}");

        instance = this;
    }

    [ClientRpc]
    public void RpcRefreshLobby()
    {
        GameLog.Write(
            "LobbyHandler",
            $"RpcRefreshLobby lobbyMenu={(LobbyMenu.instance != null ? "present" : "NULL")}");

        LobbyMenu.instance?.RefreshLobby();
    }

    public void Disconnect()
    {
        GameLog.Write(
            "LobbyHandler",
            $"Disconnect serverActive={NetworkServer.active} clientConnected={NetworkClient.isConnected}");

        if (NetworkServer.active && NetworkClient.isConnected)
        {
            GameLog.Write(
                "LobbyHandler",
                "Disconnect StopHost");

            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            GameLog.Write(
                "LobbyHandler",
                "Disconnect StopClient");

            NetworkManager.singleton.StopClient();
        }
        else
        {
            GameLog.Write(
                "LobbyHandler",
                "Disconnect nothing to stop");
        }
    }
}
