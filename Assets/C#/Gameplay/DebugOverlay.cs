using Mirror;
using UnityEngine;

public class DebugOverlay : MonoBehaviour
{
    public static string LookTarget = "-";

    public static string LastAction = "-";

    private GUIStyle style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject host =
            new GameObject("DebugOverlay");

        DontDestroyOnLoad(host);

        host.AddComponent<DebugOverlay>();
    }

    private void OnGUI()
    {
        if (!DebugDeal.Enabled)
            return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };

            style.normal.textColor = Color.cyan;
        }

        GameMaster master =
            GameMaster.Instance;

        string role =
            NetworkServer.active && NetworkClient.active
                ? "HOST"
                : NetworkServer.active
                    ? "SERVER"
                    : NetworkClient.active
                        ? "CLIENT"
                        : "OFFLINE";

        string text =
            $"DEBUG  role={role}\n" +
            $"phase={(master != null ? master.CurrentPhase.ToString() : "-")}  turn={(master != null ? master.CurrentPlayerIndex.ToString() : "-")}\n" +
            $"look={LookTarget}\n" +
            $"last={LastAction}";

        GUI.Label(
            new Rect(12f, 12f, 900f, 120f),
            text,
            style);
    }
}
