using Steamworks;
using System;

public class SteamLobbyFinder : IDisposable
{
    private readonly CallResult<LobbyMatchList_t> lobbyMatchList;

    private Action<CSteamID> onFound;

    public SteamLobbyFinder()
    {
        GameLog.Write(
            "SteamLobbyFinder",
            "Constructor creating LobbyMatchList call result");

        lobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
    }

    public void Find(Action<CSteamID> onFound)
    {
        GameLog.Write(
            "SteamLobbyFinder",
            $"Find hasCallback={onFound != null} tagKey={SteamLobbyTag.Key} tagValue={SteamLobbyTag.Value}");

        this.onFound = onFound;

        SteamMatchmaking.AddRequestLobbyListStringFilter(
            SteamLobbyTag.Key,
            SteamLobbyTag.Value,
            ELobbyComparison.k_ELobbyComparisonEqual);

        SteamMatchmaking.AddRequestLobbyListDistanceFilter(
            ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(1);

        GameLog.Write(
            "SteamLobbyFinder",
            "Find requesting lobby list (worldwide, max 1 result)");

        lobbyMatchList.Set(SteamMatchmaking.RequestLobbyList());
    }

    public void Dispose()
    {
        GameLog.Write(
            "SteamLobbyFinder",
            "Dispose");

        lobbyMatchList.Dispose();
    }

    private void OnLobbyMatchList(LobbyMatchList_t result, bool ioFailure)
    {
        GameLog.Write(
            "SteamLobbyFinder",
            $"OnLobbyMatchList ioFailure={ioFailure} lobbiesMatching={result.m_nLobbiesMatching}");

        if (ioFailure || result.m_nLobbiesMatching == 0)
        {
            GameLog.Write(
                "SteamLobbyFinder",
                $"OnLobbyMatchList no lobby to join ioFailure={ioFailure} lobbiesMatching={result.m_nLobbiesMatching}");

            return;
        }

        GameLog.Write(
            "SteamLobbyFinder",
            $"OnLobbyMatchList invoking onFound hasCallback={onFound != null}");

        onFound?.Invoke(SteamMatchmaking.GetLobbyByIndex(0));
    }
}
