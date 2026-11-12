using Steamworks;
using UnityEngine;

public class SteamLobbyJoinButton : MonoBehaviour
{
    private SteamLobbyFinder finder;

    private void Start()
    {
        GameLog.Write(
            "SteamLobbyJoinButton",
            "Start creating SteamLobbyFinder");

        finder = new SteamLobbyFinder();
    }

    private void OnDestroy()
    {
        GameLog.Write(
            "SteamLobbyJoinButton",
            $"OnDestroy hasFinder={finder != null}");

        finder?.Dispose();
        finder = null;
    }

    public void Join()
    {
        GameLog.Write(
            "SteamLobbyJoinButton",
            $"Join button pressed steamInitialized={SteamManager.Initialized} hasFinder={finder != null}");

        if (!SteamManager.Initialized || finder == null)
        {
            GameLog.Write(
                "SteamLobbyJoinButton",
                "Join aborted: Steam not initialized or finder missing");

            return;
        }

        finder.Find(JoinLobby);
    }

    private static void JoinLobby(CSteamID lobby)
    {
        GameLog.Write(
            "SteamLobbyJoinButton",
            $"JoinLobby lobbyId={lobby.m_SteamID}");

        SteamMatchmaking.JoinLobby(lobby);
    }
}
