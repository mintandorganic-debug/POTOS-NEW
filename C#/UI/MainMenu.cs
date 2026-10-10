using kcp2k;
using Mirror;
using Steamworks;
using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public static MainMenu instance;

    [SerializeField] InputField ipInput;
    [SerializeField] Transport steamTransport;
    [SerializeField] Transport ipTransport;

    private CSteamID currentLobby;
    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> requestAquired;
    private Callback<LobbyEnter_t> lobbyEntered;

    private void Start()
    {
        Cursor.visible = true;

        instance = this;

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        requestAquired = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public void HostSteam()
    {
        ((NetManager)NetworkManager.singleton).ChangeTransport(steamTransport);
        Transport.active = steamTransport;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 5);
    }

    public void HostIp()
    {
        ((NetManager)NetworkManager.singleton).ChangeTransport(ipTransport);
        Transport.active = ipTransport;
        NetworkManager.singleton.StartHost();
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
            return;

        currentLobby = new CSteamID(callback.m_ulSteamIDLobby);

        SteamMatchmaking.SetLobbyData(
            currentLobby,
            "HostAddress",
            SteamUser.GetSteamID().ToString());

        Debug.Log("Lobby host: " + SteamUser.GetSteamID());

        NetworkManager.singleton.StartHost();
    }
    private void OnJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (NetworkServer.active)
            return;

        CSteamID lobby = new CSteamID(callback.m_ulSteamIDLobby);

        string hostAddress =
            SteamMatchmaking.GetLobbyData(lobby, "HostAddress");

        Debug.Log($"Connecting to host: {hostAddress}");

        Transport.active = steamTransport;
        NetworkManager.singleton.transport = instance?.steamTransport;

        Uri uri = new Uri($"steam://{hostAddress}");
        NetworkManager.singleton.StartClient(uri);
    }

    public void Client()
    {
        ((NetManager)NetworkManager.singleton).ChangeTransport(ipTransport);
        Transport.active = ipTransport;

        NetworkManager.singleton.networkAddress = ipInput.text;
        NetworkManager.singleton.StartClient();
    }

    public void Server()
    {
        NetworkManager.singleton.StartServer();
    }

    public void Stop()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
        else if (NetworkServer.active)
            NetworkManager.singleton.StopServer();
    }

    public void CloseApp()
    {
        Application.Quit();
    }
}