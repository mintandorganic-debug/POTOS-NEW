using Mirror;
using UnityEngine;

public static class GameLog
{
    public static bool Enabled = true;

    public static void Write(
        string source,
        string message)
    {
        if (!Enabled)
            return;

        Debug.Log(
            $"[POTOS {Time.frameCount} {Role()}] {source}: {message}");
    }

    private static string Role()
    {
        if (NetworkServer.active &&
            NetworkClient.active)
        {
            return "HOST";
        }

        if (NetworkServer.active)
            return "SERVER";

        if (NetworkClient.active)
            return "CLIENT";

        return "OFFLINE";
    }
}
