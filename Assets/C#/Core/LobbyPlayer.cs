using Mirror;
using Steamworks;
using UnityEngine;

public class LobbyPlayer : NetworkRoomPlayer
{
    public UIPlayer player_UIElement;

    [SyncVar(hook = nameof(OnSteamIdChanged))]
    public ulong SteamId;

    [SyncVar(hook = nameof(OnNicknameChanged))]
    public string Nickname;

    public override void OnStartLocalPlayer()
    {
        GameLog.Write(
            "LobbyPlayer",
            $"OnStartLocalPlayer netId={netId} isServer={isServer} isClient={isClient}");

        base.OnStartLocalPlayer();

        CmdInitialize(
            SteamUser.GetSteamID().m_SteamID,
            SteamFriends.GetPersonaName());
    }

    [Command]
    private void CmdInitialize(ulong steamId, string nickname)
    {
        GameLog.Write(
            "LobbyPlayer",
            $"CmdInitialize netId={netId} steamId={steamId} nickname={nickname}");

        SteamId = steamId;
        Nickname = nickname;
    }

    private void OnSteamIdChanged(ulong oldValue, ulong newValue)
    {
        GameLog.Write(
            "LobbyPlayer",
            $"OnSteamIdChanged netId={netId} old={oldValue} new={newValue}");

        LobbyMenu.instance?.RefreshLobby();
    }

    private void OnNicknameChanged(string oldValue, string newValue)
    {
        GameLog.Write(
            "LobbyPlayer",
            $"OnNicknameChanged netId={netId} old={oldValue} new={newValue}");

        LobbyMenu.instance?.RefreshLobby();
    }

    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        GameLog.Write(
            "LobbyPlayer",
            $"ReadyStateChanged netId={netId} nickname={Nickname} old={oldReadyState} new={newReadyState} uiElement={(player_UIElement != null ? player_UIElement.name : "NULL")}");

        base.ReadyStateChanged(oldReadyState, newReadyState);

        if (player_UIElement != null)
            player_UIElement.readyCheck.SetActive(newReadyState);
    }

    public void ToggleReady()
    {
        GameLog.Write(
            "LobbyPlayer",
            $"ToggleReady netId={netId} nickname={Nickname} current={readyToBegin} requested={!readyToBegin}");

        CmdChangeReadyState(!readyToBegin);
    }
    public void ToggleReady(bool state)
    {
        GameLog.Write(
            "LobbyPlayer",
            $"ToggleReady netId={netId} nickname={Nickname} current={readyToBegin} requested={state}");

        CmdChangeReadyState(state);
    }

    public override void OnStartClient()
    {
        GameLog.Write(
            "LobbyPlayer",
            $"OnStartClient netId={netId} isLocalPlayer={isLocalPlayer} nickname={Nickname} steamId={SteamId}");

        base.OnStartClient();
        LobbyMenu.instance?.RefreshLobby();
    }

    public override void OnStopClient()
    {
        GameLog.Write(
            "LobbyPlayer",
            $"OnStopClient netId={netId} isLocalPlayer={isLocalPlayer} nickname={Nickname}");

        base.OnStopClient();
    }


    [Server]
    public void Kick()
    {
        GameLog.Write(
            "LobbyPlayer",
            $"Kick netId={netId} nickname={Nickname}");

        var conn = connectionToClient;
        if (conn == null)
        {
            GameLog.Write(
                "LobbyPlayer",
                $"Kick aborted: connectionToClient is NULL netId={netId}");

            return;
        }

        conn.Disconnect();
    }
}
