using Steamworks;
using UnityEngine;

public class SteamLobbyTagger : MonoBehaviour
{
    private Callback<LobbyCreated_t> lobbyCreated;

    private void Start()
    {
        GameLog.Write(
            "SteamLobbyTagger",
            "Start registering LobbyCreated callback");

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
    }

    private void OnDestroy()
    {
        GameLog.Write(
            "SteamLobbyTagger",
            $"OnDestroy hasCallback={lobbyCreated != null}");

        lobbyCreated?.Dispose();
        lobbyCreated = null;
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        GameLog.Write(
            "SteamLobbyTagger",
            $"OnLobbyCreated result={callback.m_eResult} lobbyId={callback.m_ulSteamIDLobby}");

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            GameLog.Write(
                "SteamLobbyTagger",
                $"OnLobbyCreated skipped tagging, result={callback.m_eResult}");

            return;
        }

        SteamMatchmaking.SetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            SteamLobbyTag.Key,
            SteamLobbyTag.Value);

        GameLog.Write(
            "SteamLobbyTagger",
            $"OnLobbyCreated lobby tagged lobbyId={callback.m_ulSteamIDLobby} key={SteamLobbyTag.Key} value={SteamLobbyTag.Value}");
    }
}
