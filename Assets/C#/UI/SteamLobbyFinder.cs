using Steamworks;
using System;

public class SteamLobbyFinder : IDisposable
{
    private readonly CallResult<LobbyMatchList_t> lobbyMatchList;

    private Action<CSteamID> onFound;

    public SteamLobbyFinder()
    {
        lobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
    }

    public void Find(Action<CSteamID> onFound)
    {
        this.onFound = onFound;

        SteamMatchmaking.AddRequestLobbyListStringFilter(
            SteamLobbyTag.Key,
            SteamLobbyTag.Value,
            ELobbyComparison.k_ELobbyComparisonEqual);

        SteamMatchmaking.AddRequestLobbyListDistanceFilter(
            ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(1);

        lobbyMatchList.Set(SteamMatchmaking.RequestLobbyList());
    }

    public void Dispose()
    {
        lobbyMatchList.Dispose();
    }

    private void OnLobbyMatchList(LobbyMatchList_t result, bool ioFailure)
    {
        if (ioFailure || result.m_nLobbiesMatching == 0)
            return;

        onFound?.Invoke(SteamMatchmaking.GetLobbyByIndex(0));
    }
}
