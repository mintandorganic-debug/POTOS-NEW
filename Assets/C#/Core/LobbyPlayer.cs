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
        base.OnStartLocalPlayer();

        CmdInitialize(
            SteamUser.GetSteamID().m_SteamID,
            SteamFriends.GetPersonaName());
    }

    [Command]
    private void CmdInitialize(ulong steamId, string nickname)
    {
        SteamId = steamId;
        Nickname = nickname;
    }

    private void OnSteamIdChanged(ulong oldValue, ulong newValue)
    {
        LobbyMenu.instance?.RefreshLobby();
    }

    private void OnNicknameChanged(string oldValue, string newValue)
    {
        LobbyMenu.instance?.RefreshLobby();
    }

    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        base.ReadyStateChanged(oldReadyState, newReadyState);

        if (player_UIElement != null)
            player_UIElement.readyCheck.SetActive(newReadyState);
    }

    public void ToggleReady()
    {
        CmdChangeReadyState(!readyToBegin);
    }
    public void ToggleReady(bool state)
    {
        CmdChangeReadyState(state);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        LobbyMenu.instance?.RefreshLobby();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
    }


    [Server]
    public void Kick()
    {
        var conn = connectionToClient;
        if (conn == null)
            return;

        conn.Disconnect();
    }
}