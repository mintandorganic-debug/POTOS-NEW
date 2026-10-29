using Mirror;
using Mirror.FizzySteam;
using Steamworks;
using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public static MainMenu instance;

    [SerializeField] InputField ipInput;

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

    private void OnDestroy()
    {
        // Release Steam callbacks so a reloaded menu does not
        // leave stale handlers pointing at a destroyed MainMenu.
        lobbyCreated?.Dispose();
        requestAquired?.Dispose();
        lobbyEntered?.Dispose();

        lobbyCreated = null;
        requestAquired = null;
        lobbyEntered = null;

        if (instance == this)
            instance = null;
    }

    // =========================================================
    // TRANSPORT
    // =========================================================

    // Transports are resolved from the persistent NetworkManager at runtime.
    // Inspector references would point at the scene copy that Mirror destroys
    // when the menu scene is reloaded.
    private static Transport GetSteamTransport()
    {
        NetworkManager manager = NetworkManager.singleton;

        return manager != null
            ? manager.GetComponentInChildren<FizzySteamworks>(true)
            : null;
    }

    private static Transport GetIpTransport()
    {
        NetworkManager manager = NetworkManager.singleton;

        if (manager == null)
            return null;

        foreach (Transport transport in
                 manager.GetComponentsInChildren<Transport>(true))
        {
            if (transport is not FizzySteamworks)
                return transport;
        }

        return null;
    }

    private static bool TrySetTransport(Transport transport)
    {
        if (NetworkManager.singleton is not NetManager manager)
        {
            Debug.LogError("MainMenu: NetManager is missing.");
            return false;
        }

        if (transport == null)
        {
            Debug.LogError("MainMenu: transport is missing on the persistent NetworkManager.");
            return false;
        }

        if (NetworkClient.active || NetworkServer.active)
        {
            Debug.LogWarning("MainMenu: network is already active, transport not changed.");
            return false;
        }

        manager.ChangeTransport(transport);
        Transport.active = transport;

        return true;
    }

    // =========================================================
    // HOST / CLIENT
    // =========================================================

    public void HostSteam()
    {
        if (!TrySetTransport(GetSteamTransport()))
            return;

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 5);
    }

    public void HostIp()
    {
        if (!TrySetTransport(GetIpTransport()))
            return;

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
        // The host also receives LobbyEnter after creating the lobby,
        // and a client must not start a second connection.
        if (NetworkServer.active || NetworkClient.active)
            return;

        CSteamID lobby = new CSteamID(callback.m_ulSteamIDLobby);

        string hostAddress =
            SteamMatchmaking.GetLobbyData(lobby, "HostAddress");

        if (string.IsNullOrEmpty(hostAddress))
        {
            Debug.LogError("MainMenu: lobby has no HostAddress.");
            return;
        }

        if (!TrySetTransport(GetSteamTransport()))
            return;

        Debug.Log($"Connecting to host: {hostAddress}");

        NetworkManager.singleton.StartClient(new Uri($"steam://{hostAddress}"));
    }

    public void Client()
    {
        if (!TrySetTransport(GetIpTransport()))
            return;

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
