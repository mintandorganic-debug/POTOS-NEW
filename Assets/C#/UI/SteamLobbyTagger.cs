using Steamworks;
using UnityEngine;

public class SteamLobbyTagger : MonoBehaviour
{
    private Callback<LobbyCreated_t> lobbyCreated;

    private void Start()
    {
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
    }

    private void OnDestroy()
    {
        lobbyCreated?.Dispose();
        lobbyCreated = null;
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
            return;

        SteamMatchmaking.SetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            SteamLobbyTag.Key,
            SteamLobbyTag.Value);
    }
}
