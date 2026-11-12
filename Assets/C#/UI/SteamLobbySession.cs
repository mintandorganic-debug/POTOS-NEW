using Mirror;
using Steamworks;
using System;
using UnityEngine;

public class SteamLobbySession : IDisposable
{
    private CSteamID currentLobby;
    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> requestAquired;
    private Callback<LobbyEnter_t> lobbyEntered;

    public SteamLobbySession()
    {
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        requestAquired = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        GameLog.Write(
            "SteamLobbySession",
            "Steam callbacks registered (LobbyCreated, GameLobbyJoinRequested, LobbyEnter)");
    }

    public void CreateLobby()
    {
        GameLog.Write(
            "SteamLobbySession",
            "CreateLobby type=Public maxMembers=5");

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 5);
    }

    public void Dispose()
    {
        GameLog.Write(
            "SteamLobbySession",
            "Dispose");

        // Release Steam callbacks so a reloaded menu does not
        // leave stale handlers pointing at a destroyed MainMenu.
        lobbyCreated?.Dispose();
        requestAquired?.Dispose();
        lobbyEntered?.Dispose();

        lobbyCreated = null;
        requestAquired = null;
        lobbyEntered = null;
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        GameLog.Write(
            "SteamLobbySession",
            $"OnLobbyCreated result={callback.m_eResult} lobbyId={callback.m_ulSteamIDLobby}");

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            GameLog.Write(
                "SteamLobbySession",
                $"OnLobbyCreated failed result={callback.m_eResult}");

            return;
        }

        currentLobby = new CSteamID(callback.m_ulSteamIDLobby);

        SteamMatchmaking.SetLobbyData(
            currentLobby,
            "HostAddress",
            SteamUser.GetSteamID().ToString());

        Debug.Log("Lobby host: " + SteamUser.GetSteamID());

        GameLog.Write(
            "SteamLobbySession",
            $"OnLobbyCreated lobby ready lobbyId={currentLobby.m_SteamID} hostAddress={SteamUser.GetSteamID()}, StartHost");

        NetworkManager.singleton.StartHost();
    }

    private void OnJoinRequested(GameLobbyJoinRequested_t callback)
    {
        GameLog.Write(
            "SteamLobbySession",
            $"OnJoinRequested lobbyId={callback.m_steamIDLobby.m_SteamID} friendId={callback.m_steamIDFriend.m_SteamID}");

        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        GameLog.Write(
            "SteamLobbySession",
            $"OnLobbyEntered lobbyId={callback.m_ulSteamIDLobby} response={callback.m_EChatRoomEnterResponse} serverActive={NetworkServer.active} clientActive={NetworkClient.active}");

        // The host also receives LobbyEnter after creating the lobby,
        // and a client must not start a second connection.
        if (NetworkServer.active || NetworkClient.active)
        {
            GameLog.Write(
                "SteamLobbySession",
                "OnLobbyEntered ignored: network already active (host or already connecting)");

            return;
        }

        CSteamID lobby = new CSteamID(callback.m_ulSteamIDLobby);

        string hostAddress =
            SteamMatchmaking.GetLobbyData(lobby, "HostAddress");

        if (string.IsNullOrEmpty(hostAddress))
        {
            GameLog.Write(
                "SteamLobbySession",
                $"OnLobbyEntered failed: lobby has no HostAddress lobbyId={lobby.m_SteamID}");

            Debug.LogError("MainMenu: lobby has no HostAddress.");
            return;
        }

        if (!NetTransportSelector.Set(NetTransportSelector.GetSteam()))
        {
            GameLog.Write(
                "SteamLobbySession",
                "OnLobbyEntered aborted: Steam transport could not be set");

            return;
        }

        Debug.Log($"Connecting to host: {hostAddress}");

        GameLog.Write(
            "SteamLobbySession",
            $"OnLobbyEntered StartClient hostAddress={hostAddress}");

        NetworkManager.singleton.StartClient(new Uri($"steam://{hostAddress}"));
    }
}
