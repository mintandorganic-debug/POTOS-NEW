using Mirror;
using UnityEngine;

public class LobbyHandler : NetworkBehaviour
{
    public static LobbyHandler instance;

    void Start()
    {
        instance = this;
    }

    [ClientRpc]
    public void RpcRefreshLobby()
    {
        LobbyMenu.instance?.RefreshLobby();
    }

    public void Disconnect()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
    }
}
