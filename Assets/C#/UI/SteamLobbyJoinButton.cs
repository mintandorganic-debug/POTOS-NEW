using Steamworks;
using UnityEngine;

public class SteamLobbyJoinButton : MonoBehaviour
{
    private SteamLobbyFinder finder;

    private void Start()
    {
        finder = new SteamLobbyFinder();
    }

    private void OnDestroy()
    {
        finder?.Dispose();
        finder = null;
    }

    public void Join()
    {
        if (!SteamManager.Initialized || finder == null)
            return;

        finder.Find(JoinLobby);
    }

    private static void JoinLobby(CSteamID lobby)
    {
        SteamMatchmaking.JoinLobby(lobby);
    }
}
